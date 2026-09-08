using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Acelerador de combate (1x/2x) y Auto-Retirada. Se monta solo sobre el Canvas de la escena,
// igual que ScreenBanner o MasterActionBar; no necesita cableado manual en el Inspector.
public class CombatHUD : MonoBehaviour
{
    [Tooltip("Gestor de oleadas: da el estado de la expedición y la vida de la escuadra.")]
    [SerializeField] private WaveManager waves;

    [Tooltip("Canvas donde se monta el panel; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Multiplicador de velocidad del modo rápido.")]
    [SerializeField] private float fastTimeScale = 2f;

    [Tooltip("Vida (0-1) del héroe que peor esté por debajo de la cual salta la Auto-Retirada.")]
    [Range(0.05f, 0.6f)]
    [SerializeField] private float autoRetreatThreshold = 0.25f;

    [Tooltip("Vida media de la escuadra (0-1) por debajo de la cual salta la Auto-Retirada.")]
    [Range(0.05f, 0.8f)]
    [SerializeField] private float autoRetreatSquadThreshold = 0.4f;

    [Tooltip("Volverse en cuanto cae un héroe; con muerte permanente, perder uno ya basta.")]
    [SerializeField] private bool autoRetreatOnHeroDown = true;

    [Tooltip("Tamaño del panel del acelerador.")]
    [SerializeField] private Vector2 panelSize = new Vector2(260f, 64f);

    [Tooltip("Posición del panel, anclada arriba a la derecha.")]
    [SerializeField] private Vector2 panelPosition = new Vector2(-150f, -24f);

    private RectTransform root;
    [Tooltip("Fondo de los botones de velocidad y auto-retirada; opaco para que se lean sobre cualquier suelo.")]
    [SerializeField] private Color buttonBackground = new Color(0.071f, 0.094f, 0.141f, 0.94f);

    [Tooltip("Fondo de la auto-retirada cuando está armada.")]
    [SerializeField] private Color buttonArmedBackground = new Color(0.290f, 0.098f, 0.129f, 0.94f);

    private Button speedButton;
    private TMP_Text speedLabel;
    private Button autoRetreatButton;
    private TMP_Text autoRetreatLabel;

    private bool fast;
    private bool autoRetreatArmed;
    private bool autoRetreatFiredThisRun;

    void Awake()
    {
        if (waves == null) waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        Build();
    }

    void OnEnable()
    {
        if (waves != null) waves.ExpeditionChanged += OnExpeditionChanged;
        LocalizationManager.LanguageChanged += RefreshLocalizedTexts;
    }

    void OnDisable()
    {
        if (waves != null) waves.ExpeditionChanged -= OnExpeditionChanged;
        LocalizationManager.LanguageChanged -= RefreshLocalizedTexts;

        // Al desactivarse (p.ej. cambio de escena) no debe quedarse el juego a cámara rápida.
        CombatFeelManager.SetNormalTimeScale(1f);
    }

    // Los botones se construían una vez en Awake() y se quedaban en el idioma de entonces
    // para siempre, aunque el resto del HUD sí reaccionara al cambio de idioma.
    private void RefreshLocalizedTexts()
    {
        if (autoRetreatLabel != null) autoRetreatLabel.text = LocalizationManager.Get("UI_AUTO_RETREAT");
    }

    private void OnExpeditionChanged(ExpeditionState state, string message)
    {
        // Cada expedición nueva vuelve a poder disparar la retirada automática una vez.
        if (state == ExpeditionState.InProgress) autoRetreatFiredThisRun = false;

        // Al salir de combate (victoria, derrota o retirada) se acaba la cámara rápida sí o sí,
        // aunque el jugador nunca haya pulsado el botón manualmente.
        if (state != ExpeditionState.InProgress && fast)
        {
            fast = false;
            CombatFeelManager.SetNormalTimeScale(1f);
            RefreshSpeedLabel();
        }
    }

    void Update()
    {
        if (root == null || waves == null) return;

        bool show = waves.State == ExpeditionState.InProgress;
        if (root.gameObject.activeSelf != show) root.gameObject.SetActive(show);
        if (!show) return;

        if (autoRetreatArmed && !autoRetreatFiredThisRun && ShouldAutoRetreat())
        {
            autoRetreatFiredThisRun = true;
            waves.RetreatExpedition();
        }
    }

    // La media de la escuadra por sí sola no servía: con cuatro sanos y uno agonizando da 0,8 y
    // no saltaba nunca, que es lo que se veía en partida. Ahora vale con que uno esté al límite
    // o con que haya caído alguien.
    private bool ShouldAutoRetreat()
    {
        if (autoRetreatOnHeroDown && waves.AnyHeroFallenThisFloor) return true;
        if (waves.LowestDeployedHealthRatio < autoRetreatThreshold) return true;

        return waves.DeployedHealthRatio < autoRetreatSquadThreshold;
    }

    private void Build()
    {
        if (canvas == null) return;

        var go = new GameObject("CombatHUD_Panel", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        root = go.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(1f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(1f, 1f);
        root.sizeDelta = panelSize;
        root.anchoredPosition = panelPosition;

        // Sin botón de Retirada aquí: duplicaba el que ya vive en el panel de decretos del
        // comandante (MasterActionBar/DEC_RETREAT), que es el que se usa de verdad.
        float buttonWidth = panelSize.x / 2f - 2f;
        var buttonSize = new Vector2(buttonWidth, panelSize.y);

        // Fondo oscuro opaco en vez del Neutral de siempre (blanco al 6%): sobre los suelos
        // claros de la arena los dos botones se leían como texto flotando sin caja.
        speedButton = UIBuild.Button(root, "SpeedButton", "x1", buttonBackground, buttonSize,
            new Vector2(-buttonWidth / 2f - 2f, 0f), ToggleSpeed);
        speedLabel = speedButton.GetComponentInChildren<TMP_Text>();

        autoRetreatButton = UIBuild.Button(root, "AutoRetreatButton",
            LocalizationManager.Get("UI_AUTO_RETREAT"), buttonBackground, buttonSize,
            new Vector2(buttonWidth / 2f + 2f, 0f), ToggleAutoRetreat);
        autoRetreatLabel = autoRetreatButton.GetComponentInChildren<TMP_Text>();

        root.gameObject.SetActive(false);
        RefreshSpeedLabel();
        RefreshAutoRetreatLook();
    }

    // La TopBar (SideMenuUI) recalcula su alto/posición real en su propio Awake; aquí se lee
    // ya resuelta porque Start() de todos los scripts corre después de todos los Awake().
    void Start()
    {
        if (canvas == null || root == null) return;

        var topBar = canvas.transform.Find("TopBar") as RectTransform;
        if (topBar == null) return;

        const float gapBelowTopBar = 12f;
        root.anchoredPosition = new Vector2(panelPosition.x, topBar.offsetMin.y - gapBelowTopBar);
    }

    private void ToggleSpeed()
    {
        fast = !fast;

        // Pasa por el CombatFeelManager: es quien sabe a qué velocidad volver tras un hitstop.
        CombatFeelManager.SetNormalTimeScale(fast ? Mathf.Max(1f, fastTimeScale) : 1f);
        RefreshSpeedLabel();
    }

    private void RefreshSpeedLabel()
    {
        if (speedLabel != null) speedLabel.text = fast ? $"x{fastTimeScale:0.#}" : "x1";
    }

    private void ToggleAutoRetreat()
    {
        autoRetreatArmed = !autoRetreatArmed;
        RefreshAutoRetreatLook();
    }

    private void RefreshAutoRetreatLook()
    {
        if (autoRetreatButton == null) return;

        var image = autoRetreatButton.targetGraphic as Image;
        if (image != null) image.color = autoRetreatArmed ? buttonArmedBackground : buttonBackground;
    }
}
