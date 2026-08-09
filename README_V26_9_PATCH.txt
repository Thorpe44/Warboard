WARBOARD v26.9 — ATTACHED LEADER DEPLOYMENT FIX

Fixes the failure:
    "Illegal deployment: the attached Leader could not be placed legally
     with the unit."

ROOT PROBLEM
The previous Leader auto-placement only tested 8 directions around each
Bodyguard model. With a dense 10-model unit such as Kabalite Warriors, and
especially near a deployment-zone boundary, a legal Yvraine position can lie
between those coarse angles. The routine could therefore report failure even
though a legal position existed.

FIX
- Tests 24 directions around every outer Bodyguard model.
- Tests several base-aware distances from each model.
- Adds a second 36-position search ring around the entire unit.
- Uses actual model base radii for contact spacing.
- Validates only the Leader's new physical placement plus joined-unit
  coherency/zone legality.
- Uses the deployed Bodyguard faction for zone validation.

INSTALL
1. Close Unity.
2. Extract this patch over the permanent Warboard project root.
3. Replace existing files.
4. Reopen Unity.
5. Attach Yvraine to the Kabalite Warriors and deploy the joined unit.

No model-pack changes are required.
