WARBOARD v45.4 - UI / PHYSICAL TRAYS / SCOREBOARD

FIXES THE ISSUES SEEN IN v45.3
==============================
1. Physical side trays were too small.
2. Reserve/dead models did not visibly appear.
3. The scoreboard disappeared.
4. The top round/faction/phase text was not truly centered.
5. Custom / House Battlefield still felt cramped on smaller resolutions.

WHAT CHANGED
============
- Side trays enlarged to 11 x 7.2 world units.
- The gameplay models are NOT moved into those boxes anymore.
- Instead, the tray creates non-interactive visual copies of the actual
  miniature visual models.
- Reserve tray shows models whose SquadBattlefieldState is Reserves.
- Destroyed tray shows each individually destroyed ModelToken.
- Visual copies have no colliders and cannot affect gameplay.
- BattlefieldWorldUI stays active, preserving the central scoreboard.
- Only its old reserve/dead text panels are hidden.
- Battle Setup has been rebuilt with a responsive custom-battlefield card.
- The ROUND | FACTION | PHASE pill is centered against Screen.width.
- Version marker becomes v45.4.

INSTALL
=======
1. Extract this ZIP directly over the main Warboard project folder.
2. Run:
   INSTALL_WARBOARD_V45_4_UI_TRAYS_SCOREBOARD.bat
3. Return to Unity and let it compile.

BACKUPS
=======
Library\WarboardBackups\V45_4UiTraysScoreboard
