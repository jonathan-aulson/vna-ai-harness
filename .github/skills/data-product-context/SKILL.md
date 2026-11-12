---
name: data-product-context
description: 'How to gather and structure ADO work item context into the standard work-item-{id}.md format. Schema includes: US details, numbered AC list, task table, completed work summaries, Figma links, external dependencies.'
---

# Data Product Context Skill

## Purpose

Standardize how work item context is gathered from Azure DevOps and structured into a session context file that all agents can reference.

## Output File

`.github/session/work-item-{id}.md`

## Schema

```markdown
# US-{id}: {Title}

## Description
{User story description text}

## Acceptance Criteria
1. {AC 1}
2. {AC 2}
3. {AC 3}
...

## Tasks
| ID | Title | State | Assigned |
|----|-------|-------|----------|
| TASK-{id} | {title} | {state} | {assignee} |

## Completed Work
{Summary of previously completed tasks and their outcomes}

## External Links
- Figma: {url or "N/A"}
- API Spec: {url or "N/A"}

## Dependencies
- {Dependency 1: description and status}
- {Dependency 2: description and status}

## Sprint Context
- Sprint: {sprint name}
- Iteration Path: {path}
- Priority: {priority}
```

## Gathering Rules

- Use ADO MCP tools to fetch all data — never guess content.
- Include ALL acceptance criteria — do not summarize or truncate.
- Preserve original formatting of descriptions.
- If Figma links are in description or attachments, extract and list them.
- List external dependencies that affect implementation.
