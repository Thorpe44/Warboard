WARBOARD v27 — BATTLE SIZE / REAL BATTLEFIELD / JOINED DEPLOYMENT

CODE-ONLY PATCH. Requires the permanent Warboard project and installed faction
model packs.

BATTLE SETUP
Warboard now opens with a battle setup screen before army import.

Official current 11e army construction sizes exposed by the Core Rules:
- INCURSION: 1,000 points
- STRIKE FORCE: 2,000 points

The Core Rules make battlefield/deployment geometry mission-dependent rather
than defining it from the points value alone. Until mission-pack data is added,
Warboard supplies a generic OPEN WAR battlefield preset:
- 44" x 60"
- players deploy from opposite X edges
- 10" deep deployment zones (10" x 60" each)
- five symmetric objective markers
- expanded generic terrain layout

A Custom / House Battlefield option allows:
- arbitrary points
- board X dimension
- board Z dimension
- deployment depth

RECTANGULAR BATTLEFIELD ENGINE
The old 30" x 30" BoardSize prototype is gone from gameplay geometry.
Warboard now tracks BoardWidth and BoardDepth independently.

Updated systems include:
- physical board mesh
- camera framing / zoom range
- deployment outlines
- board-edge tests
- deployment-zone tests
- Strategic Reserve edge tests
- world scoreboard / reserve / dead tray positioning
- objective / terrain preset

REAL-BASE "WHOLLY WITHIN" CHECKS
Deployment-zone and battlefield-edge checks now use each model's actual base
radius rather than only the model centre.

ATTACHED UNIT DEPLOYMENT
A Bodyguard and its pre-attached Leader are no longer deployed as:
    bodyguard first -> squeeze Leader into remaining gap

Instead Warboard:
1. treats Bodyguard + Leader as one joined model list;
2. builds a complete base-aware formation;
3. clamps that formation inside the deployment zone and battlefield;
4. searches nearby legal centres around the click;
5. validates coherency, terrain, collisions and the complete joined formation;
6. commits both units simultaneously.

This specifically targets cases such as:
    Ynnari Kabalite Warriors + Yvraine

where the old 30" board / 6" deployment strip and sequential Leader placement
could make a valid Attached Unit impossible to place.

INSTALL
1. Close Unity.
2. Extract this ZIP over the ROOT of the permanent Warboard project.
3. Replace existing files.
4. Reopen Unity.
5. Select Incursion / Strike Force / Custom from the new Battle Setup screen.
6. Load rosters.
7. Attach Yvraine to the Kabalite Warriors.
8. Deploy the complete joined unit with one battlefield click.

No model re-download, migration or Library deletion is required.
