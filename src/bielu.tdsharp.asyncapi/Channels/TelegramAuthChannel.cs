// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using Bielu.AspNetCore.AsyncApi.Attributes.Attributes;
using TdLib;

namespace bielu.tdsharp.asyncapi.Channels;

/// <summary>
/// Documents the TDLib authorization state machine channel.
/// TDLib clients go through a sequence of authorization states that must be handled
/// by the application (e.g. providing phone number, authentication code, password).
/// </summary>
[AsyncApi]
public class TelegramAuthChannel
{
    /// <summary>
    /// Subscribes to authorization state change events from TDLib.
    /// The receiver polls the native library and fires this event whenever
    /// the authorization state transitions (e.g. WaitPhoneNumber → WaitCode → Ready).
    /// </summary>
    [Channel("telegram/auth/state-changed",
        Description = "Authorization state change events. TDLib transitions through multiple states " +
                      "during authentication: WaitTdlibParameters → WaitPhoneNumber → WaitCode → " +
                      "WaitPassword → Ready. Applications must handle each state to complete login.")]
    [SubscribeOperation(typeof(TdApi.AuthorizationState), "Auth",
        Summary = "Subscribe to authorization state transitions",
        Description = "Receive notifications when the TDLib client's authorization state changes. " +
                      "The payload is a TdApi.AuthorizationState subclass indicating the new state. " +
                      "States include: AuthorizationStateWaitTdlibParameters, AuthorizationStateWaitPhoneNumber, " +
                      "AuthorizationStateWaitCode, AuthorizationStateWaitPassword, AuthorizationStateReady, " +
                      "AuthorizationStateLoggingOut, AuthorizationStateClosing, AuthorizationStateClosed.")]
    public void OnAuthorizationStateChanged(TdApi.AuthorizationState state) { }

    /// <summary>
    /// Sets the phone number for authentication.
    /// </summary>
    [Channel("telegram/auth/set-phone-number",
        Description = "Send the user's phone number to begin the authentication flow.")]
    [PublishOperation(typeof(TdApi.SetAuthenticationPhoneNumber), "Auth",
        Summary = "Provide phone number for authentication",
        Description = "Sends a phone number to TDLib to initiate the login process. " +
                      "After sending, the authorization state will transition to WaitCode.")]
    public void SetAuthenticationPhoneNumber(TdApi.SetAuthenticationPhoneNumber request) { }

    /// <summary>
    /// Checks the authentication code received via SMS or Telegram.
    /// </summary>
    [Channel("telegram/auth/check-code",
        Description = "Submit the authentication code received from Telegram.")]
    [PublishOperation(typeof(TdApi.CheckAuthenticationCode), "Auth",
        Summary = "Submit authentication code",
        Description = "Sends the authentication code to TDLib. If the code is correct and no 2FA password " +
                      "is set, the authorization state transitions to Ready.")]
    public void CheckAuthenticationCode(TdApi.CheckAuthenticationCode request) { }

    /// <summary>
    /// Checks the two-factor authentication password.
    /// </summary>
    [Channel("telegram/auth/check-password",
        Description = "Submit the two-factor authentication password.")]
    [PublishOperation(typeof(TdApi.CheckAuthenticationPassword), "Auth",
        Summary = "Submit 2FA password",
        Description = "Sends the two-factor authentication password to TDLib. " +
                      "Required when the account has a cloud password set.")]
    public void CheckAuthenticationPassword(TdApi.CheckAuthenticationPassword request) { }

    /// <summary>
    /// Logs out from the current Telegram session.
    /// </summary>
    [Channel("telegram/auth/log-out",
        Description = "Log out from the Telegram session. The session is terminated permanently on the server.")]
    [PublishOperation(typeof(TdApi.LogOut), "Auth",
        Summary = "Log out from Telegram",
        Description = "Sends a logout request to TDLib. The authorization state transitions to " +
                      "LoggingOut → Closing → Closed. The session is permanently invalidated.")]
    public void LogOut(TdApi.LogOut request) { }
}
