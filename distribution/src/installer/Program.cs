// SPDX-License-Identifier: GPL-3.0-or-later
// SLCraft setup host. Anomaly.InstallerCore is fetched and built locally by Setup.ps1.
using Anomaly.InstallerCore;
if(args.Length!=3 || (args[0]!="--install" && args[0]!="--dry-run"))
 throw new ArgumentException("Usage: SLCraft.Setup --install|--dry-run source-directory private-runtime-directory");
var source=Path.GetFullPath(args[1]).TrimEnd(Path.DirectorySeparatorChar);
var target=Path.GetFullPath(args[2]).TrimEnd(Path.DirectorySeparatorChar);
if(source.Equals(target,StringComparison.OrdinalIgnoreCase) ||
 target.StartsWith(source+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) ||
 source.StartsWith(target+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
 throw new ArgumentException("The original game and private target must not overlap.");
var options=new InstallOptions {
 VanillaPath=source, InstallerDir=target, SourceKind="local", NoBaseMod=true,
 DryRun=args[0]=="--dry-run"
};
if(options.DryRun) {
 // Upstream's dry run resolves the not-yet-copied target and fails on a fresh
 // install. Resolve the read-only source directly; perform no game mutation.
 var resources=new Anomaly.InstallerCore.Resources.AnomalyResourcesClient();
 var manifest=Anomaly.InstallerCore.Resources.RemoteManifest.LoadAsync(resources).GetAwaiter().GetResult();
 var context=InstallContextResolver.Resolve(source,options,resources,manifest);
 if(!context.IsIl2Cpp || context.CleanLauncher is null)
  throw new InvalidOperationException("The selected IL2CPP build has no supported clean launcher.");
 Console.WriteLine($"PASS dry run: source Unity {context.Launcher.ProductVersion}; IL2CPP; matching clean launcher.");
 Console.WriteLine($"Would copy {source} into {target}, prepare the offline clone, and install MelonLoader. No game files changed.");
 return;
}
InstallerService.Install(options);
