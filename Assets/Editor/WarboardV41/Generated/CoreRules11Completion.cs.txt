using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

// WARBOARD_V41_CORE_COMPLETION_RUNTIME

public enum CoreTerrainCategory11
{
    Exposed,
    Light,
    Dense
}

/// <summary>
/// 11e terrain interpretation for Warboard's existing TerrainFeature objects.
/// Legacy Traversable/Cover/Blocking assets map to Exposed/Light/Dense so old
/// battlefields remain usable without rebuilding scenes.
/// </summary>
public static class CoreRules11Terrain
{
    public const float HiddenDetectionRange = 15f;
    public const float GoneToGroundDetectionPenalty = 3f;
    public const float SolidGapHeight = 3f;

    public static CoreTerrainCategory11 Category(
        TerrainFeature terrain)
    {
        if (terrain == null)
            return CoreTerrainCategory11.Exposed;

        switch (terrain.Trait)
        {
            case TerrainTrait.Blocking:
                return CoreTerrainCategory11.Dense;
            case TerrainTrait.Cover:
                return CoreTerrainCategory11.Light;
            default:
                return CoreTerrainCategory11.Exposed;
        }
    }

    public static TerrainFeature[] AllTerrain()
    {
        return UnityEngine.Object
            .FindObjectsOfType<TerrainFeature>();
    }

    public static Collider TerrainCollider(
        TerrainFeature terrain)
    {
        if (terrain == null)
            return null;

        Collider own =
            terrain.GetComponent<Collider>();

        if (own != null)
            return own;

        return terrain
            .GetComponentInChildren<Collider>();
    }

    public static bool ModelInsideTerrainArea(
        ModelToken model,
        TerrainFeature terrain)
    {
        if (model == null || terrain == null)
            return false;

        Collider col = TerrainCollider(terrain);

        if (col == null)
            return false;

        Vector3 point =
            model.transform.position;

        // Terrain areas are represented by Warboard's terrain collider bounds.
        // Give the base plane a small tolerance so a base resting exactly on a
        // bounded terrain mat is still treated as within that terrain area.
        Bounds bounds = col.bounds;
        bounds.Expand(new Vector3(0.04f, 0.20f, 0.04f));

        return bounds.Contains(point);
    }

    public static bool UnitInsideLightOrDenseArea(
        SquadController unit)
    {
        if (unit == null)
            return false;

        TerrainFeature[] terrain = AllTerrain();

        return unit
            .JoinedLivingModelTokens()
            .Any(
                model =>
                    terrain.Any(
                        feature =>
                            Category(feature) !=
                                CoreTerrainCategory11.Exposed &&
                            ModelInsideTerrainArea(
                                model,
                                feature
                            )
                    )
            );
    }

    public static bool ModelInsideAnyTerrainArea(
        ModelToken model)
    {
        return AllTerrain().Any(
            terrain =>
                ModelInsideTerrainArea(
                    model,
                    terrain
                )
        );
    }

    public static bool ModelInsideLightOrDenseArea(
        ModelToken model)
    {
        return AllTerrain().Any(
            terrain =>
                Category(terrain) !=
                    CoreTerrainCategory11.Exposed &&
                ModelInsideTerrainArea(
                    model,
                    terrain
                )
        );
    }

    public static float HorizontalBaseGap(
        ModelToken a,
        ModelToken b)
    {
        if (a == null || b == null)
            return float.MaxValue;

        Vector2 pa =
            new Vector2(
                a.transform.position.x,
                a.transform.position.z
            );

        Vector2 pb =
            new Vector2(
                b.transform.position.x,
                b.transform.position.z
            );

        return Mathf.Max(
            0f,
            Vector2.Distance(pa, pb) -
            Mathf.Max(0f, a.BaseRadiusInches) -
            Mathf.Max(0f, b.BaseRadiusInches)
        );
    }

    public static float ModelDistance(
        ModelToken a,
        ModelToken b)
    {
        if (a == null || b == null)
            return float.MaxValue;

        float horizontal = HorizontalBaseGap(a, b);
        float vertical = Mathf.Abs(
            CoreRules11Geometry.ModelBasePlaneY(a) -
            CoreRules11Geometry.ModelBasePlaneY(b)
        );

        return Mathf.Sqrt(
            horizontal * horizontal +
            vertical * vertical
        );
    }

    public static bool LineVisibleIgnoringHidden(
        ModelToken observer,
        ModelToken target)
    {
        if (observer == null ||
            target == null ||
            !observer.IsAlive ||
            !target.IsAlive)
        {
            return false;
        }

        Vector3 origin =
            observer.transform.position +
            Vector3.up * 0.5f;

        Vector3 destination =
            target.transform.position +
            Vector3.up * 0.5f;

        Vector3 vector = destination - origin;
        float distance = vector.magnitude;

        if (distance <= 0.001f)
            return true;

        RaycastHit[] hits =
            Physics.RaycastAll(
                origin,
                vector.normalized,
                distance,
                ~0,
                QueryTriggerInteraction.Collide
            );

        foreach (RaycastHit hit
            in hits.OrderBy(value => value.distance))
        {
            if (hit.collider == null)
                continue;

            ModelToken model =
                hit.collider.GetComponentInParent<ModelToken>();

            if (model == observer ||
                model == target)
            {
                continue;
            }

            TerrainFeature terrain =
                hit.collider
                    .GetComponentInParent<TerrainFeature>();

            if (terrain == null)
                continue;

            CoreTerrainCategory11 category =
                Category(terrain);

            if (category ==
                CoreTerrainCategory11.Exposed)
            {
                continue;
            }

            bool observerInside =
                ModelInsideTerrainArea(
                    observer,
                    terrain
                );

            bool targetInside =
                ModelInsideTerrainArea(
                    target,
                    terrain
                );

            // Light/Dense terrain areas obscure models when the sight line
            // crosses the area and neither model is within it. Dense physical
            // geometry additionally remains solid even when one model is
            // inside the terrain area.
            if (!observerInside &&
                !targetInside)
            {
                return false;
            }

            if (category ==
                CoreTerrainCategory11.Dense)
            {
                return false;
            }
        }

        return true;
    }

    public static bool ModelHasTerrainCoverCondition(
        ModelToken attacker,
        ModelToken target)
    {
        if (attacker == null || target == null)
            return false;

        SquadController targetUnit =
            target.Squad != null
            ? target.Squad.JoinedActionController()
            : null;

        bool infantryLike =
            targetUnit != null &&
            (targetUnit.HasKeyword("INFANTRY") ||
             targetUnit.HasKeyword("BEASTS") ||
             targetUnit.HasKeyword("SWARM"));

        if (infantryLike &&
            ModelInsideAnyTerrainArea(target))
        {
            return true;
        }

        // Warboard uses a centre-line physical approximation for "not fully
        // visible due to intervening terrain". It errs on the side of granting
        // the cover condition whenever terrain interrupts that representative
        // line.
        return !LineVisibleIgnoringHidden(
            attacker,
            target
        );
    }

    public static bool MovementDestinationAllowsTerrain(
        SquadController movingUnit,
        TerrainFeature terrain)
    {
        if (terrain == null)
            return true;

        CoreTerrainCategory11 category =
            Category(terrain);

        if (category == CoreTerrainCategory11.Exposed ||
            category == CoreTerrainCategory11.Light)
        {
            return true;
        }

        if (movingUnit == null)
            return false;

        if (CoreRules11FlightRegistry.IsTakingToSkies(
                movingUnit))
        {
            return true;
        }

        return
            movingUnit.HasKeyword("INFANTRY") ||
            movingUnit.HasKeyword("BEASTS") ||
            movingUnit.HasKeyword("SWARM") ||
            movingUnit.HasKeyword("MOBILE");
    }
}

/// <summary>
/// Per-move declaration for 21.03 Flying Models. The registry is deliberately
/// transient: GameController clears it at each phase boundary.
/// </summary>
public static class CoreRules11FlightRegistry
{
    private static readonly HashSet<SquadController>
        takingToSkies =
            new HashSet<SquadController>();

    public static bool IsTakingToSkies(
        SquadController unit)
    {
        if (unit == null)
            return false;

        return takingToSkies.Contains(
            unit.JoinedActionController()
        );
    }

    public static void SetTakingToSkies(
        SquadController unit,
        bool value)
    {
        if (unit == null)
            return;

        unit = unit.JoinedActionController();

        if (value)
            takingToSkies.Add(unit);
        else
            takingToSkies.Remove(unit);
    }

    public static void Clear()
    {
        takingToSkies.Clear();
    }
}

/// <summary>
/// Parses transport capacity and explicit passenger restrictions from the raw
/// datasheet rules retained by YellowScribeImporter.
/// </summary>
public static class CoreRules11TransportRules
{
    private static readonly Dictionary<SquadController, int>
        manualCapacity =
            new Dictionary<SquadController, int>();

    private static readonly Regex[] capacityPatterns =
    {
        new Regex(
            @"transport\s+capacity\s+(?:of\s+)?(?<n>\d+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(
            @"can\s+transport\s+(?:up\s+to\s+)?(?<n>\d+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(
            @"capacity\s*[:\-]?\s*(?<n>\d+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    public static string RuleText(
        SquadController transport)
    {
        if (transport == null ||
            transport.SourceData == null ||
            transport.SourceData.datasheetRules == null)
        {
            return "";
        }

        return string.Join(
            "\n",
            transport.SourceData.datasheetRules
                .Where(rule => rule != null)
                .Select(
                    rule =>
                        (rule.name ?? "") +
                        " " +
                        (rule.text ?? "")
                )
                .ToArray()
        );
    }

    public static int Capacity(
        SquadController transport)
    {
        if (transport == null ||
            !transport.HasKeyword("TRANSPORT"))
        {
            return 0;
        }

        int manual;
        if (manualCapacity.TryGetValue(
                transport.JoinedActionController(),
                out manual) &&
            manual > 0)
        {
            return manual;
        }

        string text = RuleText(transport);

        foreach (Regex pattern in capacityPatterns)
        {
            Match match = pattern.Match(text);
            if (!match.Success)
                continue;

            int result;
            if (int.TryParse(
                    match.Groups["n"].Value,
                    out result))
            {
                return Mathf.Max(0, result);
            }
        }

        return 0;
    }

    public static void SetManualCapacity(
        SquadController transport,
        int capacity)
    {
        if (transport == null)
            return;

        transport = transport.JoinedActionController();

        if (capacity <= 0)
            manualCapacity.Remove(transport);
        else
            manualCapacity[transport] = capacity;
    }

    public static int PassengerCapacityCost(
        SquadController transport,
        SquadController passenger)
    {
        if (transport == null || passenger == null)
            return 0;

        int multiplier = 1;
        string text = RuleText(transport);

        MatchCollection slotRules =
            Regex.Matches(
                text,
                @"each\s+(?<kind>[a-z0-9 '\-]+?)\s+model\s+takes\s+up\s+(?:the\s+)?space\s+of\s+(?<n>\d+)\s+models?",
                RegexOptions.IgnoreCase
            );

        foreach (Match match in slotRules)
        {
            string kind =
                (match.Groups["kind"].Value ?? "")
                    .Trim();

            int slots;
            if (!int.TryParse(
                    match.Groups["n"].Value,
                    out slots))
            {
                continue;
            }

            string normalizedKind =
                WeaponRuleParser.NormalizeRuleName(kind);

            bool applies =
                passenger.HasKeyword(kind) ||
                WeaponRuleParser.NormalizeRuleName(
                    passenger.DisplayName ?? ""
                ).Contains(normalizedKind);

            if (applies)
                multiplier = Mathf.Max(multiplier, slots);
        }

        return
            passenger.JoinedLivingModelTokens().Count *
            Mathf.Max(1, multiplier);
    }

    public static int OccupiedCapacity(
        SquadController transport)
    {
        if (transport == null)
            return 0;

        return transport.EmbarkedPassengers
            .Where(unit => unit != null && unit.IsAlive)
            .Sum(
                unit =>
                    PassengerCapacityCost(
                        transport,
                        unit
                    )
            );
    }

    public static bool CanCarry(
        SquadController transport,
        SquadController passenger,
        out string reason)
    {
        reason = "";

        if (transport == null ||
            passenger == null ||
            transport == passenger)
        {
            reason = "Invalid transport/passenger selection.";
            return false;
        }

        transport = transport.JoinedActionController();
        passenger = passenger.JoinedActionController();

        if (!transport.HasKeyword("TRANSPORT"))
        {
            reason = transport.DisplayName + " is not a TRANSPORT.";
            return false;
        }

        if (transport.FactionId != passenger.FactionId)
        {
            reason = "Passengers must be friendly to the TRANSPORT.";
            return false;
        }

        int capacity = Capacity(transport);
        if (capacity <= 0)
        {
            reason =
                "Transport capacity was not present in the imported datasheet rules for " +
                transport.DisplayName + ".";
            return false;
        }

        int passengerModels =
            PassengerCapacityCost(
                transport,
                passenger
            );

        if (OccupiedCapacity(transport) +
                passengerModels >
            capacity)
        {
            reason = "That TRANSPORT does not have enough remaining capacity.";
            return false;
        }

        string ruleText = RuleText(transport);
        string normalized =
            ruleText.ToUpperInvariant();

        Match allowedClause =
            Regex.Match(
                normalized,
                @"CAN\s+TRANSPORT\s+(?:UP\s+TO\s+)?\d+\s+(?<allowed>[^.\r\n]+)",
                RegexOptions.IgnoreCase
            );

        if (allowedClause.Success)
        {
            string allowed =
                allowedClause.Groups["allowed"].Value;

            string[] typeKeywords =
            {
                "INFANTRY",
                "BEASTS",
                "MOUNTED",
                "VEHICLE",
                "MONSTER"
            };

            List<string> statedTypes =
                typeKeywords
                    .Where(
                        keyword =>
                            Regex.IsMatch(
                                allowed,
                                @"\b" +
                                Regex.Escape(keyword) +
                                @"\b",
                                RegexOptions.IgnoreCase
                            )
                    )
                    .ToList();

            if (statedTypes.Count > 0 &&
                !statedTypes.Any(
                    keyword =>
                        passenger.HasKeyword(keyword)))
            {
                reason =
                    passenger.DisplayName +
                    " does not match the passenger types listed on " +
                    transport.DisplayName + ".";
                return false;
            }
        }

        string[] explicitKeywords =
        {
            "AIRCRAFT",
            "MONSTER",
            "VEHICLE",
            "TERMINATOR",
            "JUMP PACK",
            "MOUNTED",
            "WRAITH CONSTRUCT"
        };

        foreach (string keyword
            in explicitKeywords)
        {
            if (!passenger.HasKeyword(keyword))
                continue;

            string exclusionPattern =
                @"(?:CANNOT\s+TRANSPORT|CANNOT\s+EMBARK|EXCLUDING)[^.\r\n]*\b" +
                Regex.Escape(keyword) +
                @"\b";

            if (Regex.IsMatch(
                    normalized,
                    exclusionPattern,
                    RegexOptions.IgnoreCase))
            {
                reason =
                    transport.DisplayName +
                    " cannot carry " +
                    keyword +
                    " models according to its datasheet.";
                return false;
            }
        }

        return true;
    }
}

public static class CoreRules11Aircraft
{
    public static bool CanDeclareCharge(
        SquadController attacker,
        SquadController target,
        out string reason)
    {
        reason = "";

        if (attacker == null || target == null)
            return false;

        attacker = attacker.JoinedActionController();
        target = target.JoinedActionController();

        if (attacker.HasKeyword("AIRCRAFT"))
        {
            reason = "AIRCRAFT units cannot declare a charge.";
            return false;
        }

        if (target.HasKeyword("AIRCRAFT") &&
            !attacker.HasKeyword("FLY"))
        {
            reason = "Only FLYING units can select an AIRCRAFT as a charge target.";
            return false;
        }

        return true;
    }

    public static bool CanFightTarget(
        SquadController attacker,
        SquadController target)
    {
        if (attacker == null || target == null)
            return false;

        attacker = attacker.JoinedActionController();
        target = target.JoinedActionController();

        if (attacker.HasKeyword("AIRCRAFT") &&
            !target.HasKeyword("FLY"))
        {
            return false;
        }

        if (target.HasKeyword("AIRCRAFT") &&
            !attacker.HasKeyword("FLY"))
        {
            return false;
        }

        return true;
    }
}
