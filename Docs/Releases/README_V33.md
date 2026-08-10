# Warboard v33 — Faction Controller Architecture

## Purpose

v33 changes the architecture before adding another large rules pass.

`GameController` remains the universal Warhammer 40,000 engine. Faction-specific
behaviour now has a dedicated runtime controller layer:

- one faction controller instance per loaded player's faction
- Aeldari controller
- Necron controller scaffold
- generic fallback controller
- automatic discovery when rosters are loaded
- event routing through `GameEventBus`

The migration layer attaches automatically, so this patch does **not** require
a replacement of the 700KB+ `GameController.cs`.

## New architecture

```text
GameController / core systems
        |
        +-- FactionControllerHost
              |
              +-- Player 1 faction controller
              |      +-- AeldariGameController
              |
              +-- Player 2 faction controller
                     +-- NecronGameController / future faction controller
```

The intent is that future faction and detachment rules enter through this
controller layer instead of adding new faction-specific branches to
`GameController`.

## Aeldari migration

`AeldariGameController` binds to the existing `AeldariRulesSystem` so v32
gameplay is retained while ownership is migrated.

It also synchronises detachment-derived state so cycling detachments no longer
leaves old grants behind:

- `YNNARI` is granted to eligible ASURYANI units only while Devoted of Ynnead
  is selected.
- Windrider Host `BATTLELINE` grants are removed when leaving Windrider Host.
- Spirit Conclave Wraith `BATTLELINE` grants are removed when leaving Spirit
  Conclave.
- Harlequin/Troupe `BATTLELINE` and OC overrides are synchronised to the
  selected detachment.

## Legacy Aeldari cleanup

`FactionRuleSystem` no longer applies Servants of the Whispering God itself.
That rule belongs to the Devoted of Ynnead detachment controller path.

The old detachment-agnostic Wraith/PSYKER Battle Focus grant has also been
removed from `FactionRuleSystem`. Spirit Guides is already implemented in the
detachment-aware `AeldariRulesSystem`.

Battle Focus generation now uses the 11e values:

- Incursion: 2
- Strike Force: 4
- Onslaught: 6

## Core event vocabulary

`GameEventType` now includes the events needed by faction reaction systems,
including:

- battle-round start/end
- turn end
- phase end
- unit selected to move
- move start/end
- unit set up
- Advance / Fall Back
- finished shooting/fighting
- charge declaration
- model destruction
- objective-control change
- embark/disembark

Not every new event is emitted by `GameController` yet. v33 establishes the
contract; subsequent core passes can emit these events as the relevant core
rules are migrated.

## Files

Replaced:
- `Assets/Scripts/Core/FactionRuleSystem.cs`
- `Assets/Scripts/Core/StratagemSystem.cs`

Added:
- `Assets/Scripts/Core/FactionControllerSystem.cs`
- `Assets/Scripts/Factions/Aeldari/AeldariGameController.cs`
- `Assets/Scripts/Factions/Necrons/NecronGameController.cs`

## Test checklist

1. Open the project and allow Unity to compile.
2. Confirm the battle setup screen still appears.
3. Load an Aeldari roster and a Necron roster.
4. Confirm deployment still works.
5. Cycle Aeldari detachments and verify the selected detachment still changes.
6. Enter Devoted of Ynnead and verify eligible ASURYANI units receive YNNARI.
7. Leave Devoted of Ynnead and verify that temporary YNNARI grant disappears.
8. Start a Strike Force battle and verify Battle Focus begins at 4 before
   detachment bonuses.
9. Play through at least one movement, shooting, charge and fight sequence.
10. Confirm Necron rules still operate as in v32.

If Unity reports a compiler error, keep the Console error and line number;
this patch is intentionally small enough that a compile problem can be fixed
without touching the rest of the project.
