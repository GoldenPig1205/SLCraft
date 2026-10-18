param([switch]$Restart)
. (Join-Path $PSScriptRoot 'Release.Common.ps1')
Assert-Payload
$installed=Join-Path $PSScriptRoot '.local/installed.json'
if(!(Test-Path -LiteralPath $installed)) { throw 'Run Setup.cmd first. See README.md for prerequisites.' }
$info=Get-Content -LiteralPath $installed -Raw | ConvertFrom-Json
$runtime=Get-PrivateGamePath
$otherClones=@(Get-CimInstance Win32_Process -Filter "Name='SCPSL.exe'" | Where-Object {
 $_.ExecutablePath -and $_.ExecutablePath -ne (Join-Path $runtime 'SCPSL.exe') -and
 (Test-Path -LiteralPath (Join-Path ([IO.Path]::GetDirectoryName($_.ExecutablePath)) 'Mods/SLcraft.Client.dll'))
})
if($otherClones.Count) { throw 'Another SLCraft copy is running. Close it first; only one linked local session can run on this PC.' }
$state=Get-Content -LiteralPath (Join-Path $runtime '.anomaly_install.json') -Raw | ConvertFrom-Json
if($state.Patched -ne $true -or $state.GameVersion -ne '14.2.7') { throw 'The private offline runtime is not ready. Rerun Setup.cmd.' }
$running=@(Get-OwnedGames)
if($running.Count -and !$Restart) { throw 'SLCraft is already running. Close it or use Restart-Client.cmd.' }
if($running.Count) {
 foreach($game in $running | Where-Object { $_.Name -eq 'SCPSL.exe' }) {
  $live=Get-Process -Id $game.ProcessId -ErrorAction SilentlyContinue
  if($live) { $live.CloseMainWindow() | Out-Null }
 }
 $deadline=[DateTime]::UtcNow.AddSeconds(20)
 do {
  if(@(Get-OwnedGames).Count -eq 0) { break }
  Start-Sleep -Milliseconds 250
 } while([DateTime]::UtcNow -lt $deadline)
 if(@(Get-OwnedGames).Count) { throw 'The old game is still saving or not responding. Close it manually before restarting; no process was force-killed.' }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'payload/SLcraft.Client.dll') -Destination (Join-Path $runtime 'Mods/SLcraft.Client.dll') -Force
$start=[Diagnostics.ProcessStartInfo]::new((Join-Path $runtime 'SCPSL.exe'))
$start.WorkingDirectory=$runtime
$start.UseShellExecute=$false
$start.Arguments='-noauth -scs -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile client-unity.log --melonloader.hideconsole'
$start.EnvironmentVariables['DOTNET_ROOT']=Join-Path $PSScriptRoot '.local/dotnet6'
$start.EnvironmentVariables['SLCRAFT_LOCAL_HOST']='1'
foreach($key in @('SLCRAFT_CLIENT_TEST','SLCRAFT_SMOKE_TEST','SLCRAFT_ENTITY_TEST','SKYCRAFT_LAN_PORT','SKYCRAFT_LAN_OFFLINE')) { $start.EnvironmentVariables.Remove($key) }
$process=[Diagnostics.Process]::Start($start)
Start-Process -FilePath powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+(Join-Path $PSScriptRoot 'Run-Minecraft.ps1')+'"')) -WindowStyle Hidden
Write-Output "SLCraft started (SL PID $($process.Id)). First launch can take several minutes. E inventory; / chat; M admin; ~ SL console; F10 release."
