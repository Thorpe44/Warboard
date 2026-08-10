@echo off
setlocal
cd /d "%~dp0"

echo.
echo Starting WARBOARD v45.7a wood-table injection fix...
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0FIX_WARBOARD_V45_7A_WOOD_TABLE.ps1"

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
