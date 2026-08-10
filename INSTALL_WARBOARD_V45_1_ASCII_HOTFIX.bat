@echo off
setlocal
cd /d "%~dp0"

echo.
echo Starting WARBOARD v45.1 UI text hotfix...
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V45_1_ASCII_HOTFIX.ps1"

echo.
if errorlevel 1 (
    echo HOTFIX FAILED.
    echo The error above will remain visible so it can be photographed.
) else (
    echo HOTFIX FINISHED.
    echo Return to Unity and let it compile.
)
echo.
pause
endlocal
