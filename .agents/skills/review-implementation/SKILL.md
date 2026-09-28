---
name: review-implementation
description: Review testman's implementation against approved specifications, with emphasis on Web safety, parsing, migrations, and result-history preservation.
---

# Review Implementation

Use this skill for an independent review after implementation or a substantial fix.

## Sources

Review against `docs/requirements.md`, relevant detailed specifications, and Accepted ADRs. Proposed decisions are not requirements.

## Checks

- No undocumented behavior fills an unresolved specification gap.
- Test-spec parsing matches the approved format and produces useful diagnostics.
- Specification content cannot inject unsafe HTML or script.
- Result submission validates inputs and does not report false success.
- Repeated executions append history rather than overwrite it.
- Specification changes, deletion, and re-import follow documented behavior.
- Schema matches documentation and every change has a migration.
- Tests cover happy paths, malformed specifications, duplicates, unknown IDs, database failures, and relevant Web security cases.
- Runtime DBs, secrets, and generated files are not tracked.

## Output

Write a report under `reports/`. For each finding, include severity, affected location, violated requirement, impact, and a concrete reproduction or explanation.

Use:

- Critical for data loss, serious security exposure, or a broken primary workflow.
- Major for requirement violations or substantial missing coverage.
- Minor for limited defects, maintainability, or documentation issues.

Do not modify implementation as part of the review unless explicitly asked.
