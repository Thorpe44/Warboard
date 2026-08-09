WARBOARD v31.2.1 — TEST WRAITH VISUAL MATCH HOTFIX

FIXES THE SCREENSHOT WITH GIANT/EXTRA WRAITH MODELS.

ROOT CAUSE
The built-in test unit is:
    DisplayName: Aeldari Wraiths
    RoleName:    Wraith
    Models:      5
    Wounds:      3 each

The visual pack matcher was supposed to alias "Aeldari Wraiths" to
"Eldar Wraithguard".

However TryResolvePack first strips the faction prefix:
    Aeldari Wraiths -> wraiths

PackAlias was only checking "aeldariwraiths", so that alias could never fire.

The loose role name "Wraith" then matched unrelated model-pack objects that
also contained "wraith", including large Wraith constructs. This produced the
giant/extra visual geometry in the screenshot even though there were still
only five gameplay ModelTokens.

FIX
- "wraiths" now explicitly aliases to "eldarwraithguard".
- placeholder role "Wraith" is treated as generic and cannot override the unit.
- added a defensive known-family guard so loose matching cannot cross between
  Wraithguard, Wraithblade, Wraithlord, Wraithknight, etc.

VERIFICATION
Against the actual 209-entry Aeldari ModelIndex, the test roster
"Aeldari Wraiths" now resolves only to the three indexed "Eldar Wraithguard"
visual variants.

No roster/model-count logic is changed.

INSTALL
Apply over v31.2:
1. Close Unity.
2. Extract into Documents\Warboard.
3. Replace Assets\Scripts\Core\ModelVisualRegistry.cs.
4. Reopen Unity and load the same test army.

Expected result:
five 3/3-W gameplay models, all using Wraithguard visual variants; no giant
Wraithknight/Wraithlord-style meshes mixed into the unit.

Static source checks passed. Unity compile/runtime still requires local test.
