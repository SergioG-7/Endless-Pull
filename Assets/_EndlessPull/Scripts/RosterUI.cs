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

    [Tooltip("Alto de cada fila de héroe, en píxeles de UI.")]
    [SerializeField] private float rowHeight = 78f;

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

    [Tooltip("Alto de la barra de pestañas de filtro.")]
    [SerializeField] private float tabHeight = 52f;

    [Tooltip("Distancia desde el borde superior del panel hasta la barra de pestañas.")]
    [SerializeField] private float tabsTopOffset = 85f;

    [Tooltip("Color del candado echado.")]
    [SerializeField] private Color lockedColor = new Color(0.62f, 0.22f, 0.22f);

    [Tooltip("Color de la pestaña seleccionada.")]
    [SerializeField] private Color activeTabColor = new Color(0.55f, 0.42f, 0.15f);

    [Tooltip("Fondo de cada tarjeta de héroe.")]
    [SerializeField] private Color cardColor = new Color(0.14f, 0.14f, 0.19f, 0.95f);

    [Tooltip("Margen interior de la tarjeta, en píxeles.")]
    [SerializeField] private float cardPadding = 12f;

    [Tooltip("Ancho de los botones de la tarjeta.")]
    [SerializeField] private float cardButtonWidth = 132f;

    [Tooltip("Alto de los botones de la tarjeta.")]
    [SerializeField] private float cardButtonHeight = 44f;

    [Tooltip("Color del botón de síntesis en reposo.")]
    [SerializeField] private Color synthColor = new Color(0.35f, 0.30f, 0.60f);

    [Tooltip("Color del botón del héroe elegido como objetivo.")]
    [SerializeField] private Color targetColor = new Color(0.70f, 0.45f, 0.15f);

    [Tooltip("Color del botón de ascender cuando se puede pagar.")]
    [SerializeField] private Color ascendColor = new Color(0.65f, 0.55f, 0.15f);

    [Tooltip("Color de los botones de equipo.")]
    [SerializeField] private Color gearColor = new Color(0.25f, 0.45f, 0.45f);

    [Tooltip("Color de un botón que ahora mismo no se puede pulsar.")]
    [SerializeField] private Color disabledColor = new Color(0.28f, 0.28f, 0.32f);

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
    }

    void Start()
    {
        BuildTabs();
        if (panel != null) panel.SetActive(false);
    }

    // Las pestañas se montan una sola vez y el scroll baja para hacerles sitio.
    private void BuildTabs()
    {
        if (panel == null) return;

        var bar = new GameObject("FilterTabs", typeof(RectTransform));
        bar.transform.SetParent(panel.transform, false);

        var brt = bar.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 1f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.sizeDelta = new Vector2(0f, tabHeight);
        brt.anchoredPosition = new Vector2(0f, -tabsTopOffset);

        string[] keys = { "UI_ALL", "1*", "2*", "3*+", "UI_PARTY" };
        for (int i = 0; i < keys.Length; i++)
        {
            var value = (RosterFilter)i;
            string text = keys[i].StartsWith("UI_") ? LocalizationManager.Get(keys[i]) : keys[i];

            var button = CreateBarButton(bar.transform, "Tab_" + value, text,
                new Vector2(16f + i * 190f, 0f), 180f, () => OnFilterPressed(value));

            tabButtons.Add(button);
        }

        sortButton = CreateBarButton(bar.transform, "Btn_Sort", string.Empty,
            new Vector2(16f + keys.Length * 190f + 30f, 0f), 260f, OnSortPressed);
        sortLabel = sortButton.GetComponentInChildren<TMP_Text>();

        // El scroll autorizado en escena tiene que dejar hueco a la barra nueva.
        if (content != null)
        {
            var scroll = content.parent != null ? content.parent.parent as RectTransform : null;
            if (scroll != null) scroll.offsetMax -= new Vector2(0f, tabHeight + 8f);
        }

        RefreshTabs();
    }

    private Button CreateBarButton(Transform parent, string name, string text,
                                   Vector2 position, float width,
                                   UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(width, tabHeight - 8f);
        rt.anchoredPosition = position;

        var image = go.GetComponent<Image>();
        image.color = synthColor;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 20f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = text;

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
        for (int i = 0; i < tabButtons.Count; i++)
            tabButtons[i].targetGraphic.color = (int)filter == i ? activeTabColor : synthColor;

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
        rt.sizeDelta = new Vector2(0f, rowHeight);
        go.GetComponent<Image>().color = cardColor;

        // En el primer frame el content aun no tiene ancho: se cae al del panel, que si es fijo.
        float disponible = content.rect.width > 100f
            ? content.rect.width
            : panel.GetComponent<RectTransform>().rect.width - 76f;

        float util = Mathf.Max(600f, disponible - cardPadding * 2f);
        float col4 = cardButtonWidth * 3f + 12f;
        float col1 = util * 0.26f;
        float col2 = util * 0.24f;
        float col3 = Mathf.Max(160f, util - col1 - col2 - col4);

        float x = cardPadding;
        BuildIdentity(go.transform, hero, x, col1);
        x += col1;
        BuildBars(go.transform, hero, x, col2);
        x += col2;
        BuildGear(go.transform, hero, x, col3);
        x += col3;
        CreateCardButtons(go.transform, hero, x, col4);
    }

    // Columna 1: estrellas, nombre y nivel, todo con el color de la rareza.
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
            oficio += "\n<color=#C08040><size=80%>· candidato a síntesis ·</size></color>";

        var label = NewCardLabel(card, "Col_Identity", x, width, 22f, TextAlignmentOptions.TopLeft);
        label.color = HeroProgress.RarityColor(hero.StarRank);
        label.text = $"{stars}\n<b>{hero.Data.heroName}</b>\nNv. {level}/{maxLevel}{tope}\n{oficio}";
    }

    // Columna 2: tres barras compactas con su cifra al lado.
    private void BuildBars(Transform card, HeroController hero, float x, float width)
    {
        float ratioHp = hero.MaxHealth > 0 ? (float)hero.CurrentHealth / hero.MaxHealth : 0f;
        float ratioMp = hero.MaxMP > 0 ? (float)hero.CurrentMP / hero.MaxMP : 0f;

        CreateBar(card, "Bar_HP", x, width, -cardPadding - 4f, ratioHp,
            new Color(0.80f, 0.25f, 0.25f), $"HP {hero.CurrentHealth}/{hero.MaxHealth}");
        CreateBar(card, "Bar_MP", x, width, -cardPadding - 38f, ratioMp,
            new Color(0.25f, 0.45f, 0.85f), $"MP {hero.CurrentMP}/{hero.MaxMP}");
        CreateBar(card, "Bar_Morale", x, width, -cardPadding - 72f, hero.MoralePercent / 100f,
            new Color(0.85f, 0.70f, 0.25f), $"Mor {hero.MoralePercent} {hero.MoodName}".TrimEnd());
    }

    private void CreateBar(Transform card, string name, float x, float width, float y,
                           float ratio, Color color, string text)
    {
        var fondo = new GameObject(name, typeof(RectTransform), typeof(Image));
        fondo.transform.SetParent(card, false);

        var rt = fondo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width - 12f, 28f);
        fondo.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 0.9f);

        var relleno = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        relleno.transform.SetParent(fondo.transform, false);

        var frt = relleno.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        relleno.GetComponent<Image>().color = color;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(fondo.transform, false);
        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(8f, 0f);
        lrt.offsetMax = Vector2.zero;

        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 16f;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.text = text;
    }

    // Columna 3: lo que lleva puesto y lo que sabe hacer.
    private void BuildGear(Transform card, HeroController hero, float x, float width)
    {
        var label = NewCardLabel(card, "Col_Gear", x, width, 17f, TextAlignmentOptions.TopLeft);

        string estados = hero.Status.Describe();
        string linea = string.IsNullOrEmpty(estados)
            ? $"Maestría: {hero.Mastery.Describe()}"
            : $"Estados: {estados}";

        label.text = $"Pasivas: {PassiveSkills.Describe(hero.Passives)}\n" +
                     $"Arma: {GearLabel(hero, EquipmentSlot.Weapon)}\n" +
                     $"Escudo: {GearLabel(hero, EquipmentSlot.Shield)}   " +
                     $"Arm.: {GearLabel(hero, EquipmentSlot.Armor)}\n{linea}";
    }

    // Columna 4: cinco botones en dos filas, para no estirar la tarjeta a lo alto.
    private void CreateCardButtons(Transform card, HeroController hero, float x, float width)
    {
        var progress = hero.GetComponent<HeroProgress>();
        bool isTarget = synthesis != null && synthesis.Target == hero;
        bool canAscend = progress != null && progress.CanAscend(economy, crafting);
        bool inParty = party != null && party.IsInParty(hero);
        bool hasGear = shop != null && shop.FirstEquippableFor(hero) != null;
        bool wearsGear = hero.Weapon != null || hero.Shield != null
                         || hero.Armor != null || hero.Accessory != null;

        CreateButton(card, "Btn_Lock", x, 0,
            LocalizationManager.Get(hero.IsLocked ? "UI_LOCK" : "UI_UNLOCK"),
            hero.IsLocked ? lockedColor : synthColor, true, () => OnLockClicked(hero));

        CreateButton(card, "Btn_Party", x, 1,
            LocalizationManager.Get(inParty ? "UI_IN_PARTY" : "UI_PARTY"),
            inParty ? targetColor : synthColor, party != null, () => OnPartyClicked(hero));

        CreateButton(card, "Btn_Equip", x, 2, LocalizationManager.Get("UI_EQUIP"),
            hasGear ? gearColor : disabledColor, hasGear, () => OnEquipClicked(hero));

        CreateButton(card, "Btn_Ascend", x, 3, LocalizationManager.Get("UI_ASCEND"),
            canAscend ? ascendColor : disabledColor, canAscend, () => OnAscendClicked(hero));

        CreateButton(card, "Btn_Synth", x, 4,
            LocalizationManager.Get(isTarget ? "UI_SYNTH_TARGET" : "UI_SYNTH"),
            hero.IsLocked ? disabledColor : (isTarget ? targetColor : synthColor),
            synthesis != null && !hero.IsLocked, () => OnSynthClicked(hero));

        CreateButton(card, "Btn_Unequip", x, 5, LocalizationManager.Get("UI_UNEQUIP"),
            wearsGear ? gearColor : disabledColor, wearsGear, () => OnUnequipClicked(hero));

        bool puedeSubclase = hero.StarRank >= HeroSubclasses.MinStarRank;
        CreateButton(card, "Btn_Subclass", x, 6, LocalizationManager.Get("UI_SUBCLASS"),
            puedeSubclase ? ascendColor : disabledColor, puedeSubclase, () => OnSubclassClicked(hero));

        bool roto = hero.FirstBrokenSlot() != null;
        CreateButton(card, "Btn_Repair", x, 7, LocalizationManager.Get("UI_REPAIR"),
            roto ? lockedColor : disabledColor, roto && crafting != null, () => OnRepairClicked(hero));
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

    // El hueco decide la posición: tres botones arriba y dos abajo.
    private void CreateButton(Transform card, string name, float x, int slotIndex, string text,
                              Color color, bool interactable, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(card, false);

        int columna = slotIndex % 3;
        int fila = slotIndex / 3;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(
            x + columna * (cardButtonWidth + 6f),
            -cardPadding - fila * (cardButtonHeight + 6f));
        rt.sizeDelta = new Vector2(cardButtonWidth, cardButtonHeight);

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
        tmp.fontSize = 17f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = text;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.interactable = interactable;
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
        rt.sizeDelta = new Vector2(width - 12f, -cardPadding * 2f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void OnAscendClicked(HeroController hero)
    {
        var progress = hero.GetComponent<HeroProgress>();
        if (progress != null) progress.AscendHero(economy, crafting);
        Rebuild();
    }

    private void OnEquipClicked(HeroController hero)
    {
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
