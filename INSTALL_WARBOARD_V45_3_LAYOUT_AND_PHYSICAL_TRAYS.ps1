$ErrorActionPreference = "Stop"

try {
    Set-Location $PSScriptRoot

    Write-Host ""
    Write-Host "WARBOARD v45.3 - LAYOUT + PHYSICAL SIDE TRAYS" -ForegroundColor Cyan
    Write-Host "================================================"
    Write-Host ""

    $Root = $PSScriptRoot
    $CoreFolder = Join-Path $Root "Assets\Scripts\Core"
    $UiFile = Join-Path $CoreFolder "GameController.UI.cs"
    $CoreFile = Join-Path $CoreFolder "GameController.Core.cs"
    $TrayFile = Join-Path $CoreFolder "WarboardV45PhysicalSideTrays.cs"

    foreach ($file in @($UiFile, $CoreFile, $TrayFile)) {
        if (-not (Test-Path $file)) {
            throw "Missing expected file: $file"
        }
    }

    $BackupRoot = Join-Path $Root "Library\WarboardBackups\V45_3LayoutAndTrays"
    New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null

    function Write-Utf8([string]$Path, [string]$Text) {
        [System.IO.File]::WriteAllText(
            $Path,
            $Text,
            [System.Text.UTF8Encoding]::new($false)
        )
    }

    function Backup-Once([string]$Path) {
        $Name = Split-Path $Path -Leaf
        $Dest = Join-Path $BackupRoot $Name
        if (-not (Test-Path $Dest)) {
            Copy-Item -LiteralPath $Path -Destination $Dest -Force
        }
    }

    function Patch-MethodRect(
        [string]$FilePath,
        [string]$MarkerText,
        [string]$PatchMarker,
        [string]$Injection
    ) {
        $Text = [System.IO.File]::ReadAllText($FilePath)

        if ($Text.Contains($PatchMarker)) {
            Write-Host "[OK] $PatchMarker already present." -ForegroundColor DarkGreen
            return
        }

        $MarkerIndex =
            $Text.IndexOf(
                $MarkerText,
                [System.StringComparison]::Ordinal
            )

        if ($MarkerIndex -lt 0) {
            throw "Could not find marker text '$MarkerText' in $(Split-Path $FilePath -Leaf)."
        }

        $MethodStart =
            $Text.LastIndexOf(
                "    private ",
                $MarkerIndex,
                [System.StringComparison]::Ordinal
            )

        if ($MethodStart -lt 0) {
            throw "Could not locate the method containing '$MarkerText'."
        }

        $NextMethod =
            $Text.IndexOf(
                "`n    private ",
                $MarkerIndex,
                [System.StringComparison]::Ordinal
            )

        if ($NextMethod -lt 0) {
            $NextMethod = $Text.Length
        }

        $MethodText =
            $Text.Substring(
                $MethodStart,
                $NextMethod - $MethodStart
            )

        $RectMatch =
            [regex]::Match(
                $MethodText,
                'Rect\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*new Rect\((?s:.*?)\);'
            )

        if (-not $RectMatch.Success) {
            throw "Could not find the main Rect in the method containing '$MarkerText'."
        }

        $RectVar = $RectMatch.Groups[1].Value
        $Inserted =
            $RectMatch.Value + "`r`n" +
            "        // $PatchMarker`r`n" +
            ($Injection.Replace("{{RECT}}", $RectVar))

        $MethodNew =
            $MethodText.Remove(
                $RectMatch.Index,
                $RectMatch.Length
            ).Insert(
                $RectMatch.Index,
                $Inserted
            )

        $TextNew =
            $Text.Remove(
                $MethodStart,
                $MethodText.Length
            ).Insert(
                $MethodStart,
                $MethodNew
            )

        Backup-Once $FilePath
        Write-Utf8 $FilePath $TextNew

        Write-Host "[FIXED] $PatchMarker" -ForegroundColor Green
    }

    $HouseInjection = @"
        {{RECT}}.width =
            Mathf.Min(
                Screen.width - 44f,
                Mathf.Max(
                    {{RECT}}.width,
                    820f
                )
            );

        {{RECT}}.height =
            Mathf.Max(
                {{RECT}}.height,
                585f
            );

        {{RECT}}.x =
            Mathf.Max(
                18f,
                (Screen.width -
                    {{RECT}}.width) * 0.5f
            );
"@

    $DeployInjection = @"
        {{RECT}}.width =
            Mathf.Min(
                Screen.width - 40f,
                Mathf.Max(
                    {{RECT}}.width,
                    900f
                )
            );

        {{RECT}}.height =
            Mathf.Max(
                {{RECT}}.height,
                360f
            );

        {{RECT}}.x =
            Mathf.Max(
                20f,
                (Screen.width -
                    {{RECT}}.width) * 0.5f
            );
"@

    Patch-MethodRect `
        -FilePath $UiFile `
        -MarkerText "CUSTOM / HOUSE BATTLEFIELD" `
        -PatchMarker "WARBOARD_V45_3_HOUSE_PANEL_LAYOUT" `
        -Injection $HouseInjection

    Patch-MethodRect `
        -FilePath $UiFile `
        -MarkerText "DEPLOYMENT ORDER" `
        -PatchMarker "WARBOARD_V45_3_DEPLOYMENT_PANEL_LAYOUT" `
        -Injection $DeployInjection

    $CoreText =
        [System.IO.File]::ReadAllText($CoreFile)

    if (-not $CoreText.Contains("WarboardV45PhysicalSideTrays")) {
        $Anchor = @"
        battlefieldWorldUI.Initialize(
            this
        );
"@

        $Insert = @"
        battlefieldWorldUI.Initialize(
            this
        );

        GameObject trayUiObject =
            new GameObject(
                "Warboard v45 Physical Side Trays"
            );

        trayUiObject.AddComponent<
            WarboardV45PhysicalSideTrays
        >();
"@

        if (-not $CoreText.Contains($Anchor)) {
            throw "Could not find the BuildWorld battlefieldWorldUI.Initialize anchor in GameController.Core.cs."
        }

        $CoreText =
            $CoreText.Replace(
                $Anchor,
                $Insert
            )

        Backup-Once $CoreFile
        Write-Utf8 $CoreFile $CoreText

        Write-Host "[FIXED] Added physical side tray system to BuildWorld." -ForegroundColor Green
    }
    else {
        Write-Host "[OK] Physical side tray system already referenced." -ForegroundColor DarkGreen
    }

    Write-Host ""
    Write-Host "PATCH COMPLETE." -ForegroundColor Green
    Write-Host ""
    Write-Host "What changed:" -ForegroundColor Cyan
    Write-Host "  - House battlefield panel widened / given more height"
    Write-Host "  - Deployment panel widened / centered"
    Write-Host "  - Legacy text-only side panels are hidden"
    Write-Host "  - Physical reserve / destroyed trays are created beside the board"
    Write-Host "  - Off-board living squads are staged into reserve trays"
    Write-Host "  - Destroyed squads are staged into destroyed trays"
    Write-Host ""
    Write-Host "Return to Unity and let it recompile." -ForegroundColor Green
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
