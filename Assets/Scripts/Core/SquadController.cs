using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SquadBattlefieldState
{
    Undeployed,
    Battlefield,
    Reserves
}

public class SquadController : MonoBehaviour
{
    public string UnitId { get; private set; }
    public string DisplayName { get; private set; }
    public string FactionId { get; private set; }

    public bool IsLeaderUnit { get; private set; }
    public string[] CanAttachToIds { get; private set; }

    public float AttachedMoveModifier { get; private set; }
    public int AttachedRangedSkillModifier { get; private set; }
    public int AttachedMeleeSkillModifier { get; private set; }

    public SquadController AttachedLeader { get; private set; }
    public SquadController AttachedBodyguard { get; private set; }

    public bool IsAttachedLeader
    {
        get { return AttachedBodyguard != null; }
    }

    public float BaseMove { get; private set; }
    public int Toughness { get; private set; }
    public int BaseSave { get; private set; }
    public int StartingModels { get; private set; }
    public int WoundsPerModel { get; private set; }

    public int BaseLeadership { get; private set; }
    public int BaseObjectiveControl { get; private set; }
    public int BaseInvulnerableSave { get; private set; }
    public bool CanDeepStrike { get; private set; }
    public bool TemporaryDeepStrike { get; set; }

    public bool HasDeepStrike
    {
        get { return CanDeepStrike || TemporaryDeepStrike; }
    }

    public SquadBattlefieldState BattlefieldState { get; private set; }
    public bool IsOnBattlefield
    {
        get { return BattlefieldState == SquadBattlefieldState.Battlefield; }
    }

    public bool IsInReserves
    {
        get { return BattlefieldState == SquadBattlefieldState.Reserves; }
    }

    public bool HasMoved { get; set; }
    public bool HasShot { get; set; }
    public bool HasCharged { get; set; }
    public bool HasFought { get; set; }
    public bool HasFallenBack { get; set; }

    public bool StartedMissionActionThisTurn
    {
        get;
        private set;
    }

    public bool IsPerformingMissionAction
    {
        get;
        private set;
    }

    public string ActiveMissionActionId
    {
        get;
        private set;
    } = "";

    public bool HasAdvanced { get; private set; }
    public int AdvanceBonus { get; private set; }

    public bool IsBattleShocked { get; private set; }
    public int LastBattleShockRoll { get; private set; }

    public bool WasSetUpThisTurn { get; private set; }
    public bool MadeChargeMove { get; private set; }

    public bool FactionSoulsightActive { get; set; }
    public bool FactionMacabreResilienceActive { get; set; }
    public bool FactionEmissariesRerollOnes { get; set; }
    public bool FactionEmissariesRerollAll { get; set; }
    public bool FactionHungryVoidActive { get; set; }
    public bool FactionSuddenStormActive { get; set; }
    public bool FactionConqueringTyrantActive { get; set; }
    public bool TargetedByFactionStratagemThisPhase { get; set; }

    public bool TemporaryFightsFirst { get; set; }
    public bool PartingTheVeilActive { get; set; }

    // Generic Aeldari detachment/Stratagem effect state. These are kept
    // deliberately data-shaped so every detachment does not need another
    // bespoke attack pipeline.
    public int AeldariOffensiveHitModifier { get; set; }
    public int AeldariOffensiveWoundModifier { get; set; }
    public int AeldariDefensiveHitModifier { get; set; }
    public int AeldariDefensiveWoundModifier { get; set; }
    public int AeldariApModifier { get; set; }
    public int AeldariDamageModifier { get; set; }
    public int AeldariInvulnerableOverride { get; set; }
    public int AeldariSustainedHits { get; set; }
    public bool AeldariLethalHits { get; set; }
    public bool AeldariDevastatingWounds { get; set; }
    public bool AeldariIgnoresCover { get; set; }
    public bool AeldariRerollHitOnes { get; set; }
    public bool AeldariRerollWoundOnes { get; set; }
    public bool AeldariRerollAllHits { get; set; }
    public bool AeldariRerollAllWounds { get; set; }
    public bool AeldariPathChoiceMadeThisPhase { get; set; }
    public bool AeldariCanShootAfterFallBack { get; set; }
    public bool AeldariCanChargeAfterFallBack { get; set; }
    public bool AeldariCanChargeAfterAdvance { get; set; }
    public bool AeldariRange18Protection { get; set; }
    public bool AeldariVectoredEnginesActive { get; set; }
    public int AeldariVengefulDeadTokens { get; set; }
    public int AeldariObjectiveControlOverride { get; set; }

    public bool AgileManoeuvreUsedThisPhase { get; set; }
    public bool FlittingShadowsActive { get; set; }
    public bool StarEnginesActive { get; set; }
    public bool SuddenStrikeActive { get; set; }
    public bool SnapShootingActive { get; set; }
    public bool KatahSustainedActive { get; set; }
    public bool KatahLethalActive { get; set; }
    public bool KatahChoiceMadeThisFight { get; set; }
    public float BattleFocusMoveBonus { get; set; }

    public bool ResurrectionOrbUsed { get; set; }
    public bool EternalRevenantUsed { get; set; }
    public bool VeilOfDarknessUsed { get; set; }
    public bool MustIngressFromVeil { get; set; }
    public bool IsRepositionedReserve { get; private set; }
    public bool TearsOfIshaUsedThisTurn { get; set; }
    public bool SpiritMarkUsedThisTurn { get; set; }
    public SquadController SpiritMarkTarget { get; set; }
    public int MyWillBeDoneUsedRound { get; set; }

    public bool IsAlive
    {
        get { return LivingModels > 0; }
    }

    public int LivingModels
    {
        get { return models.Count(m => m.IsAlive); }
    }

    private readonly List<ModelToken> models =
        new List<ModelToken>();

    private readonly List<IUnitAbility> abilities =
        new List<IUnitAbility>();

    private float spacing;
    private bool selected;
    private bool wholeSquadHighlighted;
    private ModelToken focusedModel;
    private Color baseColor;

    private UnitData sourceData;

    public UnitData SourceData
    {
        get { return sourceData; }
    }

    public int JoinedLivingModels
    {
        get
        {
            return JoinedLivingModelTokens().Count;
        }
    }

    public void Initialize(UnitData data)
    {
        sourceData = data;

        UnitId = data.id;
        DisplayName = data.displayName;
        FactionId = data.factionId;

        IsLeaderUnit = data.isLeader;
        CanAttachToIds =
            data.canAttachToIds ??
            new string[0];

        AttachedMoveModifier =
            data.attachedMoveModifier;

        AttachedRangedSkillModifier =
            data.attachedRangedSkillModifier;

        AttachedMeleeSkillModifier =
            data.attachedMeleeSkillModifier;

        BaseMove = data.move;
        Toughness = data.toughness;
        BaseSave = data.save;
        StartingModels = Mathf.Max(1, data.modelCount);
        WoundsPerModel = Mathf.Max(1, data.woundsPerModel);
        BaseLeadership =
            data.leadership > 0
            ? data.leadership
            : 7;
        BaseObjectiveControl =
            data.objectiveControl > 0
            ? data.objectiveControl
            : 1;

        BaseInvulnerableSave =
            data.invulnerableSave > 0
            ? data.invulnerableSave
            : 0;

        spacing = Mathf.Max(0.9f, data.modelSpacing);
        CanDeepStrike = data.canDeepStrike;
        BattlefieldState = SquadBattlefieldState.Battlefield;

        baseColor =
            GameController.FactionColor(FactionId);

        if (data.abilities != null)
        {
            foreach (string id in data.abilities)
            {
                IUnitAbility ability =
                    AbilityRegistry.Create(id);

                if (ability != null)
                    abilities.Add(ability);
            }
        }

        CreateModels();
    }

    private void CreateModels()
    {
        List<ModelLoadoutData> expanded =
            ExpandLoadouts();

        // Registered real miniature bases can be wider than the prototype's
        // original capsule spacing. Build the initial formation using the
        // largest registered base in the unit so models never start overlapped.
        float layoutSpacing =
            spacing;

        for (int i = 0;
             i < StartingModels;
             i++)
        {
            ModelLoadoutData previewLoadout =
                i < expanded.Count
                ? expanded[i]
                : null;

            string previewRoleName =
                previewLoadout != null
                ? previewLoadout.roleName
                : "Model";

            ModelVisualDefinition previewVisual =
                ModelVisualRegistry.Resolve(
                    DisplayName,
                    previewRoleName,
                    i
                );

            if (previewVisual != null)
            {
                layoutSpacing =
                    Mathf.Max(
                        layoutSpacing,
                        previewVisual
                            .BaseDiameterInches +
                        0.08f
                    );
            }
        }

        int columns =
            Mathf.CeilToInt(
                Mathf.Sqrt(StartingModels)
            );

        for (int i = 0; i < StartingModels; i++)
        {
            int row = i / columns;
            int col = i % columns;

            float x =
                (col - (columns - 1) * 0.5f) *
                layoutSpacing;

            float z =
                row *
                layoutSpacing;

            GameObject model =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule
                );

            ModelLoadoutData loadout =
                i < expanded.Count
                ? expanded[i]
                : null;

            string roleName =
                loadout != null
                ? loadout.roleName
                : "Model";

            model.name =
                roleName +
                "_" +
                (i + 1);

            model.transform.SetParent(
                transform,
                false
            );

            model.transform.localPosition =
                new Vector3(
                    x,
                    0.65f,
                    z
                );

            model.transform.localScale =
                IsLeaderUnit
                ? new Vector3(
                    0.82f,
                    1.02f,
                    0.82f
                  )
                : new Vector3(
                    0.7f,
                    0.85f,
                    0.7f
                  );

            GameController.SetObjectColor(
                model,
                baseColor
            );

            WeaponData[] ranged =
                ResolveLoadoutWeapons(
                    loadout != null
                        ? loadout.rangedWeapons
                        : null,
                    loadout != null
                        ? loadout.rangedWeapon
                        : null,
                    sourceData.rangedWeapon
                );

            WeaponData[] melee =
                ResolveLoadoutWeapons(
                    loadout != null
                        ? loadout.meleeWeapons
                        : null,
                    loadout != null
                        ? loadout.meleeWeapon
                        : null,
                    sourceData.meleeWeapon
                );

            ModelToken token =
                model.AddComponent<ModelToken>();

            int leadership =
                loadout != null &&
                loadout.leadership > 0
                ? loadout.leadership
                : BaseLeadership;

            int objectiveControl =
                loadout != null &&
                loadout.objectiveControl > 0
                ? loadout.objectiveControl
                : BaseObjectiveControl;

            int invulnerableSave =
                loadout != null &&
                loadout.invulnerableSave > 0
                ? loadout.invulnerableSave
                : BaseInvulnerableSave;

            token.Initialize(
                this,
                WoundsPerModel,
                roleName,
                ranged,
                melee,
                leadership,
                objectiveControl,
                invulnerableSave
            );

            ModelVisualDefinition visual =
                ModelVisualRegistry.Resolve(
                    DisplayName,
                    roleName,
                    i
                );

            if (visual != null)
            {
                token.AttachVisual(
                    visual,
                    baseColor
                );
            }

            models.Add(token);
        }
    }

    private WeaponData[] ResolveLoadoutWeapons(
        WeaponData[] multi,
        WeaponData legacy,
        WeaponData unitFallback)
    {
        if (multi != null &&
            multi.Length > 0)
        {
            return multi
                .Where(
                    weapon =>
                        weapon != null
                )
                .ToArray();
        }

        if (legacy != null)
            return new[] { legacy };

        if (unitFallback != null)
            return new[] { unitFallback };

        return new WeaponData[0];
    }

    private List<ModelLoadoutData> ExpandLoadouts()
    {
        List<ModelLoadoutData> result =
            new List<ModelLoadoutData>();

        if (sourceData.loadouts != null)
        {
            foreach (ModelLoadoutData loadout
                in sourceData.loadouts)
            {
                if (loadout == null)
                    continue;

                int count =
                    Mathf.Max(0, loadout.count);

                for (int i = 0; i < count; i++)
                    result.Add(loadout);
            }
        }

        // If loadouts are incomplete, fill remaining models
        // with the unit-level fallback weapon profiles.
        while (result.Count < StartingModels)
        {
            result.Add(
                new ModelLoadoutData
                {
                    roleName = "Model",
                    count = 1,
                    rangedWeapon = sourceData.rangedWeapon,
                    meleeWeapon = sourceData.meleeWeapon,
                    rangedWeapons =
                        sourceData.rangedWeapon != null
                        ? new[] { sourceData.rangedWeapon }
                        : new WeaponData[0],
                    meleeWeapons =
                        sourceData.meleeWeapon != null
                        ? new[] { sourceData.meleeWeapon }
                        : new WeaponData[0]
                }
            );
        }

        if (result.Count > StartingModels)
            result = result.Take(StartingModels).ToList();

        return result;
    }

    public bool CanAttachTo(
        SquadController bodyguard)
    {
        if (!IsLeaderUnit ||
            bodyguard == null ||
            bodyguard == this ||
            bodyguard.FactionId != FactionId ||
            bodyguard.IsLeaderUnit ||
            bodyguard.AttachedLeader != null)
        {
            return false;
        }

        return CanAttachToIds.Any(
            id =>
                string.Equals(
                    id,
                    bodyguard.UnitId,
                    System.StringComparison.OrdinalIgnoreCase
                )
        );
    }

    public bool AttachToBodyguard(
        SquadController bodyguard)
    {
        if (!CanAttachTo(bodyguard))
            return false;

        if (AttachedBodyguard != null)
            AttachedBodyguard.AttachedLeader = null;

        AttachedBodyguard = bodyguard;
        bodyguard.AttachedLeader = this;

        return true;
    }

    public SquadController DetachAttachedLeader()
    {
        if (AttachedLeader == null)
            return null;

        SquadController leader =
            AttachedLeader;

        AttachedLeader = null;
        leader.AttachedBodyguard = null;

        return leader;
    }

    public void DetachFromBodyguard()
    {
        if (AttachedBodyguard == null)
            return;

        SquadController bodyguard =
            AttachedBodyguard;

        AttachedBodyguard = null;

        if (bodyguard.AttachedLeader == this)
            bodyguard.AttachedLeader = null;
    }

    public SquadController JoinedActionController()
    {
        return IsAttachedLeader
            ? AttachedBodyguard
            : this;
    }

    public List<ModelToken> JoinedLivingModelTokens()
    {
        if (IsAttachedLeader &&
            AttachedBodyguard != null)
        {
            return AttachedBodyguard
                .JoinedLivingModelTokens();
        }

        List<ModelToken> joined =
            LivingModelTokens();

        if (AttachedLeader != null &&
            AttachedLeader.IsAlive &&
            AttachedLeader.IsOnBattlefield)
        {
            joined.AddRange(
                AttachedLeader.LivingModelTokens()
            );
        }

        return joined;
    }

    public string LeaderSummary()
    {
        if (AttachedLeader != null)
        {
            return
                "Leader: " +
                AttachedLeader.DisplayName;
        }

        if (IsAttachedLeader &&
            AttachedBodyguard != null)
        {
            return
                "Attached to " +
                AttachedBodyguard.DisplayName;
        }

        return IsLeaderUnit
            ? "Leader: unattached"
            : "No Leader";
    }

    public void StageForDeployment()
    {
        BattlefieldState =
            SquadBattlefieldState.Undeployed;

        SetModelPresentation(false);
    }

    public void SendToReserves(
        bool repositioned = false)
    {
        BattlefieldState =
            SquadBattlefieldState.Reserves;

        IsRepositionedReserve =
            repositioned;

        SetModelPresentation(false);
        SetSelected(false);
    }

    public void DestroyReserveWithoutTriggers()
    {
        foreach (ModelToken model
            in models)
        {
            if (model != null &&
                model.IsAlive)
            {
                model.ApplyDamage(
                    model.CurrentWounds
                );
            }
        }

        RefreshVisuals();
    }

    public void DeployToBattlefield(
        Vector3 rootPosition)
    {
        transform.position =
            rootPosition;

        BattlefieldState =
            SquadBattlefieldState.Battlefield;

        IsRepositionedReserve = false;

        SetModelPresentation(true);

        foreach (ModelToken model in models)
            model.BeginTurn();

        RefreshVisuals();
    }

    private void SetModelPresentation(bool visible)
    {
        foreach (ModelToken model in models)
        {
            if (model == null)
                continue;

            model.SetPresentationVisible(
                visible
            );

            model.SetWoundDisplayVisible(
                visible
            );
        }
    }

    public List<ModelToken> AllLivingModelTokens()
    {
        return models
            .Where(m => m.IsAlive)
            .ToList();
    }

    public void SetSelected(bool value)
    {
        selected = value;

        if (!value)
        {
            focusedModel = null;
            wholeSquadHighlighted = false;
        }

        RefreshVisuals();
    }

    public void FocusModel(ModelToken model)
    {
        focusedModel = model;
        wholeSquadHighlighted = false;
        RefreshVisuals();
    }

    public void HighlightWholeSquad()
    {
        focusedModel = null;
        wholeSquadHighlighted = true;
        RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        RefreshBattleShockVisuals();

        foreach (ModelToken token in models)
        {
            if (!token.IsAlive)
                continue;

            Color displayColor = baseColor;

            if (selected)
            {
                displayColor =
                    Color.Lerp(
                        baseColor,
                        Color.white,
                        0.28f
                    );
            }

            if (wholeSquadHighlighted)
            {
                displayColor =
                    Color.Lerp(
                        baseColor,
                        Color.cyan,
                        0.58f
                    );
            }

            if (token == focusedModel)
            {
                displayColor =
                    Color.Lerp(
                        baseColor,
                        Color.yellow,
                        0.72f
                    );
            }

            token.SetSelectionVisual(
                displayColor
            );
        }
    }

    public void AddFactionKeyword(
        string keyword)
    {
        if (sourceData == null ||
            string.IsNullOrWhiteSpace(
                keyword))
        {
            return;
        }

        List<string> values =
            new List<string>(
                sourceData.factionKeywords ??
                new string[0]
            );

        if (!values.Any(
            value =>
                string.Equals(
                    value,
                    keyword,
                    System.StringComparison.OrdinalIgnoreCase
                )))
        {
            values.Add(keyword);

            sourceData.factionKeywords =
                values.ToArray();
        }
    }

    public bool HasAnyLostWoundsOrModels()
    {
        if (LivingModels <
            StartingModels)
        {
            return true;
        }

        return models.Any(
            model =>
                model.IsAlive &&
                model.CurrentWounds <
                    model.MaxWounds
        );
    }

    public bool IsBelowStartingStrength()
    {
        SquadController actionUnit =
            JoinedActionController();

        int starting =
            actionUnit.JoinedStartingStrength();

        List<ModelToken> living =
            actionUnit.JoinedLivingModelTokens();

        if (starting == 1)
        {
            ModelToken model =
                living.FirstOrDefault();

            return
                model != null &&
                model.CurrentWounds <
                    model.MaxWounds;
        }

        return living.Count < starting;
    }

    public List<ModelToken> DestroyedModelTokens()
    {
        return models
            .Where(
                model =>
                    model != null &&
                    !model.IsAlive
            )
            .ToList();
    }

    public void ClearFactionPhaseEffects()
    {
        FactionSoulsightActive = false;
        FactionMacabreResilienceActive = false;
        FactionEmissariesRerollOnes = false;
        FactionEmissariesRerollAll = false;
        FactionHungryVoidActive = false;
        FactionConqueringTyrantActive = false;
        TargetedByFactionStratagemThisPhase = false;
        TemporaryFightsFirst = false;
        PartingTheVeilActive = false;
        AeldariOffensiveHitModifier = 0;
        AeldariOffensiveWoundModifier = 0;
        AeldariDefensiveHitModifier = 0;
        AeldariDefensiveWoundModifier = 0;
        AeldariApModifier = 0;
        AeldariDamageModifier = 0;
        AeldariInvulnerableOverride = 0;
        AeldariSustainedHits = 0;
        AeldariLethalHits = false;
        AeldariDevastatingWounds = false;
        AeldariIgnoresCover = false;
        AeldariRerollHitOnes = false;
        AeldariRerollWoundOnes = false;
        AeldariRerollAllHits = false;
        AeldariRerollAllWounds = false;
        AeldariPathChoiceMadeThisPhase = false;
        AeldariRange18Protection = false;
        AgileManoeuvreUsedThisPhase = false;
        SuddenStrikeActive = false;
        SnapShootingActive = false;
        KatahSustainedActive = false;
        KatahLethalActive = false;
        KatahChoiceMadeThisFight = false;
        BattleFocusMoveBonus = 0f;

        if (AttachedLeader != null)
        {
            AttachedLeader.FactionSoulsightActive = false;
            AttachedLeader.FactionMacabreResilienceActive = false;
            AttachedLeader.FactionEmissariesRerollOnes = false;
            AttachedLeader.FactionEmissariesRerollAll = false;
            AttachedLeader.FactionHungryVoidActive = false;
            AttachedLeader.FactionConqueringTyrantActive = false;
            AttachedLeader.TargetedByFactionStratagemThisPhase = false;
            AttachedLeader.TemporaryFightsFirst = false;
            AttachedLeader.PartingTheVeilActive = false;
            AttachedLeader.AeldariOffensiveHitModifier = 0;
            AttachedLeader.AeldariOffensiveWoundModifier = 0;
            AttachedLeader.AeldariDefensiveHitModifier = 0;
            AttachedLeader.AeldariDefensiveWoundModifier = 0;
            AttachedLeader.AeldariApModifier = 0;
            AttachedLeader.AeldariDamageModifier = 0;
            AttachedLeader.AeldariInvulnerableOverride = 0;
            AttachedLeader.AeldariSustainedHits = 0;
            AttachedLeader.AeldariLethalHits = false;
            AttachedLeader.AeldariDevastatingWounds = false;
            AttachedLeader.AeldariIgnoresCover = false;
            AttachedLeader.AeldariRerollHitOnes = false;
            AttachedLeader.AeldariRerollWoundOnes = false;
            AttachedLeader.AeldariRerollAllHits = false;
            AttachedLeader.AeldariRerollAllWounds = false;
            AttachedLeader.AeldariPathChoiceMadeThisPhase = false;
            AttachedLeader.AeldariRange18Protection = false;
            AttachedLeader.AgileManoeuvreUsedThisPhase = false;
            AttachedLeader.SuddenStrikeActive = false;
            AttachedLeader.SnapShootingActive = false;
            AttachedLeader.KatahSustainedActive = false;
            AttachedLeader.KatahLethalActive = false;
            AttachedLeader.KatahChoiceMadeThisFight = false;
            AttachedLeader.BattleFocusMoveBonus = 0f;
        }
    }

    public void ClearFactionTurnEffects()
    {
        FactionSuddenStormActive = false;
        FlittingShadowsActive = false;
        StarEnginesActive = false;
        AeldariCanShootAfterFallBack = false;
        AeldariCanChargeAfterFallBack = false;
        AeldariCanChargeAfterAdvance = false;
        AeldariVectoredEnginesActive = false;
        TearsOfIshaUsedThisTurn = false;
        SpiritMarkUsedThisTurn = false;

        if (AttachedLeader != null)
        {
            AttachedLeader.FactionSuddenStormActive = false;
            AttachedLeader.FlittingShadowsActive = false;
            AttachedLeader.StarEnginesActive = false;
            AttachedLeader.AeldariCanShootAfterFallBack = false;
            AttachedLeader.AeldariCanChargeAfterFallBack = false;
            AttachedLeader.AeldariCanChargeAfterAdvance = false;
            AttachedLeader.AeldariVectoredEnginesActive = false;
            AttachedLeader.TearsOfIshaUsedThisTurn = false;
            AttachedLeader.SpiritMarkUsedThisTurn = false;
        }
    }

    public bool HasOwnKeywordValue(
        string keyword)
    {
        return HasOwnKeyword(
            keyword
        );
    }

    public bool HasIntrinsicKeyword(
        string keyword)
    {
        return HasOwnKeyword(
            keyword
        );
    }

    public bool HasKeyword(
        string keyword)
    {
        if (HasOwnKeyword(keyword))
            return true;

        if (!IsAttachedLeader &&
            AttachedLeader != null &&
            AttachedLeader.IsAlive &&
            AttachedLeader.HasOwnKeyword(
                keyword))
        {
            return true;
        }

        return false;
    }

    private bool HasOwnKeyword(
        string keyword)
    {
        if (sourceData == null ||
            string.IsNullOrWhiteSpace(
                keyword))
        {
            return false;
        }

        string wanted =
            WeaponRuleParser
                .NormalizeRuleName(
                    keyword
                );

        IEnumerable<string> all =
            (sourceData.keywords ??
                new string[0])
            .Concat(
                sourceData.factionKeywords ??
                new string[0]
            );

        return all.Any(
            item =>
                WeaponRuleParser
                    .NormalizeRuleName(
                        item
                    ) ==
                wanted
        );
    }

    public void DeclareAdvance(
        int roll)
    {
        SquadController actionUnit =
            JoinedActionController();

        actionUnit.HasAdvanced = true;
        actionUnit.AdvanceBonus =
            Mathf.Clamp(
                roll,
                1,
                6
            );

        if (actionUnit.AttachedLeader != null)
        {
            actionUnit.AttachedLeader.HasAdvanced =
                true;

            actionUnit.AttachedLeader.AdvanceBonus =
                actionUnit.AdvanceBonus;
        }
    }

    public float GetMovementAllowanceFor(
        ModelToken model)
    {
        if (model == null)
            return 0f;

        SquadController actionUnit =
            JoinedActionController();

        return
            model.Squad.GetMove() +
            actionUnit.BattleFocusMoveBonus +
            (actionUnit.HasAdvanced
                ? actionUnit.AdvanceBonus
                : 0);
    }

    public int BestLeadership()
    {
        SquadController actionUnit =
            JoinedActionController();

        List<ModelToken> living =
            actionUnit.JoinedLivingModelTokens();

        if (living.Count == 0)
            return BaseLeadership;

        return living.Min(
            model => model.Leadership
        );
    }

    public int JoinedStartingStrength()
    {
        SquadController actionUnit =
            JoinedActionController();

        int value =
            actionUnit.StartingModels;

        if (actionUnit.AttachedLeader != null &&
            actionUnit.AttachedLeader.IsAlive)
        {
            value +=
                actionUnit.AttachedLeader.StartingModels;
        }

        return Mathf.Max(1, value);
    }

    public bool IsAtOrBelowHalfStrength()
    {
        SquadController actionUnit =
            JoinedActionController();

        int starting =
            actionUnit.JoinedStartingStrength();

        List<ModelToken> living =
            actionUnit.JoinedLivingModelTokens();

        if (starting == 1)
        {
            ModelToken only =
                living.FirstOrDefault();

            if (only == null)
                return true;

            return
                only.CurrentWounds * 2 <=
                only.MaxWounds;
        }

        return
            living.Count * 2 <=
            starting;
    }

    public void SetBattleShocked(
        bool value,
        int roll)
    {
        SquadController actionUnit =
            JoinedActionController();

        actionUnit.IsBattleShocked = value;
        actionUnit.LastBattleShockRoll = roll;

        if (actionUnit.AttachedLeader != null)
        {
            actionUnit.AttachedLeader.IsBattleShocked =
                value;
            actionUnit.AttachedLeader.LastBattleShockRoll =
                roll;
        }

        actionUnit.RefreshBattleShockVisuals();
    }

    public void RefreshBattleShockVisuals()
    {
        SquadController actionUnit =
            JoinedActionController();

        foreach (ModelToken model
            in actionUnit.AllLivingModelTokens())
        {
            if (model != null)
            {
                model.SetBattleShockVisual(
                    actionUnit.IsBattleShocked
                );
            }
        }

        if (actionUnit.AttachedLeader != null)
        {
            foreach (ModelToken model
                in actionUnit.AttachedLeader
                    .AllLivingModelTokens())
            {
                if (model != null)
                {
                    model.SetBattleShockVisual(
                        actionUnit.IsBattleShocked
                    );
                }
            }
        }
    }

    public int EffectiveObjectiveControl(
        ModelToken model)
    {
        if (model == null ||
            !model.IsAlive)
        {
            return 0;
        }

        if (JoinedActionController().IsBattleShocked)
            return 0;

        int objectiveControl =
            model.ObjectiveControl;

        if (model.Squad != null &&
            model.Squad
                .AeldariObjectiveControlOverride >
                0)
        {
            objectiveControl =
                model.Squad
                    .AeldariObjectiveControlOverride;
        }

        return Mathf.Max(
            0,
            objectiveControl
        );
    }

    public int TotalObjectiveControlWithin(
        Vector3 point,
        float radius)
    {
        SquadController actionUnit =
            JoinedActionController();

        if (actionUnit.IsBattleShocked)
            return 0;

        int total = 0;

        foreach (ModelToken model
            in actionUnit
                .JoinedLivingModelTokens())
        {
            if (model == null ||
                !model.IsAlive ||
                !CoreRules11Geometry
                    .ModelWithinObjective(
                        model,
                        point,
                        radius
                    ))
            {
                continue;
            }

            int oc =
                actionUnit
                    .AeldariObjectiveControlOverride > 0
                ? actionUnit
                    .AeldariObjectiveControlOverride
                : model.ObjectiveControl;

            total +=
                Mathf.Max(0, oc);
        }

        return total;
    }

    public void MarkSetUpThisTurn()
    {
        SquadController actionUnit =
            JoinedActionController();

        actionUnit.WasSetUpThisTurn = true;

        if (actionUnit.AttachedLeader != null)
        {
            actionUnit.AttachedLeader
                .WasSetUpThisTurn = true;
        }
    }

    public void MarkMadeChargeMove()
    {
        SquadController actionUnit =
            JoinedActionController();

        actionUnit.MadeChargeMove = true;

        if (actionUnit.AttachedLeader != null)
        {
            actionUnit.AttachedLeader
                .MadeChargeMove = true;
        }
    }

    public float MaxDistanceMovedThisTurn()
    {
        SquadController actionUnit =
            JoinedActionController();

        float best = 0f;

        foreach (ModelToken model
            in actionUnit.JoinedLivingModelTokens())
        {
            best =
                Mathf.Max(
                    best,
                    model.DistanceMovedFromTurnStart(
                        model.transform.position
                    )
                );
        }

        return best;
    }

    public float GetMove()
    {
        float value = BaseMove;

        foreach (IUnitAbility ability in abilities)
            value = ability.ModifyMove(this, value);

        if (AttachedLeader != null &&
            AttachedLeader.IsAlive)
        {
            value +=
                AttachedLeader.AttachedMoveModifier;
        }

        return Mathf.Max(0f, value);
    }

    public int GetRangedSkill(
        SquadController target,
        WeaponData weapon)
    {
        int value =
            weapon != null
            ? weapon.skill
            : 6;

        foreach (IUnitAbility ability in abilities)
        {
            value =
                ability.ModifyRangedSkill(
                    this,
                    target,
                    value
                );
        }

        if (AttachedLeader != null &&
            AttachedLeader.IsAlive)
        {
            value +=
                AttachedLeader.AttachedRangedSkillModifier;
        }

        return Mathf.Clamp(value, 2, 6);
    }

    public int GetMeleeSkill(
        SquadController target,
        WeaponData weapon)
    {
        int value =
            weapon != null
            ? weapon.skill
            : 6;

        foreach (IUnitAbility ability in abilities)
        {
            value =
                ability.ModifyMeleeSkill(
                    this,
                    target,
                    value
                );
        }

        if (AttachedLeader != null &&
            AttachedLeader.IsAlive)
        {
            value +=
                AttachedLeader.AttachedMeleeSkillModifier;
        }

        return Mathf.Clamp(value, 2, 6);
    }

    public int GetSave(SquadController attacker)
    {
        int value = BaseSave;

        foreach (IUnitAbility ability in abilities)
            value = ability.ModifySave(this, attacker, value);

        return Mathf.Clamp(value, 2, 6);
    }

    public void BeginMissionAction(
        string actionId)
    {
        SquadController actionUnit =
            JoinedActionController();

        actionUnit.StartedMissionActionThisTurn =
            true;

        actionUnit.IsPerformingMissionAction =
            true;

        actionUnit.ActiveMissionActionId =
            actionId ?? "";

        if (actionUnit.AttachedLeader != null)
        {
            actionUnit.AttachedLeader
                .StartedMissionActionThisTurn =
                true;

            actionUnit.AttachedLeader
                .IsPerformingMissionAction =
                true;

            actionUnit.AttachedLeader
                .ActiveMissionActionId =
                actionUnit.ActiveMissionActionId;
        }
    }

    public void CompleteMissionAction()
    {
        SquadController actionUnit =
            JoinedActionController();

        actionUnit.IsPerformingMissionAction =
            false;

        actionUnit.ActiveMissionActionId = "";

        if (actionUnit.AttachedLeader != null)
        {
            actionUnit.AttachedLeader
                .IsPerformingMissionAction =
                false;

            actionUnit.AttachedLeader
                .ActiveMissionActionId = "";
        }
    }

    public void CancelMissionAction()
    {
        CompleteMissionAction();
    }

    public void StartTurn()
    {
        HasMoved = false;
        HasShot = false;
        HasCharged = false;
        HasFought = false;
        HasFallenBack = false;
        HasAdvanced = false;
        AdvanceBonus = 0;
        WasSetUpThisTurn = false;

        StartedMissionActionThisTurn = false;
        IsPerformingMissionAction = false;
        ActiveMissionActionId = "";
        MadeChargeMove = false;

        ClearFactionPhaseEffects();
        ClearFactionTurnEffects();

        foreach (ModelToken model in models)
            model.BeginTurn();

        foreach (IUnitAbility ability in abilities)
            ability.OnTurnStart(this);

        RefreshVisuals();
    }

    public int HealWounds(int amount)
    {
        int remaining = amount;
        int healed = 0;

        foreach (ModelToken model
            in models.Where(
                m =>
                    m.IsAlive &&
                    m.CurrentWounds <
                    m.MaxWounds
            ))
        {
            if (remaining <= 0)
                break;

            int restored =
                model.Heal(remaining);

            healed += restored;
            remaining -= restored;
        }

        return healed;
    }

    public DamageResult ApplyUnsavedHits(
        int hits,
        int damagePerHit)
    {
        int woundsLost = 0;
        int modelsKilled = 0;

        for (int i = 0; i < hits; i++)
        {
            ModelToken target =
                GetAutomaticAllocationModel();

            if (target == null)
                break;

            bool aliveBefore =
                target.IsAlive;

            woundsLost +=
                target.ApplyDamage(
                    damagePerHit
                );

            if (aliveBefore &&
                !target.IsAlive)
            {
                modelsKilled++;
            }
        }

        RefreshVisuals();

        return new DamageResult(
            woundsLost,
            modelsKilled
        );
    }

    public ModelToken GetAutomaticAllocationModel()
    {
        // Normal damage allocation continues on an already-wounded model.
        ModelToken wounded =
            models.FirstOrDefault(
                m =>
                    m.IsAlive &&
                    m.CurrentWounds <
                    m.MaxWounds
            );

        if (wounded != null)
            return wounded;

        List<ModelToken> living =
            LivingModelTokens();

        if (living.Count <= 1)
            return living.FirstOrDefault();

        // Until we add interactive casualty choice, automatically prefer a
        // casualty whose removal leaves the remaining unit coherent.
        //
        // The previous allocator simply removed the first model in the list.
        // If that model happened to be the bridge between two parts of the
        // squad, casualties could split one unit into two disconnected groups.
        List<ModelToken> safeCandidates =
            living
                .Where(
                    candidate =>
                        WouldRemainCoherentWithout(
                            candidate
                        )
                )
                .ToList();

        if (safeCandidates.Count > 0)
        {
            // Prefer an edge model: fewest coherency neighbours first, then
            // furthest from the current squad centre.
            Vector3 centre =
                CurrentCentre();

            return safeCandidates
                .OrderBy(
                    candidate =>
                        CountCoherencyNeighbours(
                            candidate,
                            living
                        )
                )
                .ThenByDescending(
                    candidate =>
                        HorizontalDistance(
                            candidate.transform.position,
                            centre
                        )
                )
                .First();
        }

        // If no single casualty can preserve full coherency, pick the model
        // whose removal leaves the largest connected main body. This avoids
        // making an unavoidable break worse than necessary.
        return living
            .OrderByDescending(
                candidate =>
                    LargestConnectedComponentSizeWithout(
                        candidate
                    )
            )
            .ThenBy(
                candidate =>
                    CountCoherencyNeighbours(
                        candidate,
                        living
                    )
            )
            .First();
    }

    private bool WouldRemainCoherentWithout(
        ModelToken excluded)
    {
        List<ModelToken> remaining =
            LivingModelTokens()
                .Where(
                    model =>
                        model != excluded
                )
                .ToList();

        if (remaining.Count <= 1)
            return true;

        return ListIsCoherent(
            remaining
        );
    }

    private int LargestConnectedComponentSizeWithout(
        ModelToken excluded)
    {
        List<ModelToken> remaining =
            LivingModelTokens()
                .Where(
                    model =>
                        model != excluded
                )
                .ToList();

        return LargestConnectedComponentSize(
            remaining
        );
    }

    private int LargestConnectedComponentSize(
        List<ModelToken> set)
    {
        if (set == null ||
            set.Count == 0)
        {
            return 0;
        }

        HashSet<ModelToken> unvisited =
            new HashSet<ModelToken>(set);

        int largest = 0;

        while (unvisited.Count > 0)
        {
            ModelToken seed =
                unvisited.First();

            Queue<ModelToken> frontier =
                new Queue<ModelToken>();

            frontier.Enqueue(seed);
            unvisited.Remove(seed);

            int componentSize = 0;

            while (frontier.Count > 0)
            {
                ModelToken current =
                    frontier.Dequeue();

                componentSize++;

                List<ModelToken> neighbours =
                    unvisited
                        .Where(
                            other =>
                                CoreRules11Geometry.WithinCoherencyNeighbour(current, other)
                        )
                        .ToList();

                foreach (ModelToken neighbour
                    in neighbours)
                {
                    unvisited.Remove(neighbour);
                    frontier.Enqueue(neighbour);
                }
            }

            largest =
                Mathf.Max(
                    largest,
                    componentSize
                );
        }

        return largest;
    }

    public List<ModelToken> LivingModelTokens()
    {
        if (!IsOnBattlefield)
            return new List<ModelToken>();

        return models
            .Where(m => m.IsAlive)
            .ToList();
    }

    public List<Vector3> LivingPositions()
    {
        return LivingModelTokens()
            .Select(m => m.transform.position)
            .ToList();
    }

    public Vector3 CurrentCentre()
    {
        List<ModelToken> living =
            LivingModelTokens();

        if (living.Count == 0)
            return transform.position;

        Vector3 sum = Vector3.zero;

        foreach (ModelToken model in living)
            sum += model.transform.position;

        return sum / living.Count;
    }

    public Vector3 TurnStartCentre()
    {
        List<ModelToken> living =
            LivingModelTokens();

        if (living.Count == 0)
            return transform.position;

        Vector3 sum = Vector3.zero;

        foreach (ModelToken model in living)
            sum += model.TurnStartWorldPosition;

        return sum / living.Count;
    }

    public bool CanTranslateWithinNormalMove(
        Vector3 delta)
    {
        SquadController actionUnit =
            JoinedActionController();

        foreach (ModelToken model
            in actionUnit.JoinedLivingModelTokens())
        {
            Vector3 candidate =
                model.transform.position +
                delta;

            float maxMove =
                actionUnit
                    .GetMovementAllowanceFor(
                        model
                    );

            if (model.DistanceMovedFromTurnStart(
                    candidate
                ) >
                maxMove + 0.001f)
            {
                return false;
            }
        }

        return true;
    }

    public Dictionary<ModelToken, Vector3>
        CaptureLivingPositions()
    {
        Dictionary<ModelToken, Vector3> result =
            new Dictionary<ModelToken, Vector3>();

        foreach (ModelToken model
            in LivingModelTokens())
        {
            result[model] =
                model.transform.position;
        }

        return result;
    }

    public void RestorePositions(
        Dictionary<ModelToken, Vector3> positions)
    {
        foreach (
            KeyValuePair<ModelToken, Vector3>
                pair
            in positions)
        {
            if (pair.Key != null &&
                pair.Key.IsAlive)
            {
                pair.Key.transform.position =
                    pair.Value;
            }
        }

        RefreshVisuals();
    }

    public void TranslateLivingModels(
        Vector3 delta)
    {
        delta.y = 0f;

        foreach (ModelToken model
            in LivingModelTokens())
        {
            model.transform.position +=
                delta;
        }

        RefreshVisuals();
    }

    public float DistanceTo(SquadController other)
    {
        float best = float.MaxValue;

        foreach (Vector3 a in LivingPositions())
        {
            foreach (Vector3 b
                in other.LivingPositions())
            {
                best =
                    Mathf.Min(
                        best,
                        HorizontalDistance(
                            a,
                            b
                        )
                    );
            }
        }

        return best ==
            float.MaxValue
            ? 999f
            : best;
    }

    public Vector3 DirectionToward(
        SquadController other)
    {
        Vector3 from =
            ClosestPointTo(other);

        Vector3 to =
            other.ClosestPointTo(this);

        Vector3 direction =
            to - from;

        direction.y = 0f;

        return direction.sqrMagnitude >
            0.001f
            ? direction.normalized
            : Vector3.forward;
    }

    private Vector3 ClosestPointTo(
        SquadController other)
    {
        Vector3 bestPoint =
            transform.position;

        float best =
            float.MaxValue;

        foreach (Vector3 a
            in LivingPositions())
        {
            foreach (Vector3 b
                in other.LivingPositions())
            {
                float d =
                    HorizontalDistance(
                        a,
                        b
                    );

                if (d < best)
                {
                    best = d;
                    bestPoint = a;
                }
            }
        }

        return bestPoint;
    }

    public ModelToken ClosestLivingModelTo(
        Vector3 point)
    {
        ModelToken bestModel = null;
        float best = float.MaxValue;

        foreach (ModelToken model
            in LivingModelTokens())
        {
            float d =
                HorizontalDistance(
                    model.transform.position,
                    point
                );

            if (d < best)
            {
                best = d;
                bestModel = model;
            }
        }

        return bestModel;
    }

    public bool IsCoherent()
    {
        if (IsAttachedLeader &&
            AttachedBodyguard != null)
        {
            return AttachedBodyguard
                .IsCoherent();
        }

        return ListIsCoherent(
            JoinedLivingModelTokens()
        );
    }

    public bool WouldRemainCoherentAfterRemoving(
        ModelToken candidate)
    {
        if (candidate == null)
            return IsCoherent();

        SquadController actionUnit =
            JoinedActionController();

        List<ModelToken> remaining =
            actionUnit
                .JoinedLivingModelTokens()
                .Where(
                    model =>
                        model != candidate
                )
                .ToList();

        if (remaining.Count <= 1)
            return true;

        return ListIsCoherent(
            remaining
        );
    }

    public List<ModelToken> IncoherentModels()
    {
        SquadController actionUnit =
            JoinedActionController();

        List<ModelToken> living =
            actionUnit
                .JoinedLivingModelTokens()
                .Where(
                    model =>
                        model != null &&
                        model.IsAlive
                )
                .ToList();

        List<ModelToken> invalid =
            new List<ModelToken>();

        if (living.Count <= 1)
            return invalid;

        foreach (ModelToken model
            in living)
        {
            bool hasNeighbour =
                living.Any(
                    other =>
                        other != model &&
                        CoreRules11Geometry
                            .WithinCoherencyNeighbour(
                                model,
                                other
                            )
                );

            bool withinNine =
                living.All(
                    other =>
                        other == model ||
                        CoreRules11Geometry
                            .WithinCoherencyAll(
                                model,
                                other
                            )
                );

            if (!hasNeighbour ||
                !withinNine)
            {
                invalid.Add(model);
            }
        }

        return invalid;
    }

    public int IncoherentModelCount()
    {
        if (IsAttachedLeader &&
            AttachedBodyguard != null)
        {
            return AttachedBodyguard
                .IncoherentModelCount();
        }

        List<ModelToken> living =
            JoinedLivingModelTokens();

        if (living.Count <= 1)
            return 0;

        HashSet<ModelToken> invalid =
            new HashSet<ModelToken>();

        foreach (ModelToken model in living)
        {
            bool hasNeighbourWithinTwo =
                living.Any(
                    other =>
                        other != model &&
                        HorizontalDistance(
                            model.transform.position,
                            other.transform.position
                        ) <=
                        2.0f
                );

            if (!hasNeighbourWithinTwo)
                invalid.Add(model);

            bool withinNineOfEveryone =
                living.All(
                    other =>
                        other == model ||
                        HorizontalDistance(
                            model.transform.position,
                            other.transform.position
                        ) <=
                        9.0f
                );

            if (!withinNineOfEveryone)
                invalid.Add(model);
        }

        return invalid.Count;
    }

    private bool ListIsCoherent(
        List<ModelToken> living)
    {
        if (living == null ||
            living.Count <= 1)
        {
            return true;
        }

        foreach (ModelToken model
            in living)
        {
            bool hasNeighbour =
                living.Any(
                    other =>
                        other != model &&
                        CoreRules11Geometry
                            .WithinCoherencyNeighbour(
                                model,
                                other
                            )
                );

            bool withinNine =
                living.All(
                    other =>
                        other == model ||
                        CoreRules11Geometry
                            .WithinCoherencyAll(
                                model,
                                other
                            )
                );

            if (!hasNeighbour ||
                !withinNine)
            {
                return false;
            }
        }

        return true;
    }

    private int CountCoherencyNeighbours(
        ModelToken model,
        List<ModelToken> living)
    {
        int neighbours = 0;

        foreach (ModelToken other in living)
        {
            if (other == model)
                continue;

            if (HorizontalDistance(
                    model.transform.position,
                    other.transform.position
                ) <=
                2.0f)
            {
                neighbours++;
            }
        }

        return neighbours;
    }

    public string AbilityText()
    {
        if (abilities.Count == 0)
            return "none";

        return string.Join(
            ", ",
            abilities
                .Select(a => a.Id)
                .ToArray()
        );
    }

    public string LoadoutSummary()
    {
        Dictionary<string, int> groups =
            new Dictionary<string, int>();

        foreach (ModelToken model
            in LivingModelTokens())
        {
            string ranged =
                model.RangedWeapons.Count > 0
                ? string.Join(
                    " + ",
                    model.RangedWeapons
                        .Select(
                            weapon =>
                                weapon.displayName
                        )
                        .ToArray()
                  )
                : "no gun";

            string melee =
                model.MeleeWeapons.Count > 0
                ? string.Join(
                    " / ",
                    model.MeleeWeapons
                        .Select(
                            weapon =>
                                weapon.displayName
                        )
                        .Distinct()
                        .ToArray()
                  )
                : "no melee";

            string key =
                model.RoleName +
                " [" +
                ranged +
                "; " +
                melee +
                "]";

            if (!groups.ContainsKey(key))
                groups[key] = 0;

            groups[key]++;
        }

        string summary =
            string.Join(
                " | ",
                groups.Select(
                    pair =>
                        pair.Value +
                        "x " +
                        pair.Key
                ).ToArray()
            );

        if (AttachedLeader != null &&
            AttachedLeader.IsAlive)
        {
            summary +=
                " | + " +
                AttachedLeader.DisplayName;
        }

        return summary;
    }

    private static float HorizontalDistance(
        Vector3 a,
        Vector3 b)
    {
        return Vector2.Distance(
            new Vector2(a.x, a.z),
            new Vector2(b.x, b.z)
        );
    }
}
