using System;
using System.Collections.Generic;
using UnityEngine;

public class DiceRollRecord
{
    public int Sequence;
    public string Label;
    public int[] Results;
    public int Total;
    public DateTime Time;

    public DiceRollRecord(
        int sequence,
        string label,
        int[] results)
    {
        Sequence = sequence;
        Label =
            string.IsNullOrWhiteSpace(label)
            ? "Roll"
            : label;

        Results =
            results ??
            new int[0];

        int total = 0;

        foreach (int result in Results)
            total += result;

        Total = total;
        Time = DateTime.Now;
    }
}

public static class DiceRoller
{
    private const int MaxHistory = 40;

    private static readonly
        List<DiceRollRecord> history =
            new List<DiceRollRecord>();

    private static int sequence;

    public static event Action<DiceRollRecord>
        Rolled;

    public static IReadOnlyList<DiceRollRecord>
        History
    {
        get { return history; }
    }

    public static DiceRollRecord Latest
    {
        get
        {
            return history.Count > 0
                ? history[
                    history.Count - 1
                  ]
                : null;
        }
    }

    public static int RollD6(
        string label)
    {
        return RollDice(
            1,
            6,
            label
        ).Results[0];
    }

    public static int Roll2D6(
        string label)
    {
        return RollDice(
            2,
            6,
            label
        ).Total;
    }

    public static DiceRollRecord RollDice(
        int count,
        int sides,
        string label)
    {
        count =
            Mathf.Max(
                1,
                count
            );

        sides =
            Mathf.Max(
                2,
                sides
            );

        int[] results =
            new int[count];

        for (int i = 0;
             i < count;
             i++)
        {
            results[i] =
                UnityEngine.Random.Range(
                    1,
                    sides + 1
                );
        }

        DiceRollRecord record =
            new DiceRollRecord(
                ++sequence,
                label,
                results
            );

        history.Add(record);

        while (history.Count >
            MaxHistory)
        {
            history.RemoveAt(0);
        }

        Action<DiceRollRecord> handler =
            Rolled;

        if (handler != null)
            handler(record);

        return record;
    }

    public static int RollExpressionDie(
        int sides,
        string label)
    {
        return RollDice(
            1,
            sides,
            label
        ).Results[0];
    }

    public static void ClearHistory()
    {
        history.Clear();
    }
}
