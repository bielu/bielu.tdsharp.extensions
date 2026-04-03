// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using Bielu.AspNetCore.AsyncApi.Extensions;
using Bielu.AspNetCore.AsyncApi.UI;

// --------------------------------------------------------------------------
// Build a minimal web app that serves the AsyncAPI document and interactive UI
// documenting the TDLib Telegram communication channels.
// --------------------------------------------------------------------------
var builder = WebApplication.CreateBuilder(args);

// Required by AsyncAPI document generation (provides ApplicationPartManager).
// AddControllers also enables assembly scanning for channel classes.
builder.Services.AddControllers()
    .AddApplicationPart(typeof(bielu.tdsharp.asyncapi.Channels.TelegramAuthChannel).Assembly);

// Register AsyncAPI services with TDLib server and metadata
builder.Services.AddAsyncApi(options =>
{
    options.AddServer("tdlib", "localhost", "tdlib-json", server =>
    {
        server.Description =
            "TDLib native JSON client (tdjson). Communication happens via FFI calls to the " +
            "native C++ library, not over a network socket. The 'server' represents the " +
            "TDLib engine running in-process.";
    });

    options
        .WithDefaultContentType("application/json")
        .WithDescription(
            "AsyncAPI documentation for TDLib (Telegram Database Library) communication patterns " +
            "as exposed by the bielu.tdsharp.extensions libraries.\n\n" +
            "## Architecture\n\n" +
            "TDLib uses a layered architecture:\n" +
            "1. **Application** sends requests via `TdApi.IClient` (Send / Execute / ExecuteAsync)\n" +
            "2. **TdJsonClient** serializes requests to JSON and forwards them to the native TDLib library\n" +
            "3. **Receiver** polls the native library for responses and updates in a background loop\n" +
            "4. **Events** are fired back to the application (UpdateReceived, AuthorizationStateChanged)\n\n" +
            "## Channel Categories\n\n" +
            "- **Auth** — Authorization state machine (login flow)\n" +
            "- **Messages** — Sending, receiving, editing, and deleting messages\n" +
            "- **Operations** — Querying Telegram data (users, chats, options, files)\n" +
            "- **Updates** — Server-pushed events (status changes, notifications, connection state)")
        .WithLicense("MIT", "https://opensource.org/licenses/MIT");
});

var app = builder.Build();

// Map the AsyncAPI document endpoint and interactive UI
app.MapAsyncApi();
app.MapAsyncApiUi();

app.MapGet("/", () => Results.Redirect("/asyncapi"));

Console.WriteLine("=== TDLib AsyncAPI Documentation Demo ===");
Console.WriteLine();
Console.WriteLine("AsyncAPI document: http://localhost:5000/asyncapi/v1.json");
Console.WriteLine("AsyncAPI UI:       http://localhost:5000/asyncapi");
Console.WriteLine();

app.Run();
