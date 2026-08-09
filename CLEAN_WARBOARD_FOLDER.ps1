$ErrorActionPreference = "Stop"

function Find-WarboardRoot {
    param([string]$Start)

    $dir = (Resolve-Path $Start).Path

    for ($i = 0; $i -lt 8; $i++) {
        if ((Test-Path (Join-Path $dir "Assets")) -and
            (Test-Path (Join-Path $dir "Packages")) -and
            (Test-Path (Join-Path $dir "ProjectSettings"))) {
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
    Write-Host "FAILED: Could not find the Warboard project root."
    exit 1
}

Write-Host "Warboard root:"
Write-Host "  $Root"
Write-Host ""
Write-Host "Cleaning one-off Warboard setup/repair files..."
Write-Host ""

$deleted = 0

function Remove-KnownFile {
    param([string]$RelativePath)

    $path = Join-Path $Root $RelativePath

    if (Test-Path $path) {
        Remove-Item $path -Force
        Write-Host ("[REMOVED] " + $RelativePath)
        $script:deleted++
    }
}

# ------------------------------------------------------------------
# Root-level one-off installers / hotfixes / markers created here.
# ------------------------------------------------------------------
$rootFiles = @(
    "INSTALL_CUSTODES_MODELPACK.bat",

    "FIX_ROSTER_ABILITY_WARNINGS.ps1",
    "FIX_ROSTER_ABILITY_WARNINGS.bat",
    "FIX_ROSTER_ABILITY_WARNINGS_V2.ps1",
    "FIX_ROSTER_ABILITY_WARNINGS_V2.bat",

    "INSTALL_WARBOARD_V44_VISUAL_POLISH.ps1",
    "INSTALL_WARBOARD_V44_VISUAL_POLISH.bat",

    "FIX_WARBOARD_V44_COMPILE_ERROR.ps1",
    "FIX_WARBOARD_V44_COMPILE_ERROR.bat",

    "FIX_WARBOARD_V44_ENCODING.ps1",
    "FIX_WARBOARD_V44_ENCODING.bat",
    "FIX_WARBOARD_V44_ENCODING_V2.ps1",
    "FIX_WARBOARD_V44_ENCODING_V2.bat",

    "ROLLBACK_WARBOARD_V44_VISUALS.ps1",
    "ROLLBACK_WARBOARD_V44_VISUALS.bat",

    "ABILITY_WARNING_FIX_V2_INSTALLED.txt",
    "WARBOARD_V44_VISUAL_POLISH_INSTALLED.txt",
    "WARBOARD_V44_ENCODING_HOTFIX_INSTALLED.txt",
    "WARBOARD_V44_ENCODING_HOTFIX_V2_INSTALLED.txt"
)

foreach ($file in $rootFiles) {
    Remove-KnownFile $file
}

# Remove the cleanup ZIP/readme names only if they were extracted here.
$optionalRootPatterns = @(
    "Warboard_Custodes_ModelPack_Part*_of_6.zip",
    "Warboard_AbilityWarningFix*.zip",
    "Warboard_v44*.zip"
)

foreach ($pattern in $optionalRootPatterns) {
    Get-ChildItem -Path $Root -Filter $pattern -File -ErrorAction SilentlyContinue |
        ForEach-Object {
            Remove-Item $_.FullName -Force
            Write-Host ("[REMOVED] " + $_.Name)
            $script:deleted++
        }
}

# ------------------------------------------------------------------
# Timestamped / temporary backups created by our patch scripts.
# Only very specific suffixes are targeted.
# ------------------------------------------------------------------
$backupPatterns = @(
    "*.ability-warning-backup",
    "*.before_ability_fix_*.bak",
    "*.before_v44_visual_*.bak",
    "*.before_v44_hotfix_*.bak",
    "*.before_encoding_hotfix_*.bak",
    "*.before_encoding_v2_*.bak"
)

$CoreDir = Join-Path $Root "Assets\Scripts\Core"

if (Test-Path $CoreDir) {
    foreach ($pattern in $backupPatterns) {
        Get-ChildItem -Path $CoreDir -Filter $pattern -File -ErrorAction SilentlyContinue |
            ForEach-Object {
                Remove-Item $_.FullName -Force
                Write-Host (
                    "[REMOVED BACKUP] Assets\Scripts\Core\" +
                    $_.Name
                )
                $script:deleted++
            }
    }
}

# ------------------------------------------------------------------
# Remove stale meta files for files that no longer exist.
# Only the exact v44 helper is targeted.
# ------------------------------------------------------------------
$theme = Join-Path $CoreDir "WarboardVisualTheme.cs"
$themeMeta = $theme + ".meta"

if (!(Test-Path $theme) -and (Test-Path $themeMeta)) {
    Remove-Item $themeMeta -Force
    Write-Host "[REMOVED] Assets\Scripts\Core\WarboardVisualTheme.cs.meta"
    $deleted++
}

# ------------------------------------------------------------------
# Verification - do not touch real project content.
# ------------------------------------------------------------------
$required = @(
    "Assets",
    "Packages",
    "ProjectSettings",
    "Assets\Scripts\Core\GameController.UI.cs",
    "Assets\Scripts\Core\GameController.Core.cs",
    "Assets\Scripts\Core\SquadController.cs"
)

$missing = @()

foreach ($relative in $required) {
    if (!(Test-Path (Join-Path $Root $relative))) {
        $missing += $relative
    }
}

Write-Host ""

if ($missing.Count -gt 0) {
    Write-Host "WARNING: Required project items are missing:"
    foreach ($item in $missing) {
        Write-Host ("  " + $item)
    }
    exit 2
}

Write-Host ("[PASS] Removed " + $deleted + " temporary/installer file(s).")
Write-Host "[PASS] Assets folder intact."
Write-Host "[PASS] Packages folder intact."
Write-Host "[PASS] ProjectSettings folder intact."
Write-Host "[PASS] Core Warboard source files intact."
Write-Host ""
Write-Host "SUCCESS - WARBOARD FOLDER CLEANUP COMPLETE"
Write-Host ""
Write-Host "This cleanup did NOT remove game assets, Custodes models, faction rules,"
Write-Host "model indexes, roster code, Unity meta files for live assets, or project settings."
