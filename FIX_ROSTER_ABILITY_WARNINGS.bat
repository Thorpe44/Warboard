@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0FIX_ROSTER_ABILITY_WARNINGS.ps1"
echo.
pause
