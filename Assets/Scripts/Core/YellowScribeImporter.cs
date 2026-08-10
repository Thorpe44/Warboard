using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class YellowScribeImportResult
{
    public string Edition;
    public string SourceFaction;
    public List<UnitData> Units =
        new List<UnitData>();

    public int ImportedRangedWeaponInstances;
    public int ImportedMeleeWeaponInstances;
    public int ApproximateCharacteristics;
}

public static class YellowScribeImporter
{
    public static YellowScribeImportResult Parse(
        string json,
        string gameFactionId)
    {
        // v37: clear stale metadata before attempting a new import.
        RosterImportMetadataStore.Clear(
            gameFactionId);

        Dictionary<string, object> root =
            MiniJson.Deserialize(json)
            as Dictionary<string, object>;

        if (root == null)
        {
            throw new Exception(
                "YellowScribe returned invalid JSON."
            );
        }

        string error =
            StringValue(
                Get(root, "err")
            );

        if (!string.IsNullOrWhiteSpace(error))
            throw new Exception(error);

        YellowScribeImportResult result =
            new YellowScribeImportResult();

        result.Edition =
            StringValue(
                Get(root, "edition")
            );

        object armyDataObject =
            Get(root, "armyData");

        Dictionary<string, object> unitsMap =
            armyDataObject
            as Dictionary<string, object>;

        // Also support a raw /getFormattedArmy-style payload.
        if (unitsMap == null)
        {
            unitsMap =
                Get(root, "units")
                as Dictionary<string, object>;
        }

        if (unitsMap == null)
        {
            throw new Exception(
                "YellowScribe payload did not contain armyData/units."
            );
        }

        List<string> order =
            StringList(
                Get(root, "order")
            );

        if (order.Count == 0)
            order.AddRange(unitsMap.Keys);

        Dictionary<string, string> leaderDescriptions =
            new Dictionary<string, string>();

        foreach (string uuid in order)
        {
            Dictionary<string, object> sourceUnit =
                Dict(
                    Get(unitsMap, uuid)
                );

            if (sourceUnit == null)
                continue;

            UnitData converted =
                ConvertUnit(
                    uuid,
                    sourceUnit,
                    gameFactionId,
                    result,
                    leaderDescriptions
                );

            if (converted != null)
                result.Units.Add(converted);
        }

        if (result.Units.Count == 0)
        {
            throw new Exception(
                "No units could be imported from that roster."
            );
        }

        ResolveLeaderAttachments(
            result.Units,
            leaderDescriptions
        );

        ApplyLeaderCompatibilityOverrides(
            result.Units
        );

        result.SourceFaction =
            GuessFactionName(
                unitsMap,
                order
            );

        if (string.IsNullOrWhiteSpace(
            result.SourceFaction))
        {
            result.SourceFaction =
                gameFactionId;
        }

        RosterImportMetadataStore.RecordYellowScribe(

            gameFactionId,

            json,

            result.SourceFaction,

            result.Units);


        return result;
    }

    private static UnitData ConvertUnit(
        string uuid,
        Dictionary<string, object> source,
        string gameFactionId,
        YellowScribeImportResult result,
        Dictionary<string, string> leaderDescriptions)
    {
        string name =
            StringValue(
                Get(source, "name")
            );

        if (string.IsNullOrWhiteSpace(name))
            name = "Imported Unit";

        List<string> keywords =
            StringList(
                Get(source, "keywords")
            );

        List<string> factionKeywords =
            StringList(
                Get(source, "factionKeywords")
            );

        Dictionary<string, object> abilities =
            Dict(
                Get(source, "abilities")
            );

        string leaderRuleText =
            LeaderRuleText(
                abilities
            );

        bool hasLeaderAbility =
            ContainsKeyIgnoreCase(
                abilities,
                "Leader"
            ) ||
            AbilityTextContains(
                abilities,
                "Leader"
            ) ||
            leaderRuleText.IndexOf(
                "attached",
                StringComparison.OrdinalIgnoreCase
            ) >= 0;

        bool isCharacter =
            keywords.Any(
                k =>
                    string.Equals(
                        k,
                        "Character",
                        StringComparison.OrdinalIgnoreCase
                    )
            );

        bool isLeader =
            hasLeaderAbility ||
            (isCharacter &&
             leaderRuleText.IndexOf(
                 "attached",
                 StringComparison.OrdinalIgnoreCase
             ) >= 0);

        bool canDeepStrike =
            keywords.Any(
                k =>
                    k.IndexOf(
                        "Deep Strike",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
            ) ||
            AbilityNameContains(
                abilities,
                "Deep Strike"
            );

        Dictionary<string, object> modelsContainer =
            Dict(
                Get(source, "models")
            );

        Dictionary<string, object> models =
            modelsContainer != null
            ? Dict(
                Get(modelsContainer, "models")
              )
            : null;

        Dictionary<string, object> profiles =
            Dict(
                Get(source, "modelProfiles")
            );

        Dictionary<string, object> weapons =
            Dict(
                Get(source, "weapons")
            );

        if (models == null ||
            models.Count == 0)
        {
            return null;
        }

        List<ModelLoadoutData> loadouts =
            new List<ModelLoadoutData>();

        int totalModels = 0;
        string mostCommonRole = null;
        int mostCommonCount = -1;

        foreach (
            KeyValuePair<string, object> pair
            in models)
        {
            Dictionary<string, object> model =
                Dict(pair.Value);

            if (model == null)
                continue;

            string role =
                StringValue(
                    Get(model, "name")
                );

            if (string.IsNullOrWhiteSpace(role))
                role = name;

            int count =
                Mathf.Max(
                    1,
                    IntValue(
                        Get(model, "number"),
                        1
                    )
                );

            totalModels += count;

            if (count > mostCommonCount)
            {
                mostCommonCount = count;
                mostCommonRole = role;
            }

            List<List<WeaponData>> rangedByModel =
                new List<List<WeaponData>>();

            List<List<WeaponData>> meleeByModel =
                new List<List<WeaponData>>();

            for (int modelIndex = 0;
                 modelIndex < count;
                 modelIndex++)
            {
                rangedByModel.Add(
                    new List<WeaponData>()
                );

                meleeByModel.Add(
                    new List<WeaponData>()
                );
            }

            List<object> modelWeapons =
                Get(model, "weapons")
                as List<object>;

            if (modelWeapons != null)
            {
                foreach (object weaponEntryObj
                    in modelWeapons)
                {
                    Dictionary<string, object> weaponEntry =
                        Dict(weaponEntryObj);

                    if (weaponEntry == null)
                        continue;

                    string weaponName =
                        StringValue(
                            Get(
                                weaponEntry,
                                "name"
                            )
                        );

                    Dictionary<string, object> weaponProfile =
                        weapons != null
                        ? Dict(
                            Get(
                                weapons,
                                weaponName
                            )
                          )
                        : null;

                    if (weaponProfile == null)
                        continue;

                    WeaponData convertedWeapon =
                        ConvertWeapon(
                            uuid,
                            weaponName,
                            weaponProfile
                        );

                    if (convertedWeapon == null)
                        continue;

                    int copies =
                        Mathf.Max(
                            1,
                            IntValue(
                                Get(
                                    weaponEntry,
                                    "number"
                                ),
                                count
                            )
                        );

                    bool melee =
                        IsMeleeWeapon(
                            weaponProfile
                        );

                    // YellowScribe's model group gives a count plus aggregate
                    // weapon copy counts. Round-robin distribution gives the
                    // expected result for the common case:
                    // 5 models + 5 rifles => one rifle each;
                    // 1 model + 2 guns => both guns on that model.
                    for (int copy = 0;
                         copy < copies;
                         copy++)
                    {
                        int modelIndex =
                            copy % count;

                        if (melee)
                        {
                            meleeByModel[
                                modelIndex
                            ].Add(
                                convertedWeapon
                            );

                            result
                                .ImportedMeleeWeaponInstances++;
                        }
                        else
                        {
                            rangedByModel[
                                modelIndex
                            ].Add(
                                convertedWeapon
                            );

                            result
                                .ImportedRangedWeaponInstances++;
                        }
                    }
                }
            }

            Dictionary<string, object> roleProfile =
                FindProfile(
                    profiles,
                    role
                );

            int roleLeadership =
                ParseIntStat(
                    roleProfile,
                    "ld",
                    7,
                    result
                );

            int roleObjectiveControl =
                ParseIntStat(
                    roleProfile,
                    "oc",
                    1,
                    result
                );

            int roleInvulnerableSave =
                ParseOptionalInvulnerableSave(
                    roleProfile
                );

            // Emit count=1 entries so every model can carry a genuinely
            // different collection of weapons and model characteristics.
            for (int modelIndex = 0;
                 modelIndex < count;
                 modelIndex++)
            {
                loadouts.Add(
                    new ModelLoadoutData
                    {
                        roleName = role,
                        count = 1,
                        leadership =
                            roleLeadership,
                        objectiveControl =
                            roleObjectiveControl,
                        rangedWeapon =
                            rangedByModel[
                                modelIndex
                            ].FirstOrDefault(),
                        meleeWeapon =
                            meleeByModel[
                                modelIndex
                            ].FirstOrDefault(),
                        rangedWeapons =
                            rangedByModel[
                                modelIndex
                            ].ToArray(),
                        meleeWeapons =
                            meleeByModel[
                                modelIndex
                            ].ToArray()
                    }
                );
            }
        }

        if (totalModels <= 0)
            return null;

        Dictionary<string, object> mainProfile =
            FindProfile(
                profiles,
                mostCommonRole
            );

        float move =
            ParseFloatStat(
                mainProfile,
                "m",
                6f,
                result
            );

        int toughness =
            ParseIntStat(
                mainProfile,
                "t",
                4,
                result
            );

        int save =
            ParseIntStat(
                mainProfile,
                "sv",
                4,
                result
            );

        int wounds =
            ParseIntStat(
                mainProfile,
                "w",
                1,
                result
            );

        int leadership =
            ParseIntStat(
                mainProfile,
                "ld",
                7,
                result
            );

        int objectiveControl =
            ParseIntStat(
                mainProfile,
                "oc",
                1,
                result
            );

        int invulnerableSave =
            ParseOptionalInvulnerableSave(
                mainProfile
            );

        // YellowScribe's ability text is preserved as identifiers here.
        // Known scripted abilities are registered separately by our engine.
        List<string> abilityNames =
            abilities != null
            ? abilities.Keys.ToList()
            : new List<string>();

        UnitData unit =
            new UnitData
            {
                id =
                    "ys_" +
                    SanitizeId(uuid),

                displayName = name,
                factionId = gameFactionId,
                move = move,
                toughness = toughness,
                save = save,
                modelCount = totalModels,
                woundsPerModel =
                    Mathf.Max(1, wounds),
                leadership =
                    Mathf.Clamp(
                        leadership,
                        2,
                        12
                    ),
                objectiveControl =
                    Mathf.Max(
                        0,
                        objectiveControl
                    ),
                invulnerableSave =
                    invulnerableSave,
                modelSpacing = 1.05f,
                canDeepStrike =
                    canDeepStrike,
                isLeader =
                    isLeader,
                canAttachToIds =
                    new string[0],
                attachedMoveModifier = 0f,
                attachedRangedSkillModifier = 0,
                attachedMeleeSkillModifier = 0,
                loadouts =
                    loadouts.ToArray(),
                abilities =
                    abilityNames.ToArray(),
                keywords =
                    keywords.ToArray(),
                factionKeywords =
                    factionKeywords.ToArray(),
                datasheetRules =
                    ConvertDatasheetRules(
                        abilities
                    )
            };

        if (isLeader &&
            abilities != null &&
            !string.IsNullOrWhiteSpace(
                leaderRuleText))
        {
            leaderDescriptions[
                unit.id
            ] = leaderRuleText;
        }

        return unit;
    }

    private static WeaponData ConvertWeapon(
        string unitUuid,
        string weaponName,
        Dictionary<string, object> profile)
    {
        string rangeText =
            StringValue(
                Get(profile, "range")
            );

        bool melee =
            string.Equals(
                rangeText,
                "Melee",
                StringComparison.OrdinalIgnoreCase
            );

        string attacks =
            StringValue(
                Get(profile, "a")
            );

        // Backwards-compatible 9e-style fallback.
        if (string.IsNullOrWhiteSpace(
            attacks))
        {
            string type =
                StringValue(
                    Get(profile, "type")
                );

            attacks =
                LastToken(type);
        }

        string skill =
            StringValue(
                Get(profile, "bsws")
            );

        string strength =
            StringValue(
                Get(profile, "s")
            );

        string ap =
            StringValue(
                Get(profile, "ap")
            );

        string damage =
            StringValue(
                Get(profile, "d")
            );

        string abilityText =
            StringValue(
                Get(profile, "abilities")
            );

        object profileKeywordsRaw =
            Get(profile, "keywords");

        string profileKeywords =
            profileKeywordsRaw
                is List<object>
            ? string.Join(
                ", ",
                StringList(
                    profileKeywordsRaw
                ).ToArray()
              )
            : StringValue(
                profileKeywordsRaw
              );

        string ruleText =
            string.Join(
                ", ",
                new[]
                {
                    abilityText,
                    profileKeywords
                }
                .Where(
                    value =>
                        !string.IsNullOrWhiteSpace(
                            value
                        )
                )
                .Distinct()
                .ToArray()
            );

        return new WeaponData
        {
            id =
                "ys_" +
                SanitizeId(unitUuid) +
                "_" +
                SanitizeId(weaponName),

            displayName =
                string.IsNullOrWhiteSpace(
                    weaponName
                )
                ? "Imported Weapon"
                : weaponName,

            range =
                melee
                ? 0f
                : ParseLeadingNumber(
                    rangeText,
                    0f
                  ),

            attacksPerModel =
                Mathf.Max(
                    0,
                    ParseFixedOrFallback(
                        attacks,
                        1
                    )
                ),

            attacksExpression =
                IsDiceExpression(attacks)
                ? NormalizeDiceExpression(
                    attacks
                  )
                : null,

            skill =
                Mathf.Clamp(
                    ParseFixedOrFallback(
                        skill,
                        4
                    ),
                    2,
                    6
                ),

            strength =
                Mathf.Max(
                    1,
                    ParseFixedOrFallback(
                        strength,
                        4
                    )
                ),

            ap =
                ParseFixedOrFallback(
                    ap,
                    0
                ),

            damage =
                Mathf.Max(
                    1,
                    ParseFixedOrFallback(
                        damage,
                        1
                    )
                ),

            damageExpression =
                IsDiceExpression(damage)
                ? NormalizeDiceExpression(
                    damage
                  )
                : null,

            keywords =
                ExtractWeaponKeywords(
                    ruleText
                ),

            rawAbilities =
                ruleText
        };
    }

    private static DatasheetRuleData[]
        ConvertDatasheetRules(
            Dictionary<string, object> abilities)
    {
        if (abilities == null ||
            abilities.Count == 0)
        {
            return new DatasheetRuleData[0];
        }

        List<DatasheetRuleData> result =
            new List<DatasheetRuleData>();

        foreach (
            KeyValuePair<string, object> pair
            in abilities)
        {
            result.Add(
                new DatasheetRuleData
                {
                    name = pair.Key,
                    text =
                        AbilityValueText(
                            pair.Value
                        )
                }
            );
        }

        return result.ToArray();
    }

    private static string AbilityValueText(
        object raw)
    {
        if (raw == null)
            return "";

        string direct =
            raw as string;

        if (direct != null)
            return direct;

        Dictionary<string, object> ability =
            Dict(raw);

        if (ability == null)
            return StringValue(raw);

        string[] preferredKeys =
        {
            "desc",
            "text",
            "description",
            "value"
        };

        foreach (string key
            in preferredKeys)
        {
            string value =
                StringValue(
                    Get(
                        ability,
                        key
                    )
                );

            if (!string.IsNullOrWhiteSpace(
                value))
            {
                return value;
            }
        }

        return string.Join(
            " ",
            ability.Values
                .Select(StringValue)
                .Where(
                    value =>
                        !string.IsNullOrWhiteSpace(
                            value
                        )
                )
                .ToArray()
        );
    }

    private static string LeaderRuleText(
        Dictionary<string, object> abilities)
    {
        if (abilities == null)
            return "";

        List<string> pieces =
            new List<string>();

        foreach (
            KeyValuePair<string, object> pair
            in abilities)
        {
            string text =
                AbilityValueText(
                    pair.Value
                );

            bool relevant =
                pair.Key.IndexOf(
                    "Leader",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0 ||
                text.IndexOf(
                    "attached",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0 ||
                text.IndexOf(
                    "Leader",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0;

            if (relevant)
            {
                pieces.Add(
                    pair.Key +
                    ": " +
                    text
                );
            }
        }

        return string.Join(
            " ",
            pieces.ToArray()
        );
    }

    private static void ApplyLeaderCompatibilityOverrides(
        List<UnitData> units)
    {
        if (units == null ||
            units.Count == 0)
        {
            return;
        }

        foreach (UnitData leader
            in units)
        {
            IReadOnlyList<string> bodyguardNames =
                LeaderCompatibilityRegistry
                    .GetBodyguardNames(
                        leader.displayName
                    );

            if (bodyguardNames == null ||
                bodyguardNames.Count == 0)
            {
                continue;
            }

            // The compatibility table itself is authoritative enough to mark
            // the imported unit as a Leader even if YellowScribe only exposed
            // the generic Core rule.
            leader.isLeader = true;

            HashSet<string> compatibleIds =
                new HashSet<string>(
                    leader.canAttachToIds ??
                    new string[0]
                );

            foreach (string wantedName
                in bodyguardNames)
            {
                string wanted =
                    LeaderCompatibilityRegistry
                        .Normalize(
                            wantedName
                        );

                foreach (UnitData candidate
                    in units)
                {
                    if (candidate == null ||
                        candidate == leader)
                    {
                        continue;
                    }

                    string candidateName =
                        LeaderCompatibilityRegistry
                            .Normalize(
                                candidate.displayName
                            );

                    if (candidateName ==
                        wanted)
                    {
                        compatibleIds.Add(
                            candidate.id
                        );
                    }
                }
            }

            leader.canAttachToIds =
                compatibleIds.ToArray();
        }
    }

    private static void ResolveLeaderAttachments(
        List<UnitData> units,
        Dictionary<string, string> leaderDescriptions)
    {
        foreach (UnitData leader in units)
        {
            if (!leader.isLeader)
                continue;

            string description;

            if (!leaderDescriptions.TryGetValue(
                leader.id,
                out description))
            {
                continue;
            }

            List<string> attachIds =
                new List<string>();

            foreach (UnitData candidate
                in units)
            {
                if (candidate == leader ||
                    candidate.isLeader)
                {
                    continue;
                }

                if (LeaderTextNamesCandidate(
                    description,
                    candidate))
                {
                    attachIds.Add(
                        candidate.id
                    );
                }
            }

            leader.canAttachToIds =
                attachIds
                    .Distinct()
                    .ToArray();
        }
    }

    private static bool LeaderTextNamesCandidate(
        string leaderText,
        UnitData candidate)
    {
        if (string.IsNullOrWhiteSpace(
                leaderText) ||
            candidate == null ||
            string.IsNullOrWhiteSpace(
                candidate.displayName))
        {
            return false;
        }

        string normalizedRule =
            NormalizeRuleText(
                leaderText
            );

        List<string> names =
            new List<string>
            {
                candidate.displayName
            };

        if (candidate.factionKeywords != null)
        {
            foreach (string factionKeyword
                in candidate.factionKeywords)
            {
                if (string.IsNullOrWhiteSpace(
                    factionKeyword))
                {
                    continue;
                }

                string prefix =
                    factionKeyword.Trim() +
                    " ";

                if (candidate.displayName
                    .StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    names.Add(
                        candidate.displayName
                            .Substring(
                                prefix.Length
                            )
                    );
                }
            }
        }

        string[] knownDisplayPrefixes =
        {
            "Ynnari ",
            "Aeldari "
        };

        foreach (string prefix
            in knownDisplayPrefixes)
        {
            if (candidate.displayName
                .StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                names.Add(
                    candidate.displayName
                        .Substring(
                            prefix.Length
                        )
                );
            }
        }

        foreach (string name
            in names
                .Where(
                    value =>
                        !string.IsNullOrWhiteSpace(
                            value
                        )
                )
                .Distinct(
                    StringComparer.OrdinalIgnoreCase
                ))
        {
            string normalizedName =
                NormalizeRuleText(name);

            if (normalizedName.Length > 2 &&
                normalizedRule.Contains(
                    normalizedName))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeRuleText(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
            value))
        {
            return "";
        }

        char[] chars =
            value
                .ToLowerInvariant()
                .Select(
                    c =>
                        char.IsLetterOrDigit(c)
                        ? c
                        : ' '
                )
                .ToArray();

        return string.Join(
            " ",
            new string(chars)
                .Split(
                    new[] { ' ' },
                    StringSplitOptions
                        .RemoveEmptyEntries
                )
        );
    }

    private static Dictionary<string, object>
        FindProfile(
            Dictionary<string, object> profiles,
            string modelName)
    {
        if (profiles == null ||
            profiles.Count == 0)
        {
            return null;
        }

        object exact;

        if (!string.IsNullOrWhiteSpace(
                modelName) &&
            TryGetIgnoreCase(
                profiles,
                modelName,
                out exact))
        {
            return Dict(exact);
        }

        return Dict(
            profiles.First().Value
        );
    }

    private static int ParseIntStat(
        Dictionary<string, object> profile,
        string key,
        int fallback,
        YellowScribeImportResult result)
    {
        if (profile == null)
        {
            result.ApproximateCharacteristics++;
            return fallback;
        }

        string text =
            StringValue(
                Get(profile, key)
            );

        int parsed =
            ParseFixedOrFallback(
                text,
                int.MinValue
            );

        if (parsed ==
            int.MinValue)
        {
            result.ApproximateCharacteristics++;
            return fallback;
        }

        return parsed;
    }

    private static int ParseOptionalInvulnerableSave(
        Dictionary<string, object> profile)
    {
        if (profile == null)
            return 0;

        string[] keys =
        {
            "insv",
            "invsv",
            "inv",
            "invulnerable save"
        };

        foreach (string key in keys)
        {
            string text =
                StringValue(
                    Get(
                        profile,
                        key
                    )
                );

            int parsed =
                ParseFixedOrFallback(
                    text,
                    0
                );

            if (parsed > 0)
            {
                return Mathf.Clamp(
                    parsed,
                    2,
                    6
                );
            }
        }

        return 0;
    }

    private static float ParseFloatStat(
        Dictionary<string, object> profile,
        string key,
        float fallback,
        YellowScribeImportResult result)
    {
        if (profile == null)
        {
            result.ApproximateCharacteristics++;
            return fallback;
        }

        string text =
            StringValue(
                Get(profile, key)
            );

        float parsed =
            ParseLeadingNumber(
                text,
                float.NaN
            );

        if (float.IsNaN(parsed))
        {
            result.ApproximateCharacteristics++;
            return fallback;
        }

        return parsed;
    }

    private static bool IsMeleeWeapon(
        Dictionary<string, object> profile)
    {
        string range =
            StringValue(
                Get(profile, "range")
            );

        return string.Equals(
            range,
            "Melee",
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static string[] ExtractWeaponKeywords(
        string abilityText)
    {
        if (string.IsNullOrWhiteSpace(
            abilityText))
        {
            return new string[0];
        }

        string lower =
            abilityText
                .ToLowerInvariant()
                .Replace('\u2011', '-')
            .Replace('\u2013', '-')
            .Replace('\u2014', '-');

        List<string> keywords =
            new List<string>();

        AddKeywordIf(
            keywords,
            lower.Contains("torrent"),
            "torrent"
        );

        AddKeywordIf(
            keywords,
            lower.Contains("lethal hits"),
            "lethal_hits"
        );

        AddKeywordIf(
            keywords,
            lower.Contains("twin-linked") ||
            lower.Contains("twin linked"),
            "twin_linked"
        );

        AddKeywordIf(
            keywords,
            lower.Contains("pistol") ||
            lower.Contains("close-quarters") ||
            lower.Contains("close quarters"),
            "pistol"
        );

        AddKeywordIf(
            keywords,
            lower.Contains("extra attacks"),
            "extra_attacks"
        );

        AddKeywordIf(
            keywords,
            lower.Contains("devastating wounds"),
            "devastating_wounds"
        );

        AddKeywordIf(
            keywords,
            lower.Contains("precision"),
            "precision"
        );

        AddKeywordIf(
            keywords,
            lower.Contains("hazardous"),
            "hazardous"
        );

        AddKeywordIf(
            keywords,
            lower.Contains("assault"),
            "assault"
        );

        AddKeywordIf(
            keywords,
            lower.Contains("heavy"),
            "heavy"
        );

        AddKeywordIf(
            keywords,
            lower.Contains("ignores cover"),
            "ignores_cover"
        );

        AddKeywordIf(
            keywords,
            lower.Contains("indirect fire"),
            "indirect_fire"
        );

        AddKeywordIf(
            keywords,
            lower.Contains("lance"),
            "lance"
        );

        System.Text.RegularExpressions.Match cleave =
            System.Text.RegularExpressions
                .Regex.Match(
                    lower,
                    @"cleave\s*(\d+)"
                );

        if (cleave.Success)
        {
            keywords.Add(
                "cleave_" +
                cleave.Groups[1].Value
            );
        }

        System.Text.RegularExpressions.Match sustained =
            System.Text.RegularExpressions
                .Regex.Match(
                    lower,
                    @"sustained[\s\-]*hits\s*(\d+)"
                );

        if (sustained.Success)
        {
            keywords.Add(
                "sustained_hits_" +
                sustained.Groups[1].Value
            );
        }

        System.Text.RegularExpressions.Match rapid =
            System.Text.RegularExpressions
                .Regex.Match(
                    lower,
                    @"rapid[\s\-]*fire\s*(\d+)"
                );

        if (rapid.Success)
        {
            keywords.Add(
                "rapid_fire_" +
                rapid.Groups[1].Value
            );
        }

        System.Text.RegularExpressions.Match blast =
            System.Text.RegularExpressions
                .Regex.Match(
                    lower,
                    @"\bblast(?:\s+(\d+))?\b"
                );

        if (blast.Success)
        {
            string value =
                blast.Groups[1].Success
                ? blast.Groups[1].Value
                : "1";

            keywords.Add(
                "blast_" +
                value
            );
        }

        System.Text.RegularExpressions.Match melta =
            System.Text.RegularExpressions
                .Regex.Match(
                    lower,
                    @"\bmelta\s*(\d+)"
                );

        if (melta.Success)
        {
            keywords.Add(
                "melta_" +
                melta.Groups[1].Value
            );
        }

        System.Text.RegularExpressions.MatchCollection antiMatches =
            System.Text.RegularExpressions
                .Regex.Matches(
                    lower,
                    @"anti[\s\-]+([a-z][a-z0-9\s\-]*?)\s+([2-6])\+"
                );

        foreach (
            System.Text.RegularExpressions.Match match
            in antiMatches)
        {
            string target =
                WeaponRuleParser
                    .NormalizeRuleName(
                        match.Groups[1].Value
                    );

            keywords.Add(
                "anti_" +
                target +
                "_" +
                match.Groups[2].Value
            );
        }

        return keywords
            .Distinct()
            .ToArray();
    }

    private static void AddKeywordIf(
        List<string> keywords,
        bool condition,
        string keyword)
    {
        if (condition &&
            !keywords.Contains(keyword))
        {
            keywords.Add(keyword);
        }
    }

    private static string GuessFactionName(
        Dictionary<string, object> units,
        List<string> order)
    {
        foreach (string uuid in order)
        {
            Dictionary<string, object> unit =
                Dict(
                    Get(units, uuid)
                );

            if (unit == null)
                continue;

            List<string> keywords =
                StringList(
                    Get(
                        unit,
                        "factionKeywords"
                    )
                );

            if (keywords.Count > 0)
                return keywords[
                    keywords.Count - 1
                ];
        }

        return "";
    }

    private static bool AbilityNameContains(
        Dictionary<string, object> abilities,
        string text)
    {
        if (abilities == null)
            return false;

        return abilities.Keys.Any(
            name =>
                name.IndexOf(
                    text,
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
        );
    }

    private static bool AbilityTextContains(
        Dictionary<string, object> abilities,
        string text)
    {
        if (abilities == null)
            return false;

        foreach (
            KeyValuePair<string, object> pair
            in abilities)
        {
            string combined =
                pair.Key +
                " " +
                AbilityValueText(
                    pair.Value
                );

            if (combined.IndexOf(
                    text,
                    StringComparison.OrdinalIgnoreCase
                ) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string AbilityDescription(
        Dictionary<string, object> abilities,
        string name)
    {
        if (abilities == null)
            return "";

        object raw;

        if (!TryGetIgnoreCase(
            abilities,
            name,
            out raw))
        {
            return "";
        }

        return AbilityValueText(raw);
    }

    private static bool ContainsKeyIgnoreCase(
        Dictionary<string, object> dict,
        string key)
    {
        object ignored;

        return TryGetIgnoreCase(
            dict,
            key,
            out ignored
        );
    }

    private static bool TryGetIgnoreCase(
        Dictionary<string, object> dict,
        string key,
        out object value)
    {
        value = null;

        if (dict == null)
            return false;

        foreach (
            KeyValuePair<string, object> pair
            in dict)
        {
            if (string.Equals(
                pair.Key,
                key,
                StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        return false;
    }

    private static object Get(
        Dictionary<string, object> dict,
        string key)
    {
        if (dict == null)
            return null;

        object value;

        return dict.TryGetValue(
            key,
            out value)
            ? value
            : null;
    }

    private static Dictionary<string, object> Dict(
        object value)
    {
        return value
            as Dictionary<string, object>;
    }

    private static List<string> StringList(
        object value)
    {
        List<object> list =
            value as List<object>;

        if (list == null)
            return new List<string>();

        return list
            .Select(StringValue)
            .Where(
                item =>
                    !string.IsNullOrWhiteSpace(
                        item
                    )
            )
            .ToList();
    }

    private static string StringValue(
        object value)
    {
        return value == null
            ? ""
            : Convert.ToString(
                value,
                System.Globalization
                    .CultureInfo.InvariantCulture
              );
    }

    private static int IntValue(
        object value,
        int fallback)
    {
        if (value == null)
            return fallback;

        int parsed;

        return int.TryParse(
            StringValue(value),
            out parsed)
            ? parsed
            : fallback;
    }

    private static int ParseFixedOrFallback(
        string text,
        int fallback)
    {
        if (string.IsNullOrWhiteSpace(
            text))
        {
            return fallback;
        }

        string normalized =
            text
                .Trim()
                .Replace("+", "")
                .Replace("\"", "");

        int parsed;

        if (int.TryParse(
            normalized,
            out parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static float ParseLeadingNumber(
        string text,
        float fallback)
    {
        if (string.IsNullOrWhiteSpace(
            text))
        {
            return fallback;
        }

        string normalized =
            text.Trim();

        string digits = "";

        foreach (char c in normalized)
        {
            if (char.IsDigit(c) ||
                c == '-' ||
                c == '.')
            {
                digits += c;
            }
            else if (digits.Length > 0)
            {
                break;
            }
        }

        float parsed;

        if (float.TryParse(
            digits,
            System.Globalization
                .NumberStyles.Float,
            System.Globalization
                .CultureInfo.InvariantCulture,
            out parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static bool IsDiceExpression(
        string text)
    {
        if (string.IsNullOrWhiteSpace(
            text))
        {
            return false;
        }

        return text.IndexOf(
            'D',
            StringComparison.OrdinalIgnoreCase
        ) >= 0;
    }

    private static string NormalizeDiceExpression(
        string text)
    {
        return text
            .Trim()
            .ToUpperInvariant()
            .Replace(" ", "");
    }

    private static string LastToken(
        string text)
    {
        if (string.IsNullOrWhiteSpace(
            text))
        {
            return "";
        }

        string[] parts =
            text.Split(
                new[] { ' ' },
                StringSplitOptions
                    .RemoveEmptyEntries
            );

        return parts.Length > 0
            ? parts[
                parts.Length - 1
              ]
            : "";
    }

    private static string SanitizeId(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
            value))
        {
            return "unnamed";
        }

        char[] chars =
            value
                .ToLowerInvariant()
                .Select(
                    c =>
                        char.IsLetterOrDigit(c)
                        ? c
                        : '_'
                )
                .ToArray();

        return new string(chars);
    }
}
