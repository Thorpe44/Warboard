@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0CLEAN_WARBOARD_V44_3.ps1"
endlocal
