# 📡 Updates Channels

Update channels deliver asynchronous events pushed from Telegram servers. These arrive via the background receiver polling loop and are surfaced through the `UpdateReceived` event on `TdApi.IClient`. Your application subscribes to the specific update types it needs.

## Overview

| Channel | Direction | Summary |
|---|---|---|
| [`telegram/updates/chat-last-message`](#telegramupdateschat-last-message) | ⬇️ Subscribe | Subscribe to chat last message updates |
| [`telegram/updates/chat-read-inbox`](#telegramupdateschat-read-inbox) | ⬇️ Subscribe | Subscribe to chat read state changes |
| [`telegram/updates/chat-title`](#telegramupdateschat-title) | ⬇️ Subscribe | Subscribe to chat title changes |
| [`telegram/updates/connection-state`](#telegramupdatesconnection-state) | ⬇️ Subscribe | Subscribe to connection state changes |
| [`telegram/updates/file`](#telegramupdatesfile) | ⬇️ Subscribe | Subscribe to file transfer progress |
| [`telegram/updates/notification-settings`](#telegramupdatesnotification-settings) | ⬇️ Subscribe | Subscribe to notification setting changes |
| [`telegram/updates/option`](#telegramupdatesoption) | ⬇️ Subscribe | Subscribe to option changes |
| [`telegram/updates/user`](#telegramupdatesuser) | ⬇️ Subscribe | Subscribe to user profile updates |
| [`telegram/updates/user-status`](#telegramupdatesuser-status) | ⬇️ Subscribe | Subscribe to user status changes |

---

## `telegram/updates/chat-last-message`

Chat last message change events. Fired when the last message in a chat changes (new message received or previous last message deleted).

**Direction:** ⬇️ Subscribe (incoming event)  
**Payload:** `updateChatLastMessage`

Receive notifications when the most recent message in a chat changes. This is typically used to update chat list UI elements.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `chatId` | `string (int64)` |  |
| `lastMessage` | `object` |  |
| `positions` | `array<object>` |  |

---

## `telegram/updates/chat-read-inbox`

Chat read inbox state events. Fired when messages are marked as read in a chat.

**Direction:** ⬇️ Subscribe (incoming event)  
**Payload:** `updateChatReadInbox`

Receive notifications when the read pointer advances in a chat (e.g. the user read messages in another client). Contains the new last-read incoming message ID and unread count.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `chatId` | `string (int64)` |  |
| `lastReadInboxMessageId` | `string (int64)` |  |
| `unreadCount` | `string (int32)` |  |

---

## `telegram/updates/chat-title`

Chat title change events.

**Direction:** ⬇️ Subscribe (incoming event)  
**Payload:** `updateChatTitle`

Receive notifications when a chat's title is updated.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `chatId` | `string (int64)` |  |
| `title` | `string` |  |

---

## `telegram/updates/connection-state`

Connection state change events. Fired when the connection to Telegram servers changes.

**Direction:** ⬇️ Subscribe (incoming event)  
**Payload:** `updateConnectionState`

Receive notifications when the network connection state changes: WaitingForNetwork, ConnectingToProxy, Connecting, Updating, Ready.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `state` | `object` |  |

---

## `telegram/updates/file`

File download/upload progress events. Fired during file transfer operations.

**Direction:** ⬇️ Subscribe (incoming event)  
**Payload:** `updateFile`

Receive progress notifications during file downloads or uploads. Contains the file ID, expected size, downloaded size, and local/remote file info.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `file` | `object` |  |

---

## `telegram/updates/notification-settings`

Chat notification settings change events.

**Direction:** ⬇️ Subscribe (incoming event)  
**Payload:** `updateChatNotificationSettings`

Receive notifications when a chat's notification settings are modified (mute duration, sound, show preview, etc.).

**Properties:**

| Property | Type | Description |
|---|---|---|
| `chatId` | `string (int64)` |  |
| `notificationSettings` | `object` |  |

---

## `telegram/updates/option`

TDLib option value change events. Fired when an internal configuration option is updated.

**Direction:** ⬇️ Subscribe (incoming event)  
**Payload:** `updateOption`

Receive notifications when a TDLib internal option value changes. Options include 'my_id', 'unix_time', 'online', and many others.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `name` | `string` |  |
| `value` | `object` |  |

---

## `telegram/updates/user`

User profile update events. Fired when a user's profile information changes.

**Direction:** ⬇️ Subscribe (incoming event)  
**Payload:** `updateUser`

Receive notifications when a user's profile changes (name, username, profile photo, bio, etc.).

**Properties:**

| Property | Type | Description |
|---|---|---|
| `user` | `object` |  |

---

## `telegram/updates/user-status`

User online status change events. Fired when a contact's online status changes.

**Direction:** ⬇️ Subscribe (incoming event)  
**Payload:** `updateUserStatus`

Receive notifications when a user's online status changes (online, offline with last-seen timestamp, recently, last week, last month).

**Properties:**

| Property | Type | Description |
|---|---|---|
| `status` | `object` |  |
| `userId` | `string (int64)` |  |

