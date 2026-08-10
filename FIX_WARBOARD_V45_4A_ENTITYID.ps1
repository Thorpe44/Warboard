$ErrorActionPreference = "Stop"

try {
    Set-Location $PSScriptRoot

    Write-Host ""
    Write-Host "WARBOARD v45.4a - UNITY 6.5 ENTITY ID COMPILE FIX" -ForegroundColor Cyan
    Write-Host "=================================================="
    Write-Host ""

    $Root = $PSScriptRoot
    $Target =
        Join-Path $Root "Assets\Scripts\Core\WarboardV45PhysicalSideTrays.cs"

    if (-not (Test-Path $Target)) {
        throw "Could not find Assets\Scripts\Core\WarboardV45PhysicalSideTrays.cs. Extract this ZIP into the Warboard project root."
    }

    $Text =
        [System.IO.File]::ReadAllText(
            $Target
        )

    $Original = $Text

    $Text =
        $Text.Replace(
            ".GetInstanceID()",
            ".GetEntityId()"
        )

    if ($Text -eq $Original) {
        if ($Text.Contains(".GetEntityId()")) {
            Write-Host "[OK] The EntityId fix is already installed." -ForegroundColor DarkGreen
            Write-Host ""
            exit 0
        }

        throw "Could not find GetInstanceID() in WarboardV45PhysicalSideTrays.cs."
    }

    $BackupRoot =
        Join-Path $Root "Library\WarboardBackups\V45_4aEntityIdFix"

    New-Item -ItemType Directory -Force -Path $BackupRoot |
        Out-Null

    $Backup =
        Join-Path $BackupRoot "WarboardV45PhysicalSideTrays.cs"

    if (-not (Test-Path $Backup)) {
        Copy-Item `
            -LiteralPath $Target `
            -Destination $Backup `
            -Force
    }

    [System.IO.File]::WriteAllText(
        $Target,
        $Text,
        [System.Text.UTF8Encoding]::new($false)
    )

    Write-Host "[FIXED] GetInstanceID() -> GetEntityId()" -ForegroundColor Green
    Write-Host ""
    Write-Host "Return to Unity and let it recompile." -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "FIX FAILED" -ForegroundColor Red
    Write-Host "----------" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkRed
    Write-Host ""
    exit 1
}
