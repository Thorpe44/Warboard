WARBOARD v31.3 — TRADITIONAL STATE CONTROLS
TRADITIONAL COMPLETION PASS 3 OF 5

BASELINE
Apply over v31.2.1.

CORE RULE
Traditional mode does NOT interpret the player's physical/virtual dice.
The players resolve the tabletop rules themselves and only tell Warboard the
final state it needs to maintain.

ADVANCE
- ADVANCE no longer rolls automatically in Traditional.
- Warboard opens the free 3D dice tray with 1 D6 suggested.
- Player rolls/rerolls manually.
- Player enters only the final Advance bonus (1-6).
- Warboard then updates the unit's movement allowance.
- XCOM keeps automatic Advance resolution.

CHARGE
- Charge declaration remains board/range structured.
- Traditional no longer rolls 2D6 or offers Command Re-roll.
- Tray opens with 2 D6 suggested.
- Player resolves every reroll/optional rule themselves.
- Player enters only the final total (2-12).
- Warboard uses that final total for the existing charge movement/legality
  system.
- XCOM keeps automatic charge dice and rules decisions.

BATTLE-SHOCK
- Required Command-phase Battle-shock tests are now manual in Traditional.
- Tray opens with 2 D6 suggested.
- Warboard shows the unit and Leadership for reference only.
- Player resolves all dice/modifiers/rerolls themselves and presses:
    MARK PASS
    MARK FAIL
- FAIL applies the existing ice-blue Battle-shock visual and tracked state.
- PASS clears the state.
- Warboard never reads/interprets the dice.
- Phase advance is blocked until all required tests are marked.

REANIMATION PROTOCOLS
- Traditional Reanimation no longer rolls D3 automatically.
- At the Reanimation step Warboard opens a manual Reanimation panel.
- The player rolls all required dice themselves.
- Choose/cycle damaged units.
- Click a living model and use HEAL SELECTED +1.
- Cycle destroyed models and use RETURN 1W.
- Returned models are placed at a legal nearby position by Warboard.
- DONE THIS UNIT / DONE ALL tells Warboard when the player's tabletop
  resolution is complete.
- Warboard does NOT count or police Reanimation points in Traditional.
- XCOM's automated Reanimation engine is unchanged.

HAZARDOUS / DEATH TRIGGERS
- Traditional attacks using Hazardous weapons now produce a reminder after the
  attack is marked resolved.
- Warboard does not make the Hazardous roll or apply the result.
- Deadly Demise no longer auto-rolls if encountered in Traditional.
- Manual casualty removal detects Deadly Demise and raises a tabletop reminder.
- Player rolls manually and applies resulting wounds/models with WOUND EDIT.
- Trigger reminders block phase progression until acknowledged.

RANDOM ATTACKS / RANDOM DAMAGE
Traditional attack resolution was already fully disconnected from Warboard's
attack dice engine in v31.2. Weapon declarations now display A / S / AP / D
profile information, including attack/damage expressions, so D3/D6 attack or
damage values are resolved entirely by the players in the 3D tray.

BATTLE LOG
The log records:
- Advance final result entered by player
- Charge final total entered by player
- Battle-shock PASS/FAIL marked by player
- manual Reanimation state changes
- Hazardous / Deadly Demise reminders
It does not pretend those results were generated or validated by Warboard.

CARRIED FORWARD
- v31.2 free 3D physics dice tray
- fully manual Traditional attack damage
- manual wounds / model removal
- v31.1 visual-state language and transparent Battle Log
- v31.2.1 Wraith visual-matching hotfix
- v28.3 Attached Leader deployment fix
- XCOM automatic resolution path

TRADITIONAL ROADMAP
Pass 1/5 — visual states + audit log: COMPLETE
Pass 2/5 — 3D free dice + manual attacks/wounds: COMPLETE
Pass 3/5 — Advance/Charge/Battle-shock/Reanimation/triggers: THIS BUILD
Pass 4/5 — remove remaining faction/random automatic paths from Traditional,
           while preserving clickable Stratagems and visible applied effects
Pass 5/5 — full Traditional end-to-end audit + UI/edge-case cleanup

INSTALL
1. Close Unity.
2. Extract over Documents\Warboard.
3. Replace the included Core scripts.
4. Reopen Unity and allow compilation.

RECOMMENDED TEST
Traditional:
1. Advance a unit: roll D6 yourself, enter result.
2. Declare a charge: roll 2D6 yourself, enter total.
3. Force a Battle-shock test and mark FAIL; verify ice-blue aura.
4. Damage a Necron unit, end Command phase, manually resolve Reanimation.
5. Use/remove a model with Hazardous/Deadly Demise where available and confirm
   Warboard only raises a reminder.
6. Confirm XCOM still resolves those systems automatically.

Static C# source checks passed.
Unity compilation/runtime still requires local testing.
