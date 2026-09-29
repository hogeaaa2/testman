# Login tests

> Comparison sample only. This file does not define the accepted test specification format.

## Overview

Verify that an active user can log in with valid credentials and that invalid or incomplete credentials are rejected without exposing account information.

## T-AUTH-001: Valid login

### Preconditions

- The user `alice@example.com` exists and is active.

### Steps

| Step | Action | Expected result |
|---:|---|---|
| 1 | Open the login page. | The login form is displayed. |
| 2 | Enter `alice@example.com` and the valid password. | The entered email address is visible and the password is masked. |
| 3 | Submit the form. | The dashboard is displayed. |

### Notes

Use the standard test account. Do not record its password in this specification.

## T-AUTH-002: Login with an invalid password

### Preconditions

- The user `alice@example.com` exists and is active.

### Steps

| Step | Action | Expected result |
|---:|---|---|
| 1 | Open the login page. | The login form is displayed. |
| 2 | Enter `alice@example.com` and an invalid password. | The entered email address is visible and the password is masked. |
| 3 | Submit the form. | An authentication error is displayed and the dashboard is not displayed. |

### Notes

Confirm that the message does not reveal whether the account exists.

## T-AUTH-003: Login with a blank password

### Preconditions

- The user `alice@example.com` exists and is active.

### Steps

| Step | Action | Expected result |
|---:|---|---|
| 1 | Open the login page. | The login form is displayed. |
| 2 | Enter `alice@example.com` and leave the password blank. | The entered email address is visible and the password field is blank. |
| 3 | Submit the form. | A validation error is displayed and the dashboard is not displayed. |
