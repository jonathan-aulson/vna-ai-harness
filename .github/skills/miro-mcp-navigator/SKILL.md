---
name: miro-mcp-navigator
description: 'Use this skill when the user wants to work with Miro boards through the Miro MCP: search or read boards, summarize board context, create or update documents, tables, diagrams, widgets, manage comments, or troubleshoot Miro MCP setup and authentication. Use this even if the user says "whiteboard," "canvas," "retro board," "diagram this in Miro," "read this board," or provides a miro.com board URL without explicitly naming MCP.'
---

# Miro MCP Navigator

## Purpose

Help users safely and effectively use the Miro MCP server to read, search, summarize, and update Miro boards.

This guidance is based on Miro's official MCP documentation:

- Miro MCP Introduction: https://developers.miro.com/docs/mcp-intro
- Miro MCP Server Overview: https://developers.miro.com/docs/miro-mcp
- Miro MCP Tools & Prompts: https://developers.miro.com/docs/miro-mcp-prompts
- Connecting to Miro MCP: https://developers.miro.com/docs/connecting-to-miro-mcp
- Miro MCP FAQ and Troubleshooting: https://developers.miro.com/docs/miro-mcp-server-faq-and-troubleshooting

---

## Core Behavior

When helping with Miro MCP tasks:

1. Use Miro MCP tools directly when available.
2. Do not guess board content.
3. Prefer read-before-write.
4. Do not create a new board without explicit user confirmation.
5. Use the least expensive read tool that satisfies the request.
6. Respect Miro permissions and team-specific authorization.
7. Cite board names, item names, or URLs when answering from board content.
8. Be transparent before performing substantial write operations.

---

## Miro MCP Setup Reference

The Miro MCP remote server URL is:

```text
https://mcp.miro.com/
```

Manual MCP configuration example:

```json
{
  "mcpServers": {
    "miro-mcp": {
      "url": "https://mcp.miro.com/",
      "disabled": false,
      "autoApprove": []
    }
  }
}
```

Miro MCP uses:

- OAuth 2.1 authentication
- Dynamic client registration
- Existing Miro board permissions
- Standard Miro API rate limits

For Enterprise accounts, an administrator may need to enable MCP at the organization and team level.

---

## Tool Selection Guide

### Board Discovery and Reading

| User intent | Preferred tool | Notes |
|---|---|---|
| Search accessible boards | `board_search_boards` | Use when the user asks to find a board by name or topic. |
| List raw board items | `board_list_items` | Use for item-level inventory with pagination. |
| Explore high-level board structure | `context_explore` | Use for frames, docs, tables, diagrams, prototypes, and board organization. |
| Summarize a board or item | `context_get` | Use when the user wants a natural-language summary. May use AI credits. |
| Read layout/DSL | `layout_read` | Use before updating board items or layout. |

### Documents

| User intent | Tool | Rule |
|---|---|---|
| Create a document | `doc_create` | Requires a board URL or confirmed new-board creation. |
| Read a document | `doc_get` | Requires an item URL with `moveToWidget` or `focusWidget`. |
| Update a document | `doc_update` | Read first, then use exact find-and-replace strings. |

### Tables

| User intent | Tool | Rule |
|---|---|---|
| Create a table | `table_create` | Define columns clearly. |
| Read table rows | `table_list_rows` | Use filters and limits where possible. |
| Add or update rows | `table_sync_rows` | Use stable `rowId`s from `table_list_rows` when updating existing rows. |

### Layouts and Widgets

| User intent | Tool | Rule |
|---|---|---|
| Get layout DSL spec | `layout_get_dsl` | Required before `layout_create`. |
| Create multiple board items | `layout_create` | Use clear x/y coordinates and avoid overlap. |
| Read existing layout | `layout_read` | Required before `layout_update`. |
| Update existing layout | `layout_update` | Use exact find-and-replace strings from prior read. |
| Create code widget | `code_widget_create` | Use for source code snippets on a board. |
| Read code widget | `code_widget_get` | Read before update/delete. |
| Update code widget | `code_widget_update` | Read first. |
| Delete code widget | `code_widget_delete` | Confirm before deletion unless user clearly requested it. |
| List code widgets | `code_widget_list_items` | Use to locate code widgets on a board. |

### Diagrams

| User intent | Tool | Rule |
|---|---|---|
| Get diagram DSL spec | `diagram_get_dsl` | Required before creating each diagram type. |
| Create diagram | `diagram_create` | Follow the returned DSL spec exactly. |

Supported diagram types may include: `flowchart`, `uml_class`, `uml_sequence`, `entity_relationship`.

### Images

| User intent | Tool | Rule |
|---|---|---|
| Get image download URL | `image_get_url` | Use for image items on a board. |
| Get image data | `image_get_data` | Use for metadata or base64 image data. |
| Get upload URL | `image_get_upload_url` | Use before uploading an image. |
| Create image item | `image_create` | Provide either an image URL or upload token. |

### Comments

| User intent | Tool | Rule |
|---|---|---|
| List comments | `comment_list_comments` | Use before replying or resolving. |
| Create comment | `comment_create` | Can attach to a board or item. |
| Reply to comment | `comment_reply` | Use comment ID from `comment_list_comments`. |
| Resolve comment | `comment_resolve` | Confirm which comment to resolve if ambiguous. |

---

## Mandatory Preconditions

Follow these rules strictly:

1. Call `layout_get_dsl` before `layout_create` in a conversation.
2. Call `diagram_get_dsl` before `diagram_create` for each diagram type used in a conversation.
3. Call one of the following before editing existing content:
   - `layout_read`
   - `doc_get`
   - `table_list_rows`
   - `code_widget_get`
   - `comment_list_comments`
4. For `table_sync_rows`, use stable `rowId`s from `table_list_rows` when updating existing rows.
5. For `doc_update` and `layout_update`, use exact find-and-replace strings from a prior read.
6. Do not create a new board unless the user explicitly confirms the board name and intent.

---

## Safety Rules

### Read-before-write

Before updating existing content:

- Read the current item.
- Identify the exact text, row, widget, or layout element to update.
- Make the smallest necessary change.
- Summarize what changed.

### Board creation

Do not call `board_create` unless:

- The user explicitly asks to create a new board, and
- The user confirms the board name or accepts a suggested name.

If the user says something like "make me a Miro board," ask:

> Do you want me to create a new Miro board? If so, what should I name it?

### Substantial writes

Before large updates, summarize the intended action:

> I'm going to add three frames, a workshop agenda document, and an action-items table to the board. Proceed?

### Deletion and resolving

For deletes or resolving comments:

- Confirm if the target is ambiguous.
- Do not delete or resolve multiple items unless the user clearly requested it.

---

## Common Workflows

See [workflows reference](./references/workflows.md) for detailed step-by-step procedures for:

- Finding a board
- Summarizing a board
- Exploring board structure
- Reading and updating documents
- Reading and updating tables
- Creating diagrams
- Creating and updating layouts
- Reviewing comments

---

## Troubleshooting

See [troubleshooting reference](./references/troubleshooting.md) for guidance on:

- "Different Team" authorization errors
- Missing MCP tools
- Invalid response during authentication
- Permission errors

---

## Response Style

Use concise, action-oriented responses.

For board lists, items, comments, or proposed changes, use Markdown tables.

For summaries, use this structure:

```markdown
## Summary

...

## Key Sections

- ...

## Decisions

- ...

## Action Items

| Action | Owner | Due Date | Source |
|---|---|---|---|

## Source

- Board: ...
```

For write operations, use this structure:

```markdown
## Planned Change

I will:

- ...
- ...
- ...

Please confirm before I proceed.
```

After completing a write:

```markdown
## Done

I updated the Miro board.

| Change | Result |
|---|---|
| ... | ... |

Source: ...
```

---

## Important Constraints

- Never invent board content.
- Never assume the user has edit permissions.
- Never create a board without confirmation.
- Never update existing content without reading it first.
- Never use approximate find-and-replace strings for document or layout updates.
- Never ignore team-specific authorization errors.
- Prefer exact board and item URLs whenever possible.
