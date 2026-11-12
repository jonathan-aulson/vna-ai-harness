---
name: d1-infrastructure-reconciliation
description: "Reconcile MOW 2.0 D1 architecture requirements with current Azure Bicep/IaC. Use when comparing D1 to infrastructure, finding missing Azure resources or controls, updating a D1 gap runbook, planning DEV/QA non-prod completion, preparing reusable PROD parameters, naming Azure Functions by purpose, or validating that IaC changes close documented architecture gaps."
argument-hint: "Optional environment scope or updated D1 source, e.g. scope=dev,qa"
---

# D1 Infrastructure Reconciliation

## Purpose

Convert D1 architecture definitions into an evidence-based, implementation-ready reconciliation against the repository's Azure Bicep state.

The workflow must distinguish:

- What D1 explicitly requires.
- What current IaC implements.
- What is missing or partial.
- What is deferred by environment.
- What is ambiguous or contradictory and needs an architecture decision.

## Canonical Repository References

Use these repository documents by default:

1. [MOW 2.0 Azure Infrastructure Design D1](../../../docs/mow2-azure-infrastructure-design-d1.md) is the canonical architecture source. Its section headings are the citation anchors for D1 requirements.
2. [D1 vs Current Bicep Gap Runbook](../../../infrastructure/docs/d1-vs-current-bicep-gap-runbook.md) is the canonical reconciliation record, technical baseline, implementation plan, naming proposal, and PROD-readiness plan.

Do not recreate the detailed D1 service inventory, SKU matrix, network topology, naming tables, or implementation phases inside this skill. Read the relevant sections from these documents when the workflow needs them.

If the user supplies a newer approved D1 source:

1. Compare its version and date with the Markdown baseline.
2. Update the canonical D1 Markdown and `docs/assets/d1/` as needed before reconciling IaC.
3. Preserve requirements that are unchanged and identify substantive source changes in the runbook.
4. Do not use an external DOCX or extracted text as a long-term parallel source.

## When to Use

Use this skill for requests such as:

- Compare D1 with current Bicep.
- Find missing infrastructure such as Front Door, Functions, queues, SignalR, private endpoints, or governance controls.
- Update the D1 gap runbook.
- Make DEV/QA complete before Production.
- Prepare modules and parameter files for later PROD promotion.
- Propose Azure resource names using the MOW 2.0 convention.
- Validate whether implemented Bicep closes D1 gaps.

Do not use this skill for:

- Deploying Azure resources without an approved reconciliation plan.
- Treating an architecture diagram as the sole source of truth when it conflicts with D1 text.
- Production deployment execution; use the appropriate Azure deployment workflow after validation and approval.

## Inputs

Required:

1. The canonical D1 Markdown linked above.
2. The canonical gap runbook linked above.
3. Current Bicep entry points, modules, and environment parameter files.

Optional:

1. A newer approved D1 source used to refresh the canonical Markdown.
2. Architecture diagrams not already represented in the D1 Markdown.
3. Live-state evidence.
4. Environment scope: DEV, QA, Training, PROD, or all.
5. A requested supplemental output; the canonical runbook must still be updated.

## Evidence Rules

1. Never infer that a resource exists because D1 names it; verify it in IaC or live state.
2. Never infer that IaC satisfies D1 from a comment alone; inspect the actual resource properties.
3. Cite repository files and D1 section names for every material finding.
4. Separate explicit D1 requirements from proposed implementation details.
5. Mark diagram-only capabilities as `DIAGRAM_ONLY` until reconciled with D1 text.
6. Mark contradictory D1 values as `CONFLICTING`; do not silently select one.
7. Keep Production-only requirements out of the active DEV/QA implementation scope while preserving reusable module contracts.
8. Do not expose secrets, credentials, PHI, tokens, or private connection strings in the runbook.

## Status Model

Classify every requirement with exactly one status:

| Status           | Meaning                                                                                |
| ---------------- | -------------------------------------------------------------------------------------- |
| `IMPLEMENTED`    | IaC fully represents the requirement with direct evidence.                             |
| `PARTIAL`        | Some resources or controls exist, but required properties or dependencies are missing. |
| `MISSING`        | No implementation was found in IaC.                                                    |
| `DEFERRED`       | Explicitly outside the current environment or delivery phase.                          |
| `CONFLICTING`    | D1 sections or supporting diagrams disagree.                                           |
| `DIAGRAM_ONLY`   | Present in a supplied diagram but not clearly defined in D1 text.                      |
| `NOT_APPLICABLE` | Not applicable to the selected environment, with rationale.                            |

## Workflow

### 1. Load the canonical baseline

1. Read the D1 Markdown metadata, relevant architecture sections, and open-items log.
2. Read the runbook executive summary, gap matrix, technical baseline, decision register, implementation packages, naming, and parameterization sections relevant to the request.
3. Treat the D1 Markdown as desired-state evidence and the runbook as a maintained reconciliation record, not as proof of current IaC.
4. If either canonical document is missing, stop and report the missing repository dependency rather than silently rebuilding it elsewhere.

### 2. Establish scope

1. Record the D1 version/date and selected environments.
2. Default to DEV/QA non-prod scope when the current work is explicitly non-prod first.
3. Record Production requirements separately as a prepared future baseline.

### 3. Extract the D1 capability inventory

Use the D1 Markdown sections for architecture overview, compute, storage, networking, identity, security, monitoring, resilience, environments/DevOps, APIs, and open items. Capture at minimum:

1. Environment topology and subscriptions.
2. Compute and hosting SKUs.
3. Function workloads and trigger models.
4. Messaging entities and delivery semantics.
5. Storage-account separation and retention controls.
6. Ingress, API gateway, real-time and private-connectivity paths.
7. Identity, RBAC, secrets and public-network posture.
8. Monitoring, diagnostics, alerting and governance.
9. Resilience and Production-only requirements.

For diagrams, inventory nodes and edges separately, then reconcile them against D1 text.

### 4. Inventory current IaC

Start with the owning entry points:

- `infrastructure/bicep/main.bicep`
- `infrastructure/bicep/network.bicep`
- `infrastructure/bicep/modules/`
- `infrastructure/bicep/environments/`
- Infrastructure deployment scripts and pipeline path filters.

For each capability, inspect actual properties including:

1. SKU and capacity.
2. Public network access.
3. VNet integration, subnet and private endpoint/DNS resources.
4. Managed identity and RBAC assignments.
5. Diagnostics and alert bindings.
6. Environment hardcodes and module outputs.
7. Feature flags and rollback behavior.

### 5. Update the gap matrix

Update the matrix in the canonical runbook. Preserve valid existing evidence and decisions; replace stale claims when current D1 or IaC evidence disproves them.

Use this row format:

```markdown
| Capability | Environment | D1 requirement | Current IaC evidence | Status | Gap/decision | Priority |
```

Priority guidance:

- `P0`: bypass, exposure, authentication, or active-path security risk.
- `P1`: required platform capability missing or incomplete.
- `P2`: governance, promotion readiness, or later-environment alignment.

### 6. Reconcile ambiguities

Update the runbook decision register for conflicts such as:

1. Different SKUs in separate D1 sections.
2. Diagram workloads absent from textual service maps.
3. Logical workload count vs deployable Function App count.
4. Non-prod shared service vs per-environment ownership.

Each decision must include:

- Conflicting evidence.
- Temporary baseline.
- Owner/approver.
- Impact if changed later.

### 7. Maintain implementation packages

Maintain independently deployable packages in the runbook. Use its existing package order unless current evidence requires a documented change:

1. Passive ingress resources without lock-down.
2. Network subnets and private DNS.
3. Document, messaging and Durable storage.
4. Function hosting and workloads.
5. SignalR and real-time path.
6. Ingress cutover and strict anti-bypass enforcement.
7. Governance, alerts and CI gates.

For every package include:

1. Files/modules to add or update.
2. Parameters and outputs.
3. DEV deployment and validation.
4. QA promotion using the same module artifact.
5. Go/No-Go criteria.
6. Rollback sequence.
7. Evidence to capture.

### 8. Enforce promotable IaC

New implementation guidance must require:

1. Shared modules for DEV/QA/PROD; no environment-specific template forks.
2. Environment differences in `.bicepparam` files.
3. No `environment == 'prod'` policy logic inside modules when a parameter can express the decision.
4. No hardcoded subscription IDs, resource group names, principal IDs, CIDRs, tenant IDs, or resource IDs.
5. Capability-specific creation and enforcement flags.
6. Stable outputs for dependent modules.
7. Prepared PROD parameter files that compile but remain undeployed and disabled until approval.
8. A separate shared-non-prod entry point for shared Front Door or other shared resources.
9. A separate PROD network contract when INT-only VPN/BOP/transition resources must not be inherited.

### 9. Apply naming conventions

Use the naming rules and proposed resource inventory in the canonical runbook. Add or revise names there when the approved workload boundary changes.

Use:

`vna-mow2-<resource-type>-<environment>-<purpose>`

Requirements:

1. Keep Function App purpose visible, for example `vna-mow2-func-dev-bamboo-sender`.
2. Use PascalCase for .NET trigger/orchestrator names, for example `BambooApiSender` and `BillingBatchOrchestrator`.
3. Use compact lowercase alphanumeric names for storage accounts.
4. Add deterministic uniqueness suffixes only where Azure requires them, preserving purpose before the suffix.
5. Avoid generic ordinal names such as `func-dev-01`.

### 10. Validate

At minimum:

1. Build all Bicep entry points.
2. Validate DEV and QA parameter files.
3. Run and review DEV/QA what-if.
4. Compile the prepared PROD parameter contract without deploying it.
5. Check that pipeline path filters include `infrastructure/**`.
6. Reject unrelated replacements, deletions, public-network enablement, or cross-environment references.
7. Validate Markdown links from this skill to both canonical documents.
8. Validate the canonical D1 Markdown and runbook after edits.

## Required Output

Update [D1 vs Current Bicep Gap Runbook](../../../infrastructure/docs/d1-vs-current-bicep-gap-runbook.md) as the primary output. Do not create a competing gap analysis unless the user explicitly requests a separate artifact.

The maintained runbook must contain:

1. Scope and source baseline.
2. Executive summary with counts by status and priority.
3. D1 vs IaC gap matrix.
4. Non-prod technical baseline.
5. Function/queue/SignalR/storage inventory.
6. Proposed resource names by purpose.
7. Ambiguity/decision register.
8. Ordered implementation packages.
9. Parameterization and PROD-readiness rules.
10. Validation, Go/No-Go and rollback steps.
11. Residual risks and explicitly deferred Production work.

## Completion Gate

Do not call reconciliation complete unless:

1. Every extracted D1 capability has a status.
2. Every `MISSING` or `PARTIAL` item maps to a package or approved deferral.
3. Every conflict maps to a decision record.
4. DEV and QA use the same proposed module contracts.
5. Production-only requirements are separated but compile against the reusable design.
6. Claims are backed by D1 and repository evidence.
