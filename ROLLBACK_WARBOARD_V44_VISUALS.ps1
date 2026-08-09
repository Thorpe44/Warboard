$ErrorActionPreference = "Stop"

function Find-WarboardRoot {
    param([string]$Start)

    $dir = (Resolve-Path $Start).Path

    for ($i = 0; $i -lt 8; $i++) {
        $target = Join-Path $dir "Assets\Scripts\Core\GameController.UI.cs"

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
    Write-Host "FAILED: Could not find the Warboard project."
    exit 1
}

$Core =
    Join-Path $Root "Assets\Scripts\Core"

$targets = @(
    "GameController.UI.cs",
    "GameController.Core.cs",
    "ObjectiveController.cs",
    "ModelToken.cs",
    "BattlefieldWorldUI.cs",
    "WarboardBuildInfo.cs"
)

Write-Host "Warboard root:"
Write-Host "  $Root"
Write-Host ""
Write-Host "Restoring files from the backups created BEFORE v44 visual polish..."
Write-Host ""

$restored = 0
$missing = @()

foreach ($name in $targets) {
    $path =
        Join-Path $Core $name

    $pattern =
        $name +
        ".before_v44_visual_*.bak"

    $backup =
        Get-ChildItem `
            -Path $Core `
            -Filter $pattern `
            -File `
            -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $backup) {
        Write-Host ("[MISSING] " + $name)
        $missing += $name
        continue
    }

    Copy-Item `
        -Path $backup.FullName `
        -Destination $path `
        -Force

    Write-Host (
        "[RESTORED] " +
        $name +
        " <- " +
        $backup.Name
    )

    $restored++
}

# WarboardVisualTheme.cs was introduced by v44. Remove it unless a
# pre-v44 backup proves it existed before the patch.
$theme =
    Join-Path $Core "WarboardVisualTheme.cs"

$themeBackups =
    Get-ChildItem `
        -Path $Core `
        -Filter "WarboardVisualTheme.cs.before_v44_visual_*.bak" `
        -File `
        -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending

if ($themeBackups.Count -gt 0) {
    Copy-Item `
        -Path $themeBackups[0].FullName `
        -Destination $theme `
        -Force

    Write-Host "[RESTORED] WarboardVisualTheme.cs from pre-v44 backup"
}
else {
    if (Test-Path $theme) {
        Remove-Item $theme -Force
        Write-Host "[REMOVED] WarboardVisualTheme.cs"
    }

    $themeMeta =
        $theme + ".meta"

    if (Test-Path $themeMeta) {
        Remove-Item $themeMeta -Force
        Write-Host "[REMOVED] WarboardVisualTheme.cs.meta"
    }
}

# Remove marker files from the failed visual/encoding attempts.
$markers = @(
    "WARBOARD_V44_VISUAL_POLISH_INSTALLED.txt",
    "WARBOARD_V44_ENCODING_HOTFIX_INSTALLED.txt",
    "WARBOARD_V44_ENCODING_HOTFIX_V2_INSTALLED.txt"
)

foreach ($marker in $markers) {
    $markerPath =
        Join-Path $Root $marker

    if (Test-Path $markerPath) {
        Remove-Item $markerPath -Force
    }
}

Write-Host ""

if ($missing.Count -gt 0) {
    Write-Host "ROLLBACK INCOMPLETE."
    Write-Host "The following pre-v44 backups were not found:"

    foreach ($name in $missing) {
        Write-Host ("  " + $name)
    }

    Write-Host ""
    Write-Host "Do not continue patching. Send ChatGPT this window."
    exit 2
}

# Verification:
$ui =
    Get-Content -Raw -Encoding UTF8 (
        Join-Path $Core "GameController.UI.cs"
    )

$coreText =
    Get-Content -Raw -Encoding UTF8 (
        Join-Path $Core "GameController.Core.cs"
    )

$build =
    Get-Content -Raw -Encoding UTF8 (
        Join-Path $Core "WarboardBuildInfo.cs"
    )

$badThemeHook =
    $ui -match "WarboardVisualTheme" -or
    $coreText -match "WarboardVisualTheme"

$stillV44 =
    $build -match 'CurrentVersion\s*=\s*"v44\.0"'

if ($badThemeHook) {
    Write-Host "[FAIL] A v44 visual-theme hook still remains."
    exit 3
}

if ($stillV44) {
    Write-Host "[FAIL] Build version still says v44.0 after rollback."
    exit 4
}

Write-Host "[PASS] All six pre-v44 source files restored."
Write-Host "[PASS] v44 visual-theme hooks removed."
Write-Host "[PASS] v44 build identity removed."
Write-Host ""
Write-Host "SUCCESS - FULL V44 VISUAL ROLLBACK VERIFIED"
Write-Host ""
Write-Host "Return to Unity and let it recompile."
Write-Host "This restores the game to the state before the v44 visual-polish installer."
