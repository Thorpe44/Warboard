@echo off
setlocal
cd /d "%~dp0"
echo ============================================================
echo WARBOARD v44.0 - VISUAL POLISH
echo ============================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V44_VISUAL_POLISH.ps1"
set ERR=%ERRORLEVEL%
echo.
if not "%ERR%"=="0" (
    echo INSTALL FAILED - error code %ERR%
    echo Send ChatGPT a screenshot of this window.
) else (
    echo INSTALL COMPLETE AND VERIFIED.
    echo Open Unity, wait for compilation, then reload the battle.
)
echo.
pause
exit /b %ERR%
