# ⚙️ Operations Channels

Client operation channels represent request/response interactions with TDLib. Your application publishes a request (e.g. GetMe, GetChat) and receives a typed response. These are typically executed via `client.ExecuteAsync()` or `client.Execute()`.

## Overview

| Channel | Direction | Summary |
|---|---|---|
| [`telegram/operations/close`](#telegramoperationsclose) | ⬆️ Publish | Close TDLib client |
| [`telegram/operations/download-file`](#telegramoperationsdownload-file) | ⬆️ Publish | Download a file |
| [`telegram/operations/get-chat`](#telegramoperationsget-chat) | ⬆️ Publish | Get chat information |
| [`telegram/operations/get-chats`](#telegramoperationsget-chats) | ⬆️ Publish | Get chat list |
| [`telegram/operations/get-me`](#telegramoperationsget-me) | ⬆️ Publish | Get current user |
| [`telegram/operations/get-option`](#telegramoperationsget-option) | ⬆️ Publish | Get TDLib option |
| [`telegram/operations/get-user`](#telegramoperationsget-user) | ⬆️ Publish | Get user information |
| [`telegram/operations/search-messages`](#telegramoperationssearch-messages) | ⬆️ Publish | Search chat messages |

---

## `telegram/operations/close`

Close the TDLib client instance. The session is preserved and can be resumed later.

**Direction:** ⬆️ Publish (outgoing request)  
**Payload:** `close`

Closes the TDLib client gracefully. The user session is preserved on disk — recreating the client will resume the session without re-authentication. The authorization state will transition to Closing → Closed.

---

## `telegram/operations/download-file`

Download a file from Telegram servers to the local filesystem.

**Direction:** ⬆️ Publish (outgoing request)  
**Payload:** `downloadFile`

Initiates a file download. The file will be downloaded to the TDLib files directory. Progress can be tracked via UpdateFile events.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `fileId` | `string (int32)` |  |
| `limit` | `string (int64)` |  |
| `offset` | `string (int64)` |  |
| `priority` | `string (int32)` |  |
| `synchronous` | `boolean` |  |

---

## `telegram/operations/get-chat`

Retrieve detailed information about a specific chat.

**Direction:** ⬆️ Publish (outgoing request)  
**Payload:** `getChat`

Returns a TdApi.Chat object with the chat's title, type (private/group/channel), photo, last message, unread counts, and other metadata.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `chatId` | `string (int64)` |  |

---

## `telegram/operations/get-chats`

Retrieve a paginated list of the user's chats.

**Direction:** ⬆️ Publish (outgoing request)  
**Payload:** `getChats`

Returns an ordered list of chat IDs. Use with a ChatList (Main, Archive, Folder) and a limit to paginate through the user's conversations.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `chatList` | `object` |  |
| `limit` | `string (int32)` |  |

---

## `telegram/operations/get-me`

Retrieve the authenticated user's profile information.

**Direction:** ⬆️ Publish (outgoing request)  
**Payload:** `getMe`

Returns a TdApi.User object with the authenticated user's profile (ID, name, username, phone number, profile photo, etc.).

---

## `telegram/operations/get-option`

Retrieve a TDLib configuration option value (e.g. 'version', 'commit_hash').

**Direction:** ⬆️ Publish (outgoing request)  
**Payload:** `getOption`

Returns the value of a TDLib internal option. Common options include 'version' (TDLib version), 'commit_hash', 'my_id' (current user ID), and various configuration flags.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `name` | `string` |  |

---

## `telegram/operations/get-user`

Retrieve information about a Telegram user.

**Direction:** ⬆️ Publish (outgoing request)  
**Payload:** `getUser`

Returns a TdApi.User object for the specified user ID, including name, username, phone number, status, and profile photo.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `userId` | `string (int64)` |  |

---

## `telegram/operations/search-messages`

Search for messages matching a query within a specific chat.

**Direction:** ⬆️ Publish (outgoing request)  
**Payload:** `searchChatMessages`

Searches for messages in a chat by text query, sender, message type, or date range. Returns a paginated list of matching messages.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `chatId` | `string (int64)` |  |
| `filter` | `object` |  |
| `fromMessageId` | `string (int64)` |  |
| `limit` | `string (int32)` |  |
| `offset` | `string (int32)` |  |
| `query` | `string` |  |
| `senderId` | `object` |  |
| `topicId` | `object` |  |

