$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host ""
Write-Host "WARBOARD v26.8 - PROJECT CLEANUP"
Write-Host "================================"
Write-Host ""

$projectRoot = $PSScriptRoot
$assets = Join-Path $projectRoot "Assets"
$packages = Join-Path $projectRoot "Packages"
$settings = Join-Path $projectRoot "ProjectSettings"

if (-not (Test-Path $assets) -or
    -not (Test-Path $packages) -or
    -not (Test-Path $settings)) {
    Write-Host "ERROR: Run this from the Warboard project root." -ForegroundColor Red
    Write-Host "Expected Assets, Packages and ProjectSettings beside this script."
    Read-Host "Press Enter to close"
    exit 1
}

$canonicalRoot = Join-Path $projectRoot "Assets\Resources\Armies\Models\Aeldari"
$indexPath = Join-Path $canonicalRoot "ModelIndex.json"
$modelPool = Join-Path $canonicalRoot "ModelPool"
$texturePool = Join-Path $canonicalRoot "TexturePool"
$basePool = Join-Path $projectRoot "Assets\Resources\Armies\Models\Bases"

if (-not (Test-Path $indexPath) -or
    -not (Test-Path $modelPool) -or
    -not (Test-Path $texturePool)) {
    Write-Host "STOPPED: canonical Aeldari pack is incomplete." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Required:"
    Write-Host "  Assets\Resources\Armies\Models\Aeldari\ModelIndex.json"
    Write-Host "  Assets\Resources\Armies\Models\Aeldari\ModelPool"
    Write-Host "  Assets\Resources\Armies\Models\Aeldari\TexturePool"
    Write-Host ""
    Write-Host "Nothing has been deleted."
    Read-Host "Press Enter to close"
    exit 1
}

$objCount = (Get-ChildItem $modelPool -File -Filter *.obj -ErrorAction SilentlyContinue).Count
$texCount = (Get-ChildItem $texturePool -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Extension.ToLowerInvariant() -in @(".jpg",".jpeg",".png",".webp") }).Count

Write-Host "Canonical model pack:"
Write-Host "  ModelPool OBJ files: $objCount"
Write-Host "  Texture images:      $texCount"
Write-Host ""

if ($objCount -lt 206 -or $texCount -lt 204) {
    Write-Host "STOPPED: model pack counts are below the repaired-pack baseline." -ForegroundColor Yellow
    Write-Host "Expected at least 206 OBJ and 204 texture files."
    Write-Host "Nothing has been deleted."
    Read-Host "Press Enter to close"
    exit 1
}

# Validate every resource in ModelIndex before deleting old fallbacks.
$index = Get-Content $indexPath -Raw | ConvertFrom-Json
$missing = New-Object System.Collections.Generic.List[string]

function Resolve-ResourceFile([string]$resource) {
    if ([string]::IsNullOrWhiteSpace($resource)) { return $true }

    $r = $resource.Replace("/", "\")
    $base = Join-Path $projectRoot ("Assets\Resources\" + $r)
    $found = Get-ChildItem -Path ($base + ".*") -File -ErrorAction SilentlyContinue |
        Select-Object -First 1

    return ($null -ne $found)
}

foreach ($unit in $index.units) {
    if ($null -ne $unit.base -and $unit.base.resource) {
        if (-not (Resolve-ResourceFile ([string]$unit.base.resource))) {
            $missing.Add([string]$unit.base.resource)
        }
    }

    foreach ($component in $unit.components) {
        foreach ($field in @("meshResource","diffuseResource","normalResource")) {
            $value = [string]$component.$field
            if (-not [string]::IsNullOrWhiteSpace($value)) {
                if (-not (Resolve-ResourceFile $value)) {
                    $missing.Add($value)
                }
            }
        }
    }
}

$missing = @($missing | Sort-Object -Unique)

if ($missing.Count -gt 0) {
    Write-Host "STOPPED: ModelIndex still references $($missing.Count) missing resources." -ForegroundColor Yellow
    Write-Host "Nothing has been deleted."
    Write-Host ""
    Write-Host "First missing resource:"
    Write-Host "  $($missing[0])"
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host "ModelIndex verification: PASS" -ForegroundColor Green
Write-Host ""

# Old v26 test/fallback assets are now obsolete.
$oldModelRoot = Join-Path $projectRoot "Assets\Resources\WarboardModels"
$oldModelMeta = Join-Path $projectRoot "Assets\Resources\WarboardModels.meta"

if (Test-Path $oldModelRoot) {
    Write-Host "Removing obsolete Assets\Resources\WarboardModels ..."
    Remove-Item -LiteralPath $oldModelRoot -Recurse -Force
}
if (Test-Path $oldModelMeta) {
    Remove-Item -LiteralPath $oldModelMeta -Force
}

# Delete migration/verification scaffolding from the PROJECT ROOT only.
$obsoleteRootFiles = @(
    "MOVE_AELDARI_PACK_INTO_ARMIES.bat",
    "FIND_AND_MOVE_AELDARI_MODEL_PACK.bat",
    "VERIFY_AND_REPAIR_AELDARI_MODEL_PACK.bat",
    "VERIFY_AND_REPAIR_AELDARI_MODEL_PACK.ps1",
    "AELDARI_MODEL_PACK_VERIFY_REPORT.txt",
    "README_AELDARI_RELOCATOR.txt",
    "README_VERIFY_AELDARI_PACK.txt",
    "AELDARI_MODEL_PACK_README.txt"
)

foreach ($name in $obsoleteRootFiles) {
    $path = Join-Path $projectRoot $name
    if (Test-Path $path) {
        Remove-Item -LiteralPath $path -Force
    }
}

# Remove old generated version/changelog notes from the root.
Get-ChildItem $projectRoot -File -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -match '^README_V[0-9].*\.txt$' -or
        $_.Name -match '^README_AELDARI.*\.txt$'
    } |
    Remove-Item -Force

# Produce one current project map.
$map = @"
WARBOARD v26.8 - CANONICAL PROJECT STRUCTURE

Warboard/
  Assets/
    Resources/
      Armies/
        Models/
          Aeldari/
            ModelIndex.json
            ModelPool/
            TexturePool/
          Bases/
        [army JSON resources]
      Core/
      Factions/
    Scripts/
      Core/
      Factions/
  Packages/
  ProjectSettings/

MODEL RULE
The sole canonical miniature location is:
  Assets/Resources/Armies/Models/<Faction>/

Do not recreate Assets/Resources/WarboardModels.

A unit without a matching installed model-pack entry uses the normal gameplay
capsule instead of an obsolete hard-coded fallback asset.

SAFE TO REGENERATE
Library/
Logs/
Temp/
obj/

DO NOT DELETE
Assets/
Packages/
ProjectSettings/
"@

Set-Content -Path (Join-Path $projectRoot "WARBOARD_PROJECT_STRUCTURE.txt") -Value $map -Encoding UTF8

Write-Host ""
Write-Host "CLEANUP COMPLETE." -ForegroundColor Green
Write-Host ""
Write-Host "Canonical model location:"
Write-Host "  Assets\Resources\Armies\Models\Aeldari"
Write-Host ""
Write-Host "Removed:"
Write-Host "  old WarboardModels fallback tree"
Write-Host "  old migration/verifier scripts"
Write-Host "  old version README files"
Write-Host ""
Write-Host "Created:"
Write-Host "  WARBOARD_PROJECT_STRUCTURE.txt"
Write-Host ""
Write-Host "Reopen Unity. It may briefly reimport after the deleted fallback assets."
Write-Host ""
Read-Host "Press Enter to close"
