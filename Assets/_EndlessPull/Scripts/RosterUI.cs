using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pestañas superiores del roster; el orden es el de los botones.
public enum RosterFilter
{
    All,
    OneStar,
    TwoStar,
    ThreeStar,
    FourStar,
    FiveStar
}

// Criterio de ordenación de las filas.
public enum RosterSort
{
    Rarity,
    Level
}

// Panel modal que lista los héroes vivos de la base. Cada tarjeta es solo lectura:
// retrato, nombre, estrellas, nivel, subclase, vida y estado. Un clic abre la ficha única
// HeroQuickCardUI, donde viven las acciones (bloquear, escuadra, equipo, subclase, reparar).
public class RosterUI : MonoBehaviour
{
    [Tooltip("Raíz del panel; se activa y desactiva al abrir y cerrar.")]
    [SerializeField] private GameObject panel;

    [Tooltip("Contenedor donde se generan las filas de héroes.")]
    [SerializeField] private RectTransform content;

    [Tooltip("Texto que se muestra cuando no queda ningún héroe.")]
    [SerializeField] private TMP_Text emptyLabel;

    [Tooltip("Segundos entre refrescos mientras el panel está abierto.")]
    [SerializeField] private float refreshInterval = 0.5f;

    [Tooltip("Ficha única que abre un clic sobre la tarjeta; ahí viven las acciones.")]
    [SerializeField] private HeroQuickCardUI quickCard;

    // Métricas de la tarjeta compacta.
    private const float HeaderHeight = 80f;
    private const float TabHeight = 36f;
    private const float BtnGap = 8f;
    private const float RowHeight = 64f;
    private const float CardPadX = 14f;
    private const float PortraitSize = 44f;
    private const float ColName = 300f;
    private const float ColHealth = 190f;
    private const float ColStatus = 220f;

    private float refreshTimer;

    private RosterFilter filter = RosterFilter.All;
    private RosterSort sort = RosterSort.Rarity;

    private readonly List<Button> tabButtons = new List<Button>();
    private readonly List<TMP_Text> tabLabels = new List<TMP_Text>();
    private Button sortButton;
    private TMP_Text sortLabel;

    // Un widget por fila visible; se reutilizan entre refrescos en vez de destruirse y
    // recrearse, que es lo que hacía caer el framerate con un roster de 50+ héroes.
    private class CardWidgets
    {
        public GameObject root;
        public Button button;
        public Image portraitFrame;
        public TMP_Text info;
        public Image hpFill;
        public TMP_Text hpLabel;
        public TMP_Text statusLabel;
        public HeroController hero;
    }

    private readonly List<CardWidgets> pool = new List<CardWidgets>();
    private readonly List<HeroController> scratch = new List<HeroController>();

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (quickCard == null) quickCard = UnityEngine.Object.FindFirstObjectByType<HeroQuickCardUI>();
    }

    void OnEnable()
    {
        LocalizationManager.LanguageChanged += RefreshTabs;
    }

    void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshTabs;
    }

    void Start()
    {
        BuildTabs();

        if (panel != null)
        {
            UIBuild.CloseButtonTopRight(panel.transform, Close);
            panel.SetActive(false);
        }
    }

    // Filtros, orden y cierre viven en la cabecera del panel, alineados a la derecha.
    private void BuildTabs()
    {
        if (panel == null) return;

        var bar = new GameObject("FilterTabs", typeof(RectTransform));
        bar.transform.SetParent(panel.transform, false);

        var brt = bar.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 1f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.sizeDelta = new Vector2(0f, HeaderHeight);
        brt.anchoredPosition = Vector2.zero;

        // El botón [X] de UIBuild ocupa la esquina (48px + 16px de margen): los demás
        // controles se apilan hacia la izquierda dejándole sitio de sobra.
        float cursor = -(28f + 36f + BtnGap);

        sortButton = CreateBarButton(bar.transform, "Btn_Sort",
            LocalizationManager.Get("UI_SORT_RARITY"), ref cursor, OnSortPressed);
        sortLabel = sortButton.GetComponentInChildren<TMP_Text>();

        // Seis pestañas exactas: Todos y una por cada rango de estrella.
        string[] keys = { "UI_ALL", "1★", "2★", "3★", "4★", "5★+" };
        for (int i = keys.Length - 1; i >= 0; i--)
        {
            var value = (RosterFilter)i;
            string text = keys[i].StartsWith("UI_") ? LocalizationManager.Get(keys[i]) : keys[i];

            var button = CreateBarButton(bar.transform, "Tab_" + value, text,
                ref cursor, () => OnFilterPressed(value));

            tabButtons.Insert(0, button);
            tabLabels.Insert(0, button.GetComponentInChildren<TMP_Text>());
        }

        RefreshTabs();
    }

    // El ancho lo marca el texto; el cursor avanza hacia la izquierda tras cada botón.
    private Button CreateBarButton(Transform parent, string name, string text,
                                   ref float cursor,
                                   UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);

        var image = UITheme.Surface(go, Color.clear, UITheme.BorderStrong, UITheme.RadiusButton);

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = UITheme.SizeBody;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = UITheme.Text;
        tmp.text = text;

        float width = Mathf.Max(56f, tmp.preferredWidth + 32f);
        rt.sizeDelta = new Vector2(width, TabHeight);
        rt.anchoredPosition = new Vector2(cursor, -22f);
        cursor -= width + BtnGap;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        return button;
    }

    private void OnFilterPressed(RosterFilter value)
    {
        filter = value;
        RefreshTabs();
        Rebuild();
    }

    private void OnSortPressed()
    {
        sort = sort == RosterSort.Rarity ? RosterSort.Level : RosterSort.Rarity;
        RefreshTabs();
        Rebuild();
    }

    private void RefreshTabs()
    {
        // La pestaña elegida se tiñe de acento y su borde pasa a ser sólido.
        for (int i = 0; i < tabButtons.Count; i++)
        {
            bool activa = (int)filter == i;
            tabButtons[i].targetGraphic.color = activa ? UITheme.AccentPick : Color.clear;

            var borde = tabButtons[i].transform.Find("Border");
            if (borde != null)
                borde.GetComponent<Image>().color = activa ? UITheme.Accent : UITheme.BorderStrong;
        }

        if (sortLabel != null)
            sortLabel.text = LocalizationManager.Get(
                sort == RosterSort.Rarity ? "UI_SORT_RARITY" : "UI_SORT_LEVEL");

        // Solo la pestaña "Todos" es texto traducible; las de estrella son numéricas universales.
        if (tabLabels.Count > 0 && tabLabels[0] != null)
            tabLabels[0].text = LocalizationManager.Get("UI_ALL");
    }

    private bool PassesFilter(HeroController hero)
    {
        switch (filter)
        {
            case RosterFilter.OneStar: return hero.StarRank == 1;
            case RosterFilter.TwoStar: return hero.StarRank == 2;
            case RosterFilter.ThreeStar: return hero.StarRank == 3;
            case RosterFilter.FourStar: return hero.StarRank == 4;
            case RosterFilter.FiveStar: return hero.StarRank >= 5;
        }
        return true;
    }

    void Update()
    {
        if (!IsOpen) return;

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer > 0f) return;

        refreshTimer = refreshInterval;
        Rebuild();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (panel == null) return;

        // Abrir uno cierra los demas: nunca se solapan dos modales.
        UIManager.OpenExclusive(panel);
        refreshTimer = refreshInterval;
        Rebuild();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    // Actualiza las tarjetas ya existentes en vez de destruir y recrear; con un roster
    // grande abierto de fondo, esto es lo que evita el bajón de FPS al hacer scroll.
    private void Rebuild()
    {
        if (content == null) return;

        var todos = UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None);

        scratch.Clear();
        foreach (var hero in todos)
            if (hero != null && hero.Data != null && PassesFilter(hero)) scratch.Add(hero);

        // FindObjectsByType no garantiza orden y las filas no deben bailar entre refrescos.
        scratch.Sort(CompareHeroes);

        if (emptyLabel != null) emptyLabel.gameObject.SetActive(scratch.Count == 0);

        for (int i = 0; i < scratch.Count; i++)
        {
            if (i >= pool.Count) pool.Add(CreateCardWidgets());

            var widgets = pool[i];
            widgets.root.SetActive(true);
            UpdateCard(widgets, scratch[i]);
        }

        for (int i = scratch.Count; i < pool.Count; i++)
            pool[i].root.SetActive(false);
    }

    // El desempate siempre es el nombre: sin él, dos héroes iguales bailarían entre refrescos.
    private int CompareHeroes(HeroController a, HeroController b)
    {
        if (a == null || a.Data == null) return 1;
        if (b == null || b.Data == null) return -1;

        int primary = sort == RosterSort.Level
            ? LevelOf(b).CompareTo(LevelOf(a))
            : b.StarRank.CompareTo(a.StarRank);

        if (primary != 0) return primary;

        return string.Compare(a.Data.heroName, b.Data.heroName, System.StringComparison.Ordinal);
    }

    private static int LevelOf(HeroController hero)
    {
        var progress = hero != null ? hero.GetComponent<HeroProgress>() : null;
        return progress != null ? progress.Level : 1;
    }

    // Fila compacta: retrato, identidad, vida y estado. Sin botones: todo eso vive en el modal.
    private CardWidgets CreateCardWidgets()
    {
        var go = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(content, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, RowHeight);
        var cardImage = UITheme.Surface(go, UITheme.Card, UITheme.BorderSoft, UITheme.RadiusCard);

        var widgets = new CardWidgets { root = go };

        widgets.portraitFrame = BuildPortrait(go.transform, CardPadX);

        float x = CardPadX + PortraitSize + 12f;
        widgets.info = NewCardLabel(go.transform, "Col_Identity", x, ColName,
            UITheme.SizeName, TextAlignmentOptions.Left);

        x += ColName + 12f;
        widgets.hpFill = UIBuild.Bar(go.transform, "Bar_HP", new Vector2(ColHealth, 14f),
            new Vector2(x, 0f), UITheme.BarHP, out widgets.hpLabel);
        var hpRt = widgets.hpFill.transform.parent.GetComponent<RectTransform>();
        hpRt.anchorMin = new Vector2(0f, 0.5f);
        hpRt.anchorMax = new Vector2(0f, 0.5f);
        hpRt.pivot = new Vector2(0f, 0.5f);
        hpRt.anchoredPosition = new Vector2(x, 0f);

        x += ColHealth + 12f;
        widgets.statusLabel = NewCardLabel(go.transform, "Col_Status", x, ColStatus,
            UITheme.SizeSmall, TextAlignmentOptions.Left);
        widgets.statusLabel.color = UITheme.TextSoft;

        widgets.button = go.GetComponent<Button>();
        widgets.button.targetGraphic = cardImage;

        return widgets;
    }

    private void UpdateCard(CardWidgets widgets, HeroController hero)
    {
        widgets.hero = hero;

        var progress = hero.GetComponent<HeroProgress>();
        int level = progress != null ? progress.Level : 1;

        var stars = new StringBuilder();
        for (int i = 0; i < hero.StarRank; i++) stars.Append('★');

        string oficio = hero.Subclass != HeroSubclass.None
            ? hero.SubclassName
            : HeroTraits.DisplayName(hero.Trait);

        var rareza = HeroProgress.RarityColor(hero.StarRank);
        var borde = widgets.portraitFrame.transform.Find("Border");
        if (borde != null) borde.GetComponent<Image>().color = rareza;
        UIBuild.HeroArt(widgets.portraitFrame.transform, hero.Data.bodySprite, PortraitSize - 8f);

        widgets.info.text = $"<size={UITheme.SizeSmall}><b><color={UITheme.Tag(rareza)}>{stars}</color></b></size>  " +
                            $"{hero.Data.heroName}\n" +
                            $"<size={UITheme.SizeSmall}><color={UITheme.Tag(UITheme.TextMuted)}>" +
                            $"{oficio} · Nv.{level}</color></size>";

        float ratioHp = hero.MaxHealth > 0 ? (float)hero.CurrentHealth / hero.MaxHealth : 0f;
        UIBuild.SetBar(widgets.hpFill, ratioHp);
        widgets.hpLabel.text = $"{hero.CurrentHealth}/{hero.MaxHealth}";

        string estados = hero.Status.Describe();
        widgets.statusLabel.text = string.IsNullOrEmpty(estados)
            ? hero.MoodName
            : $"{hero.MoodName}  {estados}".TrimStart();

        widgets.button.onClick.RemoveAllListeners();
        widgets.button.onClick.AddListener(() => AudioManager.Play(SfxId.UiClick));
        widgets.button.onClick.AddListener(() => OnCardClicked(widgets.hero));
    }

    private void OnCardClicked(HeroController hero)
    {
        if (quickCard != null) quickCard.Show(hero, fromRoster: true);
    }

    // Cuadro con las esquinas redondeadas, el borde del color de la rareza y dentro
    // el sprite pixel art del héroe.
    private Image BuildPortrait(Transform card, float x)
    {
        var go = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(card, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(PortraitSize, PortraitSize);
        rt.anchoredPosition = new Vector2(x, 0f);

        var image = UITheme.Surface(go, UITheme.Hex("262838"), UITheme.BorderSoft, UITheme.RadiusCard);
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text NewCardLabel(Transform card, string name, float x, float width,
                                  float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(card, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta = new Vector2(width, -8f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = UITheme.Text;
        tmp.raycastTarget = false;
        return tmp;
    }
}
