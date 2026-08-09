@echo off
setlocal
cd /d "%~dp0"
echo ============================================================
echo WARBOARD v44.0 - COMPILE HOTFIX
echo ============================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0FIX_WARBOARD_V44_COMPILE_ERROR.ps1"
set ERR=%ERRORLEVEL%
echo.
if not "%ERR%"=="0" (
    echo HOTFIX FAILED - error code %ERR%
    echo Send ChatGPT a screenshot of this window.
) else (
    echo HOTFIX COMPLETE.
    echo Return to Unity and let it recompile.
)
echo.
pause
exit /b %ERR%
