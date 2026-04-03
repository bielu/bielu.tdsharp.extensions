# TDLib AsyncAPI Documentation

> Generated from AsyncAPI 3.1.0 specification

AsyncAPI documentation for TDLib (Telegram Database Library) communication patterns as exposed by the bielu.tdsharp.extensions libraries.

## Architecture

TDLib uses a layered architecture:
1. **Application** sends requests via `TdApi.IClient` (Send / Execute / ExecuteAsync)
2. **TdJsonClient** serializes requests to JSON and forwards them to the native TDLib library
3. **Receiver** polls the native library for responses and updates in a background loop
4. **Events** are fired back to the application (UpdateReceived, AuthorizationStateChanged)

## Channel Categories

- **Auth** — Authorization state machine (login flow)
- **Messages** — Sending, receiving, editing, and deleting messages
- **Operations** — Querying Telegram data (users, chats, options, files)
- **Updates** — Server-pushed events (status changes, notifications, connection state)

## Server

| Property | Value |
|---|---|
| **Name** | `tdlib` |
| **Host** | `localhost` |
| **Protocol** | `tdlib-json` |
| **Description** | TDLib native JSON client (tdjson). Communication happens via FFI calls to the native C++ library, not over a network socket. The 'server' represents the TDLib engine running in-process. |

## Channel Categories

| Category | Channels | Description |
|---|---|---|
| 🔐 [Auth](Auth-Channels) | 5 | TDLib clients must complete an authorization flow before they can interact with Telegram. The flow i... |
| 💬 [Messages](Messages-Channels) | 8 | Message channels cover sending, receiving, editing, deleting, and forwarding Telegram messages. Subs... |
| ⚙️ [Operations](Operations-Channels) | 8 | Client operation channels represent request/response interactions with TDLib. Your application publi... |
| 📡 [Updates](Updates-Channels) | 9 | Update channels deliver asynchronous events pushed from Telegram servers. These arrive via the backg... |

## All Channels

| Channel | Category | Direction | Summary |
|---|---|---|---|
| `telegram/auth/check-code` | 🔐 Auth | ⬆️ Publish | Submit authentication code |
| `telegram/auth/check-password` | 🔐 Auth | ⬆️ Publish | Submit 2FA password |
| `telegram/auth/log-out` | 🔐 Auth | ⬆️ Publish | Log out from Telegram |
| `telegram/auth/set-phone-number` | 🔐 Auth | ⬆️ Publish | Provide phone number for authentication |
| `telegram/auth/state-changed` | 🔐 Auth | ⬇️ Subscribe | Subscribe to authorization state transitions |
| `telegram/messages/content-changed` | 💬 Messages | ⬇️ Subscribe | Subscribe to message edits |
| `telegram/messages/delete` | 💬 Messages | ⬆️ Publish | Delete messages |
| `telegram/messages/edit-text` | 💬 Messages | ⬆️ Publish | Edit message text |
| `telegram/messages/forward` | 💬 Messages | ⬆️ Publish | Forward messages |
| `telegram/messages/new` | 💬 Messages | ⬇️ Subscribe | Subscribe to new messages |
| `telegram/messages/send` | 💬 Messages | ⬆️ Publish | Send a message |
| `telegram/messages/send-failed` | 💬 Messages | ⬇️ Subscribe | Subscribe to message send failures |
| `telegram/messages/send-succeeded` | 💬 Messages | ⬇️ Subscribe | Subscribe to message send confirmations |
| `telegram/operations/close` | ⚙️ Operations | ⬆️ Publish | Close TDLib client |
| `telegram/operations/download-file` | ⚙️ Operations | ⬆️ Publish | Download a file |
| `telegram/operations/get-chat` | ⚙️ Operations | ⬆️ Publish | Get chat information |
| `telegram/operations/get-chats` | ⚙️ Operations | ⬆️ Publish | Get chat list |
| `telegram/operations/get-me` | ⚙️ Operations | ⬆️ Publish | Get current user |
| `telegram/operations/get-option` | ⚙️ Operations | ⬆️ Publish | Get TDLib option |
| `telegram/operations/get-user` | ⚙️ Operations | ⬆️ Publish | Get user information |
| `telegram/operations/search-messages` | ⚙️ Operations | ⬆️ Publish | Search chat messages |
| `telegram/updates/chat-last-message` | 📡 Updates | ⬇️ Subscribe | Subscribe to chat last message updates |
| `telegram/updates/chat-read-inbox` | 📡 Updates | ⬇️ Subscribe | Subscribe to chat read state changes |
| `telegram/updates/chat-title` | 📡 Updates | ⬇️ Subscribe | Subscribe to chat title changes |
| `telegram/updates/connection-state` | 📡 Updates | ⬇️ Subscribe | Subscribe to connection state changes |
| `telegram/updates/file` | 📡 Updates | ⬇️ Subscribe | Subscribe to file transfer progress |
| `telegram/updates/notification-settings` | 📡 Updates | ⬇️ Subscribe | Subscribe to notification setting changes |
| `telegram/updates/option` | 📡 Updates | ⬇️ Subscribe | Subscribe to option changes |
| `telegram/updates/user` | 📡 Updates | ⬇️ Subscribe | Subscribe to user profile updates |
| `telegram/updates/user-status` | 📡 Updates | ⬇️ Subscribe | Subscribe to user status changes |

## Quick Start

To serve this documentation as an interactive web UI:

```csharp
using Bielu.AspNetCore.AsyncApi.Extensions;
using Bielu.AspNetCore.AsyncApi.UI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddApplicationPart(typeof(bielu.tdsharp.asyncapi.Channels.TelegramAuthChannel).Assembly);

builder.Services.AddAsyncApi(options =>
{
    options.AddServer("tdlib", "localhost", "tdlib-json");
    options.WithDefaultContentType("application/json")
        .WithDescription("TDLib Telegram communication patterns.");
});

var app = builder.Build();
app.MapAsyncApi();
app.MapAsyncApiUi();
app.Run();
```

Then visit `/asyncapi` for the interactive UI or `/asyncapi/v1.json` for the raw document.
