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
    ThreePlus,
    Party
}

// Criterio de ordenación de las filas.
public enum RosterSort
{
    Rarity,
    Level
}

// Panel modal que lista los héroes vivos de la base.
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

    [Tooltip("Tienda de la que salen inventario y equipo.")]
    [SerializeField] private ShopManager shop;

    [Tooltip("Economía que paga las ascensiones.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Taller del que salen las Piedras de Ascensión.")]
    [SerializeField] private CraftingManager crafting;

    [Tooltip("Escuadra a la que se apunta o se saca a los héroes.")]
    [SerializeField] private PartyManager party;

    [Tooltip("Gestor de síntesis al que apuntan los botones de cada fila.")]
    [SerializeField] private SynthesisManager synthesis;

    [Tooltip("Modal de equipamiento manual que abre el botón Equipar.")]
    [SerializeField] private EquipmentSelectModalUI equipModal;

    // Métricas del mockup: cabecera, tarjeta y rejilla de botones.
    private const float HeaderHeight = 80f;
    private const float TabHeight = 36f;
    private const float RowHeight = 112f;
    private const float CardPadX = 18f;
    private const float CardPadY = 14f;
    private const float ColGap = 18f;
    private const float ColIdentity = 280f;
    private const float ColBars = 260f;
    private const float BtnWidth = 96f;
    private const float BtnHeight = 38f;
    private const float BtnGap = 8f;
    private const float ColActions = BtnWidth * 4f + BtnGap * 3f;
    private const float PortraitSize = 56f;

    private float refreshTimer;

    private RosterFilter filter = RosterFilter.All;
    private RosterSort sort = RosterSort.Rarity;

    private readonly List<Button> tabButtons = new List<Button>();
    private Button sortButton;
    private TMP_Text sortLabel;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (synthesis == null) synthesis = UnityEngine.Object.FindFirstObjectByType<SynthesisManager>();
        if (shop == null) shop = UnityEngine.Object.FindFirstObjectByType<ShopManager>();
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();
        if (party == null) party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();
        if (equipModal == null) equipModal = UnityEngine.Object.FindFirstObjectByType<EquipmentSelectModalUI>();
    }

    void Start()
    {
        BuildTabs();
        if (panel != null) panel.SetActive(false);
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

        // El aspa de cerrar ya ocupa la esquina: los demás controles se apilan hacia la izquierda.
        float cursor = -(28f + 36f + BtnGap);

        sortButton = CreateBarButton(bar.transform, "Btn_Sort",
            LocalizationManager.Get("UI_SORT_RARITY"), ref cursor, OnSortPressed);
        sortLabel = sortButton.GetComponentInChildren<TMP_Text>();

        string[] keys = { "UI_ALL", "1*", "2*", "3*+", "UI_PARTY" };
        for (int i = keys.Length - 1; i >= 0; i--)
        {
            var value = (RosterFilter)i;
            string text = keys[i].StartsWith("UI_") ? LocalizationManager.Get(keys[i]) : keys[i];

            var button = CreateBarButton(bar.transform, "Tab_" + value, text,
                ref cursor, () => OnFilterPressed(value));

            tabButtons.Insert(0, button);
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
    }

    private bool PassesFilter(HeroController hero)
    {
        switch (filter)
        {
            case RosterFilter.OneStar: return hero.StarRank == 1;
            case RosterFilter.TwoStar: return hero.StarRank == 2;
            case RosterFilter.ThreePlus: return hero.StarRank >= 3;
            case RosterFilter.Party: return party != null && party.IsInParty(hero);
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

    // Regenera la lista entera; con una decena de héroes sale más barato que diferenciar.
    private void Rebuild()
    {
        if (content == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        var todos = UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None);

        var heroes = new List<HeroController>();
        foreach (var hero in todos)
            if (hero != null && hero.Data != null && PassesFilter(hero)) heroes.Add(hero);

        // FindObjectsByType no garantiza orden y las filas llevan botón: sin ordenar, bailan al refrescar.
        heroes.Sort(CompareHeroes);

        if (emptyLabel != null) emptyLabel.gameObject.SetActive(heroes.Count == 0);

        foreach (var hero in heroes)
            CreateCard(hero);
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

    // Cada héroe es una tarjeta con fondo propio y cuatro columnas de ancho fijo.
    private void CreateCard(HeroController hero)
    {
        var go = new GameObject($"Card_{hero.name}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(content, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, RowHeight);
        UITheme.Surface(go, UITheme.Card, UITheme.BorderSoft, UITheme.RadiusCard);

        // En el primer frame el content aun no tiene ancho: se cae al del panel, que si es fijo.
        float disponible = content.rect.width > 100f
            ? content.rect.width
            : panel.GetComponent<RectTransform>().rect.width - 56f;

        float util = Mathf.Max(900f, disponible - CardPadX * 2f);
        float colGear = Mathf.Max(160f, util - ColIdentity - ColBars - ColActions - ColGap * 3f);

        float x = CardPadX;
        BuildIdentity(go.transform, hero, x, ColIdentity);
        x += ColIdentity + ColGap;
        BuildBars(go.transform, hero, x, ColBars);
        x += ColBars + ColGap;
        BuildGear(go.transform, hero, x, colGear);
        x += colGear + ColGap;
        CreateCardButtons(go.transform, hero, x);
    }

    // Columna 1: retrato con el marco de la rareza y a su lado estrellas, nombre y oficio.
    private void BuildIdentity(Transform card, HeroController hero, float x, float width)
    {
        var progress = hero.GetComponent<HeroProgress>();
        int level = progress != null ? progress.Level : 1;
        int maxLevel = progress != null ? progress.MaxLevel : 0;
        string tope = progress != null && progress.IsMaxLevel ? "  TOPE" : string.Empty;

        // La fuente CJK que se añadió como fallback sí trae la estrella tipográfica.
        var stars = new StringBuilder();
        for (int i = 0; i < hero.StarRank; i++) stars.Append('★');

        string oficio = hero.Subclass != HeroSubclass.None
            ? hero.SubclassName
            : HeroTraits.DisplayName(hero.Trait);

        // Insignia discreta: el juego sugiere que sobra, no lo decide por ti.
        if (hero.IsSynthesisCandidate)
            oficio += "  <color=#C08040>· candidato a síntesis ·</color>";

        var rareza = HeroProgress.RarityColor(hero.StarRank);
        BuildPortrait(card, x, rareza, hero.Data.bodySprite);

        float textoX = x + PortraitSize + 14f;
        var label = NewCardLabel(card, "Col_Identity", textoX,
            width - PortraitSize - 14f, UITheme.SizeName, TextAlignmentOptions.Left);

        label.text = $"<size={UITheme.SizeSmall}><b><color={UITheme.Tag(rareza)}>{stars}</color></b></size>\n" +
                     $"{hero.Data.heroName}\n" +
                     $"<size={UITheme.SizeSmall}><color={UITheme.Tag(UITheme.TextMuted)}>" +
                     $"{oficio} · Nv.{level}/{maxLevel}{tope}</color></size>";
    }

    // Cuadro con las esquinas redondeadas, el borde del color de la rareza y dentro
    // el sprite pixel art del héroe.
    private void BuildPortrait(Transform card, float x, Color rareza, Sprite retrato)
    {
        var go = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(card, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(PortraitSize, PortraitSize);
        rt.anchoredPosition = new Vector2(x, 0f);

        UIBuild.HeroArt(go.transform, retrato, PortraitSize - 8f);

        UITheme.Surface(go, UITheme.Hex("262838"), rareza, UITheme.RadiusCard);
        go.GetComponent<Image>().raycastTarget = false;
    }

    // Columna 2: tres barras finas de 7 px con su rótulo a la izquierda y la cifra a la derecha.
    private void BuildBars(Transform card, HeroController hero, float x, float width)
    {
        float ratioHp = hero.MaxHealth > 0 ? (float)hero.CurrentHealth / hero.MaxHealth : 0f;
        float ratioMp = hero.MaxMP > 0 ? (float)hero.CurrentMP / hero.MaxMP : 0f;

        CreateBar(card, "Bar_HP", x, width, 12f, ratioHp, UITheme.BarHP,
            "HP", $"{hero.CurrentHealth}/{hero.MaxHealth}");
        CreateBar(card, "Bar_MP", x, width, 0f, ratioMp, UITheme.BarMP,
            "MP", $"{hero.CurrentMP}/{hero.MaxMP}");
        CreateBar(card, "Bar_Morale", x, width, -12f, hero.MoralePercent / 100f, UITheme.BarMorale,
            "Moral", $"{hero.MoralePercent} {hero.MoodName}".TrimEnd());
    }

    private void CreateBar(Transform card, string name, float x, float width, float y,
                           float ratio, Color color, string caption, string value)
    {
        var fila = new GameObject(name, typeof(RectTransform));
        fila.transform.SetParent(card, false);

        var rt = fila.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width, 12f);

        const float anchoRotulo = 42f;
        const float anchoCifra = 76f;

        var rotulo = MiniLabel(fila.transform, "Caption", 0f, anchoRotulo,
            TextAlignmentOptions.Left, UITheme.TextMuted);
        rotulo.text = caption;

        var pista = new GameObject("Track", typeof(RectTransform), typeof(Image));
        pista.transform.SetParent(fila.transform, false);

        var prt = pista.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 0.5f);
        prt.anchorMax = new Vector2(0f, 0.5f);
        prt.pivot = new Vector2(0f, 0.5f);
        prt.anchoredPosition = new Vector2(anchoRotulo + 6f, 0f);
        prt.sizeDelta = new Vector2(width - anchoRotulo - anchoCifra - 12f, 7f);
        UITheme.Surface(pista, UITheme.Track, Color.clear, 3.5f);
        pista.GetComponent<Image>().raycastTarget = false;

        var relleno = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        relleno.transform.SetParent(pista.transform, false);

        var frt = relleno.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        UITheme.Surface(relleno, color, Color.clear, 3.5f);
        relleno.GetComponent<Image>().raycastTarget = false;

        var cifra = MiniLabel(fila.transform, "Value", width - anchoCifra, anchoCifra,
            TextAlignmentOptions.Right, UITheme.TextMuted);
        cifra.text = value;
    }

    private TMP_Text MiniLabel(Transform parent, string name, float x, float width,
                               TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta = new Vector2(width, 14f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = UITheme.SizeTiny;
        tmp.alignment = align;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    // Columna 3: lo que lleva puesto y lo que sabe hacer.
    private void BuildGear(Transform card, HeroController hero, float x, float width)
    {
        var label = NewCardLabel(card, "Col_Gear", x, width, UITheme.SizeSmall,
            TextAlignmentOptions.Left);
        label.color = UITheme.TextSoft;
        label.lineSpacing = 12f;

        string estados = hero.Status.Describe();
        string linea = string.IsNullOrEmpty(estados)
            ? $"{Key("Maestría")} {hero.Mastery.Describe()}"
            : $"{Key("Estados")} {estados}";

        label.text = $"{Key("Pasivas")} {PassiveSkills.Describe(hero.Passives)}\n" +
                     $"{Key("Arma")} {GearLabel(hero, EquipmentSlot.Weapon)}   " +
                     $"{Key("Escudo")} {GearLabel(hero, EquipmentSlot.Shield)}\n" +
                     $"{Key("Arm.")} {GearLabel(hero, EquipmentSlot.Armor)}   {linea}";
    }

    // Los nombres de campo van más apagados que su valor, como en el mockup.
    private static string Key(string text) => $"<color={UITheme.Tag(UITheme.TextFaint)}>{text}</color>";

    // Columna 4: ocho botones en dos filas de cuatro, para no estirar la tarjeta a lo alto.
    private void CreateCardButtons(Transform card, HeroController hero, float x)
    {
        var progress = hero.GetComponent<HeroProgress>();
        bool isTarget = synthesis != null && synthesis.Target == hero;
        bool canAscend = progress != null && progress.CanAscend(economy, crafting);
        bool inParty = party != null && party.IsInParty(hero);
        bool hasGear = shop != null;
        bool wearsGear = hero.Weapon != null || hero.Shield != null
                         || hero.Armor != null || hero.Accessory != null;

        CreateButton(card, "Btn_Lock", x, 0,
            LocalizationManager.Get(hero.IsLocked ? "UI_LOCK" : "UI_UNLOCK"),
            hero.IsLocked ? UITheme.DangerSoft : UITheme.Neutral, true, () => OnLockClicked(hero));

        CreateButton(card, "Btn_Party", x, 1,
            LocalizationManager.Get(inParty ? "UI_IN_PARTY" : "UI_PARTY"),
            inParty ? UITheme.Amber : UITheme.Neutral, party != null, () => OnPartyClicked(hero));

        CreateButton(card, "Btn_Equip", x, 2, LocalizationManager.Get("UI_EQUIP"),
            UITheme.Teal, hasGear, () => OnEquipClicked(hero));

        CreateButton(card, "Btn_Ascend", x, 3, LocalizationManager.Get("UI_ASCEND"),
            UITheme.AmberSoft, canAscend, () => OnAscendClicked(hero));

        CreateButton(card, "Btn_Synth", x, 4,
            LocalizationManager.Get(isTarget ? "UI_SYNTH_TARGET" : "UI_SYNTH"),
            isTarget ? UITheme.Amber : UITheme.AccentSoft,
            synthesis != null && !hero.IsLocked, () => OnSynthClicked(hero));

        CreateButton(card, "Btn_Unequip", x, 5, LocalizationManager.Get("UI_UNEQUIP"),
            UITheme.Teal, wearsGear, () => OnUnequipClicked(hero));

        bool puedeSubclase = hero.StarRank >= HeroSubclasses.MinStarRank;
        CreateButton(card, "Btn_Subclass", x, 6, LocalizationManager.Get("UI_SUBCLASS"),
            UITheme.AccentSoft, puedeSubclase, () => OnSubclassClicked(hero));

        bool roto = hero.FirstBrokenSlot() != null;
        CreateButton(card, "Btn_Repair", x, 7, LocalizationManager.Get("UI_REPAIR"),
            UITheme.DangerSoft, roto && crafting != null, () => OnRepairClicked(hero));
    }

    // Rota entre las tres subclases del arquetipo del héroe.
    private void OnSubclassClicked(HeroController hero)
    {
        var progress = hero.GetComponent<HeroProgress>();
        if (progress != null) progress.CycleSubclass();

        Rebuild();
    }

    private void OnRepairClicked(HeroController hero)
    {
        if (crafting != null) crafting.TryRepair(hero);
        Rebuild();
    }

    // Nombre de la pieza con su desgaste; una rota se marca en el sitio.
    private static string GearLabel(HeroController hero, EquipmentSlot slot)
    {
        var item = hero.GetEquipped(slot);
        if (item == null) return "-";

        string etiqueta = item.ShortLabel();
        return hero.IsBroken(slot)
            ? $"{etiqueta} [{LocalizationManager.Get("UI_BROKEN")}]"
            : $"{etiqueta} ({hero.DurabilityOf(slot)}/{item.maxDurability})";
    }

    // Echar o quitar el candado; un héroe bloqueado no se puede sacrificar.
    private void OnLockClicked(HeroController hero)
    {
        bool locked = hero.ToggleLock();

        // Si estaba elegido como objetivo de síntesis, bloquearlo cancela la operación.
        if (locked && synthesis != null && synthesis.Target == hero) synthesis.ClearTarget();

        SaveManager.RequestSave();
        Rebuild();
    }

    private void OnPartyClicked(HeroController hero)
    {
        if (party == null) return;

        party.Toggle(hero);
        Rebuild();
    }

    // El hueco decide la posición: cuatro botones arriba y cuatro abajo.
    private void CreateButton(Transform card, string name, float x, int slotIndex, string text,
                              Color color, bool interactable, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(card, false);

        int columna = slotIndex % 4;
        int fila = slotIndex / 4;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(
            x + columna * (BtnWidth + BtnGap),
            -CardPadY - fila * (BtnHeight + BtnGap));
        rt.sizeDelta = new Vector2(BtnWidth, BtnHeight);

        // Lo que no se puede pulsar se apaga a neutro en vez de cambiar de color.
        var image = UITheme.Surface(go, interactable ? color : UITheme.Neutral,
            UITheme.BorderCard, UITheme.RadiusButton);

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);

        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = UITheme.SizeSmall;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = interactable ? UITheme.Text : UITheme.TextFaint;
        tmp.text = text;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.interactable = interactable;
        button.onClick.AddListener(() => AudioManager.Play(SfxId.UiClick));
        button.onClick.AddListener(onClick);
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
        rt.sizeDelta = new Vector2(width, -CardPadY * 2f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = UITheme.Text;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void OnAscendClicked(HeroController hero)
    {
        var progress = hero.GetComponent<HeroProgress>();
        if (progress != null) progress.AscendHero(economy, crafting);
        Rebuild();
    }

    // Abre el modal para elegir pieza a mano; el auto-equipar vive dentro, de atajo.
    private void OnEquipClicked(HeroController hero)
    {
        if (equipModal != null)
        {
            equipModal.Open(hero);
            return;
        }

        // Sin modal montado se cae al comportamiento de siempre.
        if (shop == null) return;

        shop.EquipFromInventory(hero, shop.FirstEquippableFor(hero));
        Rebuild();
    }

    // Quita la primera pieza que lleve puesta, de arma a accesorio.
    private void OnUnequipClicked(HeroController hero)
    {
        if (shop == null) return;

        if (hero.Weapon != null) shop.UnequipToInventory(hero, EquipmentSlot.Weapon);
        else if (hero.Shield != null) shop.UnequipToInventory(hero, EquipmentSlot.Shield);
        else if (hero.Armor != null) shop.UnequipToInventory(hero, EquipmentSlot.Armor);
        else if (hero.Accessory != null) shop.UnequipToInventory(hero, EquipmentSlot.Accessory);

        Rebuild();
    }

    // Primer clic elige objetivo; el segundo, sobre otro héroe, lo sacrifica.
    private void OnSynthClicked(HeroController hero)
    {
        if (synthesis == null) return;

        synthesis.SelectHero(hero);
        Rebuild();
    }

}
