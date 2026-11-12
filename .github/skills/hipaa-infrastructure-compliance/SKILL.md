---
name: hipaa-infrastructure-compliance
description: "Review, flag, and safely fix HIPAA-oriented compliance risks in MOW 2.0 Azure infrastructure changes. Use when reviewing Bicep, bicepparam, deployment scripts, pipelines, Azure Policy, networking, identity, storage, logging, audit retention, backup, Defender, Key Vault, or live Azure configuration for PHI safeguards; when asked whether infrastructure changes are HIPAA compliant; or before promoting infrastructure to QA, Training, or Production."
argument-hint: "Optional mode and scope, e.g. mode=review|fix scope=working-tree|branch|path environment=dev,qa live-state=false"
---

# HIPAA Infrastructure Compliance

## Purpose

Review infrastructure changes against the MOW 2.0 healthcare security baseline, report evidence-based risks, apply narrowly scoped fixes when requested, and validate the resulting IaC.

This skill assesses technical safeguards and repository controls. It does not certify HIPAA compliance, replace a legal or privacy review, prove operational procedures are followed, or confirm that a Microsoft Business Associate Agreement is in force.

## Canonical References

Read these repository sources before classifying findings:

1. [MOW 2.0 Azure Infrastructure Design D1](../../../docs/mow2-azure-infrastructure-design-d1.md), especially Sections 1.4, 2.3, 4.2-4.4, 5-10, and 14.
2. [D1 vs Current Bicep Gap Runbook](../../../infrastructure/docs/d1-vs-current-bicep-gap-runbook.md) for accepted gaps, sequencing, rollback, and environment decisions.
3. [Networking](../../../infrastructure/docs/networking.md) for the repository's healthcare network boundary.
4. [Definition of Done](../../../docs/definition-of-done.md) for security, privacy, logging, and review expectations.
5. The `d1-infrastructure-reconciliation` skill when a change alters or closes a D1 architecture gap.
6. The Microsoft `azure-compliance` skill for live-state assessment with Azure Quick Review and Key Vault expiration checks.

For current external requirements, prefer official sources from HHS and Microsoft. Distinguish a regulatory requirement, Microsoft recommendation, D1 design decision, and reviewer recommendation in every finding. Do not present a proposed HIPAA rule as an effective requirement.

## Review Modes

| Mode         | Behavior                                                                                                      |
| ------------ | ------------------------------------------------------------------------------------------------------------- |
| `review`     | Inspect changes and report findings. Do not edit files. This is the default.                                  |
| `fix`        | Review, apply eligible local IaC fixes, and validate them.                                                    |
| `live-state` | Compare deployed Azure configuration with IaC using read-only compliance tools. Never enable this implicitly. |

Live-state review requires the selected subscription, resource group/environment, authenticated Azure access, and read permissions. Repository review alone cannot prove deployed-state compliance.

## Scope Rules

1. Use the user-supplied path, commit range, or branch comparison when provided.
2. Otherwise review all current infrastructure changes, including staged, unstaged, and untracked files under:
   - `infrastructure/**`
   - `azure-pipelines.yml`
   - `pipelines/**`
   - deployment or migration scripts that affect Azure resources or data access
3. Include referenced modules and parameter files needed to understand the changed behavior.
4. If no working-tree changes exist, review the latest infrastructure commit only after stating that fallback.
5. Identify the target environments and whether real PHI is permitted in each. Never assume non-production contains no PHI unless an approved control states that.
6. Ignore unrelated application changes unless they change infrastructure security, telemetry, secrets, network paths, or data handling.

## Evidence Rules

1. A Bicep comment, resource name, or D1 statement is not implementation evidence. Inspect effective properties and dependencies.
2. A successful Bicep build proves syntax and type validity, not HIPAA compliance.
3. IaC evidence does not prove deployed state; use `live-state` evidence when that claim matters.
4. Azure Policy assignment does not prove compliance. Check assignment scope, parameters, exemptions, enforcement mode, and current compliance state.
5. A private endpoint does not prove isolation. Also verify public access, DNS, routing, and authorized callers.
6. Encryption-at-rest defaults do not prove access control, integrity, retention, or audit safeguards.
7. Never include PHI, credentials, tokens, secret values, connection strings, patient identifiers, or raw production logs in findings.
8. Record unknowns as `NEEDS_EVIDENCE`; do not convert missing evidence into a pass or fail.
9. Approved risk acceptance must identify owner, scope, expiration/review date, and compensating controls.

## Control Review Matrix

Review every changed resource against the applicable controls. Mark non-applicable controls with a rationale.

| ID             | Control area                          | Minimum infrastructure checks                                                                                                                                                                                                  |
| -------------- | ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `HIPAA-IAC-01` | Data boundary and service eligibility | US region restrictions; PHI classification/tags; approved Azure service; BAA/service eligibility is confirmed outside IaC; no unapproved data replication or egress.                                                           |
| `HIPAA-IAC-02` | Access control                        | Managed identity; least-privilege RBAC at the narrowest practical scope; no broad Owner/Contributor grants without evidence; environment separation; no shared production identity.                                            |
| `HIPAA-IAC-03` | Authentication and secrets            | Entra authentication where supported; Key Vault references; shared-key/local-auth disabled where feasible; secret/certificate expiration and rotation ownership; workload identity federation for CI/CD.                       |
| `HIPAA-IAC-04` | Network protection                    | Private endpoints for PHI/data services; public network disabled or explicitly constrained; ingress anti-bypass controls; egress restrictions; private DNS; TLS 1.2 or newer; HTTPS only.                                      |
| `HIPAA-IAC-05` | Encryption and integrity              | Encryption at rest and in transit; approved key-management decision; secure transfer required; SQL TDE; storage versioning/soft delete where required; no downgrade or plaintext protocol.                                     |
| `HIPAA-IAC-06` | Audit controls                        | Diagnostic settings for supported resources; centralized per-environment workspace; authentication, authorization, administrative, data-access, and security events captured; correlation enabled; no PHI in operational logs. |
| `HIPAA-IAC-07` | Audit retention and immutability      | Retention matches the approved policy; audit archive is append-only/immutable where required; application writers cannot alter/delete audit evidence; configuration changes are independently logged.                          |
| `HIPAA-IAC-08` | Availability and recovery             | Backup/PITR, soft delete, purge protection, redundancy, restore ownership, RPO/RTO alignment, and tested recovery procedures; destructive retention reductions are flagged.                                                    |
| `HIPAA-IAC-09` | Vulnerability and posture management  | Defender for Cloud plan/coverage decision; vulnerability assessment; secure configuration recommendations; asset inventory; alert routing and incident-response ownership.                                                     |
| `HIPAA-IAC-10` | Policy and governance                 | HIPAA/HITRUST-aligned initiatives where approved; allowed regions; required tags; diagnostic/private-access/TLS policies; exemptions and `DoNotEnforce` assignments reviewed.                                                  |
| `HIPAA-IAC-11` | Monitoring and incident response      | Action groups, actionable severity, dead-letter/integration/audit pipeline alerts, security signal routing, diagnostic health, and evidence that alerts have an owner.                                                         |
| `HIPAA-IAC-12` | Delivery controls                     | PR review, infrastructure path triggers, Bicep build/lint, security scan, what-if, environment approvals, deployment audit trail, rollback, and no long-lived pipeline secret.                                                 |
| `HIPAA-IAC-13` | Tenant and environment isolation      | No cross-environment resource IDs or identities; production subscription/RG boundary; tenant data isolation; no accidental non-prod access to production data; training refresh requires PHI scrubbing controls.               |

## Finding Model

### Severity

| Severity   | Meaning                                                                                                                                                              |
| ---------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `CRITICAL` | Likely public PHI exposure, uncontrolled destructive change, credential disclosure, or direct bypass of a required security boundary. Block deployment.              |
| `HIGH`     | Missing required access, audit, encryption, isolation, backup, or authentication safeguard with material PHI risk. Block promotion until fixed or formally accepted. |
| `MEDIUM`   | Defense-in-depth, governance, monitoring, or recoverability weakness that should be resolved before Production.                                                      |
| `LOW`      | Limited-risk hardening, maintainability, or evidence-quality issue.                                                                                                  |

### Status

Use exactly one status per control:

- `PASS`: Direct repository evidence satisfies the scoped control.
- `FAIL`: Direct evidence shows the control is violated or removed.
- `PARTIAL`: Some required safeguards exist, but the control is incomplete.
- `NEEDS_EVIDENCE`: The result depends on live state, policy, legal agreement, operational procedure, or missing context.
- `NOT_APPLICABLE`: The control does not apply, with rationale.
- `ACCEPTED_RISK`: An approved, bounded, time-limited exception exists.

## Workflow

### 1. Establish the change set

1. Record the comparison base, target environment, review mode, and whether live state is included.
2. Inventory added, modified, deleted, and renamed infrastructure files.
3. Summarize resource creates, updates, deletes, replacements, role assignments, policy changes, and network-path changes.
4. Treat resource deletion, replacement, public-access enablement, retention reduction, diagnostic removal, RBAC expansion, and security-policy exemptions as high-scrutiny changes.

### 2. Trace effective configuration

1. Follow module parameters from environment `.bicepparam` files through entry points to resource properties.
2. Inspect outputs and dependent modules for cross-environment or stale references.
3. Review conditional resources and enforcement flags in both enabled and disabled states.
4. Check deployment scripts and pipelines for overrides that weaken the Bicep definition.
5. Compare affected controls with the D1 baseline and approved runbook decisions.

### 3. Classify and report

For each material finding provide:

```markdown
### [SEVERITY] Short title

- Control: `HIPAA-IAC-XX`
- Status: `FAIL|PARTIAL|NEEDS_EVIDENCE`
- Evidence: repository file and line, changed property, and effective environment
- Risk: concrete effect on PHI confidentiality, integrity, availability, or auditability
- Requirement source: HIPAA safeguard, Microsoft guidance, D1 section, or repository policy
- Remediation: smallest safe change
- Validation: command or live-state check that proves the remediation
```

Order findings by severity. Do not bury blockers in a general checklist. If no findings exist, state that no change-scoped technical issues were found and list residual evidence gaps; never say the system is "HIPAA certified" or fully compliant.

### 4. Apply fixes in `fix` mode

Eligible automatic fixes are deterministic, local, and preserve intended architecture, for example:

1. Restore required TLS/HTTPS properties.
2. Disable unintended public or shared-key access after confirming private connectivity and identity dependencies exist.
3. Add missing diagnostics using an established repository module/pattern and approved workspace output.
4. Narrow an accidentally broadened role assignment when the intended principal, role, and scope are unambiguous.
5. Restore soft delete, purge protection, versioning, retention, or pipeline validation removed by the current change when approved values already exist.
6. Remove hardcoded secrets and use the repository's Key Vault/managed-identity pattern.

Do not automatically:

1. Deploy, change live Azure resources, or run a write-mode compliance tool.
2. Modify Production or shared networking without explicit approval.
3. Invent retention periods, legal interpretations, data classifications, RPO/RTO, regions, or BAA status.
4. Create broad policy assignments, Defender billing changes, role grants, deny rules, private endpoints, DNS changes, or resource replacements without impact review and user approval.
5. Delete resources, data, exemptions, break-glass access, backups, logs, or audit evidence.
6. Mark a finding resolved until validation proves the changed behavior.

After the first fix, run the cheapest focused validation before making another fix. Preserve unrelated user changes.

### 5. Validate IaC

Use repository-native commands where available. At minimum:

1. Build/lint every changed Bicep entry point and affected module.
2. Build affected `.bicepparam` files for each scoped environment.
3. Run focused tests or policy checks available in the repository.
4. Review Azure what-if for each deployable environment when authenticated and authorized.
5. Reject unexpected deletes/replacements, public exposure, RBAC expansion, diagnostic removal, retention reduction, and cross-environment references.
6. Confirm pipeline path filters include all changed infrastructure paths.
7. Rerun the control matrix for changed controls after fixes.

What-if is read-only but may expose resource metadata; do not paste sensitive outputs into reports.

### 6. Validate live state when requested

1. Invoke the Microsoft `azure-compliance` workflow for Azure Quick Review and Key Vault expiration checks.
2. Confirm the selected tenant, subscription, resource group, and environment before querying.
3. Use read-only resource and policy queries only.
4. Compare deployed properties, policy compliance, Defender coverage, diagnostics, RBAC, network posture, and Key Vault expiration with IaC.
5. Record drift separately from code defects.
6. If authentication or authorization fails, report the unverified controls and required read access; do not downgrade them to `PASS`.

## Required Output

Return:

1. Scope, comparison base, environments, mode, and evidence sources.
2. Deployment recommendation: `BLOCK`, `CONDITIONAL`, or `PROCEED`.
3. Findings ordered by severity with file/line evidence.
4. Control summary counts by status.
5. Fixes applied, declined, or requiring approval.
6. Validation commands and outcomes.
7. Live-state drift and evidence gaps.
8. Residual risk, accepted-risk references, and required owners.

## Completion Gate

Do not recommend `PROCEED` unless:

1. No unresolved `CRITICAL` or `HIGH` finding remains.
2. Every applicable control has a status and evidence.
3. All applied fixes pass focused Bicep/parameter validation.
4. What-if has no unexplained destructive or exposure-increasing change when what-if is available.
5. Live-state-dependent controls are clearly labeled if live state was not reviewed.
6. D1/runbook documentation is updated when the approved architecture or gap status changed.
7. The report avoids compliance certification language and identifies required human approval.
