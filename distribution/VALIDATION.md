# Release Validation — 2026-10-18

[한국어 번역](VALIDATION.ko.md)

Test environment: Windows x64, SCP:SL 14.2.7 / Unity 6000.0.43f1, Minecraft 26.3, Fabric Loader 0.19.5, MelonLoader 0.7.2, JDK 26, and .NET SDK 10.

- Per-file SHA256 verification and required-file checks passed for the final ZIP.
- The archive was checked for excluded game files, executable tools, private worlds/logs, development caches, and backups.
- Git ignore checks confirmed that game copies, backups, build output, caches, and worlds are excluded, while source, setup tools, the SLCraft DLL/JAR, and Gradle Wrapper remain shareable. Every Git-visible file is below 50MiB.
- Setup preflight passed in Windows PowerShell 5.1. The ZIP was also extracted into a fresh directory containing spaces, and the checks passed there.
- The pinned Anomaly source archive was downloaded, its SHA256 verified, and the setup host built. The original game's Unity/IL2CPP dry run passed.
- A complete Setup.ps1 installation succeeded: a separate Steam game copy, MelonLoader, generated Il2Cpp references, .NET 6.0.36, and Minecraft launch configuration. The resulting state reported GameVersion=14.2.7 and Patched=true. Generated game files and dependencies are excluded from Git and the release archive.
- The packaged C# source built with zero errors and eight existing nullable warnings. The public DLL was rebuilt without private debug paths.
- The packaged source passed 23 protocol checks, Minecraft Gradle build, and 28 Java tests.
- Launching and playing the newly packaged client was not tested during this packaging task. Earlier local 0.2.6 tests confirmed actual mob navigation, TNT breaking placed Minecraft blocks, and recovery after administrator-menu and role changes. Final visual checks of villager faces and water colors remain pending.

The installation test machine already had the SDK/JDK and some user-level download caches. A complete download and installation on a fresh Windows account or another PC remains to be tested. Setup rejects a changed source build; compatibility with other SCP:SL versions is not claimed.

This is a source-inclusive release. Game files and dependencies are downloaded or copied during setup. ZIP size does not represent installed disk usage. No GitHub upload, commit, or push was performed by the packaging workflow.
