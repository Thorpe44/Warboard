@echo off
setlocal
cd /d "%~dp0"

echo.
echo Starting WARBOARD v45.2 deployment centering patch...
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V45_2_CENTER_DEPLOYMENT.ps1"

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
