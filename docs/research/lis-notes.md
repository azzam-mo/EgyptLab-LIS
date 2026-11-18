# LIS Reference Research Notes

Status: Draft — OpenELIS complete; SENAITE and BLIS/LabBook/Bahmni research in progress.
Date: 2026-11-18
Purpose: understand architecture, data models, and workflows before Phase 0.3 (database design). These projects are for learning only. No source code has been copied.

## How to read this document

- Only facts with a cited source are treated as verified.
- Items marked **Unverified** were not confirmed from primary sources.
- A gap means "not verified in the sources reviewed", not proof that the feature cannot exist.

---

## 1. OpenELIS Global 2

Repository: https://github.com/DIGI-UW/OpenELIS-Global-2 (redirected from `I-TECH-UW/OpenELIS-Global-2`)

### License, release and maintenance

| Item | Verified value |
| --- | --- |
| License | MPL-2.0 |
| Latest release | 3.3.0, published 2026-10-20 |
| Previous release | 3.2.3.0, 2026-09-26 |
| Last `develop` commit | `cd4bfc6`, 2026-11-18, translation update |
| Maintenance | Not archived; `pushed_at` 2026-11-18 |

Sources: [repository metadata](https://api.github.com/repos/DIGI-UW/OpenELIS-Global-2), [releases](https://api.github.com/repos/DIGI-UW/OpenELIS-Global-2/releases?per_page=5), [recent commits](https://api.github.com/repos/DIGI-UW/OpenELIS-Global-2/commits?sha=develop&per_page=3), [3.3.0 release](https://github.com/DIGI-UW/OpenELIS-Global-2/releases/tag/3.3.0).

### 1.1 Order → Sample → Test → Result model and states

Statuses are database-backed (`status_of_sample`) and resolved by `StatusService`, not only UI labels.

- **Order:** `Entered`, `Started`, `Finished`, `NonConforming_depricated`; database labels include `Test Entered`, `Testing Started`, `Testing finished`, `NonConforming`. External orders also use `Entered`, `Cancelled`, `Realized`, `NonConforming`, `AwaitingSpecimen`.
- **Sample:** `SampleRejected`, `Entered`, `Canceled`, `Disposed`. `collectionDate`, `receivedDate`, and `releasedDate` are separate fields rather than a dedicated "received" state.
- **Test/Analysis:** `SampleRejected`, `NotStarted`, `Canceled`, `TechnicalAcceptance`, `TechnicalRejected`, `BiologistRejected`, `NonConforming_depricated`, `Finalized`, `RejectedByReferenceLab`.
- **Result:** no independent result status in `StatusService`; result follows the `Analysis` status. It has its own `validatedDate` for partial validation and fields such as `value`, `resultType`, `minNormal`, `maxNormal`, `isReportable`, `analyte`, `testResult`, `parentResult`, and `analyzerReportedValue`.

Documented flow: entering a result and saving moves the analysis to `TechnicalAcceptance` and makes it read-only; biological validation moves it to `Finalized`. Rejection uses `TechnicalRejected`, `BiologistRejected`, `SampleRejected`, or `RejectedByReferenceLab`. **Unverified:** the complete transition graph and every rule for reopening a finalized result.

Sources: [`StatusService.java`](https://raw.githubusercontent.com/DIGI-UW/OpenELIS-Global-2/develop/src/main/java/org/openelisglobal/common/services/StatusService.java), [`Analysis.java`](https://raw.githubusercontent.com/DIGI-UW/OpenELIS-Global-2/develop/src/main/java/org/openelisglobal/analysis/valueholder/Analysis.java), [`Sample.java`](https://raw.githubusercontent.com/DIGI-UW/OpenELIS-Global-2/develop/src/main/java/org/openelisglobal/sample/valueholder/Sample.java), [`Result.java`](https://raw.githubusercontent.com/DIGI-UW/OpenELIS-Global-2/develop/src/main/java/org/openelisglobal/result/valueholder/Result.java).

**Lesson for EgyptLab:** keep order, specimen, analysis and result as distinct entities with explicit statuses. Store event timestamps separately for collection, receipt and release rather than encoding everything in one enum.

### 1.2 Reference ranges by age and sex

`ResultLimit` fields include `testId`, `resultTypeId`, `gender`, `minAge`, `maxAge`, `lowNormal`, `highNormal`, `lowCritical`, `highCritical`, `lowValid`, `highValid`, `lowReportingRange`, `highReportingRange`, `dictionaryNormalId`, `alwaysValidate`, `componentId`, and `sampleTypeId`.

Selection rules in `ResultLimitServiceImpl`:

1. Component-specific ranges are used for multi-component tests.
2. A range tied to a sample type beats a common range (`null`); no range is borrowed from another sample type.
3. If sex and birth date are available, the order is age+sex, then age-only for sex-neutral ranges, then sex-only, then default.
4. If sex or birth date is missing, no range is borrowed from another sex or age band; a neutral range is used or a `RangeNotAppliedReason` is returned.
5. Ages are stored in days; default bounds are `minAge = 0`, `maxAge = infinity`.
6. Numeric ranges are separate from dictionary/list ranges (`dictionaryNormalId`).

Sources: [`ResultLimit.java`](https://raw.githubusercontent.com/DIGI-UW/OpenELIS-Global-2/develop/src/main/java/org/openelisglobal/resultlimits/valueholder/ResultLimit.java), [`ResultLimitServiceImpl.java`](https://raw.githubusercontent.com/DIGI-UW/OpenELIS-Global-2/develop/src/main/java/org/openelisglobal/resultlimit/service/ResultLimitServiceImpl.java).

**Lesson for EgyptLab:** a reference range needs the patient context (age in a defined unit, sex, component, specimen type). A range must not be silently substituted when required context is missing. Critical limits can live beside normal limits.

### 1.3 Correction after approval or release

- 3.3.0 adds a **Report Print Queue**. Each released order has `Partial`, `Final`, or `Amended` status plus print status.
- Each print is a numbered report version with an archived PDF and `SHA-256`, print events, `issuedByUserId`, and `printedAt`.
- A changed result after release creates an `Amended` version with a `changeSummary`; the patient report shows a correction marker.
- `Sample` carries `amendsLhuNumber`, `amendmentNumber`, `amendmentReason`; `Analysis` carries `correctedSincePatientReport`.
- `History`/`AuditTrail` and `ElectronicSignature`/`ResultSignature` exist. Electronic signature shipped in 2026-04 but is disabled by default and has deferred items.

**Unverified:** exact mechanism to reopen a `Finalized` result, approval ordering, and whether versioning is per result or per report.

Sources: [3.3.0 notes](https://uwdigi.atlassian.net/wiki/spaces/oeg/pages/1726316549/OpenELIS+Global+3.3.0+October+2026), [`ReportVersion.java`](https://raw.githubusercontent.com/DIGI-UW/OpenELIS-Global-2/develop/src/main/java/org/openelisglobal/reportqueue/valueholder/ReportVersion.java), [`ReportPrintEvent.java`](https://raw.githubusercontent.com/DIGI-UW/OpenELIS-Global-2/develop/src/main/java/org/openelisglobal/reportqueue/valueholder/ReportPrintEvent.java), [electronic signature spec](https://raw.githubusercontent.com/DIGI-UW/OpenELIS-Global-2/develop/specs/301-electronic-signature/spec.md).

**Lesson for EgyptLab:** immutable released reports with numbered versions, a change summary, and a visible correction marker match our "approved result never edited" rule. Do not depend on an optional, disabled-by-default signature feature for safety.

### 1.4 Analyzer connectivity

- A separate `DIGI-UW/openelis-analyzer-bridge` listens on `TCP:5001` and forwards to `/analyzer/astm`.
- Protocols: ASTM LIS2-A2 (default), HL7 v2.3.1, HL7 v2.5, and `FILE` import for CSV/TSV/Excel.
- ASTM listeners: `E1381-95` on ports `12000` and `12010`. HL7 MLLP listener on `2575` only when `MLLP_ENABLED=true`.
- Analyzer states: `Inactive`, `Setup`, `Validation`, `Active`, `Error Pending`, `Offline`; only the first three are manually set.
- Error dashboard categories: `Connection`, `Protocol`, `Mapping`, `Validation`, `Timeout` with `Critical`, `Error`, `Warning` severities.
- 3.3.0 added per-analyzer foundations and certificate coupling with the bridge.

Sources: [analyzer management](https://uwdigi.atlassian.net/wiki/spaces/oeg/pages/1186365449), [bridge installation](https://uwdigi.atlassian.net/wiki/spaces/oeg/pages/1669758983), [integration FAQ](https://uwdigi.atlassian.net/wiki/spaces/oeg/pages/1430650885/DRAFT+Analyzer+Integration+Frequently+Asked+Questions).

**Lesson for EgyptLab:** keep instrument communication outside the core domain behind adapters. Provide explicit analyzer states, mapping drafts, retries, and an error dashboard. Do not choose a protocol library before the lab supplies actual instrument models and interface manuals.

### 1.5 Gaps relative to EgyptLab

- **Billing/accounting:** no local accounting module verified. The documented integration is Odoo via XML-RPC (`OdooIntegrationService`), creating partners and invoices. The exact trigger is inconsistent between docs and code comments. **Unverified.**
- **Multi-branch/offline:** 3.3.0 adds `Locations & Organizations` and central-server configuration, but the offline installation guide still points to old packages without a database image. The marketing claim of offline-first architecture is **unverified**; accounting/inventory-style multi-branch support is **unverified**.
- **WhatsApp/SMS/email:** senders existed before 3.3.0; 3.3.0 added result delivery channels with an outbox, retries, and a `Sent Messages` log.
- **Egyptian specifics:** an Arabic translation file exists, but no `ar_EG`, Egyptian national-ID validation, insurance, Ministry of Health, or local regulatory integration was verified. Absence in reviewed sources is not proof of absence in the product.

Sources: [`OdooIntegrationService.java`](https://raw.githubusercontent.com/DIGI-UW/OpenELIS-Global-2/develop/src/main/java/org/openelisglobal/odoo/service/OdooIntegrationService.java), [offline install](https://uwdigi.atlassian.net/wiki/spaces/oeg/pages/1711636518/Offline+install+of+OpenELIS+Global), [3.3.0 notes](https://uwdigi.atlassian.net/wiki/spaces/oeg/pages/1726316549), [features page](https://openelis-global.org/features-and-functionality/).

---

## 2. BLIS, LabBook, Bahmni/OpenMRS

### 2.1 BLIS (C4G / Georgia Tech / CDC)

**Minimal model and workflow:** core entities are `Patient`, `Specimen`, `Test`, and `Results`. There is no separate `Order` table; specimen registration effectively acts as the test order. `specimen.patient_id` links to `patient.patient_id`, and `test.specimen_id` links to `specimen.specimen_id`; `specimen_test` defines allowed test types per specimen type. Results live in `test.result` with `verified_by` and `date_verified`. Workflow: register patient once → register specimen from the patient file → enter results singly, in batches, or via worksheet → verify results → print reports and view patient history, with daily review logs.

Sources: [glossary](https://github.com/C4G/BLIS/blob/main/docs/user_guide/98_glossary.md), [technician guide](https://github.com/C4G/BLIS/blob/main/docs/user_guide/40_technician_overview.md), [database schema](https://github.com/C4G/BLIS/blob/main/db/structure/blis_12.sql), [introduction](https://github.com/C4G/BLIS/blob/main/docs/user_guide/00_introduction.md).

**Lesson for EgyptLab:** the smallest useful LIS can omit a separate order entity, but EgyptLab needs one because orders carry pricing, panels, and invoices. Keep patient → specimen → test → result linkage explicit.

**License, release, maintenance:**

| Item | Verified value |
| --- | --- |
| License | Not formally resolved. `C4G/BLIS` has no accepted GitHub license file and embeds GPL-2.0 texts; `C4G/BLIS-NG` is GPL-3.0. External sources classify BLIS as GPL. Legal review required before redistribution. |
| Latest release | `v4.0.0.beta1` prerelease, 2026-03-18; `v3.94.beta.2` in docs and 2025-05-28; older `v3.8.1` download on the project site. |
| Maintenance | Active; last commit 2026-11-16, "Updated French locale". |

Sources: [BLIS repository](https://github.com/C4G/BLIS), [BLIS-NG](https://github.com/C4G/BLIS-NG), [LIMSWiki](https://limswiki.org/index.php/C4G_BLIS), [releases](https://github.com/C4G/BLIS/releases), [commits](https://github.com/C4G/BLIS/commits/main).

### 2.2 LabBook (Fondation Mérieux)

**Quality and audit:** LabBook has an ISO 15189-oriented Quality module covering laboratory organisation, staff and training records, prescribers, internal/external quality control, non-conformities with restricted handling roles, laboratory meetings with minutes, equipment and storage management, and software validation files. Since 3.6 it has an audit log recording login/logout, user operations, test requests, resource access, and parameter/data changes. Records include UTC timestamp, user, role, resource, action type, IP address, and `SUCCESS/FAILURE`, with archiving and purging. Default access is `root`; other roles must be granted explicitly.

Sources: [quality manual](https://www.lab-book.org/wp-content/uploads/2026/02/labbook-3-6-manual-quality.pdf), [audit-log manual](https://www.lab-book.org/wp-content/uploads/2026/02/labbook-3-6-manual-audit-log.pdf), [ISO 15189 validation guide](https://www.lab-book.org/wp-content/uploads/2026/02/labbook-3-6-manual-technical-guide-for-validation-for-sil.pdf), [3.6 resources](https://www.lab-book.org/en/resources).

**Lesson for EgyptLab:** audit events should carry user, role, branch/resource, UTC time, IP, action type and outcome. Default-deny role access and explicit grants align with the backend-permission rule.

**License, release, maintenance:**

| Item | Verified value |
| --- | --- |
| License | GPL-2.0 |
| Release | 3.6 announced 2026-03-26; latest tag `v3.6.28` on 2026-10-21; untagged 3.6.31/3.6.32 commits in November 2026. |
| Maintenance | Active; last commit 2026-11-17, "Release 3.6.32". |

Sources: [repository](https://github.com/fondationmerieux/labbook_python), [tags](https://github.com/fondationmerieux/labbook_python/tags), [commits](https://github.com/fondationmerieux/labbook_python/commits/master), [release announcement](https://www.fondation-merieux.org/en/latest/our-news/new-version-of-the-labbook-laboratory-data-management-system).

### 2.3 Bahmni / OpenMRS

**Order and result linkage to the patient record:** Bahmni creates patients and lab orders in OpenELIS, then returns results and referral information to the OpenMRS-based EMR. Technically an order is stored as an `Order` inside an `Encounter` within a `Visit` in OpenMRS; Atom Feed events are ingested by OpenELIS using the `encounter UUID` as `accession UUID`, avoiding a mapping table. Returned results are written as an `Obs` tree linked to the order, and a FHIR `DiagnosticReport` moves from `PRELIMINARY` to `FINAL` when all tests are complete. `labOrderResults` is queried by `visitUuids` or `patientUuid`. Bahmni Lite 2.0 moved to React and FHIR R4.

Sources: [laboratory page](https://www.bahmni.org/laboratory), [order docs](https://bahmni.atlassian.net/wiki/spaces/BAH/pages/5458886657/Investigations+and+Procedure+Orders), [integration notes](https://github.com/A1i-Tabatabaei/bahmni-architecture-notes/blob/master/docs/05-integration-and-events.md), [order-to-result flow](https://github.com/A1i-Tabatabaei/bahmni-architecture-notes/blob/master/docs/13-flow-lab-order-to-result.md), [Bahmni Lite 2.0](https://talk.openmrs.org/t/bahmni-lite-2-0-release/50199).

**Lesson for EgyptLab:** a stable shared identifier can link systems without a mapping table. Preliminary/final report states are a useful pattern, but EgyptLab must not use them to bypass explicit approval.

**License, release, maintenance:**

| Item | Verified value |
| --- | --- |
| License | Multi-component: Bahmni ranges from AGPL-3.0 to MPL-2.0 (with medical disclaimer) and MIT, with migration from AGPL to MPL in progress; OpenMRS MPL-2.0; OpenELIS AGPL-3.0; Odoo Community LGPL-3.0; JasperReports LGPL; dcm4chee tri-licensed. `openmrs-module-bahmniapps` is MPL-2.0. |
| Release | Bahmni Lite 2.0 announced 2026-10-05; `openmrs-module-bahmniapps` 2.0.0 released 2026-09-15 as "MPL v2 Release". Bahmni Standard 2.0 status unverified. |
| Maintenance | Active: `openmrs-core` 2026-11-17, `openmrs-module-bahmniapps` 2026-11-16, `bahmni-docker` 2026-11-04, `Bahmni/OpenElis` 2026-10-07. |

Sources: [license page](https://www.bahmni.org/license), [module license](https://github.com/Bahmni/openmrs-module-bahmniapps/blob/master/LICENSE), [2.0.0 release](https://github.com/Bahmni/openmrs-module-bahmniapps/releases/tag/2.0.0).

### 2.4 Gap matrix against EgyptLab

| Project | Billing/accounting | Multi-branch / offline-first | WhatsApp / SMS | Egyptian market specifics |
| --- | --- | --- | --- | --- |
| **BLIS** | Basic billing tables (`bills`, `payments`, `test_type_costs`) and billing workflows exist. No general ledger, tax, or fiscal-year evidence. **Unverified.** | Strongest local-first example: local server, one-way backup to BLIS Cloud when internet is available, Director role aggregating reports from several labs, satellite-lab support. Not real-time two-way sync. | No WhatsApp/SMS support found. **Unverified.** | Only `en` and `fr` language data found; no Arabic, Egyptian tax, or insurance rules. **Unverified.** |
| **LabBook** | Built-in billing and invoice generation with ODT templates and billing-state automation; no full general ledger evidence. **Unverified.** | Local ISO installation, single or multi-PC inside one lab, server mode, and LabBook Lite Android for remote sites without permanent internet. No central multi-branch management evidence. **Unverified.** | Direct result delivery via WhatsApp and email in 3.6, with SMTP and Mailjet; no SMS found. **Unverified.** | Arabic UI exists; no Egyptian VAT, insurance, or Ministry of Health reporting rules found. **Unverified.** |
| **Bahmni/OpenMRS** | Strongest: Odoo provides tax, discounts, debtors, financial years, payments, patient financial records, and accounting reports; lab orders can become quotes. | `Location` concept and location switcher exist, but core is central web. Offline repos exist; `Bahmni Connect` is deprecated with last push in 2023. **Partial.** | `whatsapp-bahmni-service` uses WhatsApp Cloud API for appointment booking, not lab result delivery; no integrated SMS for results found. **Unverified.** | Community RTL Arabic distribution exists and Transifex Arabic is around 18%; an MSF Egypt request was recorded in 2019, but no Egyptian billing/lab rules are integrated. **Unverified.** |

Sources: [BLIS schema](https://github.com/C4G/BLIS/blob/main/db/structure/blis_12.sql), [BLIS cloud sync](https://github.com/C4G/BLIS/blob/main/docs/user_guide/13_syncing_with_blis_cloud.md), [LabBook about](https://www.lab-book.org/en/about-labbook), [LabBook resources](https://www.lab-book.org/en/resources), [LabBook networking guide](https://www.lab-book.org/wp-content/uploads/2026/02/e3-labbook-3-6-manual-networking-12112026.pdf), [Bahmni billing](https://www.bahmni.org/billing-and-accounting), [Bahmni offline](https://github.com/Bahmni/bahmni-offline), [WhatsApp service](https://github.com/Bahmni/whatsapp-bahmni-service), [Arabic RTL discussion](https://talk.openmrs.org/t/bahmni-rtl-distribution-with-arabic-language/37249).

---

## 3. SENAITE LIMS and SENAITE Health

### 3.1 License, release and maintenance

| Project | License | Latest stable / verified release | Maintenance |
| --- | --- | --- | --- |
| `senaite.core` | GPL-2.0 | 2.6.0, 2025-04-04; 2.7.0 listed as unreleased | Active; `2.x` last commit 2026-11-16 |
| `senaite.lims` | GPL-2.0 | 2.6.0 | `pushed_at` 2026-07-27 |
| `senaite.health` | GPL-2.0 | GitHub v1.2.4 (2020); PyPI 1.2.5 (2021) | Last commit 2021-08-22; developer states it is no longer supported on 2.x |
| `senaite.patient` | GPL-2.0 | 1.5.0, 2025-04-04 | Active; last commit 2026-11-16; replacement for Health on 2.x |
| `senaite.instruments` | GPL-2.0 | — | Last push 2022-05-20 |
| `senaite.astm` | License not verified | — | Active; last push 2026-10-19 |
| `senaite.sync` | GPLv3 | 1.0.1, 2019-04-12 | Very old |

Caution: an old `v3.2.0` tag in `senaite.core` belongs to the Bika era (2017) and is not a current release.

Sources: [senaite.core releases](https://github.com/senaite/senaite.core/releases/tag/v2.6.0), [PyPI](https://pypi.org/project/senaite.core/), [CHANGES.rst](https://github.com/senaite/senaite.core/blob/2.x/CHANGES.rst), [senaite.lims](https://github.com/senaite/senaite.lims), [senaite.health forum note](https://community.senaite.org/t/senaite-health-anyone/1353/2), [senaite.patient](https://github.com/senaite/senaite.patient), [senaite.instruments](https://github.com/senaite/senaite.instruments), [senaite.astm](https://github.com/senaite/senaite.astm), [senaite.sync](https://github.com/senaite/senaite.sync).

### 3.2 Order → Sample → Test → Result model and states

- In `senaite.core`, the `AnalysisRequest` content type is displayed as **Sample** and contains only `Analysis` and `ResultsReport`. There is no separate Result content type; a result is a field on `Analysis`. In SENAITE, order and sample are effectively one entity.
- `AnalysisService` is the test definition; each `Analysis` is a requested test on a sample.
- SENAITE Health 1.x extends `AnalysisRequest` with `Patient`, `Doctor`, and `ClientPatientID`, adds a `Patient` type with `Gender`, `Age`, and `BirthDate`, and uses `Batch` as a case.

Sample states: `sample_registered`, `scheduled_sampling`, `to_be_sampled`, `to_be_preserved`, `sample_due`, `sample_received`, `to_be_verified`, `verified`, `published`, `rejected`, `invalid`, `cancelled`, `dispatched`, `disposed`. Transitions include `receive`, `submit`, `verify`, `publish`, `republish`, `retract`, `rollback_to_receive`, `reject`, `invalidate`, `cancel`/`reinstate`, `detach`/`reattach`, `dispatch`/`restore`, `dispose`, and `duplicate_sample`.

Analysis states: `registered`, `unassigned`, `assigned`, `to_be_verified`, `retracted`, `rejected`, `verified`, `published`, `cancelled`, `locked`. Transitions include `assign`, `submit`, `retract`, `retest`, `reject`, `verify`, `multi_verify`, `publish`, `cancel`, and `lock`/`unlock`.

Official simplified sequence: Registered → Sample Due → Received → To be verified → Verified → Published. Generating a PDF does not by itself complete publication; `Publish` or email must happen.

Sources: [`AnalysisRequest.xml`](https://github.com/senaite/senaite.core/blob/2.x/src/bika/lims/profiles/default/types/AnalysisRequest.xml), [`Analysis.xml`](https://github.com/senaite/senaite.core/blob/2.x/src/bika/lims/profiles/default/types/Analysis.xml), [quickstart](https://www.senaite.com/docs/quickstart), [sample workflow](https://github.com/senaite/senaite.core/blob/2.x/src/senaite/core/profiles/default/workflows/senaite_sample_workflow/definition.xml), [analysis workflow](https://github.com/senaite/senaite.core/blob/2.x/src/senaite/core/profiles/default/workflows/senaite_analysis_workflow/definition.xml), [Health analysis request](https://github.com/senaite/senaite.health/blob/1.x/bika/health/content/analysisrequest.py), [Health patient](https://github.com/senaite/senaite.health/blob/1.x/bika/health/content/patient.py).

**Lesson for EgyptLab:** do not merge Order and Sample into one entity. EgyptLab needs orders for pricing, panels, invoices, and payments. Keep sample state and result approval state separate and explicit.

### 3.3 Reference ranges by age and sex

- `AnalysisSpec` stores a `ResultsRange` record per analysis keyword with `min`, `max`, `warn_min`, `warn_max`, `min_operator` (default `geq`), `max_operator` (default `leq`), and `error`. Dynamic specs add `minpanic`, `maxpanic`, and `rangecomment`.
- `AnalysisSpec` can be linked to a `SampleType` and an Excel-based `DynamicAnalysisSpec`. The adapter maps spreadsheet columns to `Analysis` or `AnalysisRequest` properties and orders multiple matches.
- Age and sex are **not built into `senaite.core`**. Health 1.x required a custom `IDynamicResultsRange` adapter reading `patient.getAge()` and `patient.getGender()`.
- Current 2.x path: `senaite.patient` 1.5.0 provides `PatientDynamicResultsRange`, supporting `MinAge`/`MaxAge` in `ymd` format and `Sex` values `f`/`m`, with age calculated from `DateOfBirth` stored on the sample and the draw date.

Sources: [`analysisspec.py`](https://github.com/senaite/senaite.core/blob/2.x/src/bika/lims/content/analysisspec.py), [dynamic range adapter](https://github.com/senaite/senaite.core/blob/2.x/src/bika/lims/adapters/dynamicresultsrange.py), [developer forum](https://community.senaite.org/t/multiple-analysis-specifications/407/5), [`senaite.patient` adapter](https://github.com/senaite/senaite.patient/blob/v1.5.0/src/senaite/patient/adapters/dynamicresultsrange.py).

**Lesson for EgyptLab:** age/sex ranges require the patient module to be designed with the range engine, not added later. Store age bands with explicit units and boundaries, and record when a range could not be applied.

### 3.4 Correction after approval or release

- Before final verification, `retract`, `retest`, resubmit, and `rollback_to_receive` are available.
- After verification/publication, the official rule is that analyses cannot simply be retracted; the sample must be invalidated (`invalidate` → `invalid`) and a new sample/test created.
- Unreleased 2.7.0 adds a sample invalidation form with reason and moves audit-log logic into core.
- Audit changes are stored as snapshots with user, IP, and timestamp. However `VERSIONABLE_TYPES` in core is limited to `Calculation`, so these are audit snapshots rather than independent per-result versions.
- No formal amendment entity named "amendment" was found in core or Health. **Unverified as an absolute absence.**

Sources: [quickstart](https://www.senaite.com/docs/quickstart), [CHANGES.rst](https://github.com/senaite/senaite.core/blob/2.x/CHANGES.rst), [features](https://www.senaite.com/features), [config.py](https://github.com/senaite/senaite.core/blob/2.x/src/bika/lims/config.py).

**Lesson for EgyptLab:** invalidation plus retest is weaker than our required immutable amendment version with a mandatory reason. Design `result_versions` explicitly instead of relying on workflow rollback.

### 3.5 Analyzer connectivity

- Core provides a file-import framework with `IInstrumentImportInterface` and `IInstrumentAutoImportInterface`, per-device parsers, manual and automatic import, override options, and allowed-state restrictions. Formats include AMPLER, CSV, XML, and TXT.
- `senaite.instruments` adds many device adapters but was last pushed in 2022.
- Active `senaite.astm` provides an `asyncio` server accepting ASTM on port `4010` and HL7 v2 over MLLP on `2575`, mapping MSH→H, PID→P, OBR→O, OBX→R, NTE→C, then pushing via `senaite.jsonapi` with configurable consumers.
- A REST JSON API covers resources and actions.

Sources: [import framework README](https://github.com/senaite/senaite.core/blob/2.x/src/senaite/core/exportimport/instruments/README.md), [features](https://www.senaite.com/features), [senaite.instruments](https://github.com/senaite/senaite.instruments/blob/master/README.rst), [senaite.astm README](https://github.com/senaite/senaite.astm/blob/2.x/README.md), [JSON API docs](https://www.senaite.com/docs/json-api).

**Lesson for EgyptLab:** the adapter boundary and the separation of transport (ASTM/MLLP) from import mapping are good patterns. Choose concrete libraries only after the lab supplies device models and interface manuals.

### 3.6 Gaps relative to EgyptLab

| Need | Verified status |
| --- | --- |
| Billing/accounting | Core has a basic invoice object, billable items, VAT, price, currency, and PDF. No general ledger, collection, or receivables evidence. **Unverified as full accounting.** |
| Multi-branch / offline-first | No built-in offline or multi-branch mode found in official docs. `senaite.sync` exists for migration/sync between instances but dates from 2019. **Unverified as absence, but weak.** |
| WhatsApp / SMS | Documented core channels are email and PDF reports; no official lab WhatsApp/SMS add-on found. **Unverified.** |
| Egyptian specifics | Arabic locale and configurable currency exist, but no Egyptian national ID, MoH forms, tax, or local accreditation rules found. **Likely local development required.** |

**Recommended SENAITE path if it were ever evaluated:** `senaite.core` 2.6/2.7 with `senaite.patient` 1.5 and `senaite.astm`; avoid `senaite.health` because it is 1.x and unsupported on 2.x. This is research only; EgyptLab's accepted stack is ASP.NET Core, not SENAITE.

---

## 4. Answers to the five required research questions

| Question | Cross-project answer |
| --- | --- |
| 1. How are Order → Sample → Test → Result represented, and what states exist? | OpenELIS separates order, sample, analysis, and result, with database-backed statuses and technical/biological validation. BLIS omits a separate Order table; the specimen acts as the order. SENAITE merges Order and Sample into `AnalysisRequest` and stores the result on `Analysis`. All three use explicit sample and analysis states. EgyptLab should keep all four entities separate because orders carry prices, panels, and invoices. |
| 2. How are reference ranges stored by age/sex? | OpenELIS has the richest model: `ResultLimit` with age in days, sex, component, sample type, normal/critical/valid/reporting bounds, and explicit failure reasons when context is missing. SENAITE core stores ranges per analysis keyword; age/sex require `senaite.patient` 1.5 dynamic ranges. LabBook and BLIS did not provide verified age/sex range details. EgyptLab should follow OpenELIS's context rules without copying its code. |
| 3. How are post-approval corrections handled? | OpenELIS 3.3 uses numbered report versions, `Amended` status, `changeSummary`, archived PDF with SHA-256, and a correction marker. SENAITE invalidates the sample and creates a new one; audit snapshots exist but not per-result versions. BLIS stores `verified_by`/`date_verified`. EgyptLab must implement immutable `result_versions` with a mandatory reason and preserved prior versions. |
| 4. How are analyzers connected? | OpenELIS uses a separate Analyzer Bridge with ASTM (default), HL7 v2.3.1/2.5, FILE import, analyzer states, and an error dashboard. SENAITE uses file-import adapters plus active `senaite.astm` for ASTM port 4010 and HL7 MLLP port 2575. EgyptLab should keep adapters outside the core and wait for actual device models before selecting libraries. |
| 5. What is missing for EgyptLab? | Across these projects: local accounting with integer piasters, true offline-first multi-branch sync, Egyptian national ID/insurance/tax/MoH rules, WhatsApp result delivery with expiring links, and full Arabic RTL reporting. LabBook has WhatsApp and strong ISO-style audit; Bahmni has the strongest accounting integration but is heavy and not offline-first; BLIS is the lightest but lacks Arabic and modern delivery. None of these gaps justifies copying another system's code. |

## 5. Design conclusions for Phase 0.3

1. **Keep Order, Sample, Test, and Result as separate entities.** This supports panels, price snapshots, invoices, and specimen traceability.
2. **Model statuses explicitly** for order, sample, result entry, review, approval, and release. Store collection/received/released timestamps as events, not as a single overloaded enum.
3. **Make reference ranges a first-class configuration** keyed by test, component, sex, age-band with explicit units, and sample type. Record why a range was not applied; never silently substitute another range.
4. **Enforce immutable approved results** with `result_versions`, mandatory reason, user, timestamp, and a report correction marker. Keep the old version unchanged.
5. **Design the outbox/event log from the first migration** because every reviewed system is weak or partial on offline multi-branch operation. Tool comparison for sync belongs to Phase 5, not here.
6. **Isolate analyzer communication behind adapters.** Do not choose HL7/ASTM libraries before receiving actual device models and interface manuals.
7. **Implement billing locally with integer piasters.** Do not depend on an external accounting system in the critical path.
8. **Treat Arabic RTL, Egyptian identity rules, and patient-facing delivery as local requirements**, not features expected from an upstream LIS.
9. **Do not copy source code.** Licenses observed include MPL-2.0, GPL-2.0/2.1/3.0, AGPL-3.0, LGPL, MIT, and unresolved BLIS licensing. These notes are architectural learning only.

---
