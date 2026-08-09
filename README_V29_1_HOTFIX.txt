WARBOARD v29.1 — MISSION ACTION COMPILE HOTFIX

FIXED
Unity compile error:
  CS0165: Use of unassigned local variable 'actionResult'
  GameController.cs

ROOT CAUSE
The v29 mission-action click handler used a short-circuit expression:
    missionSystem != null && StartMissionAction(... out actionResult)

If missionSystem was null, C# could skip StartMissionAction entirely, meaning
the out variable actionResult was never assigned before the next line used it.

FIX
- actionResult now receives a safe default value first.
- StartMissionAction is called inside an explicit missionSystem != null block.
- No gameplay rules, mission data, model packs, deployment code or scoring
  logic are changed.

The CS0618 messages shown in the same Console screenshot are Unity API
deprecation WARNINGS only; they do not block compilation or Play Mode. They
can be cleaned separately without mixing API changes into this compile fix.

INSTALL
1. Close Unity.
2. Extract over the v29 project root.
3. Replace GameController.cs.
4. Reopen Unity and allow scripts to compile.

Static source checks passed.
