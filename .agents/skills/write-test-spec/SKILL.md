---
name: write-test-spec
description: Create or revise Markdown test specifications that testman can read, using the approved format and the target system's agreed behavior. Use for authoring test cases, not for changing the format or implementing testman.
---

# Write Test Specification

Use this skill to write or revise test specifications consumed by testman. This skill does not decide the test specification format; use `define-test-format` when the format itself needs to change.

## Sources

- Read `docs/test-format.md` and the relevant Accepted format ADR before editing. Treat them as the authority for syntax, structure, and Test ID rules. Do not use candidate files or parser behavior to override them.
- Derive test intent, steps, and expected results from the user's request and agreed specifications for the system under test. When the system under test is testman, follow the source priority in `AGENTS.md`.
- Inspect the destination file and related test specifications to preserve existing scope, terminology, and test intent. Examples are writing aids, not authority for product behavior.

## Authoring

- Make the test scope clear in each title's Overview before detailing its cases. Write Preconditions and Common steps so that every case under the title can be executed consistently; use `なし` when a required section has no applicable content.
- In Preconditions, identify the test data, initial state, permissions, and environment details needed to reproduce the result. Put case-specific setup in the row's Steps when it does not apply to every case.
- Write Common steps as an executable sequence and make clear where row-specific Steps fit into it. A tester should be able to run a case using its title's shared instructions and that case's row, without consulting another case's row.
- If Common steps accumulates case-specific branches, move those actions into the relevant rows' Steps so the shared sequence remains straightforward to follow.
- Give each row one test intent with a verifiable Expected result. Use row-specific Steps where cases differ. Keep the approved headings, table columns, order, escaping, and file version marker exactly as defined in `docs/test-format.md`.
- Specify literal input values when they matter to the test; if the tester may choose a value, state the rule for choosing it. Distinguish placeholders from text to enter literally. When testing acceptance of a numeric range, include its minimum, maximum, and a distinct interior value near the midpoint. If fewer than three distinct values are allowed, cover every allowed value and explain why a separate midpoint case is impossible. Ask when the bounds or usable values are not defined rather than inventing them.
- Review coverage against the agreed behavior, including relevant ordinary, boundary, and rejection or failure cases. When factors have too many combinations for exhaustive coverage, consider pairwise coverage or a risk-based selection and explain the chosen scope in the Overview. Do not claim exhaustive or pairwise coverage without checking the actual cases, and do not invent behavior or combinations solely for coverage counts.
- Before filling Major item, Middle item, and Minor item, identify the inputs or conditions in Common steps that vary between cases. When cases cover combinations of these factors, use the classification cells to show the factor values for each case so the pattern is visible from the table. Keep the meaning of each populated column consistent within a title and arrange factors from broader grouping to finer distinction. For example, a set of login cases might use `ユーザー状態: 有効` and `パスワード: 誤り` as classification values, while Steps explain how to enter the credentials and Expected result states the observable outcome.
- Do not force a factor into every classification column or invent combinations solely to fill the table. Use the format's `-` for a level with no useful classification. Keep procedural details in Steps and outcomes in Expected result rather than using classification cells as substitutes for them.
- Group rows so factor values and case differences are easy to compare, unless a required execution order takes precedence. Use the system's actual screen and field names consistently; avoid context-dependent references such as `これ`, `それ`, `そこ`, or `あそこ` when they leave the object unclear.
- In Expected result, do not use `正しい` or similar judgments such as `正常`, `適切`, `問題ない`, or `期待どおり` as acceptance criteria. State what a tester can observe: the exact message or value when specified, a visible state change, a saved record, or an explicitly absent outcome. A judgment word may appear only when it is part of an exact product message being checked, not as the writer's assessment.
- State when and where the tester should observe an Expected result when that affects the pass/fail decision. For an absence, make the observation scope clear. For asynchronous behavior, name the observable completion condition instead of saying `少し待つ` or using an arbitrary sleep; do not invent a timeout or other behavior that the agreed specification does not define.
- If a case changes state that can affect later cases, explain how to restore the required initial state or state the intended execution order and dependency. Place shared setup or reset instructions in Preconditions or Common steps and case-specific actions in Steps. Do not silently assume cases are independent.
- Preserve an existing Test ID while its test intent remains the same, including after a file rename. For a new ID, inspect the file's current content and available Git history to find the highest number ever used in that file; do not reuse removed IDs or gaps. Moving a case to another file requires a new ID in the destination.
- Do not turn unclear product behavior into an asserted expected result. Ask for the missing decision, or present a clearly labeled proposal outside the specification. Do not add unapproved behavior to the final test specification.
- Avoid real passwords, tokens, personal data, and other secrets in test specifications. Use safe placeholders or refer to a separately managed test account when the scenario needs credentials.

## Check and Handoff

- Check the finished file against `docs/test-format.md`, including required nonempty sections, one table per title, exact columns, nonempty required cells, valid unique IDs, and Markdown table escaping. Check that meaningful case variations appear in the classification cells. Read each case as a tester would, checking that its setup, action sequence, observation, and effect on later cases are clear; replace vague judgments with observable pass conditions.
- When a testman parser or preview is available, use it to check diagnostics and confirm that each intended title and case appears. Report any validation that could not be run; do not claim that visual or parser checks passed without performing them.
- Summarize the added or revised test intents, the source for expected behavior, the ID range used, and any unresolved questions. Do not commit unless the user explicitly asks.
