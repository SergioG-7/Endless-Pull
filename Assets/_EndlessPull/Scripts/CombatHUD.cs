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

    [Tooltip("Vida de escuadra (0-1) por debajo de la cual la Auto-Retirada, si está armada, se dispara sola.")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float autoRetreatThreshold = 0.20f;

    [Tooltip("Tamaño del panel del acelerador.")]
    [SerializeField] private Vector2 panelSize = new Vector2(260f, 64f);

    [Tooltip("Posición del panel, anclada arriba a la derecha.")]
    [SerializeField] private Vector2 panelPosition = new Vector2(-150f, -24f);

    private RectTransform root;
    private Button speedButton;
    private TMP_Text speedLabel;
    private Button autoRetreatButton;

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
    }

    void OnDisable()
    {
        if (waves != null) waves.ExpeditionChanged -= OnExpeditionChanged;

        // Al desactivarse (p.ej. cambio de escena) no debe quedarse el juego a cámara rápida.
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
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
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            RefreshSpeedLabel();
        }
    }

    void Update()
    {
        if (root == null || waves == null) return;

        bool show = waves.State == ExpeditionState.InProgress;
        if (root.gameObject.activeSelf != show) root.gameObject.SetActive(show);
        if (!show) return;

        if (autoRetreatArmed && !autoRetreatFiredThisRun && waves.DeployedHealthRatio < autoRetreatThreshold)
        {
            autoRetreatFiredThisRun = true;
            waves.RetreatExpedition();
        }
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

        float buttonWidth = panelSize.x * 0.5f - 4f;
        var buttonSize = new Vector2(buttonWidth, panelSize.y);

        speedButton = UIBuild.Button(root, "SpeedButton", "x1", UITheme.Neutral, buttonSize,
            new Vector2(-buttonSize.x * 0.5f - 2f, 0f), ToggleSpeed);
        speedLabel = speedButton.GetComponentInChildren<TMP_Text>();

        autoRetreatButton = UIBuild.Button(root, "AutoRetreatButton",
            LocalizationManager.Get("UI_AUTO_RETREAT"), UITheme.Neutral, buttonSize,
            new Vector2(buttonSize.x * 0.5f + 2f, 0f), ToggleAutoRetreat);

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
        Time.timeScale = fast ? Mathf.Max(1f, fastTimeScale) : 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
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
        if (image != null) image.color = autoRetreatArmed ? UITheme.DangerSoft : UITheme.Neutral;
    }
}
