$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Target = Join-Path $ProjectRoot "Assets\Editor\WarboardV44NecronsFactionRules.cs"

Write-Host ""
Write-Host "WARBOARD v44.2 - Necron migration hotfix" -ForegroundColor Cyan
Write-Host "Project: $ProjectRoot"
Write-Host ""

if (-not (Test-Path $Target)) {
    Write-Host "ERROR: Could not find the v44 Necron migration installer:" -ForegroundColor Red
    Write-Host "  $Target"
    Write-Host ""
    Write-Host "Extract this ZIP directly into the Warboard project root."
    Read-Host "Press Enter to close"
    exit 1
}

$Text = [System.IO.File]::ReadAllText($Target)
$Backup = "$Target.before_v44_2_migration_hotfix.bak"

if (-not (Test-Path $Backup)) {
    Copy-Item $Target $Backup
    Write-Host "[OK] Backup created." -ForegroundColor Green
}

$Changed = $false

# The v44 installer contains several method.Insert(index, text) calls.
# A missing/moved formatting anchor produces index == -1 and crashes with
# ArgumentOutOfRangeException(startIndex). Route every insertion through a
# guarded helper instead. Valid insertions behave identically.
if ($Text.Contains("method.Insert(")) {
    $CountBefore = ([regex]::Matches($Text, [regex]::Escape("method.Insert("))).Count
    $Text = $Text.Replace("method.Insert(", "SafeInsert(method,")
    $Changed = $true
    Write-Host "[FIXED] Hardened $CountBefore migration insertion call(s)." -ForegroundColor Green
}
else {
    Write-Host "[OK] Migration insertion calls are already hardened." -ForegroundColor DarkGreen
}

$HelperMarker = "private static string SafeInsert("
if (-not $Text.Contains($HelperMarker)) {
    $Anchor = @'
    private static string InsertAtMethodStart(
'@

    $Helper = @'
    private static string SafeInsert(
        string source,
        int startIndex,
        string value)
    {
        if (source == null)
            return source;

        if (startIndex < 0 ||
            startIndex > source.Length)
        {
            string preview =
                string.IsNullOrEmpty(value)
                ? "(empty insertion)"
                : value.Replace("\r", " ")
                       .Replace("\n", " ");

            if (preview.Length > 120)
                preview = preview.Substring(0, 120);

            Debug.LogWarning(
                "[Warboard v44] Migration anchor was not found; " +
                "skipping one optional source insertion instead of crashing. " +
                "Insertion: " + preview
            );

            return source;
        }

        return source.Insert(
            startIndex,
            value
        );
    }

'@

    $At = $Text.IndexOf($Anchor, [System.StringComparison]::Ordinal)
    if ($At -lt 0) {
        Write-Host "ERROR: Could not find the helper insertion point in the v44 installer." -ForegroundColor Red
        Write-Host "No modified installer was written."
        Read-Host "Press Enter to close"
        exit 1
    }

    $Text = $Text.Insert($At, $Helper)
    $Changed = $true
    Write-Host "[FIXED] Added guarded SafeInsert migration helper." -ForegroundColor Green
}
else {
    Write-Host "[OK] SafeInsert helper already present." -ForegroundColor DarkGreen
}

# Improve the top-level failure log so any genuinely different migration error
# is much easier to identify from a screenshot.
$OldCatch = @'
            Debug.LogError(
                "[Warboard v44] Necrons faction-rule migration failed. " +
                ex
            );
'@
$NewCatch = @'
            Debug.LogError(
                "[Warboard v44] Necrons faction-rule migration failed. " +
                ex.ToString()
            );
'@

if ($Text.Contains($OldCatch)) {
    $Text = $Text.Replace($OldCatch, $NewCatch)
    $Changed = $true
}

if ($Changed) {
    [System.IO.File]::WriteAllText(
        $Target,
        $Text,
        [System.Text.UTF8Encoding]::new($false)
    )

    Write-Host ""
    Write-Host "v44.2 migration hotfix installed successfully." -ForegroundColor Green
    Write-Host ""
    Write-Host "Return to Unity. It should compile and automatically re-run the v44 Necron migration."
    Write-Host "If Unity shows a yellow '[Warboard v44] Migration anchor was not found' warning,"
    Write-Host "send a screenshot of that warning after the migration finishes."
}
else {
    Write-Host ""
    Write-Host "No changes were required." -ForegroundColor Cyan
}

Write-Host ""
Read-Host "Press Enter to close"
