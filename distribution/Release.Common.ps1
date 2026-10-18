$ErrorActionPreference='Stop'
$script:ReleaseRoot=[IO.Path]::GetFullPath($PSScriptRoot)
function Get-ReleaseConfig { Get-Content -LiteralPath (Join-Path $script:ReleaseRoot 'dependencies.json') -Raw | ConvertFrom-Json }
function Assert-IsolatedPath([string]$Source,[string]$Target) {
 $a=[IO.Path]::GetFullPath($Source).TrimEnd('\','/')
 $b=[IO.Path]::GetFullPath($Target).TrimEnd('\','/')
 if($a.Equals($b,[StringComparison]::OrdinalIgnoreCase) -or $a.StartsWith($b+'\',[StringComparison]::OrdinalIgnoreCase) -or $b.StartsWith($a+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Source game and private runtime must be separate directories, with neither inside the other.' }
 foreach($candidate in @($a,$b)) {
  while($candidate) {
   if(Test-Path -LiteralPath $candidate) { if((Get-Item -LiteralPath $candidate -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Use a directory without junctions/symlinks: $candidate" } }
   $candidate=[IO.Path]::GetDirectoryName($candidate)
  }
 }
}
function Assert-Payload {
 $manifest=Get-Content -LiteralPath (Join-Path $script:ReleaseRoot 'payload/manifest.json') -Raw | ConvertFrom-Json
 foreach($file in $manifest.files) {
  $path=Join-Path $script:ReleaseRoot ('payload/'+$file.name)
  if(!(Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.sha256) { throw "SLCraft payload missing or changed: $($file.name)" }
 }
}
function Find-JavaHome {
 $candidates=@()
 if($env:JAVA_HOME) { $candidates+=$env:JAVA_HOME }
 $found=Get-Command java.exe -ErrorAction SilentlyContinue
 if($found) { $candidates+=[IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($found.Source)) }
 foreach($parent in @((Join-Path $env:ProgramFiles 'Java'),(Join-Path $env:ProgramFiles 'Eclipse Adoptium'),(Join-Path $env:ProgramFiles 'Microsoft'))) {
  if(Test-Path -LiteralPath $parent) { $candidates+=@(Get-ChildItem -LiteralPath $parent -Directory | Select-Object -ExpandProperty FullName) }
 }
 foreach($homeCandidate in $candidates | Select-Object -Unique) {
  $release=Join-Path $homeCandidate 'release'
  if((Test-Path -LiteralPath (Join-Path $homeCandidate 'bin/javac.exe')) -and (Test-Path -LiteralPath $release)) {
   $version=Get-Content -LiteralPath $release | Select-String '^JAVA_VERSION="(\d+)'
   if($version -and [int]$version.Matches[0].Groups[1].Value -ge 25) { return [IO.Path]::GetFullPath($homeCandidate) }
  }
 }
 throw 'JDK 25 or newer is required. Install a Windows x64 JDK and set JAVA_HOME, then rerun Setup.cmd.'
}
function Assert-Sdk {
 if(!(Get-Command dotnet.exe -ErrorAction SilentlyContinue)) { throw '.NET SDK 10 or newer is required. Install it, open a new terminal, then rerun Setup.cmd.' }
 $sdks=@(& dotnet.exe --list-sdks)
 if(!($sdks | Where-Object { $_ -match '^(1[0-9]|[2-9][0-9])\.' })) { throw '.NET SDK 10 or newer is required (the runtime alone is insufficient).' }
}
function Get-VerifiedDownload([string]$Url,[string]$Path,[string]$Hash,[string]$Algorithm='SHA256') {
 if(Test-Path -LiteralPath $Path) {
  if((Get-FileHash -LiteralPath $Path -Algorithm $Algorithm).Hash -eq $Hash) { return }
  throw "Cached download is corrupt. Rename it and retry: $Path"
 }
 [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
 Invoke-WebRequest -UseBasicParsing -Uri $Url -OutFile ($Path+'.partial')
 if((Get-FileHash -LiteralPath ($Path+'.partial') -Algorithm $Algorithm).Hash -ne $Hash) { throw "Download checksum mismatch: $Url" }
 Move-Item -LiteralPath ($Path+'.partial') -Destination $Path
}
function Get-PrivateGamePath { Join-Path $script:ReleaseRoot '.local/sl-runtime' }
function Get-OwnedGames {
 $native=Get-PrivateGamePath
 @(Get-CimInstance Win32_Process -Filter "Name='SCPSL.exe' OR Name='java.exe'" | Where-Object {
  $_.ExecutablePath -eq (Join-Path $native 'SCPSL.exe') -or
  ($_.Name -eq 'java.exe' -and $_.CommandLine -like '*devlaunchinjector.Main*' -and $_.CommandLine.IndexOf($script:ReleaseRoot,[StringComparison]::OrdinalIgnoreCase) -ge 0)
 })
}
