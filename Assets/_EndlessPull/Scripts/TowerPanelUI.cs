using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Selector de torre: un botón por piso, hasta el siguiente al más alto superado.
public class TowerPanelUI : MonoBehaviour
{
    [Tooltip("Canvas donde se monta el panel; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Gestor de oleadas al que se le pide el piso y la expedición.")]
    [SerializeField] private WaveManager waves;

    [Tooltip("Escuadra de la que salen los intentos de torre.")]
    [SerializeField] private PartyManager party;

    [Tooltip("Modal de escuadra: confirma quién sube antes de arrancar el piso.")]
    [SerializeField] private SquadManagementUI squadUI;

    [Tooltip("Alto de cada fila de piso, en píxeles de UI (min. área táctil ~88px a 1920 de referencia).")]
    [SerializeField] private float rowHeight = UITheme.MinTouchTarget;

    [Tooltip("Tamaño del modal; el mismo rango que el resto de paneles del juego.")]
    [SerializeField] private Vector2 size = new Vector2(1100f, 700f);

    private GameObject panel;
    private RectTransform content;
    private TMP_Text title;
    private TMP_Text info;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (waves == null) waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
        if (party == null) party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();
        if (squadUI == null) squadUI = UnityEngine.Object.FindFirstObjectByType<SquadManagementUI>();

        Build();
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    void OnEnable() => LocalizationManager.LanguageChanged += OnLanguageChanged;
    void OnDisable() => LocalizationManager.LanguageChanged -= OnLanguageChanged;

    // Solo se refrescaba al abrir (Open()); si el idioma cambiaba con el selector de piso ya
    // abierto, título/cabecera/filas se quedaban en el idioma anterior.
    private void OnLanguageChanged()
    {
        if (IsOpen) Rebuild();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (panel == null) return;

        UIManager.OpenExclusive(panel);
        Rebuild();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Rebuild()
    {
        if (content == null || waves == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        title.text = LocalizationManager.Get("UI_TOWER");
        info.text = LocalizationManager.Get("UI_TOWER_SQUAD") + ": "
                  + (party != null ? party.Party.Count + "/" + party.MaxPartySize : "-");

        int top = waves.HighestSelectableFloor;
        content.sizeDelta = new Vector2(content.sizeDelta.x, top * (rowHeight + 6f) + 12f);

        for (int floor = top; floor >= 1; floor--)
            CreateRow(floor);
    }

    private void CreateRow(int floor)
    {
        bool cleared = floor <= waves.HighestClearedFloor;

        string modo = cleared
            ? LocalizationManager.Get("UI_REPEAT")
            : LocalizationManager.Get("UI_FIRST_CLEAR");

        string text = LocalizationManager.Get("UI_FLOOR") + " " + floor + "   -   " + modo
                    + (cleared ? "   (0 " + LocalizationManager.Get("UI_GEMS") + ")" : string.Empty);

        // Los pisos que sacan más de un escuadrón se avisan aquí: es donde el jugador elige,
        // así que es donde puede darse la vuelta a preparar los presets.
        int escuadrones = waves.SquadCountForFloor(floor);
        if (escuadrones > 1)
            text += $"   <color={UITheme.Tag(UITheme.Cyan)}>" +
                    string.Format(LocalizationManager.Get("UI_MULTI_SQUAD"), escuadrones) + "</color>";

        var go = new GameObject("Btn_Floor_" + floor, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(content, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, rowHeight);

        // Mismos tokens del tema que el resto de listas: ámbar para lo que da recompensa,
        // teal para lo ya superado. Antes eran verdes/naranjas planos propios de este panel.
        var image = UITheme.Surface(go, cleared ? UITheme.Teal : UITheme.Amber,
            UITheme.BorderSoft, UITheme.RadiusItem);

        var tmp = UIBuild.Label(go.transform, "Label", UITheme.SizeName, TextAlignmentOptions.Center);
        UIBuild.Stretch(tmp.rectTransform);
        tmp.color = cleared ? UITheme.TextMuted : UITheme.Text;
        tmp.text = text;

        int chosen = floor;
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => AudioManager.Play(SfxId.UiClick));
        button.onClick.AddListener(() => OnFloorPressed(chosen));
        ButtonPressFeedback.Attach(go);
    }

    // Elegir piso abre la confirmación de escuadra; el combate solo arranca si el jugador confirma.
    private void OnFloorPressed(int floor)
    {
        if (waves == null || !waves.SelectFloor(floor)) return;

        Close();

        if (squadUI != null)
        {
            squadUI.OpenForConfirm(true, StartConfirmedFloor);
            return;
        }

        // Sin modal de escuadra en escena, se mantiene el arranque directo de antes.
        StartConfirmedFloor();
    }

    private void StartConfirmedFloor()
    {
        UIManager.CloseEverything();
        waves.StartFloorExpedition();
    }

    private void Build()
    {
        if (canvas == null) return;

        // Mismo constructor que el resto de modales: fondo del tema, esquinas redondeadas y
        // aspa arriba a la derecha. Antes era un Image plano con su propio color y un botón
        // "Cerrar" morado abajo, que no se parecía a ningún otro panel.
        panel = UIBuild.Panel(canvas.transform, "TowerPanel", size, UITheme.Bg);

        title = UIBuild.TopLabel(panel.transform, "Title", UITheme.SizeTitle, 34f, -20f,
            TextAlignmentOptions.Left);

        info = UIBuild.TopLabel(panel.transform, "Info", UITheme.SizeBody, 30f, -60f,
            TextAlignmentOptions.Left);
        info.color = UITheme.TextMuted;

        var viewGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image),
                                    typeof(Mask), typeof(ScrollRect));
        viewGo.transform.SetParent(panel.transform, false);
        UITheme.Surface(viewGo, UITheme.BgPanel, UITheme.BorderSoft, UITheme.RadiusCard);
        viewGo.GetComponent<Mask>().showMaskGraphic = true;

        var vrt = viewGo.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = new Vector2(20f, 20f);
        vrt.offsetMax = new Vector2(-20f, -100f);

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewGo.transform, false);
        content = contentGo.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 100f);

        var layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewGo.GetComponent<ScrollRect>();
        scroll.viewport = vrt;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.scrollSensitivity = 34f;

        UIBuild.CloseButtonTopRight(panel.transform, Close);
    }
}
