$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Registry = Join-Path $Root "Assets\Scripts\Core\ModelVisualRegistry.cs"
$Resolver = Join-Path $Root "Assets\Scripts\Core\CustodesModelPackResolver.cs"
$Index = Join-Path $Root "Assets\Resources\Armies\Models\Custodes\ModelIndex.json"

if (!(Test-Path $Registry)) { throw "ModelVisualRegistry.cs not found. Extract this ZIP into the Warboard project root, then run this script again." }
if (!(Test-Path $Resolver)) { throw "CustodesModelPackResolver.cs is missing." }
if (!(Test-Path $Index)) { throw "Custodes ModelIndex.json is missing." }

$content = Get-Content -Raw -Path $Registry
if ($content.Contains("CustodesModelPackResolver.TryResolve")) {
    Write-Host "Custodes model resolver is already installed."
    exit 0
}
$old = @"
        return
            TryResolvePack(
                unitName,
                roleName,
                modelIndex
            );
"@
$new = @"
        ModelVisualDefinition custodesVisual =
            CustodesModelPackResolver.TryResolve(
                unitName,
                roleName,
                modelIndex
            );

        if (custodesVisual != null)
            return custodesVisual;

        return
            TryResolvePack(
                unitName,
                roleName,
                modelIndex
            );
"@
if (!$content.Contains($old)) {
    throw "Could not find the expected Resolve() block in ModelVisualRegistry.cs. The project version may have changed; no file was modified."
}
Copy-Item $Registry "$Registry.custodes-backup" -Force
$content = $content.Replace($old,$new)
Set-Content -Path $Registry -Value $content -Encoding UTF8
Write-Host "Installed Custodes model-pack resolver."
Write-Host "Backup: $Registry.custodes-backup"
