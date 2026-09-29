Testman-Format-Version: 1

# Login tests

> V0.1の正式形式に準拠したサンプル。規則自体は `docs/test-format.md` を参照すること。

## Overview

Verify that an active user can log in with valid credentials and that invalid or incomplete credentials are rejected without exposing account information.

## Preconditions

- The user `alice@example.com` exists and is active.
- Use the standard test account. Do not record its password in this specification.

## Common steps

1. Open the login page.
2. Enter `alice@example.com` in the email field.
3. If the row-specific Steps cell contains instructions, follow them.
4. Submit the form.

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-1 | - | - | Valid credentials | Enter the valid password. | The entered email address is visible, the password is masked, and the dashboard is displayed. |
| TC-2 | - | - | Invalid password | Enter an invalid password. | The entered email address is visible, the password is masked, an authentication error is displayed, and the dashboard is not displayed. The message does not reveal whether the account exists. |
| TC-3 | - | - | Blank password |  | A validation error is displayed and the dashboard is not displayed. |
