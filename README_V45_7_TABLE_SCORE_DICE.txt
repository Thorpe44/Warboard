WARBOARD v45.7 - TABLE / SCORE / DICE POLISH

WHAT THIS PATCH DOES
====================
1. Cleans up the top HUD spacing on BOTH sides.
   - Left cluster no longer feels jammed into the edge.
   - Right cluster uses matching 34px-high buttons and cleaner spacing.
   - NEXT PHASE remains on the far right.

2. Makes the scoreboard more readable.
   - Larger centered strip directly under the round/faction/phase pill.
   - Shows:
       FACTION TOTAL VP   P(primary) / S(secondary)   CP

3. Reshapes the physical world dice tray.
   - Longer along the battlefield.
   - Thinner front-to-back.
   - Still below the board, but less visually dominant.

4. Adds a wood-style tabletop under the whole play space.
   - Sits beneath the battlefield, side trays and dice tray.
   - Includes a top slab, plank-like striping, darker frame and simple legs.

FILES
=====
- Adds: Assets\Scripts\Core\WarboardV45EnvironmentTable.cs
- Patches: GameController.UI.cs
- Patches: TraditionalDiceTray3D.cs
- Patches: GameController.Core.cs
- Patches: WarboardBuildInfo.cs

INSTALL
=======
1. Extract over the main Warboard project folder.
2. Run:
   INSTALL_WARBOARD_V45_7_TABLE_SCORE_DICE.bat
3. Return to Unity and let it compile.

PREREQUISITE
============
This expects your project to already have the v45.6 / v45.6a world-dice setup,
because it patches SetWorldSpaceMode() rather than recreating it from scratch.

BACKUPS
=======
Library\WarboardBackups\V45_7TableScoreDice
