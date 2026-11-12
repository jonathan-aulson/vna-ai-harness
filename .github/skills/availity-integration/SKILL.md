---
name: availity-integration
description: 'Availity REST API integration workflow for OAuth2 client-credentials auth, payer list and configurations lookup, 270/271 eligibility, 278 service reviews, 276/277 claim status, enhanced claim status, patient cost estimator, claim attachments, async polling, and .NET validation/error-handling patterns.'
---

# Availity Integration Skill

## When to Use

- Implementing or updating Availity integrations in .NET.
- Choosing the correct Availity API for eligibility, authorizations, claim status, predeterminations, value-add data, or attachments.
- Adding payer-specific validation without hard-coding payer rules.
- Handling OAuth2 tokens, async polling, pagination, demo scenarios, or Availity error responses.

## Decision Flow

Use this selection order before building a request:

| Need | API |
|------|-----|
| Discover whether a payer and transaction are available for your contract | Availity Payer List |
| Learn payer-specific required fields, regex rules, or field combinations | Configurations |
| Eligibility and benefits snapshot or details | Coverages (270/271) |
| Authorization or referral create/update/search | Service Reviews (278) |
| Standard claim status inquiry | Claim Statuses (276/277) |
| Richer claim history, detail, or value-add status data | Enhanced Claim Status |
| Predetermination or patient estimate | Patient Cost Estimator |
| Care reminders or member ID card assets | E&B Value-Add APIs |
| Medical documentation or claim attachments | Claim Attachments |

When payer-specific behavior matters, call Configurations before constructing the business request.

## Standard Workflow

1. Request an OAuth2 access token using client credentials.
2. Resolve payer support with the Payer List API if payer routing or transaction availability is unknown.
3. Resolve payer-specific rules with the Configurations API for the exact transaction type and subtype.
4. Validate the outbound request against Configurations patterns, allowed values, required fields, and required field combinations.
5. Submit the business request to the correct API.
6. If the API returns `202 Accepted`, poll the `Location` URI until a terminal state is reached.
7. Map the terminal response into internal contracts without exposing PHI in logs.
8. Surface payer or Availity validation issues as domain-safe errors.

## Authentication Rules

- Use OAuth2 client credentials against `/v1/token`.
- Treat tokens as short-lived. Availity tokens expire after 300 seconds.
- Cache the token briefly, but refresh proactively before expiry instead of retrying on every `401`.
- Scope selection matters. Some implementations need both standard and demo scopes.
- Keep client credentials and bearer tokens out of logs.

## Validation Rules

- Do not hard-code payer rules when Configurations can supply them.
- Use `pattern`, `required`, `allowed`, `requiredWhen`, `allowedWhen`, and `requiredFieldCombinations` from Configurations as the validation source of truth.
- Validate provider identifiers, member identifiers, dates, and service codes before sending requests.
- Prefer mapping Availity validation messages into internal error objects with stable application-level codes.

## Async Polling Rules

- Many Availity APIs are asynchronous and return `202 Accepted` with a `Location` header.
- Poll the returned URI until the resource reaches a terminal status and `200 OK` returns the completed payload.
- Read resource `status`, `statusCode`, `etaDate`, and `expirationDate` when available.
- Apply bounded retries with backoff for transient failures and downstream payer delays.
- Treat communication failures separately from validation failures.

## API-Specific Guidance

### Payer List

- Use it to determine whether the payer, transaction type, and submission mode are actually available under the current contract.
- Check enrollment requirements and route metadata before building workflows that assume API support.

### Configurations

- Call this first for payer-sensitive flows.
- Use it to drive request construction, UI requirements, field validation, and conditional rules.
- Service Reviews and Enhanced Claim Status are especially dependent on `type` and `subtypeId` selection.

### Coverages (270/271)

- Use for eligibility and benefit inquiries.
- `POST /v1/coverages` is the create path; the legacy create GET is deprecated.
- Map partial or invalid-but-complete responses carefully because terminal payloads can contain incomplete benefit detail.

### Service Reviews (278)

- Use for authorization and referral create, inquiry, update, and void workflows.
- Respect `updatable`, `deletable`, and `updatableFields[]` before mutating an existing review.
- A certified response can produce identifiers needed for downstream claim submission.

### Claim Statuses and Enhanced Claim Status

- Use Claim Statuses for standard 276/277 inquiry flows.
- Use Enhanced Claim Status when the workflow needs summary history, detailed claim lookup, or value-add 277 data.
- Pull matching Configurations metadata for the chosen version before building the request.

### Patient Cost Estimator

- Use for predetermination-style professional or institutional estimate workflows.
- Confirm subtype and payer support before building assumptions into pre-service estimate flows.

### E&B Value-Add APIs

- Care Reminders and Member ID Card are additive capabilities, not replacements for HIPAA transactions.
- Confirm environment and availability because beta or product-gated features may differ by subscription.

### Claim Attachments

- Use for medical documentation and attachment workflows that support claims or reviews.
- Validate document metadata and lifecycle expectations before upload or resubmission logic.

## Error Handling Rules

- Handle API-level failures from HTTP status plus the structured Availity error body.
- Distinguish request validation problems from transient platform failures and downstream payer communication failures.
- Preserve developer detail for diagnostics, but surface user-safe messages to application consumers.
- Never log raw SSNs, full member IDs, tokens, or attachment contents.

## .NET Implementation Notes

- Centralize token acquisition in a typed client or provider with short-lived caching.
- Model async polling as reusable infrastructure so Coverages, Service Reviews, Claim Statuses, and attachments do not each invent their own loop.
- Use resilient HTTP policies for `429`, `503`, gateway failures, and payer timeout scenarios.
- If Availity response data will be rendered in a UI, use the `X-Response-Encoding-Context` header that matches the rendering context.

## References

- Detailed API reference: [Availity API Reference](./references/availity-api-reference.md)
- Exhaustive API reference: [Availity API Master Reference](./references/availity-api-master-reference.md)
- Use the bundled reference for endpoint parameters, status codes, demo scenarios, and .NET implementation details.
