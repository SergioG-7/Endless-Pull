using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pestaña activa del Taller.
public enum WorkshopTab
{
    Stones,
    Equipment,
    Alchemy
}

// Taller de Alquimia: tarjetas compactas para las 4 Piedras de Ascensión, armas, reparación,
// pociones y mejora de equipo. Todo se paga con madera, hierro y comida; las gemas quedan
// para el gacha y las recargas. Repartido en 3 pestañas para que ninguna tarjeta se solape.
public class AlchemyWorkshopUI : MonoBehaviour
{
    [Tooltip("Canvas sobre el que se monta el modal.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Taller que forja las piedras y fabrica las armas.")]
    [SerializeField] private CraftingManager crafting;

    [Tooltip("Economía de la que salen los materiales.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Tamaño del modal.")]
    [SerializeField] private Vector2 size = new Vector2(1000f, 482f);

    [Tooltip("Color del texto cuando la operación sale bien.")]
    [SerializeField] private Color successColor = new Color(0.45f, 0.95f, 0.55f);

    private const float CardWidth = 300f;
    private const float CardHeight = 268f;
    private const float CardGap = 20f;

    // La tarjeta se reparte en cuatro bandas: título, cuerpo, coste y botón al pie.
    // Bajado desde 76f para dejar sitio a la fila de pestañas.
    private const float CardTop = 120f;
    private const float ButtonHeight = 48f;

    private GameObject panel;
    private TMP_Text titulo;
    private TMP_Text artesanos;
    private TMP_Text feedback;

    // Una ficha por operación: cuerpo con el detalle, coste y su botón.
    // Las piedras van cada una en su propia tarjeta, paginadas de StonePageSize en StonePageSize.
    private readonly Ficha[] piedras = new Ficha[6];
    private const float StoneCardWidth = 226f;
    private const float StoneCardGap = 16f;
    private const int StonePageSize = 4;

    private Ficha armas;
    private Ficha reparar;
    private Ficha mejora;
    private Ficha pocion;
    private Ficha pocionMana;

    // Un UIPager por fila; las flechas se ocultan solas si todo cabe en una página.
    private UIPager stonesPager;
    private UIPager forgePager;
    private UIPager alchemyPager;

    // Pestañas: Piedras, Forja/Reparación de Equipo, Alquimia.
    private WorkshopTab activeTab = WorkshopTab.Stones;
    private Button tabStones, tabEquipment, tabAlchemy;
    private TMP_Text tabStonesLabel, tabEquipmentLabel, tabAlchemyLabel;

    private class Ficha
    {
        public GameObject root;
        public TMP_Text titulo;
        public TMP_Text cuerpo;
        public TMP_Text coste;
        public Button boton;
        public TMP_Text etiqueta;
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();

        Build();
    }

    void OnEnable()
    {
        LocalizationManager.LanguageChanged += OnLanguageChanged;
        if (crafting != null) crafting.CraftResolved += OnResolved;
    }

    void OnDisable()
    {
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
        if (crafting != null) crafting.CraftResolved -= OnResolved;
    }

    // Refresh() ya cubre cabecera y tarjetas (Update() la repasa cada frame mientras está
    // abierto); las pestañas quedaban fuera porque su texto solo se fijaba en BuildTabs().
    private void OnLanguageChanged()
    {
        RefreshTabs();
        Refresh();
        stonesPager.RefreshLocalization();
        forgePager.RefreshLocalization();
        alchemyPager.RefreshLocalization();
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    // Los materiales cambian solos con la granja: mientras esté abierto se repasa.
    void Update()
    {
        if (IsOpen) Refresh();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (panel == null) return;

        if (feedback != null) feedback.text = string.Empty;
        UIManager.OpenExclusive(panel);
        RefreshTabs();
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void OnStonesTabPressed() => SetTab(WorkshopTab.Stones);
    private void OnEquipmentTabPressed() => SetTab(WorkshopTab.Equipment);
    private void OnAlchemyTabPressed() => SetTab(WorkshopTab.Alchemy);

    private void SetTab(WorkshopTab tab)
    {
        activeTab = tab;
        RefreshTabs();
    }

    // Solo una pestaña visible a la vez: sus tarjetas se muestran, el resto se oculta.
    private void RefreshTabs()
    {
        tabStonesLabel.text = LocalizationManager.Get("UI_FORGE_STONES");
        tabEquipmentLabel.text = LocalizationManager.Get("UI_TAB_EQUIPMENT");
        tabAlchemyLabel.text = LocalizationManager.Get("UI_TAB_ALCHEMY");

        StyleTab(tabStones, tabStonesLabel, activeTab == WorkshopTab.Stones);
        StyleTab(tabEquipment, tabEquipmentLabel, activeTab == WorkshopTab.Equipment);
        StyleTab(tabAlchemy, tabAlchemyLabel, activeTab == WorkshopTab.Alchemy);

        // Cada pager decide qué tarjetas de su fila se ven (la página activa) y las oculta todas
        // cuando su pestaña no es la que está abierta.
        stonesPager.SetTabActive(activeTab == WorkshopTab.Stones);
        forgePager.SetTabActive(activeTab == WorkshopTab.Equipment);
        alchemyPager.SetTabActive(activeTab == WorkshopTab.Alchemy);
    }

    private static void StyleTab(Button tab, TMP_Text label, bool active)
    {
        tab.targetGraphic.color = active ? UITheme.AccentPick : Color.clear;
        label.color = active ? UITheme.Text : UITheme.TextFaint;
    }

    public void OnCraftStonePressed(AscensionStoneTier tier)
    {
        if (crafting != null) crafting.TryCraftStone(tier);
        Refresh();
    }

    public void OnCraftWeaponPressed()
    {
        if (crafting != null) crafting.TryCraftWeapon();
        Refresh();
    }

    public void OnRepairAllPressed()
    {
        if (crafting != null) crafting.TryRepairAll();
        Refresh();
    }

    public void OnCraftPotionPressed()
    {
        if (crafting != null) crafting.TryCraftPotion();
        Refresh();
    }

    public void OnCraftManaPotionPressed()
    {
        if (crafting != null) crafting.TryCraftManaPotion();
        Refresh();
    }

    public void OnUpgradeGearPressed()
    {
        if (crafting != null) crafting.TryUpgradeAllGear();
        Refresh();
    }

    private void OnResolved(bool success, string message)
    {
        if (feedback == null) return;

        feedback.text = message;
        feedback.color = success ? successColor : UITheme.DangerLight;
    }

    private void Refresh()
    {
        if (titulo == null || crafting == null) return;

        titulo.text = LocalizationManager.Get("UI_WORKSHOP");

        // Los materiales ya se ven en el TopBar de fondo (Fase 38): solo queda aquí el
        // descuento de los artesanos, que es información propia del Taller.
        int numArtesanos = crafting.Artisans;
        artesanos.text = numArtesanos > 0
            ? $"<color={UITheme.Tag(UITheme.Cyan)}>{LocalizationManager.Get("UI_ARTISANS")} " +
              $"{numArtesanos} (-{Mathf.RoundToInt((1f - crafting.CostFactor) * 100f)}%)</color>"
            : string.Empty;

        // Forja de Piedras: las 6 tarjetas directas, sin desplegable.
        var tiers = (AscensionStoneTier[])System.Enum.GetValues(typeof(AscensionStoneTier));
        for (int i = 0; i < tiers.Length && i < piedras.Length; i++)
            RefreshStoneCard(piedras[i], tiers[i]);

        // Fabricación de Armas
        armas.titulo.text = LocalizationManager.Get("UI_FORGE_WEAPONS");
        armas.cuerpo.text = LocalizationManager.Get("UI_FORGE_WEAPONS_HELP");
        armas.coste.text = Cost(crafting.WeaponWoodCost, crafting.WeaponIronCost, crafting.WeaponFoodCost);
        SetButton(armas, LocalizationManager.Get("UI_CRAFT_EQUIPMENT"), crafting.CanCraftWeapon, UITheme.Teal);

        // Reparación de Equipo
        int desgaste = crafting.TotalWear();
        reparar.titulo.text = LocalizationManager.Get("UI_REPAIR_GEAR");
        reparar.cuerpo.text = $"{LocalizationManager.Get("UI_TOTAL_WEAR")} <b>{desgaste}</b>";
        reparar.coste.text = desgaste > 0
            ? Cost(crafting.RepairAllWoodCost(), crafting.RepairAllIronCost(), 0)
            : $"<color={UITheme.Tag(UITheme.TextFaint)}>" +
              $"{LocalizationManager.Get("UI_NOTHING_BROKEN")}</color>";

        bool puedeReparar = desgaste > 0 && economy != null
            && economy.CanAffordMaterials(crafting.RepairAllWoodCost(), crafting.RepairAllIronCost());
        SetButton(reparar, LocalizationManager.Get("UI_REPAIR_ALL"), puedeReparar, UITheme.DangerSoft);

        // Pociones de Curación
        pocion.titulo.text = LocalizationManager.Get("UI_FORGE_POTIONS");
        pocion.cuerpo.text = $"{LocalizationManager.Get("UI_POTIONS_HELD")} <b>{crafting.HealingPotions}</b>";
        pocion.coste.text = Cost(crafting.PotionWoodCost, 0, crafting.PotionFoodCost);
        SetButton(pocion, LocalizationManager.Get("UI_CRAFT_POTION"), crafting.CanCraftPotion, UITheme.Cyan);

        // Pociones de Maná
        pocionMana.titulo.text = LocalizationManager.Get("UI_FORGE_MANA_POTIONS");
        pocionMana.cuerpo.text = $"{LocalizationManager.Get("UI_MANA_POTIONS_HELD")} <b>{crafting.ManaPotions}</b>";
        pocionMana.coste.text = Cost(crafting.ManaPotionWoodCost, 0, crafting.ManaPotionFoodCost);
        bool puedeManaPotion = economy != null
            && economy.CanAffordMaterials(crafting.ManaPotionWoodCost, 0) && economy.CanAffordFood(crafting.ManaPotionFoodCost);
        SetButton(pocionMana, LocalizationManager.Get("UI_CRAFT_MANA_POTION"), puedeManaPotion, UITheme.Cyan);

        // Mejora de Equipo
        mejora.titulo.text = LocalizationManager.Get("UI_UPGRADE_GEAR");
        mejora.cuerpo.text = LocalizationManager.Get("UI_UPGRADE_GEAR_HELP");
        mejora.coste.text = crafting.ForgeUnlocked
            ? Cost(crafting.UpgradeWoodCost, crafting.UpgradeIronCost, crafting.UpgradeFoodCost)
            : $"<color={UITheme.Tag(UITheme.TextFaint)}>{LocalizationManager.Get("UI_FORGE_LOCKED")}</color>";
        SetButton(mejora, LocalizationManager.Get("UI_UPGRADE_BUTTON"), crafting.CanUpgradeGear, UITheme.Teal);
    }

    private void RefreshStoneCard(Ficha ficha, AscensionStoneTier tier)
    {
        ficha.titulo.text = AscensionStoneTiers.DisplayName(tier);
        ficha.cuerpo.text = $"{LocalizationManager.Get("UI_STONES_HELD")} " +
                            $"<b>{crafting.StoneCount(tier)}</b>\n" +
                            $"{LocalizationManager.Get("UI_SUCCESS_CHANCE")} " +
                            $"<b>{crafting.EffectiveSuccessChance * 100f:0}%</b>";
        ficha.coste.text = Cost(crafting.StoneWoodCost(tier), crafting.StoneIronCost(tier), 0);
        SetButton(ficha, LocalizationManager.Get("UI_FORGE"), crafting.CanCraftStone(tier), UITheme.Amber);
    }

    // Coste en columnas: el recurso a la izquierda y la cifra siempre en la misma tabulación.
    private static string Cost(int wood, int iron, int food)
    {
        string texto = Row(LocalizationManager.Get("UI_WOOD"), wood);

        if (iron > 0) texto += "\n" + Row(LocalizationManager.Get("UI_IRON"), iron);
        if (food > 0) texto += "\n" + Row(LocalizationManager.Get("UI_FOOD"), food);
        return texto;
    }

    private static string Row(string nombre, int cantidad)
        => $"<color={UITheme.Tag(UITheme.TextFaint)}>{nombre}</color><pos=62%><b>{cantidad}</b>";

    private static void SetButton(Ficha ficha, string texto, bool activo, Color color)
    {
        ficha.etiqueta.text = texto;
        ficha.boton.interactable = activo;
        ficha.boton.targetGraphic.color = activo ? color : UITheme.Neutral;
        ficha.etiqueta.color = activo ? UITheme.Text : UITheme.TextFaint;
    }

    private void Build()
    {
        if (canvas == null) return;

        // El tamaño de referencia solo preveía tres tarjetas en una fila; ahora las 3 pestañas
        // (Piedras, Forja/Reparación, Alquimia) muestran cada una una única fila pareja de
        // tarjetas del mismo alto — ya no hace falta una segunda fila.
        const float TabRowExtra = CardTop - 76f;
        var panelSize = new Vector2(size.x, Mathf.Max(size.y, CardTop + CardHeight + 60f) + TabRowExtra);
        panel = UIBuild.Panel(canvas.transform, "AlchemyWorkshopPanel", panelSize, UITheme.Bg);

        UIBuild.CloseButtonTopRight(panel.transform, Close);

        titulo = UIBuild.TopLabel(panel.transform, "Title", UITheme.SizeTitle, 34f, -20f,
            TextAlignmentOptions.Left);
        // Deja sitio al botón [X] (48px + 16px de margen) para que el título no quede debajo.
        titulo.rectTransform.offsetMax = new Vector2(-72f, titulo.rectTransform.offsetMax.y);

        artesanos = UIBuild.TopLabel(panel.transform, "Artisans", UITheme.SizeBody, 30f, -22f,
            TextAlignmentOptions.Right);
        artesanos.color = UITheme.TextSoft;
        artesanos.rectTransform.offsetMax = new Vector2(-72f, artesanos.rectTransform.offsetMax.y);

        BuildTabs();

        // La posición inicial no importa: stonesPager.Setup() las reubica en cuanto se crea,
        // más abajo — solo hace falta que existan como GameObjects.
        var tiers = (AscensionStoneTier[])System.Enum.GetValues(typeof(AscensionStoneTier));
        for (int i = 0; i < tiers.Length && i < piedras.Length; i++)
        {
            var tier = tiers[i];
            piedras[i] = CreateCard("Card_Stone_" + tier, 0f, 0f,
                StoneCardWidth, () => OnCraftStonePressed(tier));
        }

        // Forja/Reparación: 3 tarjetas (Armas, Mejora, Reparar). Alquimia: Poción de Curación y
        // de Maná. Mismo motivo: la posición real la fija el pager de cada fila, no CreateCard.
        armas = CreateCard("Card_Weapons", 0f, 0f, OnCraftWeaponPressed);
        mejora = CreateCard("Card_Upgrade", 0f, 0f, OnUpgradeGearPressed);
        reparar = CreateCard("Card_Repair", 0f, 0f, OnRepairAllPressed);

        pocion = CreateCard("Card_Potion", 0f, 0f, OnCraftPotionPressed);
        pocionMana = CreateCard("Card_PotionMana", 0f, 0f, OnCraftManaPotionPressed);

        feedback = UIBuild.TopLabel(panel.transform, "Feedback", UITheme.SizeTitle, 32f,
            -(CardTop + CardHeight + 8f), TextAlignmentOptions.Center);

        BuildPagers();
    }

    // Un UIPager por fila: huecos fijos por página, flechas que solo aparecen si hacen falta.
    private void BuildPagers()
    {
        const float pagerY = 74f;

        float stoneStep = StoneCardWidth + StoneCardGap;
        var stoneSlots = new float[StonePageSize];
        for (int i = 0; i < StonePageSize; i++) stoneSlots[i] = (i - (StonePageSize - 1) / 2f) * stoneStep;
        stonesPager = new UIPager(panel.transform, new Vector2(0f, pagerY));
        stonesPager.Setup(System.Array.ConvertAll(piedras, f => f.root), stoneSlots);

        float step3 = CardWidth + CardGap;
        forgePager = new UIPager(panel.transform, new Vector2(0f, pagerY));
        forgePager.Setup(new[] { armas.root, mejora.root, reparar.root }, new[] { -step3, 0f, step3 });

        float xPar = (CardWidth + CardGap) * 0.5f;
        alchemyPager = new UIPager(panel.transform, new Vector2(0f, pagerY));
        alchemyPager.Setup(new[] { pocion.root, pocionMana.root }, new[] { -xPar, xPar });

        // Arranca en la pestaña de Piedras: las otras dos filas se ocultan hasta que se abra su
        // pestaña (RefreshTabs las reactivará cuando toque).
        forgePager.SetTabActive(false);
        alchemyPager.SetTabActive(false);
    }

    // Tres botones de pestaña anclados arriba a la izquierda, mismo estilo que SanctuaryUI.
    private void BuildTabs()
    {
        tabStones = BuildTabButton("Tab_Stones", LocalizationManager.Get("UI_FORGE_STONES"),
            24f, 180f, OnStonesTabPressed);
        tabStonesLabel = tabStones.GetComponentInChildren<TMP_Text>();

        tabEquipment = BuildTabButton("Tab_Equipment", LocalizationManager.Get("UI_TAB_EQUIPMENT"),
            24f + 180f + 10f, 220f, OnEquipmentTabPressed);
        tabEquipmentLabel = tabEquipment.GetComponentInChildren<TMP_Text>();

        tabAlchemy = BuildTabButton("Tab_Alchemy", LocalizationManager.Get("UI_TAB_ALCHEMY"),
            24f + 180f + 10f + 220f + 10f, 180f, OnAlchemyTabPressed);
        tabAlchemyLabel = tabAlchemy.GetComponentInChildren<TMP_Text>();
    }

    private Button BuildTabButton(string name, string text, float x, float width,
                                  UnityEngine.Events.UnityAction onClick)
    {
        var button = UIBuild.Button(panel.transform, name, text, Color.clear,
            new Vector2(width, 36f), new Vector2(x, -64f), onClick);

        var rt = button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -64f);

        UITheme.Surface(button.gameObject, Color.clear, UITheme.BorderStrong, UITheme.RadiusButton);
        button.targetGraphic = button.GetComponent<Image>();
        return button;
    }

    // Tarjeta compacta: título arriba, cuerpo, coste pegado al botón y botón al pie.
    private Ficha CreateCard(string name, float x, float yOffset, UnityEngine.Events.UnityAction onClick)
        => CreateCard(name, x, yOffset, CardWidth, onClick);

    // Sobrecarga con ancho propio: la usan las tarjetas de piedra, más angostas que el resto.
    private Ficha CreateCard(string name, float x, float yOffset, float width,
                             UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(panel.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(width, CardHeight);
        rt.anchoredPosition = new Vector2(x, -CardTop - yOffset);

        UITheme.Surface(go, UITheme.Card, UITheme.BorderCard, UITheme.RadiusCard);

        var ficha = new Ficha
        {
            root = go,
            titulo = CardLabel(go.transform, "Title", UITheme.SizeValue, -14f, 26f,
                TextAlignmentOptions.Left),
            cuerpo = CardLabel(go.transform, "Body", UITheme.SizeBody, -46f, 62f,
                TextAlignmentOptions.TopLeft),
            coste = CardLabel(go.transform, "Cost", UITheme.SizeBody, -114f, 78f,
                TextAlignmentOptions.TopLeft)
        };

        ficha.titulo.fontStyle = FontStyles.Bold;
        ficha.cuerpo.color = UITheme.TextSoft;
        ficha.cuerpo.lineSpacing = 10f;
        ficha.coste.lineSpacing = 10f;

        // El botón se ancla al pie de la tarjeta, dentro de su borde.
        ficha.boton = UIBuild.Button(go.transform, "Btn_Action", string.Empty, UITheme.Amber,
            new Vector2(width - 32f, ButtonHeight),
            new Vector2(0f, -(CardHeight - 14f - ButtonHeight)), onClick);
        ficha.etiqueta = ficha.boton.GetComponentInChildren<TMP_Text>();
        ficha.etiqueta.fontSize = UITheme.SizeBody;
        ficha.etiqueta.fontStyle = FontStyles.Bold;

        // Sacude el botón si lo tocan sin fondos suficientes, en vez de no responder nada.
        UnaffordableFeedback.Attach(ficha.boton.gameObject);

        return ficha;
    }

    private static TMP_Text CardLabel(Transform parent, string name, float size, float y,
                                      float height, TextAlignmentOptions align)
    {
        var tmp = UIBuild.Label(parent, name, size, align);

        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(16f, 0f);
        rt.offsetMax = new Vector2(-16f, 0f);
        rt.sizeDelta = new Vector2(-32f, height);
        rt.anchoredPosition = new Vector2(0f, y);
        return tmp;
    }
}
