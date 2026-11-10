# TShock architecture and contributor guide

## What runs

TShock is a .NET 9 plugin for the Terraria dedicated server. `TShockLauncher` starts
`TerrariaApi.Server.Program`, which loads `TShockAPI` as a server plugin. The
Terraria server implementation and hooks come from the `TerrariaServerAPI` Git
submodule and its OTAPI dependency. Code in `TShockAPI` can access Terraria's
`Main`, players, NPCs, tiles, and networking types directly.

## Project map

| Path | Responsibility |
| --- | --- |
| `TShockLauncher/` | Executable entry point, assembly loading, release layout, and plugin-manager CLI. |
| `TShockAPI/TShock.cs` | Plugin initialization, config and manager setup, server hook registration, and packet entry points. |
| `TShockAPI/Commands.cs` | Chat/console command registry, permission and server-caller checks, parsing, and command handlers. |
| `TShockAPI/GetDataHandlers.cs`, `Bouncer.cs`, `Handlers/` | Incoming packet decoding and validation. |
| `TShockAPI/TSPlayer.cs`, `TSServerPlayer.cs` | Player abstractions, messaging, network sends, and server-caller behavior. |
| `TShockAPI/Rest/` | HTTP REST registration, token authentication, permissions, and endpoint handlers. |
| `TShockAPI/DB/`, `Group.cs`, `Permissions.cs` | SQLite/MySQL/PostgreSQL-backed state, groups, and permission names. |
| `TShockAPI/Configuration/`, `Localization/`, `I18n.cs` | Runtime settings and translated text. |
| `TShockPluginManager/`, `TShockInstaller/` | Plugin package management and installation tooling. |
| `TShockLauncher.Tests/` | NUnit tests; `TestSetup.cs` initializes Terraria localization and TShock. |
| `docs/`, `i18n/`, `prebuilts/` | Operator docs, translation catalogs, and bundled assets. |

## Request flow

1. `TShock.Initialize` registers Terraria server hooks and calls
   `Commands.InitCommands`.
2. Game and console commands go through `Commands.HandleCommand`. Each command
   declares a permission and may set `AllowServer = false` if it needs a real
   in-game caller. REST callers are not real players.
3. `/v3/server/rawcmd` in `Rest/RestManager.cs` constructs a `TSRestPlayer` from
   the token's user/group and calls `Commands.HandleCommand`. The token needs
   `tshock.rest.command` plus the command's own permission. Command output is
   returned in the REST `response` array.
4. Incoming Terraria packets enter `TShock.OnGetData`, dispatch through
   `GetDataHandlers`, and may be checked by `Bouncer` or specialized handlers.

For a new command, register it in `Commands.InitCommands`, implement its handler
in `Commands.cs`, reuse a permission from `Permissions.cs` only when its scope
matches, and update relevant documentation. Use `GetString` for player-facing
messages. Buff and debuff IDs and display names come from Terraria's `BuffID`
and `Lang`; `Utils.GetBuffByName` resolves localized and English names.
When explaining a component to another contributor, state both its role and
what it actually is (for example, a library, executable, server process, or
protocol).

## Build and test in this workspace

The owner requires all C# builds, tests, and server execution to run **inside
Podman**. Do not install .NET, C# packages, or Terraria dependencies on the
host. This checkout may have an empty `TerrariaServerAPI/` submodule; a build
needs that pinned submodule revision. Copy the repository into a temporary SDK
container, fetch and check out the gitlink commit recorded for the submodule
(`git ls-tree HEAD TerrariaServerAPI`), then run `dotnet test
TShockLauncher.Tests/TShockLauncher.Tests.csproj` inside that container. The
repository's `Dockerfile` is a separate production packaging path.
