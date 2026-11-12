---
name: azure-devops-mcp-assistant
description: 'Use this skill when the user asks about Azure DevOps projects, work items, repositories, pull requests, pipelines, test plans, wiki pages, search, teams, iterations, or any live Azure DevOps data or changes. Always prefer Azure DevOps MCP tools over guessing or answering from memory when the request depends on current Azure DevOps state, even if the user does not explicitly mention "MCP".'
---

# Azure DevOps MCP Assistant

## Purpose

Use Azure DevOps MCP tools to answer questions, retrieve live Azure DevOps data, and perform Azure DevOps actions accurately and efficiently.

## Core rule

**This project uses Azure DevOps. Always check to see if the Azure DevOps MCP server has a tool relevant to the user's request.**  
If a tool exists for the task, use it instead of guessing, summarizing from memory, or recommending manual UI navigation.

## When to use this skill

Use this skill whenever the user asks about or wants to modify:

- Azure DevOps projects, teams, and iterations
- Work items, bugs, tasks, user stories, epics, and queries
- Repositories, branches, commits, pull requests, and code search
- Pipelines, builds, releases, and pipeline status
- Test plans, test cases, test runs, and test results
- Wiki pages and project documentation
- Search across Azure DevOps content
- Any task that depends on current Azure DevOps data

Even if the user says things like "show me the latest work," "find the PR," "check the build," or "update the wiki," treat it as an Azure DevOps MCP opportunity.

## Project context

- Organization: `vnamow`
- Project: `mow 2.0`
- Project URL: https://dev.azure.com/vnamow/MOW%202.0
- Project ID: `9d9ef9c7-cd82-4a7d-8139-bed00407d7ef`
- MCP server: `ado-remote-mcp` (HTTP, configured in `.vscode/mcp.json`)

## Tooling principles

- Prefer **Azure DevOps MCP** over manual reasoning for live data.
- Prefer the **smallest relevant scope** of tools and domains.
- Use **read tools first** before any update, delete, or create action.
- When a request is ambiguous, make the **most likely assumption**, but say what you assumed.
- If a write action could affect shared work, summarize the intended change before doing it.

## Domain strategy

Azure DevOps MCP exposes many tools, so keep the loaded surface area focused.

Recommended domains:

- `core` — always enable; fetch project-level information
- `work` — team, iteration, planning, and project workflow
- `work-items` — work item read/write and tracking
- `search` — code and work item search
- `repositories` — repos, branches, commits, pull requests
- `pipelines` — builds, releases, and pipeline operations
- `test-plans` — test cases, test suites, and test runs
- `wiki` — wiki pages and documentation
- `advanced-security` — security-related analysis and data

If only one part of Azure DevOps is needed, prefer loading only the domains that matter.

## Recommended workflow

1. Identify the Azure DevOps area involved.
2. Check whether an MCP tool exists for that action.
3. Use the narrowest tool/domain that fits.
4. For read requests, fetch the actual data from Azure DevOps.
5. For write requests, confirm the target and the intended change if needed.
6. Present results clearly with names, IDs, URLs, and status where relevant.

## Common intent mappings

### Projects and teams
- List projects → `mcp_ado-remote-mc_core_list_projects`
- Find a project by name → `mcp_ado-remote-mc_core_list_projects`
- Show teams in a project → `mcp_ado-remote-mc_core_list_project_teams`
- Get iterations or team settings → `mcp_ado-remote-mc_work`

### Work items
- Find work items by title, state, assignee, or iteration → `mcp_ado-remote-mc_wit_query` or `mcp_ado-remote-mc_search_workitem`
- Show open bugs, tasks, stories, or epics → `mcp_ado-remote-mc_wit_query`
- Create or update a work item → `mcp_ado-remote-mc_wit_work_item_write`
- Add comments → `mcp_ado-remote-mc_wit_work_item_comment_write`
- Add links or attachments → `mcp_ado-remote-mc_wit_work_item_link_write`, `mcp_ado-remote-mc_wit_work_item_attachment`
- Move items between states → `mcp_ado-remote-mc_wit_work_item_write`
- Find work items in the current sprint → `mcp_ado-remote-mc_wit_query`
- View backlog → `mcp_ado-remote-mc_wit_backlog`

### Repositories and pull requests
- List repos in a project → `mcp_ado-remote-mc_repo_repository`
- Search code or files → `mcp_ado-remote-mc_search_code`
- Show recent commits → `mcp_ado-remote-mc_repo_search_commits`
- List open PRs → `mcp_ado-remote-mc_repo_pull_request`
- Review PR status, threads, or reviewers → `mcp_ado-remote-mc_repo_pull_request`, `mcp_ado-remote-mc_repo_pull_request_thread`
- Create or update PRs → `mcp_ado-remote-mc_repo_pull_request_write`
- Find branches → `mcp_ado-remote-mc_repo_branch`
- Create branches → `mcp_ado-remote-mc_repo_create_branch`
- Get file contents → `mcp_ado-remote-mc_repo_file`

### Pipelines and builds
- List pipelines → `mcp_ado-remote-mc_pipelines_definition`
- Show recent builds → `mcp_ado-remote-mc_pipelines_build`
- Check build status and failures → `mcp_ado-remote-mc_pipelines_build`
- View build logs → `mcp_ado-remote-mc_pipelines_build_log`
- Get artifacts → `mcp_ado-remote-mc_pipelines_artifact`
- Run a pipeline → `mcp_ado-remote-mc_pipelines_run`

### Test plans
- List test plans and suites → `mcp_ado-remote-mc_testplan`
- Show test cases → `mcp_ado-remote-mc_testplan`
- Create or update test cases → `mcp_ado-remote-mc_testplan_test_case_write`
- Review test results → `mcp_ado-remote-mc_testplan_show_test_results_from_build_id`

### Wiki
- Get wiki content → `mcp_ado-remote-mc_wiki`
- Search wiki → `mcp_ado-remote-mc_search_wiki`
- Create or update wiki pages → `mcp_ado-remote-mc_wiki_upsert_page`

## Response style

- Be concise and factual.
- Use Azure DevOps names exactly as they appear in the system.
- Include IDs, links, and statuses when useful.
- If the result is partial, say so.
- If there are multiple matches, show the best candidates and explain how you chose them.

## Safety and confirmation

- Ask for confirmation before destructive changes like deletes, mass updates, or closing many items.
- For write actions that affect shared work, confirm the target project/item if it is not obvious.
- Never claim a change was made unless the MCP tool successfully completed it.
