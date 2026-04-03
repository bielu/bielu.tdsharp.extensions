# 🔐 Auth Channels

TDLib clients must complete an authorization flow before they can interact with Telegram. The flow is a state machine that transitions through several states. Your application subscribes to state change events and responds by providing the required data (phone number, authentication code, 2FA password).

## Overview

| Channel | Direction | Summary |
|---|---|---|
| [`telegram/auth/check-code`](#telegramauthcheck-code) | ⬆️ Publish | Submit authentication code |
| [`telegram/auth/check-password`](#telegramauthcheck-password) | ⬆️ Publish | Submit 2FA password |
| [`telegram/auth/log-out`](#telegramauthlog-out) | ⬆️ Publish | Log out from Telegram |
| [`telegram/auth/set-phone-number`](#telegramauthset-phone-number) | ⬆️ Publish | Provide phone number for authentication |
| [`telegram/auth/state-changed`](#telegramauthstate-changed) | ⬇️ Subscribe | Subscribe to authorization state transitions |

---

## `telegram/auth/check-code`

Submit the authentication code received from Telegram.

**Direction:** ⬆️ Publish (outgoing request)  
**Payload:** `checkAuthenticationCode`

Sends the authentication code to TDLib. If the code is correct and no 2FA password is set, the authorization state transitions to Ready.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `code` | `string` |  |

---

## `telegram/auth/check-password`

Submit the two-factor authentication password.

**Direction:** ⬆️ Publish (outgoing request)  
**Payload:** `checkAuthenticationPassword`

Sends the two-factor authentication password to TDLib. Required when the account has a cloud password set.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `password` | `string` |  |

---

## `telegram/auth/log-out`

Log out from the Telegram session. The session is terminated permanently on the server.

**Direction:** ⬆️ Publish (outgoing request)  
**Payload:** `logOut`

Sends a logout request to TDLib. The authorization state transitions to LoggingOut → Closing → Closed. The session is permanently invalidated.

---

## `telegram/auth/set-phone-number`

Send the user's phone number to begin the authentication flow.

**Direction:** ⬆️ Publish (outgoing request)  
**Payload:** `setAuthenticationPhoneNumber`

Sends a phone number to TDLib to initiate the login process. After sending, the authorization state will transition to WaitCode.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `phoneNumber` | `string` |  |
| `settings` | `object` |  |

---

## `telegram/auth/state-changed`

Authorization state change events. TDLib transitions through multiple states during authentication: WaitTdlibParameters → WaitPhoneNumber → WaitCode → WaitPassword → Ready. Applications must handle each state to complete login.

**Direction:** ⬇️ Subscribe (incoming event)  
**Payload:** `authorizationState`

Receive notifications when the TDLib client's authorization state changes. The payload is a TdApi.AuthorizationState subclass indicating the new state. States include: AuthorizationStateWaitTdlibParameters, AuthorizationStateWaitPhoneNumber, AuthorizationStateWaitCode, AuthorizationStateWaitPassword, AuthorizationStateReady, AuthorizationStateLoggingOut, AuthorizationStateClosing, AuthorizationStateClosed.

