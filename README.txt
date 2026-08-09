WARBOARD FULL v44 VISUAL ROLLBACK

This removes the failed v44 visual-polish pass and restores the exact source
files backed up immediately before that installer changed them.

IT RESTORES
-----------
GameController.UI.cs
GameController.Core.cs
ObjectiveController.cs
ModelToken.cs
BattlefieldWorldUI.cs
WarboardBuildInfo.cs

It also removes WarboardVisualTheme.cs if that file did not exist before v44.

IT DOES NOT TOUCH
-----------------
Custodes model assets
Custodes model resolver
New Recruit / YellowScribe fixes
Ability warning fix
Faction rules
Roster data
ModelIndex files

INSTALL
-------
1. Extract this ZIP into the Warboard project root.
2. Run ROLLBACK_WARBOARD_V44_VISUALS.bat.
3. Wait for:

   SUCCESS - FULL V44 VISUAL ROLLBACK VERIFIED

4. Return to Unity and allow it to recompile.

Do not run the v44 visual-polish installer again.
