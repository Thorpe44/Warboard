WARBOARD v45.7a - WOOD TABLE INJECTION FIX

WHY v45.7 STOPPED
=================
The first three steps succeeded:
- top bar spacing
- score strip
- longer/thinner dice tray

The final wood-table injection failed because the installer searched for:

    battlefieldWorldUI.Initialize(this);

but the actual project formats that call across several lines.

THIS FIX
========
v45.7a does not search for that line anymore.

It locates the entire BuildWorld() C# method with balanced-brace scanning and
injects the table runtime immediately before BuildWorld() closes.

It is intended specifically for the partially-applied v45.7 state.

INSTALL
=======
1. Extract this ZIP over the main Warboard project folder.
2. Run:
   FIX_WARBOARD_V45_7A_WOOD_TABLE.bat
3. Return to Unity and let it compile/import.

The patch also sets the visible version marker to v45.7.

BACKUPS
=======
Library\WarboardBackups\V45_7aWoodTableFix
