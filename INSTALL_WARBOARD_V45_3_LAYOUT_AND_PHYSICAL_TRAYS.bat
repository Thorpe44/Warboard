@echo off
setlocal
cd /d "%~dp0"

echo.
echo Starting WARBOARD v45.3 layout and physical tray patch...
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V45_3_LAYOUT_AND_PHYSICAL_TRAYS.ps1"

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
