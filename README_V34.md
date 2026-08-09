# Warboard v34 — Locked Detachments + Core Event Wiring + Battle Focus Authority

## Build identity

The visible header now reads `WARBOARD v34`.

From v33 onward, `WarboardBuildInfo.CurrentVersion` is the mandatory visible
build marker for every patch.

## 1. Detachments are no longer a gameplay/debug cycle

The intended flow is now:

`YellowScribe/New Recruit roster -> Aeldari faction controller -> detachment -> locked battle configuration`

Warboard probes the same YellowScribe roster code that was imported and looks
for explicit detachment metadata.

### Normal path

If the roster exposes exactly one Aeldari detachment, Warboard:

1. detects it automatically;
2. locks it;
3. loads the matching detachment controller;
4. applies the correct detachment keyword/state grants;
5. keeps that value fixed for the battle.

No manual detachment cycling is required.

### Fallback path

If YellowScribe does not expose one unambiguous detachment value, Warboard
shows a one-time pre-game Aeldari detachment selector.

The player selects the detachment recorded on the roster and clicks
`CONFIRM & LOCK`.

After that, the value is fixed for the battle.

The legacy v32 `NEXT AELDARI DETACHMENT` control may still be drawn by the
large legacy GameController while migration continues, but v34 rejects any
attempt to change a locked detachment and immediately restores the roster's
locked value. It is no longer authoritative.

## 2. Concrete Aeldari detachment controllers

v34 adds an `IAeldariDetachmentController` layer and concrete runtime
controllers for all fifteen current Aeldari detachments:

- Warhost
- Windrider Host
- Spirit Conclave
- Guardian Battlehost
- Ghosts of the Webway
- Devoted of Ynnead
- Seer Council
- Aspect Host
- Armoured Warhost
- Fateful Performance
- Path of the Outcast
- Twilight Flickers
- Serpent's Brood
- Eldritch Raiders
- Corsair Coterie

The correct controller is created automatically when the detachment is locked.

The current v32 `AeldariRulesSystem` remains the rules implementation behind
these controllers during migration. New faction/detachment work should now be
added to the controller layer instead of adding more Aeldari branches to
`GameController`.

## 3. Core event bridge

`CoreEventBridge` turns existing authoritative Warboard state transitions into
events that faction controllers can consume.

v34 wires the following migration events:

- BattleRoundStarted
- BattleRoundEnded
- TurnEnded
- PhaseEnded
- UnitSelectedToMove
- MoveStarted
- MoveEnded
- UnitSetUp
- UnitAdvanced
- UnitFellBack
- ChargeDeclared
- UnitSelectedToFight
- UnitFinishedShooting
- UnitFinishedFighting
- ModelDestroyed (using actual living models, so moving a unit into Reserves does not create false casualty events)

Existing GameController events such as `TurnStarted`, `PhaseStarted`,
`AttackStarted`, `AttackResolved`, `ChargeRolled` and `UnitDestroyed` remain
in place and are not duplicated by the bridge.

This follows the supplied 11e Core Rules timing model, where selecting a unit,
selecting a move type, making the move, satisfying after-moving conditions and
ending the move are distinct rules moments.

## 4. Battle Focus base pool now belongs to AeldariGameController

The base Battle Focus token pool is now owned by the Aeldari faction
controller.

11e values:

- Incursion: 2
- Strike Force: 4
- Onslaught: 6

At the end of the battle round, unused base Battle Focus tokens are discarded.

Warhost and Enhancement bonus tokens continue through the existing
`AeldariRulesSystem` bonus-token path so the old gameplay remains compatible
while that logic is migrated.

The old `FactionRuleSystem` API remains as a compatibility facade for the
large existing GameController, but it delegates Aeldari Battle Focus token
queries/spending to the loaded `AeldariGameController`.

## 5. Agile Manoeuvre restrictions

The existing v32 implementations of:

- Swift as the Wind
- Flitting Shadows
- Star Engines
- Sudden Strike
- Opportunity Seized
- Fade Back

remain operational.

v34 keeps the existing per-unit `AgileManoeuvreUsedThisPhase` protection and
adds faction-controller tracking for the rule that the same Agile Manoeuvre
cannot normally be triggered more than once per phase.

`Swift as the Wind` is explicitly repeatable for different units and is not
blocked by that faction-level once-per-phase tracker.

Because GameController has not yet been physically split, v34 uses a small
compatibility call-stack bridge when the legacy GameController spends a Battle
Focus token. This can be removed once the Battle Focus buttons themselves are
moved out of GameController in a later cleanup.

## 6. Ynnari authority

`FactionRuleSystem.IsYnnari()` now defers to the Aeldari faction controller.

For an Aeldari army, Ynnari gameplay therefore follows the locked
`Devoted of Ynnead` detachment rather than being inferred merely from the
presence of Yvraine or the Yncarne.

The Devoted of Ynnead pre-game selection is rejected if the army contains
neither Yvraine nor the Yncarne. Warboard does not yet import enough roster
metadata to validate which of those models is the Warlord, so the Warlord
requirement remains a roster-validation item for a later importer pass.

## Files in this patch

Replaced:

- `Assets/Scripts/Core/FactionRuleSystem.cs`
- `Assets/Scripts/Factions/Aeldari/AeldariGameController.cs`
- `Assets/Scripts/Core/WarboardBuildInfo.cs`

Added:

- `Assets/Scripts/Core/FactionControllerRuntime.cs`
- `Assets/Scripts/Core/CoreEventBridge.cs`
- `Assets/Scripts/Factions/Aeldari/AeldariDetachmentControllers.cs`
- `Assets/Scripts/Factions/Aeldari/AeldariSetupUI.cs`

## First test

1. Extract over the Warboard project.
2. Unity -> Assets -> Refresh.
3. Confirm the header says `WARBOARD v34`.
4. Import the same Aeldari roster normally.
5. If YellowScribe exposes its detachment, confirm a small
   `AELDARI • <detachment> • LOCKED` badge appears automatically.
6. If it does not, choose the roster's detachment once in the new modal and
   click `CONFIRM & LOCK`.
7. Continue to deployment.
8. Confirm the Console has no red compiler/runtime errors.
9. Start an Incursion or Strike Force battle and confirm Battle Focus starts
   at 2 or 4 base tokens respectively before Warhost/Enhancement bonuses.


## Cumulative safety files

The ZIP also includes the v33 faction-controller host, expanded
`StratagemSystem` event vocabulary, and Necron controller scaffold. This makes
v34 safe to extract over the existing project even if one of those v33 files
was missed locally.
