import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { countTokens } from './tools/tokens.js';
import { stripHtml } from './tools/html.js';

export function createMcpServer(): McpServer {
  const server = new McpServer({
    name: 'context-broker',
    version: '1.0.0',
  });

  // ── count_tokens ────────────────────────────────────────────────────────────
  server.tool(
    'count_tokens',
    'Count tokens in text and decide if compression is needed',
    {
      text: z.string().describe('Text to tokenize'),
    },
    async ({ text }) => {
      const result = countTokens(text);
      return { content: [{ type: 'text', text: JSON.stringify(result) }] };
    },
  );

  // ── strip_html ──────────────────────────────────────────────────────────────
  server.tool(
    'strip_html',
    'Strip HTML tags and decode entities from ADO field values (Description, AcceptanceCriteria). Returns clean plain text.',
    {
      html: z.string().describe('HTML string to clean — typically System.Description or Microsoft.VSTS.Common.AcceptanceCriteria from ADO'),
    },
    async ({ html }) => {
      const text = stripHtml(html);
      return { content: [{ type: 'text', text }] };
    },
  );

  return server;
}
