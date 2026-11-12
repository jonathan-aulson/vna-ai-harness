export const config = {
  port: parseInt(process.env.PORT ?? '3100'),
  defaultCompressionRatio: 0.5,
  tokenBudgetThreshold: parseInt(process.env.TOKEN_BUDGET_THRESHOLD ?? '700'),
  sessionTokenThreshold: parseInt(process.env.SESSION_TOKEN_THRESHOLD ?? '1500'),
} as const;
