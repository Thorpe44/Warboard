using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ModelVisualComponentDefinition
{
    public string MeshResource;
    public string DiffuseResource;
    public string NormalResource;

    public Vector3 LocalPosition;
    public Vector3 LocalEuler;
    public Vector3 LocalScale;

    public ModelVisualComponentDefinition(
        string meshResource,
        string diffuseResource,
        string normalResource,
        Vector3 localPosition,
        Vector3 localEuler,
        Vector3 localScale)
    {
        MeshResource = meshResource;
        DiffuseResource = diffuseResource;
        NormalResource = normalResource;
        LocalPosition = localPosition;
        LocalEuler = localEuler;
        LocalScale = localScale;
    }
}

public sealed class ModelVisualDefinition
{
    public float BaseDiameterInches;
    public string BaseMeshResource;
    public ModelVisualComponentDefinition[]
        Components;

    public ModelVisualDefinition(
        float baseDiameterInches,
        string baseMeshResource,
        params ModelVisualComponentDefinition[] components)
    {
        BaseDiameterInches =
            baseDiameterInches;

        BaseMeshResource =
            baseMeshResource;

        Components =
            components ??
            new ModelVisualComponentDefinition[0];
    }
}


[Serializable]
public sealed class ModelPackIndexData
{
    public int formatVersion;
    public string packId;
    public string displayName;
    public string[] sources;
    public ModelPackUnitData[] units;
}

[Serializable]
public sealed class ModelPackUnitData
{
    public string source;
    public string name;
    public string guid;
    public string description;
    public string gmnotes;
    public string[] tags;
    public ModelPackBaseData @base;
    public ModelPackComponentData[] components;
}

[Serializable]
public sealed class ModelPackBaseData
{
    public float diameterMm;
    public string resource;
}

[Serializable]
public sealed class ModelPackComponentData
{
    public string role;
    public string childPath;
    public string nickname;
    public string meshResource;
    public string diffuseResource;
    public string normalResource;
    public float[] position;
    public float[] rotation;
    public float[] scale;
    public string[] objectNames;
}

public static class ModelVisualRegistry
{
    private const string AeldariPackIndexResource =
        "Armies/Models/Aeldari/ModelIndex";

    private static bool packLoadAttempted;
    private static ModelPackIndexData aeldariPack;

    private static void EnsurePackLoaded()
    {
        if (packLoadAttempted)
            return;

        packLoadAttempted = true;

        TextAsset indexAsset =
            Resources.Load<TextAsset>(
                AeldariPackIndexResource
            );

        if (indexAsset == null)
        {
            Debug.LogWarning(
                "Warboard Aeldari Model Pack index was not found at Resources/" +
                AeldariPackIndexResource +
                ".json. Units without a matching pack entry will use gameplay capsules."
            );

            return;
        }

        try
        {
            aeldariPack =
                JsonUtility.FromJson<
                    ModelPackIndexData
                >(indexAsset.text);

            int count =
                aeldariPack != null &&
                aeldariPack.units != null
                ? aeldariPack.units.Length
                : 0;

            Debug.Log(
                "Warboard Aeldari Model Pack loaded from Resources/Armies/Models/Aeldari: " +
                count +
                " indexed objects."
            );
        }
        catch (Exception exception)
        {
            aeldariPack = null;

            Debug.LogError(
                "Warboard could not read the Aeldari Model Pack index: " +
                exception.Message
            );
        }
    }

    private static string ResolvePackResourcePath(
        string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(
                resourcePath))
        {
            return "";
        }

        const string oldAeldariRoot =
            "WarboardModels/Aeldari/";

        const string oldBaseRoot =
            "WarboardModels/Bases/";

        if (resourcePath.StartsWith(
                oldAeldariRoot,
                StringComparison.OrdinalIgnoreCase))
        {
            return
                "Armies/Models/Aeldari/" +
                resourcePath.Substring(
                    oldAeldariRoot.Length
                );
        }

        if (resourcePath.StartsWith(
                oldBaseRoot,
                StringComparison.OrdinalIgnoreCase))
        {
            return
                "Armies/Models/Bases/" +
                resourcePath.Substring(
                    oldBaseRoot.Length
                );
        }

        return resourcePath;
    }

    private static Vector3 V(
        float[] values,
        Vector3 fallback)
    {
        if (values == null ||
            values.Length < 3)
        {
            return fallback;
        }

        return new Vector3(
            values[0],
            values[1],
            values[2]
        );
    }

    private static string PackAlias(
        string normalizedUnit)
    {
        // The built-in test roster is called "Aeldari Wraiths".
        // TryResolvePack strips roster/faction prefixes before this method,
        // so that name arrives here as simply "wraiths". Previously the
        // aeldariwraiths branch below was therefore unreachable and the
        // generic "wraith" stem could match Wraithknights, Wraithlords,
        // Wraithseers and other unrelated pack entries.
        if (normalizedUnit == "wraiths" ||
            normalizedUnit == "aeldariwraiths")
        {
            return "eldarwraithguard";
        }

        if (normalizedUnit == "wraithguard")
            return "eldarwraithguard";

        if (normalizedUnit.Contains(
                "guardiandefenders"))
        {
            return "guardiandefenders";
        }

        return normalizedUnit;
    }

    private static string StripRosterPrefixes(
        string normalizedName)
    {
        if (string.IsNullOrWhiteSpace(
                normalizedName))
        {
            return "";
        }

        string result =
            normalizedName;

        string[] prefixes =
        {
            "ynnari",
            "asuryani",
            "aeldari",
            "drukhari",
            "harlequin"
        };

        bool removed = true;

        while (removed)
        {
            removed = false;

            foreach (string prefix
                in prefixes)
            {
                if (result.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase) &&
                    result.Length >
                        prefix.Length)
                {
                    result =
                        result.Substring(
                            prefix.Length
                        );

                    removed = true;
                    break;
                }
            }
        }

        return result;
    }

    private static string SimpleSingular(
        string normalizedName)
    {
        if (string.IsNullOrWhiteSpace(
                normalizedName))
        {
            return "";
        }

        if (normalizedName.EndsWith(
                "ies",
                StringComparison.OrdinalIgnoreCase) &&
            normalizedName.Length > 3)
        {
            return
                normalizedName.Substring(
                    0,
                    normalizedName.Length - 3
                ) +
                "y";
        }

        if (normalizedName.EndsWith(
                "s",
                StringComparison.OrdinalIgnoreCase) &&
            !normalizedName.EndsWith(
                "ss",
                StringComparison.OrdinalIgnoreCase) &&
            normalizedName.Length > 1)
        {
            return
                normalizedName.Substring(
                    0,
                    normalizedName.Length - 1
                );
        }

        return normalizedName;
    }

    private static string KnownFamilyKey(
        string normalizedName)
    {
        string value =
            SimpleSingular(
                StripRosterPrefixes(
                    normalizedName
                )
            );

        // These family keys are deliberately specific enough not to cross
        // between nearby Aeldari datasheets (e.g. Wraithguard vs Wraithblade).
        string[] families =
        {
            "kabalite",
            "wraithblade",
            "wraithguard",
            "direavenger",
            "strikingscorpion",
            "howlingbanshee",
            "warpider",
            "warpspider",
            "swoopinghawk",
            "firedragon",
            "darkreaper",
            "guardiandefender",
            "stormguardian",
            "ranger",
            "warlock",
            "farseer",
            "windrider",
            "shiningpear",
            "shiningspear",
            "shroudrunner"
        };

        foreach (string family
            in families)
        {
            if (value.Contains(
                    family))
            {
                return family;
            }
        }

        return "";
    }

    private static bool IsGenericRole(
        string normalizedRole)
    {
        return
            string.IsNullOrWhiteSpace(
                normalizedRole) ||
            normalizedRole == "model" ||
            normalizedRole == "trooper" ||
            normalizedRole == "warrior" ||
            normalizedRole == "wraith";
    }

    private static ModelVisualDefinition
        TryResolvePack(
            string unitName,
            string roleName,
            int modelIndex)
    {
        EnsurePackLoaded();

        if (aeldariPack == null ||
            aeldariPack.units == null ||
            aeldariPack.units.Length == 0)
        {
            return null;
        }

        string unit =
            PackAlias(
                StripRosterPrefixes(
                    N(unitName)
                )
            );

        string role =
            StripRosterPrefixes(
                N(roleName)
            );

        string unitSingular =
            SimpleSingular(
                unit
            );

        string roleSingular =
            SimpleSingular(
                role
            );

        string familyKey =
            KnownFamilyKey(
                unit
            );

        List<ModelPackUnitData> exactRole =
            new List<ModelPackUnitData>();

        List<ModelPackUnitData> exactUnit =
            new List<ModelPackUnitData>();

        List<ModelPackUnitData> close =
            new List<ModelPackUnitData>();

        foreach (ModelPackUnitData entry
            in aeldariPack.units)
        {
            if (entry == null ||
                string.IsNullOrWhiteSpace(
                    entry.name) ||
                entry.components == null ||
                entry.components.Length == 0)
            {
                continue;
            }

            string entryName =
                N(entry.name);

            if (!IsGenericRole(role) &&
                entryName == role)
            {
                exactRole.Add(entry);
                continue;
            }

            if (entryName == unit)
            {
                exactUnit.Add(entry);
                continue;
            }

            // Exarch / specialist model roles are often stored as their own
            // TTS object even when the roster unit is the parent squad.
            if (!IsGenericRole(role) &&
                role.Contains("exarch") &&
                entryName.Contains("exarch"))
            {
                string unitStem =
                    unit.Replace(
                        "s",
                        ""
                    );

                string entryStem =
                    entryName.Replace(
                        "s",
                        ""
                    );

                if (entryStem.Contains(
                        unitStem) ||
                    unitStem.Contains(
                        entryStem.Replace(
                            "exarch",
                            ""
                        )))
                {
                    close.Add(entry);
                    continue;
                }
            }

            string entryNoPrefix =
                StripRosterPrefixes(
                    entryName
                );

            string entrySingular =
                SimpleSingular(
                    entryNoPrefix
                );

            string entryFamily =
                KnownFamilyKey(
                    entryNoPrefix
                );

            bool conflictingKnownFamily =
                !string.IsNullOrWhiteSpace(
                    familyKey) &&
                !string.IsNullOrWhiteSpace(
                    entryFamily) &&
                entryFamily != familyKey;

            // Once the roster has identified a specific family, never allow
            // a loose role/stem match to cross into another known family.
            // This stops e.g. Wraithguard/Wraithblade/Wraithlord/Wraithknight
            // visuals being treated as interchangeable simply because their
            // names share "wraith".
            if (conflictingKnownFamily)
                continue;

            // New Recruit / YellowScribe can name the datasheet more
            // generically than the TTS object. Examples:
            //   "Ynnari Kabalite Warriors"
            //       -> "Kabalite Warrior w/ Splinter Cannon"
            //   "Wraithblades"
            //       -> "Wraithblade / Axe, Shield"
            //
            // Prefix/stem matching handles these without hard-coding weapon
            // loadouts into gameplay data.
            if (!string.IsNullOrWhiteSpace(
                    unitSingular) &&
                (entrySingular.StartsWith(
                     unitSingular,
                     StringComparison.OrdinalIgnoreCase) ||
                 unitSingular.StartsWith(
                     entrySingular,
                     StringComparison.OrdinalIgnoreCase)))
            {
                close.Add(entry);
                continue;
            }

            if (!IsGenericRole(role) &&
                !string.IsNullOrWhiteSpace(
                    roleSingular) &&
                entrySingular.Contains(
                    roleSingular))
            {
                close.Add(entry);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(
                    familyKey) &&
                entryFamily == familyKey)
            {
                close.Add(entry);
                continue;
            }

            if (entryName.Contains(unit) ||
                unit.Contains(entryName))
            {
                close.Add(entry);
            }
        }

        List<ModelPackUnitData> candidates =
            exactRole.Count > 0
            ? exactRole
            : exactUnit.Count > 0
                ? exactUnit
                : close;

        if (candidates == null ||
            candidates.Count == 0)
        {
            return null;
        }

        int index =
            Mathf.Abs(modelIndex) %
            candidates.Count;

        ModelPackUnitData selected =
            candidates[index];

        List<ModelVisualComponentDefinition>
            components =
                new List<
                    ModelVisualComponentDefinition
                >();

        foreach (ModelPackComponentData component
            in selected.components)
        {
            if (component == null ||
                string.IsNullOrWhiteSpace(
                    component.meshResource))
            {
                continue;
            }

            components.Add(
                new ModelVisualComponentDefinition(
                    ResolvePackResourcePath(
                        component.meshResource
                    ),
                    ResolvePackResourcePath(
                        component.diffuseResource ?? ""
                    ),
                    ResolvePackResourcePath(
                        component.normalResource ?? ""
                    ),
                    V(
                        component.position,
                        Vector3.zero
                    ),
                    V(
                        component.rotation,
                        Vector3.zero
                    ),
                    V(
                        component.scale,
                        Vector3.one
                    )
                )
            );
        }

        if (components.Count == 0)
            return null;

        // Do not hand an unusable pack definition to ModelToken. A Resources
        // path can exist in ModelIndex.json while Unity has failed to import
        // or locate the corresponding OBJ. In that case return null so the
        // built-in v26 visual can still be used instead of making the token
        // invisible.
        int loadableMeshCount = 0;

        foreach (
            ModelVisualComponentDefinition
                component
            in components)
        {
            if (component == null ||
                string.IsNullOrWhiteSpace(
                    component.MeshResource))
            {
                continue;
            }

            GameObject importedModel =
                Resources.Load<GameObject>(
                    component.MeshResource
                );

            if (importedModel != null)
            {
                loadableMeshCount++;
            }
            else
            {
                Debug.LogWarning(
                    "Warboard Model Pack mesh could not be loaded from Resources: " +
                    component.MeshResource
                );
            }
        }

        if (loadableMeshCount == 0)
        {
            Debug.LogWarning(
                "Warboard Model Pack matched '" +
                unitName +
                "' to '" +
                selected.name +
                "', but none of that entry's OBJ resources could be loaded. " +
                "Using the gameplay capsule fallback."
            );

            return null;
        }

        float baseDiameterInches =
            selected.@base != null &&
            selected.@base.diameterMm > 0.1f
            ? selected.@base.diameterMm /
              25.4f
            : 1.0f;

        string baseResource =
            selected.@base != null
            ? ResolvePackResourcePath(
                selected.@base.resource ?? ""
              )
            : "";

        return
            new ModelVisualDefinition(
                baseDiameterInches,
                baseResource,
                components.ToArray()
            );
    }

    private static string N(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        char[] chars =
            value.ToLowerInvariant()
                .ToCharArray();

        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();

        foreach (char c in chars)
        {
            if (char.IsLetterOrDigit(c))
                builder.Append(c);
        }

        return builder.ToString();
    }

    private static ModelVisualComponentDefinition C(
        string mesh,
        string texture,
        Vector3 position,
        Vector3 euler,
        Vector3 scale)
    {
        return
            new ModelVisualComponentDefinition(
                mesh,
                texture,
                "",
                position,
                euler,
                scale
            );
    }

    public static ModelVisualDefinition Resolve(
        string unitName,
        string roleName,
        int modelIndex)
    {
        // v26.8: the installed faction model pack is the sole canonical
        // source of real miniature visuals. If no indexed asset matches,
        // return null and SquadController keeps the normal gameplay capsule.
        ModelVisualDefinition custodesVisual =
            CustodesModelPackResolver.TryResolve(
                unitName,
                roleName,
                modelIndex
            );

        if (custodesVisual != null)
            return custodesVisual;

        return
            TryResolvePack(
                unitName,
                roleName,
                modelIndex
            );
    }

    private static ModelVisualDefinition RangerA()
    {
        return new ModelVisualDefinition(
            28.5f / 25.4f,
            "WarboardModels/Bases/base_28_5mm",
            C(
                "WarboardModels/Aeldari/Rangers/ranger_a",
                "WarboardModels/Aeldari/Rangers/ranger_a_diffuse",
                new Vector3(
                    0.0000003866f,
                    0.129994035f,
                    0.0000012392f
                ),
                new Vector3(
                    0.0008613275f,
                    0.0261340048f,
                    -0.0004797600f
                ),
                new Vector3(
                    1.00000012f,
                    1.0f,
                    1.00000012f
                )
            )
        );
    }

    private static ModelVisualDefinition RangerB()
    {
        return new ModelVisualDefinition(
            28.5f / 25.4f,
            "WarboardModels/Bases/base_28_5mm",
            C(
                "WarboardModels/Aeldari/Rangers/ranger_b",
                "WarboardModels/Aeldari/Rangers/ranger_b_diffuse",
                new Vector3(
                    0.0000005763f,
                    0.129995942f,
                    0.0000006612f
                ),
                new Vector3(
                    0.000533300f,
                    0.025812993f,
                    0.000095355f
                ),
                new Vector3(
                    1.00000012f,
                    1.0f,
                    1.00000012f
                )
            )
        );
    }

    private static ModelVisualDefinition RangerC()
    {
        return new ModelVisualDefinition(
            28.5f / 25.4f,
            "WarboardModels/Bases/base_28_5mm",
            C(
                "WarboardModels/Aeldari/Rangers/ranger_c",
                "WarboardModels/Aeldari/Rangers/ranger_c_diffuse",
                new Vector3(
                    0.0000008849f,
                    0.129991889f,
                    0.0000000084f
                ),
                new Vector3(
                    -0.000614692f,
                    0.027950842f,
                    0.000026611f
                ),
                new Vector3(
                    1.00000012f,
                    1.0f,
                    1.00000012f
                )
            )
        );
    }

    private static ModelVisualDefinition WraithguardA()
    {
        return new ModelVisualDefinition(
            40.0f / 25.4f,
            "WarboardModels/Bases/base_40_0mm",
            C(
                "WarboardModels/Aeldari/Wraithguard/wraithguard_a",
                "WarboardModels/Aeldari/Wraithguard/wraithguard_a_diffuse",
                new Vector3(
                    0.0000003953f,
                    0.1299938f,
                    0.0000000059f
                ),
                new Vector3(
                    -0.000284168f,
                    357.3206f,
                    0.000141948f
                ),
                new Vector3(
                    0.999999642f,
                    1.0f,
                    0.999999642f
                )
            )
        );
    }

    private static ModelVisualDefinition WraithguardB()
    {
        return new ModelVisualDefinition(
            40.0f / 25.4f,
            "WarboardModels/Bases/base_40_0mm",
            C(
                "WarboardModels/Aeldari/Wraithguard/wraithguard_b",
                "WarboardModels/Aeldari/Wraithguard/wraithguard_b_diffuse",
                new Vector3(
                    0.000000476f,
                    0.129996181f,
                    0.000000018f
                ),
                new Vector3(
                    0.000071924f,
                    359.9487f,
                    -0.0003757f
                ),
                Vector3.one
            )
        );
    }

    private static ModelVisualDefinition WraithguardC()
    {
        return new ModelVisualDefinition(
            40.0f / 25.4f,
            "WarboardModels/Bases/base_40_0mm",
            C(
                "WarboardModels/Aeldari/Wraithguard/wraithguard_c",
                "WarboardModels/Aeldari/Wraithguard/wraithguard_c_diffuse",
                new Vector3(
                    -0.0000000294f,
                    0.129995167f,
                    -0.0000003891f
                ),
                new Vector3(
                    -0.000092545f,
                    0.037543844f,
                    -0.000309912f
                ),
                new Vector3(
                    0.999999762f,
                    1.0f,
                    0.999999762f
                )
            )
        );
    }

    private static ModelVisualDefinition Spiritseer()
    {
        return new ModelVisualDefinition(
            25.0f / 25.4f,
            "WarboardModels/Bases/base_25_0mm",
            C(
                "WarboardModels/Aeldari/Spiritseer/spiritseer",
                "WarboardModels/Aeldari/Spiritseer/spiritseer_diffuse",
                new Vector3(
                    -0.001855104f,
                    0.16013366f,
                    0.001655364f
                ),
                new Vector3(
                    -0.00056731f,
                    262.392029f,
                    -0.00041136f
                ),
                new Vector3(
                    0.8989339f,
                    0.9104841f,
                    0.8989339f
                )
            )
        );
    }

    private static ModelVisualDefinition DireB()
    {
        return new ModelVisualDefinition(
            28.5f / 25.4f,
            "WarboardModels/Bases/base_28_5mm",
            C(
                "WarboardModels/Aeldari/DireAvengers/dire_b",
                "WarboardModels/Aeldari/DireAvengers/dire_b_diffuse",
                new Vector3(
                    0.000001047f,
                    0.129995823f,
                    -0.000000151f
                ),
                new Vector3(
                    0.000244105f,
                    359.992432f,
                    -0.000719733f
                ),
                Vector3.one
            )
        );
    }

    private static ModelVisualDefinition DireC()
    {
        return new ModelVisualDefinition(
            28.5f / 25.4f,
            "WarboardModels/Bases/base_28_5mm",
            C(
                "WarboardModels/Aeldari/DireAvengers/dire_c",
                "WarboardModels/Aeldari/DireAvengers/dire_c_diffuse",
                new Vector3(
                    -0.001929179f,
                    0.130001456f,
                    0.023731358f
                ),
                new Vector3(
                    -0.000570076f,
                    0.166714758f,
                    -0.000210635f
                ),
                new Vector3(
                    0.9999999f,
                    1.0f,
                    0.9999999f
                )
            )
        );
    }

    private static ModelVisualDefinition DireExarchA()
    {
        return new ModelVisualDefinition(
            28.5f / 25.4f,
            "WarboardModels/Bases/base_28_5mm",
            C(
                "WarboardModels/Aeldari/DireAvengers/dire_exarch_a",
                "WarboardModels/Aeldari/DireAvengers/dire_exarch_a_diffuse",
                new Vector3(
                    0.000001120f,
                    0.129992843f,
                    -0.000000412f
                ),
                new Vector3(
                    -0.000625176f,
                    359.947632f,
                    -0.000302431f
                ),
                Vector3.one
            )
        );
    }

    private static ModelVisualDefinition DireExarchD()
    {
        return new ModelVisualDefinition(
            28.5f / 25.4f,
            "WarboardModels/Bases/base_28_5mm",
            C(
                "WarboardModels/Aeldari/DireAvengers/dire_exarch_d",
                "WarboardModels/Aeldari/DireAvengers/dire_exarch_d_diffuse",
                new Vector3(
                    0.000001028f,
                    0.12999332f,
                    -0.000000393f
                ),
                new Vector3(
                    -0.000571331f,
                    359.944183f,
                    -0.000436737f
                ),
                Vector3.one
            )
        );
    }
}

