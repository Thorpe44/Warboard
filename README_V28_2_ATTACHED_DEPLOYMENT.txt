WARBOARD v28.2 — ATTACHED UNIT DEPLOYMENT VALIDATOR FIX

This replaces the remaining fragile Attached Leader deployment validation.

ROOT PROBLEM
Bodyguard and Leader remain separate SquadController objects even after they
form one Attached Unit. The generic CanPlaceModel / AllModelsHaveLegalPlacement
path was designed for ordinary single-model movement and scans every deployed
SquadController. During joined deployment that means members of the same
Attached Unit can still participate in the external-blocker scan.

v28.2 no longer uses that generic validator for Attached Unit deployment.

NEW JOINED-UNIT VALIDATION
The complete Bodyguard + Leader formation is validated in this order:
1. complete base inside battlefield;
2. complete base wholly inside deployment zone;
3. pairwise base spacing inside the Attached Unit;
4. collisions against OTHER deployed units only;
5. blocking terrain using a base-aware probe;
6. final joined-unit coherency.

Members of the same Attached Unit are explicitly excluded from the external
unit blocker scan.

SEARCH
Deployment-centre searching is also denser:
- 15 degree angular steps;
- search radii out to 14 inches across the deployment area.

ALREADY-DEPLOYED BODYGUARDS
If a Leader is attached after its Bodyguard has already deployed, Warboard now
re-forms the complete joined unit with the same joined deployment system
instead of trying to squeeze the Leader into one leftover gap.

DIAGNOSTICS
If placement somehow still fails, the status line now reports the actual
category/model involved, for example:
- model not wholly inside deployment zone;
- internal base overlap;
- overlap with a named deployed unit;
- overlap with blocking terrain;
- joined formation not coherent.

INSTALL
1. Close Unity.
2. Extract over the Warboard v28.1 project root.
3. Replace GameController.cs.
4. Reopen Unity.
5. Test the same Bodyguard + Leader deployment.

No model-pack or mission-data changes are included.
