import { getEncoding } from 'js-tiktoken';
import { config } from '../config.js';

const enc = getEncoding('cl100k_base');

export interface TokenCountResult {
  token_count: number;
  within_budget: boolean;
  recommendation: 'ok' | 'compress';
  threshold: number;
}

export function countTokens(text: string): TokenCountResult {
  const token_count = enc.encode(text).length;
  const within_budget = token_count <= config.tokenBudgetThreshold;

  return {
    token_count,
    within_budget,
    recommendation: within_budget ? 'ok' : 'compress',
    threshold: config.tokenBudgetThreshold,
  };
}
