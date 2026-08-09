WARBOARD v31 — DUAL RESOLUTION MODE ALPHA

BASELINE
Apply this code-only patch over the working v30 project.

THE NEW GAME-MODE SPLIT
Battle Setup now asks for a resolution mode before battle size:

XCOM / AUTOMATIC
- Warboard performs routine combat dice internally.
- Hit, wound, save and damage dice are not shown.
- The Dice Tray is disabled in XCOM mode.
- Resolution pauses only when there is an actual player decision, such as:
  Command Re-roll, Parting the Veil, Macabre Resilience, weapon choice,
  casualty choice, mission choices, etc.
- Attack results still use the exact same InteractiveAttackController and
  random rolls as Traditional mode; XCOM changes presentation/control flow,
  not the combat formulas.

TRADITIONAL / MANUAL
- Preserves the existing stage-by-stage combat dice popup:
  Hits -> Wounds -> Saves -> Damage -> Apply Damage.
- Dice faces remain visible and the Dice Tray remains available.

IMPORTANT FIRST-PASS LIMITATION
v31 applies the automatic/manual abstraction fully to the interactive ATTACK
pipeline first. Advance, Charge, Battle-shock, Hazardous and some faction dice
still use their existing v30 resolution paths. They can now be moved onto the
same mode abstraction without another architectural rewrite.

MODEL-LEVEL SHOOTING — BOTH MODES
Shooting is now driven from the exact miniature selected.

Flow:
1. Click a friendly model in the Shooting phase.
2. Click an enemy target.
3. If that model has multiple legal weapons, Warboard opens SELECT WEAPON.
4. Choose the gun.
5. Resolve it in XCOM or Traditional mode.
6. Select the model again to fire another unused compatible weapon.

Per-model weapon state is tracked for the turn.
- A model cannot fire the same profile twice.
- Pistol / Close-Quarters and normal weapon groups cannot be mixed after the
  model commits to one group.
- One-shot weapon state is still respected.
- DONE MODEL lets the player skip the rest of that model's weapons.
- DONE UNIT lets the player finish the unit's shooting activation.

This replaces the previous behaviour where clicking a target caused every
eligible weapon in the whole unit to fire together.

PLAYER-CHOSEN CASUALTIES — BOTH MODES
When an attack destroys one or more models, Warboard now pauses before the
attack is finalised.

The defending player can:
- click a living model in the same physical squad to remove that miniature
  instead; or
- choose KEEP AUTOMATIC CASUALTY.

The automatic casualty selected internally by the damage allocator is only a
temporary suggestion until the player confirms casualty placement.

COHERENCY WARNING
If the chosen casualty would break Unit Coherency:
- Warboard shows a warning;
- the player can choose a different model; or
- press REMOVE ANYWAY.

If the player deliberately accepts the break, Warboard then removes
out-of-coherency models until the remaining unit is coherent.

Attached Leaders are protected from accidental casualty redirection:
- a normal Bodyguard casualty can only be swapped with another model in that
  Bodyguard squad;
- a Precision casualty on the Leader cannot simply be redirected into the
  Bodyguard.

XCOM NECRON REANIMATION FLOW
At the Necron Reanimation Protocols step, XCOM mode now opens:

    REANIMATION PROTOCOLS AVAILABLE

with the eligible damaged units.

The player chooses which unit resolves next.
Warboard rolls the D3 internally and applies Reanimation automatically.
If Their Number Is Legion creates a genuine optional re-roll decision, the game
still asks whether to keep or re-roll the generated Reanimation points.

Traditional mode keeps the existing visible/sequential Reanimation flow.

REGRESSION PROTECTION
v31 retains:
- v28.3 off-board Attached Unit deployment fix
- v29 mission battlefield / Objective Action system
- v30 mission preview and verified secondary automation

INSTALL
1. Close Unity.
2. Extract over Documents\Warboard.
3. Replace the five script files.
4. Reopen Unity.
5. At Battle Setup choose XCOM or TRADITIONAL before choosing battle size.

FIRST TEST
XCOM:
- select a model in Shooting;
- select a target;
- choose its weapon;
- confirm no routine attack dice window appears;
- kill a model and confirm SELECT CASUALTY appears;
- deliberately choose a coherency-breaking casualty once to test the warning.

TRADITIONAL:
- repeat the same shooting flow;
- weapon selection should be identical;
- the existing hit/wound/save/damage dice flow should appear;
- casualty selection should still happen after damage.

Then test a damaged Necron army at the Reanimation Protocols step.

STATIC VALIDATION
C# lexical / brace / duplicate-method / regression-token checks passed.
Unity Editor compilation and runtime still require local confirmation.
