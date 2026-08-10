$ErrorActionPreference = "Stop"

try {
    Set-Location $PSScriptRoot

    Write-Host ""
    Write-Host "WARBOARD v47 - RULES ENGINE EXPANSION" -ForegroundColor Cyan
    Write-Host "======================================"
    Write-Host ""

    $Root = $PSScriptRoot
    $Payload = Join-Path $Root "WARBOARD_V47_PAYLOAD"
    $StageRoot = Join-Path $Root "Library\WarboardV47Staging"
    $BackupRoot = Join-Path $Root "Library\WarboardBackups\V47RulesEngine"

    if (-not (Test-Path (Join-Path $Root "Assets")) -or
        -not (Test-Path (Join-Path $Root "ProjectSettings")))
    {
        throw "Extract v47 directly over the main Warboard Unity project folder."
    }

    if (-not (Test-Path $Payload)) {
        throw "WARBOARD_V47_PAYLOAD is missing. Re-extract the ZIP."
    }

    $requiredV46 = @(
        "Assets\Scripts\Core\WarboardFactionExtensionHub.cs",
        "Assets\Scripts\Factions\Standard11\StandardFactionGameController.cs",
        "Assets\Scripts\Factions\Standard11\StandardFactionSetupUI.cs",
        "Assets\Scripts\Core\RulesEngine.cs",
        "Assets\Scripts\Core\InteractiveAttackController.cs",
        "Assets\Scripts\Core\SquadController.cs",
        "Assets\Scripts\Core\GameController.Combat.cs",
        "Assets\Scripts\Core\GameController.Charge.cs",
        "Assets\Scripts\Core\GameController.CoreCompletion11.cs",
        "Assets\Scripts\Core\WarboardBuildInfo.cs"
    )

    foreach ($relative in $requiredV46) {
        $path = Join-Path $Root $relative

        if (-not (Test-Path $path)) {
            throw "Missing v46 baseline file: $relative"
        }
    }

    $hubText = [System.IO.File]::ReadAllText(
        (Join-Path $Root "Assets\Scripts\Core\WarboardFactionExtensionHub.cs")
    )

    if (-not $hubText.Contains("WARBOARD_V46") -and
        -not $hubText.Contains("StandardFactionGameController"))
    {
        throw "v47 requires the working v46 faction integration. Install v46e first."
    }

    Remove-Item -LiteralPath $StageRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $StageRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null

    $backedUp = New-Object System.Collections.Generic.List[string]
    $newFiles = New-Object System.Collections.Generic.List[string]
    $commitStarted = $false

    function Write-Utf8(
        [string]$Path,
        [string]$Text)
    {
        $parent = Split-Path $Path -Parent

        if (-not (Test-Path $parent)) {
            New-Item -ItemType Directory -Force -Path $parent | Out-Null
        }

        [System.IO.File]::WriteAllText(
            $Path,
            $Text,
            [System.Text.UTF8Encoding]::new($false)
        )
    }

    function Backup-Once(
        [string]$Relative)
    {
        $source = Join-Path $Root $Relative

        if (-not (Test-Path $source)) {
            return
        }

        if ($script:backedUp.Contains($Relative)) {
            return
        }

        $destination = Join-Path $BackupRoot $Relative
        $parent = Split-Path $destination -Parent
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
        Copy-Item -LiteralPath $source -Destination $destination -Force
        $script:backedUp.Add($Relative)
    }

    function Find-MethodRange(
        [string]$Text,
        [string]$Signature)
    {
        $signatureIndex = $Text.IndexOf(
            $Signature,
            [System.StringComparison]::Ordinal
        )

        if ($signatureIndex -lt 0) {
            return $null
        }

        $lineStart = $Text.LastIndexOf("`n", $signatureIndex)

        if ($lineStart -lt 0) {
            $lineStart = 0
        }
        else {
            $lineStart++
        }

        $openBrace = $Text.IndexOf(
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

        for ($i = $openBrace; $i -lt $Text.Length; $i++) {
            $c = $Text[$i]
            $next = if ($i + 1 -lt $Text.Length) { $Text[$i + 1] } else { [char]0 }

            if ($inLineComment) {
                if ($c -eq "`n") { $inLineComment = $false }
                continue
            }

            if ($inBlockComment) {
                if ($c -eq "*" -and $next -eq "/") {
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

                if ($c -eq '"') { $inString = $false }
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

                if ($c -eq "'") { $inChar = $false }
                continue
            }

            if ($c -eq "/" -and $next -eq "/") {
                $inLineComment = $true
                $i++
                continue
            }

            if ($c -eq "/" -and $next -eq "*") {
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
            End = $closeBrace + 1
            Length = $closeBrace + 1 - $lineStart
        }
    }

    function Replace-Exact(
        [string]$Text,
        [string]$Old,
        [string]$New,
        [string]$Label)
    {
        if ($Text.Contains($New)) {
            Write-Host ("[OK] " + $Label + " already installed.") -ForegroundColor DarkGreen
            return $Text
        }

        if ($Text.Contains($Old)) {
            $exactPattern =
                [System.Text.RegularExpressions.Regex]::Escape(
                    $Old
                )

            $exactMatches =
                [System.Text.RegularExpressions.Regex]::Matches(
                    $Text,
                    $exactPattern
                )

            if ($exactMatches.Count -ne 1) {
                throw "Ambiguous exact anchor for $Label. Matches: $($exactMatches.Count)"
            }

            $match = $exactMatches[0]
            $result = $Text.Substring(0, $match.Index) + $New + $Text.Substring($match.Index + $match.Length)

            Write-Host ("[FIXED] " + $Label) -ForegroundColor Green
            return $result
        }

        $parts = [System.Text.RegularExpressions.Regex]::Split(
            $Old.Trim(),
            '\s+'
        )

        $escaped = New-Object System.Collections.Generic.List[string]

        foreach ($part in $parts) {
            if (-not [string]::IsNullOrWhiteSpace($part)) {
                $escaped.Add(
                    [System.Text.RegularExpressions.Regex]::Escape($part)
                )
            }
        }

        $pattern = [string]::Join('\s+', $escaped.ToArray())
        $regex = New-Object System.Text.RegularExpressions.Regex(
            $pattern,
            [System.Text.RegularExpressions.RegexOptions]::Singleline
        )

        $matches = $regex.Matches($Text)

        if ($matches.Count -ne 1) {
            throw "Could not uniquely patch $Label. Flexible matches: $($matches.Count)"
        }

        $match = $matches[0]
        $result = $Text.Substring(0, $match.Index) + $New + $Text.Substring($match.Index + $match.Length)

        Write-Host ("[FIXED] " + $Label + " (flexible anchor)") -ForegroundColor Green
        return $result
    }

    function Replace-InMethod(
        [string]$Text,
        [string]$Signature,
        [string]$Old,
        [string]$New,
        [string]$Label)
    {
        $range = Find-MethodRange -Text $Text -Signature $Signature

        if ($null -eq $range) {
            throw ("Could not locate C# method for " + $Label + ": " + $Signature)
        }

        $method = $Text.Substring($range.Start, $range.Length)
        $patched = Replace-Exact -Text $method -Old $Old -New $New -Label $Label

        if ($null -eq $patched -or -not ($patched -is [string])) {
            throw "Method patch returned an invalid buffer for: $Label"
        }

        return $Text.Remove($range.Start, $range.Length).Insert($range.Start, $patched)
    }

    function Replace-Method(
        [string]$Text,
        [string]$Signature,
        [string]$Replacement,
        [string]$Label)
    {
        $range = Find-MethodRange -Text $Text -Signature $Signature

        if ($null -eq $range) {
            throw ("Could not locate C# method for " + $Label + ": " + $Signature)
        }

        $existing = $Text.Substring($range.Start, $range.Length)

        if ($existing.Contains("WARBOARD_V47_") -and
            $existing.Contains($Label.Replace(" ", "_")))
        {
            return $Text
        }

        Write-Host ("[FIXED] " + $Label + " (structural method replacement)") -ForegroundColor Green
        return $Text.Remove($range.Start, $range.Length).Insert($range.Start, $Replacement)
    }

    function Stage-Patch(
        [string]$Relative,
        [ScriptBlock]$Patcher)
    {
        $source = Join-Path $Root $Relative
        $text = [System.IO.File]::ReadAllText($source)
        $patched = & $Patcher $text

        if ($null -eq $patched) {
            throw "Patcher returned null for: $Relative"
        }

        if (-not ($patched -is [string])) {
            $patched = [string]$patched
        }

        $staged = Join-Path $StageRoot $Relative
        Write-Utf8 -Path $staged -Text $patched
    }

    # ------------------------------------------------------------------
    # INSTALLER SELF-TESTS BEFORE PROJECT PATCHING
    # ------------------------------------------------------------------
    $selfSource = "alpha`r`n    beta`r`ngamma"
    $selfOld = "alpha`nbeta"
    $selfNew = "alpha`nPATCHED`nbeta"
    $selfResult = Replace-Exact -Text $selfSource -Old $selfOld -New $selfNew -Label "v47 flexible-anchor self-test"

    if ([string]::IsNullOrWhiteSpace($selfResult) -or
        -not $selfResult.Contains("PATCHED"))
    {
        throw "v47 patch-engine self-test failed."
    }

    Write-Host "[OK] v47 patch engine self-test passed." -ForegroundColor Cyan
    Write-Host ""

    # ------------------------------------------------------------------
    # SQUAD CONTROLLER: enhancement keywords, saves and OC
    # ------------------------------------------------------------------
    Stage-Patch "Assets\Scripts\Core\SquadController.cs" {
        param($text)

        $old = @'
        if (NecronsFactionPack11.GrantsKeyword(
                this, keyword))
        {
            return true;
        }


        if (HasOwnKeyword(keyword))
'@

        $new = @'
        if (NecronsFactionPack11.GrantsKeyword(
                this, keyword))
        {
            return true;
        }

        // WARBOARD_V47_ENHANCEMENT_KEYWORDS
        if (WarboardV47FactionRules.GrantsKeyword(
                this,
                keyword))
        {
            return true;
        }

        if (HasOwnKeyword(keyword))
'@

        $text = Replace-InMethod -Text $text -Signature "public bool HasKeyword(" -Old $old -New $new -Label "enhancement-granted unit keywords"

        $old = @'
        if (JoinedActionController().IsBattleShocked)
            return 0;

        int objectiveControl =
            model.ObjectiveControl;
'@

        $new = @'
        // WARBOARD_V47_BATTLESHOCK_OC_OVERRIDE
        int battleShockOverride =
            WarboardV47FactionRules
                .BattleShockedObjectiveControl(
                    this,
                    model
                );

        if (JoinedActionController().IsBattleShocked &&
            battleShockOverride < 0)
        {
            return 0;
        }

        int objectiveControl =
            battleShockOverride >= 0
            ? battleShockOverride
            : model.ObjectiveControl;
'@

        $text = Replace-InMethod -Text $text -Signature "public int EffectiveObjectiveControl(" -Old $old -New $new -Label "enhancement Battle-shock OC override"

        $old = @'
        objectiveControl =
            NecronsFactionPack11.ModifyObjectiveControl(
                JoinedActionController(), model, objectiveControl);

        return Mathf.Max(
'@

        $new = @'
        objectiveControl =
            NecronsFactionPack11.ModifyObjectiveControl(
                JoinedActionController(), model, objectiveControl);

        // WARBOARD_V47_ENHANCEMENT_OC
        objectiveControl =
            WarboardV47FactionRules
                .ModifyObjectiveControl(
                    JoinedActionController(),
                    model,
                    objectiveControl
                );

        return Mathf.Max(
'@

        $text = Replace-InMethod -Text $text -Signature "public int EffectiveObjectiveControl(" -Old $old -New $new -Label "enhancement Objective Control modifier"

        $old = @'
        if (actionUnit.IsBattleShocked)
            return 0;

        int total = 0;
'@

        $new = @'
        // WARBOARD_V47_OC_PER_MODEL
        // EffectiveObjectiveControl handles normal Battle-shock (OC 0) and
        // exact bearer exceptions such as Stoic Defender.
        int total = 0;
'@

        $text = Replace-InMethod -Text $text -Signature "public int TotalObjectiveControlWithin(" -Old $old -New $new -Label "per-model Battle-shock OC calculation"

        $old = @'
            int oc =
                actionUnit
                    .AeldariObjectiveControlOverride > 0
                ? actionUnit
                    .AeldariObjectiveControlOverride
                : model.ObjectiveControl;

            total +=
                Mathf.Max(0, oc);
'@

        $new = @'
            int oc =
                actionUnit
                    .EffectiveObjectiveControl(
                        model
                    );

            total +=
                Mathf.Max(0, oc);
'@

        $text = Replace-InMethod -Text $text -Signature "public int TotalObjectiveControlWithin(" -Old $old -New $new -Label "shared effective OC in objective totals"

        $old = @'
        return Mathf.Clamp(value, 2, 6);
'@

        $new = @'
        // WARBOARD_V47_ENHANCEMENT_SAVE
        value =
            WarboardV47FactionRules
                .SaveOverride(
                    this,
                    value
                );

        return Mathf.Clamp(value, 2, 6);
'@

        $text = Replace-InMethod -Text $text -Signature "public int GetSave(" -Old $old -New $new -Label "enhancement Save characteristic override"

        return $text
    }

    # ------------------------------------------------------------------
    # TARGET SELECTION + RICH ATTACK SUMMARY
    # ------------------------------------------------------------------
    Stage-Patch "Assets\Scripts\Core\GameController.Combat.cs" {
        param($text)

        $old = @'
        ClearArmedCommandReroll();
'@

        $new = @'
        // WARBOARD_V47_TARGET_SELECTION_EVENT
        string v47TargetReason;

        if (!WarboardV47FactionRules.CanAttackTarget(
                attacker,
                target,
                attackMode,
                out v47TargetReason))
        {
            status = v47TargetReason;
            return;
        }

        WarboardAttackDieLedger47.Clear();

        WarboardRuleEventBus47.RaiseTargetSelected(
            this,
            attacker,
            target,
            attackMode
        );

        ClearArmedCommandReroll();
'@

        $text = Replace-InMethod -Text $text -Signature "private void BeginInteractiveAttack(" -Old $old -New $new -Label "rich attack target-selection event"

        $old = @'
        interactiveAttack = null;
'@

        $new = @'
        // WARBOARD_V47_INTERACTIVE_ATTACK_SUMMARY
        WarboardRuleEventBus47.RaiseAttackSummary(
            this,
            resolvedAttack.Attacker,
            resolvedAttack.Target,
            resolvedAttack.Mode,
            resolvedAttack.TotalAttacks,
            resolvedAttack.TotalHits,
            resolvedAttack.TotalWounds,
            resolvedAttack.TotalWoundsLost,
            resolvedAttack.TotalModelsKilled,
            "Interactive/XCOM attack resolved."
        );

        interactiveAttack = null;
'@

        $text = Replace-InMethod -Text $text -Signature "private void FinalizeInteractiveAttack()" -Old $old -New $new -Label "interactive rich attack summary event"

        return $text
    }

    # ------------------------------------------------------------------
    # CHARGE TARGET STATE
    # ------------------------------------------------------------------
    Stage-Patch "Assets\Scripts\Core\GameController.Charge.cs" {
        param($text)

        $old = @'
        NotifyChargeDeclared(

            attacker,

            target);
'@

        $new = @'
        // WARBOARD_V47_CHARGE_TARGET_STATE
        string v47TargetReason;

        if (!WarboardV47FactionRules.CanAttackTarget(
                attacker,
                target,
                AttackMode.Melee,
                out v47TargetReason))
        {
            status = v47TargetReason;
            return;
        }

        WarboardRuleEventBus47.RaiseTargetSelected(
            this,
            attacker,
            target,
            AttackMode.Melee
        );

        NotifyChargeDeclared(
            attacker,
            target);
'@

        $text = Replace-InMethod -Text $text -Signature "private void TryCharge(" -Old $old -New $new -Label "charge target-state validation and event"

        return $text
    }

    # ------------------------------------------------------------------
    # AUTOMATIC RULES ENGINE: per-critical-hit Precision/provenance
    # ------------------------------------------------------------------
    Stage-Patch "Assets\Scripts\Core\RulesEngine.cs" {
        param($text)

        $old = @'
        int totalAttacks = 0;
'@

        $new = @'
        // WARBOARD_V47_AUTOMATIC_ATTACK_LEDGER
        WarboardAttackDieLedger47.Clear();

        string v47TargetReason;

        if (!WarboardV47FactionRules.CanAttackTarget(
                attacker,
                target,
                mode,
                out v47TargetReason))
        {
            return new AttackResult(
                v47TargetReason,
                0,
                0,
                0,
                0,
                0,
                0
            );
        }

        WarboardRuleEventBus47.RaiseTargetSelected(
            game,
            attacker,
            target,
            mode
        );

        int totalAttacks = 0;
'@

        $text = Replace-InMethod -Text $text -Signature "public static AttackResult ResolveWeaponAttacks(" -Old $old -New $new -Label "automatic attack ledger and target gate"

        $old = @'
            // WARBOARD_V46_RULES_STANDARD_ATTACKS
            attacks +=
                WarboardFactionExtensionHub
                    .AdditionalAttacks(
                        attacker,
                        weapon,
                        mode
                    );
'@

        $new = @'
            // WARBOARD_V46_RULES_STANDARD_ATTACKS
            // WARBOARD_V47_MODEL_AWARE_ADDITIONAL_ATTACKS
            attacks +=
                WarboardFactionExtensionHub
                    .AdditionalAttacks(
                        game,
                        attacker,
                        target,
                        model,
                        weapon,
                        mode
                    );
'@

        $text = Replace-InMethod -Text $text -Signature "public static AttackResult ResolveWeaponAttacks(" -Old $old -New $new -Label "model-aware enhancement attack count"

        $old = @'
            precision =
                precision ||
                WarboardFactionExtensionHub
                    .GrantsPrecision(
                        attacker,
                        target,
                        weapon,
                        mode
                    );

int melta =
'@

        $new = @'
            precision =
                precision ||
                WarboardFactionExtensionHub
                    .GrantsPrecision(
                        attacker,
                        target,
                        weapon,
                        mode
                    );

            // WARBOARD_V47_CRITICAL_PRECISION_FLAG
            bool precisionOnCriticalHit =
                WarboardFactionExtensionHub
                    .GrantsPrecisionOnCriticalHit(
                        attacker,
                        target,
                        weapon,
                        mode
                    );

int melta =
'@

        $text = Replace-InMethod -Text $text -Signature "public static AttackResult ResolveWeaponAttacks(" -Old $old -New $new -Label "automatic critical-hit Precision capability"

        $old = @'
            int hits = 0;
            int lethalAutoWounds = 0;
'@

        $new = @'
            int hits = 0;
            int lethalAutoWounds = 0;

            int v47PrecisionCriticalHits = 0;
            int v47PrecisionLethalAutoWounds = 0;
'@

        $text = Replace-InMethod -Text $text -Signature "public static AttackResult ResolveWeaponAttacks(" -Old $old -New $new -Label "automatic precision-hit counters"

        $old = @'
                if (!AeldariFactionPack11.AutomaticHitSucceeds(
                        hitRoll, skill, aeldari11UniversalState))
                    continue;

                hits++;

                if (NecronsFactionPack11.IsCriticalHit(
                        attacker, hitRoll, true))
                {
                    if (lethalHits)
                        lethalAutoWounds++;

                    if (sustainedHits > 0)
                    {
                        hits +=
                            sustainedHits;

                        report.sustainedExtraHits +=
                            sustainedHits;
                    }
                }
'@

        $new = @'
                bool v47HitSuccess =
                    AeldariFactionPack11.AutomaticHitSucceeds(
                        hitRoll,
                        skill,
                        aeldari11UniversalState
                    );

                bool v47CriticalHit =
                    WarboardV47FactionRules.IsCriticalHit(
                        attacker,
                        hitRoll,
                        v47HitSuccess
                    );

                bool v47HitPrecision =
                    precision ||
                    (precisionOnCriticalHit &&
                     v47CriticalHit);

                WarboardAttackDieLedger47.RecordHit(
                    game,
                    attacker,
                    target,
                    model,
                    weapon,
                    mode,
                    hitRoll,
                    v47HitSuccess,
                    v47CriticalHit,
                    false,
                    v47HitPrecision,
                    v47CriticalHit && lethalHits,
                    v47CriticalHit
                    ? sustainedHits
                    : 0
                );

                WarboardRuleEventBus47.RaiseHit(
                    game,
                    attacker,
                    target,
                    model,
                    weapon,
                    mode,
                    hitRoll,
                    v47HitSuccess,
                    v47CriticalHit,
                    false
                );

                if (!v47HitSuccess)
                    continue;

                hits++;

                if (v47CriticalHit)
                {
                    if (v47HitPrecision)
                        v47PrecisionCriticalHits++;

                    if (lethalHits)
                    {
                        lethalAutoWounds++;

                        if (v47HitPrecision)
                            v47PrecisionLethalAutoWounds++;
                    }

                    if (sustainedHits > 0)
                    {
                        hits +=
                            sustainedHits;

                        report.sustainedExtraHits +=
                            sustainedHits;
                    }
                }
'@

        $text = Replace-InMethod -Text $text -Signature "public static AttackResult ResolveWeaponAttacks(" -Old $old -New $new -Label "automatic per-hit critical provenance"

        $old = @'
            int normalWoundRolls =
                Mathf.Max(
                    0,
                    hits -
                    lethalAutoWounds
                );
'@

        $new = @'
            int normalWoundRolls =
                Mathf.Max(
                    0,
                    hits -
                    lethalAutoWounds
                );

            // Critical-hit Precision follows the original successful hit into
            // its wound/save allocation. Sustained extra hits are not Critical
            // Hits and therefore do not inherit Precision.
            int v47PrecisionNormalWounds =
                precision
                ? lethalAutoWounds
                : v47PrecisionLethalAutoWounds;

            int v47PrecisionDevastatingWounds = 0;

            int v47PrecisionWoundRolls =
                precision
                ? normalWoundRolls
                : Mathf.Max(
                    0,
                    v47PrecisionCriticalHits -
                    v47PrecisionLethalAutoWounds
                );
'@

        $text = Replace-InMethod -Text $text -Signature "public static AttackResult ResolveWeaponAttacks(" -Old $old -New $new -Label "automatic Precision wound provenance"

        $old = @'
                if (!success)
                    continue;

                if (critical &&
                    devastating)
                {
                    devastatingWounds++;
                }
                else
                {
                    normalWounds++;
                }
'@

        $new = @'
                bool v47WoundPrecision =
                    precision ||
                    i < v47PrecisionWoundRolls;

                WarboardAttackDieLedger47.RecordWound(
                    game,
                    attacker,
                    target,
                    model,
                    weapon,
                    mode,
                    woundRoll,
                    success,
                    critical,
                    alreadyRerolled,
                    v47WoundPrecision,
                    success && critical && devastating
                );

                WarboardRuleEventBus47.RaiseWound(
                    game,
                    attacker,
                    target,
                    model,
                    weapon,
                    mode,
                    woundRoll,
                    success,
                    critical,
                    alreadyRerolled
                );

                if (!success)
                    continue;

                if (critical &&
                    devastating)
                {
                    devastatingWounds++;

                    if (v47WoundPrecision)
                        v47PrecisionDevastatingWounds++;
                }
                else
                {
                    normalWounds++;

                    if (v47WoundPrecision)
                        v47PrecisionNormalWounds++;
                }
'@

        $text = Replace-InMethod -Text $text -Signature "public static AttackResult ResolveWeaponAttacks(" -Old $old -New $new -Label "automatic per-wound Precision provenance"

        $old = @'
            for (int i = 0;
                 i < normalWounds;
                 i++)
            {
                ModelToken allocated =
                    GetAllocationModel(
                        game,
                        model,
                        target,
                        precision
                    );
'@

        $new = @'
            for (int i = 0;
                 i < normalWounds;
                 i++)
            {
                bool v47SavePrecision =
                    precision ||
                    i < v47PrecisionNormalWounds;

                ModelToken allocated =
                    GetAllocationModel(
                        game,
                        model,
                        target,
                        v47SavePrecision
                    );
'@

        $text = Replace-InMethod -Text $text -Signature "public static AttackResult ResolveWeaponAttacks(" -Old $old -New $new -Label "automatic normal-wound Precision allocation"

        $old = @'
            for (int i = 0;
                 i < devastatingWounds;
                 i++)
            {
                ModelToken allocated =
                    GetAllocationModel(
                        game,
                        model,
                        target,
                        precision
                    );
'@

        $new = @'
            for (int i = 0;
                 i < devastatingWounds;
                 i++)
            {
                bool v47DevastatingPrecision =
                    precision ||
                    i < v47PrecisionDevastatingWounds;

                ModelToken allocated =
                    GetAllocationModel(
                        game,
                        model,
                        target,
                        v47DevastatingPrecision
                    );
'@

        $text = Replace-InMethod -Text $text -Signature "public static AttackResult ResolveWeaponAttacks(" -Old $old -New $new -Label "automatic devastating-wound Precision allocation"

        $old = @'
        return new AttackResult(
            text,
            totalAttacks,
            totalHits,
            totalWounds,
            totalFailedSaves,
            totalWoundsLost,
            totalModelsKilled
        );
'@

        $new = @'
        // WARBOARD_V47_AUTOMATIC_ATTACK_SUMMARY
        WarboardRuleEventBus47.RaiseAttackSummary(
            game,
            attacker,
            target,
            mode,
            totalAttacks,
            totalHits,
            totalWounds,
            totalWoundsLost,
            totalModelsKilled,
            "Automatic RulesEngine attack resolved."
        );

        return new AttackResult(
            text,
            totalAttacks,
            totalHits,
            totalWounds,
            totalFailedSaves,
            totalWoundsLost,
            totalModelsKilled
        );
'@

        $text = Replace-InMethod -Text $text -Signature "public static AttackResult ResolveWeaponAttacks(" -Old $old -New $new -Label "automatic rich attack summary event"

        return $text
    }

    # ------------------------------------------------------------------
    # INTERACTIVE/XCOM ATTACK: per-die Precision allocation
    # ------------------------------------------------------------------
    Stage-Patch "Assets\Scripts\Core\InteractiveAttackController.cs" {
        param($text)

        $old = @'
    public bool precision;
    public int meltaBonus;
'@

        $new = @'
    public bool precision;

    // WARBOARD_V47_INTERACTIVE_DIE_PROVENANCE
    public bool precisionOnCriticalHit;
    public int precisionCriticalHits;
    public int precisionLethalAutoWounds;
    public int precisionNormalWounds;
    public int precisionDevastatingWounds;

    public readonly List<bool> woundPrecisionFlags =
        new List<bool>();

    public readonly List<int> saveTargetsPerDie =
        new List<int>();

    public readonly List<bool> savePrecisionFlags =
        new List<bool>();

    public readonly List<bool> failedSavePrecisionFlags =
        new List<bool>();

    public int meltaBonus;
'@

        $text = Replace-Exact -Text $text -Old $old -New $new -Label "interactive per-die provenance fields"

        $old = @'
    public int TotalHits
    {
        get { return volleys.Sum(volley => volley.hits); }
    }
'@

        $new = @'
    public int TotalHits
    {
        get { return volleys.Sum(volley => volley.hits); }
    }

    public int TotalAttacks
    {
        get { return volleys.Sum(volley => volley.attacks); }
    }

    public int TotalWounds
    {
        get
        {
            return volleys.Sum(
                volley =>
                    volley.normalWounds +
                    volley.devastatingWounds
            );
        }
    }
'@

        $text = Replace-Exact -Text $text -Old $old -New $new -Label "interactive attack summary totals"

        $old = @'
                // WARBOARD_V46_INTERACTIVE_STANDARD_ATTACKS
                oneModelAttacks +=
                    WarboardFactionExtensionHub
                        .AdditionalAttacks(
                            attacker,
                            weapon,
                            mode
                        );
'@

        $new = @'
                // WARBOARD_V46_INTERACTIVE_STANDARD_ATTACKS
                // WARBOARD_V47_MODEL_AWARE_ADDITIONAL_ATTACKS
                oneModelAttacks +=
                    WarboardFactionExtensionHub
                        .AdditionalAttacks(
                            game,
                            attacker,
                            target,
                            selection.model,
                            weapon,
                            mode
                        );
'@

        $text = Replace-InMethod -Text $text -Signature "private void BuildVolleys(" -Old $old -New $new -Label "interactive model-aware enhancement attack count"

        $old = @'
            // WARBOARD_V46_INTERACTIVE_STANDARD_PRECISION
            volley.precision =
                volley.precision ||
                WarboardFactionExtensionHub
                    .GrantsPrecision(
                        attacker,
                        target,
                        weapon,
                        mode
                    );
'@

        $new = @'
            // WARBOARD_V46_INTERACTIVE_STANDARD_PRECISION
            volley.precision =
                volley.precision ||
                WarboardFactionExtensionHub
                    .GrantsPrecision(
                        attacker,
                        target,
                        weapon,
                        mode
                    );

            // WARBOARD_V47_INTERACTIVE_CRITICAL_PRECISION
            volley.precisionOnCriticalHit =
                WarboardFactionExtensionHub
                    .GrantsPrecisionOnCriticalHit(
                        attacker,
                        target,
                        weapon,
                        mode
                    );
'@

        $text = Replace-InMethod -Text $text -Signature "private void BuildVolleys(" -Old $old -New $new -Label "interactive critical-hit Precision capability"

        $old = @'
            case InteractiveAttackStage.ReviewHits:
                stage =
                    InteractiveAttackStage.RollWounds;
                break;

            case InteractiveAttackStage.ReviewWounds:
'@

        $new = @'
            case InteractiveAttackStage.ReviewHits:
                WarboardAttackDieLedger47.EmitStageEvents(
                    game,
                    attacker,
                    target,
                    CurrentVolley.weapon,
                    WarboardAttackDieStage47.Hit
                );

                stage =
                    InteractiveAttackStage.RollWounds;
                break;

            case InteractiveAttackStage.ReviewWounds:
                WarboardAttackDieLedger47.EmitStageEvents(
                    game,
                    attacker,
                    target,
                    CurrentVolley.weapon,
                    WarboardAttackDieStage47.Wound
                );
'@

        $text = Replace-InMethod -Text $text -Signature "public void Continue()" -Old $old -New $new -Label "interactive final per-die hit/wound events"

        $replacement = @'
    private void RecalculateHitResults()
    {
        // WARBOARD_V47_RECALCULATE_HITS
        InteractiveWeaponVolley volley =
            CurrentVolley;

        WarboardAttackDieLedger47.ClearAttackStage(
            attacker,
            target,
            volley.weapon,
            WarboardAttackDieStage47.Hit
        );

        int hits = 0;
        int lethal = 0;
        int precisionCriticalHits = 0;
        int precisionLethal = 0;

        ModelToken sourceModel =
            volley.selections.Count > 0
            ? volley.selections[0].model
            : null;

        foreach (int roll
            in volley.hitRolls)
        {
            bool success = false;

            if (roll != 1 &&
                (volley.minimumUnmodifiedHit <= 0 ||
                 roll >= volley.minimumUnmodifiedHit))
            {
                int modified =
                    roll +
                    volley.hitRollModifier;

                success =
                    roll == 6 ||
                    modified >=
                        volley.skill;
            }

            bool critical =
                WarboardV47FactionRules.IsCriticalHit(
                    attacker,
                    roll,
                    success
                );

            bool precision =
                volley.precision ||
                (volley.precisionOnCriticalHit &&
                 critical);

            WarboardAttackDieLedger47.RecordHit(
                game,
                attacker,
                target,
                sourceModel,
                volley.weapon,
                mode,
                roll,
                success,
                critical,
                false,
                precision,
                critical && volley.lethalHits,
                critical
                ? volley.sustainedHits
                : 0
            );

            if (!success)
                continue;

            hits++;

            if (!critical)
                continue;

            if (precision)
                precisionCriticalHits++;

            if (volley.lethalHits)
            {
                lethal++;

                if (precision)
                    precisionLethal++;
            }

            if (volley.sustainedHits > 0)
            {
                hits +=
                    volley.sustainedHits;
            }
        }

        volley.hits = hits;
        volley.lethalAutoWounds = lethal;
        volley.precisionCriticalHits =
            precisionCriticalHits;
        volley.precisionLethalAutoWounds =
            precisionLethal;
    }
'@

        $range = Find-MethodRange -Text $text -Signature "private void RecalculateHitResults()"
        if ($null -eq $range) { throw "Could not locate RecalculateHitResults()." }
        $text = $text.Remove($range.Start, $range.Length).Insert($range.Start, $replacement)
        Write-Host "[FIXED] interactive per-hit critical provenance" -ForegroundColor Green

        $old = @'
        volley.woundRolls.Clear();
'@

        $new = @'
        volley.woundRolls.Clear();
        volley.woundPrecisionFlags.Clear();
'@

        $text = Replace-InMethod -Text $text -Signature "private void RollWounds()" -Old $old -New $new -Label "interactive wound provenance reset"

        $old = @'
            volley.normalWounds =
                volley.lethalAutoWounds;

            volley.devastatingWounds = 0;
'@

        $new = @'
            volley.normalWounds =
                volley.lethalAutoWounds;

            volley.devastatingWounds = 0;

            volley.precisionNormalWounds =
                volley.precision
                ? volley.lethalAutoWounds
                : volley.precisionLethalAutoWounds;

            volley.precisionDevastatingWounds = 0;
'@

        $text = Replace-InMethod -Text $text -Signature "private void RollWounds()" -Old $old -New $new -Label "interactive lethal-hit Precision provenance"

        $old = @'
        volley.woundRolls.AddRange(
            record.Results
        );
'@

        $new = @'
        volley.woundRolls.AddRange(
            record.Results
        );

        int v47PrecisionWoundDice =
            volley.precision
            ? volley.woundRolls.Count
            : Mathf.Max(
                0,
                volley.precisionCriticalHits -
                volley.precisionLethalAutoWounds
            );

        for (int i = 0;
             i < volley.woundRolls.Count;
             i++)
        {
            volley.woundPrecisionFlags.Add(
                volley.precision ||
                i < v47PrecisionWoundDice
            );
        }
'@

        $text = Replace-InMethod -Text $text -Signature "private void RollWounds()" -Old $old -New $new -Label "interactive Critical Hit to wound provenance"

        $replacement = @'
    private void RecalculateWoundResults()
    {
        // WARBOARD_V47_RECALCULATE_WOUNDS
        InteractiveWeaponVolley volley =
            CurrentVolley;

        WarboardAttackDieLedger47.ClearAttackStage(
            attacker,
            target,
            volley.weapon,
            WarboardAttackDieStage47.Wound
        );

        int normal =
            volley.lethalAutoWounds;

        int devastating = 0;

        int precisionNormal =
            volley.precision
            ? volley.lethalAutoWounds
            : volley.precisionLethalAutoWounds;

        int precisionDevastating = 0;

        ModelToken sourceModel =
            volley.selections.Count > 0
            ? volley.selections[0].model
            : null;

        for (int i = 0;
             i < volley.woundRolls.Count;
             i++)
        {
            int roll =
                volley.woundRolls[i];

            bool critical =
                roll >=
                    volley.criticalWoundThreshold;

            int modified =
                roll +
                volley.woundRollModifier;

            bool success =
                roll != 1 &&
                (critical ||
                 roll == 6 ||
                 modified >=
                    volley.woundTarget);

            bool precision =
                volley.precision ||
                (i < volley.woundPrecisionFlags.Count &&
                 volley.woundPrecisionFlags[i]);

            bool isDevastating =
                success &&
                critical &&
                volley.devastating;

            WarboardAttackDieLedger47.RecordWound(
                game,
                attacker,
                target,
                sourceModel,
                volley.weapon,
                mode,
                roll,
                success,
                critical,
                false,
                precision,
                isDevastating
            );

            if (!success)
                continue;

            if (isDevastating)
            {
                devastating++;

                if (precision)
                    precisionDevastating++;
            }
            else
            {
                normal++;

                if (precision)
                    precisionNormal++;
            }
        }

        volley.normalWounds = normal;
        volley.devastatingWounds = devastating;
        volley.precisionNormalWounds = precisionNormal;
        volley.precisionDevastatingWounds =
            precisionDevastating;
    }
'@

        $range = Find-MethodRange -Text $text -Signature "private void RecalculateWoundResults()"
        if ($null -eq $range) { throw "Could not locate RecalculateWoundResults()." }
        $text = $text.Remove($range.Start, $range.Length).Insert($range.Start, $replacement)
        Write-Host "[FIXED] interactive per-wound Precision provenance" -ForegroundColor Green

        $replacement = @'
    private int CalculateSaveTarget47(
        bool precision,
        out bool usesInvulnerable)
    {
        InteractiveWeaponVolley volley =
            CurrentVolley;

        ModelToken allocated =
            GetAllocationModel(
                precision
            );

        if (allocated == null)
        {
            usesInvulnerable = false;
            return 7;
        }

        SquadController owner =
            allocated.Squad;

        SquadController attackOwner =
            volley.selections.Count > 0 &&
            volley.selections[0].model != null
            ? volley.selections[0].model.Squad
            : attacker;

        int armourSave =
            Mathf.Clamp(
                owner.GetSave(
                    attackOwner
                ) -
                volley.effectiveAp,
                2,
                7
            );

        int invulnerable =
            allocated.InvulnerableSave;

        if (game != null)
        {
            int aeldariInvulnerable =
                game.AeldariInvulnerableOverride(
                    owner
                );

            int standardInvulnerable =
                WarboardFactionExtensionHub
                    .InvulnerableOverride(
                        game,
                        owner
                    );

            if (standardInvulnerable > 0 &&
                (invulnerable <= 0 ||
                 standardInvulnerable <
                    invulnerable))
            {
                invulnerable =
                    standardInvulnerable;
            }

            if (aeldariInvulnerable > 0)
            {
                invulnerable =
                    invulnerable > 0
                    ? Mathf.Min(
                        invulnerable,
                        aeldariInvulnerable
                      )
                    : aeldariInvulnerable;
            }
        }

        if (invulnerable > 0 &&
            invulnerable < armourSave)
        {
            usesInvulnerable = true;
            return invulnerable;
        }

        usesInvulnerable = false;
        return armourSave;
    }

    private void PrepareSaveTarget()
    {
        // WARBOARD_V47_PREPARE_SAVE_TARGET
        InteractiveWeaponVolley volley =
            CurrentVolley;

        bool usesInvulnerable;

        volley.saveTarget =
            CalculateSaveTarget47(
                volley.precision,
                out usesInvulnerable
            );

        volley.saveUsesInvulnerable =
            usesInvulnerable;
    }
'@

        $range = Find-MethodRange -Text $text -Signature "private void PrepareSaveTarget()"
        if ($null -eq $range) { throw "Could not locate PrepareSaveTarget()." }
        $text = $text.Remove($range.Start, $range.Length).Insert($range.Start, $replacement)
        Write-Host "[FIXED] per-event Precision-aware save target" -ForegroundColor Green

        $replacement = @'
    private void RollSaves()
    {
        // WARBOARD_V47_ROLL_SAVES_PER_DIE
        InteractiveWeaponVolley volley =
            CurrentVolley;

        volley.saveRolls.Clear();
        volley.saveTargetsPerDie.Clear();
        volley.savePrecisionFlags.Clear();
        volley.failedSavePrecisionFlags.Clear();

        if (volley.normalWounds <= 0)
        {
            volley.failedSaves = 0;

            lastActionText =
                "No normal saves required.";

            stage =
                InteractiveAttackStage.ReviewSaves;

            return;
        }

        for (int i = 0;
             i < volley.normalWounds;
             i++)
        {
            bool precision =
                volley.precision ||
                i < volley.precisionNormalWounds;

            bool usesInvulnerable;
            int targetNumber =
                CalculateSaveTarget47(
                    precision,
                    out usesInvulnerable
                );

            int roll =
                DiceRoller.RollD6(
                    "Save roll: " +
                    target.DisplayName
                );

            volley.saveRolls.Add(roll);
            volley.saveTargetsPerDie.Add(
                targetNumber);
            volley.savePrecisionFlags.Add(
                precision);

            if (i == 0)
            {
                volley.saveTarget =
                    targetNumber;
                volley.saveUsesInvulnerable =
                    usesInvulnerable;
            }
        }

        RecalculateSaveResults();

        lastActionText =
            volley.failedSaves +
            " failed save(s).";

        stage =
            InteractiveAttackStage.ReviewSaves;
    }
'@

        $range = Find-MethodRange -Text $text -Signature "private void RollSaves()"
        if ($null -eq $range) { throw "Could not locate RollSaves()." }
        $text = $text.Remove($range.Start, $range.Length).Insert($range.Start, $replacement)
        Write-Host "[FIXED] interactive per-die mixed Precision saves" -ForegroundColor Green

        $replacement = @'
    private void RecalculateSaveResults()
    {
        // WARBOARD_V47_RECALCULATE_SAVES
        InteractiveWeaponVolley volley =
            CurrentVolley;

        int failed = 0;
        volley.failedSavePrecisionFlags.Clear();

        for (int i = 0;
             i < volley.saveRolls.Count;
             i++)
        {
            int targetNumber =
                i < volley.saveTargetsPerDie.Count
                ? volley.saveTargetsPerDie[i]
                : volley.saveTarget;

            if (volley.saveRolls[i] >=
                targetNumber)
            {
                continue;
            }

            failed++;

            volley.failedSavePrecisionFlags.Add(
                i < volley.savePrecisionFlags.Count &&
                volley.savePrecisionFlags[i]
            );
        }

        volley.failedSaves = failed;
    }
'@

        $range = Find-MethodRange -Text $text -Signature "private void RecalculateSaveResults()"
        if ($null -eq $range) { throw "Could not locate RecalculateSaveResults()." }
        $text = $text.Remove($range.Start, $range.Length).Insert($range.Start, $replacement)
        Write-Host "[FIXED] interactive failed-save Precision provenance" -ForegroundColor Green

        $old = @'
        for (int i = 0;
             i < normalEvents;
             i++)
        {
            if (damageIndex >=
                volley.damageValues.Count)
            {
                break;
            }

            ModelToken allocated =
                GetAllocationModel(
                    volley.precision
                );
'@

        $new = @'
        for (int i = 0;
             i < normalEvents;
             i++)
        {
            if (damageIndex >=
                volley.damageValues.Count)
            {
                break;
            }

            bool v47NormalPrecision =
                volley.precision ||
                (i < volley.failedSavePrecisionFlags.Count &&
                 volley.failedSavePrecisionFlags[i]);

            ModelToken allocated =
                GetAllocationModel(
                    v47NormalPrecision
                );
'@

        $text = Replace-InMethod -Text $text -Signature "private void ApplyDamage()" -Old $old -New $new -Label "interactive normal-damage Precision allocation"

        $old = @'
        for (int i = 0;
             i < devEvents;
             i++)
        {
            if (damageIndex >=
                volley.damageValues.Count)
            {
                break;
            }

            ModelToken allocated =
                GetAllocationModel(
                    volley.precision
                );
'@

        $new = @'
        for (int i = 0;
             i < devEvents;
             i++)
        {
            if (damageIndex >=
                volley.damageValues.Count)
            {
                break;
            }

            bool v47DevastatingPrecision =
                volley.precision ||
                i < volley.precisionDevastatingWounds;

            ModelToken allocated =
                GetAllocationModel(
                    v47DevastatingPrecision
                );
'@

        $text = Replace-InMethod -Text $text -Signature "private void ApplyDamage()" -Old $old -New $new -Label "interactive devastating-damage Precision allocation"

        $old = @'
                return LowestFailedIndex(
                    volley.saveRolls,
                    volley.saveTarget
                );
'@

        $new = @'
                int bestSaveIndex = -1;
                int bestSaveRoll = int.MaxValue;

                for (int i = 0;
                     i < volley.saveRolls.Count;
                     i++)
                {
                    int targetNumber =
                        i < volley.saveTargetsPerDie.Count
                        ? volley.saveTargetsPerDie[i]
                        : volley.saveTarget;

                    int roll =
                        volley.saveRolls[i];

                    if (roll < targetNumber &&
                        roll < bestSaveRoll)
                    {
                        bestSaveRoll = roll;
                        bestSaveIndex = i;
                    }
                }

                return bestSaveIndex;
'@

        $text = Replace-InMethod -Text $text -Signature "private int FindRerollableDieIndex()" -Old $old -New $new -Label "mixed-save Command Re-roll selection"

        return $text
    }

    # ------------------------------------------------------------------
    # HIDDEN / DETECTION RANGE: consume generic detected/cloaked state
    # ------------------------------------------------------------------
    Stage-Patch "Assets\Scripts\Core\GameController.CoreCompletion11.cs" {
        param($text)

        $old = @'
        float detectionRange =
            CoreRules11Terrain.HiddenDetectionRange +
            CustodesFactionPack11.DetectionRangeBonus(
                target.Squad != null
                    ? target.Squad.JoinedActionController()
                    : null) +
            NecronsFactionPack11.DetectionRangeBonus(
                target.Squad != null
                    ? target.Squad.JoinedActionController()
                    : null);
'@

        $new = @'
        float detectionRange =
            CoreRules11Terrain.HiddenDetectionRange +
            CustodesFactionPack11.DetectionRangeBonus(
                target.Squad != null
                    ? target.Squad.JoinedActionController()
                    : null) +
            NecronsFactionPack11.DetectionRangeBonus(
                target.Squad != null
                    ? target.Squad.JoinedActionController()
                    : null) +
            // WARBOARD_V47_DETECTION_RANGE_STATE
            WarboardFactionExtensionHub.DetectionRangeModifier(
                target.Squad != null
                    ? target.Squad.JoinedActionController()
                    : null);
'@

        $text = Replace-InMethod -Text $text -Signature "private bool Core11CanSeeModel(" -Old $old -New $new -Label "generic detection-range target state"

        return $text
    }

    # ------------------------------------------------------------------
    # VERSION STAGE
    # ------------------------------------------------------------------
    Stage-Patch "Assets\Scripts\Core\WarboardBuildInfo.cs" {
        param($text)

        $regex = New-Object System.Text.RegularExpressions.Regex(
            'CurrentVersion\s*=\s*"v[^"]+"'
        )

        if ($regex.Matches($text).Count -ne 1) {
            throw "Could not uniquely locate WarboardBuildInfo.CurrentVersion."
        }

        return $regex.Replace(
            $text,
            'CurrentVersion = "v47"',
            1
        )
    }

    # ------------------------------------------------------------------
    # VALIDATE DIRECT PAYLOAD BEFORE COMMIT
    # ------------------------------------------------------------------
    $payloadFiles = Get-ChildItem -LiteralPath $Payload -File -Recurse

    if ($payloadFiles.Count -lt 10) {
        throw "v47 payload is incomplete."
    }

    foreach ($file in $payloadFiles) {
        $relative = $file.FullName.Substring($Payload.Length).TrimStart([char]92)

        if ($file.Extension -eq ".cs") {
            $content = [System.IO.File]::ReadAllText($file.FullName)

            if ($content.Contains([char]0xFFFD)) {
                throw "Unicode replacement-character corruption detected in payload: $relative"
            }
        }
    }

    Write-Host ""
    Write-Host "All v47 source transformations validated in staging." -ForegroundColor Cyan
    Write-Host "Committing v47 payload..." -ForegroundColor Cyan

    $commitStarted = $true

    # Back up every staged core source before replacing it.
    Get-ChildItem -LiteralPath $StageRoot -File -Recurse | ForEach-Object {
        $relative = $_.FullName.Substring($StageRoot.Length).TrimStart([char]92)
        Backup-Once $relative
    }

    # Back up direct payload replacements; remember genuinely new files.
    foreach ($file in $payloadFiles) {
        $relative = $file.FullName.Substring($Payload.Length).TrimStart([char]92)
        $destination = Join-Path $Root $relative

        if (Test-Path $destination) {
            Backup-Once $relative
        }
        else {
            $newFiles.Add($relative)
        }
    }

    # Commit direct payload first. It includes the new generic v47 runtime and
    # the replacement standard-faction hub/controller/UI files.
    foreach ($file in $payloadFiles) {
        $relative = $file.FullName.Substring($Payload.Length).TrimStart([char]92)
        $destination = Join-Path $Root $relative
        $parent = Split-Path $destination -Parent
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
    }

    # Commit staged baseline patches last.
    Get-ChildItem -LiteralPath $StageRoot -File -Recurse | ForEach-Object {
        $relative = $_.FullName.Substring($StageRoot.Length).TrimStart([char]92)
        $destination = Join-Path $Root $relative
        $parent = Split-Path $destination -Parent
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
    }

    Remove-Item -LiteralPath $StageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Write-Host ""
    Write-Host "WARBOARD v47 INSTALLED." -ForegroundColor Green
    Write-Host ""
    Write-Host "Rules-engine expansion:" -ForegroundColor Cyan
    Write-Host "  - persistent generic unit/target/objective rule state"
    Write-Host "  - arbitrary physical faction markers + legal placement"
    Write-Host "  - enhancement bearer assignment + passive bearer effects"
    Write-Host "  - per-critical-hit attack provenance / Precision allocation"
    Write-Host "  - rich faction reaction/event bus"
    Write-Host "  - generic special reposition/endpoint legality engine"
    Write-Host "  - generic datasheet/Stratagem choice state"
    Write-Host "  - Tyranid Tunnel Markers + Tunnel Network"
    Write-Host "  - Bastion auspex scan / pin / suppress / Heresy Undone"
    Write-Host "  - Subversion detection / Cloaked Position state"
    Write-Host ""
    Write-Host "Return to Unity and let it compile/import." -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "V47 INSTALL FAILED" -ForegroundColor Red
    Write-Host "------------------" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkRed

    if ($commitStarted) {
        Write-Host ""
        Write-Host "Rolling v47 files back..." -ForegroundColor Yellow

        foreach ($relative in $backedUp) {
            $backup = Join-Path $BackupRoot $relative
            $destination = Join-Path $Root $relative

            if (Test-Path $backup) {
                $parent = Split-Path $destination -Parent
                New-Item -ItemType Directory -Force -Path $parent | Out-Null
                Copy-Item -LiteralPath $backup -Destination $destination -Force
            }
        }

        foreach ($relative in $newFiles) {
            $destination = Join-Path $Root $relative
            Remove-Item -LiteralPath $destination -Force -ErrorAction SilentlyContinue
        }

        Write-Host "Rollback complete." -ForegroundColor Yellow
    }

    Remove-Item -LiteralPath $StageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Write-Host ""
    exit 1
}
