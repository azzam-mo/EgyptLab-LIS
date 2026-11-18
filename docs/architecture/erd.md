# EgyptLab LIS — Initial Entity Relationship Diagram

Status: Draft for owner review — Phase 0.3
Date: 2026-11-18
Implementation: `backend/src/EgyptLab.LIS.Infrastructure`
Migrations: `20261118083238_InitialSchema`, `20261118085228_AddAuthenticationFields`

This is the first database design. It implements the accepted stack in [ADR-0002](decisions/0002-stack.md) and the design conclusions in `docs/research/lis-notes.md`. It stores **structure only**: no test names, units, reference ranges, critical limits, prices, statuses, or patient data are seeded. Qualified laboratory staff must supply clinical values, and the owner must approve business policies, before any of them are used.

## Conventions applied to every table

| Rule | Implementation |
| --- | --- |
| Globally unique key | `id` is a client-generated UUID (`TEXT` in SQLite), never an auto-increment. |
| Branch scope | Every table has `branch_id`. On `branches` itself it equals `id`. A named query filter hides other branches after authentication; an unscoped query returns no rows. |
| Timestamps | `created_at` and `updated_at` are required UTC `DateTimeOffset`. |
| Soft delete | `deleted_at` is nullable; an EF global query filter excludes deleted rows on every table. |
| No hard delete | Every foreign key uses `ON DELETE RESTRICT`. |
| Money | All monetary columns end in `Piaster` and are `long` mapped to SQLite `INTEGER`. No floating-point money exists. |
| Sync readiness | `outbox_events` is created in the first migration, but no synchronization runs yet. |
| Append-only records | `audit_log`, `result_versions`, and `outbox_events` are not updated or deleted by design. |

## Diagram

```mermaid
erDiagram
    branches ||--o{ patients : "scopes"
    branches ||--o{ orders : "scopes"
    branches ||--o{ users : "scopes"
    branches ||--o{ roles : "scopes"
    branches ||--o{ tests : "scopes"
    branches ||--o{ panels : "scopes"
    branches ||--o{ invoices : "scopes"
    branches ||--o{ audit_log : "records"
    branches ||--o{ outbox_events : "queues"

    users ||--o{ user_roles : "has"
    roles ||--o{ user_roles : "granted_as"

    patients ||--o{ orders : "requests"
    orders ||--o{ order_items : "contains"
    orders ||--o{ samples : "produces"
    orders ||--o{ invoices : "billed_by"

    tests ||--o{ order_items : "ordered_as"
    tests ||--o{ panel_items : "member_of"
    tests ||--o{ reference_ranges : "configured_by"
    panels ||--o{ panel_items : "contains"
    panels ||--o{ order_items : "ordered_as"

    order_items ||--o{ samples : "collected_as"
    order_items ||--o{ results : "reported_for"
    samples ||--o{ results : "measured_from"
    samples ||--o| order_items : "linked_item"
    results ||--o{ result_versions : "corrected_by"
    result_versions ||--o| result_versions : "follows"

    invoices ||--o{ payments : "settled_by"
    payments ||--o| payments : "reversed_by"

    branches {
        TEXT id PK
        TEXT code UK
        TEXT name
        INTEGER is_active
    }
    roles {
        TEXT id PK
        TEXT branch_id FK
        TEXT name UK
    }
    users {
        TEXT id PK
        TEXT branch_id FK
        TEXT user_name
        TEXT normalized_user_name UK
        TEXT full_name
        TEXT password_hash
        TEXT security_stamp
        INTEGER is_active
        INTEGER must_change_password
        TEXT last_login_at
    }
    user_roles {
        TEXT id PK
        TEXT branch_id FK
        TEXT user_id FK
        TEXT role_id FK
    }
    patients {
        TEXT id PK
        TEXT branch_id FK
        TEXT national_id UK
        TEXT date_of_birth
        TEXT sex
        TEXT phone
    }
    tests {
        TEXT id PK
        TEXT branch_id FK
        TEXT code UK
        TEXT name
        TEXT sample_type
        TEXT unit
        INTEGER price_piaster
    }
    panels {
        TEXT id PK
        TEXT branch_id FK
        TEXT code UK
        INTEGER price_piaster
    }
    panel_items {
        TEXT id PK
        TEXT branch_id FK
        TEXT panel_id FK
        TEXT test_id FK
    }
    reference_ranges {
        TEXT id PK
        TEXT branch_id FK
        TEXT test_id FK
        TEXT component
        TEXT sample_type
        TEXT sex
        INTEGER min_age
        INTEGER max_age
        TEXT age_unit
        TEXT lower_bound
        TEXT upper_bound
        TEXT normal_text
        TEXT critical_lower
        TEXT critical_upper
        TEXT approved_by_user_id
    }
    orders {
        TEXT id PK
        TEXT branch_id FK
        TEXT patient_id FK
        TEXT order_number UK
        TEXT status
        INTEGER total_piaster
        INTEGER discount_piaster
    }
    order_items {
        TEXT id PK
        TEXT branch_id FK
        TEXT order_id FK
        TEXT test_id FK
        TEXT panel_id FK
        INTEGER unit_price_piaster
        INTEGER line_total_piaster
    }
    samples {
        TEXT id PK
        TEXT branch_id FK
        TEXT order_id FK
        TEXT order_item_id FK
        TEXT barcode UK
        TEXT status
        TEXT collected_at
        TEXT received_at
        TEXT rejection_reason
    }
    results {
        TEXT id PK
        TEXT branch_id FK
        TEXT sample_id FK
        TEXT order_item_id FK
        TEXT value_numeric
        TEXT value_text
        TEXT status
        TEXT approved_by_user_id
        TEXT released_at
        INTEGER current_version
    }
    result_versions {
        TEXT id PK
        TEXT branch_id FK
        TEXT result_id FK
        INTEGER version_number UK
        TEXT value_numeric
        TEXT value_text
        TEXT reason
        TEXT changed_by_user_id
        TEXT changed_at
        TEXT prior_version_id FK
    }
    invoices {
        TEXT id PK
        TEXT branch_id FK
        TEXT order_id FK
        TEXT invoice_number UK
        INTEGER total_piaster
        INTEGER discount_piaster
        INTEGER net_piaster
        INTEGER paid_piaster
        TEXT status
    }
    payments {
        TEXT id PK
        TEXT branch_id FK
        TEXT invoice_id FK
        INTEGER amount_piaster
        TEXT method
        TEXT reversal_of_payment_id FK
        TEXT cancellation_reason
    }
    audit_log {
        TEXT id PK
        TEXT branch_id FK
        TEXT entity_name
        TEXT entity_id
        TEXT action
        TEXT actor_user_id
        TEXT occurred_at
        TEXT before_json
        TEXT after_json
        TEXT reason
    }
    outbox_events {
        TEXT id PK
        TEXT branch_id FK
        TEXT aggregate_type
        TEXT aggregate_id
        TEXT event_type
        INTEGER sequence
        TEXT payload
        TEXT published_at
        INTEGER retry_count
    }
```

`UK` marks a unique index. Composite unique indexes are used where a code is unique only inside a branch (for example `branch_id + code` for `tests`). The sample barcode is unique across all branches, so a scanner never finds two matches before synchronization.

## Deliberate design choices

1. **Order, Sample, Test and Result are separate entities.** This follows the research conclusion and supports panels, price snapshots, invoices, and specimen traceability.
2. **`result_versions` is a separate append-only table.** A correction inserts a new version with a mandatory `reason`, `changed_by_user_id`, and `changed_at`; earlier rows are never overwritten. `results.current_version` points at the latest one.
3. **Status columns are free text with no hard-coded values.** The vocabulary and allowed transitions are a business and clinical policy to be approved, not invented here.
4. **Reference ranges are configuration, not data.** `age_unit`, `sex`, and bound columns exist; no value is filled in. Missing context must later produce a recorded "range not applied" reason rather than a silent substitution.
5. **User identifiers on `results`, `reference_ranges`, `audit_log`, and `payments` remain logical references.** Authentication now exists, but these columns are only populated by later workflow services; adding foreign keys before those write paths exist would add no safety.
6. **Three extra tables were necessary.** `user_roles`, `panel_items`, and `outbox_events` are not named in the road map entity list, but the first two are required joins that must carry the same UUID and audit columns, and the third is explicitly required by ADR-0002 for sync readiness.
7. **Critical limits are columns only.** `critical_lower` and `critical_upper` store approved limits later; the system must never derive or guess them.

## Open questions for the owner

1. ~~Are roles branch-scoped, global, or both?~~ → Branch-scoped, approved in ADR-0003.
2. ~~Can one user hold several roles?~~ → Yes; effective permissions are the union, approved in ADR-0003.
3. What age unit should be the canonical stored form for reference ranges? The column allows an explicit unit, but no default is chosen.
4. Should a panel item inherit a single specimen type, or can a panel contain tests with different specimen types? This affects sample generation in Phase 2.
5. Are refunds represented only as reversing payments, or as a separate financial document? The schema currently models reversal only.
6. How should the first manager account be bootstrapped? No account or password is seeded in source control.

## Verification

`backend/tests/EgyptLab.LIS.Infrastructure.Tests/SchemaInvariantTests.cs` applies all real migrations to an empty SQLite database and asserts: every table has the six required base columns, named soft-delete and branch filters exist everywhere, all money columns are `long`/`INTEGER`, no floating-point money or seed data exists, all foreign keys restrict deletion, result versions require a reason, and sample barcodes are globally unique. `BranchIsolationTests.cs` proves that each branch sees only its own rows and that an unscoped query fails closed.
