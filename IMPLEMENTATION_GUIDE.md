EgyptLab LIS — Phased Implementation Guide

> **Authority note:** `ROADMAP.md` is the authoritative phase plan. This document remains a detailed execution reference. Its 11-phase breakdown does not match the 6-phase roadmap; the difference is recorded for owner review and must not be resolved by assumption.

Document type: Development execution guide
Version: 1.1
Date: 17 November 2026
Audience: Project owner, development team, and Claude Code
Development environment: Linux
Target deployment: Windows local server

────────

1. Start Here — Essential Project Rules

This is the execution roadmap for building EgyptLab LIS. It is not evidence that any feature has already been implemented. A feature is complete only after its implementation, tests, documentation, and acceptance checks have been reviewed.

The most important points

1. Build in controlled phases. Complete and verify one phase before starting the next. Do not ask an AI coding agent to build the entire system in one prompt.
2. Reliability comes first. Patient identity, specimen traceability, result integrity, audit history, access control, backups, and recovery take priority over visual polish or advanced features.
3. Protect medical results. A released result must never be silently overwritten. Corrections must create a traceable amended version, including the reason, user, and timestamp.
4. Never invent clinical rules. AI must not make up reference ranges, critical limits, units, formulas, diagnostic interpretations, or analyzer mappings. Qualified laboratory professionals must provide and approve them.
5. Do not use real patient data during development. Use synthetic or properly anonymized test data.
6. Never use “last write wins” for sensitive records. This applies to medical results, approvals, invoices, payments, and other important records.
7. Test Windows deployment early. Development may happen on Linux, but the target is a Windows machine in the laboratory. Validate the deployment approach before the project becomes large.
8. Treat offline synchronization as a separate engineering project. Define branch topology, offline permissions, conflict rules, retries, and recovery before implementing synchronization.
9. Backups are not proven until restored. Define backup frequency, retention, encryption, RPO/RTO, and perform documented restore tests.
10. Review AI-generated code. Claude Code can implement tasks, but it must not decide clinical policy or bypass acceptance checks.

Definition of done for every phase

A phase is complete only when:

• The requested functionality works in the intended workflow.
• Automated tests pass, including relevant failure and permission cases.
• Database migrations work on both a clean installation and an existing test database.
• Security and data-integrity checks have been reviewed.
• No unresolved critical or high-severity defect remains.
• User-facing and technical documentation is updated.
• The acceptance checklist is complete and the project owner approves moving forward.

────────

2. Product Goal and Proposed Technology

EgyptLab LIS is intended to be a commercial Laboratory Information System for medical laboratories in Egypt. The initial product should support dependable daily laboratory operations, multiple branches, and deployment on a local Windows server. Optional cloud services and cross-site synchronization should be introduced only after the local core is stable.

|Layer         |Proposed technology                          |Responsibility                                             |
|--------------|---------------------------------------------|-----------------------------------------------------------|
|Backend       |Python, Django, Django REST Framework        |Business rules, APIs, permissions, audit trail             |
|Frontend      |React, TypeScript                            |Reception, laboratory, management, and reporting interfaces|
|Database      |PostgreSQL                                   |Transactional storage and integrity constraints            |
|API           |REST/JSON                                    |Versioned and documented communication                     |
|Development   |Linux + Claude Code                          |Controlled development workflow                            |
|Deployment    |Windows local server                         |Must be validated with a working prototype                 |
|Later services|Background worker, message broker, cloud sync|Add only when justified                                    |

Do not finalize package versions or deployment tools until a small technical prototype has been tested on Linux and on the intended Windows environment.

> A competing, unaccepted proposal is recorded in `docs/architecture/decisions/0002-stack.md`. It must be reviewed by the owner before the table above is treated as the selected stack.

────────

3. Architecture Decisions to Confirm Before Building

Record each major decision in an Architecture Decision Record (ADR), including the context, options, selected approach, reasons, risks, and date.

3.1 Branch and server topology

Initial working recommendation: one central local server at each laboratory site, with client computers connecting over the local network (LAN).

Confirm:

• Are all branches connected to one reliable network?
• Can a branch continue operating if its connection to the main site is lost?
• Must a disconnected branch register patients, collect specimens, enter results, review results, or release reports?
• Is there one database per site, or one central database for all branches?
• How will records created independently at different branches receive unique identifiers?

Do not assume that “multiple branches” automatically means “multiple independent servers.”

3.2 Define offline operation

Distinguish between:

• Internet outage: the local server and LAN remain available. Local work should normally continue.
• Inter-site network outage: a branch cannot reach another site or a central server. Permitted workflows must be explicitly defined.

For each workflow, specify whether it is available offline: patient registration, order creation, specimen collection, result entry, review, approval, report release, payments, and refunds.

3.3 Synchronization safety

If independent sites can write data while disconnected, design synchronization deliberately. Consider:

• Transactional outbox/inbox pattern.
• Stable globally unique identifiers.
• Idempotency keys to prevent duplicate processing.
• Record versioning and optimistic concurrency checks.
• Retry handling and failed-message review.
• Explicit conflict detection and human resolution for sensitive records.
• Immutable audit events for important actions.

Never silently merge conflicting medical results or financial transactions.

3.4 Tenant and branch isolation

Define whether one installation/database can host multiple independent laboratory businesses (tenants). If so, every relevant record and query must enforce tenant isolation. Branch-level permissions must be enforced by the backend, not merely hidden in the interface.

Do not begin offline synchronization until the topology and offline workflow decisions are approved.

────────

4. Phased Implementation Roadmap

Complete each phase and pass its acceptance gate before moving on.

Phase 0 — Environment Audit and Scope Confirmation

Goal: Understand the development server and confirm the foundations.

Tasks:

• Record Linux distribution/version, kernel, CPU, RAM, disk space, and network details.
• Check Python, Node.js, package managers, Git, and PostgreSQL versions.
• Check available disk and backup storage.
• Confirm how the Windows target machine will be accessed and maintained.
• Confirm branch topology and offline requirements.
• Create the initial architecture decision log.
• Identify laboratory professionals who will approve test catalogues and clinical configuration.

Deliverables: Environment audit, initial approved architecture decisions, scope/exclusions, development and deployment constraints.

Acceptance gate: The environment is understood, major architecture questions are recorded, and deployment assumptions have been verified or clearly marked as unresolved.

Phase 1 — Repository and Application Foundation

Goal: Create a maintainable project structure and repeatable development workflow.

Tasks:

• Create the Git repository and folder structure.
• Add CLAUDE.md with permanent project rules.
• Configure backend and frontend environments.
• Use environment variables and safe secret handling.
• Configure PostgreSQL for development.
• Add health-check endpoints and structured logging.
• Add formatting, linting, static checks, and automated test commands.
• Document local startup.
• Establish database migration conventions.

Deliverables: Working backend/frontend skeleton, database connection and first migration, quality-check commands, setup documentation, and .env.example with placeholders only.

Acceptance gate: A new developer can run the project by following the documentation; checks run consistently; no secrets are committed.

Phase 2 — Tenant, Branch, Users, Roles, and Audit Trail

Goal: Establish security and ownership before adding patient data.

Tasks:

• Implement laboratory tenant and branch entities according to the approved architecture.
• Implement users, roles, and backend-enforced permissions.
• Define tenant- and branch-scoped access rules.
• Add secure authentication and password policies.
• Add account activation, deactivation, and password reset.
• Record important actions in an audit trail.
• Test attempts to access another tenant’s or branch’s data.

Deliverables: Tenant/branch management, user and role management, permission matrix, audit events, security tests.

Acceptance gate: Unauthorized access is rejected by the backend; tenant and branch isolation tests pass; important actions are traceable.

Phase 3 — Patients and Test Catalogue

Goal: Build reliable patient identity and controlled test configuration.

Tasks:

• Implement patient registration and search.
• Define duplicate-patient detection and safe merge procedures.
• Store only necessary patient information.
• Implement test categories and test definitions.
• Define specimen types, units, methods, and supported result formats.
• Support age- and sex-dependent reference-range configuration where clinically required.
• Add configuration approval and change history.
• Require qualified laboratory staff to provide and approve clinical values.

Deliverables: Patient workflows, test catalogue management, controlled reference-range configuration, and catalogue change history.

Acceptance gate: Patient search and duplicate handling are tested; unauthorized users cannot change test definitions; clinical configuration contains no invented values.

Phase 4 — Orders, Reception, Specimens, and Barcodes

Goal: Track every order and specimen through laboratory processing.

Tasks:

• Implement test orders and order items.
• Implement reception workflow and order status transitions.
• Generate unique order and specimen identifiers.
• Implement barcode generation and printing integration.
• Record specimen type, collection time, collector, receipt time, and status changes.
• Support rejection, recollection, and cancellation with documented reasons.
• Prevent invalid or unauthorized status transitions.
• Provide reception and laboratory worklists.

Deliverables: Reception/order screens, specimen lifecycle, barcode labels, worklists, and status history.

Acceptance gate: Every specimen is traceable to its patient and order; duplicate identifiers and invalid transitions are prevented; cancellation and rejection are auditable.

Phase 5 — Results, Review, Approval, and Reports

Goal: Implement the most safety-critical part of the LIS.

Tasks:

• Build result entry screens for each supported test/result type.
• Validate units, formats, required fields, and permitted values.
• Display approved reference ranges using the correct patient context.
• Flag out-of-range values using approved configuration.
• Separate entry, review, approval, and release permissions.
• Implement controlled status transitions, for example: Draft → Entered → Reviewed → Approved → Released.
• Prevent silent modification of approved or released results.
• Implement corrected/amended reports with reason, author, timestamp, and version history.
• Generate reports and verify Arabic/English rendering if required.
• Record report release and access events where appropriate.
• Define critical-result escalation with laboratory leadership.

Deliverables: Result entry/review, approval and release controls, versioned PDF reports, corrected-report workflow, audit trail, and safety tests.

Acceptance gate: Required approvals cannot be bypassed; released results cannot be silently overwritten; amendments preserve prior versions; reports match stored approved results; permission and audit tests pass.

Clinical restriction: Developers and AI agents must not invent reference intervals, critical values, clinical rules, or interpretations. Qualified laboratory professionals must approve these configurations before production use.

Phase 6 — Accounts, Cashier, and Management Reports

Goal: Add traceable financial workflows.

Tasks:

• Implement test pricing and controlled price changes.
• Implement discounts with authorization limits and reasons.
• Track invoices, payments, balances, refunds, and adjustments.
• Implement cashier shifts and shift closing.
• Record user, time, branch, and reason for financial changes.
• Add daily revenue and operational reports.
• Define how cancellations and refunds affect financial records.
• Use transactions and integrity constraints to prevent inconsistent balances.

Deliverables: Pricing and invoices, payments/discounts/refunds, cashier shifts, management reports, and audit history.

Acceptance gate: Financial totals reconcile with transactions; unauthorized discounts/refunds are blocked; changes are traceable; concurrent updates cannot create inconsistent balances.

Phase 7 — Patient Report Access Portal

Goal: Let patients access released reports through a controlled and auditable process.

Tasks:

• Define identity verification and consent requirements.
• Do not use a national ID number alone as authentication.
• Prefer OTP to the phone number already registered with the laboratory, or a secure expiring link/QR workflow.
• Apply rate limits and abuse protection.
• Restrict access to reports approved for release.
• Support token expiry and revocation.
• Log access attempts, successful views, and downloads.
• Avoid exposing patient information in URLs or logs.

Deliverables: Patient access flow, secure report delivery, expiring/revocable tokens, and access audit events.

Acceptance gate: A report cannot be retrieved using only a guessed identifier or national ID; access expires or can be revoked; access events are auditable.

Phase 8 — Full QA, Security, Backup, and Recovery

Goal: Prove the system is safe and recoverable before deployment.

Tasks:

• Add unit, integration, API, permission, and end-to-end tests.
• Test critical workflows from reception through report release.
• Test concurrent use by multiple staff members.
• Test invalid input, duplicate requests, interrupted operations, and database failures.
• Review authentication, authorization, sessions, secrets, dependencies, and logs.
• Define backup frequency, retention, encryption, and off-device storage.
• Define RPO (maximum acceptable data loss) and RTO (maximum acceptable recovery time).
• Perform documented restore tests on a clean environment.
• Test database migrations and recovery procedures.
• Prepare incident response and support procedures.

Deliverables: Test report and defect register, security review, backup/recovery runbook, restore-test evidence, and release-readiness checklist.

Acceptance gate: Critical workflows pass; no critical security or data-integrity issue remains; a backup has been restored successfully and the recovery procedure is documented.

Phase 9 — Windows Local Deployment

Goal: Install and operate reliably on the laboratory Windows machine.

Tasks:

• Select and test the Windows deployment method.
• Verify Windows edition, hardware, storage, and network requirements.
• Package backend, frontend, and database dependencies appropriately.
• Configure safe startup and recovery after restart.
• Configure firewall rules and LAN access.
• Secure database and administrative access.
• Configure local backups and off-device backup transfer.
• Document installation, update, rollback, troubleshooting, and recovery.
• Test on a clean or representative Windows machine using synthetic data.

Deliverables: Windows installation package/procedure, configuration guide, administrator/operator manuals, update/rollback procedure, and deployment test report.

Acceptance gate: A clean Windows installation succeeds using the documented procedure; LAN clients connect; restart, backup, restore, and update procedures are tested.

Phase 10 — Offline Synchronization and Multi-Site Expansion

Goal: Add safe cross-site synchronization only after local operation is stable and business rules are approved.

Tasks:

• Reconfirm the approved branch/site topology.
• Define permitted operations during each type of outage.
• Implement outbox/inbox processing and idempotent message handling.
• Implement version checks, retries, monitoring, and failed-message review.
• Detect conflicts explicitly.
• Require authorized human resolution for sensitive medical and financial conflicts.
• Test prolonged outages, duplicate delivery, out-of-order events, partial failures, and recovery.
• Add synchronization status screens and operational alerts.

Deliverables: Documented synchronization protocol, conflict-resolution workflow, monitoring/recovery tools, and multi-site test report.

Acceptance gate: Synchronization is repeatable and auditable; retries do not create duplicates; sensitive conflicts are never silently overwritten; outage and recovery tests pass.

────────

5. Advanced Features — Defer Until the Core Is Stable

Do not delay a safe first release for these features:

• Analyzer and instrument integrations.
• Autoverification and Delta Check.
• Advanced critical-value automation.
• Microbiology, histopathology, molecular diagnostics, and specialized departments.
• Advanced specimen lifecycle, reruns, retesting, and partial results.
• Turnaround-time analytics and advanced worklists.
• Corporate and insurance accounts.
• Cost and profitability analytics.
• Workforce management and advanced scheduling.
• Incident and document-control modules.
• Advanced monitoring, 2FA, and device management.
• Public developer APIs and webhooks.
• Advanced cloud synchronization and cross-site orchestration.

Each advanced feature needs separate requirements, an accountable clinical/business owner, a risk review, tests, and acceptance criteria.

────────

6. How to Work with Claude Code

Claude Code should work on one bounded task at a time. Do not allow it to make broad architectural or clinical decisions without review.

Before each task

1. Ensure the current Git branch is clean or existing changes are safely committed.
2. Specify the phase and one task.
3. Ask Claude Code to inspect relevant existing code and tests before changing anything.
4. For substantial work, ask it to list proposed changes and risks first.
5. Require tests and documentation as part of the task.

Reusable prompt

> You are working on EgyptLab LIS. Read `CLAUDE.md` and this implementation guide before making changes. Work only on the specific task below. First inspect the existing code and relevant tests. Do not assume a feature exists unless you verify it. Do not change unrelated modules or make unapproved architecture decisions. Never invent clinical values, reference ranges, critical limits, formulas, or diagnostic rules. Never silently overwrite released medical results or sensitive financial records. Do not use real patient data. Implement the smallest safe change, add or update tests, run relevant checks, and update documentation. If a requirement is ambiguous or conflicts with the architecture, stop and explain the issue instead of guessing.
> 
> **Current phase:** [PHASE NUMBER AND NAME]  
> **Task:** [ONE SPECIFIC TASK]  
> **Acceptance criteria:** [CLEAR, TESTABLE CONDITIONS]
> 
> Before finishing, report: files changed, design decisions, migrations, tests run and results, security/data-integrity considerations, known limitations, and the recommended next task. Do not start the next phase.

End-of-task review checklist

☐ Existing implementation was inspected first.
☐ Changes are limited to the requested task.
☐ Required database migrations are included.
☐ Tests were added or updated and actually run.
☐ Permissions and tenant/branch isolation were considered.
☐ Sensitive data is protected.
☐ Required audit events are recorded.
☐ Documentation is updated.
☐ A human reviewed and accepted the result.

────────

7. Required Project Documentation

Keep these documents in the repository:

|File/folder                   |Purpose                                                   |
|------------------------------|----------------------------------------------------------|
|`CLAUDE.md`                   |Permanent coding, safety, and AI workflow rules           |
|`README.md`                   |Project overview and quick start                          |
|`docs/architecture/`          |Architecture diagrams and technical design                |
|`docs/architecture/decisions/`|Architecture Decision Records                             |
|`docs/requirements/`          |Approved functional and non-functional requirements       |
|`docs/security/`              |Security model, access rules, and threat reviews          |
|`docs/clinical-configuration/`|Professionally approved test and clinical configuration   |
|`docs/operations/`            |Installation, backup, restore, update, and troubleshooting|
|`docs/testing/`               |Test strategy and acceptance evidence                     |
|`CHANGELOG.md`                |User-visible changes by release                           |

Never store passwords, API keys, private certificates, or real patient records in documentation or source control.

────────

8. Production Release Principles

The system is not production-ready merely because it starts or the interface looks complete.

Before each production release, verify:

• Patient and specimen traceability.
• Correct result storage, display, approval, and report generation.
• Role and branch permissions.
• Audit history for sensitive actions.
• Database migration safety.
• Backup and a successfully tested restore.
• Windows installation and restart behavior.
• Error handling and operational logging.
• No unresolved critical or high-severity defects.
• Approval from designated clinical and operational owners at the laboratory.

For updates: back up first, verify compatibility, apply migrations safely, run smoke tests, and maintain a documented rollback or recovery plan.

────────

9. Immediate Next Actions

Start with these actions only:

1. Put this guide in the root of the EgyptLab LIS repository.
2. Create or review CLAUDE.md for permanent safety and coding rules.
3. Ask Claude Code to perform Phase 0 only: inspect the Linux environment, report actual installed tools and available resources, and identify deployment questions.
4. Review its report and correct assumptions.
5. Record and approve architecture decisions before creating major application modules.

Do not ask Claude Code to generate the entire LIS at once. Establish the architecture, build a small working foundation, verify it, and then expand phase by phase.
