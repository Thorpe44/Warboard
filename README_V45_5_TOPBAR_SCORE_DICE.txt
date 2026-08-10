WARBOARD v45.5 - TOP BAR / SCORE / DICE POLISH

WHAT THIS PATCH DOES
====================
- Removes the separate "Wound Edit / Restore Edit / Reserves" row.
- Rebuilds the top command bar as a cleaner single-row system.
- Keeps the round/faction/phase pill centered.
- Keeps "Next Phase" on the far right.
- Adds a centered score strip under the round/faction/phase pill.
- Moves the visible build version from the top-left into a subtle bottom-left
  watermark.
- Adds a best-effort permanent dice-tray dock at the bottom of the board and
  forces showDiceTray = true.

FILES
=====
- Adds: Assets\Scripts\Core\WarboardV45HudOverlay.cs
- Patches: GameController.UI.cs
- Patches: GameController.Core.cs
- Patches: WarboardBuildInfo.cs

INSTALL
=======
1. Extract over the main Warboard project folder.
2. Run:
   INSTALL_WARBOARD_V45_5_TOPBAR_SCORE_DICE.bat
3. Return to Unity and let it compile.

BACKUPS
=======
Library\WarboardBackups\V45_5TopbarScoreDice
