# v40 Fight Phase Audit

Edition 11 section 12 is now represented as a phase-wide state machine rather
than a per-unit activation wrapper.

| Rule | v40 state |
|---|---|
| 12.01 Start of Fight Phase | Existing start-rule flow retained |
| 12.02 Pile In step | Implemented phase-wide, active player then opponent |
| 12.03 Pile-in Move | Implemented with target, distance, base-contact, coherency and engagement checks |
| 12.04 Fight Step | Implemented with Fights First / Remaining alternation |
| 12.05 Normal Fight | Implemented using existing model-level melee pipeline |
| 12.06 Overrun Fight | Implemented with additional pile-in before attacks |
| 12.07 Consolidate step | Implemented phase-wide, active player then opponent |
| 12.08 Consolidation Move | Ongoing / Engaging / Objective modes implemented |
| 12.08 New Foes To Face | Implemented as forced one-at-a-time fights |
| 12.09 End of Fight Phase | Existing end-of-phase rules resume after combat steps |

The combat-movement engine continues to use Warboard's discrete model placement
UI, so "if possible" clauses are validated through the chosen legal endpoint
rather than a full exhaustive search of every geometrically possible endpoint.
