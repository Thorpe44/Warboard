using System;

[Serializable]
public class WeaponData
{
    public string id;
    public string displayName;
    public float range;
    public int attacksPerModel;

    // Optional dice expressions used by imported roster data,
    // e.g. "D6+1". Built-in JSON can continue using the integer fields.
    public string attacksExpression;

    public int skill;
    public int strength;
    public int ap;
    public int damage;
    public string damageExpression;

    public string[] keywords;

    // Raw profile rule text from imports, kept for the datasheet UI even
    // before every rule has a scripted gameplay implementation.
    public string rawAbilities;
}

[Serializable]
public class DatasheetRuleData
{
    public string name;
    public string text;
}

[Serializable]
public class ModelLoadoutData
{
    public string roleName;
    public int count;

    public int leadership;
    public int objectiveControl;
    public int invulnerableSave;

    // Legacy v6-v11 fields remain valid for hand-authored test JSON.
    public WeaponData rangedWeapon;
    public WeaponData meleeWeapon;

    // Preferred v12 fields: every actual weapon assigned to this model group.
    // Duplicate entries are allowed and represent duplicate ranged weapons.
    public WeaponData[] rangedWeapons;
    public WeaponData[] meleeWeapons;
}

[Serializable]
public class UnitData
{
    public string id;
    public string displayName;
    public string factionId;

    public float move;
    public int toughness;
    public int save;
    public int modelCount;
    public int woundsPerModel;
    public int leadership;
    public int objectiveControl;
    public int invulnerableSave;
    public float modelSpacing;

    // Pre-game / reserve capability.
    public bool canDeepStrike;

    // Generic Leader / attached-unit architecture.
    public bool isLeader;
    public string[] canAttachToIds;

    // Modifiers a Leader grants while attached.
    // Negative skill modifiers improve a 3+ to a 2+, etc.
    public float attachedMoveModifier;
    public int attachedRangedSkillModifier;
    public int attachedMeleeSkillModifier;

    // Backwards-compatible fallback profiles.
    public WeaponData rangedWeapon;
    public WeaponData meleeWeapon;

    // Preferred v6 model-specific equipment.
    public ModelLoadoutData[] loadouts;

    // Gameplay ability IDs used by AbilityRegistry.
    public string[] abilities;

    // Display metadata retained from roster imports.
    public string[] keywords;
    public string[] factionKeywords;
    public DatasheetRuleData[] datasheetRules;
}

[Serializable]
public class ArmyEntryData
{
    public string unitResource;
    public float x;
    public float z;
}

[Serializable]
public class ArmyData
{
    public string factionId;
    public ArmyEntryData[] units;
}
