WARBOARD v31.2 — TRADITIONAL TABLETOP MODE
TRADITIONAL COMPLETION PASS 2 OF 5

BASELINE
Apply over the working v31.1 Visual State + Battle Log build.

CORE PHILOSOPHY CHANGE
Traditional mode is now a DIGITAL TABLETOP, not a rules referee.

WARBOARD STILL HANDLES
- turns and phases
- movement / range / LOS legality
- deployment / reserves
- model and weapon selection
- mission state and objective control
- CP and clickable Stratagem state
- visual statuses such as Battle-shock
- Unit Coherency warnings
- weapon-used state
- battlefield / score / mission UI

WARBOARD DOES NOT RESOLVE ATTACK DICE OR DAMAGE IN TRADITIONAL
When a Traditional attack is declared:
1. Warboard records attacker, target and weapon(s).
2. It checks the board-state legality already used by the game.
3. It opens the free 3D dice tray.
4. The players roll whatever dice the tabletop rules require.
5. The players apply wounds manually.
6. Click ATTACK RESOLVED when finished.

No hit/wound/save/damage result is calculated by Warboard in Traditional mode.
No failed roll is automatically identified.
No reroll is offered because the players are responsible for knowing when a
reroll or optional rule applies.

XCOM MODE IS UNCHANGED
XCOM continues to use the automatic rules engine and its transparent Battle Log.

FREE 3D PHYSICS DICE TRAY
The old flat result display has been replaced in Traditional mode by a real
runtime 3D dice tray rendered through its own camera.

Features:
- D6 count from 1 to 40
- -5 / -1 / +1 / +5 count controls
- ROLL ALL
- physical Rigidbody gravity / collisions / tumble / bounce
- numbered faces on each physical die
- click individual dice in the tray to select them
- REROLL SELECTED is entirely free-form; Warboard does not ask why
- CLEAR
- available at any time from the 3D DICE top-bar button
- automatically opens when a Traditional attack is declared
- final physical dice values are written to the Battle Log, but are NOT
  interpreted as hits, wounds, saves, etc.

MANUAL WOUND TRACKING
Traditional now has a WOUND EDIT mode in the board action bar.

Turn WOUND EDIT on, then click ANY model — friendly or enemy.

Controls:
- -1 wound
- +1 wound
- REMOVE model

Warboard does not connect these wound changes to the dice tray. The players
decide what damage happened and update the model themselves.

CASUALTY / COHERENCY
If REMOVE would break Unit Coherency:
- Warboard warns;
- choose another model, or click REMOVE ANYWAY;
- if confirmed, additional out-of-coherency models are removed until the
  remaining unit is coherent.

Those forced removals do not automatically resolve death-triggered dice rules
in Traditional mode. The players resolve those manually as on tabletop.

ATTACK FLOW
A small TRADITIONAL — ATTACK DECLARED panel remains on screen while the players
roll dice and edit wounds.

The game cannot advance phase while this attack is pending.

Click ATTACK RESOLVED once the tabletop resolution is finished. Warboard then:
- records the attack as manually resolved;
- updates per-model weapon-used state;
- advances normal shooting/fight activation state;
- continues fight priority / special callback flow where applicable.

LOG FIX
Also fixes the v31.1 LOG top-bar toggle so opening the Battle Log no longer
immediately switches itself off.

TRADITIONAL ROADMAP
Pass 1/5: visual state + Battle Log — complete
Pass 2/5: real free 3D dice tray + fully manual attack damage — THIS BUILD
Pass 3/5: convert Advance, Charge, Battle-shock, Reanimation, Hazardous and
          other compulsory dice to tabletop-manual interactions
Pass 4/5: remove remaining automatic/random faction-resolution paths from
          Traditional while retaining clickable Stratagem/effect state
Pass 5/5: end-to-end Traditional audit, edge cases and UI cleanup

INSTALL
1. Close Unity.
2. Extract this ZIP over Documents\Warboard.
3. Replace GameController.cs and add TraditionalDiceTray3D.cs.
4. Reopen Unity and allow compilation.

FIRST TEST
1. Choose TRADITIONAL.
2. Enter Shooting phase.
3. Select model -> target -> weapon.
4. Confirm the game declares the attack without opening the old combat
   hit/wound/save/damage resolver.
5. Use 3D DICE and watch the physical dice tumble into the tray.
6. Turn on WOUND EDIT, click the target model and manually use -1 / REMOVE.
7. Click ATTACK RESOLVED.
8. Repeat once in XCOM and confirm automatic combat still works.

STATIC VALIDATION
C# lexical / brace / regression-token checks passed.
Unity Editor compilation and physics/render behaviour still require local
testing in Unity.
