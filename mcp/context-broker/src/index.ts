import Fastify from 'fastify';
import { StreamableHTTPServerTransport } from '@modelcontextprotocol/sdk/server/streamableHttp.js';
import { createMcpServer } from './server.js';
import { config } from './config.js';

const app = Fastify({
  logger: {
    level: process.env.LOG_LEVEL ?? 'info',
    transport:
      process.env.NODE_ENV !== 'production'
        ? { target: 'pino-pretty' }
        : undefined,
  },
});

// ── Health check ─────────────────────────────────────────────────────────────
app.get('/health', async () => ({
  status: 'ok',
  service: 'context-broker',
  version: '1.0.0',
  tools: ['count_tokens', 'strip_html'],
}));

// ── MCP endpoint (Streamable HTTP, stateless) ─────────────────────────────────
app.post('/mcp', async (req, reply) => {
  reply.hijack();

  const server = createMcpServer();
  const transport = new StreamableHTTPServerTransport({
    sessionIdGenerator: undefined,
  });

  try {
    await server.connect(transport);
    await transport.handleRequest(req.raw, reply.raw, req.body);
  } catch (err) {
    app.log.error({ err }, 'MCP request failed');
    if (!reply.raw.headersSent) {
      reply.raw.writeHead(500, { 'Content-Type': 'application/json' });
      reply.raw.end(JSON.stringify({ error: 'Internal MCP error' }));
    }
  } finally {
    await server.close().catch(() => undefined);
  }
});

// ── 404 for unknown routes ────────────────────────────────────────────────────
app.setNotFoundHandler((_req, reply) => {
  reply.status(404).send({ error: 'Not found' });
});

// ── Start ─────────────────────────────────────────────────────────────────────
try {
  const address = await app.listen({ port: config.port, host: '0.0.0.0' });
  app.log.info(`context-broker MCP running at ${address}/mcp`);
} catch (err) {
  app.log.error(err);
  process.exit(1);
}
