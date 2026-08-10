WARBOARD v45.3 - LAYOUT + PHYSICAL SIDE TRAYS

PURPOSE
=======
Addresses the next visual/UX issues:

1. The CUSTOM / HOUSE BATTLEFIELD setup area is too cramped.
2. The deployment panel should be wider and centered.
3. The side model-list boxes should become physical trays beside the board.

WHAT THIS PATCH DOES
====================
- WIDENS the battle-setup / custom-house-battlefield panel and gives it more
  vertical room.
- WIDENS + CENTERS the deployment panel.
- Adds a new runtime component:
      WarboardV45PhysicalSideTrays.cs
- Creates physical 3D reserve / destroyed trays on the left and right sides of
  the board.
- Hides the legacy BattlefieldWorldUI side panels.
- Stages off-board living squads into reserve trays.
- Stages destroyed squads into destroyed trays.

INSTALL
=======
1. Extract this ZIP over the main Warboard project folder.
2. Run:
   INSTALL_WARBOARD_V45_3_LAYOUT_AND_PHYSICAL_TRAYS.bat
3. Return to Unity and let it compile.

BACKUPS
=======
Backups are written to:
Library\WarboardBackups\V45_3LayoutAndTrays
