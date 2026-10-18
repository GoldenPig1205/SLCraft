# Licenses and external components

- SLCraft client and bridge changes: MIT, `LICENSE`.
- Minecraft bridge derived from chasmlol/SkyCraft: MIT, `licenses/SkyCraft-MIT.txt`. Original notices are retained in source.
- The separate setup host in `src/installer` is GPL-3.0-or-later; its license is in that directory. It is built locally against Anomaly.InstallerCore, downloaded from AnomalyLabsIO/anomaly-launcher at the commit and archive SHA256 pinned in `dependencies.json`. The upstream GPL source, license, dependency declarations and native helper references remain in `.tools` after setup. No Anomaly compiled component is shipped in this ZIP. Upstream: https://github.com/AnomalyLabsIO/anomaly-launcher .
- Anomaly's matching resource snapshot is pinned by commit. The installer verifies resource hashes using its upstream manifest. It downloads MelonLoader 0.7.2 from LavaGang's release during installation. These components retain their own licenses and are not part of the SLCraft MIT grant.
- Gradle Wrapper is included for reproducible builds under Apache-2.0; see `licenses/Apache-2.0.txt`. Gradle, Fabric Loader, Fabric API, Minecraft libraries and .NET are obtained during setup under their respective terms.
- No SCP:SL or Minecraft game executable, assets, textures, account credentials, world saves or developer-specific settings are shipped. Game use remains subject to the original publishers' terms. Users prepare their own supported installations and Minecraft entitlement.

Windows-only experimental offline local-host build. No supported public-server or remote-server mode is advertised.
