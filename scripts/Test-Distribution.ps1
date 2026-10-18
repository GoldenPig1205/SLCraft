param([string]$ZipPath)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$package=Join-Path $root 'distribution'
if(!$ZipPath) { $ZipPath=Join-Path $root 'releases/SLCraft-Client-0.2.6.zip' }
. (Join-Path $package 'Release.Common.ps1')
Assert-Payload
$checks=0
foreach($scriptFile in Get-ChildItem -LiteralPath $package -File -Filter '*.ps1') {
 $errors=$null; $tokens=$null
 [void][Management.Automation.Language.Parser]::ParseFile($scriptFile.FullName,[ref]$tokens,[ref]$errors)
 if($errors.Count) { throw "PowerShell syntax error: $($scriptFile.Name): $errors" }; $checks++
}
foreach($pair in @(@('C:\SLCraftGame','C:\SLCraftGame'),@('C:\SLCraftGame','C:\SLCraftGame\sub'),@('C:\SLCraftGame\sub','C:\SLCraftGame'))) {
 $rejected=$false
 try { Assert-IsolatedPath $pair[0] $pair[1] } catch { $rejected=$true }
 if(!$rejected) { throw 'Overlapping installation paths were accepted.' }; $checks++
}
Assert-IsolatedPath 'C:\SLCraftGame' 'C:\SLCraftGameSeparate'; $checks++
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($ZipPath))
try {
 $prefix='SLCraft-Client-0.2.6/'
 $names=@($zip.Entries.FullName)
 foreach($required in @('Setup.cmd','Setup.ps1','Start-Client.cmd','README.ko.md','LICENSE','payload/manifest.json','payload/SLcraft.Client.dll','payload/slcraft-minecraft-client-0.2.6.jar','src/installer/Program.cs','src/client/Core.cs','src/minecraft/gradle/wrapper/gradle-wrapper.jar','SHA256SUMS.txt')) {
  if($names -notcontains ($prefix+$required)) { throw "Missing ZIP file: $required" }; $checks++
 }
 foreach($entry in $zip.Entries) {
  if($entry.FullName -match '(^|/)(\.tools|\.local|\.git|\.gradle|build|bin|obj|run|saves|migration-original|work)/' -or $entry.FullName -match '\.(exe|pdb|dmp|log|bak)$' -or $entry.Length -ge 50MB) { throw "Private/generated/large file in ZIP: $($entry.FullName)" }
 }
 $sumEntry=$zip.GetEntry($prefix+'SHA256SUMS.txt')
 $reader=[IO.StreamReader]::new($sumEntry.Open())
 try { $lines=$reader.ReadToEnd() -split '\r?\n' | Where-Object { $_ } } finally { $reader.Dispose() }
 if(@($lines).Count -ne $zip.Entries.Count-1) { throw 'ZIP checksum manifest does not cover all files.' }
 foreach($line in $lines) {
  if($line -notmatch '^([0-9a-f]{64})  (.+)$') { throw 'Invalid checksum line' }
  $expected=$Matches[1]; $name=$Matches[2]; $entry=$zip.GetEntry($prefix+$name)
  if(!$entry) { throw "Checksum entry missing: $name" }
  $hash=[Security.Cryptography.SHA256]::Create(); $inputStream=$entry.Open()
  try { $actual=([BitConverter]::ToString($hash.ComputeHash($inputStream))).Replace('-','').ToLowerInvariant() } finally { $inputStream.Dispose(); $hash.Dispose() }
  if($actual -ne $expected) { throw "ZIP content hash mismatch: $name" }; $checks++
 }
} finally { $zip.Dispose() }
Push-Location $root
try {
 foreach($ignored in @('work/slcraft-client-runtime/SCPSL.exe','migration-original/outputs/anything','outputs/SLcraft-0.1.0.zip','distribution/.tools/download.zip','distribution/src/minecraft/run/saves/example/level.dat','distribution/src/client/obj/project.assets.json')) {
  & git check-ignore --quiet -- $ignored
  if($LASTEXITCODE -ne 0) { throw "Private path is not ignored: $ignored" }; $checks++
 }
 foreach($shared in @('distribution/Setup.ps1','distribution/payload/SLcraft.Client.dll','distribution/payload/slcraft-minecraft-client-0.2.6.jar','distribution/src/client/Core.cs','distribution/src/minecraft/gradle/wrapper/gradle-wrapper.jar','scripts/Build-Distribution.ps1')) {
  & git check-ignore --quiet -- $shared
  if($LASTEXITCODE -eq 0) { throw "Required source/payload path is ignored: $shared" }; $checks++
 }
 $shareFiles=@(& git ls-files --cached --others --exclude-standard | ForEach-Object { if(Test-Path -LiteralPath $_ -PathType Leaf) { Get-Item -LiteralPath $_ } })
 if($shareFiles | Where-Object { $_.Length -ge 50MB }) { throw 'A Git-visible file exceeds 50 MiB.' }
 $shareBytes=($shareFiles | Measure-Object Length -Sum).Sum
 [pscustomobject]@{Checks=$checks;ZipBytes=(Get-Item -LiteralPath $ZipPath).Length;GitVisibleFiles=$shareFiles.Count;GitVisibleBytes=$shareBytes;Result='PASS'} | ConvertTo-Json
} finally { Pop-Location }
