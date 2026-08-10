WARBOARD v45 — UI / UX / PRESENTATION PASS

INSTALL
=======
1. Close Play Mode in Unity.
2. Extract this ZIP directly over the MAIN Warboard project folder.
3. Double-click:
      INSTALL_WARBOARD_V45_PRESENTATION.bat
4. Return to Unity and let it import/compile.

WHAT CHANGES
============
- Coherent dark sci-fi IMGUI skin instead of stock grey controls.
- Rebuilt top command bar with clearer match/phase hierarchy.
- Dedicated selected-unit command card.
- Textured battlefield surface.
- Actual concrete / industrial metal / rubble texture assets.
- Mission terrain keeps its existing rules collider, but the visible solid cube
  is replaced by dressed ruin, barricade or rubble geometry.
- Objective circles become sci-fi objective nodes with a central beacon and
  segmented perimeter.
- World scoreboard / reserves / destroyed trays receive metallic framing and
  textured faces.
- Visible build marker becomes WARBOARD v45.

RULES SAFETY
============
The terrain gameplay object and collider are left in place. Only its Renderer is
hidden and child visual geometry is added without colliders. This means the v45
art pass does not change LOS/movement/mission rules geometry.

BACKUPS
=======
Library\WarboardBackups\V45Presentation
