. (Join-Path $PSScriptRoot 'Release.Common.ps1')
$info=Get-Content -LiteralPath (Join-Path $PSScriptRoot '.local/installed.json') -Raw | ConvertFrom-Json
$env:JAVA_HOME=$info.JavaHome
foreach($key in @('SLCRAFT_CLIENT_TEST','SLCRAFT_SMOKE_TEST','SLCRAFT_ENTITY_TEST','SKYCRAFT_LAN_PORT','SKYCRAFT_LAN_OFFLINE')) { [Environment]::SetEnvironmentVariable($key,$null,'Process') }
Set-Location -LiteralPath (Join-Path $PSScriptRoot 'src/minecraft')
& .\gradlew.bat runClient --no-daemon --console=plain *> (Join-Path $PSScriptRoot '.local/minecraft-client.log')
exit $LASTEXITCODE
