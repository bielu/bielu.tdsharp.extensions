// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using Bielu.AspNetCore.AsyncApi.Attributes.Attributes;
using TdLib;

namespace bielu.tdsharp.asyncapi.Channels;

/// <summary>
/// Documents the TDLib messaging channels for sending and receiving messages.
/// </summary>
[AsyncApi]
public class TelegramMessagingChannel
{
    /// <summary>
    /// Subscribes to new incoming messages.
    /// </summary>
    [Channel("telegram/messages/new",
        Description = "New message events. Fired when a new message is received in any chat " +
                      "the authenticated user is a member of.")]
    [SubscribeOperation(typeof(TdApi.Update.UpdateNewMessage), "Messages",
        Summary = "Subscribe to new messages",
        Description = "Receive notifications when new messages arrive. The payload contains the full " +
                      "TdApi.Message object including sender, chat ID, content, and metadata.")]
    public void OnNewMessage(TdApi.Update.UpdateNewMessage update) { }

    /// <summary>
    /// Subscribes to message content updates (edits).
    /// </summary>
    [Channel("telegram/messages/content-changed",
        Description = "Message content change events. Fired when an existing message is edited.")]
    [SubscribeOperation(typeof(TdApi.Update.UpdateMessageContent), "Messages",
        Summary = "Subscribe to message edits",
        Description = "Receive notifications when the content of a message changes (e.g. text edit, " +
                      "media replacement). Provides the chat ID, message ID, and new content.")]
    public void OnMessageContentChanged(TdApi.Update.UpdateMessageContent update) { }

    /// <summary>
    /// Subscribes to message send state updates.
    /// </summary>
    [Channel("telegram/messages/send-succeeded",
        Description = "Message send success events. Fired when a pending message has been sent successfully.")]
    [SubscribeOperation(typeof(TdApi.Update.UpdateMessageSendSucceeded), "Messages",
        Summary = "Subscribe to message send confirmations",
        Description = "Receive a notification when a message previously sent by the client has been " +
                      "delivered to the server. Contains the old temporary message ID and the new permanent one.")]
    public void OnMessageSendSucceeded(TdApi.Update.UpdateMessageSendSucceeded update) { }

    /// <summary>
    /// Subscribes to message send failure events.
    /// </summary>
    [Channel("telegram/messages/send-failed",
        Description = "Message send failure events. Fired when a message could not be sent.")]
    [SubscribeOperation(typeof(TdApi.Update.UpdateMessageSendFailed), "Messages",
        Summary = "Subscribe to message send failures",
        Description = "Receive a notification when a message failed to send. Contains the error " +
                      "code and message describing the failure reason.")]
    public void OnMessageSendFailed(TdApi.Update.UpdateMessageSendFailed update) { }

    /// <summary>
    /// Sends a message to a chat.
    /// </summary>
    [Channel("telegram/messages/send",
        Description = "Send a message to a Telegram chat. Supports text, photos, videos, documents, and more.")]
    [PublishOperation(typeof(TdApi.SendMessage), "Messages",
        Summary = "Send a message",
        Description = "Sends a message to the specified chat. The InputMessageContent determines " +
                      "the type of message (text, photo, video, etc.). Returns a Message object " +
                      "with a temporary ID that will be updated via UpdateMessageSendSucceeded.")]
    public void SendMessage(TdApi.SendMessage request) { }

    /// <summary>
    /// Edits the text of an existing message.
    /// </summary>
    [Channel("telegram/messages/edit-text",
        Description = "Edit the text content of a previously sent message.")]
    [PublishOperation(typeof(TdApi.EditMessageText), "Messages",
        Summary = "Edit message text",
        Description = "Edits the text of a message. Can only edit messages sent by the current user " +
                      "in non-channel chats, or any message in a channel the user administers.")]
    public void EditMessageText(TdApi.EditMessageText request) { }

    /// <summary>
    /// Deletes messages from a chat.
    /// </summary>
    [Channel("telegram/messages/delete",
        Description = "Delete messages from a chat.")]
    [PublishOperation(typeof(TdApi.DeleteMessages), "Messages",
        Summary = "Delete messages",
        Description = "Deletes one or more messages from a chat. The Revoke parameter controls " +
                      "whether messages are deleted for all users or only the current user.")]
    public void DeleteMessages(TdApi.DeleteMessages request) { }

    /// <summary>
    /// Forwards messages to another chat.
    /// </summary>
    [Channel("telegram/messages/forward",
        Description = "Forward messages from one chat to another.")]
    [PublishOperation(typeof(TdApi.ForwardMessages), "Messages",
        Summary = "Forward messages",
        Description = "Forwards one or more messages to a target chat. " +
                      "Options include sending as a copy and removing captions.")]
    public void ForwardMessages(TdApi.ForwardMessages request) { }
}
