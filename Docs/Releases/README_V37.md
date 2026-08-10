# Warboard v37 — Roster-Driven Detachments

## Visible build identity

The in-game build marker is now:

`WARBOARD v37`

## What v37 changes

v37 finishes the Aeldari pre-game detachment flow.

The intended path is now:

`Roster import -> roster metadata -> faction controller -> detachment controller -> locked battle configuration`

There is no runtime detachment cycling.

### 1. Uses the roster payload that was already imported

`YellowScribeImporter.Parse` is connected to a new
`RosterImportMetadataStore`.

The store receives metadata from the exact JSON payload that produced the army.
AeldariGameController reads that metadata directly.

This removes the old v34 behaviour where AeldariGameController made a second
YellowScribe network request just to try to discover the detachment.

### 2. Conservative automatic detection

The metadata store is faction-neutral.

It records:

- values beneath explicit `detachment`-style fields;
- structural roster selection names such as `name`, `title`, `selection`,
  `force`, `category`, and `type`;
- the imported unit IDs belonging to that roster.

AeldariGameController then resolves those values against the fifteen supported
Aeldari detachment names.

Automatic locking only happens when Warboard finds one unambiguous matching
detachment.

If several detachment names are present, or no supported detachment is exposed,
Warboard does not guess.

### 3. Metadata is tied to the actual imported army

Roster metadata stores the imported UnitData IDs.

Before using metadata, the Aeldari controller checks that those IDs match the
army currently loaded for that player.

That prevents an old roster's detachment from being reused accidentally after
a different army is loaded.

### 4. One-time fallback selection

If the imported roster does not expose one unambiguous detachment, the existing
Aeldari pre-game screen becomes the fallback.

The user selects the detachment written on the roster once and confirms it.

This is selection, not cycling.

### 5. Deployment is now hard-gated

v37 adds a generic `IFactionPreGameController` contract.

A faction controller can report whether its required pre-game configuration is
complete.

`BeginDeployment` now asks `FactionControllerHost` whether every faction is
ready.

For Aeldari, deployment is blocked until the detachment is locked.

This makes the detachment fixed before any model is deployed.

### 6. Legacy Aeldari guessing is removed

The one-time v37 migration removes:

- `AutoDetectDefault`
- the Yvraine / Yncarne -> Devoted of Ynnead guess
- the all-Harlequin -> Ghosts of the Webway guess
- `NextDetachment`
- the old detachment-order cycling array

`AeldariRulesSystem` keeps a Warhost placeholder only as a legacy storage
default. No detachment-specific state is applied by AeldariGameController until
the real detachment is locked.

### 7. Roster replacement resets pre-game detachment state

If a different matching roster is imported before deployment, the controller
drops the old pre-game detachment state and resolves the new roster again.

Once deployment has started, the selected detachment cannot be changed.

## Files replaced directly

- `Assets/Scripts/Core/FactionControllerSystem.cs`
- `Assets/Scripts/Core/GameController.RuntimeApi.cs`
- `Assets/Scripts/Core/WarboardBuildInfo.cs`
- `Assets/Scripts/Factions/Aeldari/AeldariGameController.cs`
- `Assets/Scripts/Factions/Aeldari/AeldariSetupUI.cs`

## Files added

- `Assets/Scripts/Core/RosterImportMetadataStore.cs`
- `Assets/Editor/WarboardV37RosterDetachmentMigration.cs`

The Editor migration is one-time only. After it successfully updates the local
split source it deletes itself.

## Files modified by the one-time migration

- `Assets/Scripts/Core/YellowScribeImporter.cs`
- `Assets/Scripts/Core/AeldariRulesSystem.cs`
- `Assets/Scripts/Core/GameController.Setup.cs`

Backups are written to:

`Library/WarboardBackups/V37/`

The migration report is written to:

`Library/WarboardV37RosterDetachmentReport.txt`

## Expected Unity sequence

1. Extract v37 over the Warboard project and replace files.
2. Unity compiles the new v37 source.
3. The v37 migration patches the three existing local source files.
4. The migration script deletes itself.
5. Unity compiles a second time.
6. The visible header reads `WARBOARD v37`.

## Smoke test

1. Import both rosters normally.
2. If the Aeldari roster exposes one detachment, confirm a badge appears:
   `Player X • AELDARI • <detachment> • ROSTER LOCKED`.
3. If it does not, confirm the one-time Aeldari detachment screen appears.
4. Try to begin deployment without confirming the fallback selection:
   deployment must be blocked.
5. Confirm the detachment.
6. Begin deployment.
7. Start the game and verify normal phase flow.
8. Confirm there are no red Console errors.
