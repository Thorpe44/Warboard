@echo off
setlocal
cd /d "%~dp0"
echo ============================================================
echo WARBOARD - FULL v44 VISUAL ROLLBACK
echo ============================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0ROLLBACK_WARBOARD_V44_VISUALS.ps1"
set ERR=%ERRORLEVEL%
echo.
if not "%ERR%"=="0" (
    echo ROLLBACK FAILED - error code %ERR%
    echo Send ChatGPT a screenshot of this window.
) else (
    echo ROLLBACK COMPLETE AND VERIFIED.
    echo Return to Unity and let it recompile.
)
echo.
pause
exit /b %ERR%
