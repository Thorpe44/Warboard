@echo off
setlocal
cd /d "%~dp0"
echo ============================================================
echo WARBOARD - SAFE PROJECT CLEANUP
echo ============================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0CLEAN_WARBOARD_FOLDER.ps1"
set ERR=%ERRORLEVEL%
echo.
if not "%ERR%"=="0" (
    echo CLEANUP REPORTED AN ISSUE - error code %ERR%
    echo Send ChatGPT a screenshot of this window.
) else (
    echo CLEANUP COMPLETE.
)
echo.
pause
exit /b %ERR%
