# Warboard v35 — GameController Structural Refactor

Apply this over the working v34 project.

v35 removes the "do not touch GameController" approach.

The goal of this build is not to wrap the monolithic controller in more bridges.
It restructures the actual local `GameController.cs` while preserving its
existing method bodies and gameplay behaviour.

## What happens when Unity imports this patch

The included editor refactor runs once after the first script compilation.

It:

1. creates a safety copy at
   `Library/WarboardBackups/GameController_PreV35.cs.txt`;
2. changes `GameController` into a partial class;
3. removes the legacy `NEXT AELDARI DETACHMENT` GUI blocks;
4. changes the real GameController header to use
   `WarboardBuildInfo.CurrentVersion`;
5. physically moves GameController methods into focused partial files;
6. leaves the original `GameController.cs` as the state/lifecycle owner;
7. creates a small internal runtime API so faction controllers no longer need
   reflection to read GameController state;
8. changes `FactionControllerHost` to use GameController's actual loaded squad
   collection rather than searching the scene every 0.2 seconds;
9. removes the temporary v34 `CoreEventBridge` entirely;
10. removes the Aeldari controller's reflection dependency for core game state;
11. writes a refactor report to
    `Library/WarboardV35RefactorReport.txt`.

Unity will normally compile twice:
- first compile loads the one-time editor refactor;
- second compile compiles the newly split GameController files.

That is expected.

## Resulting GameController layout

The exact method count is generated from the user's current source, but the
target files are:

- `GameController.cs` — state and Unity lifecycle/orchestration
- `GameController.Core.cs`
- `GameController.Setup.cs`
- `GameController.Movement.cs`
- `GameController.Charge.cs`
- `GameController.Combat.cs`
- `GameController.Fight.cs`
- `GameController.Missions.cs`
- `GameController.Rules.cs`
- `GameController.Traditional.cs`
- `GameController.UI.cs`
- `GameController.RuntimeApi.cs`

All of these are the same `GameController` partial class. Existing method
bodies are moved verbatim, so references to private fields and private methods
continue to work without bridge objects or reflection.

## Faction architecture

The v34 faction-controller structure remains:

`GameController -> FactionControllerHost -> faction controller -> detachment controller`

Aeldari still has one loaded detachment controller for the roster's locked
detachment.

The structural change in v35 is that faction systems now receive a clean
internal surface from the real GameController rather than reading its private
fields through reflection.

## Core events

The temporary `CoreEventBridge.cs` from v34 is deleted by the refactor.

Existing events already raised directly by GameController remain intact.
The expanded event vocabulary stays in `StratagemSystem.cs`, but future event
work should now be added at the real transition points inside the newly split
Movement / Charge / Combat / Fight files rather than inferred by polling a
bridge.

Aeldari Battle Focus phase/round housekeeping no longer depends on the bridge;
the Aeldari faction controller observes the authoritative GameController phase
and round state directly.

## Visible version

After the second compile the real Warboard header uses:

`WARBOARD v35`

There is no separate overlay header in this build.

## First test

1. Extract this ZIP over the Warboard project and replace files.
2. Return to Unity and use `Assets -> Refresh`.
3. Let Unity finish both compile passes.
4. Open Console.
5. Confirm there are no red compiler errors.
6. Confirm the top header reads `WARBOARD v35`.
7. Check that the old `NEXT AELDARI DETACHMENT` buttons are gone.
8. Import the same Aeldari roster and confirm the detachment locks as before.
9. Run one normal move, shooting attack, charge and fight to verify behaviour
   survived the structural split.

If anything fails, the automatic pre-v35 GameController backup remains in
Library so the refactor is reversible.
