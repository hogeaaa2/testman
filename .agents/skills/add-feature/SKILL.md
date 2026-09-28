---
name: add-feature
description: Implement a testman Web or CLI feature from approved specifications without inventing missing product behavior.
---

# Add Feature

Use this skill when implementing a Web feature or management CLI command.

## Preconditions

Read `docs/requirements.md`, the relevant detailed specification, and Accepted ADRs. Do not treat Proposed ADRs as implementation authority.

If behavior needed for implementation is absent or contradictory, stop that part of the work and report:

```text
SPEC-QUESTION:
<question>
```

## Implementation Constraints

- Keep HTTP/UI, application logic, parsing, and persistence responsibilities separable.
- Do not let page handlers or CLI parsing become the only home of business rules.
- Encode or sanitize specification content before rendering it.
- Preserve existing test-run history.
- Accompany schema changes with migrations.
- Avoid new dependencies unless they have a concrete benefit and fit the approved architecture.

## Verification

Test the approved happy path, invalid input, unknown IDs, persistence failures, repeated execution, and any security-sensitive rendering. Run the full build and test suite before handoff.

Report changed behavior, verification results, and any unresolved questions.
