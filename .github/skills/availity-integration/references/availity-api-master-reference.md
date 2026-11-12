# Availity REST API — Master Reference

> Compiled from the Availity for Developers documentation (developer.availity.com), pages last updated 2025-10-31.
> This is the canonical exhaustive source document in this repo for the Availity API surface: API Guide, HIPAA Transaction APIs, Enhanced Claim Status, Service Reviews Add-On APIs, E&B Value-Add APIs, and the Claim Attachments API.
>
> For the shorter skill-facing handbook used by the Availity customization, see `.github/skills/availity-integration/references/availity-api-reference.md`.

## Table of Contents

1. [Platform Overview](#1-platform-overview)
2. [Authentication (OAuth2 Client Credentials)](#2-authentication-oauth2-client-credentials)
3. [Cross-Cutting Conventions](#3-cross-cutting-conventions)
   - [Security & XSS / Response Encoding](#31-security--xss--response-encoding)
   - [Collections, Pagination & Link Relations](#32-collections-pagination--link-relations)
   - [HTTP Status & Error Codes](#33-http-status--error-codes)
   - [The Async Polling Pattern](#34-the-async-polling-pattern)
   - [Demo / Mock Environment](#35-demo--mock-environment)
4. [Supported EDI / HIPAA Transactions](#4-supported-edi--hipaa-transactions)
5. [HIPAA Transaction APIs](#5-hipaa-transaction-apis)
   - [Availity Payer List 1.0.4](#51-availity-payer-list-104)
   - [Configurations 1.0.0](#52-configurations-100)
   - [Coverages 1.0.0 (270/271)](#53-coverages-100-270271)
   - [Service Reviews 2.0.0 (278)](#54-service-reviews-200-278)
   - [Claim Statuses 1.0.0 (276/277)](#55-claim-statuses-100-276277)
   - [Patient Cost Estimator APIs (Predeterminations)](#56-patient-cost-estimator-apis-predeterminations)
6. [Enhanced Claim Status (claim-status 1.0.0)](#6-enhanced-claim-status-claim-status-100)
7. [Service Reviews Add-On APIs](#7-service-reviews-add-on-apis)
   - [IsAuthRequired 2.0.0](#71-isauthrequired-200-currently-unavailable)
   - [Attachments 2.0.0](#72-attachments-200-currently-unavailable)
8. [E&B Value-Add APIs](#8-eb-value-add-apis)
   - [Care Reminders (Beta)](#81-care-reminders-beta)
   - [Member ID Card (Beta)](#82-member-id-card-beta)
9. [Claim Attachments 1.0.0](#9-claim-attachments-100)
10. [Implementation Notes for .NET](#10-implementation-notes-for-net)
11. [Quick Reference: Endpoint Index](#11-quick-reference-endpoint-index)
12. [Coverage & Notes](#12-coverage--notes)

---

## 1. Platform Overview

Availity operates the Availity Health Information Network, a HIPAA-operationally-compliant clearinghouse that processes ANSI ASC X12N standard transactions. Its real-time REST APIs give an application connectivity to health plans nationwide, reaching over a million providers and millions of daily healthcare transactions.

**Base host:** `https://api.availity.com`
(Some product-specific endpoints use longer path prefixes, and several beta / claim-status samples in the source docs use a `qua.api.availity.com` or `qa-apps.availity.com` QA host. Always confirm the correct host for your environment.)

**Representation formats:** All APIs return JSON and XML (including errors). Some additionally return CSV, PDF, PNG, or XLS.

**Design principles:**
- Predictable, resource-oriented URLs.
- HTTP status codes signal success/failure.
- OAuth2 over HTTPS (with SHA256/MD5 also referenced) for security.
- Standardized responses for HIPAA inquiries (eligibility, claim status, predeterminations, etc.).

**Date/time format:** ISO 8601, e.g. `"createdDate": "2014-06-16T18:38:12.000+0000"`.

**Product families:**
- **Healthcare HIPAA Transactions** — Payer List, Configurations, Coverages, Service Reviews, Claim Statuses, Patient Cost Estimator (Professional & Institutional v1, Professional v2), plus the OAuth2 token provider.
- **Add-On APIs** — Enhanced Claim Status, Service Reviews Add-Ons (IsAuthRequired, Auth Attachments), E&B Value-Add (Care Reminders, Member ID Card).
- **Claim Attachments** — Medical attachment submission (275 / 277RFAI workflows).

**Getting access:** Create an API account, register an application, and subscribe to an API product. A free Demo trial package (no PHI) is available without a formal application. Full production access (Standard Plan) is arranged through Availity's Trading Partner Management / Contact Sales process.

---

## 2. Authentication (OAuth2 Client Credentials)

All Availity REST APIs use **OAuth 2.0 Client Credentials Grant** over HTTPS — application-only auth (no end-user context). This applies even to demo resources.

**Token endpoint:** `POST https://api.availity.com/v1/token`

**Token lifetime:** The access token is valid for **300 seconds (5 minutes)** only — request a fresh token frequently / on demand.

### Token request

```bash
curl -i -X POST "https://api.availity.com/v1/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&scope=$SCOPE_PERMISSION&client_id=$API_KEY&client_secret=$CLIENT_SECRET"
```

Body parameters are **case-sensitive** and must match exactly:
- `grant_type=client_credentials`
- `client_id=<your_api_key>`
- `client_secret=<your_client_secret>`
- `scope=<scope_permission>` — when two scopes are needed, separate them with a single space (e.g. `healthcare-hipaa-transactions healthcare-hipaa-transactions-demo`).

### Token response

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6...",
  "consented_on": 1761647526,
  "expires_in": 300,
  "scope": "$SCOPE_PERMISSION",
  "token_type": "Bearer"
}
```

### Calling an API with the token

```bash
curl -i -X GET "https://api.availity.com/v1/availity-payer-list" \
  -H "Authorization: Bearer $ACCESS_TOKEN"
```

### Standard workflow
1. POST credentials + scope to `/v1/token`.
2. Receive `access_token`, `expires_in`, `token_type`.
3. Call the subscribed API with `Authorization: Bearer <access_token>`.
4. Handle the response (and for async resources, poll — see §3.4).

---

## 3. Cross-Cutting Conventions

### 3.1 Security & XSS / Response Encoding

Availity defends against cross-site scripting via (1) whitelist input validation/sanitization and (2) context-specific output encoding.

- **Input validation:** Requests containing characters Availity deems unsafe return `HTTP 400` with an *inbound injection* error. Sanitize input before sending.
- **Response encoding (opt-in):** Encoding is **not applied by default**. Opt in by sending the `X-Response-Encoding-Context` header with one of:
  - `HTML` — HTML entities
  - `HTML_ATTRIBUTE` — HTML attribute values
  - `CSS` — stylesheets / style tags
  - `URL` — URL context
  - `JAVA_SCRIPT` — JavaScript data values
  - `XML` — XML entities
  - `XML_ATTRIBUTE` — XML attribute values

Use the encoding context that matches where you will render the data, especially when displaying API responses directly in a UI.

### 3.2 Collections, Pagination & Link Relations

A **collection resource** wraps multiple resources and supports pagination, sorting, and search.

**Collection envelope fields:**

| Field | Type | Description | Min | Max | Default |
| --- | --- | --- | --- | --- | --- |
| `links` | Object | Set of resource URIs (self/first/last/next/prev). | — | — | — |
| `offset` | Integer | Zero-based starting index of first item returned. | 0 | — | 0 |
| `limit` | Integer | Max items per request. | 1 | 50 | 50 |
| `count` | Integer | Number of items returned. | 0 | — | — |
| `totalCount` | Integer | Total items matching the query. | 0 | — | — |
| `resources` | Array | Current page of resources (array name = plural resource type, e.g. `coverages`). | 0 | — | — |

**Pagination query params:** `offset` (default 0) and `limit` (1–50). Example: `?offset=10&limit=2`.

**Link relations** (returned in `links`; absent relations come back null). For a 102-record set with `offset=10&limit=2`:

| Relation | Meaning | Example href |
| --- | --- | --- |
| `self` | Current page | `.../availity-payer-list?offset=10&limit=2` |
| `first` | First page | `.../availity-payer-list?offset=0&limit=2` |
| `last` | Last page | `.../availity-payer-list?offset=100&limit=2` |
| `next` | Next page | `.../availity-payer-list?offset=12&limit=2` |
| `prev` | Previous page | `.../availity-payer-list?offset=8&limit=2` |

### 3.3 HTTP Status & Error Codes

`2XX` = success, `4XX` = bad request / auth problem, `5XX` = Availity or downstream error.

**Error response shape:**

```json
{
  "userMessage": "The requested resource does not exist.",
  "developerMessage": "The requested resource does not exist. Please check that you're using the proper resource ID.",
  "reasonCode": 0,
  "statusCode": 404
}
```

**General status codes:**

| Code | Status | Typical verbs | Meaning |
| --- | --- | --- | --- |
| 200 | OK | GET, PUT, PATCH, GET-by-ID | Retrieved/updated successfully (GET-by-ID returns 0+ resources; `count` reports how many). |
| 201 | Created | POST | New resource created. |
| 202 | Accepted | any | Accepted but not finished; `Location` header holds the URI to poll. |
| 204 | No Content | DELETE | Fulfilled; resource considered gone. |
| 303 | See Other | GET | Resource at a different URI (in `Location`); retrieve via GET. |
| 400 | Bad Request | any | Malformed query/syntax. |
| 401 | Unauthorized | any | Authentication required / not authorized. |
| 403 | Forbidden | any | Understood but refused. |
| 404 | Not Found | any | Nothing matches the request URI. |
| 405 | Method Not Allowed | any | Verb not allowed for resource. |
| 406 | Not Acceptable | any | Cannot satisfy Accept headers. |
| 409 | Conflict | mutating | Conflicts with current resource state. |
| 410 | Gone | any | Permanently unavailable. |
| 415 | Unsupported Media Type | any | Request entity format unsupported. |
| 422 | Unprocessable Entity | mutating | Well-formed but failed validation / wrong type/version. |
| 429 | Too Many Requests | any | Rate limit exceeded. |
| 500 | Internal Server Error | any | Unexpected Availity-side error. |

**Error attributes:**

| Attribute | Type | Definition |
| --- | --- | --- |
| `statusCode` | Integer | HTTP status code. |
| `reasonCode` | Integer | Internal error code (currently unused). |
| `userMessage` | String | End-user-appropriate message. |
| `developerMessage` | String | Developer-detail message. |
| `url` | String | Link to more info. |
| `errors` | Array | Extended error messages. |
| `errors.code` | Integer | Service-specific error code. |
| `errors.errorMessage` | String | Service-specific message. |

### 3.4 The Async Polling Pattern

Many Availity transactions reach a health plan asynchronously. The pattern:

1. **Submit** (POST/PUT/DELETE or a search GET). A valid request returns `202 Accepted` with a `Location` header containing a result URI (and usually a resource `id`).
2. **Poll** the returned URI / `GET .../{id}` until status changes from in-progress to a terminal state.
3. Terminal "complete" states return `200` with the full resource.

Resources expose `status`/`statusCode` (and often `etaDate`/`expirationDate`) so you know when to stop polling. Availity caches short-lived local copies of results until `expirationDate`.

### 3.5 Demo / Mock Environment

- Subscribe to the Demo environment (no PHI) to explore APIs at no cost.
- Select a canned scenario by sending header `X-Api-Mock-Scenario-ID: <scenario_id>`.
- Confirm a mock response via the response header `X-Api-Mock-Response: true`.
- Demo data is static — responses don't vary with request content.
- Not all APIs support demo scenarios; supported scenario IDs are listed per-API.

---

## 4. Supported EDI / HIPAA Transactions

| X12N Format | Version | Transaction type |
| --- | --- | --- |
| 837 | 005010X223A2 | Institutional claims |
| 837 | 005010X222A1 | Professional claims |
| 837 | 005010X224A2 | Dental claims |
| 270 / 271 | 005010X279A1 | Eligibility & benefits inquiry / response |
| 275 | 005010X210 | Additional info to support a claim (medical attachments) |
| 276 / 277 | 005010X212 | Claim status request / response |
| 278 | 005010X217 | Services request (auth & referral) for review/response |
| 278 | 005010X216 | Services review notification & acknowledgement |
| 835 | 005010X221A1 | Claim payment / remittance advice |

Format, content, and data-element rules live in the X12 TR3 implementation guides, available (for a fee) from Washington Publishing Company (WPC).

`transactionType` codes used by the Payer List API: `270`, `276`, `278I`, `835`, `837P`, `837PEncounter`, `837PPredetermination`, `837I`, `837IEncounter`, `837IPredetermination`, `837D`, `837DEncounter`, `837DPredetermination`, `277RFAI`, `275`, `278N`, `ClaimStatusSummary`, `ClaimStatusDetail`, `ClaimStatusValueAdd`.

---

## 5. HIPAA Transaction APIs

### 5.1 Availity Payer List 1.0.4

Returns the customized list of payers and transactions available under your Availity contract.

**Endpoint:** `GET /availity-payer-list`

**Query/header parameters (all optional):**

| Parameter | In | Notes |
| --- | --- | --- |
| `payerId` | query | Availity-specific payer identifier. |
| `transactionType` | query array | EDI transaction codes (see §4 list). |
| `submissionMode` | query array | `Portal`, `Batch`, `RealTime`, `API`. |
| `availability` | query | `available` (no contract needed) or `contractrequired`. |
| `enrollmentRequired` | query boolean | Filter on enrollment requirement. |
| `Accept` | header | e.g. `application/json`. |

**Sample request:**

```bash
curl --request GET \
  --url 'https://api.availity.com/availity/v1/availity-payer-list?payerId=591209257&transactionType=276&submissionMode=PORTAL&availability=AVAILABLE&enrollmentRequired=true' \
  --header 'Authorization: Bearer REPLACE_BEARER_TOKEN' \
  --header 'accept: application.json'
```

**Primary response fields:**

| Field | Type | Definition |
| --- | --- | --- |
| `name` | String | Common health-plan name. |
| `payerId` | String | Availity-specific payer ID. |
| `displayName` | String | Name shown in Availity Essentials. |
| `shortName` | String | Short name used in batch file naming. |
| `processingRoutes` | Object | Route info from Availity to payer (see below). |

**`processingRoutes` fields:**

| Field | Type | Definition |
| --- | --- | --- |
| `transactionDescription` | String | HIPAA transaction type description. |
| `submissionMode` | String | `Portal`, `Batch`, `RealTime`, `API`. |
| `effectiveDate` | String | Date the transaction became available. |
| `availability` | Boolean | Available under current contract vs requires extra contract. |
| `enrollmentRequired` | Boolean | Enrollment with Availity required. |
| `enrollmentMode` | String | e.g. Manual Payer Submission, Paperless, Payer Portal Enrollment, Paper, Email Attachment, Auto Complete. |
| `additionalInfo` | String | Extra transaction info. |
| `rebateTier` | String | Route's cost tier. |
| `passThroughRate` | String | Route's pass-through rate. |
| `newTierNotice` | String | Upcoming tier-change notice. |
| `gateway` | String | Whether Availity is the gateway for this route. |
| `recentlyAdded` | String | Date the route was added. |

### 5.2 Configurations 1.0.0

The Configurations API delivers per-payer field requirements, validation patterns (regex), required-field combinations, selection-group code/values, and more — letting you normalize differences across health plans instead of hand-coding each payer. Treat it as the source of truth for what each payer requires for a given transaction.

**Endpoint:** `GET /v1/configurations`

**Parameters:**

| Parameter | In | Notes |
| --- | --- | --- |
| `type` | query (**required**) | HIPAA transaction type (see type table below). |
| `subtypeId` | query | Subtype for the request. |
| `payerId` | query | Health plan's Availity ID. Omit to list all payers supporting the type. |
| `Accept` | header | `application/json` or `application/xml`. |

**`type` / `subtypeId` values by API:**

| API | `type` | `subtypeId` |
| --- | --- | --- |
| Coverages | `270` | — |
| Service Reviews (request) | `service-reviews` | `HS` (outpatient auth), `AR` (inpatient auth/admission), `SC` (referral) |
| Service Reviews (inquiry) | `service-reviews-inquiry` | `HS`, `AR`, `SC` |
| Claim Statuses | `claim-statuses-inquiry` | — |
| PCE – Professional (v1) | `professional-claims` | `PRE_DETERMINATION` |
| PCE – Institutional (v1) | `institutional-claims` | `PRE_DETERMINATION` |
| Enhanced Claim Status | `enhanced-claim-status` | `HIPAA_276`, `VALUE_ADDS_277`, `SUMMARY`, `DETAIL` |

**Sample request:**

```bash
curl -X GET "https://api.availity.com/availity/v1/configurations?payerId=BCBSF&type=270"
```

**Top-level response fields:** `totalCount`, `count`, `offset`, `limit`, `links`, `configurations[]`.

**`configurations[]` fields:** `type`, `categoryId`, `categoryValue`, `subtypeId`, `subtypeValue`, `payerId`, `payerName`, `version`, `sourceId`, `elements` (object), `requiredFieldCombinations` (array — each entry is a rule where all fields in at least one of its lists must be supplied), `settings` (key/value object).

`version` values (for Enhanced Claim Status): `HIPAA_276` (searchBy276), `VALUE_ADDS_277` (valueAdds277), `CLAIM_HISTORY`/`SERVICE_DATE`/`CHECK_NUMBER`/`MEMBER_ID` (summarySearch), `CLAIM_NUMBER` (detailSearch).

**`elements` object** — describes each form field. Key sub-fields:

| Field | Type | Definition |
| --- | --- | --- |
| `type` | String | UI data type: `Unsupported`, `Boolean`, `Collection`, `Date`, `Enumeration`, `Information`, `Section`, `Text`, `Number`, `Object`, `ObjectArray`. |
| `label` | String | Display label. |
| `order` | Integer | Suggested UI ordering index. |
| `helpTopicId` | String | Help topic reference. |
| `elements` | Object | Child elements. |
| `errorMessage` | String | Validation error message for UI use. |
| `maxRepeats` / `minRepeats` | Integer | Repetition bounds. |
| `allowed` | Boolean | Field is valid to use. |
| `required` | Boolean | Field is required. |
| `information` / `groups` / `values` | Array/String | Supporting lists. |
| `repeats` / `hidden` | Boolean | Repetition / visibility flags. |
| `defaultValue` | String | Pre-populate value. |
| `valuesWhen` | Object | Conditional values. |
| `min` / `max` | String (date) | Date bounds. |
| `pattern` | String | Regex for validation. |
| `maxLength` / `minLength` | Integer | Length bounds. |
| `maxLengthWhen` / `patternWhen` | Object | Conditional length/pattern. |
| `mode` | String | Drop-down vs radio group. |
| `allowedWhen` / `notAllowedWhen` / `requiredWhen` / `notRequiredWhen` | Object | Conditional rules (see conditional-fields table). |
| `objectTypes` | Object | ObjectArray item prototypes. |

**Conditional-field operators** (`*When` objects): `equalTo`, `containedIn`, `greaterThan`, `lessThan`, `greaterEqual`, `lessEqual`, `maxLength`, `pattern`, `values`.

**`elements.objectTypes` fields:** `label`, `minInstances`, `maxInstances`, `required`, `allowedWhen`, `notAllowedWhen`, `requiredWhen`, `notRequiredWhen`, `fieldValues`.

> **Pattern note:** A typical 270 element looks like
> `"providerNpi": { "type": "Text", "label": "Provider NPI", "allowed": true, "required": false, "pattern": "^[1-4][0-9]{9}$", "maxLength": 10, "errorMessage": "Enter a valid NPI..." }`.
> Use `pattern` directly for client-side validation.

### 5.3 Coverages 1.0.0 (270/271)

Eligibility & benefits — find a snapshot/details of a member's coverage. Enables X12 270/271. Code lists come from the 270/271 TR3.

**Endpoints:**

| # | Path | Function |
| --- | --- | --- |
| 1 | `POST /v1/coverages` | Retrieve a coverage snapshot (async) or search recent coverage requests. |
| 2 | `GET /v1/coverages/{id}` | Retrieve a specific coverage by response ID. |
| 3 | `DELETE /v1/coverages/{id}` | Delete a specific coverage by response ID. |

> **Important:** The old `GET /v1/coverages` (create) endpoint is **deprecated** — use `POST /v1/coverages` to create requests.

**Demo scenarios** (`X-Api-Mock-Scenario-ID`):

| Scenario ID | Code | Meaning |
| --- | --- | --- |
| `Coverages-Complete-i` | 200 | Coverage retrieved. |
| `Coverages-PayerError1-i` | 200 | Provider ineligible for inquiries. |
| `Coverages-PayerError2-i` | 200 | Subscriber name invalid. |
| `Coverages-InProgress-i` | 202 | Retrieval in progress. |
| `Coverages-Retrying-i` | 202 | Payer didn't respond; retrying. |
| `Coverages-RequestError1-i` | 400 | Failed input validation. |
| `Coverages-RequestError2-i` | 400 | Failed input validation. |

**POST request** — `Content-Type: x-www-form-urlencoded`; `Accept: application/json|xml`. Key body fields (all optional unless a payer requires them; check Configurations):

Provider: `payerId`, `providerLastName`, `providerFirstName`, `providerType`, `providerNpi` (most payers require), `providerTaxId`, `payerAssignedProviderId`, `providerSSN`, `providerPIN`, `providerCity`, `providerState`, `providerZipCode`, `providerSpecialty` (taxonomy), `placeOfService`, `submitterId`.

Service/date: `asOfDate`, `toDate`, `serviceType`, `cardIssueDate`, `procedureCode`.

Patient/subscriber: `memberId`, `medicaidId`, `patientSSN`, `patientLastName`, `patientFirstName`, `patientMiddleName`, `patientSuffix`, `patientGender`, `patientBirthDate`, `patientState`, `groupNumber`, `subscriberRelationship` (`18` Self, `01` Spouse, `19` Child, `G8` Other).

**Search params (with the above):** `status` (`In Progress`/`Request Error`/`Communication Error`/`Complete`), `planStatus` (`Active`/`Inactive`), `q` (**required** for search; may be empty `q=`), `sortBy` (`lastUpdateDate`/`asOfdate`/`patientLastName`, default `lastUpdateDate`), `sortDirection` (`asc`/`desc`, default `desc`).

**Coverage status codes:**

| Code | Status | Meaning |
| --- | --- | --- |
| 0 | In Progress | Availity retrieving from payer. |
| 19 | Request Error | Payer returned `validationMessages`. |
| R1 | Communication Error, Retrying | No payer response; retrying. |
| 7 | Communication Error | No payer response. |
| 13 | Communication Error | Invalid payer response. |
| 14 | Communication Error | No payer response. |
| 15 | Communication Error | Payer down for maintenance. |
| 4 | Complete | Refresh successful. |
| 3 | Complete (Invalid Response) | Successful but partially invalid; some values may be missing. |

**Sample POST:**

```bash
curl --request POST \
  --url https://api.availity.com/availity/v1/coverages/ \
  --header 'Authorization: Bearer ' \
  --header 'Content-Type: application/x-www-form-urlencoded' \
  --data payerId=123 \
  --data providerNpi=123 \
  --data 'providerLastName=ABC' \
  --data asOfDate=1990-01-01 \
  --data 'serviceType[]=30' \
  --data memberId=ABC123 \
  --data patientBirthDate=1900-01-01 \
  --data patientLastName=LAST \
  --data patientFirstName=FIRST \
  --data patientGender=M \
  --data patientState=FL \
  --data subscriberRelationship=18
```

**Response structure (high level):** A coverage carries `id`, `customerId`, `controlNumber`, `submitterStateCode`, `status`/`statusCode`, dates (`createdDate`, `updatedDate`, `expirationDate`, `asOfDate`, `toDate`, `cardIssueDate`), and these objects/arrays:

- `payer` — `payerId`, `name`, `responsePayerId`, `responseName`, plus (on full coverage) type, plan/group/policy numbers, COB dates, contact info, `payerNotes`.
- `requestingProvider` — provider identifiers (NPI, taxId, SSN, EIN, ETIN, PIN, submitterId, payerAssignedProviderId), `category`/`type`/`role` + codes, address, `contactInformation`, `deliveryInformation`, `authorizationRequired`.
- `patient` / `subscriber` — names, `birthDate`, `gender`/`genderCode` (F/M/U), `memberId`, `medicaidId`, `ssn`, `subscriberRelationship`(+Code), `address`, `caseNumber`, `updateYourRecords`.
- `plans[]` — `status`/`statusCode`, group/plan/policy/network identifiers, many lifecycle dates (eligibility, coverage, COBRA, plan start/end), `additionalPayers`, `primaryCareProvider`, `contacts`, `benefits[]`, `limitations`, `costContainment`, `preexistingConditions`, `coverageBasis`, `insuranceType`(+Code), `payerNotes`.
- `plans[].benefits[]` — `name`, `type`, `source`, `status`(+Code), `amounts`, `limitations`, `benefitDescriptions`, `nonCovered`, `exclusions`, `coverageBasis`, `additionalPayers`, `contacts`.
- `benefitDetail` — split by network: `inNetwork[]`, `outOfNetwork[]`, `notApplicableNetwork[]`, `noNetwork[]` (each a `networkBenefit`).
- `benefitDetail.amounts` — `coPayment`, `outOfPocket`, `deductibles`, `coInsurance`.
- `networkBenefit` — `status`/`statusCode`, `insuranceType`(+Code), `amount`/`units`, `remaining`/`total` (+time periods), `level`, `quantity`(+qualifier), `authorizationRequired`(+Unknown), `placeOfService`(+Code), plan/policy/network IDs, lifecycle dates, `contacts`, `payerNotes`, `deliveryInformation`.
- `requestedServiceType[]` — `{ code, value }` (e.g. `30` = Health Benefit Plan Coverage).
- `procedureCode[]`, `validationMessages[]` (`field`, `code`, `errorMessage`, `index`).
- `supplementalInformation` — booleans/flags like `professionalPatientCostEstimator`, `institutionalPatientCostEstimator`, `patientCareSummary`, `assessmentCarePlan`, `outOfArea`, `pregnant`, `csnpIndicator`, `pharmacyRestrictions`, `erReferralCompleted`, plus third-party clinical-exchange keys (`localMemberId`, `pceMemberLocatorKey`, `pceHostIndicator`).
- `reminders` — `titles`, `messages[]`, `inference` (links to the Care Reminders value-add).
- `payerNotes[]` — `type`(+Code), `message`.

**Delete** returns `204 No Content`.

### 5.4 Service Reviews 2.0.0 (278)

Create, update, void, and search authorizations and referrals (Admission Reviews `AR`, Health Service Reviews `HS`, Specialty Care Reviews `SC`). Enables X12 278. Code lists from the 278 TR3.

**Endpoints:**

| # | Path | Function |
| --- | --- | --- |
| 1 | `POST /v2/service-reviews` | Create/submit auth or referral (async → `Location` header). |
| 2 | `GET /v2/service-reviews` | Search the payer's system (async; auth/referral **inquiry**; prerequisite to update/delete). |
| 3 | `GET /v2/service-reviews/{id}` | Retrieve a specific service review. |
| 4 | `PUT /v2/service-reviews` | Update (only when `updatable=true`; `updatableFields[]` lists allowed fields). |
| 5 | `DELETE /v2/service-reviews/{id}` | Void (only when `deletable=true`). |

**Demo scenarios** (selected): `SR-CreateRequestAccepted-i` (POST 202), `SR-CreateRequestError-i` (POST 400), `SR-GetComplete-i` (GET id `12345678` → 200), `SR-GetInProgress-i` (202), `SR-GetPayerDown-i` (504), `SR-GetRetrying-i` (202), `SR-UpdateRequestAccepted-i` (PUT 202), `SR-DeleteRequestAccepted-i` (DELETE 202), plus `SRI-*` variants for the inquiry path. For POST demos, send empty body `{}`.

**Validation rules:** Driven by Configurations — `type=service-reviews` (request) or `service-reviews-inquiry` (inquiry), each with `subtypeId` `HS`/`AR`/`SC`. Example:
`GET https://api.availity.com/availity/v1/configurations?type=service-reviews&subtypeId=HS&payerId=BCBSF`

**`serviceReview` request body — major fields:**

- **Identity/state:** `id`, `customerId`, `controlNumber`, `userId`, `shortFormIndicator`, `updatable`, `deletable`, `updatableFields[]`, `status`, `statusCode`, `statusReasons[]`, `createdDate`, `updatedDate`, `expirationDate`, `validationMessages[]`, `providerNotes[]`, `payerNotes[]`.
- **Parties:** `payer`, `requestingProvider`, `subscriber`, `patient`, `renderingProviders[]`.
- **Clinical:** `diagnoses[]`, `procedures[]`, `serviceType`(+Code), `additionalServiceTypes[]`, `placeOfService`(+Code), `serviceLevel`(+Code).
- **Certification/reference:** `certificationIssueDate`, `certificationEffectiveDate`, `certificationExpirationDate`, `certificationNumber`, `referenceNumber`, `traceNumbers[]`.
- **Request type:** `requestType`(+Code = `HS`/`AR`/`SC`).
- **Dates/qty:** `fromDate`, `toDate`, `quantity`, `quantityType`(+Code).
- **Admission:** `admissionType`(+Code), `admissionSource`(+Code), `nursingHomeResidentialStatus`(+Code).
- **Home health:** `homeHealthStartDate`, `homeHealthCertificationPeriodStartDate/EndDate`.
- **Transport:** `transportType`(+Code), `transportDistance`, `transportPurpose`, `transportLocations[]`.
- **Chiropractic/spinal:** `chiropracticTreatmentCount`, `beginningSubluxationLevel`(+Code), `endingSubluxationLevel`(+Code), `spinalCondition`(+Code/+Description).
- **Oxygen:** `oxygenEquipmentType`(+Code), `oxygenFlowRate`, `oxygenDailyUseCount`, `oxygenUsePeriodHourCount`, `oxygenOrderText`, `oxygenDeliverySystemType`(+Code).
- **Other:** `supplementalInformation` (contains `attachments`, `diagnoses`, `procedures`, `refAuthNumber`, `sequence`, `updateType`).

**GET (search) query params** include `payer.id`, the full `requestingProvider.*` set (name, npi, taxId, specialtyCode, address, contact, phone/fax), `subscriber.*`, `patient.*` (including `birthDate`, `subscriberRelationshipCode`), `requestTypeCode`, `fromDate`, `toDate`, `certificationIssueDate`, `certificationNumber`, `referenceNumber`, `statusCode`, and `sessionId` (valid 24h, returned on 200/404/504).

**Submission flow:** valid POST → `202 Accepted` + `Location` (e.g. `.../v2/service-reviews/-11923818311`) + `X-Status-Message`. Poll until the payer responds. Invalid → `400` with `validationMessages[]`. A search GET returns a `Location` carrying a `sessionId` (e.g. `?sessionId=-1283121411`); poll that until `200` returns the matched `serviceReviews[]`.

**`status` / `statusCode` values:**

| statusCode | status | Meaning |
| --- | --- | --- |
| A1 | Certified in Total | Fully authorized; `certificationNumber` usable on an 837. |
| A2 | Certified - Partial | Partially authorized; check `procedures[]`. |
| A3 | Not Certified | Denied; check `statusReasons[]`. |
| A4 | Pending | Still processing; check back. |
| A6 | Modified | Request was modified; check back. |
| C | Canceled | Request canceled. |
| CT | Contact Payer | Provider should contact the plan (see `payer`). |
| NA | No Action Required | Authorization not required. |
| 51 | Complete | Request complete. |
| 71 | Term Expired | Authorization term expired. |

(Interim processing states also appear, e.g. `BR` Building Request, `0` In Progress.)

**Response objects:**
- **Top level:** `id`, `customerId`, `userId`, `controlNumber`, `status`/`statusCode`, `statusReasons[]`, `createdDate`, `updatedDate`, `expirationDate`, `shortFormIndicator`, `updatable`, `updatableFields[]`, `deletable`, `validationMessages[]`, certification dates + `certificationNumber`, `referenceNumber`, `traceNumbers[]`, `requestType`(+Code), service/admission/transport/oxygen/chiropractic/spinal fields (mirror the request body), `payer`, `payerNotes[]`, `providerNotes[]`, `requestingProvider`, `subscriber`, `patient`, `diagnoses[]`, `renderingProviders[]`, `procedures[]`, `supplementalInformation`.
- **`shortFormIndicator` note:** Whether TRUE or FALSE, it sets transaction type code 25 (admission reviews) or 26 (health service reviews); for specialty care review, TRUE→429, FALSE→24.
- **`statusReasons[]`:** `value` (description), `code` (X12 code source 886: Service Review Decision Reason Codes).
- **`validationMessages[]`:** `errorMessage`, `field`, `index`, `code` (Availity-side errors carry `field`/`index`; payer errors carry `code`).
- **`payer`:** `id`, `name`, `contactName`, `phone`/`extension`/`fax`, `emailAddress`, `url`.
- **`requestingProvider` / `renderingProviders[]`:** names, `npi`, `taxId`, `payerAssignedProviderId`, `submitterId`, `specialty`(+Code; X12 source 682 taxonomy), address, `contactName`, `phone`/`extension`/`fax`, `emailAddress`, `url`. `renderingProviders[]` add `role`(+Code; provider codes from 278 TR3).
- **`subscriber`:** `memberId`, names, address.
- **`patient`:** names, `birthDate`, `gender`(+Code F/M/U), `accountNumber`, `subscriberRelationship`(+Code: 18 Self / 01 Spouse / 19 Child / G8 Other), address, `status`(+Code; X12 source 239 Patient Status), `condition`(+Code), `medicareCoverage`, `prognosis`(+Code: 2 Guarded, 3 Fair, 4 Good, 5 Very Good, 6 Excellent, 7 <6 Months to Live, 8 Terminal).
- **`diagnoses[]`:** `qualifier`(+Code; 278 TR3 diagnosis qualifiers), `value`, `date`.
- **`transportLocations[]`:** `name`, address.
- **`procedures[]`:** `status`(+Code; same status table), `statusReasons[]`, certification dates + `certificationNumber`, `qualifier`(+Code), `value`, `code` (278 TR3), `modifier1..4`(+Codes), `description`, `quantity`/`quantityType`(+Code), `fromDate`/`toDate`, `payerNotes[]`, `traceNumbers[]`.
- **`supplementalInformation.attachments[]`:** `fileName`, `id`, `idType`, `dateReceived`.

### 5.5 Claim Statuses 1.0.0 (276/277)

Find, create, and manage claim status info; enables the standard X12 276/277 transactions. (For richer search and value-add fields, the **Enhanced Claim Status** API in §6 supersedes this.)

**Configurations type:** `claim-statuses-inquiry` (send `type` alone to list supporting payers; add `payerId` for per-payer rules).

**Endpoints:**

| # | Path | Function |
| --- | --- | --- |
| 1 | `POST /v1/claim-statuses` | Initiate a new inquiry (or view an existing request). |
| 2 | `GET /v1/claim-statuses/{id}` | Retrieve a full claim status by response ID. |
| 3 | `DELETE /v1/claim-statuses/{id}` | Delete a claim status by response ID. |

> **Quirk:** The create call is a `POST` that *tunnels a GET* — you must send `Content-Type: x-www-form-urlencoded` **and** the header `X-HTTP-Method-Override: GET`. Parameters are form fields.

**POST form parameters (all optional):** `payer.id`; `submitter.lastName`/`firstName`/`middleName`/`suffix`/`id`; `providers.lastName`/`firstName`/`middleName`/`suffix`/`npi`/`taxId`/`payerAssignedProviderId`; `subscriber.memberId`/`lastName`/`firstName`/`middleName`/`suffix`; `patient.lastName`/`firstName`/`middleName`/`suffix`/`birthDate`/`genderCode` (F/M)/`accountNumber`/`subscriberRelationshipCode` (X12 837 relationship codes); `fromDate`; `toDate`; `claimNumber`; `claimAmount`; `facilityTypeCode` (X12 source 237/236); `frequencyTypeCode` (X12 source 235). `Accept`/`Content-Type` headers as usual.

**Async flow:** valid POST → `202` + `Location` to poll (an empty `claimStatuses: []` envelope while waiting); maintenance/no-response → `504`; success → `200` with `claimStatuses[]` summaries. Then `GET /{id}` returns the full detail.

**HTTP codes (GET/create):** 200 fulfilled; 202 awaiting payer (retry later); 400 payer-indicated error (see `validationMessages`); 404 no claim status with that ID; 504 payer timeout. **DELETE:** 204 deleted; 404 not found.

**Status codes** (`status`/`statusCode`) reference X12 External Code Source 508 (Claim Status Codes); category codes reference source 507. A common interim state is `statusCode: 0` / `In Progress`; terminal success is `statusCode: 4` / `Complete`.

**Response — primary result set:** `id`, `customerId`, `controlNumber`, `createdDate`, `updatedDate`, `expirationDate`, `status`/`statusCode`, `payer`, `submitter`, `providers[]`, `patient`, `subscriber`, `fromDate`/`toDate`, `claimNumber`, `claimAmount`, `facilityTypeCode`(+`facilityType`), `frequencyTypeCode`(+`frequencyType`), `claimCount`, `claimStatuses` (object/array of per-claim detail). The `payer`/`submitter`/`providers`/`patient`/`subscriber` field interiors follow the X12 276/277 TR3.

**`claimStatuses[]` (per-claim):** `traceId`, `claimControlNumber`, `facilityTypeCode`(+`facilityType`), `frequencyTypeCode`(+`frequencyType`), `patientControlNumber`, `pharmacyPrescriptionNumber`, `voucherNumber`, `claimIdentificationNumber`, `fromDate`/`toDate`, `statusDetails[]`, `serviceLines`.

**`claimStatuses.statusDetails[]`:** `category`(+`categoryCode`; source 507), `status`(+`statusCode`; source 508), `entity`(+`entityCode`; 276/277 TR3), `effectiveDate`, `claimAmount`(+`claimAmountUnits`), `paymentAmount`(+`paymentAmountUnits`), `finalizedDate`, `remittanceDate`, `checkNumber`.

**`claimStatuses.serviceLines[]`:** `procedureQualifier`(+Code), `procedure`(+`procedureCode`; 276/277 TR3), `modifier1..4`(+Codes), `chargeAmount`(+Units), `paymentAmount`(+Units), `service`/`serviceCode`, `quantity`, `controlNumber`, `fromDate`/`toDate`, and a nested `statusDetails[]` (same shape: `category`/`categoryCode`, `status`/`statusCode`, `entity`/`entityCode`, `effectiveDate`, amounts, dates, `checkNumber`).

### 5.6 Patient Cost Estimator APIs (Predeterminations)

A Patient Cost Estimator (PCE), a.k.a. **predetermination**, is a pre-service claim a provider submits so patients learn in advance whether a service is covered. All three variants are **asynchronous** (POST → `202` + `Location` → poll the `{id}` until `200`).

| Variant | Create endpoint | Get endpoint | Configurations |
| --- | --- | --- | --- |
| PCE 1.0.0 – Professional | `POST /v1/professional-claims` | `GET /v1/professional-claims/{id}` | `type=professional-claims`, `subtypeId=PRE_DETERMINATION` |
| PCE 1.0.0 – Institutional | `POST /v1/institutional-claims` | `GET /v1/institutional-claims/{id}` | `type=institutional-claims`, `subtypeId=PRE_DETERMINATION` |
| PCE 2.0.0 – Professional | `POST /v2/patient-cost-estimates/prof` | `GET /v2/patient-cost-estimates/prof/{id}` | (validation surfaced in the response) |

**HTTP codes (all variants):** 200 processed (body has result); 202 processing/retrying; 400 validation failed (correct & resubmit); 404 no predetermination with that ID; 500 health-plan server error (retry later); 504 gateway timeout after retries. **PCE v2 adds:** 422 Unprocessable (could not process).

**Demo scenarios (`X-Api-Mock-Scenario-ID`):**
- PCE v1 Professional: `CCEP-Success-i` (GET id `123` →200), `CCEP-Accepted-i` (POST 202), `CCEP-RequestErrors1-i`/`2-i` (POST 400), `CCEP-RequestParseError-i` (POST 500), `CCEP-MultiServiceLines-i` (GET id `54321` →200).
- PCE v2 Professional: `PCEP-Success` (GET id `efe8b6ab-e47d-4975-aa73-adda35311850` →200), `PCEP-Accepted` (POST 202), `PCEP-BadRequestError` (400), `PCEP-UnprocessableError` (422), `PCEP-InternalError` (500), `PCEP-TimeoutError` (504).

#### Request body — common shape

All three take a single claim body. **PCE v1** top-level: `requestTypeCode` (`PRE_DETERMINATION`), `submitter`, `payer`, `billingProvider`, `patient`, `subscriber`, `claimInformation`, `payerSpecificFlags[]`. **PCE v2** drops `requestTypeCode` and instead carries `coverageId` (the eligibility ID for the active member) and `payerId` at the top, wrapping everything else the same way; its `200` response nests the data under a `result` object and uses `createdDt` instead of `createdDate`.

Minimal Professional example (`claimInformation` with `placeOfServiceCode`, `diagnoses[]`, `serviceLines[]`):

```json
{
  "requestTypeCode": "PRE_DETERMINATION",
  "billingProvider": { "npi": "1234567893", "ein": "111222333", "payerAssignedProviderId": "XYZ321" },
  "patient": { "relationshipCode": "01", "lastName": "LASTNAME", "firstName": "FIRSTNAME", "stateCode": "FL", "birthDate": "YYYY-MM-DD", "genderCode": "M" },
  "payer": { "id": "BCBSF" },
  "submitter": { "id": "123456789", "lastName": "SUBMITTER" },
  "subscriber": { "memberId": "MEMBER_ID", "groupName": "GROUP_NAME", "groupNumber": "GROUP_NUMBER" },
  "claimInformation": {
    "placeOfServiceCode": "11",
    "diagnoses": [ { "qualifierCode": "ABK", "code": "J3089" } ],
    "serviceLines": [ { "procedureCode": "92523", "quantity": "100", "amount": "250", "fromDate": "2016-05-10" } ]
  }
}
```

The **Institutional** body differs in `claimInformation`: it uses `facilityTypeCode`, a `principalDiagnosis` object (`code`, `qualifierCode`), and `serviceLines[]` with `revenueCode` + `procedureCode`.

#### Response — shared top level

`id`, `createdDate` (v2: `createdDt`), `updatedDate`, `expirationDate`, `message`, `customerId`, `requestTypeCode`, `submitter`, `payer`, `billingProvider`, `subscriber`, `patient`, `claimInformation`, `payerSpecificFlags[]`. (PCE v2 wraps everything below `id`/`createdDt` inside `result`.)

- **`payer` (select):** `naicCode`, `responsibilitySequenceCode` (P/S/T), `insuranceTypeCode`, `claimFilingIndicatorCode` (837 TR3).
- **`billingProvider` (select):** `specialtyCode` (taxonomy 682), `stateLicenseNumber`, `upin`, `payerAssignedProviderId`, `payToAddress`; PCE v2 adds `networkStatus`.
- **`subscriber` (select):** `totalDeductible`, `accumulatedDeductible`, `remainingDeductible`, `onHold` (bool), `holdReasons[]`. **Institutional** also has family-level `totalFamilyDeductible`/`accumulatedFamilyDeductible`/`remainingFamilyDeductible`.
- **`patient`:** names, `birthDate`, `gender`(+Code), `subscriberRelationship`(+Code) — interiors per 837 TR3.

#### `claimInformation` — Professional (v1 & v2)

A large object. Grouped highlights (most fields map directly to X12 837 Professional TR3, which the source defers to for code lists):

- **Identifiers/control:** `controlNumber`, `claimNumber` (v2), `placeOfServiceCode` (source 237), `frequencyTypeCode` (source 235), `claimIdentifier`, `medicalRecordNumber`, `demonstrationProjectIdentifier`, `payerClaimControlNumber`, `repricedClaimReferenceNumber`, `adjustedRepricedClaimReferenceNumber`, `carePlanOversightNumber`, `mammographyCertificationNumber`, `clinicalLaboratoryImprovementAmendmentNumber`, `investigationalDeviceExemptionIdentifier`, `referralNumber`, `priorAuthorizationNumber`.
- **Assignment/consent codes:** `providerSignatureOnFile` (Y/N), `providerAcceptAssignmentCode` (A/B/C), `benefitsAssignmentCertification` (Y/N/W), `informationReleaseCode` (Y/I), `patientSignatureSourceCode`.
- **Program/contract:** `specialProgramCode` (02/03/05/09), `delayReasonCode`, `serviceTypeCode` (270/271 service types), `contractTypeCode` (01–06/09), `contractAmount`/`contractPercentage`/`contractCode`/`contractTermsDiscountPercentage`/`contractVersionIdentifier`, `serviceAuthorizationExceptionCode`, `medicareCrossoverIndicator` (Y/N), `patientPaidAmount`.
- **Accident:** `accidentCauseCode1/2` (AA/EM/OA), `accidentStateCode` (source 22), `accidentDate`.
- **Clinical dates:** `onsetOfCurrentIllnessOrSymptomDate`, `initialTreatmentDate`, `lastSeenDate`, `acuteManifestationDate`, `lastMenstrualPeriodDate`, `lastXrayDate`, `hearingAndVisionPrescriptionDate`, `disabilityPeriodStartDate`/`EndDate`, `lastWorkedDate`, `authorizedReturnToWorkDate`, `admissionDate`, `dischargeDate`, `assumedCareDate`, `relinquishedCareDate`, `propertyAndCasualtyFirstContactDate`, `repricerReceivedDate`.
- **Notes:** `claimNoteReferenceCode` (ADD/CER/DCP/DGN/TPO), `claimNoteText`.
- **Ambulance:** `ambulanceTransportReasonCode`, `ambulanceTransportDistance`, round-trip/stretcher purpose descriptions, `ambulanceCertifications[]` (`certificationConditionIndicator` Y/N, `conditionCode1..5`), `ambulancePickUpLocation`/`ambulanceDropOffLocation`.
- **Chiropractic/vision/EPSDT:** `spinalManipulationServicePatientConditionCode`(+Description1/2), `visionConditions[]` (`codeCategory` E1/E2/E3, condition codes), `homeboundIndicator` (Y/N), `epsdtReferralCertificationConditionCodeAppliesIndicator` (Y/N), `epsdtReferralConditionCode1/2/3` (AV/NU/S2/ST).
- **Anesthesia:** `anesthesiaRelatedSurgicalPrincipalProcedure`, `anesthesiaRelatedSurgicalOtherProcedure` (HCPCS, source 130).
- **Diagnoses:** `diagnoses[]` → `qualifier`/`qualifierCode` (ABK ICD-10-CM / BK ICD-9-CM), `code`.
- **Provider/facility objects:** `referringProvider`, `primaryCareProvider`, `renderingProvider`, `serviceFacility`, `supervisingProvider`.
- **Other payers (COB):** `otherPayers[]` — `responsibilitySequenceCode` (P/S/T), `insuranceTypeCode`, `claimFilingIndicatorCode`, `claimPaidDate`, `secondaryPayerIdentificationNumber`, `ein`, `claimOfficeNumber`, `naicCode`, `priorAuthorizationNumber`, `referralNumber`, `claimAdjustmentIndicator` (Y), `claimControlNumber`, `paidAmount`, `remainingPatientLiabilityAmount`, `nonCoveredAmount`, consent codes, `subscriberRelationshipCode`.
- **`supplementalInformation[]`:** `reportTypeCode`, `reportTransmissionCode`, `controlNumber`.
- **Totals (each an amount object with `amount`/`description`/`code`/`codeType`/`procedure`/`patientLiable`):** `totalCharges`, `totalAllowed`, `totalEstimated`, `totalNotCovered`, `totalCoPay`, `totalCoInsurance`, `totalDeductible`, `totalContractual`, `totalProviderInitiated`, `totalProviderResponsibility`, `totalPatientLiability`. PCE v2 also returns `claimTotalCharges`/`claimTotalEstimated`/`claimTotalPatientLiability`. Plus `bundled` (bool), `messages[]`, `displayMessage`.

**`claimInformation.serviceLines[]` (Professional)** — `controlNumber`, `procedureCode` (837 TR3), `modifierCode1..4`, `procedureDescription`, `amount`, `quantityTypeCode`/`quantity`, `placeOfServiceCode`, `diagnosisCodePointer1..4` (1–12), `emergencyIndicator`/`epsdtIndicator`/`familyPlanningIndicator` (Y/N), `copayStatusCode`, a full DME set (`dmeProcedureCode`, prices, `dmeFrequencyCode` 1/4/6, `dmeCertificationTypeCode` I/R/S, certification condition codes/dates, `dmeLengthOfMedicalNecessity`, `dmeDuration`), ambulance fields, drug fields (`nationalDrugCode`, `nationalDrugUnitCount`, `nationalDrugUnitCodeQualifer` F2/GR/ME/ML/UN, `linkSequenceNumber`, `pharmacyPrescriptionNumber`), many clinical dates, repricing/reference numbers, tax/postage amounts, notes, provider/facility/location objects, `estimatedPatientResponsibility`, `bundlingDescription`, `denyReason`, `holdReasons[]`, `messages`/`displayMessage`, the same set of amount objects (`allowed`, `notCovered`, `coPay`, `coInsurance`, `deductible`, `estimated`, `contractual`, `providerInitiated`, `providerResponsibility`), `remarks[]` (Remittance Advice Remark Codes), `adjudicationInformation[]`, `formIdentificationCodes[]`. PCE v2 adds `patientHeight`, `priorAuthorizationOtherPayerId`, `priorAuthRequired`, `priorAuthVendorInfo`.
  - **`serviceLines[].adjudicationInformation[]`:** `payerId`, `paidAmount`, `procedureCode`, `modifierCode1..4`, `procedureDescription`, `quantity`, `lineNumber`, `claimPaidDate`, `remainingPatientLiabilityAmount`, `claimAdjustmentGroups[]`.
  - **`claimAdjustmentGroups[]`:** `groupCode` (CO/CR/OA/PI/PR), `adjustments[]` (`reasonCode`, `amount`, `quantity`).
  - **`serviceLines[].formIdentificationCodes[]`:** `codeListQualifierCode` (AS/UT), `formIdentifier`, `supportingDocumentation[]` (`questionNumber`, `yesNoResponse` Y/N/W, `textResponse`, `percentResponse`, `dateResponse`).

#### `claimInformation` — Institutional (v1)

Institutional-specific fields: `controlNumber`, `facilityTypeCode` (Uniform Bill Type, positions 1–2), `frequencyTypeCode` (source 235), `providerAcceptAssignmentCode`, `benefitsAssignmentCertification`, `informationReleaseCode`, `admissionTypeCode` (source 231), `admissionSourceCode` (source 230), `patientStatusCode` (source 239), `bundled`, `messages[]`/`specialMessages[]`/`displayMessage`, `serviceTypeCode`, `occurrenceCode`(+`occurrenceCodeDate`; source 132 NUBC), `valueCode`(+`valueCodeAmount`; NUBC), `conditionCode` (NUBC), `claimIdentifier`, `diagnosisRelatedGroupCode` (source 229 DRG), and the same family of total-amount objects plus `attendingProvider`.

Diagnosis/procedure objects (each with `qualifier`/`qualifierCode`/`code`/`description` and, where noted, `presentOnAdmissionIndicatorCode` Y/N/U/W or `codeDate`):
- `principalDiagnosis` (ABK/BK), `admittingDiagnosis` (ABJ/BJ), `patientsReasonForVisit` (APR/PR), `principalProcedure` (BBR/BR/CAH), `otherDiagnoses[]` (ABF/BF), `otherProcedures[]` (BBQ/BQ).

**`claimInformation.serviceLines[]` (Institutional)** — `controlNumber`, `fromDate`/`toDate`, `revenue`/`revenueCode` (NUBC source 132), `procedure`/`procedureCode`/`alternateProcedure` (837 Institutional TR3), `modifierCode1..4`, `procedureDescription`, `amount`, `quantityTypeCode` (DA/UN)/`quantity`, `estimatedPatientResponsibility`, `bundlingDescription`, `denyReason`, `holdReasons[]`, `messages`/`displayMessage`, and the amount objects (`allowed`, `estimated`, `notCovered`, `coPay`, `coInsurance`, `deductible`, `contractual`, `providerInitiated`, `providerResponsibility`), `remarks[]`.

---

## 6. Enhanced Claim Status (claim-status 1.0.0)

Supports the HIPAA 276/277 plus proprietary searches and value-add fields (remark codes/descriptions, amount fields, group number, line of business, provider types, and more). Two workflows, two calls each.

**Workflow A — searchBy276 + valueAdds277:**
- `searchBy276` renders a standard X12 276 request as JSON.
- `valueAdds277` returns the 277 response merged with payer value-add data. **Requires** a prior successful `searchBy276`.

**Workflow B — summarySearch + detailSearch:**
- `summarySearch` — high-level info on one/many claims by service date, member ID, claim history, and/or check number.
- `detailSearch` — detailed single-claim info by provider TIN/NPI + claim number. Can run independently or after a `summarySearch`.

**Configurations:** `type=enhanced-claim-status`, `subtypeId` ∈ `HIPAA_276` | `VALUE_ADDS_277` | `SUMMARY` | `DETAIL`.

**HTTP codes:** 200 OK; 400 validation/invalid; 403 not allowed; 404 URL issue; 410 response too old; 500 unexpected; 502 payer error outside Availity; 503 temporarily unavailable; 504 timed out (resubmit).

> The endpoint base path in the docs is `/clmsmgmt/claim-status/claim-status/v1/status/...`. Sample cURL uses a QA host (`qa-apps.availity.com/.../internal/...`); use your environment's production base path.

### 6.1 searchBy276

| # | Path | Function |
| --- | --- | --- |
| 1 | `POST .../status/searchBy276` | Perform a 276 search. |
| 2 | `GET .../status/searchBy276?id={id}` | Poll for completion using the response `id`. |

**POST filter — required fields:** `payerId`, `fromDate`, `toDate`, `patientLastName`, `patientFirstName`, `patientBirthDate`, `patientSameAsSubscriber` (boolean), `providerLastName`.
**Optional:** `offset`/`limit`, `traceId`, `claimNumber`, `claimAmount`, `frequencyTypeCode`, `patientMemberId`, `patientAccountNumber`, `patientMiddleName`, `patientSuffix`, `patientGender` (F/M/U), `providerFirstName`/`MiddleName`/`Suffix`, `providerNpi`, `providerPayerAssignedId`, `providerTaxId`, `subscriber*` (lastName/firstName/middleName/suffix/memberId).

**Sample POST body:**

```json
{
  "payerId": "CSP01",
  "patientMemberId": "MEMBER_ID",
  "providerLastName": "last",
  "providerNpi": "1003868548",
  "subscriberMemberId": "MEMBER_ID",
  "requestType": "HIPAA_276",
  "subscriberLastName": "sublast",
  "patientLastName": "Last",
  "patientFirstName": "First",
  "patientBirthDate": "2000-01-01",
  "fromDate": "2023-01-01",
  "toDate": "2023-01-01",
  "patientSameAsSubscriber": false
}
```

POST returns `{ "id": "<uuid>" }`; GET with that `id` returns the claim status `items[]` once `completeCode: 4`.

### 6.2 valueAdds277

| # | Path | Function |
| --- | --- | --- |
| 1 | `POST .../status/valueAdds277` | Request value-add data for a prior 277. Requires a prior `searchBy276`. |
| 2 | `GET .../status/valueAdds277?id={id}` | Poll for completion. |

**POST filter — required:** `parentTransactionId` (the `id`/`AVAILITY_TRACE_ID` from searchBy276), `claimNumber`, `claimIndex` (zero-based index of the claim in the parent transaction), `payerId` (must match searchBy276).
**Optional:** `traceId`, `requestType` (only `VALUE_ADD_277`), `providerTaxId`, `processedDate`.

Returns a single enriched `claim` object with full `amounts` map (see §6.5), `serviceLines[]`, etc.

### 6.3 summarySearch

| # | Path | Function |
| --- | --- | --- |
| 1 | `POST .../status/summarySearch` | Real-time summary search. |
| 2 | `GET .../status/summarySearch/{id}` | Poll for completion. |

> Provider TINs and NPIs must be set up under the Availity organization you registered.

**POST filter — required:** `payerId`, `requestType` (`SERVICE_DATE` or `MEMBER_ID`).
**Optional:** `offset`/`limit`, `traceId`, `memberId`, `memberBirthDate`, `groupNumber`, `fromDate`, `toDate`, `processedDate`, `providerNpi`, `providerTaxId`, `checkNumber`, `patientAccountNumber`, `payerAssignedProviderId`, and `requestedStatus`:

- `ALL` — all statuses incl. `ACKNOWLEDGED`
- `ALL_PENDING` — `ACTION_REQUIRED` + `PENDING`
- `ALL_FINALIZED` — `ADJUSTED`, `CAPITATED`, `DENIED`, `FINALIZED`, `PAID`
- `ALL_ERROR_REJECTED` — `ERROR`, `REJECTED`, `RETURNED_TO_PROVIDER`

### 6.4 detailSearch

| # | Path | Function |
| --- | --- | --- |
| 1 | `POST .../status/detailSearch` | Detailed single-claim search. |
| 2 | `GET .../status/detailSearch/{id}` | Poll for completion. |

**POST filter — required:** `payerId`, `requestType` (only `CLAIM_NUMBER`), `claimNumber`.
**Optional:** `parentTransactionId` (from a prior summarySearch — not required), `traceId`, `processedDate`, `providerNpi`, `providerTaxId`, `fromDate`, `toDate`.

The detail `claim` exposes workflow eligibility flags: `eligibleForAttachment`, `requiresAttachment`, `eligibleForCorrection`, `eligibleForMessaging`, `eligibleForAppeal`, `eligibleForValueAdds`, `eligibleForChat`, `eligibleForOverpayment`, plus `adjusted`/`summary`.

### 6.5 Response definitions (shared)

**Top level:** `offset`, `limit`, `count`, `totalCount`, `id`, `errors[]` (`code`, `level` ERROR/WARNING, `developerMessage`, `userMessage`), `request`, `items[]` (or single `claim`), `traceIds` (map: `SUBMITTER_TRACE_ID`, `AVAILITY_TRACE_ID`, `PAYER_TRACE_ID`, `FAST_PATH_ID`, `AVAILITY_LEGACY_TRACE_ID`, `VALUE_ADD_*`). `completeCode: 4` indicates done.

**`claim` object — key fields:** `claimNumber`, `type` (`INSTITUTIONAL`/`PROFESSIONAL`/`DENTAL`), `customerId`, `status`, `comment`, `statusCode`(+Description; X12 code source 508), `customStatusCode`(+Description), `categoryCode`(+Description; source 507), `customCategoryCode`(+Description), dates (`submittedDate`, `receivedDate`, `exchangeDate`, `promptPayPenaltyDate`, `effectiveDate`, `finalizedDate`, `fromDate`, `toDate`), `lineOfBusiness`, `billTypeIdentifier`(+Description), `drgCode`/`drgVersion`/`drgWeight`, `controlNumber`, `userId`, `claimGenKey`, `facilityTypeCode`(+Description), `frequencyTypeCode`(+Description; source 235), `otherIds`, `adjusted`, `summary`, `remarks[]`/`customRemarks[]`, `amounts`, `payer`, `otherInsurances[]`, `submitter`, `providers[]`, `subscriber`, `patient`, `remittanceInfo[]`, `statusDetails[]`, `serviceLines[]`, `additionalProperties`.

**`status` enum:** `ACKNOWLEDGED`, `ACTION_REQUIRED`, `ADJUSTED`, `CAPITATED`, `DENIED`, `ERROR`, `FINALIZED`, `PAID`, `PENDING`, `REJECTED`, `RETURNED_TO_PROVIDER`.

**`amounts` map keys:** `ADJUSTMENT`, `ALLOWED`, `BILLED`, `COINSURANCE`, `COPAY`, `DEDUCTIBLE`, `HEALTH_CARE_ACCOUNT`, `INELIGIBLE`, `INSURANCE_CURRENT_PAID`, `INSURANCE_PRIOR_PAID`, `INSURANCE_TOTAL_PAID`, `INTEREST_OR_PENALTY`, `MEDICARE_PAID`, several `OTHER_INSURANCE_*` and `OUT_OF_NETWORK_*` (COB), `PATIENT_PAID`, `PATIENT_RESPONSIBILITY`, `PRIOR_NOTIFICATION_*`, `POOL_LIABILITY`, `SEQUESTRATION`, `THIRD_PARTY_ADMINISTRATOR`. Each value is `{ "value": "<amount>", "detail": "USD" }`.

**`remittanceInfo[]`:** `id`, `remittanceDate`, `payeeType` (`SUBSCRIBER`/`PROVIDER`/`ALTERNATE`/`INSTITUTIONAL`/`UNKNOWN`), `payeeName`, `payeeTaxId`, `payeeNPI`, `payeeId`, `checkAmount`, `checkNumber`, `checkDate`, `checkStatus`, `checkCashedDate`, `deliveryInfo`.

**`statusDetails[]`:** `statusCode`(+Description; 508), `customStatusCode`(+Description), `categoryCode`(+Description; 507), `customCategoryCode`(+Description), `entityCode`(+Description), `effectiveDate`, `finalizedDate`.

**`serviceLines[]`:** `lineNumber`, `procedureQualifier`(+Code/+Description), `procedureCode`(+Description), `revenueCode`(+Description; source 132), `diagnosisCodes[]`, `modifiers`, `amounts`, `remarks[]`/`customRemarks[]`, `adjustments`, `serviceCode`, `quantity`, `controlNumber`, `fromDate`/`toDate`, `statusDetails[]`, status fields, `additionalProperties`.

**`serviceLines[].adjustments`:** `contractualObligations[]`, `otherObligations[]`, `payerInitiatedObligations[]`, `patientResponsibility[]` — each item: `reasonCode` (source 139), `reasonDescription`, `amount`, `quantity`, `additionalProperties`.

---

## 7. Service Reviews Add-On APIs

Two add-ons that enhance the 278 (auth/referral) transaction. Both reuse the Service Reviews `serviceReview` body and response definitions.

### 7.1 IsAuthRequired 2.0.0 (Currently Unavailable)

> **Status:** Unavailable to new customers.

Determines whether a payer requires an authorization for a service before completing the auth request. Builds a payer-specific IsAuthRequired request from a Service Reviews request; integrates the result back with a procedure status of **Auth Required** / **No Auth Required**.

| # | Path | Function |
| --- | --- | --- |
| 1 | `POST /value-adds/v1/isauthrequired` | Create the IsAuthRequired request (async → `Location`). |
| 2 | `GET /value-adds/v1/isauthrequired/{id}` | Poll for the result. |

Send a `serviceReview` body (same schema as §5.4). In the GET response, the `procedures[].status` (and `statusCode`, e.g. `AR`) field indicates whether auth is required.

### 7.2 Attachments 2.0.0 (Currently Unavailable)

> **Status:** Closed pilot; unavailable to new customers.

Submit attachments with an authorization request and check attachment status.

| # | Path | Function |
| --- | --- | --- |
| 1 | `POST /value-adds/v2/attachments` | Submit a service review with an `attachments[]` array. |
| 2 | `GET /value-adds/v2/attachments/{id}` | Poll attachment status. |

**Top-level POST fields:** `id`, `vaultUri`, `status` (`SUBMITTED`/`AV_ACCEPTED`/`AV_REJECTED`/`PY_ACCEPTED`/`PY_REJECTED`), `responseMessage[]`, `serviceReview` (minimum fields: `payer.id`, `customerId`, `patient.lastName`/`firstName`, `subscriber.memberId`, `requestingProvider.lastName`/`stateCode`/(`npi` OR `taxId`), `controlNumber`, and `referenceNumber` OR `certificationNumber`), `attachments[]`.

**`attachments[]` fields:** `attachmentTypeCode` (X12 275 codes), `contentType`, `title` (incl. extension), `data` (base64), `size`, `creation`.

**Payer-specific attachment limits** (allowed file types vary by payer; all require `applicationTypeCode`, `contentType`, `title`, `data`, `size`, `creation`; max **10 files** each):

| Payer | Max file size (bytes) |
| --- | --- |
| Aetna | 201,326,592 |
| Humana | 6,000,000 |
| Anthem | 104,857,600 |
| HCSC | 41,943,040 |
| Molina | 134,217,728 |
| Premera | 10,485,760 |
| Regence | 62,914,560 |

(Allowed MIME types differ per payer — e.g. Humana excludes Word docs; Premera/Regence allow the widest set including Office, RTF, Outlook msg, HTML, BMP. Verify per payer before sending.)

---

## 8. E&B Value-Add APIs

Two value-adds for the eligibility & benefits (270/271) transaction. Both are multi-payer and use HTTP response codes (2xx success, 5xx error) with a `statusCode: 4` "Success" completion marker.

### 8.1 Care Reminders (Beta)

Real-time request/retrieval of care reminders (care gaps) for use within an E&B transaction.

**Endpoint:** `POST /pre-claim/eb-value-adds/care-reminders`

**Common parameters:** `payerId` (**required**), `memberId` (**required** — send full ID Prefix+Policy#+Sequence#), `stateId`, `lineOfBusiness` (EB04), `insuranceTypeCode` (EB04), `controlNumber` (from a prior Coverages call), `firstName`/`lastName`/`middleName`, `providerNPI`, `providerTax`, `providerPAPI`, `submitterId`, `subscriberRelationship` (`S`/`D`), `suffix`, `groupNumber`, `dateOfBirth`, `genderCode` (F/M/U).

**Payer-specific required fields (examples):**
- BCBS Michigan (00710): `payerId`, `memberId`, `lineOfBusiness` (BCN/COMMERCIAL/MAPPO).
- Florida Blue family (BCBSF, TRULI, FBM01, OTHER BLUE PLANS): `payerId`, `memberId`; optional `controlNumber`, `firstName`, `lastName`.
- Healthfirst NY (80141T): `payerId`, `memberId`; optional `asOfDate`.
- Humana (HUMANA) & Regence family: `payerId`, `memberId` + a broad set of optional fields.
- Molina family: `payerId`, `memberId`, `stateId`.

**Response shape:** `status`, `statusCode`, `createdDate`, and `data` → `disclaimer`, `title`, `careReminderDetails[]`. Each detail item is a renderable table: `order[]` (column order), `headers{}` (key→display text), `rows[]` (key→cell value). Availity does not format the table — it returns column order, headers, and rows so you build the UI.

### 8.2 Member ID Card (Beta)

Real-time request/retrieval of member ID cards during an E&B inquiry. Supports **PDF and PNG**.

**Endpoints:**
- `POST /pre-claim/eb-value-adds/member-card` — request the card; returns `data.memberCards.type` (e.g. `application/pdf`) and `uris[]` containing a `{GTID}`.
- `GET /pre-claim/eb-value-adds/member-card/{GTID}` — retrieve the actual card document(s).

**Common parameters:** `payerId` (**required**), `memberId` (**required**), `groupNumber`, `controlNumber`, `firstName`, `lastName`, `dateOfBirth`, `planType` (Medical/Dental), `effectiveDate`, `stateCode`, `planId`, `policyNumber`, `responsePayerId`, `asOfDate`, `subscriberRelationship` (`S`/`D`), `insuranceTypeCode` (EB04), `providerNPI`.

**Payer notes (examples):**
- Aetna family: requires `payerId`, `memberId`, `subscriberRelationship`, `dateOfBirth`, `insuranceTypeCode`, `asOfDate`.
- Aetna Better Health (ABH01) & Mercy Care–Arizona (AZM01): require a `responsePayerId` from a defined allow-list (state-specific IDs).
- Horizon BCBS NJ: if `memberId` starts with prefix `3HZN`, `planId` and `groupNumber` become required.
- Molina family: requires `stateCode`, `planId`, `policyNumber`, `effectiveDate`.

(The source lists many more payer-specific requirement rows — consult the live doc for the exhaustive per-payer matrix.)

---

## 9. Claim Attachments 1.0.0

> **Attention:** Never submit test data to PROD.

External API for clearinghouses to submit medical claim attachments. Two workflows:

1. **Unsolicited** — submit documentation alongside/at the time of claim submission; Availity forwards to the payer.
2. **Solicited** — respond to a payer's `277RFAI` (Request For Additional Information). Availity matches responses to the provider inbox work queue, sends to the payer, and moves the request to the Sent queue.

**Endpoints:**

| # | Path | Function |
| --- | --- | --- |
| 1 | `POST /medical-attachments/ma/external/rest/response` | Submit an attachment to a claim. |
| 2 | `GET /medical-attachments/ma/external/rest/status/{requestId}` | Check attachment status. |

**POST body (top-level, all objects **required** unless noted):** `submitter`, `subscriber`, `patient`, `provider`, `payer`, `encounter`, `payloads[]`, `product`.

**Object fields:**

- **submitter:** `id`*, `state`*, `displayName`* (organization name).
- **subscriber:** `firstName`*, `middleName`, `lastName`*, `accountNumber`*, `memberNumber`*, `dateOfBirth`*, `gender`.
- **patient:** `firstName`*, `middleName`, `lastName`*, `accountNumber`*, `memberNumber`*, `dateOfBirth`*, `gender`.
- **provider:** `lastName`*, `firstName`, `middleName`, `npi`*, `taxId`, `address`, `address2`, `city`, `state`, `zip`.
- **payer:** `id`* (Availity payer ID; max length 35), `displayName`*.
- **encounter:** `claimNumber`* (required for solicited responses), `serviceFrom`*, `requestNumber`* (unsolicited → unique Attachment Control Number per attachment; solicited → payer Attachment Control Number), `serviceTo` (conditionally required if multi-day), `submitDate`, `serviceChargeAmount`.
- **product:** `category`* (only `MEDICAL`), `type`* (only `CLAIM`).
- **payloads[]:** `reasonCodes[]`* , `requestNumber` (only if per-attachment differs from encounter), `contents[]`.
  - **payloads.reasonCodes:** `codeType`* (only `LOINC`), `value`* (unique, non-custom; see LOINC.org).
  - **payloads.contents:** `contentType`* (`{ type, subType }`, e.g. application/PDF), `filename`* (1–255 chars), `data`* (base64-encoded attachment).

(* = required)

**Sample POST (abridged):**

```json
{
  "payer": { "id": "68069" },
  "submitter": { "id": "477823", "state": "TX" },
  "product": { "type": "CLAIM", "category": "MEDICAL" },
  "patient": { "lastName": "LASTNAME", "firstName": "FIRSTNAME", "dateOfBirth": "YYYY-MM-DD", "memberNumber": "MEMBER_NUMBER", "accountNumber": "ACCOUNT_NUMBER" },
  "subscriber": { "lastName": "LASTNAME", "firstName": "FIRSTNAME", "dateOfBirth": "YYYY-MM-DD", "memberNumber": "MEMBER_NUMBER", "accountNumber": "ACCOUNT_NUMBER" },
  "provider": { "lastName": "Nxgen MDX" },
  "encounter": { "serviceTo": "2021-06-24", "serviceFrom": "2021-06-24", "requestNumber": "RN85363437337111" },
  "payloads": [
    { "contents": [
      { "reasonCodes": [ { "value": "76438-1", "codeType": "LOINC" } ],
        "filename": "test.pdf",
        "contentType": { "type": "application", "subtype": "pdf" },
        "data": "JVBERi0xLjQK...EOF" }
    ] }
  ]
}
```

**Success response:** `{ "id": 3325491 }` → use that `id` against `GET /status/{requestId}` (returns `{ "status": "SUBMITTED" }`).
**Validation failure:** a `parameterViolations[]` array with `constraintType`, `path`, `message`, `value`.

---

## 10. Implementation Notes for .NET

A few practical patterns for consuming these APIs from C#/.NET:

**Token management.** Tokens live only 300s, so cache and refresh proactively (e.g. refresh at ~80% of `expires_in`). Use a single typed `HttpClient` (or `IHttpClientFactory`) and a small token provider:

```csharp
public sealed class AvailityTokenProvider
{
    private readonly HttpClient _http;
    private readonly string _clientId, _clientSecret, _scope;
    private string? _token;
    private DateTimeOffset _expiresAt;

    public AvailityTokenProvider(HttpClient http, string clientId, string clientSecret, string scope)
        => (_http, _clientId, _clientSecret, _scope) = (http, clientId, clientSecret, scope);

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        if (_token is not null && DateTimeOffset.UtcNow < _expiresAt)
            return _token;

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.availity.com/v1/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "client_credentials",
                ["client_id"]     = _clientId,
                ["client_secret"] = _clientSecret,
                ["scope"]         = _scope          // space-separate multiple scopes
            })
        };

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var tok = await resp.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)
                  ?? throw new InvalidOperationException("Empty token response.");

        _token = tok.access_token;
        // Refresh a little early to avoid edge-of-expiry 401s.
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(tok.expires_in - 30);
        return _token;
    }

    private sealed record TokenResponse(string access_token, int expires_in, string token_type, string scope);
}
```

**Async polling.** For Coverages, Service Reviews, Enhanced Claim Status, and PCE v2, treat `202 + Location` (or a returned `id`) as "submitted, not done." Poll the `Location`/`GET .../{id}` with a bounded retry + backoff loop, stopping on a terminal `status`/`statusCode` (e.g. Coverages `4`/`3`, Enhanced Claim Status `completeCode: 4`) or when `etaDate`/timeout passes. Respect `429` with backoff.

**Configurations-driven validation.** Rather than hard-coding per-payer field rules, call `GET /v1/configurations` and use each element's `pattern` (regex), `required`, `maxLength`, and `requiredFieldCombinations` to drive validation/UI dynamically. This is the single biggest maintenance win when supporting many payers.

**Resilience.** A `Polly` policy pairs well here: retry on `5xx`/`408`/`429` and transient `HttpRequestException`, with jittered exponential backoff; treat `4xx` (except `429`) as non-retryable and surface `errors[]`/`validationMessages[]`/`parameterViolations[]` to callers.

**Serialization.** Responses use nested objects/arrays with many optional fields; `System.Text.Json` with `JsonSerializerDefaults.Web` and `[JsonIgnore(Condition = WhenWritingNull)]` handles the sparse shapes cleanly. Watch for fields typed as `String` in the docs that carry numeric/boolean-looking values (e.g. amounts are string-valued `{ "value": "211.73" }`).

**Output encoding.** If you render API data straight into a web UI, send `X-Response-Encoding-Context` matching the sink (`HTML`, `HTML_ATTRIBUTE`, etc.) so Availity escapes for you — but still encode on your side as defense-in-depth.

---

## 11. Quick Reference: Endpoint Index

| API | Method & Path |
| --- | --- |
| OAuth2 Token | `POST /v1/token` |
| Payer List | `GET /availity-payer-list` (a.k.a. `/v1/availity-payer-list`) |
| Configurations | `GET /v1/configurations` |
| Coverages | `POST /v1/coverages` · `GET /v1/coverages/{id}` · `DELETE /v1/coverages/{id}` |
| Service Reviews | `POST /v2/service-reviews` · `GET /v2/service-reviews` · `GET /v2/service-reviews/{id}` · `PUT /v2/service-reviews` · `DELETE /v2/service-reviews/{id}` |
| Claim Statuses 1.0.0 | `POST /v1/claim-statuses` (with `X-HTTP-Method-Override: GET`) · `GET /v1/claim-statuses/{id}` · `DELETE /v1/claim-statuses/{id}` |
| PCE v1 – Professional | `POST /v1/professional-claims` · `GET /v1/professional-claims/{id}` |
| PCE v1 – Institutional | `POST /v1/institutional-claims` · `GET /v1/institutional-claims/{id}` |
| PCE v2 – Professional | `POST /v2/patient-cost-estimates/prof` · `GET /v2/patient-cost-estimates/prof/{id}` |
| Enhanced Claim Status | `POST/GET .../status/searchBy276` · `.../status/valueAdds277` · `.../status/summarySearch` · `.../status/detailSearch` |
| IsAuthRequired | `POST /value-adds/v1/isauthrequired` · `GET /value-adds/v1/isauthrequired/{id}` |
| Auth Attachments | `POST /value-adds/v2/attachments` · `GET /value-adds/v2/attachments/{id}` |
| Care Reminders | `POST /pre-claim/eb-value-adds/care-reminders` |
| Member ID Card | `POST /pre-claim/eb-value-adds/member-card` · `GET /pre-claim/eb-value-adds/member-card/{GTID}` |
| Claim Attachments | `POST /medical-attachments/ma/external/rest/response` · `GET /medical-attachments/ma/external/rest/status/{requestId}` |

---

## 12. Coverage & Notes

This reference is now **complete across all six source pages**: the API Guide, every HIPAA Transaction API (Payer List, Configurations, Coverages, Service Reviews 2.0.0, Claim Statuses 1.0.0, and the three Patient Cost Estimator variants), Enhanced Claim Status, the Service Reviews Add-Ons, the E&B Value-Add APIs, and Claim Attachments.

A few deliberate compressions: the PCE `claimInformation` and `serviceLines` objects carry 80+ fields each, most of which map directly to the X12 837 Professional/Institutional TR3 and are grouped here by theme (with all enums/option codes preserved) rather than reproduced as exhaustive one-row-per-field tables. For the authoritative per-field TR3 code lists, consult the 837 TR3 and the Configurations API (`professional-claims`/`institutional-claims`/`claim-statuses-inquiry`), which returns each payer's actual required fields, patterns, and valid values at runtime.

> **Source disclaimer:** Endpoint base paths in the Availity docs are occasionally shown against QA hosts (`qua.api.availity.com`, `qa-apps.availity.com`). Always confirm the correct production host/path and your subscribed scopes before integrating. All content here is derived from Availity's public developer documentation (© Availity, LLC) and reorganized for engineering use.
