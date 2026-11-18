# ADR-0003 — Authentication and Authorization Model

Status: Accepted
Date: 2026-11-18
Owner: project owner — decisions approved 18 November 2026

## Context

Phase 0.4 requires users, roles, branch linkage, and backend proof that a user cannot read another branch or act outside their role. ADR-0002 selected ASP.NET Core Identity, but the default Identity tables do not satisfy the immutable rule that every table has a UUID key plus `branch_id`, `created_at`, `updated_at`, and `deleted_at`.

The owner was asked to resolve this conflict and approve the permission model. The following decisions were approved before implementation:

1. Roles are branch-scoped.
2. One user may hold several roles; effective permissions are their union.
3. Authentication uses a custom store over the existing `users`, `roles`, and `user_roles` tables, with ASP.NET Core Identity password-hashing primitives.
4. Authentication uses short-lived bearer JWTs signed with a configured key.
5. The least-privilege role-permission matrix below is accepted.

## Options considered

- Default Identity tables: rejected because they violate the UUID and per-table audit-column rules.
- Full `IdentityDbContext` with customized entities: rejected as unnecessary complexity for this phase.
- Server-side cookies: secure, but requires anti-forgery handling and an installed HTTPS certificate for the Secure flag; deferred in favor of the SPA-friendly bearer flow.
- Custom store with Identity primitives: accepted because it keeps the schema compliant and uses audited password hashing.

## Selected approach

### Store and authentication

- `users` stores `PasswordHash`, `SecurityStamp`, `NormalizedUserName`, and `IsActive`. No plaintext password is persisted.
- Login requires branch code plus username because usernames are unique only inside a branch.
- `IPasswordHasher<User>` from ASP.NET Core Identity hashes and verifies passwords. Rehashes are saved when Identity reports they are needed.
- Access tokens are short-lived HMAC-SHA256 JWTs. Issuer, audience, expiry, and signing key come from configuration. Production startup fails without a key of at least 32 bytes; development uses an ephemeral in-process key so no secret is committed.
- Every request validates the token against the user record. Deactivation, deletion, or a changed security stamp rejects otherwise-valid tokens.

### Authorization

- Roles are branch-scoped and stored in `roles`; assignments are in `user_roles`.
- Multiple roles are allowed. A role from a different branch is ignored even if a bad assignment row exists.
- Unknown role names grant no permissions.
- Authorization policies are permission-based and deny by default. `/health` and `/auth/login` are the only anonymous endpoints.
- Approved least-privilege matrix:

| Role | Permissions |
| --- | --- |
| Reception | patient, order, invoice, and payment read/write |
| Technician | order and sample read; sample and result-entry write; catalogue read |
| Reviewer | result read/review/approve; order and sample read; report read/export |
| Accountant | order read; invoice and payment read/write; finance reports |
| Manager | user/role/branch administration; read-only operational and report permissions |

The manager role deliberately does **not** include result approval. A manager who must approve results is assigned the reviewer role explicitly; two-step approval still requires a different person, which is enforced when result workflows are implemented.

### Branch isolation

- A request-scoped branch scope is populated from the validated token and cleared after the request.
- Every domain table has a named EF query filter on `branch_id` in addition to the named soft-delete filter. An unscoped query returns no rows.
- The branch scope is part of the EF model cache key so one branch's filter cannot be reused for another.

## Consequences

Positive:

- Backend policy checks and query filters enforce both parts of the acceptance gate.
- Deactivating a user invalidates existing tokens without a session table.
- No default Identity schema or committed secret weakens the immutable rules.

Negative and unresolved:

- Username/password login and token validation require a branch code and a local user; self-service password reset and account lockout remain later security work.
- There is no seeded administrator account. A controlled bootstrap procedure for the first manager must be defined before deployment.
- Branch-scoped role names are resolved through a static code map; no role-permission UI exists yet.
- The manager-only user and role endpoints in this phase are read-only except for assigning an existing role to an existing user.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Token replay after deactivation | Security stamp and active-user check on every request. |
| Cross-branch data leak | Query filter plus model cache key, covered by integration and persistence tests. |
| Role privilege drift | Static approved map with tests for manager, reception, multiple roles, and cross-branch role rows. |
| Weak production signing key | Startup validation rejects keys shorter than 32 bytes. |

## Verification

`backend/tests/EgyptLab.LIS.Api.Tests/AuthenticationTests.cs` covers login, wrong password, wrong branch, deny-by-default, role denial, branch-scoped user listings, multiple-role unions, deactivated-token rejection, and cross-branch role rejection. `backend/tests/EgyptLab.LIS.Infrastructure.Tests/BranchIsolationTests.cs` proves that unscoped queries fail closed and each branch sees only its own rows.

## Approval

Approved by the project owner on 18 November 2026 through explicit answers during Phase 0.4.
