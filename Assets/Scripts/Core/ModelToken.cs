using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ModelToken : MonoBehaviour
{
    public SquadController Squad { get; private set; }

    public string RoleName { get; private set; }

    // Backwards-compatible convenience accessors.
    public WeaponData RangedWeapon
    {
        get { return rangedWeapons.FirstOrDefault(); }
    }

    public WeaponData MeleeWeapon
    {
        get { return meleeWeapons.FirstOrDefault(); }
    }

    public IReadOnlyList<WeaponData> RangedWeapons
    {
        get { return rangedWeapons; }
    }

    public IReadOnlyList<WeaponData> MeleeWeapons
    {
        get { return meleeWeapons; }
    }

    public int MaxWounds { get; private set; }
    public int CurrentWounds { get; private set; }

    public int Leadership { get; private set; }
    public int ObjectiveControl { get; private set; }
    public int InvulnerableSave { get; private set; }

    public Vector3 TurnStartWorldPosition { get; private set; }

    private readonly HashSet<string>
        oneShotWeaponsUsed =
            new HashSet<string>(
                System.StringComparer.OrdinalIgnoreCase
            );

    private readonly HashSet<string>
        rangedWeaponsFiredThisTurn =
            new HashSet<string>(
                System.StringComparer.OrdinalIgnoreCase
            );

    private string rangedFireGroupThisTurn = "";

    public bool HasCompletedShootingThisTurn
    {
        get;
        private set;
    }

    private GameObject woundDisplayObject;
    private TextMesh woundText;
    private bool woundDisplayRequestedVisible = true;

    private Renderer proxyRenderer;
    private Collider proxyCollider;
    private GameObject visualRoot;
    private Renderer selectionMarkerRenderer;

    private GameObject battleShockMarkerObject;
    private Renderer battleShockMarkerRenderer;
    private bool battleShockVisualActive;
    private bool presentationVisible = true;

    private readonly List<Renderer> visualRenderers =
        new List<Renderer>();

    public bool HasCustomVisual
    {
        get { return visualRoot != null; }
    }

    public float BaseRadiusInches { get; private set; } = 0.39f;

    private readonly List<WeaponData> rangedWeapons =
        new List<WeaponData>();

    private readonly List<WeaponData> meleeWeapons =
        new List<WeaponData>();

    public bool IsAlive
    {
        get { return gameObject.activeSelf; }
    }

    public void ApplyFactionMaxWoundsModifier(int amount)
    {
        if (amount == 0)
            return;

        MaxWounds = Mathf.Max(1, MaxWounds + amount);
        CurrentWounds = Mathf.Clamp(
            CurrentWounds + amount,
            0,
            MaxWounds);

        RefreshWoundDisplay();
    }

    public void Initialize(
        SquadController squad,
        int wounds,
        string roleName,
        WeaponData rangedWeapon,
        WeaponData meleeWeapon)
    {
        Initialize(
            squad,
            wounds,
            roleName,
            rangedWeapon != null
                ? new[] { rangedWeapon }
                : new WeaponData[0],
            meleeWeapon != null
                ? new[] { meleeWeapon }
                : new WeaponData[0],
            7,
            1,
            0
        );
    }

    public void Initialize(
        SquadController squad,
        int wounds,
        string roleName,
        IEnumerable<WeaponData> ranged,
        IEnumerable<WeaponData> melee,
        int leadership,
        int objectiveControl,
        int invulnerableSave)
    {
        Squad = squad;
        MaxWounds = Mathf.Max(1, wounds);
        CurrentWounds = MaxWounds;

        Leadership =
            Mathf.Clamp(
                leadership > 0 ? leadership : 7,
                2,
                12
            );

        ObjectiveControl =
            Mathf.Max(
                0,
                objectiveControl
            );

        InvulnerableSave =
            invulnerableSave > 0
            ? Mathf.Clamp(
                invulnerableSave,
                2,
                6
              )
            : 0;

        RoleName =
            string.IsNullOrWhiteSpace(roleName)
            ? "Model"
            : roleName;

        rangedWeapons.Clear();
        meleeWeapons.Clear();

        if (ranged != null)
        {
            rangedWeapons.AddRange(
                ranged.Where(
                    weapon =>
                        weapon != null
                )
            );
        }

        if (melee != null)
        {
            meleeWeapons.AddRange(
                melee.Where(
                    weapon =>
                        weapon != null
                )
            );
        }

        TurnStartWorldPosition =
            transform.position;

        proxyRenderer =
            GetComponent<Renderer>();

        proxyCollider =
            GetComponent<Collider>();

        CreateBattleShockMarker(
            BaseRadiusInches *
            2f
        );

        CreateWoundDisplay();
        RefreshWoundDisplay();
    }

    public bool AttachVisual(
        ModelVisualDefinition definition,
        Color factionColor)
    {
        if (definition == null ||
            definition.Components == null ||
            definition.Components.Length == 0)
        {
            return false;
        }

        if (visualRoot != null)
        {
            Destroy(visualRoot);
            visualRoot = null;
            visualRenderers.Clear();
        }

        GameObject root =
            new GameObject(
                "Visual Model"
            );

        root.transform.SetParent(
            transform,
            false
        );

        // Existing token centres are 0.65" above the tabletop. TTS model
        // assets are authored from tabletop Y=0, so shift the reconstructed
        // visual back down by that amount while keeping gameplay positions
        // unchanged.
        root.transform.localPosition =
            new Vector3(
                0f,
                -0.65f,
                0f
            );

        root.transform.localRotation =
            Quaternion.identity;

        root.transform.localScale =
            Vector3.one;

        visualRoot =
            root;

        BaseRadiusInches =
            Mathf.Max(
                0.1f,
                definition.BaseDiameterInches *
                0.5f
            );

        ResizeBattleShockMarker(
            definition.BaseDiameterInches
        );

        // The gameplay proxy remains the authoritative selectable object,
        // but its footprint now reflects the miniature's real circular base.
        transform.localScale =
            Vector3.one;

        CapsuleCollider capsule =
            proxyCollider as CapsuleCollider;

        if (capsule != null)
        {
            capsule.radius =
                BaseRadiusInches;

            capsule.height =
                Mathf.Max(
                    1.30f,
                    BaseRadiusInches *
                    2.0f
                );

            capsule.center =
                Vector3.zero;
        }

        int loadedVisualCount = 0;

        if (!string.IsNullOrWhiteSpace(
                definition.BaseMeshResource))
        {
            GameObject baseAsset =
                Resources.Load<GameObject>(
                    definition.BaseMeshResource
                );

            if (baseAsset != null)
            {
                GameObject baseObject =
                    Instantiate(
                        baseAsset,
                        visualRoot.transform
                    );

                baseObject.name =
                    "Tabletop Base";

                baseObject.transform.localPosition =
                    Vector3.zero;

                baseObject.transform.localRotation =
                    Quaternion.identity;

                baseObject.transform.localScale =
                    Vector3.one;

                Material baseMaterial =
                    CreateRuntimeMaterial(
                        "",
                        "",
                        new Color(
                            0.075f,
                            0.075f,
                            0.085f,
                            1f
                        )
                    );

                ApplyMaterialRecursively(
                    baseObject,
                    baseMaterial
                );
            }
        }

        foreach (
            ModelVisualComponentDefinition
                component
            in definition.Components)
        {
            if (component == null ||
                string.IsNullOrWhiteSpace(
                    component.MeshResource))
            {
                continue;
            }

            GameObject asset =
                Resources.Load<GameObject>(
                    component.MeshResource
                );

            if (asset == null)
            {
                Debug.LogWarning(
                    "Warboard model asset not found: " +
                    component.MeshResource
                );

                continue;
            }

            GameObject instance =
                Instantiate(
                    asset,
                    visualRoot.transform
                );

            instance.name =
                "Miniature Visual";

            instance.transform.localPosition =
                component.LocalPosition;

            instance.transform.localEulerAngles =
                component.LocalEuler;

            instance.transform.localScale =
                component.LocalScale;

            Material material =
                CreateRuntimeMaterial(
                    component.DiffuseResource,
                    component.NormalResource,
                    Color.white
                );

            ApplyMaterialRecursively(
                instance,
                material
            );

            loadedVisualCount++;
        }

        if (loadedVisualCount == 0)
        {
            Debug.LogWarning(
                "Warboard could not instantiate any visual mesh for " +
                gameObject.name +
                ". Keeping the gameplay capsule visible."
            );

            if (visualRoot != null)
            {
                Destroy(visualRoot);
                visualRoot = null;
            }

            visualRenderers.Clear();

            if (proxyRenderer != null)
                proxyRenderer.enabled = true;

            return false;
        }

        if (proxyRenderer != null)
            proxyRenderer.enabled = false;

        CreateSelectionMarker(
            definition.BaseDiameterInches,
            factionColor
        );

        Renderer[] renderers =
            visualRoot.GetComponentsInChildren<
                Renderer
            >(true);

        foreach (Renderer renderer
            in renderers)
        {
            if (renderer == null ||
                renderer ==
                    selectionMarkerRenderer)
            {
                continue;
            }

            visualRenderers.Add(
                renderer
            );
        }

        SetPresentationVisible(
            Squad == null ||
            Squad.IsOnBattlefield
        );

        return
            visualRenderers.Count > 0;
    }

    public void SetPresentationVisible(
        bool visible)
    {
        presentationVisible = visible;

        if (proxyCollider == null)
            proxyCollider =
                GetComponent<Collider>();

        if (proxyRenderer == null)
            proxyRenderer =
                GetComponent<Renderer>();

        if (proxyCollider != null)
            proxyCollider.enabled = visible;

        if (proxyRenderer != null)
        {
            proxyRenderer.enabled =
                visible &&
                !HasCustomVisual;
        }

        foreach (Renderer renderer
            in visualRenderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }

        if (selectionMarkerRenderer != null)
        {
            selectionMarkerRenderer.enabled =
                visible;
        }

        if (battleShockMarkerRenderer != null)
        {
            battleShockMarkerRenderer.enabled =
                visible &&
                battleShockVisualActive;
        }
    }

    public void SetBattleShockVisual(
        bool active)
    {
        battleShockVisualActive =
            active;

        if (battleShockMarkerRenderer != null)
        {
            battleShockMarkerRenderer.enabled =
                active &&
                presentationVisible &&
                IsAlive;
        }
    }

    private void CreateBattleShockMarker(
        float baseDiameter)
    {
        if (battleShockMarkerObject != null)
        {
            ResizeBattleShockMarker(
                baseDiameter
            );

            return;
        }

        battleShockMarkerObject =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder
            );

        battleShockMarkerObject.name =
            "Battle-shock Ice Aura";

        battleShockMarkerObject.transform
            .SetParent(
                transform,
                false
            );

        battleShockMarkerObject.transform
            .localPosition =
            new Vector3(
                0f,
                -0.625f,
                0f
            );

        Collider col =
            battleShockMarkerObject
                .GetComponent<Collider>();

        if (col != null)
            Destroy(col);

        battleShockMarkerRenderer =
            battleShockMarkerObject
                .GetComponent<Renderer>();

        if (battleShockMarkerRenderer != null)
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit"
                );

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Sprites/Default"
                    );
            }

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Standard"
                    );
            }

            if (shader != null)
            {
                Material material =
                    new Material(shader);

                material.color =
                    new Color(
                        0.36f,
                        0.82f,
                        1.0f,
                        1f
                    );

                if (material.HasProperty(
                        "_EmissionColor"))
                {
                    material.EnableKeyword(
                        "_EMISSION"
                    );

                    material.SetColor(
                        "_EmissionColor",
                        new Color(
                            0.22f,
                            0.72f,
                            1.0f,
                            1f
                        ) *
                        1.8f
                    );
                }

                battleShockMarkerRenderer
                    .material =
                    material;
            }

            battleShockMarkerRenderer.enabled =
                false;
        }

        ResizeBattleShockMarker(
            baseDiameter
        );
    }

    private void ResizeBattleShockMarker(
        float baseDiameter)
    {
        if (battleShockMarkerObject == null)
            return;

        float diameter =
            Mathf.Max(
                0.55f,
                baseDiameter +
                0.34f
            );

        battleShockMarkerObject.transform
            .localScale =
            new Vector3(
                diameter,
                0.010f,
                diameter
            );
    }

    private void Update()
    {
        if (!battleShockVisualActive ||
            battleShockMarkerRenderer == null ||
            !battleShockMarkerRenderer.enabled)
        {
            return;
        }

        float pulse =
            0.5f +
            0.5f *
            Mathf.Sin(
                Time.time *
                4.2f
            );

        Color ice =
            Color.Lerp(
                new Color(
                    0.18f,
                    0.58f,
                    0.92f,
                    1f
                ),
                new Color(
                    0.72f,
                    0.94f,
                    1.0f,
                    1f
                ),
                pulse
            );

        battleShockMarkerRenderer
            .material.color =
            ice;

        Material material =
            battleShockMarkerRenderer
                .material;

        if (material.HasProperty(
                "_EmissionColor"))
        {
            material.SetColor(
                "_EmissionColor",
                ice *
                Mathf.Lerp(
                    1.4f,
                    2.3f,
                    pulse
                )
            );
        }
    }

    public void SetSelectionVisual(
        Color color)
    {
        if (selectionMarkerRenderer != null)
        {
            selectionMarkerRenderer
                .material.color =
                color;

            return;
        }

        if (proxyRenderer == null)
            proxyRenderer =
                GetComponent<Renderer>();

        if (proxyRenderer != null)
            proxyRenderer.material.color = color;
    }

    private void CreateSelectionMarker(
        float baseDiameter,
        Color factionColor)
    {
        GameObject marker =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder
            );

        marker.name =
            "Selection Ring";

        marker.transform.SetParent(
            visualRoot.transform,
            false
        );

        marker.transform.localPosition =
            new Vector3(
                0f,
                0.012f,
                0f
            );

        float diameter =
            Mathf.Max(
                0.4f,
                baseDiameter +
                0.10f
            );

        marker.transform.localScale =
            new Vector3(
                diameter,
                0.010f,
                diameter
            );

        Collider markerCollider =
            marker.GetComponent<Collider>();

        if (markerCollider != null)
            Destroy(markerCollider);

        selectionMarkerRenderer =
            marker.GetComponent<Renderer>();

        if (selectionMarkerRenderer != null)
        {
            Material markerMaterial =
                CreateRuntimeMaterial(
                    "",
                    "",
                    factionColor
                );

            selectionMarkerRenderer.sharedMaterial =
                markerMaterial;
        }
    }

    private Material CreateRuntimeMaterial(
        string diffuseResource,
        string normalResource,
        Color fallbackColor)
    {
        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Lit"
            );

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Standard"
                );
        }

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Legacy Shaders/Diffuse"
                );
        }

        Material material =
            new Material(shader);

        material.color =
            fallbackColor;

        if (!string.IsNullOrWhiteSpace(
                diffuseResource))
        {
            Texture2D diffuse =
                Resources.Load<Texture2D>(
                    diffuseResource
                );

            if (diffuse != null)
            {
                material.mainTexture =
                    diffuse;

                material.color =
                    Color.white;
            }
        }

        if (!string.IsNullOrWhiteSpace(
                normalResource))
        {
            Texture2D normal =
                Resources.Load<Texture2D>(
                    normalResource
                );

            if (normal != null)
            {
                material.SetTexture(
                    "_BumpMap",
                    normal
                );

                material.EnableKeyword(
                    "_NORMALMAP"
                );
            }
        }

        return material;
    }

    private void ApplyMaterialRecursively(
        GameObject target,
        Material material)
    {
        if (target == null ||
            material == null)
        {
            return;
        }

        Renderer[] renderers =
            target.GetComponentsInChildren<
                Renderer
            >(true);

        foreach (Renderer renderer
            in renderers)
        {
            if (renderer != null)
            {
                renderer.sharedMaterial =
                    material;
            }
        }
    }

    public void BeginTurn()
    {
        if (!IsAlive)
            return;

        TurnStartWorldPosition =
            transform.position;

        rangedWeaponsFiredThisTurn.Clear();
        rangedFireGroupThisTurn = "";
        HasCompletedShootingThisTurn = false;
    }

    public bool HasFiredRangedWeaponThisTurn(
        WeaponData weapon)
    {
        if (weapon == null)
            return false;

        return rangedWeaponsFiredThisTurn
            .Contains(
                WeaponUsageKey(
                    weapon
                )
            );
    }

    private string RangedFireGroup(
        WeaponData weapon)
    {
        bool closeQuarters =
            RulesEngine.HasKeyword(
                weapon,
                "pistol"
            ) ||
            WeaponRuleParser.Has(
                weapon,
                "close_quarters"
            );

        return closeQuarters
            ? "close"
            : "normal";
    }

    public bool CanFireRangedWeaponThisTurn(
        WeaponData weapon)
    {
        if (weapon == null ||
            HasCompletedShootingThisTurn ||
            !CanUseWeapon(
                weapon) ||
            HasFiredRangedWeaponThisTurn(
                weapon))
        {
            return false;
        }

        string group =
            RangedFireGroup(
                weapon
            );

        return
            string.IsNullOrWhiteSpace(
                rangedFireGroupThisTurn) ||
            rangedFireGroupThisTurn ==
                group;
    }

    public void MarkRangedWeaponFiredThisTurn(
        WeaponData weapon)
    {
        if (weapon == null)
            return;

        string group =
            RangedFireGroup(
                weapon
            );

        if (string.IsNullOrWhiteSpace(
                rangedFireGroupThisTurn))
        {
            rangedFireGroupThisTurn =
                group;
        }

        rangedWeaponsFiredThisTurn.Add(
            WeaponUsageKey(
                weapon
            )
        );

        bool anyRemaining =
            rangedWeapons.Any(
                candidate =>
                    candidate != null &&
                    RangedFireGroup(
                        candidate) ==
                        rangedFireGroupThisTurn &&
                    CanUseWeapon(
                        candidate) &&
                    !HasFiredRangedWeaponThisTurn(
                        candidate)
            );

        if (!anyRemaining)
            HasCompletedShootingThisTurn = true;
    }

    public void CompleteShootingThisTurn()
    {
        HasCompletedShootingThisTurn = true;
    }

    public float DistanceMovedFromTurnStart(
        Vector3 destination)
    {
        return HorizontalDistance(
            TurnStartWorldPosition,
            destination
        );
    }

    public bool CanUseWeapon(
        WeaponData weapon)
    {
        if (weapon == null)
            return false;

        if (!WeaponRuleParser.Has(
            weapon,
            "one_shot"))
        {
            return true;
        }

        return !oneShotWeaponsUsed.Contains(
            WeaponUsageKey(weapon)
        );
    }

    public void MarkWeaponUsed(
        WeaponData weapon)
    {
        if (weapon == null ||
            !WeaponRuleParser.Has(
                weapon,
                "one_shot"))
        {
            return;
        }

        oneShotWeaponsUsed.Add(
            WeaponUsageKey(weapon)
        );
    }

    private string WeaponUsageKey(
        WeaponData weapon)
    {
        return
            !string.IsNullOrWhiteSpace(
                weapon.id)
            ? weapon.id
            : (weapon.displayName ?? "weapon");
    }

    public int ApplyDamage(int damage)
    {
        if (!IsAlive || damage <= 0)
            return 0;

        int lost =
            Mathf.Min(
                CurrentWounds,
                damage
            );

        CurrentWounds -= lost;

        RefreshWoundDisplay();

        if (CurrentWounds <= 0)
        {
            gameObject.SetActive(false);

            if (GameController.Current != null)
            {
                GameController.Current.NotifyModelDestroyed(
                    Squad);
            }
        }

        return lost;
    }

    public void Revive(
        int wounds,
        Vector3 position)
    {
        transform.position =
            position;

        CurrentWounds =
            Mathf.Clamp(
                wounds,
                1,
                MaxWounds
            );

        gameObject.SetActive(true);

        RefreshWoundDisplay();

        SetWoundDisplayVisible(
            Squad != null &&
            Squad.IsOnBattlefield
        );
    }

    public int Heal(int amount)
    {
        if (!IsAlive || amount <= 0)
            return 0;

        int before =
            CurrentWounds;

        CurrentWounds =
            Mathf.Min(
                MaxWounds,
                CurrentWounds + amount
            );

        RefreshWoundDisplay();

        return CurrentWounds - before;
    }

    public void SetWoundDisplayVisible(
        bool visible)
    {
        woundDisplayRequestedVisible =
            visible;

        if (woundDisplayObject != null)
        {
            woundDisplayObject.SetActive(
                visible &&
                IsAlive
            );
        }
    }

    public void RefreshWoundDisplay()
    {
        if (woundText == null)
            return;

        woundText.text =
            CurrentWounds +
            "/" +
            MaxWounds +
            " W";

        float ratio =
            MaxWounds > 0
            ? (float)CurrentWounds /
              MaxWounds
            : 0f;

        if (ratio > 0.66f)
        {
            woundText.color =
                new Color(
                    0.45f,
                    1.00f,
                    0.45f,
                    1f
                );
        }
        else if (ratio > 0.33f)
        {
            woundText.color =
                new Color(
                    1.00f,
                    0.82f,
                    0.25f,
                    1f
                );
        }
        else
        {
            woundText.color =
                new Color(
                    1.00f,
                    0.30f,
                    0.25f,
                    1f
                );
        }

        if (woundDisplayObject != null)
        {
            woundDisplayObject.SetActive(
                woundDisplayRequestedVisible &&
                IsAlive
            );
        }
    }

    private void CreateWoundDisplay()
    {
        if (woundDisplayObject != null)
            return;

        woundDisplayObject =
            new GameObject(
                "Wound Display"
            );

        woundDisplayObject.transform
            .SetParent(
                transform,
                false
            );

        woundDisplayObject.transform.localPosition =
            new Vector3(
                0f,
                1.55f,
                0f
            );

        woundDisplayObject.transform.localScale =
            Vector3.one;

        woundText =
            woundDisplayObject
                .AddComponent<TextMesh>();

        Font font =
            Resources
                .GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf"
                );

        if (font != null)
        {
            woundText.font = font;

            MeshRenderer textRenderer =
                woundDisplayObject
                    .GetComponent<MeshRenderer>();

            if (textRenderer != null)
            {
                textRenderer.sharedMaterial =
                    font.material;
            }
        }

        woundText.anchor =
            TextAnchor.MiddleCenter;

        woundText.alignment =
            TextAlignment.Center;

        woundText.fontSize = 48;
        woundText.characterSize = 0.055f;
        woundText.fontStyle =
            FontStyle.Bold;

        woundDisplayObject
            .AddComponent<WoundDisplayBillboard>();
    }

    public string EquipmentText()
    {
        string ranged =
            rangedWeapons.Count > 0
            ? string.Join(
                ", ",
                rangedWeapons
                    .Select(
                        weapon =>
                            weapon.displayName
                    )
                    .ToArray()
              )
            : "no ranged weapon";

        string melee =
            meleeWeapons.Count > 0
            ? string.Join(
                ", ",
                meleeWeapons
                    .Select(
                        weapon =>
                            weapon.displayName
                    )
                    .Distinct()
                    .ToArray()
              )
            : "no melee weapon";

        return
            RoleName +
            ": " +
            ranged +
            " / " +
            melee;
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
