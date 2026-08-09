$ErrorActionPreference = "Stop"

function Find-WarboardRoot {
    param([string]$Start)

    $dir = (Resolve-Path $Start).Path

    for ($i = 0; $i -lt 8; $i++) {
        $target = Join-Path $dir "Assets\Scripts\Core\BattlefieldWorldUI.cs"

        if (Test-Path $target) {
            return $dir
        }

        $parent = Split-Path -Parent $dir

        if ([string]::IsNullOrWhiteSpace($parent) -or
            $parent -eq $dir) {
            break
        }

        $dir = $parent
    }

    return $null
}

$Root = Find-WarboardRoot $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($Root)) {
    Write-Host "FAILED: Could not find Warboard project."
    exit 1
}

$Target =
    Join-Path $Root "Assets\Scripts\Core\BattlefieldWorldUI.cs"

$content =
    Get-Content -Raw -Path $Target

$stamp =
    Get-Date -Format "yyyyMMdd_HHmmss"

Copy-Item $Target "$Target.before_v44_hotfix_$stamp.bak" -Force

$changed = $false

# Main malformed token produced by v44.0 installer:
if ($content.Contains('$10.075f')) {
    $replacement = @'
background.transform.localScale =
            new Vector3(
                width,
                height,
                0.075f
'@

    $content =
        $content.Replace(
            '$10.075f',
            $replacement
        )

    $changed = $true
}

# Fallback: repair a bare $1 artifact if one exists near this assignment.
$pattern =
    '(?ms)background\.transform\.localPosition\s*=\s*Vector3\.zero\s*;\s*' +
    '\$1?0?\.?075f\s*\)\s*;'

if ($content -match $pattern) {
    $replacement = @'
background.transform.localPosition =
            Vector3.zero;

        background.transform.localScale =
            new Vector3(
                width,
                height,
                0.075f
            );
'@

    $content =
        [regex]::Replace(
            $content,
            $pattern,
            $replacement,
            1
        )

    $changed = $true
}

Set-Content -Path $Target -Value $content -Encoding UTF8

$verify =
    Get-Content -Raw -Path $Target

$hasBadDollar =
    $verify -match '\$10?\.075f|\$1'

$hasCorrectScale =
    $verify -match
        '(?ms)background\.transform\.localScale\s*=\s*new\s+Vector3\s*\(\s*width\s*,\s*height\s*,\s*0\.075f\s*\)'

Write-Host ""
Write-Host "Warboard v44 world-UI hotfix"
Write-Host ""

if ($hasBadDollar) {
    Write-Host "[FAIL] Malformed dollar token still exists."
    exit 2
}

if (!$hasCorrectScale) {
    Write-Host "[FAIL] Correct world-panel scale block was not found."
    exit 3
}

Write-Host "[PASS] Malformed `$10.075f token removed"
Write-Host "[PASS] BattlefieldWorldUI scale block restored"
Write-Host ""
Write-Host "SUCCESS - v44.0 HOTFIX VERIFIED"
Write-Host ""
Write-Host "Return to Unity and let it recompile."
