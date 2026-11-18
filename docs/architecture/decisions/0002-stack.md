# ADR-0002 — Application Stack for the Local-First LIS

Status: Proposed — pending owner approval
Date: 2026-11-18
Owner: project owner to confirm

## Context

EgyptLab LIS must run reliably on a **Windows local server in each laboratory**, with staff using browsers and printers over the LAN. The first pilot is one branch with no analyzer integration. Internet or inter-site connectivity may be unavailable, so core workflows must keep working against a **local database**; synchronization is a later phase.

The governing priorities, in order, are: result accuracy, patient-data security, reliability, ease of operation, and scalability. The following constraints shape the decision:

- Every table uses a UUID primary key; medical and financial records use soft delete and immutable versions.
- Sensitive actions must be audited, and role/branch permissions must be enforced in the backend.
- Money is stored as integer piasters, never floating point.
- Reports need correct Arabic (RTL, joined letters) PDF output with branch branding and a verification QR.
- The database design must be sync-ready from day one (outbox/event log), even though sync is not implemented in the pilot.
- Development is intended on Linux; deployment is on Windows. Both must be supported.
- The existing `IMPLEMENTATION_GUIDE.md` proposes Python/Django + React + PostgreSQL, but that proposal is **not yet accepted**. This ADR re-evaluates it against the constraints above and supersedes it if accepted.

## Decision drivers

| Driver | Why it matters |
| --- | --- |
| Windows operability | The lab staff, not a dedicated DevOps team, will run the server; startup, backup, restart, and updates must be simple. |
| Local-first data | A branch must keep working without internet or a central server. |
| Data integrity | Strong typing, transactions, migrations, and concurrency control protect results and money. |
| Backend-enforced security | Authentication, role/branch policies, and audit must live in the server. |
| Arabic reporting | PDF generation must be validated before being relied upon. |
| Cross-platform development | Linux development and Windows deployment must use the same codebase. |

## Options

### Option A — Python, Django REST Framework, React, PostgreSQL

- Mature ORM, migrations, admin site, authentication, and a large ecosystem.
- Excellent for complex business logic and rapid development.
- PostgreSQL is robust but requires a separately installed and maintained database service on every Windows branch server.
- Windows service packaging and Python runtime management add operational steps for the lab.
- Arabic PDF must be validated (for example, a browser-based renderer or a reporting library); WeasyPrint-style dependencies are awkward on Windows.
- Fits the existing implementation guide's unaccepted proposal.

### Option B — TypeScript, NestJS, React, SQLite locally, PostgreSQL centrally

- One language across frontend and backend; strong module and guard structure for permissions.
- SQLite fits the local-first model; a central PostgreSQL can be added later.
- Native SQLite drivers require per-platform builds, and a Windows service still needs an external wrapper or packaging step.
- Arabic PDF still needs a validated renderer; Node PDF libraries often need extra shaping work for Arabic.
- Good long-term sync story, but more assembling of framework pieces (auth, admin, migrations, PDF) by the team.

### Option C — ASP.NET Core, EF Core, React, SQLite locally, PostgreSQL centrally

- First-class Windows deployment: self-contained publish, Windows Service or IIS hosting, no separate language runtime to install.
- EF Core provides migrations, transactions, UUID keys, soft-delete query filters, concurrency tokens, and change tracking that can drive the audit service.
- ASP.NET Core Identity plus policy-based authorization supports role and branch checks in the backend, not only in the UI.
- SQLite runs in-process with no database service to install; WAL mode and `VACUUM INTO` support local operation and safe backups. A repository layer keeps the door open to central PostgreSQL for later synchronization.
- Strong static typing supports integer piasters and typed result values, reducing result and money errors.
- React + TypeScript is served as static files by the same server, so production has no separate Node process.
- Arabic PDF tooling must still be prototyped; .NET has commercial and community PDF libraries, but no library is selected here.

### Option D — Python, FastAPI, React, SQLite locally

- Lightweight and quick to start; SQLite fits local-first operation.
- Fewer built-in pieces than Django: authentication, admin, migrations, and audit infrastructure must be assembled explicitly.
- Windows service packaging and PDF rendering concerns remain similar to Option A.

## Selected approach

**Proposed: Option C — ASP.NET Core with EF Core, React + TypeScript, SQLite as the local branch database, and PostgreSQL reserved for a future central/sync role.**

Target shape, subject to a small Phase 1 prototype before package versions are frozen:

- **Backend:** ASP.NET Core Web API on the current .NET LTS line (10.x at the time of writing), C#, EF Core with code-first migrations.
- **Frontend:** React + TypeScript, served as built static files by the backend. Arabic is the primary UI language with full RTL; English is secondary, through translation files.
- **Local database:** SQLite in WAL mode, one database file per branch, stored on local disk with a documented backup procedure.
- **Future central database:** PostgreSQL, introduced only in the synchronization phase behind the same persistence abstraction.
- **Identity and permissions:** ASP.NET Core Identity with policies that always include branch scope; denied by default.
- **Audit:** EF Core `SaveChanges` interceptor writing append-only audit events for patient, result, and financial changes.
- **Money:** `long` piasters in the database and domain model; formatting happens only at the UI edge.
- **Reporting:** a dedicated prototype will validate Arabic shaping, RTL layout, branch branding, and QR generation before Phase 3. No final PDF library is chosen in this ADR.
- **CI:** GitHub Actions on Linux for build, lint, and tests; a Windows smoke test is added before deployment sign-off.

Rationale: this option best satisfies the top three priorities. Self-contained Windows deployment and an in-process database reduce the number of moving parts that can fail in the lab. EF Core and C# give compile-time and database-level support for the immutable-result, audit, soft-delete, and integer-money rules. React gives full control over RTL and translation behavior.

## Consequences

Positive:

- The pilot server can be installed and restarted with standard Windows mechanisms.
- Backups can be built around a single local database file plus documented application configuration.
- The same codebase builds on the planned Linux host and on Windows.
- The persistence layer can later add central PostgreSQL without rewriting business rules.

Negative or unresolved:

- The team must be comfortable with C# and EF Core; this is a learning cost.
- SQLite is suitable for one branch with hundreds of orders per day, but write concurrency must be measured in load testing before committing to it as a hard constraint.
- Arabic PDF rendering remains a prototype item and could influence the final reporting library.
- The 6-phase roadmap and the 11-phase implementation guide still differ; this ADR does not resolve that documentation conflict.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Windows deployment assumptions are wrong | Build a self-contained prototype and run it on a representative Windows server during Phase 1, before adding product features. |
| SQLite write contention under concurrent staff use | Keep all writes behind the API, enable WAL, add load tests in Phase 8, and revisit only with measured evidence. |
| Arabic PDF shaping or RTL layout fails | Validate a small report with synthetic, non-patient data before Phase 3; do not select a library by assumption. |
| Audit events are missed on some write paths | Route all sensitive writes through EF Core and test that patient, result, and invoice updates produce audit rows. |
| Branch isolation leaks | Enforce branch scope in authorization policies and query filters, with backend tests that attempt cross-branch access. |
| Sync is designed too late | Include UUID keys, `deleted_at`, and an outbox/event-log design in the Phase 0.3 ERD, without implementing sync yet. |

## Alternatives explicitly rejected for now

- **Full-stack Blazor only:** viable for .NET, but the React requirement in the roadmap and the need for a decoupled, testable RTL UI make React + TypeScript the safer choice for this phase.
- **A cloud-first or managed database:** rejected because the critical path must work without internet.

## Approval

Pending. The project owner must set the status to `Accepted` before this stack is treated as binding. If accepted, update `ROADMAP.md` section `الـ Stack` and task `0.1` accordingly, and mark the previously unaccepted proposal in `IMPLEMENTATION_GUIDE.md` as superseded.
