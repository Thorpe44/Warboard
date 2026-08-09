@echo off
setlocal
cd /d "%~dp0"
echo ============================================================
echo WARBOARD - ROSTER ABILITY WARNING FIX V2
echo ============================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0FIX_ROSTER_ABILITY_WARNINGS_V2.ps1"
set ERR=%ERRORLEVEL%
echo.
if not "%ERR%"=="0" (
    echo PATCH FAILED - error code %ERR%
    echo Send ChatGPT a screenshot of this window.
) else (
    echo PATCH COMPLETE AND VERIFIED.
    echo Return to Unity and let it compile, then reload the roster.
)
echo.
pause
exit /b %ERR%
