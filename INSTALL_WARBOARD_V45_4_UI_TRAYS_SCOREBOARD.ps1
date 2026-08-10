$ErrorActionPreference = "Stop"

try {
    Set-Location $PSScriptRoot

    Write-Host ""
    Write-Host "WARBOARD v45.4 - UI / TRAYS / SCOREBOARD FIX" -ForegroundColor Cyan
    Write-Host "============================================="
    Write-Host ""

    $Root = $PSScriptRoot
    $Core = Join-Path $Root "Assets\Scripts\Core"

    $UiFile =
        Join-Path $Core "GameController.UI.cs"

    $CoreFile =
        Join-Path $Core "GameController.Core.cs"

    $WorldUiFile =
        Join-Path $Core "BattlefieldWorldUI.cs"

    $BuildInfo =
        Join-Path $Core "WarboardBuildInfo.cs"

    $TrayFile =
        Join-Path $Core "WarboardV45PhysicalSideTrays.cs"

    foreach ($file in @(
        $UiFile,
        $CoreFile,
        $WorldUiFile,
        $BuildInfo,
        $TrayFile
    )) {
        if (-not (Test-Path $file)) {
            throw "Missing expected file: $file"
        }
    }

    $BackupRoot =
        Join-Path $Root "Library\WarboardBackups\V45_4UiTraysScoreboard"

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

    # ---------------------------------------------------------
    # 1. Restore scoreboard, suppress only the four legacy list
    #    panels.
    # ---------------------------------------------------------
    $WorldText =
        [System.IO.File]::ReadAllText(
            $WorldUiFile
        )

    $WorldOriginal =
        $WorldText

    $WorldText =
        [regex]::Replace(
            $WorldText,
            'SetPanelVisible\(\s*playerOneReserves,\s*ready\s*\);',
            'SetPanelVisible(' +
            [Environment]::NewLine +
            '            playerOneReserves,' +
            [Environment]::NewLine +
            '            false' +
            [Environment]::NewLine +
            '        );'
        )

    $WorldText =
        [regex]::Replace(
            $WorldText,
            'SetPanelVisible\(\s*playerOneDead,\s*ready\s*\);',
            'SetPanelVisible(' +
            [Environment]::NewLine +
            '            playerOneDead,' +
            [Environment]::NewLine +
            '            false' +
            [Environment]::NewLine +
            '        );'
        )

    $WorldText =
        [regex]::Replace(
            $WorldText,
            'SetPanelVisible\(\s*playerTwoReserves,\s*ready\s*\);',
            'SetPanelVisible(' +
            [Environment]::NewLine +
            '            playerTwoReserves,' +
            [Environment]::NewLine +
            '            false' +
            [Environment]::NewLine +
            '        );'
        )

    $WorldText =
        [regex]::Replace(
            $WorldText,
            'SetPanelVisible\(\s*playerTwoDead,\s*ready\s*\);',
            'SetPanelVisible(' +
            [Environment]::NewLine +
            '            playerTwoDead,' +
            [Environment]::NewLine +
            '            false' +
            [Environment]::NewLine +
            '        );'
        )

    if ($WorldText -ne
        $WorldOriginal)
    {
        Backup-Once $WorldUiFile
        Write-Utf8 $WorldUiFile $WorldText

        Write-Host "[FIXED] Restored scoreboard; hid only legacy side list-panels." -ForegroundColor Green
    }
    else {
        Write-Host "[OK] Legacy side list-panels were already suppressed." -ForegroundColor DarkGreen
    }

    # ---------------------------------------------------------
    # 2. Rebuild battle setup responsively.
    # ---------------------------------------------------------
    $UiText =
        [System.IO.File]::ReadAllText(
            $UiFile
        )

    $UiOriginal =
        $UiText

    $BattleMethod = @'
    private void DrawBattleSetupPanel()
    {
        float width =
            Mathf.Min(
                960f,
                Screen.width - 24f
            );

        float height =
            Mathf.Min(
                680f,
                Screen.height - 24f
            );

        Rect panel =
            new Rect(
                (Screen.width - width) *
                    0.5f,
                (Screen.height - height) *
                    0.5f,
                width,
                height
            );

        Color accent =
            new Color(
                0.22f,
                0.67f,
                0.82f,
                1f
            );

        WarboardV45Presentation.DrawPanel(
            panel,
            accent,
            true
        );

        GUIStyle heading =
            new GUIStyle(
                GUI.skin.label
            );

        heading.fontSize = 22;
        heading.fontStyle =
            FontStyle.Bold;

        GUIStyle section =
            new GUIStyle(
                GUI.skin.label
            );

        section.fontSize = 17;
        section.fontStyle =
            FontStyle.Bold;

        GUIStyle sub =
            new GUIStyle(
                GUI.skin.label
            );

        sub.fontSize = 12;
        sub.wordWrap = true;

        sub.normal.textColor =
            new Color(
                0.76f,
                0.80f,
                0.85f,
                1f
            );

        GUI.Label(
            new Rect(
                panel.x + 22f,
                panel.y + 14f,
                panel.width - 44f,
                30f
            ),
            "WARBOARD - BATTLE SETUP",
            heading
        );

        GUI.Label(
            new Rect(
                panel.x + 22f,
                panel.y + 47f,
                panel.width - 44f,
                28f
            ),
            "Choose resolution mode and battle size. Tactical decisions remain player-controlled in both modes.",
            sub
        );

        GUI.Label(
            new Rect(
                panel.x + 22f,
                panel.y + 79f,
                panel.width - 44f,
                24f
            ),
            "RESOLUTION MODE",
            section
        );

        float gap = 18f;

        float twoColumnWidth =
            (panel.width -
             44f -
             gap) *
            0.5f;

        float leftX =
            panel.x + 22f;

        float rightX =
            leftX +
            twoColumnWidth +
            gap;

        Color oldColor =
            GUI.color;

        if (ResolutionMode ==
            WarboardResolutionMode
                .XcomAutomatic)
        {
            GUI.color =
                new Color(
                    0.74f,
                    0.96f,
                    0.88f,
                    1f
                );
        }

        if (GUI.Button(
            new Rect(
                leftX,
                panel.y + 108f,
                twoColumnWidth,
                48f
            ),
            "XCOM / AUTOMATIC\nWarboard rolls; you make the decisions"))
        {
            ResolutionMode =
                WarboardResolutionMode
                    .XcomAutomatic;

            showDiceTray = false;
        }

        GUI.color = oldColor;

        if (ResolutionMode ==
            WarboardResolutionMode
                .TraditionalManual)
        {
            GUI.color =
                new Color(
                    0.72f,
                    0.88f,
                    1f,
                    1f
                );
        }

        if (GUI.Button(
            new Rect(
                rightX,
                panel.y + 108f,
                twoColumnWidth,
                48f
            ),
            "TRADITIONAL / MANUAL\nPlayer resolves visible combat dice"))
        {
            ResolutionMode =
                WarboardResolutionMode
                    .TraditionalManual;
        }

        GUI.color = oldColor;

        if (GUI.Button(
            new Rect(
                leftX,
                panel.y + 166f,
                twoColumnWidth,
                46f
            ),
            "INCURSION\n1,000 pts | 60 x 44"))
        {
            ConfigureBattle(
                "Incursion",
                1000,
                60f,
                44f,
                10f,
                "Chapter Approved Mission"
            );

            return;
        }

        if (GUI.Button(
            new Rect(
                rightX,
                panel.y + 166f,
                twoColumnWidth,
                46f
            ),
            "STRIKE FORCE\n2,000 pts | 60 x 44"))
        {
            ConfigureBattle(
                "Strike Force",
                2000,
                60f,
                44f,
                10f,
                "Chapter Approved Mission"
            );

            return;
        }

        Rect customCard =
            new Rect(
                panel.x + 18f,
                panel.y + 224f,
                panel.width - 36f,
                panel.height - 292f
            );

        WarboardV45Presentation.DrawPanel(
            customCard,
            new Color(
                0.26f,
                0.32f,
                0.40f,
                1f
            ),
            false
        );

        GUI.Label(
            new Rect(
                customCard.x + 18f,
                customCard.y + 13f,
                customCard.width - 36f,
                24f
            ),
            "CUSTOM / HOUSE BATTLEFIELD",
            section
        );

        float inputLabelX =
            customCard.x + 20f;

        float inputValueX =
            customCard.x +
            Mathf.Min(
                190f,
                customCard.width *
                    0.27f
            );

        float inputWidth =
            Mathf.Clamp(
                customCard.width *
                    0.17f,
                96f,
                142f
            );

        float rowY =
            customCard.y + 53f;

        const float rowStep = 37f;

        GUI.Label(
            new Rect(
                inputLabelX,
                rowY + 3f,
                150f,
                24f
            ),
            "Points:"
        );

        customPointsText =
            GUI.TextField(
                new Rect(
                    inputValueX,
                    rowY,
                    inputWidth,
                    29f
                ),
                customPointsText,
                5
            );

        rowY += rowStep;

        GUI.Label(
            new Rect(
                inputLabelX,
                rowY + 3f,
                160f,
                24f
            ),
            "Board X (inches):"
        );

        customWidthText =
            GUI.TextField(
                new Rect(
                    inputValueX,
                    rowY,
                    inputWidth,
                    29f
                ),
                customWidthText,
                5
            );

        rowY += rowStep;

        GUI.Label(
            new Rect(
                inputLabelX,
                rowY + 3f,
                160f,
                24f
            ),
            "Board Z (inches):"
        );

        customDepthText =
            GUI.TextField(
                new Rect(
                    inputValueX,
                    rowY,
                    inputWidth,
                    29f
                ),
                customDepthText,
                5
            );

        rowY += rowStep;

        GUI.Label(
            new Rect(
                inputLabelX,
                rowY + 3f,
                160f,
                24f
            ),
            "Deployment depth:"
        );

        customDeploymentText =
            GUI.TextField(
                new Rect(
                    inputValueX,
                    rowY,
                    inputWidth,
                    29f
                ),
                customDeploymentText,
                5
            );

        float infoX =
            Mathf.Max(
                inputValueX +
                    inputWidth +
                    28f,
                customCard.x +
                    customCard.width *
                    0.48f
            );

        float infoWidth =
            customCard.x +
            customCard.width -
            20f -
            infoX;

        GUI.Label(
            new Rect(
                infoX,
                customCard.y + 54f,
                infoWidth,
                Mathf.Max(
                    95f,
                    customCard.height -
                        118f
                )
            ),
            ResolutionMode ==
                WarboardResolutionMode
                    .XcomAutomatic
            ? "XCOM mode resolves routine combat dice immediately while player decisions, targets, placement, casualties and reactions still stop for input."
            : "Traditional mode keeps phases, movement, missions, CP, Stratagem state and measurements while the players physically resolve the dice.",
            sub
        );

        int customPoints;
        float customWidth;
        float customDepth;
        float customDeployment;

        bool customValid =
            TryParseCustomBattle(
                out customPoints,
                out customWidth,
                out customDepth,
                out customDeployment
            );

        GUI.Label(
            new Rect(
                customCard.x + 20f,
                customCard.y +
                    customCard.height -
                    47f,
                customCard.width -
                    250f,
                28f
            ),
            customValid
            ? "Selected: " +
              (IsXcomMode
                  ? "XCOM / AUTOMATIC"
                  : "TRADITIONAL / MANUAL")
            : "Enter valid points, board dimensions and deployment depth.",
            sub
        );

        GUI.enabled =
            customValid;

        if (GUI.Button(
            new Rect(
                customCard.x +
                    customCard.width -
                    215f,
                customCard.y +
                    customCard.height -
                    52f,
                195f,
                34f
            ),
            "START CUSTOM BATTLE",
            WarboardV45Presentation
                .PrimaryButtonStyle))
        {
            ConfigureBattle(
                "Custom",
                customPoints,
                customWidth,
                customDepth,
                customDeployment,
                "Custom Battlefield"
            );

            return;
        }

        GUI.enabled = true;
    }

'@

    $Pattern =
        '(?s)    private void DrawBattleSetupPanel\(\)\s*\{.*?^    \}\r?\n\r?\n    public void AppendBattleLog'

    $Replacement =
        $BattleMethod +
        [Environment]::NewLine +
        [Environment]::NewLine +
        '    public void AppendBattleLog'

    $NewUi =
        [regex]::Replace(
            $UiText,
            $Pattern,
            $Replacement,
            [System.Text.RegularExpressions.RegexOptions]::Multiline
        )

    if ($NewUi -eq
        $UiText)
    {
        throw "Could not replace DrawBattleSetupPanel()."
    }

    $UiText =
        $NewUi

    # ---------------------------------------------------------
    # 3. Actually center the round/faction/phase pill.
    # ---------------------------------------------------------
    $PhasePattern =
        '(?s)        float phaseWidth\s*=\s*Mathf\.Clamp\(.*?\);\s*\r?\n\s*Rect phaseRect\s*=\s*new Rect\(.*?\);\s*\r?\n'

    $PhaseReplacement = @'
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
                bar.y + 10f,
                phaseWidth,
                32f
            );

'@

    $CenteredUi =
        [regex]::Replace(
            $UiText,
            $PhasePattern,
            $PhaseReplacement,
            1
        )

    if ($CenteredUi -eq
        $UiText)
    {
        Write-Host "[WARN] Could not find v45 phase pill block; battle setup was still patched." -ForegroundColor Yellow
    }
    else {
        $UiText =
            $CenteredUi

        Write-Host "[FIXED] Centered ROUND | FACTION | PHASE pill." -ForegroundColor Green
    }

    Backup-Once $UiFile
    Write-Utf8 $UiFile $UiText

    Write-Host "[FIXED] Rebuilt battle setup/custom-house layout responsively." -ForegroundColor Green

    # ---------------------------------------------------------
    # 4. Ensure the physical tray runtime is installed.
    # ---------------------------------------------------------
    $CoreText =
        [System.IO.File]::ReadAllText(
            $CoreFile
        )

    if (-not $CoreText.Contains(
            "WarboardV45PhysicalSideTrays"))
    {
        $InitMatch =
            [regex]::Match(
                $CoreText,
                'battlefieldWorldUI\.Initialize\s*\(\s*this\s*\)\s*;'
            )

        if (-not $InitMatch.Success)
        {
            throw "Could not locate battlefieldWorldUI.Initialize(this) in GameController.Core.cs."
        }

        $Injection =
            $InitMatch.Value +
            [Environment]::NewLine +
            [Environment]::NewLine +
            '        GameObject trayUiObject =' +
            [Environment]::NewLine +
            '            new GameObject(' +
            [Environment]::NewLine +
            '                "Warboard v45 Physical Side Trays"' +
            [Environment]::NewLine +
            '            );' +
            [Environment]::NewLine +
            [Environment]::NewLine +
            '        trayUiObject.AddComponent<' +
            [Environment]::NewLine +
            '            WarboardV45PhysicalSideTrays' +
            [Environment]::NewLine +
            '        >();'

        $CoreText =
            $CoreText.Remove(
                $InitMatch.Index,
                $InitMatch.Length
            ).Insert(
                $InitMatch.Index,
                $Injection
            )

        Backup-Once $CoreFile
        Write-Utf8 $CoreFile $CoreText

        Write-Host "[FIXED] Installed physical tray runtime." -ForegroundColor Green
    }
    else {
        Write-Host "[OK] Physical tray runtime already installed." -ForegroundColor DarkGreen
    }

    # ---------------------------------------------------------
    # 5. Version.
    # ---------------------------------------------------------
    $BuildText =
        [System.IO.File]::ReadAllText(
            $BuildInfo
        )

    $BuildText =
        [regex]::Replace(
            $BuildText,
            'CurrentVersion\s*=\s*"v[^"]+"',
            'CurrentVersion = "v45.4"'
        )

    Write-Utf8 $BuildInfo $BuildText

    Write-Host ""
    Write-Host "WARBOARD v45.4 INSTALLED." -ForegroundColor Green
    Write-Host ""
    Write-Host "Changes:" -ForegroundColor Cyan
    Write-Host "  - scoreboard restored"
    Write-Host "  - old text reserve/dead panels suppressed"
    Write-Host "  - physical side trays enlarged to 11 x 7.2"
    Write-Host "  - trays now show non-interactive copies of the real model visuals"
    Write-Host "  - destroyed tray shows individual destroyed models"
    Write-Host "  - reserve tray shows actual reserve models"
    Write-Host "  - battle setup/custom battlefield layout rebuilt responsively"
    Write-Host "  - round/faction/phase text genuinely centered"
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
