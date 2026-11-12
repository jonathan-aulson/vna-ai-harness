---
name: ac-validation
description: 'How to parse, classify, and validate acceptance criteria. Defines the standard PASS / FAIL / PARTIAL output format with evidence and confidence score used by all reviewer agents.'
---

# AC Validation Skill

## Purpose

Standardized acceptance criteria validation format used by all reviewer and QA agents.

## Parsing Acceptance Criteria

1. Extract numbered AC list from work item context.
2. Classify each AC by domain: Backend, Frontend, Data, Integration, UI.
3. Identify testable assertions in each criterion.

## Validation Output Format

For **each** acceptance criterion:

```
AC #{number}: {full criterion text}
Domain: {Backend | Frontend | Data | Integration | UI}
Status: {PASS | FAIL | PARTIAL}
Evidence: {Specific file, test, or observation that proves the status}
Confidence: {0.0 - 1.0}
```

## Status Definitions

| Status | Meaning |
|--------|---------|
| PASS | Criterion fully satisfied with evidence |
| FAIL | Criterion not met — specific gap identified |
| PARTIAL | Criterion partially met — specifics on what's missing |

## Confidence Scale

| Range | Meaning |
|-------|---------|
| 0.9 - 1.0 | Verified by automated test or direct observation |
| 0.7 - 0.89 | High confidence from code inspection |
| 0.5 - 0.69 | Moderate confidence — should flag for human review |
| < 0.5 | Low confidence — requires human verification |

## Summary Template

```
## AC Validation Summary
Total: {n}
PASS: {count} | FAIL: {count} | PARTIAL: {count}
Overall Confidence: {average}
Recommendation: {APPROVE | REQUEST_CHANGES | ESCALATE}
```

## Rules

- Every AC must have a status — never skip one.
- Confidence below 0.7 should flag for human review.
- Evidence must be specific (file path, test name, or observable behavior).
- PARTIAL must explain exactly what's missing.
