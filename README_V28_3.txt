WARBOARD v28.3 — ATTACHED UNIT OFF-BOARD MODEL FIX

ROOT CAUSE CONFIRMED
SquadController.LivingModelTokens() deliberately returns an empty list when a
squad is not on the battlefield.

Attached Unit deployment was doing:
    bodyguard.LivingModelTokens()
    leader.LivingModelTokens()
BEFORE either squad was switched from Undeployed to Battlefield.

Therefore a perfectly healthy staged unit appeared to contain zero models and
triggered:
    "Illegal deployment: the Attached unit has no models available to place."

FIX
- TryDeployJoinedFormation now uses AllLivingModelTokens() for both the
  Bodyguard and Leader while they are staged off-board.
- The older Leader placement helper is also defensive about off-board units.
- If a genuine zero-model condition ever occurs again, the error now reports
  the exact model counts and unit names.

No geometry, deployment-zone dimensions, mission rules, model assets or roster
data are changed by this patch.

INSTALL
1. Close Unity.
2. Extract over the permanent Warboard v28.2 project.
3. Replace GameController.cs.
4. Reopen Unity.
5. Attach Yvraine to the Kabalite Warriors and deploy them.

Static source checks passed.
