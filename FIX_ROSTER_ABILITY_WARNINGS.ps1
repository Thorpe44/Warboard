$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$AbilitySystem = Join-Path $Root "Assets\Scripts\Core\AbilitySystem.cs"
$SquadController = Join-Path $Root "Assets\Scripts\Core\SquadController.cs"

if (!(Test-Path $AbilitySystem)) {
    throw "AbilitySystem.cs not found. Put this patch in the ROOT of the Warboard project."
}

if (!(Test-Path $SquadController)) {
    throw "SquadController.cs not found. Put this patch in the ROOT of the Warboard project."
}

$abilityContent = Get-Content -Raw -Path $AbilitySystem
$squadContent = Get-Content -Raw -Path $SquadController

if ($abilityContent.Contains("public static bool TryCreate(") -and
    $squadContent.Contains("AbilityRegistry.TryCreate(")) {
    Write-Host "Roster ability warning fix is already installed."
    exit 0
}

$abilityOld = @'
    public static IUnitAbility Create(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        if (Factories.TryGetValue(id, out var factory))
            return factory();

        UnityEngine.Debug.LogWarning("Unknown ability id: " + id);
        return null;
    }
'@

$abilityNew = @'
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

    public static IUnitAbility Create(string id)
    {
        IUnitAbility ability;

        if (TryCreate(id, out ability))
            return ability;

        if (!string.IsNullOrWhiteSpace(id))
        {
            UnityEngine.Debug.LogWarning(
                "Unknown ability id: " + id);
        }

        return null;
    }
'@

$squadOld = @'
        if (data.abilities != null)
        {
            foreach (string id in data.abilities)
            {
                IUnitAbility ability =
                    AbilityRegistry.Create(id);

                if (ability != null)
                    abilities.Add(ability);
            }
        }
'@

$squadNew = @'
        if (data.abilities != null)
        {
            foreach (string id in data.abilities)
            {
                // YellowScribe/New Recruit supplies every datasheet rule name
                // here. Most modern rules are consumed directly from
                // SourceData by UniversalRuleRegistry and the faction-pack
                // systems; only explicitly registered legacy ability objects
                // belong in this modifiers list.
                IUnitAbility ability;

                if (AbilityRegistry.TryCreate(
                        id,
                        out ability) &&
                    ability != null)
                {
                    abilities.Add(ability);
                }
            }
        }
'@

if (!$abilityContent.Contains($abilityOld)) {
    throw "Expected AbilityRegistry.Create block was not found. No files were changed."
}

if (!$squadContent.Contains($squadOld)) {
    throw "Expected SquadController ability-loading block was not found. No files were changed."
}

Copy-Item $AbilitySystem "$AbilitySystem.ability-warning-backup" -Force
Copy-Item $SquadController "$SquadController.ability-warning-backup" -Force

$abilityContent = $abilityContent.Replace($abilityOld, $abilityNew)
$squadContent = $squadContent.Replace($squadOld, $squadNew)

Set-Content -Path $AbilitySystem -Value $abilityContent -Encoding UTF8
Set-Content -Path $SquadController -Value $squadContent -Encoding UTF8

Write-Host ""
Write-Host "Warboard roster ability warning fix installed."
Write-Host ""
Write-Host "Changed:"
Write-Host "  Assets\Scripts\Core\AbilitySystem.cs"
Write-Host "  Assets\Scripts\Core\SquadController.cs"
Write-Host ""
Write-Host "Backups:"
Write-Host "  AbilitySystem.cs.ability-warning-backup"
Write-Host "  SquadController.cs.ability-warning-backup"
Write-Host ""
Write-Host "Return to Unity and allow it to recompile."
