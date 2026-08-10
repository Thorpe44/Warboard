$ErrorActionPreference = "Stop"

try {
    Set-Location $PSScriptRoot

    Write-Host ""
    Write-Host "WARBOARD v45.3a - TRAY INJECTION FIX" -ForegroundColor Cyan
    Write-Host "===================================="
    Write-Host ""

    $Root = $PSScriptRoot
    $CoreFolder = Join-Path $Root "Assets\Scripts\Core"
    $CoreFile = Join-Path $CoreFolder "GameController.Core.cs"
    $TrayFile = Join-Path $CoreFolder "WarboardV45PhysicalSideTrays.cs"

    foreach ($file in @($CoreFile, $TrayFile)) {
        if (-not (Test-Path $file)) {
            throw "Missing expected file: $file"
        }
    }

    $BackupRoot = Join-Path $Root "Library\WarboardBackups\V45_3aTrayInjection"
    New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null

    function Write-Utf8([string]$Path, [string]$Text) {
        [System.IO.File]::WriteAllText(
            $Path,
            $Text,
            [System.Text.UTF8Encoding]::new($false)
        )
    }

    function Backup-Once([string]$Path) {
        $Name = Split-Path $Path -Leaf
        $Dest = Join-Path $BackupRoot $Name
        if (-not (Test-Path $Dest)) {
            Copy-Item -LiteralPath $Path -Destination $Dest -Force
        }
    }

    $CoreText = [System.IO.File]::ReadAllText($CoreFile)

    if ($CoreText.Contains("WarboardV45PhysicalSideTrays")) {
        Write-Host "[OK] Physical tray system is already referenced in GameController.Core.cs." -ForegroundColor DarkGreen
    }
    else {
        $Pattern = 'battlefieldWorldUI\.Initialize\s*\(\s*this\s*\)\s*;'
        $Match = [regex]::Match($CoreText, $Pattern)

        if (-not $Match.Success) {
            $Pattern = 'battlefieldWorldUI\.Initialize\s*\((?s:.*?)\)\s*;'
            $Match = [regex]::Match($CoreText, $Pattern)
        }

        if (-not $Match.Success) {
            throw "Could not find any battlefieldWorldUI.Initialize(...) call in GameController.Core.cs."
        }

        $Injection = $Match.Value + "`r`n" + @'
        GameObject trayUiObject =
            new GameObject(
                "Warboard v45 Physical Side Trays"
            );

        trayUiObject.AddComponent<
            WarboardV45PhysicalSideTrays
        >();
'@

        $NewCoreText =
            $CoreText.Remove(
                $Match.Index,
                $Match.Length
            ).Insert(
                $Match.Index,
                $Injection
            )

        Backup-Once $CoreFile
        Write-Utf8 $CoreFile $NewCoreText

        Write-Host "[FIXED] Injected the physical tray runtime after battlefieldWorldUI.Initialize(...)." -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "PATCH COMPLETE." -ForegroundColor Green
    Write-Host ""
    Write-Host "This patch only fixes the failed core injection step."
    Write-Host "Your earlier successful layout changes remain in place."
    Write-Host ""
    Write-Host "Return to Unity and let it recompile." -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "PATCH FAILED" -ForegroundColor Red
    Write-Host "------------" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkRed
    Write-Host ""
    exit 1
}
