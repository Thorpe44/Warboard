@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0FIX_WARBOARD_V44_2_NECRON_MIGRATION.bat.ps1"
endlocal
