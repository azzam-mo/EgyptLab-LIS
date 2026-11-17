# Scope, Exclusions, and Constraints — Draft

Status: Draft derived from `IMPLEMENTATION_GUIDE.md`. Not approved by the project owner.
Date: 17 November 2026

This document restates the guide's scope so that assumptions can be reviewed. It does not add new requirements or make architecture decisions.

## Initial in-scope intent

- Dependable daily laboratory operations for medical laboratories in Egypt.
- Local deployment on a Windows server in the laboratory.
- Multiple branches, with topology still to be confirmed.
- Patient identity, specimen traceability, results, review and approval, reports, accounts, and management reporting as described in Phases 2 through 7.
- Strong reliability, permissions, auditability, backup, and recovery before advanced features.

## Explicitly deferred

Deferred until the local core is stable and each feature has its own requirements, owner, risk review, tests, and acceptance criteria:

- Analyzer and instrument integrations.
- Autoverification and Delta Check.
- Advanced critical-value automation.
- Microbiology, histopathology, molecular diagnostics, and specialized departments.
- Advanced specimen lifecycle, reruns, retesting, and partial results.
- Turnaround-time analytics and advanced worklists.
- Corporate and insurance accounts.
- Cost and profitability analytics.
- Workforce management and advanced scheduling.
- Incident and document-control modules.
- Advanced monitoring, 2FA, and device management.
- Public developer APIs and webhooks.
- Advanced cloud synchronization and cross-site orchestration.

Offline synchronization is Phase 10 and must not begin before topology and offline workflow decisions are approved.

## Development and deployment constraints

- Build in controlled phases; one bounded task at a time.
- No real patient data in development.
- No invented clinical values, rules, units, formulas, or interpretations.
- No silent overwrite of released results or sensitive financial records.
- Backend-enforced tenant and branch isolation.
- Windows deployment must be tested early with a working prototype.
- Package versions and deployment tooling are not finalized until a prototype is tested.

## Open questions

See `docs/operations/environment-audit.md`, section "Unresolved items requiring owner decision". No answer is assumed here.
