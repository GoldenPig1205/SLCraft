# SLCraft Client 0.2.6 — Small Distribution

[한국어 번역](README.ko.md)

> **CLIENT ONLY — NEVER INSTALL OR RUN THIS BUILD ON A SERVER.**
> Install and run SLCraft 0.2.6 only on the Windows game client. Do not place the DLL/JAR in an SCP:SL dedicated/headless server, a LabAPI/EXILED plugin directory, or a Minecraft dedicated-server mods directory. Do not deploy it to public or remote servers. No server deployment is permitted for this build.

The launcher's offline local host and Minecraft's integrated world simulation are internal to the local client session. They are not a separate supported server installation. `Setup.cmd` must receive the original **client** game directory, never a dedicated-server directory.

An experimental Windows x64 client that brings Minecraft hands, HUD, inventory, blocks, entities, and movement into SCP:SL facilities. This is the **integrated client**, not the old 0.1.0 server-bridge prototype. The supported mode is a separate, offline, single-player local host.

## Prerequisites

- Windows 10/11 x64.
- An **unmodified SCP:SL 14.2.7 Windows IL2CPP installation** from Steam. Setup accepts only the tested GameAssembly.dll SHA256. Other builds are not supported simply because they are newer.
- [Microsoft .NET SDK 10 or later](https://dotnet.microsoft.com/download/dotnet/10.0). Installing only the runtime is not enough to build the setup helper.
- A [Windows x64 JDK 25 or later](https://adoptium.net/temurin/releases/). Set JAVA_HOME or add Java to PATH after installation.
- A valid Minecraft Java Edition entitlement. Minecraft 26.3 currently runs in a Gradle development environment; the package does not import an account or profile from the normal Minecraft launcher.
- An Internet connection and approximately **12GB or more of free disk space** for installation. The ZIP is small, but the game copy, Java libraries, textures, and runtimes still require disk space. Usage varies by installation.

This ZIP contains no SCP:SL game files, Minecraft game files or textures, saved worlds, .NET/JDK installations, MelonLoader binaries, or compiled Anomaly components. Setup copies your local SCP:SL installation and downloads the required tools and dependencies. A small download does not eliminate the space required to run the games.

## Installation

1. Extract the complete ZIP into a **separate, writable folder outside the original game directory**. Keep the folder structure intact. Do not extract it into `Program Files`.
2. Install the SDK and JDK listed above, then run `Setup.cmd`.
3. Setup checks Steam's default installation location. If the game is not found, enter the original directory containing SCPSL.exe. For another Steam library, you can also specify the path in PowerShell:

```powershell
.\Setup.ps1 -SLPath 'D:\SteamLibrary\steamapps\common\SCP Secret Laboratory'
```

Setup checks the game build, file hashes, and separation of source and target directories. It builds a helper from pinned Anomaly.InstallerCore source. Anomaly prepares the local copy for offline use and installs MelonLoader. Your original Steam files are not overwritten. A separate .NET 6.0.36 runtime is downloaded for this copy. Gradle then prepares Minecraft and Fabric dependencies. Initial setup and first launch can take a while, depending on your Internet connection.

When setup completes, run `Start-Client.cmd`. The isolated SL client and background Minecraft start, create a local host and Class-D role, and connect automatically. A new Minecraft world is created on first use. Automatic test villagers, water, and TNT are disabled.

### Setup and restart options

- Restart a running session with `Restart-Client.cmd`. If saving or shutdown takes too long, the launcher reports the problem instead of force-killing the game.
- Preflight checks only: `Setup.ps1 -Check -SLPath 'original game directory'`.
- Installer dry run: `Setup.ps1 -DryRun -SLPath 'original game directory'`. This downloads/builds the setup helper cache but does not copy or patch the game.
- If a network or build error occurs, read the message and rerun the same Setup command. If a cached download is corrupt, rename it before retrying.
- Before repacking or publishing, exclude `.local`, `.tools`, `src/minecraft/.gradle`, `build`, `run`, `bin`, `obj`, and logs.

## Controls

| Key | Action |
| --- | --- |
| E | Minecraft inventory |
| T or / | Minecraft chat and commands |
| M | Open/close the SL Remote Admin panel |
| ~ | SL console |
| Esc | Close a menu |
| F8 | Toggle the Minecraft connection |
| F10 | Release Minecraft control and return to SL controls |

The Minecraft HUD temporarily hides while an SL administrator menu is open and returns when that menu closes. The SL inventory is hidden while connected. For flight, use `/gamemode creative` in Minecraft and double-tap Space. To pass through walls, use `/gamemode spectator`. The new world does not include the developer's inventory or save data.

## Files and limitations

- `payload/`: current SLCraft DLL/JAR and checksums. No game DLLs are distributed alongside these two binaries.
- `src/client`, `src/minecraft`, `src/tests`: SLCraft source and build/test tools.
- `src/installer`: the small setup host built locally against Anomaly.InstallerCore.
- `.local/sl-runtime`: your private SL copy, created during installation.
- `src/minecraft/run/saves`: your private worlds, created when running the game.
- `.tools`: downloaded tool source, archives, and the locally built setup host.
- `dependencies.json`: pinned versions, download URLs, and verification hashes. `payload/manifest.json`: SLCraft binary hashes.

TNT affects placed Minecraft blocks; it does not destroy the native SL facility. Facility collision uses a 0.25m approximation. Not all doors, slopes, elevators, or roles have been tested. Final visual rechecks of villager faces and water colors are still pending.

For troubleshooting, check `.local/minecraft-client.log`, `.local/sl-runtime/client-unity.log`, and `.local/sl-runtime/MelonLoader/Latest.log`. Logs can contain PC paths; review them before publishing.

## Build from source

Run these commands after setup has generated the Il2Cpp references:

```powershell
dotnet build src/client/SLcraft.Client.csproj -c Release
dotnet run --project src/tests/Client.Protocol.Tests.csproj -c Release
cd src/minecraft
.\gradlew.bat build --no-daemon --console=plain
```

Source builds do not overwrite `payload` automatically. If you update the binaries, also regenerate the payload checksums. This release ships the 0.2.6 binaries.

See the [third-party notices](THIRD-PARTY-NOTICES.md) and [validation report](VALIDATION.md). This project is not an official product of Northwood, Mojang, AnomalyLabs, or SkyCraft.
