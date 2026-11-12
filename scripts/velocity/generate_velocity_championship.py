#!/usr/bin/env python3
"""
Generates the daily Velocity Championship dashboards (IC + Team Leads) from
Azure DevOps work item data.

Auth: expects an Azure DevOps Personal Access Token (Work Items: Read scope)
in the ADO_PAT environment variable.

Usage:
    ADO_PAT=xxxxx python3 scripts/velocity/generate_velocity_championship.py

Config lives in scripts/velocity/config.json (org, project, iteration paths,
team leads roster). Historical daily snapshots accumulate in
data/velocity_history.json so the trend chart grows over time.
"""
import base64
import json
import os
import re
import sys
from collections import defaultdict
from datetime import datetime, timedelta
from pathlib import Path
from zoneinfo import ZoneInfo

import requests

ROOT = Path(__file__).resolve().parents[2]
SCRIPT_DIR = Path(__file__).resolve().parent
CONFIG_PATH = SCRIPT_DIR / "config.json"
TEMPLATE_PATH = SCRIPT_DIR / "template.html"
HISTORY_PATH = ROOT / "data" / "velocity_history.json"
OUTPUT_DIR = ROOT / "docs" / "velocity"

API_VERSION = "7.1"


def load_config():
    with open(CONFIG_PATH) as f:
        return json.load(f)


def ado_auth_header(pat: str) -> dict:
    token = base64.b64encode(f":{pat}".encode()).decode()
    return {"Authorization": f"Basic {token}", "Content-Type": "application/json"}


def name_only(assignee):
    if not assignee:
        return None
    m = re.match(r"^(.*?)\s*<", assignee)
    return m.group(1).strip() if m else assignee.strip()


def run_wiql(org, project, headers, iteration_paths):
    url = f"https://dev.azure.com/{org}/{project}/_apis/wit/wiql?api-version={API_VERSION}"
    iterations_clause = ",".join(f"'{p}'" for p in iteration_paths)
    wiql = {
        "query": (
            "SELECT [System.Id] FROM WorkItems "
            f"WHERE [System.TeamProject] = '{project}' "
            f"AND [System.IterationPath] IN ({iterations_clause}) "
            "AND [System.WorkItemType] IN ('User Story','Task','Bug') "
            "ORDER BY [System.IterationPath], [System.Id]"
        )
    }
    resp = requests.post(url, headers=headers, json=wiql, timeout=60)
    resp.raise_for_status()
    return [w["id"] for w in resp.json().get("workItems", [])]


def get_batch(org, headers, ids):
    url = f"https://dev.azure.com/{org}/_apis/wit/workitemsbatch?api-version={API_VERSION}"
    fields = [
        "System.Id", "System.WorkItemType", "System.Title", "System.State",
        "System.AssignedTo", "System.IterationPath",
        "Microsoft.VSTS.Scheduling.StoryPoints", "System.Parent",
    ]
    items = []
    for i in range(0, len(ids), 200):
        chunk = ids[i:i + 200]
        body = {"ids": chunk, "fields": fields}
        resp = requests.post(url, headers=headers, json=body, timeout=60)
        resp.raise_for_status()
        items.extend(resp.json().get("value", []))
    return items


def fetch_work_items(cfg):
    pat = os.environ.get("ADO_PAT")
    if not pat:
        print("ERROR: ADO_PAT environment variable is not set.", file=sys.stderr)
        sys.exit(1)
    headers = ado_auth_header(pat)
    org = cfg["ado_org"]
    project = cfg["ado_project"]
    ids = run_wiql(org, project, headers, cfg["iteration_paths"])
    if not ids:
        print("WARNING: WIQL query returned no work items.", file=sys.stderr)
        return []
    return get_batch(org, headers, ids)


def compute_velocity(items):
    """Proportional-by-subtask-count velocity allocation.

    Story points live on the User Story, but credit goes to whoever owns the
    child Tasks/Bugs under that story (not the story's own assignee), split
    proportionally by how many subtasks each person owns. Only Closed
    stories with points count. Stories with zero subtasks fall back to
    crediting the story's own assignee.
    """
    stories, tasks = {}, []
    for it in items:
        f = it["fields"]
        wid = f["System.Id"]
        if f["System.WorkItemType"] == "User Story":
            stories[wid] = {
                "id": wid,
                "state": f.get("System.State"),
                "points": f.get("Microsoft.VSTS.Scheduling.StoryPoints"),
                "iteration": f.get("System.IterationPath"),
                "owner": name_only(f.get("System.AssignedTo")),
            }
        else:
            tasks.append({
                "parent": f.get("System.Parent"),
                "assignee": name_only(f.get("System.AssignedTo")),
            })

    children_by_parent = defaultdict(list)
    for t in tasks:
        if t["parent"]:
            children_by_parent[t["parent"]].append(t)

    completed = {k: v for k, v in stories.items() if v["state"] == "Closed" and v["points"]}

    person_points = defaultdict(float)
    person_sprint_points = defaultdict(lambda: defaultdict(float))

    for sid, s in completed.items():
        kids = children_by_parent.get(sid, [])
        if not kids:
            owner = s["owner"] or "Unassigned"
            person_points[owner] += s["points"]
            person_sprint_points[owner][s["iteration"]] += s["points"]
            continue
        counts = defaultdict(int)
        for k in kids:
            counts[k["assignee"] or "Unassigned"] += 1
        total_tasks = sum(counts.values())
        for person, cnt in counts.items():
            pts = s["points"] * cnt / total_tasks
            person_points[person] += pts
            person_sprint_points[person][s["iteration"]] += pts

    return person_points, person_sprint_points


def sprint_index(iteration_paths, iteration_path):
    try:
        return iteration_paths.index(iteration_path)
    except ValueError:
        return None


def build_leaderboard(person_points, person_sprint_points, iteration_paths):
    rows = []
    for person, total in person_points.items():
        row = person_sprint_points.get(person, {})
        sprint_vals = [0.0, 0.0, 0.0]
        for path, pts in row.items():
            idx = sprint_index(iteration_paths, path)
            if idx is not None and idx < 3:
                sprint_vals[idx] = round(pts, 2)
        rows.append({
            "name": person,
            "sprint0": sprint_vals[0],
            "sprint1": sprint_vals[1],
            "sprint2": sprint_vals[2],
            "total": round(total, 2),
        })
    rows.sort(key=lambda r: -r["total"])
    return rows


def load_history():
    if HISTORY_PATH.exists():
        with open(HISTORY_PATH) as f:
            return json.load(f)
    return []


def save_history(history):
    HISTORY_PATH.parent.mkdir(parents=True, exist_ok=True)
    with open(HISTORY_PATH, "w") as f:
        json.dump(history, f, indent=2)


def update_history(history, today, leaderboard, max_days):
    history = [row for row in history if row["date"] != today]
    for row in leaderboard:
        history.append({
            "date": today,
            "person": row["name"],
            "sprint0": row["sprint0"],
            "sprint1": row["sprint1"],
            "sprint2": row["sprint2"],
            "total": row["total"],
        })
    cutoff = (datetime.strptime(today, "%Y-%m-%d") - timedelta(days=max_days)).strftime("%Y-%m-%d")
    history = [row for row in history if row["date"] >= cutoff]
    history.sort(key=lambda r: r["date"])
    return history


def history_to_chart_data(history, names):
    dates = sorted({row["date"] for row in history})
    by_person = defaultdict(dict)
    for row in history:
        by_person[row["person"]][row["date"]] = row["total"]
    result = {}
    for name in names:
        series = []
        last = 0.0
        for d in dates:
            if d in by_person.get(name, {}):
                last = by_person[name][d]
            series.append(last)
        result[name] = series
    return dates, result


def render_html(template, leaderboard, dates, history_by_person, last_updated,
                 page_title, page_h1, page_subtitle, bg1, bg2, accent):
    data = {
        "leaderboard": leaderboard,
        "history_dates": dates,
        "history": history_by_person,
    }
    html = template
    html = html.replace("__PAGE_TITLE__", page_title)
    html = html.replace("__PAGE_H1__", page_h1)
    html = html.replace("__PAGE_SUBTITLE__", page_subtitle)
    html = html.replace("__THEME_BG1__", bg1)
    html = html.replace("__THEME_BG2__", bg2)
    html = html.replace("__THEME_ACCENT__", accent)
    html = html.replace("__DATA_JSON__", json.dumps(data))
    html = html.replace("__LAST_UPDATED__", last_updated)
    return html


def main():
    cfg = load_config()
    items = fetch_work_items(cfg)
    person_points, person_sprint_points = compute_velocity(items)
    leaderboard = build_leaderboard(person_points, person_sprint_points, cfg["iteration_paths"])

    now_ct = datetime.now(ZoneInfo("America/Chicago"))
    today = now_ct.strftime("%Y-%m-%d")
    last_updated = now_ct.strftime("%Y-%m-%d %I:%M %p %Z")

    history = load_history()
    history = update_history(history, today, leaderboard, cfg.get("history_max_days", 90))
    save_history(history)

    leads = set(cfg["team_leads"])
    ic_rows = [r for r in leaderboard if r["name"] not in leads]
    lead_rows = [r for r in leaderboard if r["name"] in leads]

    ic_names = [r["name"] for r in ic_rows]
    lead_names = [r["name"] for r in lead_rows]

    ic_history_rows = [row for row in history if row["person"] in ic_names]
    lead_history_rows = [row for row in history if row["person"] in lead_names]

    ic_dates, ic_hist = history_to_chart_data(ic_history_rows, ic_names)
    lead_dates, lead_hist = history_to_chart_data(lead_history_rows, lead_names)

    template = TEMPLATE_PATH.read_text()
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    ic_html = render_html(
        template, ic_rows, ic_dates, ic_hist, last_updated,
        page_title="🏆 MOW 2.0 Velocity Championship",
        page_h1="🏆 MOW 2.0 Velocity Championship",
        page_subtitle="Individual Contributor leaderboard — velocity credited to subtask owners (Sprint 0–2). Team Leads tracked separately.",
        bg1="#1e1b4b", bg2="#4c1d95", accent="#7c3aed",
    )
    (OUTPUT_DIR / "velocity-championship.html").write_text(ic_html)

    lead_html = render_html(
        template, lead_rows, lead_dates, lead_hist, last_updated,
        page_title="👑 MOW 2.0 Team Leads Championship",
        page_h1="👑 MOW 2.0 Team Leads Championship",
        page_subtitle="Leadership leaderboard — velocity credited to subtask owners (Sprint 0–2). Individual contributors tracked separately.",
        bg1="#0c4a6e", bg2="#164e63", accent="#0ea5e9",
    )
    (OUTPUT_DIR / "team-leads-championship.html").write_text(lead_html)

    print(f"Generated dashboards for {today}: {len(ic_rows)} ICs, {len(lead_rows)} leads.")


if __name__ == "__main__":
    main()
