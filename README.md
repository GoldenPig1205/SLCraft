# SLCraft Client 0.2.6

[한국어 번역](README.ko.md)

An experimental Windows x64 client that brings Minecraft hands, HUD, inventory, blocks, entities, and movement into SCP:SL facilities. This version supports a separate, offline, single-player local host.

## Download and install

Download `SLCraft-Client-0.2.6.zip` from this repository's Releases page when available, or build the archive from source using the commands below. Extract the entire archive to a writable folder outside your original game installation.

Prepare the supported, unmodified **SCP:SL 14.2.7 Windows IL2CPP build**, **.NET SDK 10 or later**, and **JDK 25 or later**. Run `Setup.cmd`, then `Start-Client.cmd` after setup completes. See the [installation guide](distribution/README.md) for prerequisites, controls, disk requirements, and limitations.

The archive does not include game files or the developer's saved worlds. Setup copies your own SCP:SL installation into a separate directory and downloads the required tools and dependencies. A small ZIP still requires additional installation space. Minecraft currently runs through a Gradle development environment rather than a normal launcher profile.

## Publishing this project

- Upload `releases/SLCraft-Client-0.2.6.zip` and its `.sha256` file as GitHub Release assets.
- For the source repository, use normal Git add/commit commands from this directory. `.gitignore` excludes game copies, backups, caches, worlds, and private logs. Do not use `git add -f` to bypass these exclusions.
- The shareable project consists of `distribution/`, `scripts/`, the documentation, and the root launch wrappers.
- `outputs/SLcraft-0.1.0.zip` is the old server-bridge prototype, not the current integrated client release.

The local packaging workflow does not create GitHub releases, commit changes, or push files automatically.

## Rebuild the release archive

Run these commands in PowerShell 7:

```powershell
.\scripts\Build-Distribution.ps1
.\scripts\Test-Distribution.ps1
```

`distribution/payload` contains the current 0.2.6 DLL/JAR. If you change the source, rebuild the binaries and update the payload checksums yourself. The packaging script bundles the current source and payload; it does not install or launch the game.

## Local development data

`outputs/SLcraftClient`, `work`, and `migration-original` are local development and recovery data excluded from Git. Before the new distribution is installed, the root launch wrappers can use an existing development installation if present. Users downloading the public repository should follow the setup instructions above.

See [third-party notices](distribution/THIRD-PARTY-NOTICES.md) and the [release validation report](distribution/VALIDATION.md). Remote-server play and combat integration are unfinished; not all doors, slopes, elevators, or roles have been tested.
