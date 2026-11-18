# EgyptLab LIS

Lab Management Software Built Around The Way Your Lab Operates

A planned commercial Laboratory Information System for medical laboratories in Egypt, targeting a local Windows server deployment.

**Status:** Phase 0.1 is complete. Phase 0.2 adds the backend and frontend foundation with CI. No product feature or database schema has been implemented yet.

## Governing documents

- `ROADMAP.md` — authoritative phase sequence and project rules. When it conflicts with the guide below, the roadmap governs and the conflict must be recorded.
- `IMPLEMENTATION_GUIDE.md` — detailed execution reference, rules, and acceptance gates.
- `CLAUDE.md` — permanent rules for AI coding agents.
- `docs/operations/environment-audit.md` — verified facts about the current environment.
- `docs/operations/git-workflow.md` — GitHub-as-source-of-truth routine and automatic commit backup.
- `docs/requirements/scope-and-exclusions.md` — draft scope, exclusions, and open questions.
- `docs/architecture/decisions/` — Architecture Decision Record log.

## Repository layout

| Path | Purpose |
| --- | --- |
| `backend/` | ASP.NET Core API and xUnit test project |
| `frontend/` | React + TypeScript client |
| `.github/workflows/` | GitHub Actions CI |
| `docs/architecture/` | Architecture diagrams and technical design |
| `docs/architecture/decisions/` | Architecture Decision Records |
| `docs/requirements/` | Approved functional and non-functional requirements |
| `docs/security/` | Security model and threat reviews |
| `docs/clinical-configuration/` | Professionally approved test and clinical configuration |
| `docs/operations/` | Environment audit, installation, backup, restore, update procedures |
| `docs/testing/` | Test strategy and acceptance evidence |

The directories for security, clinical configuration, and testing are placeholders for later phases; no clinical values are stored here.

## Working tree and Git metadata

GitHub is the source of truth: `https://github.com/azzam-mo/EgyptLab-LIS` (private). Work is done on a local clone and pushed immediately after every commit by the `post-commit` hook described in `docs/operations/git-workflow.md`.

Earlier Phase 0 notes recorded a `Z:\lab` SSHFS working tree. That is historical documentation and no longer describes the active clone.

## Quick start

Prerequisites: .NET 10 SDK and Node.js 22.

```sh
# Backend
dotnet restore EgyptLab.LIS.slnx
dotnet build EgyptLab.LIS.slnx --configuration Release
dotnet test EgyptLab.LIS.slnx --configuration Release

# Frontend
cd frontend
npm ci
npm run lint
npm run test
npm run build
```

The backend exposes `GET /health` for smoke checks. No clinical data, database schema, or product workflow is present yet.
