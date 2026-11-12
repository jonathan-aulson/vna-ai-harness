# Velocity Championship — Daily Automation

Generates the two "Velocity Championship" HTML dashboards
(`docs/velocity/velocity-championship.html` for ICs,
`docs/velocity/team-leads-championship.html` for Team Leads) from live Azure
Boards data, and keeps a running history so the trend chart shows real
day-over-day movement.

## How it works

- `scripts/velocity/config.json` — org/project/iteration paths + the Team
  Leads roster (people excluded from the IC board and shown on the Leads
  board instead).
- `scripts/velocity/generate_velocity_championship.py` — queries Azure
  DevOps (WIQL + work item batch API) for Sprint 0–2 User Stories/Tasks/Bugs,
  computes velocity credit per person (proportional by subtask count, not
  story owner), updates `data/velocity_history.json`, and renders both HTML
  files from `scripts/velocity/template.html`.
- `pipelines/velocity-championship-pipeline.yml` — Azure Pipelines schedule
  that runs the script daily and commits the regenerated files back to
  `develop`.

## One-time setup (you need to do this)

1. **Create an Azure DevOps PAT** with **Work Items (Read)** scope only.
2. In Azure DevOps → Pipelines → Library, create a variable group named
   `velocity-championship-secrets` with a secret variable `ADO_PAT` set to
   that token.
3. In Azure DevOps → Pipelines, create a new pipeline pointing at
   `pipelines/velocity-championship-pipeline.yml`.
4. Grant the pipeline's build service account **Contribute** permission on
   this repo (Project Settings → Repositories → Security) so it can push the
   daily commit back to `develop`.
5. (Optional) Adjust `scripts/velocity/config.json` — e.g. update
   `iteration_paths` each time you want the "first 3 sprints" window to
   shift, or add/remove people from `team_leads`.

## Running manually

```bash
pip install requests
ADO_PAT=<your PAT> python3 scripts/velocity/generate_velocity_championship.py
```

## Schedule notes

Azure Pipelines cron triggers are UTC-only and don't observe DST. The
pipeline schedules two triggers (13:00 and 14:00 UTC) to cover both CDT and
CST, and the script itself only proceeds if the current time in
`America/Chicago` is 8am — so exactly one of the two daily triggers actually
does work.
