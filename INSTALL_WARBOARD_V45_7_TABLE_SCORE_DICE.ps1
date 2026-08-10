$ErrorActionPreference = "Stop"

try {
    Set-Location $PSScriptRoot

    Write-Host ""
    Write-Host "WARBOARD v45.7 - TABLE / SCORE / DICE POLISH" -ForegroundColor Cyan
    Write-Host "============================================="
    Write-Host ""

    $Root = $PSScriptRoot
    $Core = Join-Path $Root "Assets\Scripts\Core"

    $UiFile =
        Join-Path $Core "GameController.UI.cs"

    $DiceFile =
        Join-Path $Core "TraditionalDiceTray3D.cs"

    $CoreFile =
        Join-Path $Core "GameController.Core.cs"

    $BuildInfo =
        Join-Path $Core "WarboardBuildInfo.cs"

    foreach ($file in @(
        $UiFile,
        $DiceFile,
        $CoreFile,
        $BuildInfo
    )) {
        if (-not (Test-Path $file)) {
            throw "Missing expected file: $file"
        }
    }

    $BackupRoot =
        Join-Path $Root "Library\WarboardBackups\V45_7TableScoreDice"

    New-Item -ItemType Directory -Force -Path $BackupRoot |
        Out-Null

    function Backup-Once([string]$Path) {
        $Name = Split-Path $Path -Leaf
        $Dest = Join-Path $BackupRoot $Name

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
        $methodEnd = -1

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
                    $methodEnd = $i + 1
                    break
                }
            }
        }

        if ($methodEnd -lt 0) {
            return $null
        }

        return [PSCustomObject]@{
            Start = $lineStart
            End = $methodEnd
            Length = $methodEnd - $lineStart
        }
    }

    function Replace-CSharpMethod(
        [string]$Text,
        [string]$Signature,
        [string]$Replacement)
    {
        $range =
            Find-MethodRange `
                -Text $Text `
                -Signature $Signature

        if ($null -eq $range) {
            throw "Could not locate C# method: $Signature"
        }

        return $Text.Remove(
            $range.Start,
            $range.Length
        ).Insert(
            $range.Start,
            $Replacement
        )
    }

    function Insert-AfterAnchor(
        [string]$Text,
        [string]$Anchor,
        [string]$Insertion)
    {
        $index =
            $Text.IndexOf(
                $Anchor,
                [System.StringComparison]::Ordinal
            )

        if ($index -lt 0) {
            throw "Could not locate anchor: $Anchor"
        }

        return $Text.Insert(
            $index + $Anchor.Length,
            $Insertion
        )
    }

    # ---------------------------------------------------------
    # 1. TOP BAR / SCORE STRIP
    # ---------------------------------------------------------
    $UiText =
        [System.IO.File]::ReadAllText(
            $UiFile
        )

    $TopBar = @'
    private void DrawTopCommandBar()
    {
        // WARBOARD_V45_7_TOP_BAR
        Rect bar =
            new Rect(
                8f,
                6f,
                Screen.width - 16f,
                76f
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

        float phaseWidth =
            Mathf.Clamp(
                Screen.width * 0.16f,
                250f,
                286f
            );

        Rect phaseRect =
            new Rect(
                (Screen.width -
                    phaseWidth) *
                    0.5f,
                bar.y + 7f,
                phaseWidth,
                32f
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
            phaseRect,
            roundText +
            "  |  " +
            ActiveFactionDisplayName() +
            "  |  " +
            phaseText,
            WarboardV45Presentation
                .PhasePillStyle
        );

        float leftMetaX =
            bar.x + 12f;

        GUI.Label(
            new Rect(
                leftMetaX,
                bar.y + 7f,
                100f,
                14f
            ),
            IsXcomMode
            ? "XCOM / AUTO"
            : "TRADITIONAL",
            WarboardV45Presentation
                .SubHeaderStyle
        );

        GUI.Label(
            new Rect(
                leftMetaX,
                bar.y + 24f,
                100f,
                14f
            ),
            battleSizeName.ToUpper(),
            WarboardV45Presentation
                .SubHeaderStyle
        );

        float leftX =
            bar.x + 120f;

        const float leftGap = 8f;

        if (GUI.Button(
            new Rect(
                leftX,
                bar.y + 8f,
                86f,
                34f
            ),
            "WARBOARD"))
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

        leftX += 86f + leftGap;

        if (GUI.Button(
            new Rect(
                leftX,
                bar.y + 8f,
                80f,
                34f
            ),
            "MISSION"))
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

        leftX += 80f + leftGap;

        if (GUI.Button(
            new Rect(
                leftX,
                bar.y + 8f,
                54f,
                34f
            ),
            "LOG"))
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

        float rightX =
            bar.x + bar.width - 12f;

        if (!deploymentMode)
        {
            rightX -= 116f;

            if (GUI.Button(
                new Rect(
                    rightX,
                    bar.y + 8f,
                    116f,
                    34f
                ),
                "NEXT PHASE >",
                WarboardV45Presentation
                    .PrimaryButtonStyle))
            {
                NextPhase();
            }
        }

        if (!IsXcomMode &&
            !deploymentMode)
        {
            rightX -= 8f + 86f;

            if (GUI.Button(
                new Rect(
                    rightX,
                    bar.y + 8f,
                    86f,
                    34f
                ),
                showDiceTray
                ? "HIDE CTRL"
                : "DICE CTRL"))
            {
                showDiceTray =
                    !showDiceTray;
            }
        }

        rightX -= 8f + 104f;

        GUI.enabled =
            !deploymentMode;

        if (GUI.Button(
            new Rect(
                rightX,
                bar.y + 8f,
                104f,
                34f
            ),
            "STRATAGEMS"))
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

        rightX -= 8f + 96f;

        if (GUI.Button(
            new Rect(
                rightX,
                bar.y + 8f,
                96f,
                34f
            ),
            "COMMANDS"))
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

        if (!deploymentMode &&
            factions.Count >= 2)
        {
            string p1 = factions[0];
            string p2 = factions[1];

            int p1Primary =
                TotalScoreType(p1, true);

            int p1Secondary =
                TotalScoreType(p1, false);

            int p2Primary =
                TotalScoreType(p2, true);

            int p2Secondary =
                TotalScoreType(p2, false);

            int p1Total =
                p1Primary +
                p1Secondary;

            int p2Total =
                p2Primary +
                p2Secondary;

            Rect scoreRect =
                new Rect(
                    (Screen.width - 620f) *
                        0.5f,
                    bar.y + 46f,
                    620f,
                    24f
                );

            DrawTintedBox(
                scoreRect,
                new Color(
                    0.025f,
                    0.040f,
                    0.060f,
                    0.97f
                )
            );

            GUIStyle scoreStyle =
                new GUIStyle(
                    GUI.skin.label
                );

            scoreStyle.alignment =
                TextAnchor.MiddleCenter;

            scoreStyle.fontStyle =
                FontStyle.Bold;

            scoreStyle.fontSize = 12;

            GUI.Label(
                scoreRect,
                DisplayFactionName(p1).ToUpper() +
                "  " +
                p1Total +
                " VP   P" +
                p1Primary +
                " / S" +
                p1Secondary +
                "   " +
                GetCommandPoints(p1) +
                " CP     ||     " +
                DisplayFactionName(p2).ToUpper() +
                "  " +
                p2Total +
                " VP   P" +
                p2Primary +
                " / S" +
                p2Secondary +
                "   " +
                GetCommandPoints(p2) +
                " CP",
                scoreStyle
            );
        }
    }

'@

    $UiText =
        Replace-CSharpMethod `
            -Text $UiText `
            -Signature "private void DrawTopCommandBar()" `
            -Replacement $TopBar

    Backup-Once $UiFile
    Write-Utf8 $UiFile $UiText

    Write-Host "[FIXED] Top bar spacing cleaned up on both left and right." -ForegroundColor Green
    Write-Host "[FIXED] Score strip enlarged and made more readable." -ForegroundColor Green

    # ---------------------------------------------------------
    # 2. DICE TRAY SHAPE / POSITION
    # ---------------------------------------------------------
    $DiceText =
        [System.IO.File]::ReadAllText(
            $DiceFile
        )

    if ($DiceText.Contains(
            "public void SetWorldSpaceMode("))
    {
        $SetWorld = @'

    public void SetWorldSpaceMode(
        bool enabled)
    {
        EnsureBuilt();

        worldSpaceMode = enabled;

        if (trayRoot == null)
            return;

        if (!enabled)
        {
            trayRoot.SetActive(false);
            return;
        }

        GameObject board =
            GameObject.Find("Board");

        if (board == null)
            return;

        float boardDepth =
            board.transform.localScale.z;

        trayOrigin =
            new Vector3(
                board.transform.position.x,
                0.055f,
                board.transform.position.z -
                    boardDepth * 0.5f -
                    4.25f
            );

        trayRoot.SetActive(true);

        trayRoot.transform.position =
            trayOrigin;

        trayRoot.transform.localScale =
            new Vector3(
                1.55f,
                1.0f,
                0.58f
            );

        Camera main =
            Camera.main;

        if (main != null)
        {
            main.cullingMask |=
                1 << DiceLayer;
        }
    }

'@

        $DiceText =
            Replace-CSharpMethod `
                -Text $DiceText `
                -Signature "public void SetWorldSpaceMode(" `
                -Replacement $SetWorld
    }
    else {
        throw "TraditionalDiceTray3D.cs does not contain SetWorldSpaceMode(). Install v45.6/v45.6a first."
    }

    Backup-Once $DiceFile
    Write-Utf8 $DiceFile $DiceText

    Write-Host "[FIXED] Dice tray now runs longer and thinner along the board." -ForegroundColor Green

    # ---------------------------------------------------------
    # 3. WOOD TABLE IN THE WORLD
    # ---------------------------------------------------------
    $CoreText =
        [System.IO.File]::ReadAllText(
            $CoreFile
        )

    if (-not $CoreText.Contains(
            "WarboardV45EnvironmentTable"))
    {
        $Anchor =
            "battlefieldWorldUI.Initialize(this);"

        $Insertion = @'

        GameObject tableEnvironmentObject =
            new GameObject(
                "Warboard v45 Environment Table"
            );

        tableEnvironmentObject.AddComponent<
            WarboardV45EnvironmentTable
        >();
'@

        $CoreText =
            Insert-AfterAnchor `
                -Text $CoreText `
                -Anchor $Anchor `
                -Insertion $Insertion

        Backup-Once $CoreFile
        Write-Utf8 $CoreFile $CoreText

        Write-Host "[FIXED] Installed wood-table environment runtime." -ForegroundColor Green
    }
    else {
        Write-Host "[OK] Wood-table environment runtime already installed." -ForegroundColor DarkGreen
    }

    # ---------------------------------------------------------
    # 4. VERSION
    # ---------------------------------------------------------
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
    Write-Host "WARBOARD v45.7 INSTALLED." -ForegroundColor Green
    Write-Host ""
    Write-Host "Included changes:" -ForegroundColor Cyan
    Write-Host "  - top-left and top-right buttons de-cramped"
    Write-Host "  - taller, clearer score strip under the centre pill"
    Write-Host "  - long, thinner world-space dice tray"
    Write-Host "  - large wood-style tabletop underneath the board/trays/dice"
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
