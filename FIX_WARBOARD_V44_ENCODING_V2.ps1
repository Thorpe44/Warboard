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

function Read-Utf8 {
    param([string]$Path)

    $utf8 =
        New-Object System.Text.UTF8Encoding(
            $false,
            $false
        )

    return [System.IO.File]::ReadAllText(
        $Path,
        $utf8
    )
}

function Write-Utf8 {
    param(
        [string]$Path,
        [string]$Content
    )

    $utf8 =
        New-Object System.Text.UTF8Encoding(
            $false
        )

    [System.IO.File]::WriteAllText(
        $Path,
        $Content,
        $utf8
    )
}

function Has-MojibakeMarker {
    param([string]$Text)

    if ($null -eq $Text) {
        return $false
    }

    # These are the first Unicode characters normally seen when UTF-8 bytes
    # have been decoded as Windows-1252: U+00E2, U+00C2 and U+00C3.
    foreach ($ch in $Text.ToCharArray()) {
        $code = [int][char]$ch

        if ($code -eq 0x00E2 -or
            $code -eq 0x00C2 -or
            $code -eq 0x00C3) {
            return $true
        }
    }

    return $false
}

function Repair-Mojibake {
    param([string]$Text)

    $cp1252 =
        [System.Text.Encoding]::GetEncoding(
            1252,
            [System.Text.EncoderExceptionFallback]::new(),
            [System.Text.DecoderExceptionFallback]::new()
        )

    $utf8 =
        New-Object System.Text.UTF8Encoding(
            $false,
            $true
        )

    $bytes =
        $cp1252.GetBytes($Text)

    return $utf8.GetString($bytes)
}

$Root = Find-WarboardRoot $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($Root)) {
    Write-Host "FAILED: Could not find the Warboard project."
    exit 1
}

$Core =
    Join-Path $Root "Assets\Scripts\Core"

$files = @(
    "GameController.UI.cs",
    "GameController.Core.cs",
    "ObjectiveController.cs",
    "ModelToken.cs",
    "BattlefieldWorldUI.cs",
    "WarboardBuildInfo.cs"
)

$stamp =
    Get-Date -Format "yyyyMMdd_HHmmss"

$fixedFiles = 0

Write-Host "Warboard root:"
Write-Host "  $Root"
Write-Host ""

foreach ($name in $files) {
    $path =
        Join-Path $Core $name

    if (!(Test-Path $path)) {
        Write-Host ("[SKIP] " + $name)
        continue
    }

    $content =
        Read-Utf8 $path

    if (!(Has-MojibakeMarker $content)) {
        Write-Host ("[OK]   " + $name)
        continue
    }

    try {
        $repaired =
            Repair-Mojibake $content
    }
    catch {
        Write-Host ("[FAIL] Could not safely repair " + $name)
        Write-Host $_.Exception.Message
        exit 2
    }

    if ($repaired -eq $content) {
        Write-Host ("[FAIL] Repair made no change to " + $name)
        exit 3
    }

    Copy-Item $path "$path.before_encoding_v2_$stamp.bak" -Force
    Write-Utf8 $path $repaired

    $verify =
        Read-Utf8 $path

    if (Has-MojibakeMarker $verify) {
        Write-Host ("[FAIL] Suspicious encoding marker remains in " + $name)
        exit 4
    }

    if ($verify.IndexOf([char]0xFFFD) -ge 0) {
        Write-Host ("[FAIL] Unicode replacement character found in " + $name)
        exit 5
    }

    Write-Host ("[FIXED] " + $name)
    $fixedFiles++
}

# A few visible strings are useful as a sanity check. Construct them entirely
# from character codes so this script remains ASCII-only.
$bullet =
    [string][char]0x2022

$emdash =
    [string][char]0x2014

$arrow =
    [string][char]0x2192

$ui =
    Read-Utf8 (
        Join-Path $Core "GameController.UI.cs"
    )

$foundReadableSymbol =
    $ui.Contains($bullet) -or
    $ui.Contains($emdash) -or
    $ui.Contains($arrow)

if (!$foundReadableSymbol) {
    Write-Host ""
    Write-Host "[WARN] No expected punctuation symbol was found in GameController.UI.cs."
    Write-Host "       The files may already have been altered beyond the original v44 corruption."
}

$marker =
    Join-Path $Root "WARBOARD_V44_ENCODING_HOTFIX_V2_INSTALLED.txt"

$markerText =
    "Warboard v44 encoding hotfix V2 installed." +
    [Environment]::NewLine +
    "Fixed files: " +
    $fixedFiles +
    [Environment]::NewLine +
    "All output was written explicitly as UTF-8."

Write-Utf8 $marker $markerText

Write-Host ""
Write-Host "[PASS] Encoding repair completed."
Write-Host "[PASS] No mojibake markers remain in repaired files."
Write-Host "[PASS] No Unicode replacement characters were introduced."
Write-Host ""
Write-Host "SUCCESS - V44 ENCODING HOTFIX V2 VERIFIED"
Write-Host ""
Write-Host "Return to Unity and let it recompile."
