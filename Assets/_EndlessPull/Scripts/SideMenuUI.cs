using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Menú lateral colapsable: agrupa los botones sueltos en secciones y respeta la zona segura.
public class SideMenuUI : MonoBehaviour
{
    [Tooltip("Contenedor con los botones que se van a agrupar.")]
    [SerializeField] private RectTransform sidebar;

    [Tooltip("Arranca plegado; en móvil deja la pantalla despejada.")]
    [SerializeField] private bool startCollapsed = true;

    [Tooltip("Alto del botón que abre y cierra el menú.")]
    [SerializeField] private float toggleHeight = 84f;

    [Tooltip("Alto de cada rótulo de sección.")]
    [SerializeField] private float headerHeight = 34f;

    [Tooltip("Margen mínimo contra los bordes, además de la zona segura del dispositivo.")]
    [SerializeField] private float safeMargin = 40f;

    [Tooltip("Color del botón que abre el menú.")]
    [SerializeField] private Color toggleColor = new Color(0.22f, 0.20f, 0.34f);

    [Tooltip("Color del rótulo de sección.")]
    [SerializeField] private Color headerColor = new Color(0.75f, 0.68f, 0.45f);

    [Tooltip("Barra de decretos: mientras se vea, el menú de gestión se esconde.")]
    [SerializeField] private MasterActionBar combatBar;

    // Cada sección con los botones que le tocan, por nombre de GameObject.
    private static readonly (string clave, string[] botones)[] Secciones =
    {
        ("UI_SECTION_MANAGEMENT", new[] { "Btn_Tower_Open", "Btn_Expeditions_Open" }),
        ("UI_SECTION_STAFF", new[] { "Btn_Roster", "Btn_Pull", "Btn_HealAll" }),
        ("UI_SECTION_FACILITIES", new[] { "Btn_Craft_Open", "Btn_Shop_Open" })
    };

    private GameObject body;
    private RectTransform viewport;
    private Button toggleButton;
    private TMP_Text toggleLabel;
    private readonly List<TMP_Text> headers = new List<TMP_Text>();
    private readonly List<string> headerKeys = new List<string>();
    private bool open;

    public bool IsOpen => open;

    void Awake()
    {
        if (sidebar == null)
        {
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas != null) sidebar = canvas.transform.Find("Sidebar") as RectTransform;
        }

        if (combatBar == null) combatBar = UnityEngine.Object.FindFirstObjectByType<MasterActionBar>();

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
    }

    public void Toggle() => SetOpen(!open);

    public void SetOpen(bool value)
    {
        open = value;
        if (body != null) body.SetActive(open);
        RefreshTexts();
    }

    private void RefreshTexts()
    {
        // El identico U+2261 hace de hamburguesa: la fuente no trae el simbolo U+2630.
        if (toggleLabel != null)
            toggleLabel.text = (open ? "≡  " : "≡  ") + LocalizationManager.Get("UI_MENU");

        for (int i = 0; i < headers.Count; i++)
            headers[i].text = LocalizationManager.Get(headerKeys[i]).ToUpperInvariant();
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

        sidebar.anchoredPosition = new Vector2(izquierda, -(arriba + 90f));

        // El botón se ancla arriba del todo; el scroll cuelga justo debajo.
        toggleButton = CreateButton("Btn_MenuToggle", toggleColor, toggleHeight);
        toggleButton.transform.SetAsFirstSibling();

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
        viewGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
        viewGo.GetComponent<Mask>().showMaskGraphic = true;

        body = new GameObject("MenuBody", typeof(RectTransform));
        body.transform.SetParent(viewGo.transform, false);

        var brt = body.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 1f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.anchoredPosition = Vector2.zero;

        var scroll = viewGo.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = brt;
        scroll.horizontal = false;
        scroll.scrollSensitivity = 30f;

        var layout = body.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        body.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        foreach (var seccion in Secciones)
        {
            var header = CreateHeader(seccion.clave);
            headers.Add(header);
            headerKeys.Add(seccion.clave);

            foreach (var nombre in seccion.botones)
            {
                var boton = sidebar.Find(nombre);
                if (boton == null) continue;

                boton.SetParent(body.transform, false);
                boton.SetAsLastSibling();
            }
        }

        // El alto del scroll sale de lo que queda de pantalla bajo el botón, con hueco abajo.
        float abajo = safeMargin + segura.y / escala;
        float disponible = Mathf.Max(200f, altoCanvas - arriba - abajo - toggleHeight - 130f);

        sidebar.sizeDelta = new Vector2(sidebar.sizeDelta.x, toggleHeight + disponible);

        viewport.anchorMin = new Vector2(0f, 1f);
        viewport.anchorMax = new Vector2(1f, 1f);
        viewport.pivot = new Vector2(0.5f, 1f);
        viewport.sizeDelta = new Vector2(0f, disponible);
        viewport.anchoredPosition = new Vector2(0f, -(toggleHeight + 10f));

        // Todos los nombres del menú al mismo tamaño, incluidos los que venían de la escena.
        foreach (var etiqueta in body.GetComponentsInChildren<TMP_Text>(true))
            etiqueta.fontSize = UIBuild.NameSize;
    }

    private TMP_Text CreateHeader(string key)
    {
        var go = new GameObject("Header_" + key, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(body.transform, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, headerHeight);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 22f;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = headerColor;
        tmp.raycastTarget = false;
        tmp.text = LocalizationManager.Get(key).ToUpperInvariant();
        return tmp;
    }

    private Button CreateButton(string name, Color color, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(sidebar, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, height);

        var image = go.GetComponent<Image>();
        image.color = color;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = UIBuild.NameSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }
}
