@echo off
setlocal
cd /d "%~dp0"

echo.
echo Starting WARBOARD v45.5 top bar / score / dice patch...
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V45_5_TOPBAR_SCORE_DICE.ps1"

echo.
if errorlevel 1 (
    echo PATCH FAILED.
    echo The error above will remain visible.
) else (
    echo PATCH FINISHED.
    echo Return to Unity and let it compile.
)
echo.
pause
endlocal
