WARBOARD v32 — AELDARI RULES FOUNDATION / PASS 1

SOURCE BASELINE
Built against the supplied Aeldari Faction Pack snapshot:
- Warhammer 40,000 11th edition
- Aeldari Faction Pack v1.1
- July 2026
- 15 matched-play detachment rulesets represented in the module

IMPORTANT SCOPE
This is the first Aeldari faction-wide rules pass.

It does NOT claim that every one of the roughly 80 detachment Stratagems and
Enhancements is fully mechanically automated yet.

What v32 does is replace the old "Ynnari exceptions in GameController" approach
with a real Aeldari rules module, register every current detachment, expose all
of their Enhancements/Stratagems in-game, and wire a substantial first set of
detachment/Enhancement mechanics into the shared combat/movement engine.

DETACHMENT SELECTION
Mission Setup now detects an Aeldari player and shows:
    AELDARI: <DETACHMENT> — <DETACHMENT RULE>

Use:
    NEXT AELDARI DETACHMENT

to cycle all 15 current matched-play detachments before deployment.

Auto-detection defaults:
- Yvraine / Yncarne / Ynnari roster -> Devoted of Ynnead
- all-Harlequin roster -> Ghosts of the Webway
- otherwise -> Warhost

ALL 15 REGISTERED
1. Warhost — Martial Grace
2. Windrider Host — Ride the Wind
3. Spirit Conclave — Shepherds of the Dead
4. Guardian Battlehost — Defend at All Costs
5. Ghosts of the Webway — Acrobatic Onslaught
6. Devoted of Ynnead — Strength from Death
7. Seer Council — Strands of Fate
8. Aspect Host — Path of the Warrior
9. Armoured Warhost — Skilled Crews
10. Fateful Performance — Acrobatic Onslaught
11. Path of the Outcast — Far-Reaching Doom
12. Twilight Flickers — Dance of Distortion
13. Serpent's Brood — Boons of the Brood
14. Eldritch Raiders — Yriel's Own
15. Corsair Coterie — Relentless Raiders

Every detachment's Enhancement names and Stratagem names/costs/rule summaries
are now registered in AeldariRulesSystem and surfaced in the STRATAGEMS UI.

AUTOMATED / ENGINE-WIRED IN THIS PASS

CORE BATTLE FOCUS
Existing Battle Focus is now routed through the Aeldari module as well as the
legacy faction engine.
- Spirit Guides can grant Battle Focus in Spirit Conclave.
- bonus Battle Focus tokens can coexist with the normal token pool.

WARHOST — MARTIAL GRACE
- +1 Battle Focus token each battle round.
- Timeless Strategist contributes its additional token.
- Swift as the Wind becomes +3 Move instead of +2.
- Agile Manoeuvre D6 results receive +1.
- combined Battle Focus total is shown/consumed correctly.

WINDRIDER HOST — RIDE THE WIND
- Windriders gain Battleline.
- eligible Asuryani Mounted/Vyper reserves treat the round as one higher for
  arrival/setup restrictions.
- end of the opponent's turn offers Ride the Wind extraction.
- extraction cap follows battle size: 1 Incursion / 2 Strike Force / 3 larger.
- extracted units enter Strategic Reserves.

SPIRIT CONCLAVE — SHEPHERDS OF THE DEAD
- Wraithblades/Wraithguard gain Battleline.
- nearby Asuryani Psyker Spirit Guides grant Battle Focus.
- destruction of an Asuryani Psyker model gives the killer a Vengeful Dead
  token.
- Wraith Constructs attacking a Vengeful Dead target receive +1 Hit/+1 Wound.

GUARDIAN BATTLEHOST — DEFEND AT ALL COSTS
- Dire Avengers / Guardians / Support Weapons / War Walkers receive +1 Hit
  when attacker and/or target is within an objective.

GHOSTS OF THE WEBWAY
- Harlequin charge paths can pass through enemy models.
- Troupes gain Battleline.
- Troupe models use OC 2.

DEVOTED OF YNNEAD
- the existing Strength from Death implementation is retained.
- Lethal Intent / Lethal Surge / Lethal Reprisal and the existing six
  Stratagem paths are now gated behind actually selecting Devoted of Ynnead,
  preventing a Yvraine-containing roster from accidentally running those rules
  while using another Aeldari detachment.
- non-Epic Asuryani gain the YNNARI keyword when the detachment is finalized.

ASPECT HOST — PATH OF THE WARRIOR
When an eligible unit first resolves attacks in a phase:
- choose re-roll Hit rolls of 1; or
- choose re-roll Wound rolls of 1.
XCOM applies the selected rerolls automatically.
Traditional records/displays the chosen effect but leaves the physical dice to
the players.
Mantle of Wisdom grants both choices automatically.

ARMOURED WARHOST — SKILLED CREWS
- Aeldari Vehicle ranged weapons are treated as Assault for Advance/shoot
  eligibility and range guidance.

FATEFUL PERFORMANCE
- Acrobatic Onslaught charge-path behaviour is implemented for Harlequins.

TWILIGHT FLICKERS — DANCE OF DISTORTION
- Harlequin units have the Stealth attack modifier against ranged attacks.

SERPENT'S BROOD — BOONS OF THE BROOD
- Harlequin Mounted/Vehicle attacks receive Sustained Hits 1.
- Troupes gain Battleline and OC 2.
The disembark-triggered Sustained Hits component awaits the transport
embark/disembark system.

ELDRITCH RAIDERS — YRIEL'S OWN
- all Aeldari units can charge after Advancing.
- Anhrathe / Rangers / Shroud Runners get their optional Advance reroll in XCOM.
- Traditional leaves the reroll entirely to the player.

CORSAIR COTERIE — RELENTLESS RAIDERS
- Void Thieves objective securing is applied automatically at end of phase for
  Anhrathe units on objectives they control.
The move-triggered 2+/D3 mortal-wound trap awaits a proper "unit has finished
its move" event so it cannot incorrectly trigger once per individually moved
model.

GENERIC AELDARI COMBAT HOOKS
The shared XCOM attack engine can now receive:
- offensive/defensive Hit modifiers
- offensive/defensive Wound modifiers
- Sustained Hits
- Lethal Hits
- Devastating Wounds
- AP modifiers
- Damage modifiers
- invulnerable-save overrides
- hit/wound reroll state
- ranged range modifiers

This already enables/passively supports examples such as:
- Psychic Destroyer
- Aspect of Murder
- Mirage Field
- Shimmerstone
- Voidstone
- Assassins' Eye Upgrade
- Stone of Eldritch Fury

RULE UI
The WARBOARD panel now shows the selected Aeldari detachment, rule summary,
current combined Battle Focus pool and the detachment's Enhancement names.

The STRATAGEMS menu now shows the six Stratagem cards belonging to whichever
Aeldari detachment was selected, rather than only knowing Devoted of Ynnead.

Devoted of Ynnead keeps its existing clickable/automatic Stratagem controls.
Other detachment cards are currently source-backed rule cards until their
individual timing/target/effect handlers are added in the next Aeldari pass.

NOT YET MECHANICALLY COMPLETE
The largest remaining Aeldari-specific items are:
- Seer Council Fate-dice generation/editing and CP discounts
- Path of the Outcast hidden/detection-range system
- remaining Windrider reserve/setup edge cases
- Serpent's Brood disembark trigger (needs transport system)
- Corsair Coterie move-triggered objective mortal wounds (needs unit-end-move
  event, not per-model movement)
- all remaining detachment Stratagem timing/target/effect handlers
- remaining Enhancement-specific state/once-per-battle mechanics
- detachment-specific redeploy/scout/infiltrator/transport effects

REGRESSION PROTECTION
Carried forward:
- v31.5 model-level melee
- manual pile-in / consolidation
- Traditional digital-tabletop philosophy
- XCOM automatic attack pipeline
- Battle Log transparency
- Alt-hold live movement ruler
- D3/D4/D6/D8/D10/D12/D20 mixed 3D dice tray
- manual casualty NullReference hotfix
- v28.3 off-board Attached Unit deployment fix
- permanent model-pack matching logic

INSTALL
1. Close Unity.
2. Extract over Documents\Warboard.
3. Replace the included files and add AeldariRulesSystem.cs.
4. Reopen Unity and let it compile.
5. Load the Aeldari test army.
6. In Mission Setup, confirm the new NEXT AELDARI DETACHMENT control.
7. Start with Devoted of Ynnead to verify no existing Ynnari behaviour
   regressed, then cycle Warhost / Aspect Host / Spirit Conclave for the new
   mechanics.

STATIC VALIDATION
C# lexical / brace / duplicate-method / regression-token checks passed.
This environment does not contain the Unity Editor, so local Unity compilation
and runtime testing are still required.
