$ErrorActionPreference = "Stop"

try {
    Set-Location $PSScriptRoot

    Write-Host ""
    Write-Host "WARBOARD v45.2 - CENTER DEPLOYMENT PANEL" -ForegroundColor Cyan
    Write-Host "========================================="
    Write-Host ""

    $Root = $PSScriptRoot
    $Target = Join-Path $Root "Assets\Scripts\Core\GameController.UI.cs"

    if (-not (Test-Path $Target)) {
        throw "Could not find Assets\Scripts\Core\GameController.UI.cs. Extract this ZIP into the Warboard project root."
    }

    $Text = [System.IO.File]::ReadAllText($Target)

    if ($Text.Contains("WARBOARD_V45_2_CENTERED_DEPLOYMENT_PANEL")) {
        Write-Host "The deployment centering patch is already installed." -ForegroundColor DarkGreen
        Write-Host ""
        exit 0
    }

    $anchor = "DEPLOYMENT ORDER"
    $anchorIndex = $Text.IndexOf($anchor, [System.StringComparison]::Ordinal)

    if ($anchorIndex -lt 0) {
        throw "Could not find the deployment panel anchor text ('DEPLOYMENT ORDER') in GameController.UI.cs."
    }

    $methodStart = $Text.LastIndexOf("private void ", $anchorIndex, [System.StringComparison]::Ordinal)
    if ($methodStart -lt 0) {
        throw "Could not locate the start of the deployment UI method."
    }

    $nextMethod = $Text.IndexOf("`n    private ", $anchorIndex, [System.StringComparison]::Ordinal)
    if ($nextMethod -lt 0) {
        $nextMethod = $Text.Length
    }

    $methodText = $Text.Substring($methodStart, $nextMethod - $methodStart)

    $rectPattern = 'Rect\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*new Rect\((?s:.*?)\);'
    $rectMatch = [regex]::Match($methodText, $rectPattern)

    if (-not $rectMatch.Success) {
        throw "Could not find the main Rect definition inside the deployment UI method."
    }

    $rectVar = $rectMatch.Groups[1].Value
    $insertion = $rectMatch.Value + "`r`n" +
        "        // WARBOARD_V45_2_CENTERED_DEPLOYMENT_PANEL`r`n" +
        "        $rectVar.x = Mathf.Max(12f, (Screen.width - $rectVar.width) * 0.5f);"

    $methodNew = $methodText.Remove($rectMatch.Index, $rectMatch.Length).Insert($rectMatch.Index, $insertion)
    $newText = $Text.Remove($methodStart, $methodText.Length).Insert($methodStart, $methodNew)

    $BackupRoot = Join-Path $Root "Library\WarboardBackups\V45_2CenteredDeployment"
    New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null
    $Backup = Join-Path $BackupRoot "GameController.UI.cs"
    if (-not (Test-Path $Backup)) {
        Copy-Item -LiteralPath $Target -Destination $Backup -Force
    }

    [System.IO.File]::WriteAllText(
        $Target,
        $newText,
        [System.Text.UTF8Encoding]::new($false)
    )

    Write-Host "Centered the deployment panel." -ForegroundColor Green
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
