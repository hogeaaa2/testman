---
name: add-feature
description: Implement a testman application, Web, persistence, or CLI feature from approved specifications without inventing missing product behavior.
---

# Add Feature

Use this skill when implementing testman application code, including specification parsing, Web UI, persistence, migrations, and management CLI commands.

## Specification Authority

Always read `docs/requirements.md`, the detailed specification for the affected area, and every relevant Accepted ADR. Do not treat Proposed ADRs, candidate documents, comparison records, or existing implementation as authority over those documents.

Route work to the current sources instead of copying their details into code assumptions:

- Application structure and technology choices: `docs/decisions/ADR-001-web-application-architecture.md`
- Test specification grammar, validation, compatibility, and safe rendering: `docs/test-format.md` and `docs/decisions/ADR-003-test-specification-format.md`
- Web behavior: `docs/web-ui.md`
- Persistence and result history: `docs/database.md`
- CLI behavior: `docs/cli.md`
- Unresolved product decisions: `docs/open-questions.md`

Files under `testspecs/candidates/` and test-format comparison documents are examples or decision history, not normative specifications.

If behavior needed for implementation is absent or contradictory, stop that part of the work and report:

```text
SPEC-QUESTION:
<question>
```

## Implementation Constraints

- Preserve the approved .NET 10 Razor Pages architecture, SQLite persistence, and localhost-only operating boundary unless a later Accepted ADR changes them.
- Keep HTTP/UI, application logic, parsing, and persistence responsibilities separable.
- Do not let page handlers or CLI parsing become the only home of business rules.
- Read current test specifications directly from the Markdown files; do not import or duplicate specification bodies into SQLite.
- Treat the specification format version as the parser-format marker defined by `docs/test-format.md`, not as a content revision number.
- Parse and diagnose specifications at the granularity required by `docs/test-format.md`; do not turn one invalid title or row into an unstated whole-file failure.
- Sanitize both Markdown-generated HTML and input raw HTML with the approved allowlist before rendering. Localhost-only operation is not permission to bypass sanitization.
- Bind a saved result only to a valid, unambiguous Test ID. Never silently choose among duplicate or invalid IDs.
- Preserve existing test-run history, including results for specifications or Test IDs later removed.
- Record the Git commit SHA required by the persistence specification; use Git history rather than storing historical specification bodies in SQLite.
- Accompany schema changes with migrations.
- Avoid new dependencies unless they have a concrete benefit and fit the approved architecture.

## Verification

Select tests from the affected specification rather than applying a fixed checklist blindly. At minimum, cover the approved happy path and the relevant failure boundaries, including malformed specification structure, duplicate or invalid IDs, partial display, persistence failures, history preservation, and unsafe Markdown or HTML when those areas are touched.

Run the full build and test suite before handoff. If the repository does not yet contain an executable solution or test suite, report that fact rather than claiming verification.

Report changed behavior, verification results, and any unresolved questions.
