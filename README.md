# EgyptLab LIS

Lab Management Software Built Around The Way Your Lab Operates

A planned commercial Laboratory Information System for medical laboratories in Egypt, targeting a local Windows server deployment.

**Status:** Phase 0 documentation and environment audit only. No application code, database schema, or product feature has been implemented.

## Governing documents

- `ROADMAP.md` — authoritative phase sequence and project rules. When it conflicts with the guide below, the roadmap governs and the conflict must be recorded.
- `IMPLEMENTATION_GUIDE.md` — detailed execution reference, rules, and acceptance gates.
- `CLAUDE.md` — permanent rules for AI coding agents.
- `docs/operations/environment-audit.md` — verified facts about the current environment.
- `docs/requirements/scope-and-exclusions.md` — draft scope, exclusions, and open questions.
- `docs/architecture/decisions/` — Architecture Decision Record log.

## Repository layout

| Path | Purpose |
| --- | --- |
| `docs/architecture/` | Architecture diagrams and technical design |
| `docs/architecture/decisions/` | Architecture Decision Records |
| `docs/requirements/` | Approved functional and non-functional requirements |
| `docs/security/` | Security model and threat reviews |
| `docs/clinical-configuration/` | Professionally approved test and clinical configuration |
| `docs/operations/` | Environment audit, installation, backup, restore, update procedures |
| `docs/testing/` | Test strategy and acceptance evidence |

The directories for security, clinical configuration, and testing are placeholders for later phases; no clinical values are stored here.

## Working tree and Git metadata

The repository is stored entirely at `Z:\lab`, including a real `.git` directory. `git init` fails directly on this SSHFS mount, so the `.git` directory was created locally and copied into place; verified with `git -C Z:\lab status`. Avoid deleting or replacing that directory.

GitHub remote: `https://github.com/azzam-mo/EgyptLab-LIS` (private). Local backup remote: `D:\EgyptLab-LIS-backup.git`.

## Quick start

There is no application to start yet. Phase 1 will add the backend and frontend skeletons, PostgreSQL development configuration, quality checks, and documented startup instructions.
