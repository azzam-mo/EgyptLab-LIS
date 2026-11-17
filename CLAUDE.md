# EgyptLab LIS — Permanent Project Rules

Read `IMPLEMENTATION_GUIDE.md` before any work. These rules must not be overridden by convenience or by a request to move faster.

## Workflow

- Work on one bounded task at a time, and only within the current approved phase.
- Inspect existing code and relevant tests before changing anything.
- Verify that a feature exists before relying on it. Never assume it was implemented.
- Do not make architecture, clinical, or business-policy decisions without explicit approval. Record major decisions in `docs/architecture/decisions/`.
- Do not start the next phase until the current acceptance gate has been reviewed and approved by the project owner.
- Stop and explain the issue when a requirement is ambiguous or conflicts with the architecture. Do not guess.
- Keep changes limited to the requested task. Do not change unrelated modules.

## Safety and data integrity

- Never invent reference ranges, critical limits, units, formulas, diagnostic interpretations, analyzer mappings, or clinical rules.
- Never use real patient data. Use synthetic or properly anonymized data only.
- Never silently overwrite released medical results or sensitive financial records. Corrections must create a traceable amended version with reason, user, and timestamp.
- Never use last-write-wins for sensitive records.
- Enforce tenant and branch isolation in the backend, not only in the interface.
- Record audit events for sensitive actions.
- Never commit passwords, API keys, private certificates, or patient records.

## Environment and repository notes

- Working tree and Git metadata are both on `Z:\lab`, an SSHFS mount. `git init` fails on this mount, but a real `.git` directory copied from a locally initialized repository works. Do not replace it with a pointer file.
- The owner decided to provision a Linux development host. Application work is paused until that host is available; this Windows machine was used only for Phase 0 documentation.
- Verified host facts are recorded in `docs/operations/environment-audit.md`.

## Task report format

Before finishing any task, report: files changed, design decisions, migrations, tests run and results, security and data-integrity considerations, known limitations, and the recommended next task. Do not start the next phase.
