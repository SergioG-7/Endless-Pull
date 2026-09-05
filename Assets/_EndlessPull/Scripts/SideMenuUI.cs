using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Menú lateral colapsable: agrupa los botones sueltos en secciones y respeta la zona segura.
public class SideMenuUI : MonoBehaviour
{
    [Tooltip("Contenedor con los botones que se van a agrupar.")]
    [SerializeField] private RectTransform sidebar;

    [Tooltip("Arranca plegado; por defecto no, para que el menú se vea nada más entrar.")]
    [SerializeField] private bool startCollapsed;

    [Tooltip("Ancho del cajón, según el mockup.")]
    [SerializeField] private float drawerWidth = 216f;

    [Tooltip("Alto del botón que abre y cierra el menú.")]
    [SerializeField] private float toggleHeight = 64f;

    [Tooltip("Alto de cada rótulo de sección.")]
    [SerializeField] private float headerHeight = 22f;

    [Tooltip("Alto de cada opción del menú.")]
    [SerializeField] private float itemHeight = 40f;

    [Tooltip("Margen mínimo contra los bordes, además de la zona segura del dispositivo.")]
    [SerializeField] private float safeMargin = 40f;

    [Tooltip("Barra de decretos: mientras se vea, el menú de gestión se esconde.")]
    [SerializeField] private MasterActionBar combatBar;

    [Tooltip("Barra superior, que arranca justo a la derecha del cajón.")]
    [SerializeField] private RectTransform topBar;

    [Tooltip("Hueco entre el cajón y la barra superior.")]
    [SerializeField] private float topBarGap = 24f;

    [Tooltip("Alto de la barra superior, según el mockup.")]
    [SerializeField] private float topBarHeight = 64f;

    // Cada sección con los botones que le tocan, por nombre de GameObject.
    private static readonly (string clave, string[] botones)[] Secciones =
    {
        ("UI_SECTION_MANAGEMENT", new[] { "Btn_Tower_Open", "Btn_Squads", "Btn_Expeditions_Open", "Btn_Quests" }),
        ("UI_SECTION_STAFF", new[] { "Btn_Roster", "Btn_Pull", "Btn_HealAll" }),
        ("UI_SECTION_FACILITIES", new[] { "Btn_Craft_Open", "Btn_Shop_Open", "Btn_Sanctuary_Open",
                                          "Btn_Gallery_Open" })
    };

    // Los botones venían con el texto en español fijo en el prefab: aquí se cablean a sus claves.
    private static readonly Dictionary<string, string> ButtonLabelKeys = new Dictionary<string, string>
    {
        { "Btn_Tower_Open", "UI_TOWER" },
        { "Btn_Squads", "UI_SQUADS" },
        { "Btn_Expeditions_Open", "UI_EXPEDITIONS" },
        { "Btn_Quests", "UI_QUESTS" },
        { "Btn_Roster", "UI_VIEW_HEROES" },
        { "Btn_Pull", "UI_SUMMON" },
        { "Btn_HealAll", "UI_HEAL_ALL" },
        { "Btn_Craft_Open", "UI_CRAFT" },
        { "Btn_Shop_Open", "UI_SHOP" },
        { "Btn_Sanctuary_Open", "UI_SANCTUARY" },
        { "Btn_Gallery_Open", "BLD_GALLERY" }
    };

    private GameObject body;
    private RectTransform viewport;
    private Button toggleButton;
    private TMP_Text toggleLabel;
    private readonly List<TMP_Text> headers = new List<TMP_Text>();
    private readonly List<string> headerKeys = new List<string>();
    private readonly List<TMP_Text> buttonLabels = new List<TMP_Text>();
    private readonly List<string> buttonLabelKeys = new List<string>();
    private bool open;
    private Transform materiales;

    public bool IsOpen => open;

    void Awake()
    {
        if (sidebar == null)
        {
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas != null) sidebar = canvas.transform.Find("Sidebar") as RectTransform;
        }

        if (combatBar == null) combatBar = UnityEngine.Object.FindFirstObjectByType<MasterActionBar>();

        if (topBar == null && sidebar != null && sidebar.parent != null)
            topBar = sidebar.parent.Find("TopBar") as RectTransform;

        Build();
    }

    void OnEnable() => LocalizationManager.LanguageChanged += RefreshTexts;
    void OnDisable() => LocalizationManager.LanguageChanged -= RefreshTexts;

    void Start()
    {
        SetOpen(!startCollapsed);
    }

    // Cada menú en su sitio: en la arena solo se ven los decretos, en la base solo la gestión.
    void Update()
    {
        if (sidebar == null) return;

        bool enCombate = combatBar != null && combatBar.ShouldShow;
        if (sidebar.gameObject.activeSelf == enCombate) sidebar.gameObject.SetActive(!enCombate);

        // El ancho real de ventana tarda un frame o más en estabilizarse tras cargar la escena;
        // calcularlo solo una vez en Build() podía dejar el chip escondido para siempre con un
        // Screen.width todavía sin asentar. Se revisa cada frame, es una comparación barata.
        RefreshMaterialsVisibility();
    }

    private void RefreshMaterialsVisibility()
    {
        if (materiales == null || topBar == null) return;

        var canvas = sidebar != null ? sidebar.GetComponentInParent<Canvas>() : null;
        float escala = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        var segura = Screen.safeArea;
        float izquierda = safeMargin + segura.x / escala;
        float derecha = safeMargin + (Screen.width - segura.xMax) / escala;
        float anchoCanvas = Screen.width / escala;
        float anchoTopBar = anchoCanvas - derecha - (izquierda + drawerWidth + topBarGap);

        materiales.gameObject.SetActive(anchoTopBar >= 900f);
    }

    public void Toggle() => SetOpen(!open);

    public void SetOpen(bool value)
    {
        open = value;

        // Se esconde el viewport entero: si solo se ocultaba el cuerpo quedaba su recuadro oscuro.
        if (viewport != null) viewport.gameObject.SetActive(open);
        if (body != null) body.SetActive(true);

        RefreshTexts();
    }

    private void RefreshTexts()
    {
        // El identico U+2261 hace de hamburguesa: la fuente no trae el simbolo U+2630.
        if (toggleLabel != null)
            toggleLabel.text = (open ? "≡  " : "≡  ") + LocalizationManager.Get("UI_MENU");

        for (int i = 0; i < headers.Count; i++)
            headers[i].text = LocalizationManager.Get(headerKeys[i]).ToUpperInvariant();

        for (int i = 0; i < buttonLabels.Count; i++)
            buttonLabels[i].text = LocalizationManager.Get(buttonLabelKeys[i]);
    }

    // Reordena lo que ya hay: el botón de abrir arriba y los existentes bajo su sección.
    private void Build()
    {
        if (sidebar == null) return;

        // El canvas escala con el ancho: mezclar pixeles con unidades sacaba el menu de pantalla.
        var canvas = sidebar.GetComponentInParent<Canvas>();
        float escala = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        float altoCanvas = Screen.height / escala;

        // La zona segura evita que el menú quede bajo el notch o la barra de gestos.
        var segura = Screen.safeArea;
        float izquierda = safeMargin + segura.x / escala;
        float arriba = safeMargin + (Screen.height - segura.yMax) / escala;

        sidebar.anchoredPosition = new Vector2(izquierda, -arriba);
        sidebar.sizeDelta = new Vector2(drawerWidth, sidebar.sizeDelta.y);

        // La barra superior arranca donde acaba el cajón y respeta la misma zona segura.
        if (topBar != null)
        {
            float derecha = safeMargin + (Screen.width - segura.xMax) / escala;
            topBar.offsetMin = new Vector2(izquierda + drawerWidth + topBarGap, -arriba - topBarHeight);
            topBar.offsetMax = new Vector2(-derecha, -arriba);

            // En pantallas muy estrechas (móvil vertical) no cabe el chip de materiales: se
            // esconde para que Gemas y Piso no se corten ni se solapen. Ver RefreshMaterialsVisibility,
            // que repite este cálculo cada frame (Screen.width al arrancar puede venir stale).
            materiales = topBar.Find("Txt_Materials");
        }

        // El botón se ancla arriba del todo; el cajón cuelga justo debajo.
        toggleButton = CreateToggle();

        var trt = toggleButton.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.sizeDelta = new Vector2(0f, toggleHeight);
        trt.anchoredPosition = Vector2.zero;
        toggleLabel = toggleButton.GetComponentInChildren<TMP_Text>();
        toggleButton.onClick.AddListener(Toggle);

        // Viewport con scroll: con tres secciones no caben todas las opciones de golpe.
        var viewGo = new GameObject("MenuViewport", typeof(RectTransform), typeof(Image),
                                    typeof(Mask), typeof(ScrollRect));
        viewGo.transform.SetParent(sidebar, false);
        viewport = viewGo.GetComponent<RectTransform>();
        UITheme.Surface(viewGo, UITheme.GlassDeep, UITheme.Hex("E9E9ED", 0.08f), UITheme.RadiusDrawer);
        viewGo.GetComponent<Mask>().showMaskGraphic = true;

        body = new GameObject("MenuBody", typeof(RectTransform));
        body.transform.SetParent(viewGo.transform, false);

        var brt = body.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 1f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.sizeDelta = new Vector2(0f, 0f);
        brt.anchoredPosition = Vector2.zero;

        var scroll = viewGo.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = brt;
        scroll.horizontal = false;
        scroll.scrollSensitivity = 30f;

        // Margen interior de 14 y separación de 14 entre secciones, como en el mockup.
        var layout = body.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        // Cada sección mide lo que suman sus opciones; sin esto las cabeceras se solapaban.
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        body.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        foreach (var seccion in Secciones)
        {
            var grupo = CreateSection(seccion.clave);

            var header = CreateHeader(grupo, seccion.clave);
            headers.Add(header);
            headerKeys.Add(seccion.clave);

            foreach (var nombre in seccion.botones)
            {
                var boton = sidebar.Find(nombre);
                if (boton == null) continue;

                boton.SetParent(grupo, false);
                boton.SetAsLastSibling();
                StyleItem(boton);

                if (ButtonLabelKeys.TryGetValue(nombre, out string labelKey))
                {
                    var label = boton.GetComponentInChildren<TMP_Text>(true);
                    if (label != null)
                    {
                        buttonLabels.Add(label);
                        buttonLabelKeys.Add(labelKey);
                    }
                }
            }
        }

        // El alto del cajón sale de lo que queda de pantalla bajo el botón, con hueco abajo.
        float abajo = safeMargin + segura.y / escala;
        float disponible = Mathf.Max(200f, altoCanvas - arriba - abajo - toggleHeight - 10f);

        sidebar.sizeDelta = new Vector2(drawerWidth, toggleHeight + 10f + disponible);

        viewport.anchorMin = new Vector2(0f, 1f);
        viewport.anchorMax = new Vector2(1f, 1f);
        viewport.pivot = new Vector2(0.5f, 1f);
        viewport.sizeDelta = new Vector2(0f, disponible);
        viewport.anchoredPosition = new Vector2(0f, -(toggleHeight + 10f));
    }

    // Cada sección es su propio grupo: dentro los botones se separan 6, fuera 14.
    private Transform CreateSection(string key)
    {
        var go = new GameObject("Section_" + key, typeof(RectTransform));
        go.transform.SetParent(body.transform, false);

        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        return go.transform;
    }

    private TMP_Text CreateHeader(Transform parent, string key)
    {
        var go = new GameObject("Header_" + key, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, headerHeight);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = UITheme.SizeMicro;
        tmp.fontStyle = FontStyles.Bold;
        tmp.characterSpacing = 10f;
        tmp.alignment = TextAlignmentOptions.BottomLeft;
        tmp.color = UITheme.Hex("A7A1DB", 0.85f);
        tmp.raycastTarget = false;
        tmp.text = LocalizationManager.Get(key).ToUpperInvariant();
        return tmp;
    }

    // Opción del cajón: fondo transparente, borde fino y texto a la izquierda.
    private void StyleItem(Transform item)
    {
        var rt = item as RectTransform;
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, itemHeight);
            // Un botón colocado a mano en el Editor puede traer un localScale distinto de 1
            // (p.ej. Btn_Sanctuary_Open llegó a 1.14): fuerza que todos midan igual.
            rt.localScale = Vector3.one;
        }

        UITheme.Surface(item.gameObject, Color.clear, UITheme.Border, UITheme.RadiusItem);

        var tmp = item.GetComponentInChildren<TMP_Text>(true);
        if (tmp == null) return;

        tmp.fontSize = UITheme.SizeBody;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = UITheme.Text;
        tmp.rectTransform.offsetMin = new Vector2(12f, 0f);
        tmp.rectTransform.offsetMax = new Vector2(-12f, 0f);
    }

    private Button CreateToggle()
    {
        var go = new GameObject("Btn_MenuToggle", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(sidebar, false);
        go.transform.SetAsFirstSibling();
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, toggleHeight);

        var image = UITheme.Surface(go, UITheme.Glass, UITheme.Border, UITheme.RadiusDrawer);

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 15f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = UITheme.Text;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }
}
