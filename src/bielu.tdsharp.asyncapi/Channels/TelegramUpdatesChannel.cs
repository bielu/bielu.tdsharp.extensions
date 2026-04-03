// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using Bielu.AspNetCore.AsyncApi.Attributes.Attributes;
using TdLib;

namespace bielu.tdsharp.asyncapi.Channels;

/// <summary>
/// Documents the TDLib update channels — events pushed from the Telegram server
/// to the client via the background receiver polling loop.
/// </summary>
[AsyncApi]
public class TelegramUpdatesChannel
{
    /// <summary>
    /// Subscribes to user status updates (online/offline/last seen).
    /// </summary>
    [Channel("telegram/updates/user-status",
        Description = "User online status change events. Fired when a contact's online status changes.")]
    [SubscribeOperation(typeof(TdApi.Update.UpdateUserStatus), "Updates", "User",
        Summary = "Subscribe to user status changes",
        Description = "Receive notifications when a user's online status changes " +
                      "(online, offline with last-seen timestamp, recently, last week, last month).")]
    public void OnUserStatusUpdate(TdApi.Update.UpdateUserStatus update) { }

    /// <summary>
    /// Subscribes to user profile updates.
    /// </summary>
    [Channel("telegram/updates/user",
        Description = "User profile update events. Fired when a user's profile information changes.")]
    [SubscribeOperation(typeof(TdApi.Update.UpdateUser), "Updates", "User",
        Summary = "Subscribe to user profile updates",
        Description = "Receive notifications when a user's profile changes (name, username, " +
                      "profile photo, bio, etc.).")]
    public void OnUserUpdate(TdApi.Update.UpdateUser update) { }

    /// <summary>
    /// Subscribes to chat title, photo, and metadata changes.
    /// </summary>
    [Channel("telegram/updates/chat-title",
        Description = "Chat title change events.")]
    [SubscribeOperation(typeof(TdApi.Update.UpdateChatTitle), "Updates", "Chat",
        Summary = "Subscribe to chat title changes",
        Description = "Receive notifications when a chat's title is updated.")]
    public void OnChatTitleUpdate(TdApi.Update.UpdateChatTitle update) { }

    /// <summary>
    /// Subscribes to chat last message changes.
    /// </summary>
    [Channel("telegram/updates/chat-last-message",
        Description = "Chat last message change events. Fired when the last message in a chat changes " +
                      "(new message received or previous last message deleted).")]
    [SubscribeOperation(typeof(TdApi.Update.UpdateChatLastMessage), "Updates", "Chat",
        Summary = "Subscribe to chat last message updates",
        Description = "Receive notifications when the most recent message in a chat changes. " +
                      "This is typically used to update chat list UI elements.")]
    public void OnChatLastMessageUpdate(TdApi.Update.UpdateChatLastMessage update) { }

    /// <summary>
    /// Subscribes to chat read inbox state changes.
    /// </summary>
    [Channel("telegram/updates/chat-read-inbox",
        Description = "Chat read inbox state events. Fired when messages are marked as read in a chat.")]
    [SubscribeOperation(typeof(TdApi.Update.UpdateChatReadInbox), "Updates", "Chat",
        Summary = "Subscribe to chat read state changes",
        Description = "Receive notifications when the read pointer advances in a chat " +
                      "(e.g. the user read messages in another client). Contains the " +
                      "new last-read incoming message ID and unread count.")]
    public void OnChatReadInboxUpdate(TdApi.Update.UpdateChatReadInbox update) { }

    /// <summary>
    /// Subscribes to notification setting changes.
    /// </summary>
    [Channel("telegram/updates/notification-settings",
        Description = "Chat notification settings change events.")]
    [SubscribeOperation(typeof(TdApi.Update.UpdateChatNotificationSettings), "Updates", "Chat",
        Summary = "Subscribe to notification setting changes",
        Description = "Receive notifications when a chat's notification settings are modified " +
                      "(mute duration, sound, show preview, etc.).")]
    public void OnChatNotificationSettingsUpdate(TdApi.Update.UpdateChatNotificationSettings update) { }

    /// <summary>
    /// Subscribes to file download progress updates.
    /// </summary>
    [Channel("telegram/updates/file",
        Description = "File download/upload progress events. Fired during file transfer operations.")]
    [SubscribeOperation(typeof(TdApi.Update.UpdateFile), "Updates", "Files",
        Summary = "Subscribe to file transfer progress",
        Description = "Receive progress notifications during file downloads or uploads. " +
                      "Contains the file ID, expected size, downloaded size, and local/remote file info.")]
    public void OnFileUpdate(TdApi.Update.UpdateFile update) { }

    /// <summary>
    /// Subscribes to connection state changes.
    /// </summary>
    [Channel("telegram/updates/connection-state",
        Description = "Connection state change events. Fired when the connection to Telegram servers changes.")]
    [SubscribeOperation(typeof(TdApi.Update.UpdateConnectionState), "Updates", "Connection",
        Summary = "Subscribe to connection state changes",
        Description = "Receive notifications when the network connection state changes: " +
                      "WaitingForNetwork, ConnectingToProxy, Connecting, Updating, Ready.")]
    public void OnConnectionStateUpdate(TdApi.Update.UpdateConnectionState update) { }

    /// <summary>
    /// Subscribes to option value changes.
    /// </summary>
    [Channel("telegram/updates/option",
        Description = "TDLib option value change events. Fired when an internal configuration option is updated.")]
    [SubscribeOperation(typeof(TdApi.Update.UpdateOption), "Updates", "Config",
        Summary = "Subscribe to option changes",
        Description = "Receive notifications when a TDLib internal option value changes. " +
                      "Options include 'my_id', 'unix_time', 'online', and many others.")]
    public void OnOptionUpdate(TdApi.Update.UpdateOption update) { }
}
