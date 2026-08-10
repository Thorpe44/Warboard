$ErrorActionPreference = "Stop"

try {
    Set-Location $PSScriptRoot

    Write-Host ""
    Write-Host "WARBOARD v45.1 - UI TEXT ENCODING HOTFIX" -ForegroundColor Cyan
    Write-Host "========================================"
    Write-Host ""

    $Root = $PSScriptRoot
    $Core = Join-Path $Root "Assets\Scripts\Core"

    if (-not (Test-Path $Core)) {
        throw "Could not find Assets\Scripts\Core. Extract this ZIP directly into the Warboard project root."
    }

    $BackupRoot = Join-Path $Root "Library\WarboardBackups\V45_1AsciiHotfix"
    New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null

    function Write-Utf8([string]$Path, [string]$Text) {
        [System.IO.File]::WriteAllText(
            $Path,
            $Text,
            [System.Text.UTF8Encoding]::new($false)
        )
    }

    function Replace-Token(
        [string]$Text,
        [string]$Old,
        [string]$New
    ) {
        if ([string]::IsNullOrEmpty($Old)) {
            return $Text
        }

        return $Text.Replace($Old, $New)
    }

    $files = @(Get-ChildItem -Path $Core -Filter *.cs -Recurse)

    if ($files.Count -eq 0) {
        throw "No C# files found under Assets\Scripts\Core."
    }

    # Unicode tokens are built by character code so this installer remains
    # plain ASCII and cannot suffer from its own encoding problem.
    $bullet = [string][char]0x2022
    $arrow = [string][char]0x2192
    $emdash = [string][char]0x2014
    $endash = [string][char]0x2013
    $multiply = [string][char]0x00D7
    $triangle = [string][char]0x25BA
    $ellipsis = [string][char]0x2026
    $nbsp = [string][char]0x00A0

    # Common UTF-8-to-Windows-1252 mojibake sequences.
    $mojiBullet =
        ([string][char]0x00E2) +
        ([string][char]0x20AC) +
        ([string][char]0x00A2)

    $mojiArrow =
        ([string][char]0x00E2) +
        ([string][char]0x2020) +
        ([string][char]0x2019)

    $mojiEmdash =
        ([string][char]0x00E2) +
        ([string][char]0x20AC) +
        ([string][char]0x201D)

    $mojiEndash =
        ([string][char]0x00E2) +
        ([string][char]0x20AC) +
        ([string][char]0x201C)

    $mojiEllipsis =
        ([string][char]0x00E2) +
        ([string][char]0x20AC) +
        ([string][char]0x00A6)

    $changedFiles = 0

    foreach ($file in $files) {
        $path = $file.FullName
        $name = $file.Name

        $text = [System.IO.File]::ReadAllText($path)
        $original = $text

        $text = Replace-Token $text $bullet " | "
        $text = Replace-Token $text $arrow " -> "
        $text = Replace-Token $text $emdash " - "
        $text = Replace-Token $text $endash " - "
        $text = Replace-Token $text $multiply " x "
        $text = Replace-Token $text $triangle "> "
        $text = Replace-Token $text $ellipsis "..."
        $text = Replace-Token $text $nbsp " "

        $text = Replace-Token $text $mojiBullet " | "
        $text = Replace-Token $text $mojiArrow " -> "
        $text = Replace-Token $text $mojiEmdash " - "
        $text = Replace-Token $text $mojiEndash " - "
        $text = Replace-Token $text $mojiEllipsis "..."

        if ($name -eq "WarboardBuildInfo.cs") {
            $text = $text.Replace(
                'CurrentVersion = "v45"',
                'CurrentVersion = "v45.1"'
            )
        }

        if ($text -ne $original) {
            $relative =
                $path.Substring($Core.Length).TrimStart("\")
            $safeName =
                $relative.Replace("\", "__")

            $backup =
                Join-Path $BackupRoot $safeName

            if (-not (Test-Path $backup)) {
                Copy-Item -LiteralPath $path -Destination $backup -Force
            }

            Write-Utf8 $path $text
            $changedFiles++

            Write-Host "[OK] $relative" -ForegroundColor Green
        }
    }

    Write-Host ""
    Write-Host "HOTFIX COMPLETE." -ForegroundColor Green
    Write-Host "Changed C# files: $changedFiles"
    Write-Host ""
    Write-Host "Return to Unity and let it recompile."
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "HOTFIX FAILED" -ForegroundColor Red
    Write-Host "-------------" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkRed
    Write-Host ""
    exit 1
}
