@echo off
setlocal
cd /d "%~dp0"

echo.
echo Starting WARBOARD v46 deep-audit faction install...
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V46.ps1"

echo.
if errorlevel 1 (
    echo V46 INSTALL FAILED.
    echo Baseline files were rolled back automatically.
    echo The error above will remain visible.
) else (
    echo V46 INSTALL FINISHED.
    echo Return to Unity and let it compile/import.
)
echo.
pause
endlocal
