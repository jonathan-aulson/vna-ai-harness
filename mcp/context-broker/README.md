# Context Broker MCP

A lightweight Model Context Protocol server that provides utility tools to the AI orchestration pipeline.

**Zero external API calls. No local model downloads. No external dependencies.**

---

## Tools

All tools are exposed via MCP JSON-RPC over HTTP at `POST /mcp`.

### `count_tokens`

Counts tokens using `cl100k_base` (js-tiktoken, GPT-4 tokenizer). Returns whether the text is within budget and a compression recommendation.

```json
{ "tool": "count_tokens", "arguments": { "text": "..." } }
```

```json
{
  "token_count": 920,
  "within_budget": false,
  "recommendation": "compress",
  "threshold": 700
}
```

**Used at:** P7a3 (brief gate), P5 (Context7 chunk gate), P7e (findings gate), P7h2 (session file gate).

---

### `strip_html`

Strips HTML tags and decodes entities from ADO field values (`System.Description`, `Microsoft.VSTS.Common.AcceptanceCriteria`). Returns clean plain text.

```json
{ "tool": "strip_html", "arguments": { "html": "<p>Some <b>HTML</b> content</p>" } }
```

```
Some HTML content
```

**Used by:** `ado-agent` — before writing Description or AcceptanceCriteria to session files.

---

## Quick Start

### Prerequisites

- Docker 24+ with the Compose plugin (`docker compose`)
- Node.js 22+ (only needed if running outside Docker)

### 1. Configure environment

```bash
# from mcp/context-broker/
cp .env.example .env
```

### 2. Start the stack

```bash
docker compose up -d
```

Check health:

```bash
curl http://localhost:3100/health
# {"status":"ok","service":"context-broker","tools":["count_tokens","strip_html"]}
```

### 3. Register the MCP in your agent config

`.vscode/mcp.json`:

```json
{
  "servers": {
    "context-broker": {
      "type": "http",
      "url": "http://localhost:3100/mcp"
    }
  }
}
```

---

## Environment Variables

| Variable | Default | Description |
|---|---|---|
| `BROKER_HOST_PORT` | `3100` | Host port for the Context Broker |
| `BROKER_DEV_PORT` | `3101` | Host port for the hot-reload dev service |
| `TOKEN_BUDGET_THRESHOLD` | `700` | Token count above which `count_tokens` recommends compression |

---

## Docker Compose Reference

| Command | What it does |
|---|---|
| `docker compose up -d` | Start Context Broker in background |
| `docker compose down` | Stop and remove containers |
| `docker compose up -d --build context-broker` | Rebuild and restart the broker |
| `docker compose --profile dev up` | Start with hot-reload broker on port 3101 |
| `docker compose logs -f context-broker` | Tail broker logs |

---

## Development Mode

```bash
docker compose --profile dev up
```

Or run directly without Docker:

```bash
# from mcp/context-broker/
npm install
npm run dev   # tsx watch — hot reload
```

---

## Project Structure

```
mcp/context-broker/
├── src/
│   ├── index.ts      Fastify server — /health + /mcp endpoints
│   ├── server.ts     MCP server factory — tool registration
│   ├── config.ts     Environment configuration
│   └── tools/
│       ├── tokens.ts count_tokens — js-tiktoken, cl100k_base
│       └── html.ts   strip_html — tag removal and entity decoding
├── dist/             Compiled output (git-ignored)
├── node_modules/     (git-ignored)
├── Dockerfile
├── docker-compose.yml
├── .env.example
├── package.json
└── tsconfig.json
```

---

## Technical Details

### MCP transport

Uses **Streamable HTTP** (`StreamableHTTPServerTransport` from `@modelcontextprotocol/sdk`). Each `POST /mcp` request creates a fresh `McpServer` + transport instance and tears down after — fully stateless.

### Token counting

Uses **js-tiktoken** with `cl100k_base` (GPT-4 tokenizer). Initialized once at module load — synchronous, no network.

### HTML stripping

Pure string processing — no DOM parser. Converts block elements to newlines, strips all tags, decodes common HTML entities (`&amp;`, `&nbsp;`, `&quot;`, `&lt;`, `&gt;`, numeric `&#N;`), and normalizes whitespace.
