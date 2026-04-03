# 💬 Messages Channels

Message channels cover sending, receiving, editing, deleting, and forwarding Telegram messages. Subscribe channels deliver server-pushed events (new messages, edits, send confirmations), while publish channels represent requests sent to TDLib.

## Overview

| Channel | Direction | Summary |
|---|---|---|
| [`telegram/messages/content-changed`](#telegrammessagescontent-changed) | ⬇️ Subscribe | Subscribe to message edits |
| [`telegram/messages/delete`](#telegrammessagesdelete) | ⬆️ Publish | Delete messages |
| [`telegram/messages/edit-text`](#telegrammessagesedit-text) | ⬆️ Publish | Edit message text |
| [`telegram/messages/forward`](#telegrammessagesforward) | ⬆️ Publish | Forward messages |
| [`telegram/messages/new`](#telegrammessagesnew) | ⬇️ Subscribe | Subscribe to new messages |
| [`telegram/messages/send`](#telegrammessagessend) | ⬆️ Publish | Send a message |
| [`telegram/messages/send-failed`](#telegrammessagessend-failed) | ⬇️ Subscribe | Subscribe to message send failures |
| [`telegram/messages/send-succeeded`](#telegrammessagessend-succeeded) | ⬇️ Subscribe | Subscribe to message send confirmations |

---

## `telegram/messages/content-changed`

Message content change events. Fired when an existing message is edited.

**Direction:** ⬇️ Subscribe (incoming event)  
**Payload:** `updateMessageContent`

Receive notifications when the content of a message changes (e.g. text edit, media replacement). Provides the chat ID, message ID, and new content.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `chatId` | `string (int64)` |  |
| `messageId` | `string (int64)` |  |
| `newContent` | `object` |  |

---

## `telegram/messages/delete`

Delete messages from a chat.

**Direction:** ⬆️ Publish (outgoing request)  
**Payload:** `deleteMessages`

Deletes one or more messages from a chat. The Revoke parameter controls whether messages are deleted for all users or only the current user.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `chatId` | `string (int64)` |  |
| `messageIds` | `array<string>` |  |
| `revoke` | `boolean` |  |

---

## `telegram/messages/edit-text`

Edit the text content of a previously sent message.

**Direction:** ⬆️ Publish (outgoing request)  
**Payload:** `editMessageText`

Edits the text of a message. Can only edit messages sent by the current user in non-channel chats, or any message in a channel the user administers.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `chatId` | `string (int64)` |  |
| `inputMessageContent` | `object` |  |
| `messageId` | `string (int64)` |  |
| `replyMarkup` | `object` |  |

---

## `telegram/messages/forward`

Forward messages from one chat to another.

**Direction:** ⬆️ Publish (outgoing request)  
**Payload:** `forwardMessages`

Forwards one or more messages to a target chat. Options include sending as a copy and removing captions.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `chatId` | `string (int64)` |  |
| `fromChatId` | `string (int64)` |  |
| `messageIds` | `array<string>` |  |
| `options` | `object` |  |
| `removeCaption` | `boolean` |  |
| `sendCopy` | `boolean` |  |
| `topicId` | `object` |  |

---

## `telegram/messages/new`

New message events. Fired when a new message is received in any chat the authenticated user is a member of.

**Direction:** ⬇️ Subscribe (incoming event)  
**Payload:** `updateNewMessage`

Receive notifications when new messages arrive. The payload contains the full TdApi.Message object including sender, chat ID, content, and metadata.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `message` | `object` |  |

---

## `telegram/messages/send`

Send a message to a Telegram chat. Supports text, photos, videos, documents, and more.

**Direction:** ⬆️ Publish (outgoing request)  
**Payload:** `sendMessage`

Sends a message to the specified chat. The InputMessageContent determines the type of message (text, photo, video, etc.). Returns a Message object with a temporary ID that will be updated via UpdateMessageSendSucceeded.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `chatId` | `string (int64)` |  |
| `inputMessageContent` | `object` |  |
| `options` | `object` |  |
| `replyMarkup` | `object` |  |
| `replyTo` | `object` |  |
| `topicId` | `object` |  |

---

## `telegram/messages/send-failed`

Message send failure events. Fired when a message could not be sent.

**Direction:** ⬇️ Subscribe (incoming event)  
**Payload:** `updateMessageSendFailed`

Receive a notification when a message failed to send. Contains the error code and message describing the failure reason.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `error` | `object` |  |
| `message` | `object` |  |
| `oldMessageId` | `string (int64)` |  |

---

## `telegram/messages/send-succeeded`

Message send success events. Fired when a pending message has been sent successfully.

**Direction:** ⬇️ Subscribe (incoming event)  
**Payload:** `updateMessageSendSucceeded`

Receive a notification when a message previously sent by the client has been delivered to the server. Contains the old temporary message ID and the new permanent one.

**Properties:**

| Property | Type | Description |
|---|---|---|
| `message` | `object` |  |
| `oldMessageId` | `string (int64)` |  |

