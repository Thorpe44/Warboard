$ErrorActionPreference = "Stop"

try {
    Set-Location $PSScriptRoot

    Write-Host ""
    Write-Host "WARBOARD v45.7a - WOOD TABLE INJECTION FIX" -ForegroundColor Cyan
    Write-Host "==========================================="
    Write-Host ""

    $Root = $PSScriptRoot
    $CoreFolder =
        Join-Path $Root "Assets\Scripts\Core"

    $CoreFile =
        Join-Path $CoreFolder "GameController.Core.cs"

    $BuildInfo =
        Join-Path $CoreFolder "WarboardBuildInfo.cs"

    $TableFile =
        Join-Path $CoreFolder "WarboardV45EnvironmentTable.cs"

    foreach ($file in @(
        $CoreFile,
        $BuildInfo,
        $TableFile
    )) {
        if (-not (Test-Path $file)) {
            throw "Missing expected file: $file"
        }
    }

    $BackupRoot =
        Join-Path $Root "Library\WarboardBackups\V45_7aWoodTableFix"

    New-Item -ItemType Directory -Force -Path $BackupRoot |
        Out-Null

    function Backup-Once(
        [string]$Path)
    {
        $Name =
            Split-Path $Path -Leaf

        $Dest =
            Join-Path $BackupRoot $Name

        if (-not (Test-Path $Dest)) {
            Copy-Item `
                -LiteralPath $Path `
                -Destination $Dest `
                -Force
        }
    }

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

    function Find-MethodRange(
        [string]$Text,
        [string]$Signature)
    {
        $signatureIndex =
            $Text.IndexOf(
                $Signature,
                [System.StringComparison]::Ordinal
            )

        if ($signatureIndex -lt 0) {
            return $null
        }

        $lineStart =
            $Text.LastIndexOf(
                "`n",
                $signatureIndex
            )

        if ($lineStart -lt 0) {
            $lineStart = 0
        }
        else {
            $lineStart++
        }

        $openBrace =
            $Text.IndexOf(
                "{",
                $signatureIndex,
                [System.StringComparison]::Ordinal
            )

        if ($openBrace -lt 0) {
            return $null
        }

        $depth = 0
        $inString = $false
        $inChar = $false
        $inLineComment = $false
        $inBlockComment = $false
        $escaped = $false
        $closeBrace = -1

        for ($i = $openBrace;
             $i -lt $Text.Length;
             $i++)
        {
            $c = $Text[$i]

            $next =
                if ($i + 1 -lt $Text.Length) {
                    $Text[$i + 1]
                }
                else {
                    [char]0
                }

            if ($inLineComment) {
                if ($c -eq "`n") {
                    $inLineComment = $false
                }

                continue
            }

            if ($inBlockComment) {
                if ($c -eq "*" -and
                    $next -eq "/")
                {
                    $inBlockComment = $false
                    $i++
                }

                continue
            }

            if ($inString) {
                if ($escaped) {
                    $escaped = $false
                    continue
                }

                if ($c -eq "\") {
                    $escaped = $true
                    continue
                }

                if ($c -eq '"') {
                    $inString = $false
                }

                continue
            }

            if ($inChar) {
                if ($escaped) {
                    $escaped = $false
                    continue
                }

                if ($c -eq "\") {
                    $escaped = $true
                    continue
                }

                if ($c -eq "'") {
                    $inChar = $false
                }

                continue
            }

            if ($c -eq "/" -and
                $next -eq "/")
            {
                $inLineComment = $true
                $i++
                continue
            }

            if ($c -eq "/" -and
                $next -eq "*")
            {
                $inBlockComment = $true
                $i++
                continue
            }

            if ($c -eq '"') {
                $inString = $true
                continue
            }

            if ($c -eq "'") {
                $inChar = $true
                continue
            }

            if ($c -eq "{") {
                $depth++
                continue
            }

            if ($c -eq "}") {
                $depth--

                if ($depth -eq 0) {
                    $closeBrace = $i
                    break
                }
            }
        }

        if ($closeBrace -lt 0) {
            return $null
        }

        return [PSCustomObject]@{
            Start = $lineStart
            OpenBrace = $openBrace
            CloseBrace = $closeBrace
        }
    }

    $CoreText =
        [System.IO.File]::ReadAllText(
            $CoreFile
        )

    if ($CoreText.Contains(
            "WarboardV45EnvironmentTable"))
    {
        Write-Host "[OK] Wood table runtime was already injected." -ForegroundColor DarkGreen
    }
    else {
        $BuildWorld =
            Find-MethodRange `
                -Text $CoreText `
                -Signature "private void BuildWorld()"

        if ($null -eq $BuildWorld) {
            throw "Could not locate BuildWorld() in GameController.Core.cs."
        }

        $Injection = @'

        GameObject tableEnvironmentObject =
            new GameObject(
                "Warboard v45 Environment Table"
            );

        tableEnvironmentObject.AddComponent<
            WarboardV45EnvironmentTable
        >();

'@

        $CoreText =
            $CoreText.Insert(
                $BuildWorld.CloseBrace,
                $Injection
            )

        Backup-Once $CoreFile
        Write-Utf8 $CoreFile $CoreText

        Write-Host "[FIXED] Wood table runtime injected at the end of BuildWorld()." -ForegroundColor Green
    }

    $BuildText =
        [System.IO.File]::ReadAllText(
            $BuildInfo
        )

    $BuildText =
        [regex]::Replace(
            $BuildText,
            'CurrentVersion\s*=\s*"v[^"]+"',
            'CurrentVersion = "v45.7"'
        )

    Write-Utf8 $BuildInfo $BuildText

    Write-Host ""
    Write-Host "v45.7a FIX COMPLETE." -ForegroundColor Green
    Write-Host ""
    Write-Host "The earlier v45.7 HUD, score and dice changes were already applied." -ForegroundColor Cyan
    Write-Host "This patch only completes the missing wood-table step and version marker."
    Write-Host ""
    Write-Host "Return to Unity and let it compile/import." -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "PATCH FAILED" -ForegroundColor Red
    Write-Host "------------" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkRed
    Write-Host ""
    exit 1
}
