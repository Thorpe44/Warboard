WARBOARD v29 — MISSION BATTLEFIELD + OBJECTIVE ACTIONS

BASELINE
Apply this code-only patch over the confirmed v28.3 Warboard project.

MAJOR CHANGE 1 — MATCHUP-AWARE MISSION BATTLEFIELDS
The v28 rectangular deployment-strip prototype is no longer the primary mission
deployment system.

v29 adds a data-driven MissionBattlefieldDefinition layer with named deployment
archetypes:
- Tipping Point
- Sweeping Engagement
- Crucible of Battle
- Search and Destroy
- Hammer and Anvil
- Dawn of War

The selected Force-Disposition matchup resolves to a battlefield archetype.
Mission Setup now displays the archetype for the selected matchup/layout.

Deployment zones are polygons rather than simple minX/maxX strips.
"Wholly within" checks use each miniature's real base radius and sample around
the circular base edge.

Infiltrators and reserve restrictions use distance to the actual deployment
zone rather than distance to an old rectangular boundary.

Search and Destroy also supports a central circular exclusion area.

MAJOR CHANGE 2 — DATA-DRIVEN OBJECTIVES / TERRAIN
Objectives are generated from mission battlefield definitions rather than from
the old hard-coded v28 objective function.

Objectives carry mission roles:
- Player 1 home
- Player 2 home
- Central
- Expansion

Terrain areas now have persistent mission IDs and can store mission state:
- operation-marker ownership
- trapped-by faction
- trapped round
- whether the terrain area is also mission-objective terrain

Three layout slots (A/B/C) are generated from a symmetric data-driven terrain
framework.

IMPORTANT TERRAIN LIMITATION
These A/B/C terrain footprints are functional mission-framework layouts. They
are NOT claimed to be exact measured reproductions of all 45 Battlemaster /
Chapter Approved recommended terrain cards yet.

The deployment archetype layer and terrain data structures are deliberately
separate so exact measured terrain packs can replace the framework layouts
without rewriting the mission engine.

One matchup — Reconnaissance vs Reconnaissance — currently uses the isolated
Hammer and Anvil fallback mapping because its referenced layout image was not
reliably retrievable during this build. It is isolated in
MissionBattlefieldRegistry for easy correction.

MAJOR CHANGE 3 — OBJECTIVE ACTION FRAMEWORK
v29 implements the 11th-edition core Action state model:
- unit must be on the battlefield
- not AIRCRAFT / FORTIFICATION
- not Battle-shocked
- must have positive OC
- normally cannot be engaged
- cannot have Advanced or Fallen Back this turn
- cannot already have started another action this turn
- action-starting units cannot shoot (except TITANIC)
- action-starting units cannot declare a charge
- movement before completion can cancel a pending action

Mission actions are exposed contextually in the Shooting phase.

Supported primary-action framework includes:
- Smoke and Mirrors — Decoy objective
- Death Trap — trap terrain
- Extract Relic — Sensor Sweep / operation-marker terrain
- Sabotage
- Vanguard Operation
- Triangulation
- Gather Intel
- Surveil the Foe
- Secure Asset
- Vital Link

Interaction:
1. Select an eligible friendly unit in the Shooting phase.
2. Use the mission-action button in the context action bar.
3. If the action needs a target, Warboard enters target mode.
4. Click the required objective, terrain area or enemy unit.
5. Warboard validates the action and stores/completes the mission state.
6. Pending actions resolve at end of turn.
7. Mission scoring reads that battlefield state.

MISSION STATE
Objectives can now retain:
- DECOYED
- TRIANGULATED
- INTEL
- SECURED
- CONSECRATED

Terrain can retain operation markers and traps.

MISSION PANEL
The existing Mission panel remains available for diagnostics/manual correction.
The new battlefield/action state is synchronized into mission scoring so the
manual counters are no longer the only way mission operations can work.

ATTACHED-UNIT REGRESSION PROTECTION
The confirmed v28.3 fix is retained:
deployment uses AllLivingModelTokens() for staged Bodyguard + Leader models.
Do not revert this to LivingModelTokens(), which is intentionally empty while
a unit is off the battlefield.

INSTALL
1. Close Unity.
2. Extract this ZIP directly over Documents\Warboard.
3. Replace the six script files.
4. Reopen Unity and allow scripts to compile.

RECOMMENDED TEST
1. Strike Force.
2. Load the same two rosters used for v28.3.
3. Enter Mission Setup.
4. Change both Force Dispositions and confirm the displayed deployment
   archetype changes.
5. Begin deployment and confirm the zone outlines are no longer always the old
   rectangular strips.
6. Re-test Yvraine + Kabalite Warriors attached deployment.
7. Reach the Shooting phase in a mission with an Objective Action.
8. Select an eligible unit and test the mission-action context button and
   battlefield target selection.

VALIDATION
Static C# lexical / brace / duplicate-method checks passed.
Unity Editor compilation and runtime behaviour still require local testing in
Unity.

SOURCE / SCOPE NOTE
v29 is the mission-engine battlefield/action implementation pass. Exact measured
terrain-card reproduction and full automatic handling of every secondary card
remain later data/automation passes.
