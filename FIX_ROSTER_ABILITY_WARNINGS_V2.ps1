$ErrorActionPreference = "Stop"

function Find-WarboardRoot {
    param([string]$Start)

    $dir = (Resolve-Path $Start).Path

    # First: current folder and parents.
    for ($i = 0; $i -lt 8; $i++) {
        $squad = Join-Path $dir "Assets\Scripts\Core\SquadController.cs"
        $ability = Join-Path $dir "Assets\Scripts\Core\AbilitySystem.cs"

        if ((Test-Path $squad) -and (Test-Path $ability)) {
            return $dir
        }

        $parent = Split-Path -Parent $dir
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $dir) {
            break
        }
        $dir = $parent
    }

    # Second: user may have extracted this into a folder sitting inside Warboard.
    $candidate = Get-ChildItem -Path $Start -Directory -Recurse -ErrorAction SilentlyContinue |
        Where-Object {
            Test-Path (Join-Path $_.FullName "Assets\Scripts\Core\SquadController.cs")
        } |
        Select-Object -First 1

    if ($candidate) {
        return $candidate.FullName
    }

    return $null
}

$Root = Find-WarboardRoot $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($Root)) {
    Write-Host ""
    Write-Host "FAILED: Could not find the Warboard project."
    Write-Host "Put this BAT/PS1 in your Warboard folder (or a folder directly inside it) and run again."
    Write-Host ""
    exit 1
}

$AbilitySystem = Join-Path $Root "Assets\Scripts\Core\AbilitySystem.cs"
$SquadController = Join-Path $Root "Assets\Scripts\Core\SquadController.cs"

Write-Host "Warboard root:"
Write-Host "  $Root"
Write-Host ""

$abilityContent = Get-Content -Raw -Path $AbilitySystem
$squadContent = Get-Content -Raw -Path $SquadController

$changedAbility = $false
$changedSquad = $false

# ----------------------------------------------------------------------
# AbilitySystem.cs
# Add TryCreate immediately before Create if it is not already present.
# ----------------------------------------------------------------------
if ($abilityContent -notmatch 'public\s+static\s+bool\s+TryCreate\s*\(') {

    $createPattern = '(?ms)(^[ \t]*public\s+static\s+IUnitAbility\s+Create\s*\(\s*string\s+id\s*\))'

    if ($abilityContent -notmatch $createPattern) {
        throw "Could not locate AbilityRegistry.Create(string id) in AbilitySystem.cs. Nothing was changed."
    }

    $tryCreate = @'
    public static bool TryCreate(
        string id,
        out IUnitAbility ability)
    {
        ability = null;

        if (string.IsNullOrWhiteSpace(id))
            return false;

        Func<IUnitAbility> factory;

        if (!Factories.TryGetValue(id, out factory))
            return false;

        ability = factory();
        return ability != null;
    }

'@

    $abilityContent = [regex]::Replace(
        $abilityContent,
        $createPattern,
        $tryCreate + '$1',
        1
    )

    $changedAbility = $true
}

# ----------------------------------------------------------------------
# SquadController.cs
# Replace the roster ability instantiation with quiet TryCreate.
# Works across formatting/line-break differences.
# ----------------------------------------------------------------------
if ($squadContent -notmatch 'AbilityRegistry\.TryCreate\s*\(\s*id') {

    $oldPattern = '(?ms)' +
        'IUnitAbility\s+ability\s*=\s*AbilityRegistry\.Create\s*\(\s*id\s*\)\s*;\s*' +
        'if\s*\(\s*ability\s*!=\s*null\s*\)\s*' +
        'abilities\.Add\s*\(\s*ability\s*\)\s*;'

    if ($squadContent -notmatch $oldPattern) {
        throw "Could not locate the AbilityRegistry.Create(id) block in SquadController.cs. Nothing was changed."
    }

    $replacement = @'
IUnitAbility ability;

                // Imported New Recruit/YellowScribe ability names are retained
                // in SourceData for the universal and faction rule engines.
                // Only instantiate entries that are explicitly registered with
                // the legacy IUnitAbility modifier system.
                if (AbilityRegistry.TryCreate(
                        id,
                        out ability) &&
                    ability != null)
                {
                    abilities.Add(ability);
                }
'@

    $squadContent = [regex]::Replace(
        $squadContent,
        $oldPattern,
        $replacement,
        1
    )

    $changedSquad = $true
}

# Back up only immediately before writing.
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"

if ($changedAbility) {
    Copy-Item $AbilitySystem "$AbilitySystem.before_ability_fix_$stamp.bak" -Force
    Set-Content -Path $AbilitySystem -Value $abilityContent -Encoding UTF8
}

if ($changedSquad) {
    Copy-Item $SquadController "$SquadController.before_ability_fix_$stamp.bak" -Force
    Set-Content -Path $SquadController -Value $squadContent -Encoding UTF8
}

# ----------------------------------------------------------------------
# VERIFY THE FILES ON DISK.
# Do not report success unless both expected changes are present.
# ----------------------------------------------------------------------
$verifyAbility = Get-Content -Raw -Path $AbilitySystem
$verifySquad = Get-Content -Raw -Path $SquadController

$ok1 = $verifyAbility -match 'public\s+static\s+bool\s+TryCreate\s*\('
$ok2 = $verifySquad -match 'AbilityRegistry\.TryCreate\s*\(\s*id'
$oldCallStillThere = $verifySquad -match 'AbilityRegistry\.Create\s*\(\s*id\s*\)'

Write-Host ""

if (!$ok1 -or !$ok2 -or $oldCallStillThere) {
    Write-Host "FAILED VERIFICATION."
    Write-Host "TryCreate in AbilitySystem: $ok1"
    Write-Host "TryCreate used by SquadController: $ok2"
    Write-Host "Old Create(id) call still in SquadController: $oldCallStillThere"
    Write-Host ""
    exit 2
}

$marker = Join-Path $Root "ABILITY_WARNING_FIX_V2_INSTALLED.txt"
@"
Warboard roster ability warning fix V2 installed successfully.
Date: $(Get-Date)
AbilitySystem: $AbilitySystem
SquadController: $SquadController

Verified:
- AbilityRegistry.TryCreate exists.
- SquadController uses TryCreate(id).
- SquadController no longer calls AbilityRegistry.Create(id).
"@ | Set-Content -Path $marker -Encoding UTF8

Write-Host "SUCCESS - PATCH VERIFIED ON DISK"
Write-Host ""
Write-Host "Verified:"
Write-Host "  [PASS] AbilityRegistry.TryCreate exists"
Write-Host "  [PASS] SquadController calls TryCreate(id)"
Write-Host "  [PASS] Old AbilityRegistry.Create(id) call is gone from SquadController"
Write-Host ""
Write-Host "Marker written:"
Write-Host "  $marker"
Write-Host ""
Write-Host "Now return to Unity and wait for it to recompile."
