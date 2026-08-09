WARBOARD CLEAN UNITY 6 TEST

This project was rebuilt from scratch for Unity 6000.5.7f1.

IMPORTANT:
After extraction, the folder you add to Unity Hub must directly contain:

Assets
Packages
ProjectSettings
README_FIRST.txt

Assets must contain Scripts and Resources.

OPEN:
1. Extract this ZIP to a new folder, for example C:\Unity\WarboardClean
2. Open Unity Hub.
3. Add project from disk.
4. Select C:\Unity\WarboardClean (the folder containing Assets).
5. Open using Unity 6000.5.7f1.
6. Let Unity finish importing.
7. Press Play.

There are no optional Unity packages in Packages/manifest.json.
No Collaborate / Version Control package is included.

TEST CONTROLS:
Left click friendly model = select squad
MOVE phase: click board
SHOOT phase: click enemy
CHARGE phase: click enemy
FIGHT phase: click enemy
E = next phase
Esc = deselect
WASD = move camera
Mouse wheel = zoom

ARCHITECTURE:
Core engine contains shared movement, attacks, damage, turns, objectives and squad logic.
Faction modules register abilities separately.
Faction/unit data lives in JSON under Resources.
