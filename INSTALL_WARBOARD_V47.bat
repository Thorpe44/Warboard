@echo off
setlocal
cd /d "%~dp0"

echo.
echo Starting WARBOARD v47a installer fix - rules-engine expansion...
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V47.ps1"

if errorlevel 1 (
    echo.
    echo V47 INSTALL FAILED.
    echo The error above will remain visible.
    echo.
    pause
    exit /b 1
)

echo.
echo V47 INSTALL FINISHED.
echo Return to Unity and let it compile/import.
echo.
pause
endlocal
