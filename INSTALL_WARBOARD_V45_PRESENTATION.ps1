$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host ""
Write-Host "WARBOARD v45 - UI / UX / PRESENTATION PASS" -ForegroundColor Cyan
Write-Host "=========================================="
Write-Host ""

$Root = $PSScriptRoot
$Core = Join-Path $Root "Assets\Scripts\Core"

$UI = Join-Path $Core "GameController.UI.cs"
$CoreFile = Join-Path $Core "GameController.Core.cs"
$MissionFile = Join-Path $Core "GameController.Missions.cs"
$ObjectiveFile = Join-Path $Core "ObjectiveController.cs"
$WorldUiFile = Join-Path $Core "BattlefieldWorldUI.cs"
$BuildInfo = Join-Path $Core "WarboardBuildInfo.cs"
$Presentation = Join-Path $Core "WarboardV45Presentation.cs"
$SelectedCard = Join-Path $Core "GameController.V45Presentation.cs"

$Required = @(
    $UI,
    $CoreFile,
    $MissionFile,
    $ObjectiveFile,
    $WorldUiFile,
    $BuildInfo,
    $Presentation,
    $SelectedCard
)

foreach ($file in $Required) {
    if (-not (Test-Path $file)) {
        Write-Host "ERROR: Missing expected file:" -ForegroundColor Red
        Write-Host "  $file"
        Write-Host ""
        Write-Host "Extract this ZIP directly over the Warboard project root first."
        Read-Host "Press Enter to close"
        exit 1
    }
}

$BackupRoot = Join-Path $Root "Library\WarboardBackups\V45Presentation"
New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null

function Backup-File([string]$Path) {
    $name = Split-Path $Path -Leaf
    $dest = Join-Path $BackupRoot $name
    if (-not (Test-Path $dest)) {
        Copy-Item -LiteralPath $Path -Destination $dest -Force
    }
}

function Write-Utf8([string]$Path, [string]$Text) {
    [System.IO.File]::WriteAllText(
        $Path,
        $Text,
        [System.Text.UTF8Encoding]::new($false)
    )
}

foreach ($file in @(
    $UI,
    $CoreFile,
    $MissionFile,
    $ObjectiveFile,
    $WorldUiFile,
    $BuildInfo
)) {
    Backup-File $file
}

# ------------------------------------------------------------
# UI: apply theme at the beginning of every OnGUI pass.
# ------------------------------------------------------------
$Text = [System.IO.File]::ReadAllText($UI)

if (-not $Text.Contains("WarboardV45Presentation.ApplyGuiTheme();")) {
    $Pattern = 'private void OnGUI\(\)\s*\{'
    $Replacement = @'
private void OnGUI()
    {
        // WARBOARD_V45_UI_THEME
        WarboardV45Presentation.ApplyGuiTheme();
'@
    $New = [regex]::Replace(
        $Text,
        $Pattern,
        $Replacement,
        1
    )

    if ($New -eq $Text) {
        throw "v45 installer could not find GameController.OnGUI."
    }

    $Text = $New
    Write-Host "[OK] Applied the v45 global UI skin." -ForegroundColor Green
}

# ------------------------------------------------------------
# UI: rebuilt top command bar.
# ------------------------------------------------------------
if (-not $Text.Contains("WARBOARD_V45_TOP_COMMAND_BAR")) {
    $TopBar = @'
    private void DrawTopCommandBar()
    {
        // WARBOARD_V45_TOP_COMMAND_BAR
        Rect bar =
            new Rect(
                10f,
                8f,
                Screen.width - 20f,
                52f
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

        string phaseText =
            deploymentMode
            ? "DEPLOYMENT"
            : phase.ToString().ToUpper();

        string roundText =
            deploymentMode
            ? "PRE-GAME"
            : "ROUND " + round;

        GUI.Label(
            new Rect(
                bar.x + 16f,
                bar.y + 7f,
                185f,
                22f
            ),
            "WARBOARD " +
            WarboardBuildInfo.CurrentVersion,
            WarboardV45Presentation.HeaderStyle
        );

        GUI.Label(
            new Rect(
                bar.x + 17f,
                bar.y + 30f,
                250f,
                16f
            ),
            (IsXcomMode
                ? "XCOM / AUTOMATIC"
                : "TRADITIONAL") +
            "   •   " +
            battleSizeName.ToUpper(),
            WarboardV45Presentation.SubHeaderStyle
        );

        float phaseWidth =
            Mathf.Clamp(
                Screen.width * 0.24f,
                270f,
                410f
            );

        Rect phaseRect =
            new Rect(
                Mathf.Min(
                    bar.x + 270f,
                    bar.x +
                        bar.width -
                        phaseWidth -
                        650f
                ),
                bar.y + 10f,
                phaseWidth,
                32f
            );

        GUI.Label(
            phaseRect,
            roundText +
            "   •   " +
            ActiveFactionDisplayName() +
            "   •   " +
            phaseText,
            WarboardV45Presentation.PhasePillStyle
        );

        float right =
            bar.x +
            bar.width -
            12f;

        if (!deploymentMode)
        {
            right -= 124f;

            if (GUI.Button(
                new Rect(
                    right,
                    bar.y + 9f,
                    116f,
                    34f
                ),
                "NEXT PHASE  >",
                WarboardV45Presentation
                    .PrimaryButtonStyle))
            {
                NextPhase();
            }
        }

        right -= 86f;

        GUI.enabled =
            !IsXcomMode;

        if (GUI.Button(
            new Rect(
                right,
                bar.y + 9f,
                78f,
                34f
            ),
            IsXcomMode
            ? "AUTO"
            : (showDiceTray
                ? "HIDE DICE"
                : "3D DICE"),
            WarboardV45Presentation
                .ToolbarButtonStyle))
        {
            showDiceTray =
                !showDiceTray;
        }

        GUI.enabled = true;

        right -= 108f;

        GUI.enabled =
            !deploymentMode;

        if (GUI.Button(
            new Rect(
                right,
                bar.y + 9f,
                100f,
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

        right -= 104f;

        if (GUI.Button(
            new Rect(
                right,
                bar.y + 9f,
                96f,
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

        right -= 70f;

        if (GUI.Button(
            new Rect(
                right,
                bar.y + 9f,
                62f,
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

        right -= 98f;

        if (GUI.Button(
            new Rect(
                right,
                bar.y + 9f,
                90f,
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

        right -= 100f;

        if (GUI.Button(
            new Rect(
                right,
                bar.y + 9f,
                92f,
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

        if (selectedSquad != null &&
            !deploymentMode &&
            right > phaseRect.xMax + 108f)
        {
            right -= 104f;

            if (GUI.Button(
                new Rect(
                    right,
                    bar.y + 9f,
                    96f,
                    34f
                ),
                "DATASHEET",
                WarboardV45Presentation
                    .ToolbarButtonStyle))
            {
                showWarboardPanel = false;
                showBasicCommandsPanel = false;
                showStratagemMenu = false;

                OpenDatasheetForSelection();
            }
        }
    }

    private void DrawWarboardPanel()
'@

    $Pattern =
        '(?s)    private void DrawTopCommandBar\(\)\s*\{.*?^    \}\r?\n\r?\n    private void DrawWarboardPanel\(\)'

    $New = [regex]::Replace(
        $Text,
        $Pattern,
        $TopBar,
        [System.Text.RegularExpressions.RegexOptions]::Multiline
    )

    if ($New -eq $Text) {
        throw "v45 installer could not replace DrawTopCommandBar."
    }

    $Text = $New
    Write-Host "[OK] Rebuilt the top command HUD." -ForegroundColor Green
}

# ------------------------------------------------------------
# UI: add selected-unit command card before the status toast.
# ------------------------------------------------------------
if (-not $Text.Contains("DrawV45SelectedUnitCard();")) {
    $Pattern =
        '(\s*DrawContextActionBar\(\);\s*)(DrawStatusToast\(\);)'

    $Replacement =
        '$1DrawV45SelectedUnitCard();' +
        [Environment]::NewLine +
        '        $2'

    $New = [regex]::Replace(
        $Text,
        $Pattern,
        $Replacement,
        1
    )

    if ($New -eq $Text) {
        Write-Host "[WARN] Could not inject the selected-unit card call automatically." -ForegroundColor Yellow
    } else {
        $Text = $New
        Write-Host "[OK] Added the selected-unit command card." -ForegroundColor Green
    }
}

Write-Utf8 $UI $Text

# ------------------------------------------------------------
# Battlefield surface.
# ------------------------------------------------------------
$Text = [System.IO.File]::ReadAllText($CoreFile)

if (-not $Text.Contains("WarboardV45Presentation.StyleBoard(")) {
    $Pattern =
        '(?s)(SetObjectColor\(\s*board,\s*new Color\(0\.19f,\s*0\.22f,\s*0\.19f\)\s*\);)'

    $New = [regex]::Replace(
        $Text,
        $Pattern,
        '$1' +
        [Environment]::NewLine +
        [Environment]::NewLine +
        '        WarboardV45Presentation.StyleBoard(board);',
        1
    )

    if ($New -eq $Text) {
        throw "v45 installer could not find the Board material anchor."
    }

    $Text = $New
    Write-Host "[OK] Textured and framed the battlefield surface." -ForegroundColor Green
}

if (-not $Text.Contains("WarboardV45Presentation.StyleTerrain(")) {
    $Pattern =
        '(?s)(private void CreateTerrain\(.*?SetObjectColor\(\s*terrain,\s*color\s*\);)'

    $New = [regex]::Replace(
        $Text,
        $Pattern,
        '$1' +
        [Environment]::NewLine +
        [Environment]::NewLine +
        '        WarboardV45Presentation.StyleTerrain(' +
        [Environment]::NewLine +
        '            terrain,' +
        [Environment]::NewLine +
        '            trait,' +
        [Environment]::NewLine +
        '            terrain.name,' +
        [Environment]::NewLine +
        '            scale' +
        [Environment]::NewLine +
        '        );',
        1
    )

    if ($New -ne $Text) {
        $Text = $New
    }
}

Write-Utf8 $CoreFile $Text

# ------------------------------------------------------------
# Mission terrain: keep the original gameplay cube/collider but hide its
# renderer and dress it with ruin/barricade/rubble geometry.
# ------------------------------------------------------------
$Text = [System.IO.File]::ReadAllText($MissionFile)

if (-not $Text.Contains("WARBOARD_V45_MISSION_TERRAIN_STYLE")) {
    $Pattern =
        '(?s)(private void CreateMissionTerrainFeature\(.*?SetObjectColor\(\s*terrain,\s*color\s*\);)'

    $Replacement =
        '$1' +
        [Environment]::NewLine +
        [Environment]::NewLine +
        '        // WARBOARD_V45_MISSION_TERRAIN_STYLE' +
        [Environment]::NewLine +
        '        WarboardV45Presentation.StyleTerrain(' +
        [Environment]::NewLine +
        '            terrain,' +
        [Environment]::NewLine +
        '            spec.Trait,' +
        [Environment]::NewLine +
        '            spec.Id,' +
        [Environment]::NewLine +
        '            spec.Size' +
        [Environment]::NewLine +
        '        );'

    $New = [regex]::Replace(
        $Text,
        $Pattern,
        $Replacement,
        1
    )

    if ($New -eq $Text) {
        throw "v45 installer could not find mission terrain creation."
    }

    $Text = $New
    Write-Host "[OK] Replaced terrain-box visuals with dressed ruins/barricades/rubble." -ForegroundColor Green
}

Write-Utf8 $MissionFile $Text

# ------------------------------------------------------------
# Objective nodes.
# ------------------------------------------------------------
$Text = [System.IO.File]::ReadAllText($ObjectiveFile)

if (-not $Text.Contains("WarboardV45Presentation.StyleObjectiveMarker(")) {
    $Pattern =
        '(markerRenderer\s*=\s*marker\.GetComponent<Renderer>\(\);)'

    $Replacement =
        '$1' +
        [Environment]::NewLine +
        [Environment]::NewLine +
        '        WarboardV45Presentation.StyleObjectiveMarker(' +
        [Environment]::NewLine +
        '            marker,' +
        [Environment]::NewLine +
        '            transform,' +
        [Environment]::NewLine +
        '            role' +
        [Environment]::NewLine +
        '        );'

    $New = [regex]::Replace(
        $Text,
        $Pattern,
        $Replacement,
        1
    )

    if ($New -eq $Text) {
        throw "v45 installer could not find the objective marker renderer."
    }

    $Text = $New
    Write-Host "[OK] Upgraded objective markers to sci-fi control nodes." -ForegroundColor Green
}

Write-Utf8 $ObjectiveFile $Text

# ------------------------------------------------------------
# Scoreboard / reserves / dead world panels.
# ------------------------------------------------------------
$Text = [System.IO.File]::ReadAllText($WorldUiFile)

if (-not $Text.Contains("WarboardV45Presentation.StyleWorldPanel(")) {
    $Pattern =
        '(\s*panel\.Text\.text\s*=\s*"";\s*)(\r?\n\s*return panel;)'

    $Replacement =
        '$1' +
        [Environment]::NewLine +
        '        WarboardV45Presentation.StyleWorldPanel(' +
        [Environment]::NewLine +
        '            panel.Root,' +
        [Environment]::NewLine +
        '            panel.Background,' +
        [Environment]::NewLine +
        '            panel.Text,' +
        [Environment]::NewLine +
        '            width,' +
        [Environment]::NewLine +
        '            height,' +
        [Environment]::NewLine +
        '            accentColor' +
        [Environment]::NewLine +
        '        );' +
        '$2'

    $New = [regex]::Replace(
        $Text,
        $Pattern,
        $Replacement,
        1
    )

    if ($New -eq $Text) {
        throw "v45 installer could not find BattlefieldWorldUI panel return."
    }

    $Text = $New
    Write-Host "[OK] Framed and textured scoreboard/reserve/dead panels." -ForegroundColor Green
}

Write-Utf8 $WorldUiFile $Text

# ------------------------------------------------------------
# Version marker.
# ------------------------------------------------------------
$Text = [System.IO.File]::ReadAllText($BuildInfo)
$Text = $Text.Replace(
    'CurrentVersion = "v44"',
    'CurrentVersion = "v45"'
)
Write-Utf8 $BuildInfo $Text

Write-Host ""
Write-Host "WARBOARD v45 PRESENTATION PATCH INSTALLED." -ForegroundColor Green
Write-Host ""
Write-Host "Return to Unity and allow it to import the new textures and compile."
Write-Host ""
Write-Host "This pass changes presentation only:"
Write-Host "  - gameplay terrain colliders/rules remain unchanged"
Write-Host "  - objective control radius remains unchanged"
Write-Host "  - battle logic is untouched"
Write-Host ""
Write-Host "Backups:"
Write-Host "  Library\WarboardBackups\V45Presentation"
Write-Host ""
Read-Host "Press Enter to close"
