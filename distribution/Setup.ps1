param([string]$SLPath,[switch]$Check,[switch]$DryRun)
. (Join-Path $PSScriptRoot 'Release.Common.ps1')
if(![Environment]::Is64BitOperatingSystem -or ![Environment]::Is64BitProcess) { throw 'Run this package with 64-bit Windows PowerShell.' }
Assert-Payload
Assert-Sdk
$java=Find-JavaHome
$config=Get-ReleaseConfig
if(!$SLPath) {
 $steam=Get-ItemPropertyValue 'HKCU:\Software\Valve\Steam' 'SteamPath' -ErrorAction SilentlyContinue
 if($steam -and (Test-Path -LiteralPath (Join-Path $steam 'steamapps/common/SCP Secret Laboratory/SCPSL.exe'))) { $SLPath=Join-Path $steam 'steamapps/common/SCP Secret Laboratory' }
}
if(!$SLPath) { $SLPath=(Read-Host 'Enter your unmodified SCP Secret Laboratory game directory').Trim('"') }
$SLPath=[IO.Path]::GetFullPath($SLPath.Trim('"'))
$runtime=Get-PrivateGamePath
Assert-IsolatedPath $SLPath $runtime
foreach($required in @('SCPSL.exe','UnityPlayer.dll','GameAssembly.dll','SCPSL_Data/il2cpp_data/Metadata/global-metadata.dat')) {
 if(!(Test-Path -LiteralPath (Join-Path $SLPath $required))) { throw "Source game file missing: $required" }
}
if((Get-FileHash -LiteralPath (Join-Path $SLPath 'GameAssembly.dll')).Hash -ne $config.gameAssemblySha256) { throw 'This release supports the tested unmodified SCP:SL 14.2.7 build only. The selected GameAssembly.dll differs; no game files were changed.' }
if(@(Get-OwnedGames).Count) { throw 'Close the SLCraft game before installing or updating it.' }
if($Check) { Write-Output "PASS: payload, SDK, JDK and supported source game verified. Target: $runtime"; return }
$tools=Join-Path $PSScriptRoot '.tools'
New-Item -ItemType Directory -Path $tools -Force | Out-Null
$sourceZip=Join-Path $tools 'anomaly-source.zip'
Get-VerifiedDownload $config.anomalySourceUrl $sourceZip $config.anomalySourceSha256
$core=Join-Path $tools ('anomaly-launcher-'+$config.anomalyCommit+'/Anomaly.InstallerCore/Anomaly.InstallerCore.csproj')
if(!(Test-Path -LiteralPath $core)) { Expand-Archive -LiteralPath $sourceZip -DestinationPath $tools -Force }
$helper=Join-Path $tools 'setup-host'
& dotnet.exe build (Join-Path $PSScriptRoot 'src/installer/SLCraft.Setup.csproj') -c Release "-p:AnomalyCorePath=$core" -o $helper --nologo
if($LASTEXITCODE -ne 0) { throw 'Installer helper build failed.' }
$env:ANOMALY_RESOURCES_BASE_URL=$config.anomalyResourcesUrl
$env:ANOMALY_RESOURCES_FALLBACK_BASE_URL=$config.anomalyResourcesUrl
$env:ANOMALY_PATCHINFO_PATH=$null
$action=if($DryRun){'--dry-run'}else{'--install'}
if($DryRun) {
 & dotnet.exe (Join-Path $helper 'SLCraft.Setup.dll') $action $SLPath $runtime
 if($LASTEXITCODE -ne 0) { throw 'Installer dry run failed.' }; return
}
$local=Join-Path $PSScriptRoot '.local'
New-Item -ItemType Directory -Path $local -Force | Out-Null
$dotnetZip=Join-Path $tools 'dotnet6-runtime.zip'
Get-VerifiedDownload $config.dotnetRuntimeUrl $dotnetZip $config.dotnetRuntimeSha512 'SHA512'
$dotnet=Join-Path $local 'dotnet6'
if(!(Test-Path -LiteralPath (Join-Path $dotnet 'dotnet.exe'))) { Expand-Archive -LiteralPath $dotnetZip -DestinationPath $dotnet -Force }
$oldDotnet=$env:DOTNET_ROOT
try {
 $env:DOTNET_ROOT=$dotnet
 $stateFile=Join-Path $runtime '.anomaly_install.json'
 $ready=$false
 if(Test-Path -LiteralPath $stateFile) { $state=Get-Content -LiteralPath $stateFile -Raw | ConvertFrom-Json; $ready=$state.Patched -eq $true -and $state.GameVersion -eq '14.2.7' }
 if(!$ready) {
  & dotnet.exe (Join-Path $helper 'SLCraft.Setup.dll') $action $SLPath $runtime
  if($LASTEXITCODE -ne 0) { throw 'Private game installation failed. See the installer output; the Steam source was not modified.' }
 }
} finally { $env:DOTNET_ROOT=$oldDotnet }
$state=Get-Content -LiteralPath (Join-Path $runtime '.anomaly_install.json') -Raw | ConvertFrom-Json
if($state.Patched -ne $true -or $state.GameVersion -ne '14.2.7') { throw 'Private runtime did not pass the supported-build check.' }
New-Item -ItemType Directory -Path (Join-Path $runtime 'Mods') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'payload/SLcraft.Client.dll') -Destination (Join-Path $runtime 'Mods/SLcraft.Client.dll') -Force
'gamedir_for_configs: true' | Set-Content -LiteralPath (Join-Path $runtime 'hoster_policy.txt') -Encoding ascii
@{Version=$config.version;JavaHome=$java;SourceGame=$SLPath;InstalledAt=[DateTimeOffset]::Now.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $local 'installed.json') -Encoding utf8
$env:JAVA_HOME=$java
Push-Location (Join-Path $PSScriptRoot 'src/minecraft')
try {
 & .\gradlew.bat classes clientClasses configureClientLaunch --no-daemon --console=plain
 if($LASTEXITCODE -ne 0) { throw 'Minecraft preparation failed. Check Internet access and the installed JDK, then rerun Setup.cmd.' }
} finally { Pop-Location }
'Setup complete. Start-Client.cmd launches the private offline local host.'
