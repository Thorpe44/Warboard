$ErrorActionPreference = "Stop"

try {
    Set-Location $PSScriptRoot

    Write-Host ""
    Write-Host "WARBOARD v46e TRADITIONAL BATTLE-SHOCK FIX - ORKS / TYRANIDS / SPACE MARINES" -ForegroundColor Cyan
    Write-Host "============================================================"
    Write-Host ""

    $Root = $PSScriptRoot
    $Payload = Join-Path $Root "WARBOARD_V46_PAYLOAD"
    $Core = Join-Path $Root "Assets\Scripts\Core"
    $BackupRoot =
        Join-Path $Root "Library\WarboardBackups\V46ThreeFactions"
    $StageRoot =
        Join-Path $Root "Library\WarboardV46Staging"
    $PayloadCopied = $false

    if (-not (Test-Path (Join-Path $Root "Assets")) -or
        -not (Test-Path (Join-Path $Root "ProjectSettings")))
    {
        throw "This is not the Warboard Unity project root. Extract the ZIP directly over the main Warboard folder."
    }

    if (-not (Test-Path $Payload)) {
        throw "WARBOARD_V46_PAYLOAD is missing."
    }

    New-Item -ItemType Directory -Force -Path $BackupRoot |
        Out-Null

    if (Test-Path $StageRoot) {
        Remove-Item `
            -LiteralPath $StageRoot `
            -Recurse `
            -Force
    }

    New-Item -ItemType Directory -Force -Path $StageRoot |
        Out-Null

    function Read-Utf8([string]$Path) {
        return [System.IO.File]::ReadAllText($Path)
    }

    function Write-Utf8(
        [string]$Path,
        [string]$Text)
    {
        $parent = Split-Path $Path -Parent

        if (-not (Test-Path $parent)) {
            New-Item -ItemType Directory -Force -Path $parent |
                Out-Null
        }

        [System.IO.File]::WriteAllText(
            $Path,
            $Text,
            [System.Text.UTF8Encoding]::new($false)
        )
    }

    function Backup-Once([string]$Path) {
        if (-not (Test-Path $Path)) {
            throw "Expected file is missing: $Path"
        }

        $relative =
            $Path.Substring($Root.Length).TrimStart([char]92)

        $dest =
            Join-Path $BackupRoot $relative

        $destParent =
            Split-Path $dest -Parent

        if (-not (Test-Path $destParent)) {
            New-Item `
                -ItemType Directory `
                -Force `
                -Path $destParent |
                Out-Null
        }

        if (-not (Test-Path $dest)) {
            Copy-Item `
                -LiteralPath $Path `
                -Destination $dest `
                -Force
        }
    }

    function Replace-Exact(
        [string]$Text,
        [string]$Old,
        [string]$New,
        [string]$Label,
        [switch]$AllowAlready)
    {
        if ($Text.Contains($New)) {
            Write-Host ("[OK] " + $Label + " already installed.") -ForegroundColor DarkGreen
            return $Text
        }

        if ($Text.Contains($Old)) {
            Write-Host ("[FIXED] " + $Label) -ForegroundColor Green

            return $Text.Replace(
                $Old,
                $New
            )
        }

        # v46c: Windows working copies can differ in indentation/newline style
        # from the source used to build the patch. Fall back to a flexible
        # whitespace match, but ONLY when that match is unique.
        $trimmedOld = $Old.Trim()

        $parts =
            [System.Text.RegularExpressions.Regex]::Split(
                $trimmedOld,
                '\s+'
            )

        $escaped =
            New-Object System.Collections.Generic.List[string]

        foreach ($part in $parts) {
            if (-not [string]::IsNullOrWhiteSpace($part)) {
                $escaped.Add(
                    [System.Text.RegularExpressions.Regex]::Escape(
                        $part
                    )
                )
            }
        }

        $flexPattern =
            [string]::Join(
                '\s+',
                $escaped.ToArray()
            )

        $flexRegex =
            New-Object System.Text.RegularExpressions.Regex(
                $flexPattern,
                [System.Text.RegularExpressions.RegexOptions]::Singleline
            )

        $matches =
            $flexRegex.Matches($Text)

        if ($matches.Count -eq 1) {
            $match = $matches[0]

            Write-Host ("[FIXED] " + $Label + " (flexible anchor)") -ForegroundColor Green

            $result =
                $Text.Substring(
                    0,
                    $match.Index
                ) +
                $New +
                $Text.Substring(
                    $match.Index +
                    $match.Length
                )

            return $result
        }

        if ($matches.Count -gt 1) {
            throw "Ambiguous anchor for: $Label (" + $matches.Count + " flexible matches)"
        }

        if ($AllowAlready) {
            Write-Host ("[WARN] " + $Label + " anchor not found; leaving file unchanged.") -ForegroundColor Yellow
            return $Text
        }

        throw "Could not find anchor for: $Label"
    }

    function Replace-RegexOnce(
        [string]$Text,
        [string]$Pattern,
        [string]$Replacement,
        [string]$Label,
        [switch]$AllowAlready)
    {
        $rx =
            New-Object System.Text.RegularExpressions.Regex(
                $Pattern,
                [System.Text.RegularExpressions.RegexOptions]::Singleline
            )

        $matches = $rx.Matches($Text)

        if ($matches.Count -eq 0) {
            if ($AllowAlready) {
                Write-Host ("[WARN] " + $Label + " pattern not found; leaving file unchanged.") -ForegroundColor Yellow
                return $Text
            }

            throw "Could not find pattern for: $Label"
        }

        Write-Host ("[FIXED] " + $Label) -ForegroundColor Green

        return $rx.Replace(
            $Text,
            $Replacement,
            1
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
            End = $closeBrace + 1
            Length = $closeBrace + 1 - $lineStart
        }
    }

    function Replace-InMethod(
        [string]$Text,
        [string]$Signature,
        [string]$Old,
        [string]$New,
        [string]$Label,
        [switch]$AllowAlready)
    {
        $range =
            Find-MethodRange `
                -Text $Text `
                -Signature $Signature

        if ($null -eq $range) {
            throw "Could not locate method for: $Label ($Signature)"
        }

        $method =
            $Text.Substring(
                $range.Start,
                $range.Length
            )

        if ($method.Contains($New)) {
            Write-Host ("[OK] " + $Label + " already installed.") -ForegroundColor DarkGreen
            return $Text
        }

        if (-not $method.Contains($Old)) {
            if ($AllowAlready) {
                Write-Host ("[WARN] " + $Label + " method anchor not found.") -ForegroundColor Yellow
                return $Text
            }

            throw "Could not locate method anchor for: $Label"
        }

        $method =
            $method.Replace(
                $Old,
                $New
            )

        Write-Host ("[FIXED] " + $Label) -ForegroundColor Green

        return $Text.Remove(
            $range.Start,
            $range.Length
        ).Insert(
            $range.Start,
            $method
        )
    }

    function Insert-AfterMethodOpen(
        [string]$Text,
        [string]$Signature,
        [string]$Insertion,
        [string]$Marker,
        [string]$Label)
    {
        if ($Text.Contains($Marker)) {
            Write-Host ("[OK] " + $Label + " already installed.") -ForegroundColor DarkGreen
            return $Text
        }

        $range =
            Find-MethodRange `
                -Text $Text `
                -Signature $Signature

        if ($null -eq $range) {
            throw "Could not locate method for: $Label"
        }

        Write-Host ("[FIXED] " + $Label) -ForegroundColor Green

        return $Text.Insert(
            $range.OpenBrace + 1,
            $Insertion
        )
    }

    function Insert-BeforeMethodClose(
        [string]$Text,
        [string]$Signature,
        [string]$Insertion,
        [string]$Marker,
        [string]$Label)
    {
        if ($Text.Contains($Marker)) {
            Write-Host ("[OK] " + $Label + " already installed.") -ForegroundColor DarkGreen
            return $Text
        }

        $range =
            Find-MethodRange `
                -Text $Text `
                -Signature $Signature

        if ($null -eq $range) {
            throw "Could not locate method for: $Label"
        }

        Write-Host ("[FIXED] " + $Label) -ForegroundColor Green

        return $Text.Insert(
            $range.CloseBrace,
            $Insertion
        )
    }

    function Patch-File(
        [string]$Relative,
        [scriptblock]$Patcher)
    {
        $path =
            Join-Path $Root $Relative

        Backup-Once $path

        $text =
            Read-Utf8 $path

        $patched =
            & $Patcher $text

        if ($null -eq $patched) {
            throw "Patcher returned null for: $Relative"
        }

        if (-not ($patched -is [string])) {
            $patched =
                [string]$patched
        }

        $staged =
            Join-Path $StageRoot $Relative

        Write-Utf8 $staged $patched
    }

    # ---------------------------------------------------------------------
    # v46d PATCH ENGINE SELF-TEST
    # Runs before any project source is staged.
    # ---------------------------------------------------------------------
    $selfTestSource = @'
alpha
    beta
gamma
'@

    $selfTestOld = @'
alpha
beta
'@

    $selfTestNew = @'
alpha
PATCHED
beta
'@

    $selfTestExact =
        Replace-Exact `
            -Text $selfTestSource `
            -Old $selfTestOld `
            -New $selfTestNew `
            -Label "installer flexible-anchor self-test"

    if ([string]::IsNullOrWhiteSpace(
            $selfTestExact) -or
        -not $selfTestExact.Contains(
            "PATCHED"))
    {
        throw "Installer self-test failed: flexible Replace-Exact returned an invalid result."
    }

    $selfTestRegex =
        Replace-RegexOnce `
            -Text "one   two" `
            -Pattern 'one\s+two' `
            -Replacement "ONE TWO" `
            -Label "installer regex self-test"

    if ($selfTestRegex -ne "ONE TWO") {
        throw "Installer self-test failed: Replace-RegexOnce returned an invalid result."
    }

    Write-Host "[OK] v46d patch engine self-test passed." -ForegroundColor Cyan
    Write-Host ""

    # ---------------------------------------------------------------------
    # 1. MODULAR CONTROLLER FACTORY
    # ---------------------------------------------------------------------
    Patch-File "Assets\Scripts\Core\FactionControllerSystem.cs" {
        param($text)

        $old = @'
        return new GenericFactionGameController();
'@

        $new = @'
        // WARBOARD_V46_STANDARD_FACTION_FACTORY
        IFactionGameController extension =
            WarboardFactionExtensionHub
                .TryCreateController(
                    army
                );

        if (extension != null)
            return extension;

        return new GenericFactionGameController();
'@

        return Replace-Exact `
            -Text $text `
            -Old $old `
            -New $new `
            -Label "shared post-v45 faction-controller factory"
    }

    # ---------------------------------------------------------------------
    # 2. UNIVERSAL RULE ENGINE
    # ---------------------------------------------------------------------
    Patch-File "Assets\Scripts\Core\UniversalRuleEngine.cs" {
        param($text)

        if (-not $text.Contains(
                "WARBOARD_V46_STANDARD_CORE_ABILITIES"))
        {
            $old = @'
        if (NecronsFactionPack11.GrantsCoreAbility(
                squad, ruleName))
        {
            return true;
        }
'@

            $new = @'
        if (NecronsFactionPack11.GrantsCoreAbility(
                squad, ruleName))
        {
            return true;
        }

        // WARBOARD_V46_STANDARD_CORE_ABILITIES
        if (WarboardFactionExtensionHub
            .GrantsCoreAbility(
                squad,
                ruleName))
        {
            return true;
        }
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard faction granted core abilities"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_STANDARD_FNP"))
        {
            $old = @'
        fnp =
            CustodesFactionPack11.ConditionalFeelNoPain(
                squad, label, fnp);
'@

            $new = @'
        fnp =
            CustodesFactionPack11.ConditionalFeelNoPain(
                squad, label, fnp);

        // WARBOARD_V46_STANDARD_FNP
        fnp =
            WarboardFactionExtensionHub
                .ConditionalFeelNoPain(
                    squad,
                    label,
                    fnp
                );
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard faction conditional Feel No Pain"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_FINAL_ATTACK_STATE"))
        {
            $old = @'
        CustodesFactionPack11.ApplyAttackModifiers(
            game, attacker, target, shooter, weapon, mode, state);
'@

            $new = @'
        CustodesFactionPack11.ApplyAttackModifiers(
            game, attacker, target, shooter, weapon, mode, state);

        // WARBOARD_V46_FINAL_ATTACK_STATE
        WarboardFactionExtensionHub
            .FinalizeAttackState(
                attacker,
                state
            );
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "final standard attack-state normalization"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_GRANTED_HEAVY"))
        {
            $old = @'
            if (mode != AttackMode.Ranged ||
                !WeaponRuleParser.Has(
                    weapon,
                    "heavy") ||
                game == null ||
'@

            $new = @'
            if (mode != AttackMode.Ranged ||
                // WARBOARD_V46_GRANTED_HEAVY
                (!WeaponRuleParser.Has(
                    weapon,
                    "heavy") &&
                 !WarboardFactionExtensionHub
                    .GrantsHeavy(
                        attacker,
                        weapon,
                        mode)) ||
                game == null ||
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "granted Heavy ability in universal attack state"
        }

        return $text
    }

    # ---------------------------------------------------------------------
    # 2B. STANDARD FACTION STRATAGEM MENU BRIDGE
    # ---------------------------------------------------------------------
    Patch-File "Assets\Scripts\Core\GameController.UI.cs" {
        param($text)

        if (-not $text.Contains(
                "WARBOARD_V46_STANDARD_STRATAGEM_MENU"))
        {
            $old = @'
        else if (NecronsFactionPack11Runtime.Controller(activeFaction) != null)
        {
            DrawNecrons11StratagemCards(
                left, right, y, cardWidth);
        }
        else if (isNecrons)
'@

            $new = @'
        else if (NecronsFactionPack11Runtime.Controller(activeFaction) != null)
        {
            DrawNecrons11StratagemCards(
                left, right, y, cardWidth);
        }
        // WARBOARD_V46_STANDARD_STRATAGEM_MENU
        else if (WarboardFactionExtensionHub
                    .ControllerFor(
                        activeFaction) != null)
        {
            DrawStandardFactionStratagemCards(
                left,
                right,
                y,
                cardWidth
            );
        }
        else if (isNecrons)
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard faction cards in the main Stratagem menu"
        }

        return $text
    }

    # ---------------------------------------------------------------------
    # 3. COMBAT ENTRY / SHOOTING ELIGIBILITY / POST-ATTACK RULES
    # ---------------------------------------------------------------------
    Patch-File "Assets\Scripts\Core\GameController.Combat.cs" {
        param($text)

        if (-not $text.Contains(
                "WARBOARD_V46_STANDARD_ATTACK_HOOK"))
        {
            $old = @'
        NecronsFactionPack11.ApplyAttackModifiers(
            this, attacker, target, null, weapon, attackMode, state);
'@

            $new = @'
        NecronsFactionPack11.ApplyAttackModifiers(
            this, attacker, target, null, weapon, attackMode, state);

        // WARBOARD_V46_STANDARD_ATTACK_HOOK
        WarboardFactionExtensionHub
            .ApplyAttackModifiers(
                this,
                attacker,
                target,
                null,
                weapon,
                attackMode,
                state
            );
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "shared standard faction attack modifier hook"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_STANDARD_FALLBACK_SHOOT"))
        {
            $old = @'
            !Custodes11CanShootAfterFallBack(attacker) &&
            !(aeldariRules != null &&
'@

            $new = @'
            !Custodes11CanShootAfterFallBack(attacker) &&
            // WARBOARD_V46_STANDARD_FALLBACK_SHOOT
            !WarboardFactionExtensionHub
                .CanShootAfterFallBack(
                    attacker) &&
            !(aeldariRules != null &&
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard faction shoot-after-Fall-Back eligibility"
        }

        # Fix the indirect-fire Advance legality before adding the generic
        # direct-weapon Advance hook.
        if (-not $text.Contains(
                "WARBOARD_V46_INDIRECT_ADVANCE_LEGAL"))
        {
            $pattern =
                '!attacker\.HasAdvanced\s*&&\s*!Necrons11CanShootAfterAdvance\(attacker\)\s*&&\s*!Custodes11CanShootAfterAdvance\(attacker\)\s*&&\s*!targetEngaged'

            $replacement = @'
(
                            // WARBOARD_V46_INDIRECT_ADVANCE_LEGAL
                            !attacker.HasAdvanced ||
                            Necrons11CanShootAfterAdvance(attacker) ||
                            Custodes11CanShootAfterAdvance(attacker) ||
                            WarboardFactionExtensionHub
                                .CanShootAfterAdvance(attacker) ||
                            attacker
                                .JoinedActionController()
                                .SnapShootingActive ||
                            WeaponRuleParser.Has(
                                weapon,
                                "assault")
                        ) &&
                        !targetEngaged
'@

            $text =
                Replace-RegexOnce `
                    -Text $text `
                    -Pattern $pattern `
                    -Replacement $replacement `
                    -Label "indirect-fire Advance legality"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_STANDARD_ADVANCE_SHOOT"))
        {
            $old = @'
                    !Custodes11CanShootAfterAdvance(attacker) &&
                    !attacker
'@

            $new = @'
                    !Custodes11CanShootAfterAdvance(attacker) &&
                    // WARBOARD_V46_STANDARD_ADVANCE_SHOOT
                    !WarboardFactionExtensionHub
                        .CanShootAfterAdvance(
                            attacker) &&
                    !attacker
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard faction shoot-after-Advance eligibility"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_STANDARD_POST_ATTACK"))
        {
            $old = @'
        if (resolvedAttack.Mode ==
            AttackMode.Ranged)
        {
            postAttackFlowQueue.Enqueue(
'@

            $new = @'
        if (resolvedAttack.Mode ==
            AttackMode.Ranged)
        {
            // WARBOARD_V46_STANDARD_POST_ATTACK
            postAttackFlowQueue.Enqueue(
                () =>
                {
                    if (!StandardOfferPostAttackReaction(
                            resolvedAttack,
                            ContinuePostAttackFlow))
                    {
                        ContinuePostAttackFlow();
                    }
                }
            );

            postAttackFlowQueue.Enqueue(
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard faction post-ranged-attack reaction flow"
        }

        return $text
    }

    # ---------------------------------------------------------------------
    # 4. CHARGE PERMISSIONS / MODIFIERS / OPTIONAL FACTION RE-ROLLS
    # ---------------------------------------------------------------------
    Patch-File "Assets\Scripts\Core\GameController.Charge.cs" {
        param($text)

        if (-not $text.Contains(
                "WARBOARD_V46_SELECTED_CHARGE_PERMISSIONS"))
        {
            $old = @'
        bool canChargeAfterFallBack =
            aeldariRules != null &&
            aeldariRules.CanChargeAfterFallBack(
                selectedSquad
            );

        bool canChargeAfterAdvance =
            aeldariRules != null &&
            aeldariRules.CanChargeAfterAdvance(
                selectedSquad
            );
'@

            $new = @'
        // WARBOARD_V46_SELECTED_CHARGE_PERMISSIONS
        bool canChargeAfterFallBack =
            Necrons11CanChargeAfterFallBack(
                selectedSquad) ||
            Custodes11CanChargeAfterFallBack(
                selectedSquad) ||
            WarboardFactionExtensionHub
                .CanChargeAfterFallBack(
                    selectedSquad) ||
            (aeldariRules != null &&
             aeldariRules.CanChargeAfterFallBack(
                 selectedSquad
             ));

        bool canChargeAfterAdvance =
            Necrons11CanChargeAfterAdvance(
                selectedSquad) ||
            Custodes11CanChargeAfterAdvance(
                selectedSquad) ||
            WarboardFactionExtensionHub
                .CanChargeAfterAdvance(
                    selectedSquad) ||
            (aeldariRules != null &&
             aeldariRules.CanChargeAfterAdvance(
                 selectedSquad
             ));
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "selected-unit charge permissions"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_STANDARD_FALLBACK_CHARGE"))
        {
            $old = @'
            !Custodes11CanChargeAfterFallBack(attacker) &&
            !(aeldariRules != null &&
'@

            $new = @'
            !Custodes11CanChargeAfterFallBack(attacker) &&
            // WARBOARD_V46_STANDARD_FALLBACK_CHARGE
            !WarboardFactionExtensionHub
                .CanChargeAfterFallBack(
                    attacker) &&
            !(aeldariRules != null &&
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard faction charge-after-Fall-Back eligibility"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_STANDARD_ADVANCE_CHARGE"))
        {
            $old = @'
            !Custodes11CanChargeAfterAdvance(attacker) &&
            !(aeldariRules != null &&
'@

            $new = @'
            !Custodes11CanChargeAfterAdvance(attacker) &&
            // WARBOARD_V46_STANDARD_ADVANCE_CHARGE
            !WarboardFactionExtensionHub
                .CanChargeAfterAdvance(
                    attacker) &&
            !(aeldariRules != null &&
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard faction charge-after-Advance eligibility"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_STANDARD_CHARGE_ROLL"))
        {
            $old = @'
        roll +=
            Necrons11ChargeRollModifier(
                attacker, target);

        float targetDistance =
'@

            $new = @'
        // WARBOARD_V46_STANDARD_CHARGE_ROLL
        if (StandardOfferFactionChargeReroll(
                attacker,
                target,
                roll,
                wasRerolled))
        {
            return;
        }

        roll +=
            Necrons11ChargeRollModifier(
                attacker, target);

        roll +=
            WarboardFactionExtensionHub
                .ChargeRollModifier(
                    this,
                    attacker,
                    target
                );

        float targetDistance =
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard faction Charge reroll/modifier hook"
        }

        return $text
    }

    # ---------------------------------------------------------------------
    # 5. ADVANCE + MOVEMENT MODIFIERS
    # ---------------------------------------------------------------------
    Patch-File "Assets\Scripts\Core\GameController.Movement.cs" {
        param($text)

        $insertion = @'

        // WARBOARD_V46_STANDARD_ADVANCE_RESULT
        if (unit != null)
        {
            int fixedAdvance =
                WarboardFactionExtensionHub
                    .FixedAdvanceResult(
                        unit
                    );

            if (fixedAdvance > 0)
            {
                roll = fixedAdvance;
            }
            else if (IsXcomMode)
            {
                roll +=
                    WarboardFactionExtensionHub
                        .AdvanceRollModifier(
                            this,
                            unit
                        );
            }
        }

'@

        return Insert-AfterMethodOpen `
            -Text $text `
            -Signature "private void ApplyAdvanceRoll(" `
            -Insertion $insertion `
            -Marker "WARBOARD_V46_STANDARD_ADVANCE_RESULT" `
            -Label "fixed/modified Advance results"
    }

    Patch-File "Assets\Scripts\Core\SquadController.cs" {
        param($text)

        $text =
            Replace-InMethod `
                -Text $text `
                -Signature "public float GetMove()" `
                -Old @'
        return Mathf.Max(0f, value);
'@ `
                -New @'
        // WARBOARD_V46_STANDARD_MOVE_MODIFIER
        value +=
            WarboardFactionExtensionHub
                .MoveModifier(
                    GameController.Current,
                    this
                );

        return Mathf.Max(0f, value);
'@ `
                -Label "standard faction Move characteristic modifier"

        $eventInsertion = @'

        // WARBOARD_V46_DISEMBARK_EVENT
        GameController currentGame =
            GameController.Current;

        if (currentGame != null)
        {
            GameEventBus.Raise(
                new GameEventContext
                {
                    Type =
                        GameEventType.UnitDisembarked,
                    Game = currentGame,
                    ActingFaction =
                        actionUnit.FactionId,
                    Phase =
                        currentGame.CurrentPhase,
                    Source = actionUnit,
                    Note =
                        actionUnit.DisplayName +
                        " disembarked."
                }
            );
        }

'@

        $text =
            Insert-BeforeMethodClose `
                -Text $text `
                -Signature "public void DisembarkFromTransport(" `
                -Insertion $eventInsertion `
                -Marker "WARBOARD_V46_DISEMBARK_EVENT" `
                -Label "authoritative UnitDisembarked event"

        return $text
    }

    # ---------------------------------------------------------------------
    # 6. TYRANID SYNAPSE BATTLE-SHOCK DICE
    # ---------------------------------------------------------------------
    Patch-File "Assets\Scripts\Core\GameController.Core.cs" {
        param($text)

        $phaseGate = @'

        // WARBOARD_V46_STANDARD_PHASE_GATE
        string standardFactionPhaseReason;

        if (!WarboardFactionExtensionHub
                .CanAdvancePhase(
                    this,
                    out standardFactionPhaseReason))
        {
            status = standardFactionPhaseReason;
            return;
        }

'@

        $text =
            Insert-AfterMethodOpen `
                -Text $text `
                -Signature "private void NextPhase()" `
                -Insertion $phaseGate `
                -Marker "WARBOARD_V46_STANDARD_PHASE_GATE" `
                -Label "mandatory faction-choice phase gate"

        if (-not $text.Contains(
                "WARBOARD_V46_COMMAND_BATTLESHOCK_DICE"))
        {
            $old = @'
            int roll =
                DiceRoller.Roll2D6(
                    "Battle-shock: " +
                    unit.DisplayName
                );
'@

            $new = @'
            // WARBOARD_V46_COMMAND_BATTLESHOCK_DICE
            int battleShockDice =
                WarboardFactionExtensionHub
                    .BattleShockDice(
                        this,
                        unit
                    );

            int roll =
                battleShockDice >= 3
                ? DiceRoller.RollDice(
                    battleShockDice,
                    6,
                    "Battle-shock: " +
                    unit.DisplayName
                  ).Total
                : DiceRoller.Roll2D6(
                    "Battle-shock: " +
                    unit.DisplayName
                  );
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "Synapse dice in normal Command Battle-shock"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_FORCED_BATTLESHOCK_DICE"))
        {
            $pattern =
                'int roll\s*=\s*DiceRoller\.Roll2D6\(\s*label\s*\+\s*": "\s*\+\s*target\.DisplayName\s*\);'

            $replacement = @'
        // WARBOARD_V46_FORCED_BATTLESHOCK_DICE
        int standardBattleShockDice =
            WarboardFactionExtensionHub
                .BattleShockDice(
                    this,
                    target
                );

        int roll =
            standardBattleShockDice >= 3
            ? DiceRoller.RollDice(
                standardBattleShockDice,
                6,
                label +
                ": " +
                target.DisplayName
              ).Total
            : DiceRoller.Roll2D6(
                label +
                ": " +
                target.DisplayName
              );
'@

            $text =
                Replace-RegexOnce `
                    -Text $text `
                    -Pattern $pattern `
                    -Replacement $replacement `
                    -Label "Synapse dice in externally triggered Battle-shock"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_FORCED_TRADITIONAL_BATTLESHOCK_DICE"))
        {
            $old = @'
            OpenTraditionalDicePrompt(
                2
            );

            status =
                label +
                ": " +
                target.DisplayName +
                " must take a Battle-shock test. Resolve 2D6 manually and mark PASS or FAIL.";
'@

            $new = @'
            // WARBOARD_V46_FORCED_TRADITIONAL_BATTLESHOCK_DICE
            int forcedDiceCount =
                WarboardFactionExtensionHub
                    .BattleShockDice(
                        this,
                        target
                    );

            OpenTraditionalDicePrompt(
                forcedDiceCount
            );

            status =
                label +
                ": " +
                target.DisplayName +
                " must take a Battle-shock test. Resolve " +
                forcedDiceCount +
                "D6 manually and mark PASS or FAIL.";
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "Traditional Synapse dice in triggered Battle-shock"
        }

        # Keep the shooting range guide honest for detachments that grant
        # Advance-and-shoot.
        if (-not $text.Contains(
                "WARBOARD_V46_RANGE_GUIDE_ADVANCE_SHOOT"))
        {
            $old = @'
                !selectedSquad
                    .JoinedActionController()
                    .StarEnginesActive &&
                !(aeldariRules != null &&
'@

            $new = @'
                !selectedSquad
                    .JoinedActionController()
                    .StarEnginesActive &&
                // WARBOARD_V46_RANGE_GUIDE_ADVANCE_SHOOT
                !WarboardFactionExtensionHub
                    .CanShootAfterAdvance(
                        selectedSquad) &&
                !(aeldariRules != null &&
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "shooting threat-ring Advance permissions"
        }

        return $text
    }

    Patch-File "Assets\Scripts\Core\GameController.Traditional.cs" {
        param($text)

        $range =
            Find-MethodRange `
                -Text $text `
                -Signature "private void BeginNextTraditionalBattleShock()"

        if ($null -eq $range) {
            throw "Could not locate BeginNextTraditionalBattleShock()."
        }

        $method =
            $text.Substring(
                $range.Start,
                $range.Length
            )

        if (-not $method.Contains(
                "WARBOARD_V46_TRADITIONAL_SYNAPSE"))
        {
            $old = @'
            OpenTraditionalDicePrompt(
                2
            );

            status =
                "BATTLE-SHOCK TEST REQUIRED: " +
                traditionalBattleShockUnit.DisplayName +
                ". Roll 2D6 manually, apply any tabletop rules yourself, then mark PASS or FAIL.";
'@

            $new = @'
            // WARBOARD_V46_TRADITIONAL_SYNAPSE
            int diceCount =
                WarboardFactionExtensionHub
                    .BattleShockDice(
                        this,
                        traditionalBattleShockUnit
                    );

            OpenTraditionalDicePrompt(
                diceCount
            );

            status =
                "BATTLE-SHOCK TEST REQUIRED: " +
                traditionalBattleShockUnit.DisplayName +
                ". Roll " +
                diceCount +
                "D6 manually, apply any tabletop rules yourself, then mark PASS or FAIL.";
'@

            # v46e: patch the extracted method through the same robust
            # exact/flexible engine used everywhere else. The old v46d block
            # bypassed that helper and therefore still depended on byte-for-byte
            # indentation/line-ending equality inside this one method.
            $method =
                Replace-Exact `
                    -Text $method `
                    -Old $old `
                    -New $new `
                    -Label "Traditional Synapse Battle-shock dice"

            if ([string]::IsNullOrWhiteSpace(
                    $method) -or
                -not $method.Contains(
                    "WARBOARD_V46_TRADITIONAL_SYNAPSE"))
            {
                throw "Traditional Synapse method patch did not produce the expected marker."
            }

            $text =
                $text.Remove(
                    $range.Start,
                    $range.Length
                ).Insert(
                    $range.Start,
                    $method
                )
        }
        else {
            Write-Host "[OK] Traditional Synapse Battle-shock dice already installed." -ForegroundColor DarkGreen
        }

        return $text
    }

    # ---------------------------------------------------------------------
    # 7. LEGACY AUTOMATIC ATTACK RESOLVER — SAME STANDARD HUB
    # ---------------------------------------------------------------------
    Patch-File "Assets\Scripts\Core\RulesEngine.cs" {
        param($text)

        if (-not $text.Contains(
                "WARBOARD_V46_RULES_STANDARD_ATTACKS"))
        {
            $old = @'
                        attacks +=
                NecronsFactionPack11.AdditionalAttacks(
                    game, attacker, model, weapon, mode, target);

attacks +=
'@

            $new = @'
                        attacks +=
                NecronsFactionPack11.AdditionalAttacks(
                    game, attacker, model, weapon, mode, target);

            // WARBOARD_V46_RULES_STANDARD_ATTACKS
            attacks +=
                WarboardFactionExtensionHub
                    .AdditionalAttacks(
                        attacker,
                        weapon,
                        mode
                    );

attacks +=
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard additional attacks in RulesEngine"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_RULES_STANDARD_LETHAL"))
        {
            $old = @'
                        lethalHits = lethalHits ||
                NecronsFactionPack11.GrantsLethalHits(
                    attacker, mode);

int sustainedHits =
'@

            $new = @'
                        lethalHits = lethalHits ||
                NecronsFactionPack11.GrantsLethalHits(
                    attacker, mode);

            // WARBOARD_V46_RULES_STANDARD_LETHAL
            lethalHits =
                lethalHits ||
                WarboardFactionExtensionHub
                    .GrantsLethalHits(
                        attacker,
                        target,
                        weapon,
                        mode
                    );

int sustainedHits =
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard Lethal Hits in RulesEngine"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_RULES_STANDARD_SUSTAINED"))
        {
            $old = @'
                        sustainedHits = Mathf.Max(
                sustainedHits,
                NecronsFactionPack11.MinimumSustainedHits(
                    attacker, weapon, mode));

bool twinLinked =
'@

            $new = @'
                        sustainedHits = Mathf.Max(
                sustainedHits,
                NecronsFactionPack11.MinimumSustainedHits(
                    attacker, weapon, mode));

            // WARBOARD_V46_RULES_STANDARD_SUSTAINED
            sustainedHits =
                Mathf.Max(
                    sustainedHits,
                    WarboardFactionExtensionHub
                        .MinimumSustainedHits(
                            attacker,
                            target,
                            weapon,
                            mode
                        )
                );

bool twinLinked =
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard Sustained Hits in RulesEngine"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_RULES_STANDARD_DEVASTATING"))
        {
            $old = @'
                        devastating = devastating ||
                NecronsFactionPack11.GrantsDevastatingWounds(
                    attacker, weapon, mode);

bool precision =
'@

            $new = @'
                        devastating = devastating ||
                NecronsFactionPack11.GrantsDevastatingWounds(
                    attacker, weapon, mode);

            // WARBOARD_V46_RULES_STANDARD_DEVASTATING
            devastating =
                devastating ||
                WarboardFactionExtensionHub
                    .GrantsDevastatingWounds(
                        attacker,
                        target,
                        weapon,
                        mode
                    );

bool precision =
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard Devastating Wounds in RulesEngine"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_RULES_STANDARD_PRECISION"))
        {
            $old = @'
                        precision = precision ||
                NecronsFactionPack11.GrantsPrecision(
                    attacker, weapon, mode);

int melta =
'@

            $new = @'
                        precision = precision ||
                NecronsFactionPack11.GrantsPrecision(
                    attacker, weapon, mode);

            // WARBOARD_V46_RULES_STANDARD_PRECISION
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

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard Precision in RulesEngine"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_RULES_STANDARD_STRENGTH"))
        {
            $old = @'
            int woundTarget =
                WoundRollNeeded(
                    weapon.strength,
                    target.Toughness
                );
'@

            $new = @'
            // WARBOARD_V46_RULES_STANDARD_STRENGTH
            int effectiveStrength =
                weapon.strength +
                WarboardFactionExtensionHub
                    .StrengthModifier(
                        game,
                        attacker,
                        target,
                        weapon,
                        mode
                    );

            int woundTarget =
                WoundRollNeeded(
                    effectiveStrength,
                    target.Toughness
                );
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard Strength modifiers in RulesEngine"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_RULES_STANDARD_AP"))
        {
            $old = @'
                        int necronsApModifier =
                NecronsFactionPack11.ApModifier(
                    game, attacker, target, model, weapon, mode);

int failedSaves = 0;
'@

            $new = @'
                        int necronsApModifier =
                NecronsFactionPack11.ApModifier(
                    game, attacker, target, model, weapon, mode);

            // WARBOARD_V46_RULES_STANDARD_AP
            int standardApModifier =
                WarboardFactionExtensionHub
                    .ApModifier(
                        game,
                        attacker,
                        target,
                        weapon,
                        mode
                    );

int failedSaves = 0;
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard AP modifiers in RulesEngine"

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old @'
                        ) -
                        weapon.ap + necronsApModifier,
'@ `
                    -New @'
                        ) -
                        weapon.ap +
                        necronsApModifier -
                        standardApModifier,
'@ `
                    -Label "standard AP applied to save target"
        }

        # Automatic rerolls: use the exact source rule only where it can be
        # represented without making a player-choice assumption.
        if (-not $text.Contains(
                "WARBOARD_V46_RULES_STANDARD_HIT_REROLLS"))
        {
            $old = @'
if (!aeldari11UniversalState.cannotRerollHits &&
                    AeldariFactionPack11.AutomaticRerollHit(
                        attacker, hitRoll, skill, aeldari11UniversalState))
                {
                    hitRoll = DiceRoller.RollD6(
                        "Aeldari Hit re-roll: " + weapon.displayName);
                }

                if (!AeldariFactionPack11.AutomaticHitSucceeds(
'@

            $new = @'
if (!aeldari11UniversalState.cannotRerollHits &&
                    AeldariFactionPack11.AutomaticRerollHit(
                        attacker, hitRoll, skill, aeldari11UniversalState))
                {
                    hitRoll = DiceRoller.RollD6(
                        "Aeldari Hit re-roll: " + weapon.displayName);
                }

                // WARBOARD_V46_RULES_STANDARD_HIT_REROLLS
                if (!aeldari11UniversalState.cannotRerollHits)
                {
                    bool standardHitSuccess =
                        AeldariFactionPack11
                            .AutomaticHitSucceeds(
                                hitRoll,
                                skill,
                                aeldari11UniversalState
                            );

                    bool rerollStandardHit =
                        WarboardFactionExtensionHub
                            .RerollAllHits(
                                game,
                                attacker,
                                target,
                                weapon,
                                mode
                            )
                        ? !standardHitSuccess
                        : WarboardFactionExtensionHub
                            .RerollHitOnes(
                                game,
                                attacker,
                                target,
                                weapon,
                                mode
                            ) &&
                          hitRoll == 1;

                    if (rerollStandardHit)
                    {
                        hitRoll =
                            DiceRoller.RollD6(
                                "Faction Hit re-roll: " +
                                weapon.displayName
                            );
                    }
                }

                if (!AeldariFactionPack11.AutomaticHitSucceeds(
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard Hit rerolls in RulesEngine"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_RULES_STANDARD_WOUND_REROLLS"))
        {
            $old = @'
                if (!success &&
                    twinLinked)
'@

            $new = @'
                // WARBOARD_V46_RULES_STANDARD_WOUND_REROLLS
                if (!alreadyRerolled)
                {
                    bool standardReroll =
                        WarboardFactionExtensionHub
                            .RerollAllWounds(
                                game,
                                attacker,
                                target,
                                weapon,
                                mode
                            )
                        ? !success
                        : WarboardFactionExtensionHub
                            .RerollWoundOnes(
                                game,
                                attacker,
                                target,
                                weapon,
                                mode
                            ) &&
                          woundRoll == 1;

                    if (standardReroll)
                    {
                        woundRoll =
                            DiceRoller.RollD6(
                                "Faction Wound re-roll: " +
                                weapon.displayName
                            );

                        success =
                            AeldariFactionPack11
                                .AutomaticWoundSucceeds(
                                    woundRoll,
                                    woundTarget,
                                    criticalThreshold,
                                    aeldari11UniversalState
                                        .woundRollModifier
                                );

                        alreadyRerolled = true;
                    }
                }

                if (!success &&
                    twinLinked)
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard Wound rerolls in RulesEngine"
        }

        return $text
    }

    # ---------------------------------------------------------------------
    # 8. INTERACTIVE / XCOM ATTACK RESOLVER — SAME STANDARD HUB
    # ---------------------------------------------------------------------
    Patch-File "Assets\Scripts\Core\InteractiveAttackController.cs" {
        param($text)

        if (-not $text.Contains(
                "WARBOARD_V46_INTERACTIVE_STANDARD_ATTACKS"))
        {
            $old = @'
                                oneModelAttacks +=
                    NecronsFactionPack11.AdditionalAttacks(
                        game, attacker, selection.model,
                        weapon, mode, target);

oneModelAttacks +=
'@

            $new = @'
                                oneModelAttacks +=
                    NecronsFactionPack11.AdditionalAttacks(
                        game, attacker, selection.model,
                        weapon, mode, target);

                // WARBOARD_V46_INTERACTIVE_STANDARD_ATTACKS
                oneModelAttacks +=
                    WarboardFactionExtensionHub
                        .AdditionalAttacks(
                            attacker,
                            weapon,
                            mode
                        );

oneModelAttacks +=
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard additional attacks in interactive resolver"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_INTERACTIVE_STANDARD_STRENGTH"))
        {
            $old = @'
                        volley.effectiveStrength +=
                NecronsFactionPack11.StrengthModifier(
                    attacker, first.model, weapon, mode);

volley.effectiveAp =
'@

            $new = @'
                        volley.effectiveStrength +=
                NecronsFactionPack11.StrengthModifier(
                    attacker, first.model, weapon, mode);

            // WARBOARD_V46_INTERACTIVE_STANDARD_STRENGTH
            volley.effectiveStrength +=
                WarboardFactionExtensionHub
                    .StrengthModifier(
                        game,
                        attacker,
                        target,
                        weapon,
                        mode
                    );

volley.effectiveAp =
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard Strength in interactive resolver"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_INTERACTIVE_STANDARD_AP"))
        {
            $old = @'
            volley.effectiveAp +=
                NecronsFactionPack11.ApModifier(
                    game, attacker, target, first.model, weapon, mode);

volley.woundTarget =
'@

            $new = @'
            volley.effectiveAp +=
                NecronsFactionPack11.ApModifier(
                    game, attacker, target, first.model, weapon, mode);

            // WARBOARD_V46_INTERACTIVE_STANDARD_AP
            volley.effectiveAp +=
                WarboardFactionExtensionHub
                    .ApModifier(
                        game,
                        attacker,
                        target,
                        weapon,
                        mode
                    );

volley.woundTarget =
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard AP in interactive resolver"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_INTERACTIVE_STANDARD_LETHAL"))
        {
            $old = @'
            volley.lethalHits =
                volley.lethalHits ||
                NecronsFactionPack11.GrantsLethalHits(
                    attacker, mode);

            volley.sustainedHits =
'@

            $new = @'
            volley.lethalHits =
                volley.lethalHits ||
                NecronsFactionPack11.GrantsLethalHits(
                    attacker, mode);

            // WARBOARD_V46_INTERACTIVE_STANDARD_LETHAL
            volley.lethalHits =
                volley.lethalHits ||
                WarboardFactionExtensionHub
                    .GrantsLethalHits(
                        attacker,
                        target,
                        weapon,
                        mode
                    );

            volley.sustainedHits =
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard Lethal Hits in interactive resolver"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_INTERACTIVE_STANDARD_SUSTAINED"))
        {
            $old = @'
            volley.sustainedHits =
                Mathf.Max(
                    volley.sustainedHits,
                    NecronsFactionPack11.MinimumSustainedHits(
                        attacker, weapon, mode));

volley.twinLinked =
'@

            $new = @'
            volley.sustainedHits =
                Mathf.Max(
                    volley.sustainedHits,
                    NecronsFactionPack11.MinimumSustainedHits(
                        attacker, weapon, mode));

            // WARBOARD_V46_INTERACTIVE_STANDARD_SUSTAINED
            volley.sustainedHits =
                Mathf.Max(
                    volley.sustainedHits,
                    WarboardFactionExtensionHub
                        .MinimumSustainedHits(
                            attacker,
                            target,
                            weapon,
                            mode
                        )
                );

volley.twinLinked =
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard Sustained Hits in interactive resolver"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_INTERACTIVE_STANDARD_DEVASTATING"))
        {
            $old = @'
                        volley.devastating =
                volley.devastating ||
                NecronsFactionPack11.GrantsDevastatingWounds(
                    attacker, weapon, mode);

volley.precision =
'@

            $new = @'
                        volley.devastating =
                volley.devastating ||
                NecronsFactionPack11.GrantsDevastatingWounds(
                    attacker, weapon, mode);

            // WARBOARD_V46_INTERACTIVE_STANDARD_DEVASTATING
            volley.devastating =
                volley.devastating ||
                WarboardFactionExtensionHub
                    .GrantsDevastatingWounds(
                        attacker,
                        target,
                        weapon,
                        mode
                    );

volley.precision =
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard Devastating Wounds in interactive resolver"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_INTERACTIVE_STANDARD_PRECISION"))
        {
            $old = @'
            volley.precision =
                volley.precision ||
                NecronsFactionPack11.GrantsPrecision(
                    attacker, weapon, mode);

            volley.effectiveAp +=
'@

            $new = @'
            volley.precision =
                volley.precision ||
                NecronsFactionPack11.GrantsPrecision(
                    attacker, weapon, mode);

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

            volley.effectiveAp +=
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard Precision in interactive resolver"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_INTERACTIVE_STANDARD_HIT_REROLLS"))
        {
            $old = @'
        bool rerollAll =
            (mode == AttackMode.Melee &&
             actionUnit
                .FactionEmissariesRerollAll) ||
            actionUnit.AeldariRerollAllHits ||
            (conquering && ledNecron);

        bool rerollOnes =
            (mode == AttackMode.Melee &&
             actionUnit
                .FactionEmissariesRerollOnes) ||
            actionUnit.AeldariRerollHitOnes ||
            (conquering && !ledNecron);
'@

            $new = @'
        // WARBOARD_V46_INTERACTIVE_STANDARD_HIT_REROLLS
        bool rerollAll =
            (mode == AttackMode.Melee &&
             actionUnit
                .FactionEmissariesRerollAll) ||
            actionUnit.AeldariRerollAllHits ||
            (conquering && ledNecron) ||
            WarboardFactionExtensionHub
                .RerollAllHits(
                    game,
                    attacker,
                    target,
                    volley.weapon,
                    mode
                );

        bool rerollOnes =
            (mode == AttackMode.Melee &&
             actionUnit
                .FactionEmissariesRerollOnes) ||
            actionUnit.AeldariRerollHitOnes ||
            (conquering && !ledNecron) ||
            WarboardFactionExtensionHub
                .RerollHitOnes(
                    game,
                    attacker,
                    target,
                    volley.weapon,
                    mode
                );
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard Hit rerolls in interactive resolver"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_INTERACTIVE_STANDARD_WOUND_REROLLS"))
        {
            $old = @'
        if (necronsWoundRerolled)
            volley.automaticWoundRerolls = true;

        RecalculateWoundResults();
'@

            $new = @'
        if (necronsWoundRerolled)
            volley.automaticWoundRerolls = true;

        // WARBOARD_V46_INTERACTIVE_STANDARD_WOUND_REROLLS
        bool standardWoundRerolled = false;

        if (!volley.automaticWoundRerolls)
        {
        for (int i = 0;
             i < volley.woundRolls.Count;
             i++)
        {
            int roll =
                volley.woundRolls[i];

            bool critical =
                roll >=
                volley.criticalWoundThreshold;

            bool success =
                roll != 1 &&
                (critical ||
                 roll == 6 ||
                 roll +
                    volley.woundRollModifier >=
                    volley.woundTarget);

            bool shouldReroll =
                WarboardFactionExtensionHub
                    .RerollAllWounds(
                        game,
                        attacker,
                        target,
                        volley.weapon,
                        mode
                    )
                ? !success
                : WarboardFactionExtensionHub
                    .RerollWoundOnes(
                        game,
                        attacker,
                        target,
                        volley.weapon,
                        mode
                    ) &&
                  roll == 1;

            if (!shouldReroll)
                continue;

            volley.woundRolls[i] =
                DiceRoller.RollD6(
                    "Faction Wound re-roll: " +
                    volley.weapon.displayName
                );

            standardWoundRerolled =
                true;
        }
        }

        if (standardWoundRerolled)
        {
            volley.automaticWoundRerolls =
                true;
        }

        RecalculateWoundResults();
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard Wound rerolls in interactive resolver"
        }

        if (-not $text.Contains(
                "WARBOARD_V46_INTERACTIVE_STANDARD_INVULN"))
        {
            $old = @'
            if (aeldariInvulnerable > 0)
            {
'@

            # Insert standard save override immediately before the Aeldari
            # block; this keeps the existing best-save comparison.
            $new = @'
            // WARBOARD_V46_INTERACTIVE_STANDARD_INVULN
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
'@

            $text =
                Replace-Exact `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Label "standard faction invulnerable-save overrides"
        }

        return $text
    }

    # ---------------------------------------------------------------------
    # 9. COMMIT THE FULLY VALIDATED CORE PATCH SET AT ONCE
    # ---------------------------------------------------------------------
    Write-Host ""
    Write-Host "All source anchors validated. Committing staged Core changes..." -ForegroundColor Cyan

    Get-ChildItem `
        -LiteralPath $StageRoot `
        -File `
        -Recurse |
    ForEach-Object {
        $relative =
            $_.FullName.Substring(
                $StageRoot.Length
            ).TrimStart([char]92)

        $destination =
            Join-Path $Root $relative

        Copy-Item `
            -LiteralPath $_.FullName `
            -Destination $destination `
            -Force
    }

    Write-Host "[OK] Core patch set committed atomically." -ForegroundColor DarkGreen

    # ---------------------------------------------------------------------
    # 10. COPY THE NEW DATA-DRIVEN FACTION MODULE
    # ---------------------------------------------------------------------
    Write-Host ""
    Write-Host "Installing shared faction module and source-card data..." -ForegroundColor Cyan

    $payloadFiles =
        Get-ChildItem `
            -LiteralPath $Payload `
            -File `
            -Recurse

    $PayloadCopied = $true

    foreach ($file in $payloadFiles)
    {
        $relative =
            $file.FullName.Substring(
                $Payload.Length
            ).TrimStart([char]92)

        $destination =
            Join-Path $Root $relative

        $parent =
            Split-Path $destination -Parent

        if (-not (Test-Path $parent)) {
            New-Item `
                -ItemType Directory `
                -Force `
                -Path $parent |
                Out-Null
        }

        Copy-Item `
            -LiteralPath $file.FullName `
            -Destination $destination `
            -Force
    }

    Write-Host "[FIXED] Orks shared faction pack installed." -ForegroundColor Green
    Write-Host "[FIXED] Tyranids shared faction pack installed." -ForegroundColor Green
    Write-Host "[FIXED] Base Space Marines shared faction pack installed." -ForegroundColor Green

    # ---------------------------------------------------------------------
    # 11. DATA VALIDATION
    # ---------------------------------------------------------------------
    function Validate-Pack(
        [string]$Id,
        [int]$Detachments,
        [int]$Enhancements,
        [int]$Stratagems)
    {
        $path =
            Join-Path `
                $Root `
                ("Assets\Resources\FactionPacks11\" +
                 $Id +
                 ".json")

        if (-not (Test-Path $path)) {
            throw "Faction pack missing after install: $Id"
        }

        $data =
            Get-Content `
                -LiteralPath $path `
                -Raw |
            ConvertFrom-Json

        $actualDetachments =
            @($data.detachments).Count

        $actualEnhancements = 0
        $actualStratagems = 0

        foreach ($detachment
            in @($data.detachments))
        {
            $actualEnhancements +=
                @($detachment.enhancements).Count

            $actualStratagems +=
                @($detachment.stratagems).Count
        }

        if ($actualDetachments -ne
                $Detachments -or
            $actualEnhancements -ne
                $Enhancements -or
            $actualStratagems -ne
                $Stratagems)
        {
            throw (
                "Faction pack count mismatch for " +
                $Id +
                ": got " +
                $actualDetachments +
                "/" +
                $actualEnhancements +
                "/" +
                $actualStratagems +
                "; expected " +
                $Detachments +
                "/" +
                $Enhancements +
                "/" +
                $Stratagems
            )
        }

        Write-Host (
            "[OK] " +
            $Id +
            ": " +
            $actualDetachments +
            " detachments / " +
            $actualEnhancements +
            " enhancements / " +
            $actualStratagems +
            " stratagems"
        ) -ForegroundColor DarkGreen
    }

    Validate-Pack "orks" 13 44 66
    Validate-Pack "tyranids" 10 34 51
    Validate-Pack "space_marines" 16 59 81

    # ---------------------------------------------------------------------
    # 12. VERSION
    # ---------------------------------------------------------------------
    $BuildInfo =
        Join-Path $Core "WarboardBuildInfo.cs"

    Backup-Once $BuildInfo

    $BuildText =
        Read-Utf8 $BuildInfo

    $BuildText =
        [regex]::Replace(
            $BuildText,
            'CurrentVersion\s*=\s*"v[^"]+"',
            'CurrentVersion = "v46"'
        )

    Write-Utf8 $BuildInfo $BuildText

    if (Test-Path $Payload) {
        Remove-Item `
            -LiteralPath $Payload `
            -Recurse `
            -Force
    }

    if (Test-Path $StageRoot) {
        Remove-Item `
            -LiteralPath $StageRoot `
            -Recurse `
            -Force
    }

    Write-Host ""
    Write-Host "WARBOARD v46 INSTALLED." -ForegroundColor Green
    Write-Host "========================"
    Write-Host ""
    Write-Host "New factions:" -ForegroundColor Cyan
    Write-Host "  - ORKS"
    Write-Host "  - TYRANIDS"
    Write-Host "  - SPACE MARINES (base faction only; no supplements)"
    Write-Host ""
    Write-Host "Source-card content:" -ForegroundColor Cyan
    Write-Host "  - 39 matched-play detachments"
    Write-Host "  - 137 enhancements"
    Write-Host "  - 198 stratagems"
    Write-Host ""
    Write-Host "Important:" -ForegroundColor Yellow
    Write-Host "  Unity must now perform the final C# compile."
    Write-Host "  This environment cannot launch the Unity project."
    Write-Host "  Complex rules that cannot be represented exactly are surfaced as"
    Write-Host "  source-card/manual choices rather than silently approximated."
    Write-Host ""
    Write-Host "Audit report:" -ForegroundColor Cyan
    Write-Host "  Docs\WARBOARD_V46_DEEP_AUDIT.md"
    Write-Host ""
    Write-Host "Backups:" -ForegroundColor Cyan
    Write-Host "  Library\WarboardBackups\V46ThreeFactions"
    Write-Host ""
    Write-Host "Return to Unity and let it import/compile." -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "V46 INSTALL FAILED" -ForegroundColor Red
    Write-Host "------------------" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkRed
    Write-Host ""

    Write-Host "Rolling patched baseline files back from the v46 backup..." -ForegroundColor Yellow

    if (Test-Path $BackupRoot) {
        Get-ChildItem `
            -LiteralPath $BackupRoot `
            -File `
            -Recurse `
            -ErrorAction SilentlyContinue |
        ForEach-Object {
            $relative =
                $_.FullName.Substring(
                    $BackupRoot.Length
                ).TrimStart([char]92)

            $destination =
                Join-Path $Root $relative

            $parent =
                Split-Path $destination -Parent

            if (-not (Test-Path $parent)) {
                New-Item `
                    -ItemType Directory `
                    -Force `
                    -Path $parent |
                    Out-Null
            }

            Copy-Item `
                -LiteralPath $_.FullName `
                -Destination $destination `
                -Force
        }
    }

    if ($PayloadCopied) {
        foreach ($relative in @(
            "Assets\Scripts\Core\GameController.StandardFactionApi.cs",
            "Assets\Scripts\Core\WarboardFactionExtensionHub.cs",
            "Assets\Scripts\Factions\Standard11\StandardFactionPack11.cs",
            "Assets\Scripts\Factions\Standard11\StandardFactionGameController.cs",
            "Assets\Scripts\Factions\Standard11\StandardFactionSetupUI.cs",
            "Assets\Resources\FactionPacks11\orks.json",
            "Assets\Resources\FactionPacks11\tyranids.json",
            "Assets\Resources\FactionPacks11\space_marines.json",
            "Docs\WARBOARD_V46_DEEP_AUDIT.md"
        )) {
            $candidate =
                Join-Path $Root $relative

            if (Test-Path $candidate) {
                Remove-Item `
                    -LiteralPath $candidate `
                    -Force `
                    -ErrorAction SilentlyContinue
            }
        }
    }

    if (Test-Path $StageRoot) {
        Remove-Item `
            -LiteralPath $StageRoot `
            -Recurse `
            -Force `
            -ErrorAction SilentlyContinue
    }

    Write-Host "Rollback complete. The failed v46 installer has not been left half-applied." -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
