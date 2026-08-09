using System;
using System.Collections.Generic;
using UnityEngine;

public static class CustodesModelPackResolver
{
    private const string IndexResource = "Armies/Models/Custodes/ModelIndex";
    private static bool loadAttempted;
    private static ModelPackIndexData pack;

    private static void EnsureLoaded()
    {
        if (loadAttempted) return;
        loadAttempted = true;
        TextAsset asset = Resources.Load<TextAsset>(IndexResource);
        if (asset == null)
        {
            Debug.LogWarning("Warboard Custodes Model Pack index not found at Resources/" + IndexResource + ".json.");
            return;
        }
        try
        {
            pack = JsonUtility.FromJson<ModelPackIndexData>(asset.text);
            Debug.Log("Warboard Custodes Model Pack loaded: " +
                (pack != null && pack.units != null ? pack.units.Length : 0) + " indexed objects.");
        }
        catch (Exception e)
        {
            pack = null;
            Debug.LogError("Warboard could not read the Custodes Model Pack index: " + e.Message);
        }
    }

    private static string N(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        System.Text.StringBuilder b = new System.Text.StringBuilder();
        foreach (char c in value.ToLowerInvariant()) if (char.IsLetterOrDigit(c)) b.Append(c);
        return b.ToString()
            .Replace("armour", "armor")
            .Replace("ventari", "venatari")
            .Replace("saggitarum", "sagittarum")
	.Replace("aquilon", "aquillon");
    }

    private static string StripPrefixes(string value)
    {
        string v = N(value);
        string[] prefixes = { "adeptuscustodes", "custodes", "legiocustodes", "talonsoftheemperor", "talons" };
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (string p in prefixes)
            {
                if (v.StartsWith(p, StringComparison.OrdinalIgnoreCase) && v.Length > p.Length)
                {
                    v = v.Substring(p.Length);
                    changed = true;
                    break;
                }
            }
        }
        return v;
    }

    private static string Alias(string value)
    {
        string v = StripPrefixes(value);
        v = v.Replace("captaingeneral", "");
        if (v == "trajannvaloris") return "trajannvaloris";
        if (v.Contains("shieldcaptain") && v.Contains("allarus")) return "shieldcaptainallarus";
        if (v.Contains("shieldcaptain") && (v.Contains("dawneagle") || v.Contains("jetbike"))) return "shieldcaptainjetbike";
        if (v.Contains("venerablecontemptor")) return "venerablecontemptor";
        if (v.Contains("custodianguard")) return "custodianguard";
        if (v.Contains("custodianwarden")) return "custodianwarden";
        if (v.Contains("allarus")) return "allarus";
        if (v.Contains("aquillon")) return "aquillon";
        if (v.Contains("vertuspraetor")) return "vertuspraetor";
        if (v.Contains("venatari")) return "venatari";
        if (v.Contains("sagittarum")) return "sagittarum";
        if (v.Contains("bladechampion")) return "bladechampion";
        if (v.Contains("shieldcaptain")) return "shieldcaptain";
        if (v.Contains("trajann")) return "trajannvaloris";
        if (v.Contains("prosecutor")) return "prosecutor";
        if (v.Contains("witchseeker")) return "witchseeker";
        if (v.Contains("vigilator")) return "vigilator";
        if (v.Contains("galatus")) return "galatus";
        if (v.Contains("achillus")) return "achillus";
        if (v.Contains("telemon")) return "telemon";
        if (v.Contains("caladius")) return "caladius";
        if (v.Contains("coronus")) return "coronus";
        if (v.Contains("pallas")) return "pallas";
        if (v.Contains("agamatus")) return "agamatus";
        if (v.Contains("ares")) return "ares";
        if (v.Contains("orion")) return "orion";
        if (v.Contains("landraider")) return "landraider";
        if (v.Contains("rhino")) return "rhino";
        if (v.Contains("jenetiakrole")) return "jenetiakrole";
        if (v.Contains("aleya")) return "aleya";
        if (v.Contains("valerian")) return "valerian";
        return v;
    }

    private static bool GenericRole(string value)
    {
        string v = N(value);
        return string.IsNullOrWhiteSpace(v) || v == "model" || v == "trooper" || v == "warrior" || v == "custodian";
    }

    private static Vector3 V(float[] values, Vector3 fallback)
    {
        if (values == null || values.Length < 3) return fallback;
        return new Vector3(values[0], values[1], values[2]);
    }

    public static ModelVisualDefinition TryResolve(string unitName, string roleName, int modelIndex)
    {
        EnsureLoaded();
        if (pack == null || pack.units == null || pack.units.Length == 0) return null;

        string unit = Alias(unitName);
        string role = Alias(roleName);
        if (string.IsNullOrWhiteSpace(unit)) return null;

        List<ModelPackUnitData> best = new List<ModelPackUnitData>();
        int bestScore = 0;
        foreach (ModelPackUnitData entry in pack.units)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.name) || entry.components == null || entry.components.Length == 0) continue;
            string raw = StripPrefixes(entry.name);
            string name = Alias(entry.name);
            int score = 0;

            if (!GenericRole(roleName) && name == role) score = 1200;
            else if (name == unit) score = 1000;
            else if (!GenericRole(roleName) && raw.Contains(StripPrefixes(roleName))) score = 900;
            else if (!string.IsNullOrWhiteSpace(unit) && (raw.Contains(unit) || unit.Contains(raw))) score = 800;
            else if (!string.IsNullOrWhiteSpace(name) && (name.Contains(unit) || unit.Contains(name))) score = 700;

            // Specialist models should not displace ordinary squad members unless requested.
            string e = N(entry.name);
            if ((e.Contains("vexil") || e.Contains("vexilla")) &&
                !N(unitName).Contains("vexil") && !N(roleName).Contains("vexil")) score -= 250;

            if (score <= 0) continue;
            if (score > bestScore)
            {
                bestScore = score;
                best.Clear();
                best.Add(entry);
            }
            else if (score == bestScore)
            {
                best.Add(entry);
            }
        }

        if (best.Count == 0) return null;
        ModelPackUnitData selected = best[Mathf.Abs(modelIndex) % best.Count];
        List<ModelVisualComponentDefinition> components = new List<ModelVisualComponentDefinition>();
        foreach (ModelPackComponentData c in selected.components)
        {
            if (c == null || string.IsNullOrWhiteSpace(c.meshResource)) continue;
            if (Resources.Load<GameObject>(c.meshResource) == null) continue;
            components.Add(new ModelVisualComponentDefinition(
                c.meshResource,
                c.diffuseResource ?? "",
                c.normalResource ?? "",
                V(c.position, Vector3.zero),
                V(c.rotation, Vector3.zero),
                V(c.scale, Vector3.one)));
        }
        if (components.Count == 0) return null;

        float diameterInches = selected.@base != null && selected.@base.diameterMm > 0.1f
            ? selected.@base.diameterMm / 25.4f
            : 40.0f / 25.4f;
        string baseResource = selected.@base != null ? (selected.@base.resource ?? "") : "";
        return new ModelVisualDefinition(diameterInches, baseResource, components.ToArray());
    }
}
