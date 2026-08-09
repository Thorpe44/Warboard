# Warboard v37.1 — Aeldari Rules Ready Hotfix

Fixes the pre-game message:

`Aeldari rules have not finished loading yet.`

## Cause

v37 notifies the faction-controller host immediately after roster import.
At that point the backing AeldariRulesSystem can already exist but may not yet
have been configured for the newly imported army.

The Aeldari faction controller therefore sees the roster, but
AeldariRulesSystem.IsAeldariFaction(...) can still return false and reject the
detachment lock.

## Fix

AeldariGameController now ensures the backing AeldariRulesSystem is configured
against the current GameController armies before detachment validation/locking.

This does not add polling, reflection, or a runtime bridge.

## Version

Visible in-game header: `WARBOARD v37.1`

## Install

Extract over the Warboard project and replace files. This hotfix needs only the
normal Unity compile; there is no one-time Editor migration.
