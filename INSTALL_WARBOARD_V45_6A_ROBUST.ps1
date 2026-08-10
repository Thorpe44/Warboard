$ErrorActionPreference = "Stop"

try {
    Set-Location $PSScriptRoot

    Write-Host ""
    Write-Host "WARBOARD v45.6a - ROBUST WORLD DICE INSTALLER" -ForegroundColor Cyan
    Write-Host "=============================================="
    Write-Host ""

    $Root = $PSScriptRoot
    $Core = Join-Path $Root "Assets\Scripts\Core"

    $UiFile =
        Join-Path $Core "GameController.UI.cs"

    $DiceFile =
        Join-Path $Core "TraditionalDiceTray3D.cs"

    $BuildInfo =
        Join-Path $Core "WarboardBuildInfo.cs"

    foreach ($file in @(
        $UiFile,
        $DiceFile,
        $BuildInfo
    )) {
        if (-not (Test-Path $file)) {
            throw "Missing expected file: $file"
        }
    }

    $BackupRoot =
        Join-Path $Root "Library\WarboardBackups\V45_6aWorldDice"

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

    function Insert-AfterCSharpMethod(
        [string]$Text,
        [string]$Signature,
        [string]$Insertion)
    {
        $range =
            Find-MethodRange `
                -Text $Text `
                -Signature $Signature

        if ($null -eq $range) {
            throw "Could not locate C# method: $Signature"
        }

        return $Text.Insert(
            $range.End,
            $Insertion
        )
    }

    # ---------------------------------------------------------
    # UI
    # ---------------------------------------------------------
    $UiText =
        [System.IO.File]::ReadAllText(
            $UiFile
        )

    $TopBar = @'
    private void DrawTopCommandBar()
    {
        // WARBOARD_V45_6_BALANCED_TOP_BAR
        Rect bar =
            new Rect(
                6f,
                6f,
                Screen.width - 12f,
                70f
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
                280f
            );

        Rect phaseRect =
            new Rect(
                (Screen.width -
                    phaseWidth) *
                    0.5f,
                bar.y + 5f,
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

        float leftX =
            bar.x + 10f;

        GUI.Label(
            new Rect(
                leftX,
                bar.y + 5f,
                105f,
                15f
            ),
            IsXcomMode
            ? "XCOM / AUTO"
            : "TRADITIONAL",
            WarboardV45Presentation
                .SubHeaderStyle
        );

        GUI.Label(
            new Rect(
                leftX,
                bar.y + 22f,
                105f,
                15f
            ),
            battleSizeName.ToUpper(),
            WarboardV45Presentation
                .SubHeaderStyle
        );

        leftX += 118f;

        if (GUI.Button(
            new Rect(
                leftX,
                bar.y + 5f,
                86f,
                32f
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

        leftX += 92f;

        if (GUI.Button(
            new Rect(
                leftX,
                bar.y + 5f,
                80f,
                32f
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

        leftX += 86f;

        if (GUI.Button(
            new Rect(
                leftX,
                bar.y + 5f,
                58f,
                32f
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

        float right =
            bar.x + bar.width - 10f;

        if (!deploymentMode)
        {
            right -= 122f;

            if (GUI.Button(
                new Rect(
                    right,
                    bar.y + 5f,
                    114f,
                    32f
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
            right -= 94f;

            if (GUI.Button(
                new Rect(
                    right,
                    bar.y + 5f,
                    86f,
                    32f
                ),
                showDiceTray
                ? "HIDE CTRL"
                : "DICE CTRL"))
            {
                showDiceTray =
                    !showDiceTray;
            }
        }

        right -= 108f;

        GUI.enabled =
            !deploymentMode;

        if (GUI.Button(
            new Rect(
                right,
                bar.y + 5f,
                100f,
                32f
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

        right -= 100f;

        if (GUI.Button(
            new Rect(
                right,
                bar.y + 5f,
                92f,
                32f
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

        GUI.enabled = true;

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

            GUIStyle scoreStyle =
                new GUIStyle(
                    GUI.skin.label
                );

            scoreStyle.alignment =
                TextAnchor.MiddleCenter;

            scoreStyle.fontStyle =
                FontStyle.Bold;

            scoreStyle.fontSize = 11;

            Rect scoreRect =
                new Rect(
                    (Screen.width - 470f) *
                        0.5f,
                    bar.y + 41f,
                    470f,
                    24f
                );

            DrawTintedBox(
                scoreRect,
                new Color(
                    0.025f,
                    0.045f,
                    0.060f,
                    0.96f
                )
            );

            GUI.Label(
                scoreRect,
                DisplayFactionName(p1) +
                "  " +
                (p1Primary + p1Secondary) +
                " VP (P" +
                p1Primary +
                "/S" +
                p1Secondary +
                ") | " +
                GetCommandPoints(p1) +
                " CP    ||    " +
                DisplayFactionName(p2) +
                "  " +
                (p2Primary + p2Secondary) +
                " VP (P" +
                p2Primary +
                "/S" +
                p2Secondary +
                ") | " +
                GetCommandPoints(p2) +
                " CP",
                scoreStyle
            );
        }
    }

'@

    $DrawDice = @'
    private void DrawDiceTray()
    {
        if (armyImportMode ||
            deploymentMode)
        {
            if (traditionalDiceTray != null)
            {
                traditionalDiceTray
                    .SetWorldSpaceMode(false);
            }

            return;
        }

        if (IsXcomMode)
        {
            if (traditionalDiceTray != null)
            {
                traditionalDiceTray
                    .SetWorldSpaceMode(false);
            }

            return;
        }

        EnsureTraditionalDiceTray();

        traditionalDiceTray
            .SetWorldSpaceMode(true);

        if (showDiceTray)
            traditionalDiceTray.DrawGUI();
    }

'@

    $UiText =
        Replace-CSharpMethod `
            -Text $UiText `
            -Signature "private void DrawTopCommandBar()" `
            -Replacement $TopBar

    $UiText =
        Replace-CSharpMethod `
            -Text $UiText `
            -Signature "private void DrawDiceTray()" `
            -Replacement $DrawDice

    # The old large context row is intentionally suppressed. The useful wound
    # and restore controls now live in the selected-unit card.
    $UiText =
        $UiText.Replace(
            "        DrawContextActionBar();",
            "        // WARBOARD_V45_6_CONTEXT_CONTROLS_MOVED_TO_UNIT_CARD"
        )

    Backup-Once $UiFile
    Write-Utf8 $UiFile $UiText

    Write-Host "[FIXED] Top command bar replaced using brace-safe method detection." -ForegroundColor Green
    Write-Host "[FIXED] Dice display flow changed to world-space Traditional mode." -ForegroundColor Green

    # ---------------------------------------------------------
    # DICE SYSTEM
    # ---------------------------------------------------------
    $DiceText =
        [System.IO.File]::ReadAllText(
            $DiceFile
        )

    if ($DiceText.Contains(
            "private readonly Vector3 trayOrigin ="))
    {
        $DiceText =
            $DiceText.Replace(
                "private readonly Vector3 trayOrigin =",
                "private Vector3 trayOrigin ="
            )
    }

    if (-not $DiceText.Contains(
            "private bool worldSpaceMode;"))
    {
        $anchor =
            "    private PhysicsMaterial dicePhysics;"

        if (-not $DiceText.Contains($anchor)) {
            throw "Could not find dicePhysics field in TraditionalDiceTray3D.cs."
        }

        $DiceText =
            $DiceText.Replace(
                $anchor,
                $anchor +
                [Environment]::NewLine +
                "    private bool worldSpaceMode;"
            )
    }

    if (-not $DiceText.Contains(
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
                0.05f,
                board.transform.position.z -
                    boardDepth * 0.5f -
                    5.0f
            );

        trayRoot.SetActive(true);

        trayRoot.transform.position =
            trayOrigin;

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
            Insert-AfterCSharpMethod `
                -Text $DiceText `
                -Signature "public void Initialize(" `
                -Insertion $SetWorld
    }

    # Replace Update robustly so repeated installs cannot duplicate click calls.
    $UpdateRange =
        Find-MethodRange `
            -Text $DiceText `
            -Signature "private void Update()"

    if ($null -eq $UpdateRange) {
        throw "Could not locate TraditionalDiceTray3D.Update()."
    }

    $ExistingUpdate =
        $DiceText.Substring(
            $UpdateRange.Start,
            $UpdateRange.Length
        )

    if (-not $ExistingUpdate.Contains(
            "HandleWorldDiceClick();"))
    {
        $braceIndex =
            $ExistingUpdate.IndexOf("{")

        if ($braceIndex -lt 0) {
            throw "Could not locate Update() opening brace."
        }

        $ExistingUpdate =
            $ExistingUpdate.Insert(
                $braceIndex + 1,
                [Environment]::NewLine +
                "        if (worldSpaceMode)" +
                [Environment]::NewLine +
                "            HandleWorldDiceClick();" +
                [Environment]::NewLine
            )

        $DiceText =
            $DiceText.Remove(
                $UpdateRange.Start,
                $UpdateRange.Length
            ).Insert(
                $UpdateRange.Start,
                $ExistingUpdate
            )
    }

    if (-not $DiceText.Contains(
            "private void HandleWorldDiceClick()"))
    {
        $WorldClick = @'

    private void HandleWorldDiceClick()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        Camera main =
            Camera.main;

        if (main == null)
            return;

        Ray ray =
            main.ScreenPointToRay(
                Input.mousePosition
            );

        RaycastHit hit;

        if (!Physics.Raycast(
                ray,
                out hit,
                250f,
                1 << DiceLayer))
        {
            return;
        }

        TraditionalDiceMarker marker =
            hit.collider
                .GetComponentInParent<
                    TraditionalDiceMarker
                >();

        if (marker != null)
        {
            marker.SetSelected(
                !marker.Selected
            );
        }
    }

'@

        $refreshRange =
            Find-MethodRange `
                -Text $DiceText `
                -Signature "private void RefreshSettledText()"

        if ($null -eq $refreshRange) {
            throw "Could not locate RefreshSettledText()."
        }

        $DiceText =
            $DiceText.Insert(
                $refreshRange.Start,
                $WorldClick
            )
    }

    $CompactGui = @'
    public void DrawGUI()
    {
        EnsureBuilt();
        EnsurePoolInitialized();

        float width =
            Mathf.Min(
                650f,
                Screen.width - 28f
            );

        const float height = 176f;

        Rect panel =
            new Rect(
                Screen.width -
                    width -
                    14f,
                Screen.height -
                    height -
                    14f,
                width,
                height
            );

        GUI.Box(panel, "");

        GUIStyle heading =
            new GUIStyle(
                GUI.skin.label
            );

        heading.fontSize = 15;
        heading.fontStyle =
            FontStyle.Bold;

        GUI.Label(
            new Rect(
                panel.x + 12f,
                panel.y + 8f,
                panel.width - 24f,
                22f
            ),
            "WORLD DICE TRAY CONTROLS",
            heading
        );

        float typeY =
            panel.y + 36f;

        float typeWidth =
            (panel.width -
             24f -
             6f * 6f) /
            7f;

        float typeX =
            panel.x + 12f;

        foreach (int sides
            in SupportedSides)
        {
            Color old = GUI.color;

            if (sides == selectedSides)
            {
                GUI.color =
                    new Color(
                        0.76f,
                        0.90f,
                        1f,
                        1f
                    );
            }

            if (GUI.Button(
                new Rect(
                    typeX,
                    typeY,
                    typeWidth,
                    34f
                ),
                "D" +
                sides +
                " " +
                requestedPool[sides]))
            {
                selectedSides = sides;
            }

            GUI.color = old;

            typeX +=
                typeWidth + 6f;
        }

        float y =
            panel.y + 78f;

        if (GUI.Button(
            new Rect(
                panel.x + 12f,
                y,
                40f,
                28f
            ),
            "-5"))
        {
            AdjustSelectedPool(-5);
        }

        if (GUI.Button(
            new Rect(
                panel.x + 56f,
                y,
                40f,
                28f
            ),
            "-1"))
        {
            AdjustSelectedPool(-1);
        }

        GUI.Label(
            new Rect(
                panel.x + 104f,
                y + 4f,
                108f,
                22f
            ),
            "D" +
            selectedSides +
            ": " +
            requestedPool[selectedSides]
        );

        if (GUI.Button(
            new Rect(
                panel.x + 212f,
                y,
                40f,
                28f
            ),
            "+1"))
        {
            AdjustSelectedPool(1);
        }

        if (GUI.Button(
            new Rect(
                panel.x + 256f,
                y,
                40f,
                28f
            ),
            "+5"))
        {
            AdjustSelectedPool(5);
        }

        GUI.enabled =
            RequestedPoolTotal() > 0;

        if (GUI.Button(
            new Rect(
                panel.x + 306f,
                y,
                106f,
                28f
            ),
            "ROLL POOL"))
        {
            RollAll();
        }

        GUI.enabled = true;

        int selectedCount =
            dice.Count(
                die =>
                    die != null &&
                    die.Selected
            );

        GUI.enabled =
            selectedCount > 0;

        if (GUI.Button(
            new Rect(
                panel.x + 418f,
                y,
                122f,
                28f
            ),
            "REROLL SELECTED"))
        {
            RerollSelected();
        }

        GUI.enabled = true;

        if (GUI.Button(
            new Rect(
                panel.x +
                    panel.width -
                    100f,
                y,
                88f,
                28f
            ),
            "CLEAR"))
        {
            ClearDice();
        }

        GUI.Label(
            new Rect(
                panel.x + 12f,
                panel.y + 116f,
                panel.width - 24f,
                22f
            ),
            "Pool: " +
            PoolSummary() +
            " | " +
            settledText +
            (selectedCount > 0
                ? " | " +
                  selectedCount +
                  " selected"
                : "")
        );

        GUI.Label(
            new Rect(
                panel.x + 12f,
                panel.y + 140f,
                panel.width - 24f,
                22f
            ),
            "Dice are physical objects below the battlefield. Click a die there to select it for reroll."
        );
    }

'@

    $DiceText =
        Replace-CSharpMethod `
            -Text $DiceText `
            -Signature "public void DrawGUI()" `
            -Replacement $CompactGui

    Backup-Once $DiceFile
    Write-Utf8 $DiceFile $DiceText

    Write-Host "[FIXED] Existing physics dice tray converted to shared world-space presentation." -ForegroundColor Green
    Write-Host "[FIXED] Giant RenderTexture popup replaced by compact controls." -ForegroundColor Green

    # ---------------------------------------------------------
    # VERSION
    # ---------------------------------------------------------
    $BuildText =
        [System.IO.File]::ReadAllText(
            $BuildInfo
        )

    $BuildText =
        [regex]::Replace(
            $BuildText,
            'CurrentVersion\s*=\s*"v[^"]+"',
            'CurrentVersion = "v45.6"'
        )

    Write-Utf8 $BuildInfo $BuildText

    Write-Host ""
    Write-Host "WARBOARD v45.6 INSTALLED SUCCESSFULLY." -ForegroundColor Green
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
