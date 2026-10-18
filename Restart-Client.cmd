@echo off
if exist "%~dp0distribution\.local\installed.json" goto packaged
if exist "%~dp0outputs\SLcraftClient\Restart-Client.cmd" (
 call "%~dp0outputs\SLcraftClient\Restart-Client.cmd"
 exit /b
)
:packaged
call "%~dp0distribution\Restart-Client.cmd"
