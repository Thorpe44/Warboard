$ErrorActionPreference = "Stop"

try {
    Set-Location $PSScriptRoot

    Write-Host ""
    Write-Host "WARBOARD v45.5 - TOP BAR / SCORE / DICE POLISH" -ForegroundColor Cyan
    Write-Host "==============================================="
    Write-Host ""

    $Root = $PSScriptRoot
    $Core = Join-Path $Root "Assets\Scripts\Core"

    $UiFile = Join-Path $Core "GameController.UI.cs"
    $CoreFile = Join-Path $Core "GameController.Core.cs"
    $BuildInfo = Join-Path $Core "WarboardBuildInfo.cs"
    $OverlayFile = Join-Path $Core "WarboardV45HudOverlay.cs"

    foreach ($file in @(
        $UiFile,
        $CoreFile,
        $BuildInfo,
        $OverlayFile
    )) {
        if (-not (Test-Path $file)) {
            throw "Missing expected file: $file"
        }
    }

    $BackupRoot =
        Join-Path $Root "Library\WarboardBackups\V45_5TopbarScoreDice"

    New-Item -ItemType Directory -Force -Path $BackupRoot |
        Out-Null

    function Backup-Once([string]$Path) {
        $Name = Split-Path $Path -Leaf
        $Dest = Join-Path $BackupRoot $Name
        if (-not (Test-Path $Dest)) {
            Copy-Item -LiteralPath $Path -Destination $Dest -Force
        }
    }

    function Write-Utf8([string]$Path, [string]$Text) {
        [System.IO.File]::WriteAllText(
            $Path,
            $Text,
            [System.Text.UTF8Encoding]::new($false)
        )
    }

    # ---------------------------------------------------------
    # 1. Replace DrawTopCommandBar with a tighter single-row
    #    version and remove version from top-left.
    # ---------------------------------------------------------
    $UiText =
        [System.IO.File]::ReadAllText(
            $UiFile
        )

    $TopMethod = @'
    private void DrawTopCommandBar()
    {
        // WARBOARD_V45_5_TOP_COMMAND_BAR
        Rect bar =
            new Rect(
                8f,
                8f,
                Screen.width - 16f,
                44f
            );

        Color accent =
            FactionColor(
                activeFaction
            );

        WarboardV45Presentation.DrawPanel(
            bar,
            accent,
            true
        );

        GUI.Label(
            new Rect(
                bar.x + 10f,
                bar.y + 6f,
                150f,
                18f
            ),
            (IsXcomMode
                ? "XCOM"
                : "TRADITIONAL"),
            WarboardV45Presentation
                .SubHeaderStyle
        );

        GUI.Label(
            new Rect(
                bar.x + 10f,
                bar.y + 22f,
                160f,
                18f
            ),
            battleSizeName.ToUpper(),
            WarboardV45Presentation
                .SubHeaderStyle
        );

        string phaseText =
            deploymentMode
            ? "DEPLOYMENT"
            : phase.ToString().ToUpper();

        string roundText =
            deploymentMode
            ? "PRE-GAME"
            : "ROUND " + round;

        float phaseWidth =
            Mathf.Clamp(
                Screen.width * 0.16f,
                250f,
                280f
            );

        Rect phaseRect =
            new Rect(
                (Screen.width -
                    phaseWidth) *
                    0.5f,
                bar.y + 6f,
                phaseWidth,
                32f
            );

        GUI.Label(
            phaseRect,
            roundText +
            "  |  " +
            ActiveFactionDisplayName() +
            "  |  " +
            phaseText,
            WarboardV45Presentation
                .PhasePillStyle
        );

        float right =
            bar.x + bar.width - 10f;

        if (!deploymentMode)
        {
            right -= 126f;

            if (GUI.Button(
                new Rect(
                    right,
                    bar.y + 5f,
                    118f,
                    34f
                ),
                "NEXT PHASE >",
                WarboardV45Presentation
                    .PrimaryButtonStyle))
            {
                NextPhase();
            }
        }

        right -= 78f;

        if (GUI.Button(
            new Rect(
                right,
                bar.y + 5f,
                70f,
                34f
            ),
            "3D DICE",
            WarboardV45Presentation
                .ToolbarButtonStyle))
        {
            showDiceTray = true;
        }

        right -= 112f;

        GUI.enabled =
            !deploymentMode;

        if (GUI.Button(
            new Rect(
                right,
                bar.y + 5f,
                104f,
                34f
            ),
            "STRATAGEMS",
            WarboardV45Presentation
                .ToolbarButtonStyle))
        {
            showStratagemMenu =
                !showStratagemMenu;

            showWarboardPanel = false;
            showBasicCommandsPanel = false;
            showDatasheet = false;
            showMissionPanel = false;
            showBattleLog = false;
        }

        GUI.enabled = true;

        right -= 96f;

        if (GUI.Button(
            new Rect(
                right,
                bar.y + 5f,
                88f,
                34f
            ),
            "COMMANDS",
            WarboardV45Presentation
                .ToolbarButtonStyle))
        {
            showBasicCommandsPanel =
                !showBasicCommandsPanel;

            if (showBasicCommandsPanel)
            {
                showWarboardPanel = false;
                showStratagemMenu = false;
                showDatasheet = false;
                showMissionPanel = false;
                showBattleLog = false;
            }
        }

        right -= 66f;

        if (GUI.Button(
            new Rect(
                right,
                bar.y + 5f,
                58f,
                34f
            ),
            "LOG",
            WarboardV45Presentation
                .ToolbarButtonStyle))
        {
            showBattleLog =
                !showBattleLog;

            if (showBattleLog)
            {
                showWarboardPanel = false;
                showBasicCommandsPanel = false;
                showStratagemMenu = false;
                showDatasheet = false;
                showMissionPanel = false;
            }
        }

        right -= 84f;

        if (GUI.Button(
            new Rect(
                right,
                bar.y + 5f,
                76f,
                34f
            ),
            "MISSION",
            WarboardV45Presentation
                .ToolbarButtonStyle))
        {
            showMissionPanel =
                !showMissionPanel;

            if (showMissionPanel)
            {
                showWarboardPanel = false;
                showBasicCommandsPanel = false;
                showStratagemMenu = false;
                showDatasheet = false;
                showBattleLog = false;
            }
        }

        right -= 94f;

        if (GUI.Button(
            new Rect(
                right,
                bar.y + 5f,
                86f,
                34f
            ),
            "WARBOARD",
            WarboardV45Presentation
                .ToolbarButtonStyle))
        {
            showWarboardPanel =
                !showWarboardPanel;

            if (showWarboardPanel)
            {
                showBasicCommandsPanel = false;
                showStratagemMenu = false;
                showDatasheet = false;
                showMissionPanel = false;
                showBattleLog = false;
            }
        }

        GUI.enabled = true;
    }

'@

    $Pattern =
        '(?s)    private void DrawTopCommandBar\(\)\s*\{.*?^    \}\r?\n\r?\n    private void DrawWarboardPanel\(\)'

    $Replacement =
        $TopMethod +
        [Environment]::NewLine +
        [Environment]::NewLine +
        '    private void DrawWarboardPanel()'

    $NewUi =
        [regex]::Replace(
            $UiText,
            $Pattern,
            $Replacement,
            [System.Text.RegularExpressions.RegexOptions]::Multiline
        )

    if ($NewUi -eq $UiText) {
        throw "Could not replace DrawTopCommandBar()."
    }

    $UiText = $NewUi

    # ---------------------------------------------------------
    # 2. Suppress the old separate context action row.
    # ---------------------------------------------------------
    $UiText =
        $UiText.Replace(
            '        DrawContextActionBar();',
            '        // WARBOARD_V45_5_MERGED_CONTEXT_BAR'
        )

    Backup-Once $UiFile
    Write-Utf8 $UiFile $UiText

    Write-Host "[FIXED] Rebuilt top command bar and removed separate wound-edit row." -ForegroundColor Green

    # ---------------------------------------------------------
    # 3. Install HUD overlay runtime after other world runtime
    #    helpers.
    # ---------------------------------------------------------
    $CoreText =
        [System.IO.File]::ReadAllText(
            $CoreFile
        )

    if (-not $CoreText.Contains(
            "WarboardV45HudOverlay"))
    {
        $Pattern =
            'battlefieldWorldUI\.Initialize\s*\(\s*this\s*\)\s*;'

        $Match =
            [regex]::Match(
                $CoreText,
                $Pattern
            )

        if (-not $Match.Success)
        {
            throw "Could not locate battlefieldWorldUI.Initialize(this) in GameController.Core.cs."
        }

        $Injection =
            $Match.Value +
            [Environment]::NewLine +
            [Environment]::NewLine +
            '        GameObject hudOverlayObject =' +
            [Environment]::NewLine +
            '            new GameObject(' +
            [Environment]::NewLine +
            '                "Warboard v45 HUD Overlay"' +
            [Environment]::NewLine +
            '            );' +
            [Environment]::NewLine +
            [Environment]::NewLine +
            '        hudOverlayObject.AddComponent<' +
            [Environment]::NewLine +
            '            WarboardV45HudOverlay' +
            [Environment]::NewLine +
            '        >();'

        $CoreText =
            $CoreText.Remove(
                $Match.Index,
                $Match.Length
            ).Insert(
                $Match.Index,
                $Injection
            )

        Backup-Once $CoreFile
        Write-Utf8 $CoreFile $CoreText

        Write-Host "[FIXED] Installed HUD overlay runtime." -ForegroundColor Green
    }
    else {
        Write-Host "[OK] HUD overlay runtime already installed." -ForegroundColor DarkGreen
    }

    # ---------------------------------------------------------
    # 4. Version marker.
    # ---------------------------------------------------------
    $BuildText =
        [System.IO.File]::ReadAllText(
            $BuildInfo
        )

    $BuildText =
        [regex]::Replace(
            $BuildText,
            'CurrentVersion\s*=\s*"v[^"]+"',
            'CurrentVersion = "v45.5"'
        )

    Write-Utf8 $BuildInfo $BuildText

    Write-Host "[FIXED] Moved visible build version to watermark usage (v45.5)." -ForegroundColor Green

    Write-Host ""
    Write-Host "WARBOARD v45.5 INSTALLED." -ForegroundColor Green
    Write-Host ""
    Write-Host "Changes:" -ForegroundColor Cyan
    Write-Host "  - wound/restore/reserves moved off their own row"
    Write-Host "  - top bar kept centered around the round/faction/phase pill"
    Write-Host "  - next phase remains on far right"
    Write-Host "  - current score strip added under the center pill"
    Write-Host "  - build version moved to a bottom-left watermark"
    Write-Host "  - best-effort permanent 3D dice tray docking at board bottom"
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
