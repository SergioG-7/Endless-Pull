using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ficha del Archivo del Santuario: hitos de ascensión registrados y lore ya
// desbloqueado por piso. La abre BuildingInspectUI/similar al visitar el edificio (paso 2).
public class SanctuaryArchiveUI : MonoBehaviour
{
    [Tooltip("Canvas donde se monta la ficha; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Registro de hitos y lore que alimenta esta ficha.")]
    [SerializeField] private SanctuaryArchiveManager archive;

    [Tooltip("Tamaño de la ficha.")]
    [SerializeField] private Vector2 size = new Vector2(880f, 700f);

    private GameObject panel;
    private RectTransform lista;
    private TMP_Text titulo;
    private TMP_Text closeLabel;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (archive == null) archive = UnityEngine.Object.FindFirstObjectByType<SanctuaryArchiveManager>();

        Build();
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    void OnEnable()
    {
        LocalizationManager.LanguageChanged += OnLanguageChanged;
    }

    void OnDisable()
    {
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        if (IsOpen) Refresh();
    }

    public void Show()
    {
        if (panel == null) return;

        UIManager.OpenExclusive(panel);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Refresh()
    {
        if (lista == null) return;

        if (titulo != null) titulo.text = LocalizationManager.Get("UI_ARCHIVE_TITLE");
        if (closeLabel != null) closeLabel.text = LocalizationManager.Get("UI_CLOSE");

        for (int i = lista.childCount - 1; i >= 0; i--)
            Destroy(lista.GetChild(i).gameObject);

        float y = 0f;
        y = AddHeader(LocalizationManager.Get("UI_ARCHIVE_MILESTONES"), y);

        if (archive == null || archive.Milestones.Count == 0)
            y = AddRow(LocalizationManager.Get("UI_ARCHIVE_NO_MILESTONES"), UITheme.TextMuted, y);
        else
            foreach (var hito in archive.Milestones)
                y = AddRow(SanctuaryArchiveManager.Format(hito), UITheme.Text, y);

        y = AddHeader(LocalizationManager.Get("UI_ARCHIVE_LORE"), y);

        if (archive != null)
        {
            foreach (var entrada in archive.UnlockedLore())
            {
                y = AddRow($"<b>{LocalizationManager.Get(entrada.titleKey)}</b>", UITheme.Amber, y);
                y = AddRow(LocalizationManager.Get(entrada.summaryKey), UITheme.TextMuted, y);
            }
        }

        y = AddHeroMemories(y);

        lista.sizeDelta = new Vector2(lista.sizeDelta.x, -y + 16f);
    }

    // Recuerdos ya recuperados de todo el roster: es lo que convierte el Archivo en el sitio
    // donde vive la historia de tus héroes, y no solo cuatro fichas de lore por piso.
    private float AddHeroMemories(float y)
    {
        y = AddHeader(LocalizationManager.Get("UI_ARCHIVE_MEMORIES"), y);

        var todos = UnityEngine.Object.FindObjectsByType<HeroController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        int escritos = 0;
        foreach (var hero in todos)
        {
            if (hero == null || hero.Data == null || hero.Discarded) continue;
            if (hero.Data.memories == null || hero.Data.memories.Length == 0) continue;

            bool cabecera = false;
            foreach (var memoria in hero.Data.memories)
            {
                if (memoria == null || !memoria.IsValid || !hero.MemoryUnlocked(memoria)) continue;

                if (!cabecera)
                {
                    y = AddRow($"<b>{hero.Data.heroName}</b>", UITheme.Accent2, y);
                    cabecera = true;
                }

                y = AddRow(memoria.GetLocalized(), UITheme.TextMuted, y);
                escritos++;
            }
        }

        if (escritos == 0)
            y = AddRow(LocalizationManager.Get("UI_ARCHIVE_NO_MEMORIES"), UITheme.TextMuted, y);

        return y;
    }

    private float AddHeader(string text, float y)
    {
        var label = MakeLabel("Header", text, UIBuild.NameSize, UITheme.Amber, y);
        label.fontStyle = FontStyles.Bold;
        return y - 40f;
    }

    private float AddRow(string text, Color color, float y)
    {
        var label = MakeLabel("Row", text, UIBuild.BodySize, color, y);
        label.enableWordWrapping = true;

        // Alto real del texto ya envuelto: una crónica larga no debe solaparse con la fila siguiente.
        float ancho = Mathf.Max(100f, lista.rect.width - 16f);
        float alto = Mathf.Max(40f, label.GetPreferredValues(text, ancho, 0f).y);
        label.rectTransform.sizeDelta = new Vector2(-16f, alto);

        return y - alto - 6f;
    }

    private TMP_Text MakeLabel(string name, string text, float size, Color color, float y)
    {
        var tmp = UIBuild.Label(lista, name, size, TextAlignmentOptions.Left);
        tmp.text = text;
        tmp.color = color;

        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(-16f, 40f);
        return tmp;
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "SanctuaryArchive", size, new Color(0.10f, 0.12f, 0.15f, 0.98f));

        titulo = UIBuild.TopLabel(panel.transform, "Title", UIBuild.TitleSize, 46f, -14f,
            TextAlignmentOptions.Left);
        titulo.text = LocalizationManager.Get("UI_ARCHIVE_TITLE");

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image),
                                      typeof(Mask), typeof(ScrollRect));
        viewport.transform.SetParent(panel.transform, false);

        var vrt = viewport.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = new Vector2(20f, 90f);
        vrt.offsetMax = new Vector2(-20f, -70f);
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
        viewport.GetComponent<Mask>().showMaskGraphic = true;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewport.transform, false);
        lista = contentGo.GetComponent<RectTransform>();
        lista.anchorMin = new Vector2(0f, 1f);
        lista.anchorMax = new Vector2(1f, 1f);
        lista.pivot = new Vector2(0.5f, 1f);
        lista.anchoredPosition = Vector2.zero;
        lista.sizeDelta = new Vector2(0f, 100f);

        var scroll = viewport.GetComponent<ScrollRect>();
        scroll.viewport = vrt;
        scroll.content = lista;
        scroll.horizontal = false;

        var btnClose = UIBuild.Button(panel.transform, "Btn_CloseArchive", LocalizationManager.Get("UI_CLOSE"),
            new Color(0.32f, 0.28f, 0.36f), new Vector2(300f, 60f), new Vector2(0f, -(size.y - 74f)),
            Close);
        closeLabel = btnClose.GetComponentInChildren<TMP_Text>();
    }
}
