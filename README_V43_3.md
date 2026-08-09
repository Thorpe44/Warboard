# Warboard v43.3 — Custodes Setup Compile Hotfix

The v43.2 migration got past the UniversalRuleEngine parser issue and then
exposed a bad identifier inserted into `GameController.Setup.cs`.

The broken generated code used:

`CustodesFactionPack11.CanIngressFirstMovement(squad)`

but that reserve-arrival method uses GameController's existing
`reservePlacementSquad` state. There is no local variable named `squad`.

v43.3 does two things:

1. Adds a temporary partial-class compile shim so the already-broken local
   GameController.Setup.cs can compile once.
2. The v43 migration immediately repairs the actual source to use
   `reservePlacementSquad`, then removes the temporary shim before the final
   compile.

It also changes the migration template so the bad identifier cannot be
reinserted on subsequent runs.

Install directly over the current failed v43.2 state and choose Replace files.

Visible header after completion: `WARBOARD v43.3`.

The remaining CoreRules11Completion CS0618 message shown in the console is a
warning only.
