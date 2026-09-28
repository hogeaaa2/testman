---
name: define-test-format
description: Design or change testman's human-edited test specification format, including compatibility, parsing, and safe Web rendering decisions.
---

# Define Test Format

Use this skill when the Architect designs or changes the test specification format.

## Required Inputs

Read `docs/requirements.md`, `docs/test-format-options.md`, `docs/open-questions.md`, and the current format ADR. Inspect representative specifications supplied by the user before finalizing a format.

## Outcome

Produce a format that is comfortable to edit and review in Git, can be validated without ambiguity, and can be rendered safely in the browser.

## Decisions to Make Explicit

- File and test-case boundaries
- Required and optional fields
- Relationship between actions and expected results
- Multiline content and escaping
- Stable Test ID rules
- Duplicate and invalid-input behavior
- Allowed Markdown or HTML features
- Compatibility and versioning strategy

Do not accept arbitrary raw HTML without documenting an allowlist and sanitization policy.

## Before Acceptance

Write representative samples in each serious candidate format. Present tradeoffs and a recommendation to the user. Keep the ADR Proposed until the user approves the format.

## Outputs

- `docs/test-format.md` after approval
- Updated sample specifications
- Updated `docs/decisions/ADR-003-test-specification-format.md`
- Relevant resolved items removed from `docs/open-questions.md`
