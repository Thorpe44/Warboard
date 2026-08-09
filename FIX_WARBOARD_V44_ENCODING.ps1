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
    "WarboardBuildInfo.cs",
    "WarboardVisualTheme.cs"
)

# Known UTF-8 -> Windows-1252 mojibake sequences introduced when the
# v44 installer read existing source with Windows PowerShell's default
# legacy encoding and then wrote it as UTF-8.
$replacements =
    [ordered]@{
        "â€¢" = "•"
        "â€”" = "—"
        "â€“" = "–"
        "â†’" = "→"
        "â‰¥" = "≥"
        "â‰¤" = "≤"
        "â€¦" = "…"
        "Ã—" = "×"
        "Â°" = "°"
        "Â£" = "£"
        "Â" = ""
    }

$stamp =
    Get-Date -Format "yyyyMMdd_HHmmss"

$totalChanges = 0

Write-Host "Warboard root:"
Write-Host "  $Root"
Write-Host ""

foreach ($name in $files) {
    $path =
        Join-Path $Core $name

    if (!(Test-Path $path)) {
        continue
    }

    $content =
        Read-Utf8 $path

    $before = $content
    $fileChanges = 0

    foreach ($pair in
        $replacements.GetEnumerator()) {

        $countBefore =
            ([regex]::Matches(
                $content,
                [regex]::Escape(
                    $pair.Key
                )
            )).Count

        if ($countBefore -gt 0) {
            $content =
                $content.Replace(
                    $pair.Key,
                    $pair.Value
                )

            $fileChanges +=
                $countBefore
        }
    }

    if ($content -ne $before) {
        Copy-Item $path "$path.before_encoding_hotfix_$stamp.bak" -Force
        Write-Utf8 $path $content

        Write-Host (
            "[FIXED] " +
            $name +
            " (" +
            $fileChanges +
            " replacement(s))"
        )

        $totalChanges +=
            $fileChanges
    }
    else {
        Write-Host (
            "[OK]    " +
            $name
        )
    }
}

# Verification: no known bad mojibake should remain in the touched files.
$bad = @()

foreach ($name in $files) {
    $path =
        Join-Path $Core $name

    if (!(Test-Path $path)) {
        continue
    }

    $content =
        Read-Utf8 $path

    foreach ($key in
        $replacements.Keys) {

        if ($content.Contains($key)) {
            $bad +=
                "$name -> $key"
        }
    }
}

Write-Host ""

if ($bad.Count -gt 0) {
    Write-Host "[FAIL] Mojibake still remains:"
    foreach ($entry in $bad) {
        Write-Host "  $entry"
    }

    exit 2
}

$marker =
    Join-Path $Root "WARBOARD_V44_ENCODING_HOTFIX_INSTALLED.txt"

$markerText =
    "Warboard v44 encoding hotfix installed.`r`n" +
    "Date: " +
    (Get-Date) +
    "`r`n" +
    "Replacements made: " +
    $totalChanges +
    "`r`n" +
    "All touched files verified as UTF-8."

Write-Utf8 $marker $markerText

Write-Host "[PASS] No known v44 encoding artefacts remain."
Write-Host "[PASS] Files rewritten explicitly as UTF-8."
Write-Host ""
Write-Host "SUCCESS - v44 ENCODING HOTFIX VERIFIED"
Write-Host ""
Write-Host "Return to Unity and let it recompile."
