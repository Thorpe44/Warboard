$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Target = Join-Path $ProjectRoot "Assets\Scripts\Core\GameController.NecronsFaction11.cs"

Write-Host ""
Write-Host "WARBOARD v44.1 - Necron compile fix" -ForegroundColor Cyan
Write-Host "Project: $ProjectRoot"
Write-Host ""

if (-not (Test-Path $Target)) {
    Write-Host "ERROR: Could not find:" -ForegroundColor Red
    Write-Host "  $Target"
    Write-Host ""
    Write-Host "Extract this ZIP directly into the Warboard project root, then run the BAT again."
    Read-Host "Press Enter to close"
    exit 1
}

$Text = [System.IO.File]::ReadAllText($Target)

$Fixes = @(
    @{
        Old = "DiceRoller.RollD3(label)"
        New = "DiceRoller.RollExpressionDie(3, label)"
        Name = "RollD3 -> RollExpressionDie(3)"
    },
    @{
        Old = "phase != Phase.Movement"
        New = "phase != Phase.Move"
        Name = "Phase.Movement -> Phase.Move"
    },
    @{
        Old = "phase != Phase.Shooting"
        New = "phase != Phase.Shoot"
        Name = "Phase.Shooting -> Phase.Shoot"
    }
)

$Changed = $false

foreach ($Fix in $Fixes) {
    if ($Text.Contains($Fix.Old)) {
        $Text = $Text.Replace($Fix.Old, $Fix.New)
        Write-Host "[FIXED] $($Fix.Name)" -ForegroundColor Green
        $Changed = $true
    }
    elseif ($Text.Contains($Fix.New)) {
        Write-Host "[OK]    $($Fix.Name) was already fixed." -ForegroundColor DarkGreen
    }
    else {
        Write-Host "[WARN]  Could not find expected text for: $($Fix.Name)" -ForegroundColor Yellow
    }
}

if ($Changed) {
    $Backup = "$Target.before_v44_1_compile_fix.bak"
    if (-not (Test-Path $Backup)) {
        Copy-Item $Target $Backup
        Write-Host ""
        Write-Host "Backup created:"
        Write-Host "  $Backup"
    }

    # UTF-8 without BOM, safe for Unity/C# source.
    [System.IO.File]::WriteAllText(
        $Target,
        $Text,
        [System.Text.UTF8Encoding]::new($false)
    )

    Write-Host ""
    Write-Host "v44.1 compile fix installed successfully." -ForegroundColor Green
    Write-Host "Return to Unity and let it recompile."
}
else {
    Write-Host ""
    Write-Host "No file changes were required." -ForegroundColor Cyan
    Write-Host "If Unity still shows errors, send a screenshot of the Console."
}

Write-Host ""
Read-Host "Press Enter to close"
