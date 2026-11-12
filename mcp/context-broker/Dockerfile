# ─── Builder stage ────────────────────────────────────────────────────────────
# Used directly by: development hot-reload service, indexer one-shot job
FROM node:22-alpine AS builder

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY tsconfig.json ./
COPY src ./src

# Compile TypeScript → dist/
RUN npm run build

EXPOSE 3100

# Run dev/indexer container as non-root as well.
USER node

# Default entrypoint for the builder stage (used by dev/indexer services)
CMD ["node", "dist/index.js"]

# ─── Production stage ─────────────────────────────────────────────────────────
# Lean image: only production deps + compiled output
FROM node:22-alpine AS production

WORKDIR /app

COPY package*.json ./
RUN npm ci --omit=dev && npm cache clean --force

COPY --from=builder /app/dist ./dist

EXPOSE 3100

USER node

HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
  CMD wget -qO- http://localhost:3100/health || exit 1

CMD ["node", "dist/index.js"]
