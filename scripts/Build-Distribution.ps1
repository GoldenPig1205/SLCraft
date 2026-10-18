param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$source=Join-Path $root 'distribution'
if(!$OutputDirectory) { $OutputDirectory=Join-Path $root 'releases' }
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
$config=Get-Content (Join-Path $source 'dependencies.json') -Raw | ConvertFrom-Json
. (Join-Path $source 'Release.Common.ps1')
Assert-Payload
$omit=@('.local','.tools','.gradle','build','bin','obj','run','saves','logs','.git')
$files=@(Get-ChildItem -LiteralPath $source -File -Recurse -Force | Where-Object {
 $parts=([IO.Path]::GetRelativePath($source,$_.FullName) -split '[\\/]')
 !($parts | Where-Object { $_ -in $omit }) -and $_.Extension -notin @('.log','.pid','.pdb','.dmp','.bak','.zip')
})
foreach($file in $files) {
 $relative=[IO.Path]::GetRelativePath($source,$file.FullName).Replace('\','/')
 if($file.Length -ge 50MB) { throw "Unexpected large file: $relative" }
 if($file.Extension -eq '.dll' -and $relative -ne 'payload/SLcraft.Client.dll') { throw "Unexpected DLL: $relative" }
 if($file.Extension -eq '.jar' -and $relative -notin @('payload/slcraft-minecraft-client-0.2.6.jar','src/minecraft/gradle/wrapper/gradle-wrapper.jar')) { throw "Unexpected JAR: $relative" }
 if($file.Extension -eq '.exe') { throw "Game/tool executables must not be packaged: $relative" }
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$zipPath=Join-Path $OutputDirectory ('SLCraft-Client-'+$config.version+'.zip')
$stream=[IO.File]::Open($zipPath,[IO.FileMode]::Create)
$archive=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create)
$checksums=[Collections.Generic.List[string]]::new()
$prefix='SLCraft-Client-'+$config.version+'/'
try {
 foreach($file in $files | Sort-Object FullName) {
  $relative=[IO.Path]::GetRelativePath($source,$file.FullName).Replace('\','/')
  [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$file.FullName,$prefix+$relative,[IO.Compression.CompressionLevel]::Optimal)
  $checksums.Add((Get-FileHash -LiteralPath $file.FullName).Hash.ToLowerInvariant()+'  '+$relative)
 }
 $entry=$archive.CreateEntry($prefix+'SHA256SUMS.txt')
 $writer=[IO.StreamWriter]::new($entry.Open())
 try { $writer.WriteLine(($checksums -join "`n")) } finally { $writer.Dispose() }
} finally { $archive.Dispose(); $stream.Dispose() }
$size=(Get-Item -LiteralPath $zipPath).Length
Write-Output "Created $zipPath ($size bytes, $($files.Count) packaged files plus SHA256SUMS.txt)."
(Get-FileHash -LiteralPath $zipPath).Hash.ToLowerInvariant()+'  '+[IO.Path]::GetFileName($zipPath) | Set-Content -LiteralPath ($zipPath+'.sha256') -Encoding ascii
