# MOW 2.0 — Complete System Architecture

> Source: [Miro Board](https://miro.com/app/board/uXjVHXJOe1Q=/?focusWidget=3458764670776145327)

---

## Executive Summary

MOW 2.0 is a comprehensive enterprise modernization of the VNA (Visiting Nurse Association) Meals on Wheels platform. It replaces legacy systems (Netsmart, RoadNet, custom applications) with an Azure-based cloud-native SaaS product following event-driven architecture patterns.

---

## Platform Architecture (Three-Tier)

### Client Tier
- **MOW 2.0 Web Client** — React SPA with WebTWAIN scanning integration
- **MOW Mobile App** — React Native/Expo for drivers/volunteers (existing, retained)
- **Client & Family Portal** — Mobile-responsive web portal for clients/families
- **Authentication** — Azure Entra External ID (OAuth)

### Service Tier
- **MOW 2.0 API/Services** — Core backend with 6 functional modules
- **Azure API Management** — API gateway with CDM-enabled RESTful services
- **Azure Function Apps** — Event-driven serverless processing (4+ function apps)
- **Azure Service Bus** — Messaging backbone for async/event-driven communication
- **Azure Load Balancer** — Traffic distribution
- **Auto-scaling** — Azure App Service Plans with auto-scaling

### Data Tier
- **Azure SQL** — Multi-tenant database architecture with elastic pooling
- **Azure Blob Storage** — Document management
- **Azure Redis Cache** — Caching layer
- **M365 Business Central** — GL/accounting integration (via SFTP export)

### Cross-Cutting Concerns
- **Monitoring**: Azure Monitor, Application Insights, Service Health
- **Security**: Azure Key Vault, App Configuration, Entra ID managed identities
- **Logging**: Structured logging with correlation IDs → Application Insights
- **Compliance**: HIPAA, HITECH, PCI DSS, TCPA

---

## Azure Cloud Infrastructure

| Component | Service |
|-----------|---------|
| Identity | Azure Entra ID / External ID |
| API Gateway | Azure API Management |
| Web App | Azure App Service (auto-scale) |
| Serverless | Azure Function Apps |
| Messaging | Azure Service Bus |
| Database | Azure SQL (multi-tenant, elastic pool) |
| Storage | Azure Blob Storage |
| Caching | Azure Redis Cache |
| Secrets | Azure Key Vault |
| Config | Azure App Configuration |
| Maps | Azure Maps |
| Comms | Azure Communication Services |
| Monitoring | Azure Monitor + Application Insights |
| Networking | 3 VNets: Application, Service, Data |

---

## Core Functional Modules

### 1. Referrals & Intake
**Purpose**: Front door of client lifecycle

**Referral Input Channels** (5):
1. Batch Import
2. REST API (FHIR R4)
3. File Upload with Manual Validation
4. Phone/Email/Fax Manual Entry
5. Web Portal

**Status Progression**: Received → In Review → Pending Assessment → Eligible → Waitlisted/Enrolled

**Key Integrations**:
- Availity 270/271 — Eligibility verification
- DocuSign — Consent management
- Azure Maps — Address validation & geocoding
- BOP API — Route/depot assignment
- Twilio SMS / SendGrid Email — Client notifications

**Decision Gates**: Duplicate detection, QA validation, eligibility check, capacity assessment, consent signing, data conflict resolution

**Operational Scale**: Max 10 funding sources, 21 depots, ~532 total routes, ~315 active daily, 10-30 stops/route

---

### 2. Clinical & Assessments
**Purpose**: Health assessments, clinical workflows, client status lifecycle

**Client Status Lifecycle** (7 states):
- Referral → Intake Pending → Active → Hospitalized → On Hold → Deceased → Discharged

**Workflow**:
1. Client record creation & referral acceptance
2. Intake (demographics + clinical data)
3. Assessment scheduling, completion, validation
4. Supervisor approval for high-risk findings
5. Service plan generation & staff assignment
6. Active service delivery
7. Status change management (hospitalization, hold, death, discharge)

**Key Features**:
- Automated validation preventing incomplete intakes
- Supervisor approval for high-risk assessments
- Audit trails for all clinical documentation changes
- Status-triggered downstream actions (pause billing, lock records, route changes)
- Survey.js for assessment forms

**Key Actors**: Intake Coordinator, Clinical Supervisor, Clinical Staff, Assessment Admin, Case Manager

---

### 3. Scheduling & Routing
**Purpose**: Meal scheduling, route optimization, delivery execution

**Schedule Generation**:
- Nightly batch process reads service plan frequency rules
- Generates meal records with 30-day rolling buffer
- Prerequisites: Client Active + Service Plan Approved + Authorization Valid

**Route Optimization**:
- Azure Maps API for distance matrix
- Google OR Tools (Traveling Salesman algorithm)
- Groups meals by route/depot
- Outputs delivery manifests
- Same-day re-optimization capability

**Delivery Execution**:
- BOP + Mobile App for real-time tracking
- Driver pickup → stop-by-stop delivery → confirmation
- Delivery hours: 8:30 AM – 5:00 PM

**Time & Attendance**:
- Volunteer/staff time entry
- Supervisor approval workflow
- Payroll export to PrismHR via SFTP

**Auto-Verification**:
- Post-delivery validation (delivery confirmed + authorization valid)
- Pass → ready to bill; Fail → exception queue for manual review

**Key Actors**: Routing Coordinator, Driver, Volunteer, Supervisor, Billing Staff

---

### 4. Client & Family Services
**Purpose**: Client/family portal, communications, additional services

**Portal Features**:
- Account provisioning (VNA staff gatekeeping)
- Write-back actions: Update delivery preferences, Update contact info
- Read-only views: Upcoming deliveries (date, ETA, route), Meal menu, Documents (consent forms, program info)
- Messaging capability

**Additional Features**:
- Pet tracking
- Tele-visits
- SMS messaging (Twilio)
- Outreach templates
- Referral app

**Backend Integration**: MOW 2.0 ↔ BOP ↔ Scheduling module

---

### 5. Financial & Billing
**Purpose**: Complete revenue cycle management

#### Invoicing Calculations
**Trigger**: Nightly scheduled job or on-demand manual run

**Workflow**:
1. Query billable meals (auto-verified status)
2. Payer identification — hierarchical service plan matching (Primary → Secondary → Tertiary)
3. Authorization verification (active, not expired, units available)
4. Rate lookup hierarchy: Client-specific → Contract → Payer → Standard fallback
5. Date range validation (rates active on service date)
6. Copay & adjustment rules (county/state/waiver discounts)
7. Line item calculation (client ID, service codes, units, amounts, responsibility)
8. Authorization consumption tracking (deduct used units)
9. Aggregate by Claim + Payer + Billing Period

**Outputs**: Invoice line items, billable claims, copay records, exception lists

#### TMHP Billing (Texas Medicaid)
- 837P Professional claims generation
- Multi-payer routing: TMHP, Medicare (CMS/NCO), Private Payers (UHC, Aetna, BCBS)
- Clearinghouse: Availity
- Acknowledgment: 997/999
- Remittance: 835

#### Revenue Cycle Management
**Flow**: Delivery Confirmation → Pre-Billing Audit → 837P Generation → Availity Submission → Payer Response

**Payment Processing**:
- Paid claims → auto-posting
- Denied/adjusted → escalation or resubmission (frequency codes: 7=replace, 8=void)
- Uncollectable → write-off (approval required above threshold)

**Collections**: AR tracking at 30/60/90/120 day intervals with automated staff alerts

**GL Integration**: Nightly export to M365 Business Central via SFTP

---

### 6. Reporting & Analytics
- Reporting/Dashboards
- Custom Report Builder (Power BI)
- Audit Logs

---

## Integration Architecture

### Pattern: Hub-and-Spoke
- Centralized MOW API as integration hub
- Adapter pattern for each external system
- Event-driven messaging via Azure Service Bus
- APIM-registered CDM-enabled RESTful APIs

### External System Integrations

| System | Purpose | Pattern |
|--------|---------|---------|
| Bamboo Health | Patient data, referral submission | REST API, HL7 FHIR, events |
| Availity | Eligibility (270/271), Claims (837/835/999) | EDI clearinghouse |
| Stripe | Payment processing | REST API |
| Twilio | SMS notifications | REST API |
| Azure Communication Services | Email, video calls | SDK |
| DocuSign | Consent management | REST API |
| Azure Maps | Address validation, geocoding, distance matrix | REST API |
| Google OR Tools | Route optimization (TSP) | Library |
| BOP (Back Office Portal) | Routing, delivery, driver mgmt | REST API (ASP.NET Core 2.1) |
| Trio Kitchen | Meal file push | SFTP |
| PrismHR / Engage PEO | Payroll export | SFTP |
| M365 Business Central | GL/accounting | SFTP file transfer |
| Salesforce VP | Volunteer management | OAuth, REST API |
| Sterling Volunteers | Background checks | REST API |
| Survey.js | Assessment forms | Library |
| Netsmart | Legacy EHR (retiring) | Migration |
| RoadNet | Legacy routing (retiring) | Migration |
| Alldocs | Legacy docs (migrating to Blob) | Migration |

### Integration Adapters (Custom Development Required)
- Bamboo Health Adapter
- Netsmart Adapter (address validation events)
- Roadnet Adapter
- Crystal Reports Adapter
- Salesforce VP Adapter
- Sterling Volunteers Adapter
- Drop Site App Adapter
- Engage PEO Adapter
- Alldocs Adapter
- Push Notification Adapter

---

## User Roles (RBAC Matrix)

**9 Official Internal Roles**:
1. Routing Supervisor
2. Routing Coordinator
3. Intake Supervisor
4. Intake Cordinator
5. Community Health Worker
6. Community Health Team Supervisor
7. Billing Supervisor
8. Billing Coordinator
9. System Admin

**Permission Levels**: ✅ Full CRUD | 👁️ Read Only | ❌ No Access | ⚙️ Admin/Config Only

**18 System Modules/Capabilities**:
User Management, Role & Permission Config, Referrals & Intake, Client Records, Standard Assessments, Custom Assessments, Service Plans, Scheduling & Routing, Meal Delivery (Mobile), Client Messaging (SMS), Billing & Revenue Cycle, Financial Exports (GL), Reporting/Dashboards, Custom Report Builder (Power BI), Audit Logs, System Configuration, Integration Monitoring, Time Entry

---

## Data Domains

| Domain | Sub-domains |
|--------|-------------|
| Clients | Addresses, Demographics, Clinical Assessments, Care Plans |
| Eligibility | Eligibility Management |
| Financial | Billing Management, Authorization Tracking |
| Sites | Site Management, Meal Congregation |
| Drivers | Stats, Background Check, Assignments |
| Dispatch | Site Management, Meal Congregation |
| Communication | Push Notifications, Preferences, Filters |

---

## Phasing

| Phase | Scope |
|-------|-------|
| Phase 1a | Mandatory features — MVP |
| Phase 1b | Preferred / Highly Preferred features |
| Phase 2 | Deferred features (AI-assisted, context-based assessments) |

---

## Key Operational Data Flows

- **Patient data** flows from Bamboo Health → MOW 2.0
- **Client data sync** occurs at 4am & 5am
- **Driver routes/status** updates: 8:30 AM – 5:00 PM
- **Nightly billing jobs** for invoice calculation
- **Nightly GL export** to Business Central via SFTP
- **Payroll export** to PrismHR via SFTP

---

## Legacy Systems Being Replaced/Retired

| System | Disposition |
|--------|-------------|
| Netsmart | EHR — Retiring |
| RoadNet | Routing — Retiring |
| BOP | Back Office Portal — Enhancing (ASP.NET Core 2.1, end-of-life) |
| Alldocs | Document mgmt — Migrating to Azure Blob |
| Crystal Reports | Reporting — Replacing with Power BI |
| Custom applications | Various — Consolidating into MOW 2.0 |

---

## Open Discovery Items

- Trio Kitchen integration specifics
- Engage PEO payroll format specification
- M365 Business Central GL export format
- VoIP platform selection (8x8 vs Teams)
- Assessment form templates
- Document volume for Alldocs migration
- BOP data contract for RoadNet migration
- CEHRT requirement (pending legal confirmation)
