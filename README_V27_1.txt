WARBOARD v27.1 — MODEL-ANCHORED ACTION RANGE GUIDES

CODE-ONLY patch on top of v27.

FIXED — MOVEMENT GUIDE
- Clicking one model in the Movement phase now anchors the circle to that
  exact model's CURRENT position.
- The radius is that model's REMAINING movement, not a circle drawn from the
  unit centre or from an obsolete formation centre.
- Double-click whole-unit movement still uses a formation-centre guide.

NEW — SHOOTING GUIDE
- Click an individual friendly model in the Shooting phase.
- An amber ring appears around that exact model.
- Radius = the longest currently usable ranged weapon on that model.
- Advanced models only advertise weapons that remain usable under the current
  attack rules.

NEW — CHARGE GUIDE
- Click an individual friendly model in the Charge phase.
- A red ring appears around that exact model.
- Radius = 12", the maximum possible pre-roll 2D6 charge reach.
- The guide is hidden when the unit has Advanced, Fallen Back, already charged,
  or is already engaged.
- Actual charge legality still uses the rolled distance, target, every model in
  the unit, base sizes, coherency, collisions and engagement requirements.

CLICK FIX
- Model selection now resolves ModelToken through the collider hierarchy. This
  matters for imported OBJ visuals whose clicked collider can belong to a child
  of the gameplay token.

INSTALL
1. Close Unity.
2. Extract over the permanent Warboard project root.
3. Replace GameController.cs.
4. Reopen and test one individual model in Move, Shoot and Charge.
