# Common Workflows

## Find a board

When the user says:

- "Find my Miro board about X"
- "Search for the retro board"
- "Where's the planning board?"

Workflow:

1. Use `board_search_boards` with the search term.
2. Present matching board names, descriptions, and URLs.
3. If results are ambiguous, ask the user to choose.

---

## Summarize a board

When the user says:

- "Summarize this board"
- "What's on this Miro?"
- "Give me the key points from this board"

Workflow:

1. If a board URL is provided, call `context_get`.
2. If no URL is provided, use `board_search_boards`.
3. Present:
   - Executive summary
   - Main sections or frames
   - Key decisions
   - Action items, if present
   - Source board URL

---

## Explore board structure

When the user says:

- "What's on this board?"
- "Show me the frames"
- "List the docs and tables on this board"

Workflow:

1. Use `context_explore`.
2. Present a structured table of frames, documents, tables, diagrams, and notable items.
3. Ask whether the user wants a deeper summary if needed.

---

## Read a specific document

When the user gives a Miro document item URL:

1. Use `doc_get`.
2. Summarize or quote the relevant content.
3. Cite the item URL.

If the user gives only a board URL:

1. Use `context_explore`.
2. Identify candidate documents.
3. Ask which document to read if ambiguous.

---

## Update a document

Workflow:

1. Use `doc_get`.
2. Identify exact text to replace.
3. Use `doc_update` with exact `old_content` and `new_content`.
4. Confirm the update.

Do not paraphrase the `old_content`; it must match exactly.

---

## Read or update a table

To read:

1. Use `table_list_rows`.
2. Present rows in a Markdown table.

To update:

1. Use `table_list_rows` first.
2. Preserve valid column names and select values.
3. Use `table_sync_rows`.
4. Include `rowId` when updating existing rows.
5. Omit `rowId` only when inserting new rows.

---

## Create a diagram

When the user asks:

- "Diagram this in Miro"
- "Create a sequence diagram"
- "Add a flowchart to this board"

Workflow:

1. Confirm or obtain the board URL.
2. Call `diagram_get_dsl` for the diagram type.
3. Create valid DSL based on the returned spec.
4. Call `diagram_create`.
5. Summarize the created diagram and provide the board/item URL if available.

---

## Create sticky notes, frames, or layouts

Workflow:

1. Confirm board URL.
2. Call `layout_get_dsl`.
3. Create layout DSL with:
   - Frames first
   - Clear titles
   - Non-overlapping coordinates
   - Logical grouping
4. Call `layout_create`.
5. Report what was created.

---

## Update existing board layout

Workflow:

1. Use `layout_read`.
2. Identify the exact DSL fragment to replace.
3. Use `layout_update`.
4. Do not modify unrelated items.

---

## Review comments

When the user says:

- "Show unresolved comments"
- "What feedback is on this board?"
- "Reply to this comment"
- "Resolve these comments"

Workflow:

1. Use `comment_list_comments`.
2. Present comments with IDs, authors, status, and text.
3. For replies, use `comment_reply`.
4. For resolving, use `comment_resolve`.
5. Confirm if comment targets are ambiguous.
