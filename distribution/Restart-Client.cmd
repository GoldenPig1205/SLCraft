@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start-Client.ps1" -Restart
pause
