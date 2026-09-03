using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Hada guía de la base (Isel): patrulla entre plaza/cantina/portal con levitación,
// da pistas contextuales al tocarla y media disputas de héroes desmoralizados en automático.
// Sin arte real disponible: usa un icono procedural (orbe de brillo), mismo criterio que el
// resto del proyecto donde no hay sprite propio (swatches de color en vez de arte final).
public class BaseFairyController : MonoBehaviour
{
    [Tooltip("Velocidad de desplazamiento entre puntos de patrulla.")]
    [SerializeField] private float moveSpeed = 1.2f;

    [Tooltip("Amplitud vertical de la levitación (bobbing).")]
    [SerializeField] private float bobAmplitude = 0.18f;

    [Tooltip("Velocidad de la levitación.")]
    [SerializeField] private float bobSpeed = 2.2f;

    [Tooltip("Radio de toque para pedirle una pista.")]
    [SerializeField] private float touchRadius = 1.1f;

    [Tooltip("Comida por debajo de la cual avisa de héroes hambrientos.")]
    [SerializeField] private int lowFoodThreshold = 120;

    [Tooltip("Segundos que se enseña el bocadillo de pista.")]
    [SerializeField] private float hintShowSeconds = 4f;

    [Tooltip("Rango de segundos entre chequeos de mediación de disputas.")]
    [SerializeField] private Vector2 mediationIntervalRange = new Vector2(25f, 45f);

    [Tooltip("Moral que restaura a cada héroe desmoralizado al mediar.")]
    [SerializeField] private float mediationMoraleGain = 30f;

    public static BaseFairyController Instance { get; private set; }

    private WaveManager waves;
    private CraftingManager crafting;
    private EconomyManager economy;

    private readonly List<Vector2> waypoints = new List<Vector2>();
    private int waypointIndex;
    private Transform visual;
    private TextMeshPro hintLabel;
    private GameObject hintBubble;
    private float hintHideTimer;
    private float mediationTimer;

    void Awake()
    {
        Instance = this;
        waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
        crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();
        economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();

        BuildVisual();
        mediationTimer = Random.Range(mediationIntervalRange.x, mediationIntervalRange.y);
    }

    // TowerGateway.Position y BaseBuilding.All dependen de que esos otros componentes ya se
    // hayan registrado en su propio Awake(); Unity no garantiza el orden entre Awake() de
    // distintos scripts, así que hay que esperar a Start() para leerlos sin arriesgar (0,0) falso.
    void Start()
    {
        BuildWaypoints();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Plaza (origen de la base), Cantina (si existe ya) y Portal de la Torre.
    private void BuildWaypoints()
    {
        waypoints.Add(Vector2.zero);
        waypoints.Add(TowerGateway.Position);

        foreach (var building in BaseBuilding.All)
        {
            if (building != null && building.Type == BuildingType.Canteen)
            {
                waypoints.Add(building.transform.position);
                break;
            }
        }

        transform.position = waypoints[0];
    }

    void Update()
    {
        Patrol();
        Bob();
        TickHint();
        TickMediation();
    }

    private void Patrol()
    {
        if (waypoints.Count == 0) return;

        Vector2 target = waypoints[waypointIndex];
        Vector2 current = transform.position;
        Vector2 next = Vector2.MoveTowards(current, target, moveSpeed * Time.deltaTime);
        transform.position = next;

        if (Vector2.Distance(next, target) < 0.05f)
            waypointIndex = (waypointIndex + 1) % waypoints.Count;
    }

    private void Bob()
    {
        if (visual == null) return;
        visual.localPosition = new Vector3(0f, Mathf.Sin(Time.time * bobSpeed) * bobAmplitude, 0f);
    }

    private void TickHint()
    {
        if (hintHideTimer <= 0f) return;

        hintHideTimer -= Time.deltaTime;
        if (hintHideTimer <= 0f && hintBubble != null) hintBubble.SetActive(false);
    }

    // Consejos y pistas al clic (WorldInteractionManager la detecta y llama aquí).
    public void OnTapped()
    {
        Say(PickHint());
    }

    public bool IsWithinTouch(Vector2 point) => Vector2.Distance(transform.position, point) <= touchRadius;

    private string PickHint()
    {
        if (economy != null && economy.Food < lowFoodThreshold)
            return LocalizationManager.Get("FAIRY_HINT_HUNGRY");

        if (crafting != null)
        {
            foreach (AscensionStoneTier tier in System.Enum.GetValues(typeof(AscensionStoneTier)))
                if (crafting.HasStone(tier)) return LocalizationManager.Get("FAIRY_HINT_STONE_READY");
        }

        if (waves != null)
        {
            int nextFloor = waves.HighestSelectableFloor;
            FloorMissionType nextType = waves.PeekMissionType(nextFloor);
            if (nextType != FloorMissionType.Subjugation)
            {
                string tipo = LocalizationManager.Get(MissionTitleKey(nextType));
                return string.Format(LocalizationManager.Get("FAIRY_HINT_FLOOR_ALERT"), nextFloor, tipo);
            }
        }

        return LocalizationManager.Get(Random.value < 0.5f ? "FAIRY_HINT_IDLE_1" : "FAIRY_HINT_IDLE_2");
    }

    private static string MissionTitleKey(FloorMissionType type) => type switch
    {
        FloorMissionType.Survival => "MISSION_TITLE_SURVIVAL",
        FloorMissionType.Escort => "MISSION_TITLE_ESCORT",
        FloorMissionType.BossHunt => "MISSION_TITLE_BOSSHUNT",
        _ => "MISSION_TITLE_SUBJUGATION"
    };

    // Detiene disputas menores: si hay 2+ héroes desmoralizados en base a la vez (no desplegados),
    // les sube la moral de golpe para sacarlos del umbral que ya penaliza velocidad/cadencia.
    private void TickMediation()
    {
        mediationTimer -= Time.deltaTime;
        if (mediationTimer > 0f) return;

        mediationTimer = Random.Range(mediationIntervalRange.x, mediationIntervalRange.y);

        var upset = new List<HeroController>();
        foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
        {
            if (hero == null || hero.IsDeployed || !hero.IsDemoralized) continue;
            upset.Add(hero);
        }

        if (upset.Count < 2) return;

        foreach (var hero in upset) hero.AddMorale(mediationMoraleGain);

        Say(LocalizationManager.Get("FAIRY_MEDIATION_TOAST"));
        Debug.Log($"[Isel] Media entre {upset.Count} héroes desmoralizados: +{mediationMoraleGain} moral a cada uno.", this);
    }

    private void Say(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (hintBubble == null) BuildBubble();

        hintLabel.text = text;
        hintBubble.SetActive(true);
        hintHideTimer = hintShowSeconds;
    }

    // Orbe de brillo con núcleo dorado; sin arte final disponible, mismo criterio de swatch de
    // color que el resto del proyecto (ver StorageUI/AlchemyWorkshopUI).
    private void BuildVisual()
    {
        var visualGo = new GameObject("Visual");
        visualGo.transform.SetParent(transform, false);
        visual = visualGo.transform;

        var outer = new GameObject("Glow", typeof(SpriteRenderer));
        outer.transform.SetParent(visual, false);
        var outerRenderer = outer.GetComponent<SpriteRenderer>();
        outerRenderer.sprite = GlowSprite();
        outerRenderer.color = new Color(0.4f, 0.85f, 1f, 0.55f);
        outerRenderer.transform.localScale = Vector3.one * 0.9f;
        outerRenderer.sortingLayerName = "Characters";

        var core = new GameObject("Core", typeof(SpriteRenderer));
        core.transform.SetParent(visual, false);
        var coreRenderer = core.GetComponent<SpriteRenderer>();
        coreRenderer.sprite = GlowSprite();
        coreRenderer.color = new Color(1f, 0.92f, 0.65f, 0.95f);
        coreRenderer.transform.localScale = Vector3.one * 0.35f;
        coreRenderer.sortingLayerName = "Characters";
        coreRenderer.sortingOrder = 1;
    }

    private static Sprite cachedGlow;
    private static Sprite GlowSprite()
    {
        if (cachedGlow != null) return cachedGlow;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float t = Mathf.Clamp01(1f - dist / (size / 2f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(t, 1.8f)));
            }
        }

        tex.Apply();
        cachedGlow = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedGlow;
    }

    private void BuildBubble()
    {
        hintBubble = new GameObject("IselHint", typeof(TextMeshPro));
        hintBubble.transform.SetParent(transform, false);
        hintBubble.transform.localPosition = new Vector3(0f, 1.1f, 0f);

        var backdropGo = new GameObject("Backdrop", typeof(SpriteRenderer));
        backdropGo.transform.SetParent(hintBubble.transform, false);
        var backdrop = backdropGo.GetComponent<SpriteRenderer>();
        backdrop.sprite = Resources.Load<Sprite>("UI/UI_Rounded");
        backdrop.drawMode = SpriteDrawMode.Sliced;
        backdrop.size = new Vector2(5.6f, 1.5f);
        backdrop.color = UITheme.Hex("141A26", 0.92f);
        // Sin capa explícita cae en "Default", por debajo de "Characters": el bocadillo se
        // dibujaba detrás de Isel, invisible aunque estuviera activo.
        backdrop.sortingLayerName = "Characters";
        backdrop.sortingOrder = 19;

        var borderGo = new GameObject("Border", typeof(SpriteRenderer));
        borderGo.transform.SetParent(hintBubble.transform, false);
        var border = borderGo.GetComponent<SpriteRenderer>();
        border.sprite = Resources.Load<Sprite>("UI/UI_RoundedRingThin");
        border.drawMode = SpriteDrawMode.Sliced;
        border.size = new Vector2(5.6f, 1.5f);
        border.color = UITheme.Cyan;
        border.sortingLayerName = "Characters";
        border.sortingOrder = 20;

        hintLabel = hintBubble.GetComponent<TextMeshPro>();
        // Fuente fija de 1.05 salía casi ilegible sin hacer zoom; auto-sizing con un mínimo
        // más alto y ajuste de línea para que los consejos largos quepan en dos líneas.
        hintLabel.enableAutoSizing = true;
        hintLabel.fontSizeMin = 1.4f;
        hintLabel.fontSizeMax = 2.1f;
        hintLabel.enableWordWrapping = true;
        hintLabel.alignment = TextAlignmentOptions.Center;
        hintLabel.color = UITheme.Text;
        hintLabel.sortingLayerID = SortingLayer.NameToID("Characters");
        hintLabel.sortingOrder = 21;
        hintLabel.rectTransform.sizeDelta = new Vector2(5.2f, 1.4f);

        hintBubble.SetActive(false);
    }
}
