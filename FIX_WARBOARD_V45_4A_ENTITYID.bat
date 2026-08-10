@echo off
setlocal
cd /d "%~dp0"

echo.
echo Starting WARBOARD v45.4a Unity 6.5 compile fix...
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0FIX_WARBOARD_V45_4A_ENTITYID.ps1"

echo.
if errorlevel 1 (
    echo FIX FAILED.
    echo The error above will remain visible.
) else (
    echo FIX FINISHED.
    echo Return to Unity and let it compile.
)
echo.
pause
endlocal
