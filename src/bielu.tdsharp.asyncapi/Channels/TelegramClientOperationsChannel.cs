// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using Bielu.AspNetCore.AsyncApi.Attributes.Attributes;
using TdLib;

namespace bielu.tdsharp.asyncapi.Channels;

/// <summary>
/// Documents the core TDLib client operations — synchronous and asynchronous requests
/// sent to the TDLib native library for querying Telegram data.
/// </summary>
[AsyncApi]
public class TelegramClientOperationsChannel
{
    /// <summary>
    /// Gets the current authenticated user.
    /// </summary>
    [Channel("telegram/operations/get-me",
        Description = "Retrieve the authenticated user's profile information.")]
    [PublishOperation(typeof(TdApi.GetMe), "Operations", "User",
        Summary = "Get current user",
        Description = "Returns a TdApi.User object with the authenticated user's profile " +
                      "(ID, name, username, phone number, profile photo, etc.).")]
    public void GetMe(TdApi.GetMe request) { }

    /// <summary>
    /// Gets information about a chat by its ID.
    /// </summary>
    [Channel("telegram/operations/get-chat",
        Description = "Retrieve detailed information about a specific chat.")]
    [PublishOperation(typeof(TdApi.GetChat), "Operations", "Chat",
        Summary = "Get chat information",
        Description = "Returns a TdApi.Chat object with the chat's title, type (private/group/channel), " +
                      "photo, last message, unread counts, and other metadata.")]
    public void GetChat(TdApi.GetChat request) { }

    /// <summary>
    /// Gets a list of chats the user has.
    /// </summary>
    [Channel("telegram/operations/get-chats",
        Description = "Retrieve a paginated list of the user's chats.")]
    [PublishOperation(typeof(TdApi.GetChats), "Operations", "Chat",
        Summary = "Get chat list",
        Description = "Returns an ordered list of chat IDs. Use with a ChatList (Main, Archive, Folder) " +
                      "and a limit to paginate through the user's conversations.")]
    public void GetChats(TdApi.GetChats request) { }

    /// <summary>
    /// Gets information about a user by their ID.
    /// </summary>
    [Channel("telegram/operations/get-user",
        Description = "Retrieve information about a Telegram user.")]
    [PublishOperation(typeof(TdApi.GetUser), "Operations", "User",
        Summary = "Get user information",
        Description = "Returns a TdApi.User object for the specified user ID, including " +
                      "name, username, phone number, status, and profile photo.")]
    public void GetUser(TdApi.GetUser request) { }

    /// <summary>
    /// Gets a TDLib internal option value.
    /// </summary>
    [Channel("telegram/operations/get-option",
        Description = "Retrieve a TDLib configuration option value (e.g. 'version', 'commit_hash').")]
    [PublishOperation(typeof(TdApi.GetOption), "Operations", "Config",
        Summary = "Get TDLib option",
        Description = "Returns the value of a TDLib internal option. Common options include " +
                      "'version' (TDLib version), 'commit_hash', 'my_id' (current user ID), " +
                      "and various configuration flags.")]
    public void GetOption(TdApi.GetOption request) { }

    /// <summary>
    /// Searches for messages in a chat.
    /// </summary>
    [Channel("telegram/operations/search-messages",
        Description = "Search for messages matching a query within a specific chat.")]
    [PublishOperation(typeof(TdApi.SearchChatMessages), "Operations", "Messages",
        Summary = "Search chat messages",
        Description = "Searches for messages in a chat by text query, sender, message type, " +
                      "or date range. Returns a paginated list of matching messages.")]
    public void SearchChatMessages(TdApi.SearchChatMessages request) { }

    /// <summary>
    /// Downloads a file from Telegram servers.
    /// </summary>
    [Channel("telegram/operations/download-file",
        Description = "Download a file from Telegram servers to the local filesystem.")]
    [PublishOperation(typeof(TdApi.DownloadFile), "Operations", "Files",
        Summary = "Download a file",
        Description = "Initiates a file download. The file will be downloaded to the TDLib files " +
                      "directory. Progress can be tracked via UpdateFile events.")]
    public void DownloadFile(TdApi.DownloadFile request) { }

    /// <summary>
    /// Closes the TDLib client instance.
    /// </summary>
    [Channel("telegram/operations/close",
        Description = "Close the TDLib client instance. The session is preserved and can be resumed later.")]
    [PublishOperation(typeof(TdApi.Close), "Operations", "Lifecycle",
        Summary = "Close TDLib client",
        Description = "Closes the TDLib client gracefully. The user session is preserved on disk — " +
                      "recreating the client will resume the session without re-authentication. " +
                      "The authorization state will transition to Closing → Closed.")]
    public void Close(TdApi.Close request) { }
}
