$ErrorActionPreference = "Stop"

try {
    Set-Location $PSScriptRoot

    Write-Host ""
    Write-Host "WARBOARD v45.1 - CHARACTER LITERAL COMPILE FIX" -ForegroundColor Cyan
    Write-Host "==============================================="
    Write-Host ""

    $Root = $PSScriptRoot
    $Core = Join-Path $Root "Assets\Scripts\Core"

    $Targets = @(
        (Join-Path $Core "WeaponRuleParser.cs"),
        (Join-Path $Core "YellowScribeImporter.cs")
    )

    foreach ($Target in $Targets) {
        if (-not (Test-Path $Target)) {
            throw "Missing expected file: $Target"
        }
    }

    $BackupRoot =
        Join-Path $Root "Library\WarboardBackups\V45_1CharLiteralFix"

    New-Item -ItemType Directory -Force -Path $BackupRoot |
        Out-Null

    function Write-Utf8(
        [string]$Path,
        [string]$Text)
    {
        [System.IO.File]::WriteAllText(
            $Path,
            $Text,
            [System.Text.UTF8Encoding]::new($false)
        )
    }

    foreach ($Target in $Targets) {
        $Name = Split-Path $Target -Leaf
        $Text = [System.IO.File]::ReadAllText($Target)
        $Original = $Text

        # The broken v45.1 ASCII pass changed the original:
        #
        #   .Replace('\u2011', '-')
        #   .Replace('\u2013', '-')
        #   .Replace('\u2014', '-')
        #
        # into:
        #
        #   .Replace('\u2011', '-')
        #   .Replace(' - ', '-')
        #   .Replace(' - ', '-')
        #
        # Restore all three using ASCII-only C# Unicode escape syntax.
        $Pattern =
            "(?s)\.Replace\('[^']',\s*'-'\)" +
            "\s*\.Replace\('\s*-\s*',\s*'-'\)" +
            "\s*\.Replace\('\s*-\s*',\s*'-'\)"

        $Replacement =
            ".Replace('\u2011', '-')" +
            [Environment]::NewLine +
            "            .Replace('\u2013', '-')" +
            [Environment]::NewLine +
            "            .Replace('\u2014', '-')"

        $NewText =
            [regex]::Replace(
                $Text,
                $Pattern,
                $Replacement,
                1
            )

        # Fallback for the exact damaged form if the first source character
        # has also already been represented differently by the editor.
        if ($NewText -eq $Text) {
            $BrokenPair =
                ".Replace(' - ', '-')" +
                [Environment]::NewLine +
                "            .Replace(' - ', '-')"

            if ($Text.Contains($BrokenPair)) {
                $FixedPair =
                    ".Replace('\u2013', '-')" +
                    [Environment]::NewLine +
                    "            .Replace('\u2014', '-')"

                $NewText =
                    $Text.Replace(
                        $BrokenPair,
                        $FixedPair
                    )
            }
        }

        if ($NewText -eq $Original) {
            # Allow a rerun if it is already fixed.
            if ($Text.Contains(".Replace('\u2013', '-')") -and
                $Text.Contains(".Replace('\u2014', '-')"))
            {
                Write-Host "[OK] $Name was already fixed." -ForegroundColor DarkGreen
                continue
            }

            throw "Could not locate the damaged character literals in $Name."
        }

        $Backup =
            Join-Path $BackupRoot $Name

        if (-not (Test-Path $Backup)) {
            Copy-Item -LiteralPath $Target -Destination $Backup -Force
        }

        Write-Utf8 $Target $NewText

        Write-Host "[FIXED] $Name" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "COMPILE FIX COMPLETE." -ForegroundColor Green
    Write-Host ""
    Write-Host "Return to Unity and let it recompile."
    Write-Host "The four CS1012 errors should be gone."
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "FIX FAILED" -ForegroundColor Red
    Write-Host "----------" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkRed
    Write-Host ""
    exit 1
}
