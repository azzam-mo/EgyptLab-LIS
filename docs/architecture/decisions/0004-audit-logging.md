# ADR-0004 — Audit Logging for Clinical and Financial Changes

Status: Accepted
Date: 2026-11-18
Owner: project owner — implementation authorized 18 November 2026

## Context

Phase 0.5 requires `audit_log` to act as a shared service that is called
automatically with every modification. The accepted project rules require that
every change to patient, result, or invoice data records who changed it, when,
the before and after values, and the branch. Audit rows must be append-only,
and patient data must not be written to ordinary application logs.

The `audit_log` table already exists in the initial schema. It has no foreign
keys and no database-level append-only protection. ADR-0002 selected an EF Core
`SaveChanges` interceptor as the audit mechanism, so this ADR records how that
mechanism is implemented and where its limits are.

## Decision

### Interceptor and transaction boundary

`AuditWriter` is an EF Core `SaveChangesInterceptor` registered by
`LisDbContext.OnConfiguring`. Registering it there, instead of only in the API
dependency-injection container, ensures the same write path is used by API
requests, infrastructure tests, and design-time contexts.

The interceptor adds audit rows to the same `DbContext` change tracker before
the database write. Audit rows therefore commit or roll back in the same
transaction as the business change. If audit persistence fails, the business
change fails closed and is not committed.

### Audited entity set

Phase 0.5 audits exactly these entities:

- `Patient`
- `Result`
- `Invoice`

Creation, update, and soft deletion are audited. Hard deletion of these
entities is rejected by the interceptor; callers must set `DeletedAt` instead.

This is an explicit allowlist. It deliberately excludes `AuditLog` (to prevent
recursion and forged rows), `OutboxEvent` (to avoid duplicate sync payloads),
`User` (so login timestamp updates do not create misleading actor records),
and all other catalogue and administrative tables. Payments, orders, and
result versions are not audited in this phase; that is a recorded limitation,
not an implicit claim of coverage.

### Before and after snapshots

For updates and soft deletes, only properties whose EF state is `Modified` are
recorded. For creation, the persisted scalar values are recorded as the after
snapshot. Navigation properties are never serialized.

The snapshots use `System.Text.Json` and are stored in `before_json` and
`after_json`. The snapshot does not include unchanged properties.

### Actor identity and branch isolation

`AuditActorMiddleware` reads the authenticated principal only after JWT
validation has succeeded. The actor user ID comes from the `sub` claim and the
actor branch from `branch_id`; no client-supplied body, header, or query value
is trusted. The IP address comes from the server connection. Anonymous requests
leave the actor unset.

The interceptor enforces all of the following before writing an audit row:

- the entity has a non-empty branch;
- the entity branch matches the current `BranchScope`;
- the entity branch matches the authenticated actor branch;
- an existing entity's branch cannot be changed.

Audit rows inherit the standard branch and soft-delete query filters, so audit
reads fail closed when no branch scope is set. Cross-branch reads and writes
remain covered by the existing backend isolation tests.

The access token currently carries no role claim. `ActorRole` is therefore
`null` in Phase 0.5. Adding role claims is a separate authentication change and
must not be simulated by trusting request data.

### Append-only protection

Three layers protect audit integrity:

1. `AuditLog` updates and deletes are rejected by the interceptor.
2. Direct `AuditLog` inserts by application code are rejected; only the
   interceptor's pending rows are allowed.
3. SQLite `BEFORE UPDATE` and `BEFORE DELETE` triggers reject direct SQL that
   bypasses EF Core. The migration is
   `20261118104514_AuditLogAppendOnly`.

The database triggers are SQLite-specific. A PostgreSQL equivalent is required
when synchronization introduces that provider.

### Privacy safeguards

`audit_log` is treated as a controlled, branch-scoped medical record, not as an
ordinary application log. Audit content is never written to application logs,
error messages, or console output.

The following direct patient identifiers are always redacted in audit JSON:

- first name;
- last name;
- national ID;
- phone;
- date of birth.

Authentication secrets and credential-like property names are always redacted,
including password hashes, security stamps, passwords, tokens, secrets, API
keys, private keys, and connection strings.

Clinical values (`Result` values and status), financial values (`Invoice`
amounts and status), sex, and non-identifying catalogue references remain
visible so the audit record stays useful for traceability. Free-text fields
such as `Result.ValueText`, `Invoice.ContractualParty`, and cancellation
reasons are not structurally redacted; they must not be used to store patient
identifiers. Future audit read endpoints must require a dedicated permission,
must enforce branch scope, and must not expose cross-branch rows.

### Test-only mutation path

The API integration test uses a test-host-only probe middleware registered by
`IStartupFilter`. It is not part of the production pipeline. It exists because
Phase 0.5 has no production endpoint that mutates a patient, result, or
invoice; the test still uses the real login endpoint, the real JWT validation,
the real audit middleware, and the real `LisDbContext`.

## Consequences

Positive:

- Clinical and invoice changes cannot commit without their audit row.
- Actor identity is derived only from a validated token.
- Patient direct identifiers and authentication secrets do not appear in audit
  JSON.
- Ordinary EF Core updates and deletes of audit rows fail before SQL executes.
- Direct SQL updates and deletes of audit rows fail at the database.

Negative or unresolved:

- `ExecuteUpdate`, `ExecuteDelete`, and raw SQL against `patients`, `results`,
  or `invoices` bypass the interceptor for the business change. They must not
  be used for audited entities. The audit_log triggers do not compensate for
  this; a future phase can add database triggers or an outbox-backed write path
  for those tables.
- A database administrator or an attacker with file-level access can drop
  triggers or modify the SQLite file. Raw SQL can also insert a syntactically
  valid forged audit row; append-only protection prevents updates and deletes,
  not forgery. Tamper-evident hash chaining, signing, encryption at rest, and
  protected backups belong to Phase 6.
- `RemoteIpAddress` is the direct connection address. If a reverse proxy is
  introduced, forwarded-header handling must be configured and trusted
  explicitly; the middleware must not trust arbitrary client headers.
- `ActorRole` is null until the token includes a role claim.
- Payments, orders, and result versions are outside the Phase 0.5 allowlist.
- Audit rows accumulate indefinitely. Retention, archival, and backup policy
  are not defined in this phase.
- The SQLite trigger implementation must be repeated for PostgreSQL before
  cross-branch synchronization is used. Any future migration that rebuilds the
  `audit_log` table must recreate the append-only triggers.

## Rejected alternatives

- **Auditing all `AuditedEntity` types.** Rejected because it would record
  login and outbox traffic, duplicate synchronization payloads, and create
  noisy or misleading rows outside the approved scope.
- **Auditing only updates.** Rejected because creation and soft deletion are
  also sensitive changes and the cost of covering them is small.
- **Storing full entity snapshots.** Rejected because unchanged patient
  identifiers would be copied into every audit row for no traceability benefit.
- **Relying only on EF Core.** Rejected because `ExecuteUpdate`, `ExecuteDelete`,
  and raw SQL bypass the interceptor. Database triggers cover direct SQL
  against `audit_log` itself.
- **Using the quarantined `stash@{0}` implementation directly.** Rejected
  because it audited every `AuditedEntity`, used a `DbContext` override instead
  of the ADR-0002 interceptor, did not enforce branch consistency or
  append-only behavior, and did not redact patient names.

## Approval

The project owner authorized the Phase 0.5 implementation plan, including the
explicit entity allowlist, identifier redaction, branch consistency, atomic
audit writes, and append-only enforcement, on 18 November 2026. This ADR is
binding for Phase 0.5 and later work until superseded.
