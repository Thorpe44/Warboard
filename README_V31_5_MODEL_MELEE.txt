WARBOARD v31.5 — MODEL-LEVEL MELEE + MANUAL PILE-IN / CONSOLIDATION

BASELINE
Apply over v31.4.4.

MELEE NOW FOLLOWS THE SAME MODEL-DRIVEN PHILOSOPHY AS SHOOTING.

NORMAL FIGHT ACTIVATION
1. Select the exact friendly model/unit you want to activate.
2. Click an enemy unit to start that unit's fight activation.
3. Resolve PILE-IN model-by-model.
4. Click DONE PILE-IN.
5. For each fighting model:
       click model
       -> choose melee weapon/profile
       -> click enemy target
6. Resolve that model's attack.
7. Repeat for the other models, or mark a model DONE.
8. Click DONE ATTACKS when finished.
9. Resolve CONSOLIDATION model-by-model.
10. Click DONE CONSOLIDATE.

PILE-IN
The previous automatic whole-unit pile-in is removed for normal fight
activations.

During PILE-IN:
- click a model in the active unit;
- click the battlefield to place that model;
- each model is limited to the normal 3" pile-in distance;
- Sudden Strike still raises the limit to 6";
- movement path / model collision / terrain / battlefield checks remain active;
- moved models must finish closer to the nearest enemy model;
- temporary out-of-coherency positions are allowed while moving individual
  models, but DONE PILE-IN is blocked until the complete unit is coherent.

HOLD ALT during PILE-IN:
- live world-space line from the selected model to cursor;
- segment distance;
- total distance from that model's pile-in starting position;
- current maximum;
- legal line is purple;
- illegal destination turns red.

MODEL-LEVEL MELEE ATTACKS
After pile-in:
- click the exact fighting model;
- if that model has multiple normal melee profiles, Warboard opens
  SELECT MELEE WEAPON;
- if it has one normal profile, it is selected immediately;
- then click the enemy target.

Weapon selection occurs BEFORE target selection.

Any [EXTRA ATTACKS] profiles on that same model are added automatically to the
selected normal profile.

Each model is tracked independently for the current fight activation.
DONE MODEL skips/finishes the selected model.
DONE ATTACKS moves the unit to consolidation.

XCOM
The selected model + selected melee profile is resolved automatically through
the existing InteractiveAttackController.
Routine dice remain hidden but audited through the Battle Log.

TRADITIONAL
Exactly the same model / weapon / target declaration is used.
The attack becomes a declaration only:
- free 3D dice tray;
- player rolls/interprets everything;
- manual wound/model controls;
- ATTACK RESOLVED returns Warboard to the next model in the fight activation.

A single model's attack no longer marks the entire unit HasFought.

MELEE RANGE
During the attack-selection stage:
- selected model gets a melee-range action circle;
- HOLD ALT gives a live model-to-cursor melee ruler.

CONSOLIDATION
The previous automatic whole-unit consolidation is removed for normal fight
activations.

Consolidation is now model-by-model:
- select model;
- click destination;
- normal 3" maximum;
- 6" with Sudden Strike;
- existing placement/path restrictions remain;
- model must finish closer to the nearest enemy model;
- final unit coherency is required before DONE CONSOLIDATE.

HOLD ALT:
- live green consolidation ruler;
- segment + total/max distance;
- invalid destinations turn red.

Only after DONE CONSOLIDATE:
- the unit is marked HasFought;
- Martial Ka'tah temporary attack state is cleared;
- Fight priority advances to the next eligible unit.

SPECIAL FIGHT-ON-DEATH
Existing special fight-on-death attack handling is retained. This patch changes
the normal Fight-phase activation flow rather than rewriting every special
reaction in the same pass.

CARRIED FORWARD
- v31.4.4 manual-casualty NullReference hotfix
- Alt-hold measurement ruler
- D3/D4/D6/D8/D10/D12/D20 mixed 3D dice tray
- Traditional manual state controls / faction-random firewall
- XCOM automatic attack resolution + Battle Log
- ice-blue Battle-shock visuals
- live objective control colours
- v31.2.1 Wraith visual-match fix
- v28.3 Attached Leader deployment fix

INSTALL
1. Close Unity.
2. Extract over Documents\Warboard.
3. Replace Assets\Scripts\Core\GameController.cs.
4. Reopen Unity and allow compilation.

RECOMMENDED TEST
Use a unit with several melee models:
1. enter Fight;
2. select one actual model and an enemy to activate the unit;
3. manually pile in at least two different models;
4. hold ALT during pile-in and confirm the ruler follows the selected model;
5. DONE PILE-IN;
6. select a model with multiple melee profiles if available;
7. choose profile, then click target;
8. verify XCOM auto-resolves or Traditional declares only;
9. resolve another model separately;
10. DONE ATTACKS;
11. move two models during consolidation with ALT ruler;
12. DONE CONSOLIDATE;
13. confirm Fight priority advances only at that point.

STATIC VALIDATION
C# lexical / brace / duplicate-method / regression-token checks passed.
Unity Editor compilation and runtime still require local testing.
