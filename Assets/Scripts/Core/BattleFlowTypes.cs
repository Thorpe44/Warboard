using System;
using System.Collections.Generic;
using UnityEngine;

public class RuleChoiceOption
{
    public string Label;
    public Action Action;

    public RuleChoiceOption(string label, Action action)
    {
        Label = label;
        Action = action;
    }
}

public class DestroyedModelRecord
{
    public ModelToken Model;
    public SquadController Unit;
    public SquadController Attacker;
    public Vector3 Position;
    public PhaseSnapshot Phase;
    public bool HadFought;
}

public class DestroyedUnitRecord
{
    public SquadController Unit;
    public SquadController Attacker;
    public Vector3 LastPosition;
    public string FactionId;
    public PhaseSnapshot Phase;
}

public enum PhaseSnapshot
{
    Command,
    Move,
    Shoot,
    Charge,
    Fight,
    End
}

public enum FightPriorityStep
{
    None,
    FightsFirst,
    Remaining
}
