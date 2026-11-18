# EgyptLab LIS

Lab Management Software Built Around The Way Your Lab Operates

A planned commercial Laboratory Information System for medical laboratories in Egypt, targeting a local Windows server deployment.

**Status:** Phases 0.1 through 0.4 are complete. The initial database design, authentication, and backend-enforced role/branch permissions are present. No clinical values or product workflow are implemented yet.

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
| `backend/` | ASP.NET Core API, EF Core persistence, and xUnit test projects |
| `frontend/` | React + TypeScript client |
| `.github/workflows/` | GitHub Actions CI |
| `docs/architecture/erd.md` | Initial database ERD and schema conventions |
| `docs/architecture/` | Architecture diagrams and technical design |
| `docs/architecture/decisions/` | Architecture Decision Records |
| `docs/research/` | Reference LIS research notes |
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
# Backend (restore the local EF tool before generating migrations)
dotnet tool restore
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

The backend exposes `GET /health` for smoke checks. `POST /auth/login` accepts `branchCode`, `userName`, and `password`, then returns a bearer token. `GET /auth/me` returns the current identity and permissions. Manager-only `GET /admin/users` and `GET /admin/roles` demonstrate branch-scoped authorization.

### Backend configuration

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings:Lis` | SQLite database path; defaults to `Data Source=egyptlab.db`. |
| `Jwt:Issuer` / `Jwt:Audience` | Token issuer and audience; safe non-secret defaults are provided. |
| `Jwt:ExpiryMinutes` | Access-token lifetime; defaults to 15 minutes. |
| `Jwt:SigningKey` | Required in production and must be at least 32 bytes. Development generates an ephemeral key when absent; never commit a real key. |

No administrator account or clinical value is seeded. Bootstrap and account-management procedures must be approved before deployment.
