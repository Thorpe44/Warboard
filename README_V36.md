# Warboard v36 — Direct Core Events + Faction Ownership Cleanup

## Build identity

The visible build marker is now `WARBOARD v36`.

## What v36 changes

v35.1 physically split the old GameController monolith. v36 finishes the next
architecture step: the split files now become the real rule-timing sources
instead of being observed by bridges or faction polling loops.

### Direct events from the real core code

The one-time v36 source migration wires the authoritative methods directly:

- battle start / battle-round start / battle-round end
- turn end
- phase end
- unit selected to move
- move start / move end
- Advance declaration
- Fall Back
- unit set up from Reserves/reposition
- charge declaration
- unit selected to fight / finished fighting
- unit finished shooting
- model destroyed

Existing direct events such as `TurnStarted`, `PhaseStarted`, attack events,
`ChargeRolled` and `UnitDestroyed` remain in the core and are not replaced by a
polling observer.

`CoreEventBridge.cs` is removed.

### FactionControllerHost is event-driven

The old host refreshed every 0.20 seconds and called every faction controller
from `Update()`.

v36 removes that loop. GameController now exposes a `RostersChanged` event.
Faction controllers are rebuilt only when a roster actually changes, and the
host otherwise only routes `GameEventBus` events.

### Clean GameController runtime API

Faction code now uses explicit GameController properties/methods rather than
private-field reflection:

- `ActiveFactionId`
- `BattleRound`
- `CurrentPhase`
- `BattleSizeName`
- `BattlePoints`
- `PreGameReady`
- `FactionIds`
- `AllSquads`
- `AeldariRules`
- `GetRosterCode(factionId)`
- `GetArmy(factionId)`

### Aeldari Battle Focus is physically split out

New file:

`Assets/Scripts/Factions/Aeldari/AeldariBattleFocusController.cs`

It owns:

- the base Battle Focus token pool
- Incursion / Strike Force / Onslaught base token values
- current battle-round resource state
- unused-token loss at battle-round end
- per-phase Agile Manoeuvre repetition tracking

`AeldariGameController` now delegates Battle Focus resource state to this
controller.

### No StackTrace Battle Focus inference

v34 temporarily inferred the Agile Manoeuvre name from the C# call stack.
v36 removes that completely.

`GameController.SpendBattleFocusFor(unit, manoeuvre)` already knows the exact
manoeuvre name, so the v36 migration passes that value directly to
`AeldariGameController`.

### No Aeldari reflection / timing polling

`AeldariGameController` no longer contains:

- `System.Reflection`
- private-field reads against GameController
- `ObserveCoreTiming`
- phase/round polling in `Tick()`

It reacts to the direct core event stream instead.

### Temporary detachment keyword provenance

The v34 migration could remove an imported `YNNARI` or `BATTLELINE` keyword
because temporary detachment grants and imported roster keywords used the same
array.

v36 tracks which keywords the detachment controller itself added and only
removes those. Imported roster keywords are left alone.

`AeldariRulesSystem.ApplyDetachmentKeywords` becomes a compatibility facade
that delegates to the Aeldari faction controller instead of mutating keywords
itself.

## One-time source migration

`Assets/Editor/WarboardV36ArchitectureCleanup.cs` is included only to edit the
already-generated v35.1 split files on your machine. It is **not** a runtime
bridge.

On a successful run it:

1. verifies the v35.1 split exists;
2. backs up touched source files under `Library/WarboardBackups/V36/`;
3. injects the direct event calls into the real split methods;
4. removes `CoreEventBridge.cs` if it still exists;
5. removes the old v35 refactor Editor script;
6. validates the result;
7. deletes itself;
8. refreshes Unity for the final compile.

The resulting project therefore contains no v35/v36 refactor script and no
CoreEventBridge.

## Expected Unity sequence

1. Extract this ZIP over the Warboard project and replace files.
2. `Assets -> Refresh`.
3. First compile loads the v36 source migration.
4. The migration edits the split GameController files and removes itself.
5. Unity compiles once more.
6. Confirm the header says `WARBOARD v36`.
7. Confirm the Console has no red errors.
8. Start a small test game and make one Normal move, one Advance, one shooting
   activation, one charge and one fight activation.

If the migration safety-check fails, it leaves its own Editor script in place
and writes the error to the Unity Console rather than deleting itself.
