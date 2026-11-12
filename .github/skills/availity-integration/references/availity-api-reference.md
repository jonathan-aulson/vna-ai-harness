# Availity API Reference

> Skill-focused handbook derived from the bundled master reference in this folder. This version keeps the decision-critical rules, endpoint inventory, request quirks, and implementation guidance needed during coding. It intentionally omits exhaustive field catalogs and payer matrices that are better sourced from Configurations or the live Availity docs.

## How to Use This Reference

1. Identify the business need in the API chooser below.
2. Check whether the payer and transaction are supported.
3. Pull Configurations for payer-specific validation before building the request.
4. Submit the transaction and handle `202 Accepted` as async work.
5. Map Availity and payer errors into internal contracts without exposing PHI.

## Platform Basics

- Base host: `https://api.availity.com`
- Auth model: OAuth2 client credentials only
- Token lifetime: 300 seconds
- Data formats: JSON and XML are common; some value-adds also return PDF, PNG, CSV, or XLS
- Common collection fields: `offset`, `limit`, `count`, `totalCount`, `links`, `resources`
- Standard pagination: `offset` default `0`, `limit` range `1-50`
- Common async behavior: valid requests often return `202` and a `Location` header to poll

## API Chooser

| Need | Primary API | Why it exists |
| --- | --- | --- |
| Determine whether a payer/transaction is supported | Payer List | Contract- and route-aware support lookup |
| Determine payer-required fields and validation rules | Configurations | Runtime source of truth for required fields, regex, conditional rules |
| Eligibility and benefits | Coverages | 270/271 workflow |
| Prior auths and referrals | Service Reviews | 278 workflow |
| Standard claim status | Claim Statuses | 276/277 workflow |
| Richer claim history or value-add status data | Enhanced Claim Status | Extended claim search and enrichment |
| Predetermination / patient estimate | Patient Cost Estimator | Pre-service estimate workflows |
| Eligibility value-add data | Care Reminders, Member ID Card | Additional member-facing artifacts during E&B flows |
| Medical documentation / claim attachments | Claim Attachments | Attachment submission and status tracking |

## Authentication

**Token endpoint**

```http
POST /v1/token
Content-Type: application/x-www-form-urlencoded
```

**Required form fields**

- `grant_type=client_credentials`
- `client_id`
- `client_secret`
- `scope`

**Operational rules**

- Tokens expire in 5 minutes, so short-lived caching is required.
- Refresh early instead of waiting for `401` responses.
- Multiple scopes are space-separated in the `scope` field.
- Do not log client credentials or bearer tokens.

## Cross-Cutting Rules

### Response encoding and input safety

- Availity rejects unsafe inbound characters with `400` errors.
- Response encoding is opt-in via `X-Response-Encoding-Context`.
- If API content is rendered into a UI, choose the header value that matches the sink: `HTML`, `HTML_ATTRIBUTE`, `CSS`, `URL`, `JAVA_SCRIPT`, `XML`, or `XML_ATTRIBUTE`.

### HTTP status handling

| Code | Meaning | Normal action |
| --- | --- | --- |
| `200` | Completed successfully | Map payload |
| `201` | Created | Capture resource identity |
| `202` | Accepted, still processing | Poll `Location` or resource ID |
| `204` | Deleted / no content | Treat as success |
| `400` | Bad request or validation issue | Fix request, do not blindly retry |
| `401` | Auth issue | Refresh token or credentials |
| `403` | Forbidden | Check product access or payer permissions |
| `404` | Resource not found | Validate IDs and endpoint path |
| `409` / `422` | State or validation conflict | Fix request semantics |
| `429` | Rate limit | Retry with backoff |
| `500` / `502` / `503` / `504` | Platform or downstream failure | Retry if operation is safe |

**Common error body**

```json
{
  "userMessage": "The requested resource does not exist.",
  "developerMessage": "The requested resource does not exist. Please check that you're using the proper resource ID.",
  "reasonCode": 0,
  "statusCode": 404
}
```

### Async polling

1. Submit the request.
2. If the response is `202`, capture `Location` or returned `id`.
3. Poll until the resource reaches a terminal status.
4. Stop when the API returns a completed payload, an explicit terminal status, or a timeout policy is exhausted.

Treat payer validation failures separately from transient communication failures.

### Demo and mock behavior

- Demo access uses the same auth pattern.
- Some APIs support `X-Api-Mock-Scenario-ID` to return canned responses.
- Mock responses do not necessarily vary with request content.

## Payer List

**Use when**

- You do not know whether a payer/transaction pair is available.
- You need submission modes, enrollment requirements, or route metadata.

**Endpoint**

```http
GET /availity-payer-list
```

**Key filters**

- `payerId`
- `transactionType`
- `submissionMode`
- `availability`
- `enrollmentRequired`

**What to retain**

- `processingRoutes[]`
- `availability`
- `enrollmentRequired`
- `submissionMode`

Do not assume API support until the payer route confirms it.

## Configurations

**Use when**

- You are building any payer-sensitive request.
- You need required fields, regex patterns, conditional requirements, or allowed values.

**Endpoint**

```http
GET /v1/configurations
```

**Core query fields**

- `type` required
- `subtypeId` when the transaction distinguishes sub-flows
- `payerId` for payer-specific rules

**Important type mappings**

| API | `type` | `subtypeId` |
| --- | --- | --- |
| Coverages | `270` | none |
| Service Reviews request | `service-reviews` | `HS`, `AR`, `SC` |
| Service Reviews inquiry | `service-reviews-inquiry` | `HS`, `AR`, `SC` |
| Claim Statuses | `claim-statuses-inquiry` | none |
| PCE Professional | `professional-claims` | `PRE_DETERMINATION` |
| PCE Institutional | `institutional-claims` | `PRE_DETERMINATION` |
| Enhanced Claim Status | `enhanced-claim-status` | `HIPAA_276`, `VALUE_ADDS_277`, `SUMMARY`, `DETAIL` |

**Most useful response members**

- `elements`
- `requiredFieldCombinations`
- `settings`
- `pattern`
- `requiredWhen`, `allowedWhen`, `notAllowedWhen`

Treat Configurations as the runtime validation authority instead of freezing payer rules in code.

## Coverages (270/271)

**Use when**

- You need eligibility, benefit snapshot data, or service-type-specific benefit detail.

**Endpoints**

```http
POST /v1/coverages
GET /v1/coverages/{id}
DELETE /v1/coverages/{id}
```

**Request quirks**

- The create path is `POST /v1/coverages`.
- The older create-via-GET path is deprecated.
- Request content is form-encoded.
- Payer-specific required fields must come from Configurations.

**Frequently important request fields**

- `payerId`
- provider identifiers, especially `providerNpi`
- `memberId`
- patient name and birth date
- `serviceType`
- `asOfDate`
- `subscriberRelationship`

**Important terminal status codes**

| Code | Meaning |
| --- | --- |
| `4` | Complete |
| `3` | Complete but partially invalid |
| `19` | Request error |
| `R1` | Communication error, retrying |
| `7`, `13`, `14`, `15` | Communication failure states |

Expect partial payload quality even on terminal success. Benefit detail can be incomplete.

## Service Reviews (278)

**Use when**

- You need prior authorization, referral, update, inquiry, or void flows.

**Endpoints**

```http
POST /v2/service-reviews
GET /v2/service-reviews
GET /v2/service-reviews/{id}
PUT /v2/service-reviews
DELETE /v2/service-reviews/{id}
```

**Subtype mapping**

- `HS`: outpatient authorization
- `AR`: inpatient authorization or admission review
- `SC`: referral

**Rules that matter**

- Request and inquiry each have separate Configurations types.
- Search is async and commonly required before update or delete.
- `updatable`, `deletable`, and `updatableFields[]` govern allowed mutations.
- `certificationNumber` may be required later in downstream claim workflows.

**Important status codes**

| Code | Meaning |
| --- | --- |
| `A1` | Certified in total |
| `A2` | Certified partial |
| `A3` | Not certified |
| `A4` | Pending |
| `A6` | Modified |
| `NA` | No action required |
| `CT` | Contact payer |
| `51` | Complete |
| `71` | Term expired |

On denial or partial certification, inspect `statusReasons[]` and procedure-level statuses.

## Claim Statuses (276/277)

**Use when**

- You need the standard claim status inquiry workflow and do not need enhanced claim-history features.

**Endpoints**

```http
POST /v1/claim-statuses
GET /v1/claim-statuses/{id}
DELETE /v1/claim-statuses/{id}
```

**Critical request quirk**

- Create uses `POST` with `Content-Type: application/x-www-form-urlencoded` and `X-HTTP-Method-Override: GET`.

**Typical inputs**

- payer ID
- submitter identifiers
- provider identifiers such as NPI and tax ID
- subscriber and patient identifiers
- date range
- claim number and amount

**Normal flow**

- `202` while the payer response is pending
- `200` when summary claim statuses are ready
- follow with `GET /{id}` for full detail

**Common status pattern**

- `0` is a common in-progress state
- `4` indicates complete

## Patient Cost Estimator

**Use when**

- You need a predetermination or patient estimate before service is rendered.

**Endpoints**

```http
POST /v1/professional-claims
GET /v1/professional-claims/{id}

POST /v1/institutional-claims
GET /v1/institutional-claims/{id}

POST /v2/patient-cost-estimates/prof
GET /v2/patient-cost-estimates/prof/{id}
```

**Variant notes**

- v1 professional and institutional use Configurations with `PRE_DETERMINATION`.
- v2 professional uses a newer response shape and carries `coverageId` and `payerId` at the top level.
- All variants are async.

**Typical terminal codes**

- `200` processed
- `202` pending or retrying
- `400` validation failure
- `404` unknown ID
- `500` or `504` platform/downstream failure
- v2 may return `422`

Keep the integration focused on the claim shell and service lines the payer actually requires. Avoid reproducing full 837-style field catalogs unless needed.

## Enhanced Claim Status

**Use when**

- You need more than standard 276/277 claim status.
- You need enriched value-add data, summary search, or claim detail search.

**Workflows**

| Workflow | Purpose |
| --- | --- |
| `searchBy276` -> `valueAdds277` | Standard 276-style search followed by enriched 277 response |
| `summarySearch` -> `detailSearch` | Claim-history style summary then single-claim detail |

**Subtype mapping**

- `HIPAA_276`
- `VALUE_ADDS_277`
- `SUMMARY`
- `DETAIL`

**Behavior to preserve**

- `valueAdds277` depends on a prior successful `searchBy276`.
- `summarySearch` and `detailSearch` can also operate independently depending on the use case.
- `completeCode: 4` is the key completion signal.
- Response objects expose richer claim flags such as attachment, correction, appeal, and messaging eligibility.

**Common HTTP outcomes**

- `200`, `400`, `403`, `404`, `410`, `500`, `502`, `503`, `504`

## Service Review Add-Ons

These are adjacent to the 278 workflow and reuse Service Reviews shapes.

### IsAuthRequired

- Endpoint pair: `POST /value-adds/v1/isauthrequired`, `GET /value-adds/v1/isauthrequired/{id}`
- Status: unavailable to new customers
- Purpose: evaluate whether auth is required before full auth submission

### Auth Attachments

- Endpoint pair: `POST /value-adds/v2/attachments`, `GET /value-adds/v2/attachments/{id}`
- Status: closed pilot / unavailable to new customers
- Purpose: send attachments with a service review
- Key caveat: payer-specific MIME type and file-size rules vary substantially

## E&B Value-Add APIs

### Care Reminders

**Endpoint**

```http
POST /pre-claim/eb-value-adds/care-reminders
```

**Use when**

- The eligibility flow needs care-gap or reminder data.

**Common required inputs**

- `payerId`
- `memberId`

**Response pattern**

- Returns a renderable table model: title, headers, row order, and row data.
- The client must render the table; Availity does not provide final UI formatting.

### Member ID Card

**Endpoints**

```http
POST /pre-claim/eb-value-adds/member-card
GET /pre-claim/eb-value-adds/member-card/{GTID}
```

**Use when**

- The eligibility flow needs a PDF or PNG member card.

**Response pattern**

- POST returns a reference with document URIs.
- GET retrieves the actual card asset.
- Required fields vary materially by payer, so validate against payer guidance before assuming a generic request shape.

## Claim Attachments

**Use when**

- You need to submit medical claim attachments or respond to a payer request for additional information.

**Endpoints**

```http
POST /medical-attachments/ma/external/rest/response
GET /medical-attachments/ma/external/rest/status/{requestId}
```

**Workflow modes**

- Unsolicited: send documentation with or near claim submission
- Solicited: respond to `277RFAI` and match the payer's request number

**Top-level request objects**

- `submitter`
- `subscriber`
- `patient`
- `provider`
- `payer`
- `encounter`
- `payloads[]`
- `product`

**Attachment payload essentials**

- `reasonCodes[]` with `LOINC`
- `contents[]` with filename, content type, and base64 data

**Success and failure signals**

- Success returns an attachment request `id`
- Status polling returns states such as `SUBMITTED`
- Validation issues come back as `parameterViolations[]`

Never send test payloads to production.

## .NET Integration Guidance

### Token provider pattern

- Centralize token retrieval in one provider or delegating handler.
- Cache for slightly less than 300 seconds.
- Prefer `IHttpClientFactory` over ad hoc `HttpClient` creation.

### Polling pattern

- Build one reusable polling abstraction for `202` workflows.
- Support bounded retries, jittered backoff, and terminal-status inspection.
- Respect `429` and gateway timeout scenarios.

### Validation pattern

- Call Configurations before building payer-sensitive forms or DTOs.
- Use `pattern`, `required`, `maxLength`, and `requiredFieldCombinations` directly in validation logic.
- Keep payer rules data-driven instead of baking them into handlers.

### Serialization pattern

- Expect sparse nested payloads.
- `System.Text.Json` works well if null handling and string-backed numeric fields are modeled carefully.
- Some amounts and flags that look numeric in docs are string-valued in payloads.

### Resilience pattern

- Retry `429`, `408`, and transient `5xx` conditions with jittered exponential backoff.
- Treat most `4xx` responses other than `429` as non-retryable.
- Surface structured Availity validation messages without leaking PHI.

## Endpoint Index

| API | Endpoint set |
| --- | --- |
| OAuth2 Token | `POST /v1/token` |
| Payer List | `GET /availity-payer-list` |
| Configurations | `GET /v1/configurations` |
| Coverages | `POST /v1/coverages`, `GET /v1/coverages/{id}`, `DELETE /v1/coverages/{id}` |
| Service Reviews | `POST /v2/service-reviews`, `GET /v2/service-reviews`, `GET /v2/service-reviews/{id}`, `PUT /v2/service-reviews`, `DELETE /v2/service-reviews/{id}` |
| Claim Statuses | `POST /v1/claim-statuses`, `GET /v1/claim-statuses/{id}`, `DELETE /v1/claim-statuses/{id}` |
| PCE Professional v1 | `POST /v1/professional-claims`, `GET /v1/professional-claims/{id}` |
| PCE Institutional v1 | `POST /v1/institutional-claims`, `GET /v1/institutional-claims/{id}` |
| PCE Professional v2 | `POST /v2/patient-cost-estimates/prof`, `GET /v2/patient-cost-estimates/prof/{id}` |
| Enhanced Claim Status | `searchBy276`, `valueAdds277`, `summarySearch`, `detailSearch` endpoint pairs |
| IsAuthRequired | `POST /value-adds/v1/isauthrequired`, `GET /value-adds/v1/isauthrequired/{id}` |
| Auth Attachments | `POST /value-adds/v2/attachments`, `GET /value-adds/v2/attachments/{id}` |
| Care Reminders | `POST /pre-claim/eb-value-adds/care-reminders` |
| Member ID Card | `POST /pre-claim/eb-value-adds/member-card`, `GET /pre-claim/eb-value-adds/member-card/{GTID}` |
| Claim Attachments | `POST /medical-attachments/ma/external/rest/response`, `GET /medical-attachments/ma/external/rest/status/{requestId}` |

## What This Reference Intentionally Leaves Out

- Exhaustive payer-specific matrices for beta APIs
- Full field-by-field claim and service-line catalogs for PCE variants
- Full nested response member inventories for every resource
- X12 code-list details that should come from TR3 guides or live Configurations responses

Use `./availity-api-master-reference.md` and live Availity documentation when you need exhaustive field inventories or payer-by-payer beta requirements.
