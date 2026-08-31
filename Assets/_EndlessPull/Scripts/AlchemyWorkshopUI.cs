using System.Collections.Generic;
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
    private const float RowGap = 24f;

    // La tarjeta se reparte en cuatro bandas: título, cuerpo, coste y botón al pie.
    // Bajado desde 76f para dejar sitio a la fila de pestañas.
    private const float CardTop = 120f;
    private const float ButtonHeight = 48f;

    // Alto que añade la segunda fila (poción + mejora) por debajo de las tres tarjetas originales.
    private const float RowExtra = CardHeight + RowGap;

    private GameObject panel;
    private TMP_Text titulo;
    private TMP_Text recursos;
    private TMP_Text feedback;
    private TMP_Text etiquetaCerrar;

    // Una ficha por operación: cuerpo con el detalle, coste y su botón.
    private Ficha piedra;
    private Ficha armas;
    private Ficha reparar;
    private Ficha pocion;
    private Ficha mejora;

    // Las 4 piedras se agrupan en un desplegable en vez de una tarjeta cada una.
    private AscensionStoneTier selectedStoneTier = AscensionStoneTier.Menor;
    private Button stoneDropdownTrigger;
    private GameObject stoneDropdownList;
    private GameObject stoneDropdownCatcher;
    private readonly List<Button> stoneOptionButtons = new List<Button>();
    private readonly List<TMP_Text> stoneOptionLabels = new List<TMP_Text>();

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
        LocalizationManager.LanguageChanged += Refresh;
        if (crafting != null) crafting.CraftResolved += OnResolved;
    }

    void OnDisable()
    {
        LocalizationManager.LanguageChanged -= Refresh;
        if (crafting != null) crafting.CraftResolved -= OnResolved;
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
        CloseStoneDropdown();
        UIManager.OpenExclusive(panel);
        RefreshTabs();
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        CloseStoneDropdown();
    }

    private void OnStonesTabPressed() => SetTab(WorkshopTab.Stones);
    private void OnEquipmentTabPressed() => SetTab(WorkshopTab.Equipment);
    private void OnAlchemyTabPressed() => SetTab(WorkshopTab.Alchemy);

    private void SetTab(WorkshopTab tab)
    {
        activeTab = tab;
        CloseStoneDropdown();
        RefreshTabs();
    }

    // Solo una pestaña visible a la vez: sus tarjetas se muestran, el resto se oculta.
    private void RefreshTabs()
    {
        StyleTab(tabStones, tabStonesLabel, activeTab == WorkshopTab.Stones);
        StyleTab(tabEquipment, tabEquipmentLabel, activeTab == WorkshopTab.Equipment);
        StyleTab(tabAlchemy, tabAlchemyLabel, activeTab == WorkshopTab.Alchemy);

        piedra.root.SetActive(activeTab == WorkshopTab.Stones);

        bool equip = activeTab == WorkshopTab.Equipment;
        armas.root.SetActive(equip);
        reparar.root.SetActive(equip);
        mejora.root.SetActive(equip);

        pocion.root.SetActive(activeTab == WorkshopTab.Alchemy);
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

        int artesanos = crafting.Artisans;
        string rebaja = artesanos > 0
            ? $"   <color={UITheme.Tag(UITheme.Cyan)}>" +
              $"{LocalizationManager.Get("UI_ARTISANS")} {artesanos} " +
              $"(-{Mathf.RoundToInt((1f - crafting.CostFactor) * 100f)}%)</color>"
            : string.Empty;

        recursos.text = economy != null
            ? $"{LocalizationManager.Get("UI_WOOD")} <b>{economy.Wood}</b>   " +
              $"{LocalizationManager.Get("UI_IRON")} <b>{economy.Iron}</b>   " +
              $"{LocalizationManager.Get("UI_FOOD")} <b>{economy.Food}</b>{rebaja}"
            : string.Empty;

        // Forja de Piedras: una sola ficha con la piedra elegida en el desplegable.
        RefreshStoneCard(piedra, selectedStoneTier);
        RefreshStoneDropdown();

        // Fabricación de Armas
        armas.titulo.text = LocalizationManager.Get("UI_FORGE_WEAPONS");
        armas.cuerpo.text = LocalizationManager.Get("UI_FORGE_WEAPONS_HELP");
        armas.coste.text = Cost(crafting.WeaponWoodCost, crafting.WeaponIronCost, crafting.WeaponFoodCost);
        SetButton(armas, LocalizationManager.Get("UI_CRAFT_WEAPON"), crafting.CanCraftWeapon, UITheme.Teal);

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

        // Mejora de Equipo
        mejora.titulo.text = LocalizationManager.Get("UI_UPGRADE_GEAR");
        mejora.cuerpo.text = LocalizationManager.Get("UI_UPGRADE_GEAR_HELP");
        mejora.coste.text = crafting.ForgeUnlocked
            ? Cost(crafting.UpgradeWoodCost, crafting.UpgradeIronCost, crafting.UpgradeFoodCost)
            : $"<color={UITheme.Tag(UITheme.TextFaint)}>{LocalizationManager.Get("UI_FORGE_LOCKED")}</color>";
        SetButton(mejora, LocalizationManager.Get("UI_UPGRADE_BUTTON"), crafting.CanUpgradeGear, UITheme.Teal);

        etiquetaCerrar.text = LocalizationManager.Get("UI_CLOSE");
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

    // Repinta el disparador del desplegable y resalta la piedra elegida en la lista.
    private void RefreshStoneDropdown()
    {
        if (stoneDropdownTrigger == null) return;

        for (int i = 0; i < stoneOptionLabels.Count; i++)
            stoneOptionLabels[i].color = (AscensionStoneTier)i == selectedStoneTier ? UITheme.Amber : UITheme.Text;
    }

    private void ToggleStoneDropdown()
    {
        if (stoneDropdownList == null) return;

        if (stoneDropdownList.activeSelf) CloseStoneDropdown();
        else OpenStoneDropdown();
    }

    private void OpenStoneDropdown()
    {
        if (stoneDropdownList != null) stoneDropdownList.SetActive(true);
        if (stoneDropdownCatcher != null) stoneDropdownCatcher.SetActive(true);
    }

    // La llama también el catcher a pantalla completa: clic fuera de la lista la cierra.
    private void CloseStoneDropdown()
    {
        if (stoneDropdownList != null) stoneDropdownList.SetActive(false);
        if (stoneDropdownCatcher != null) stoneDropdownCatcher.SetActive(false);
    }

    private void SelectStoneTier(AscensionStoneTier tier)
    {
        selectedStoneTier = tier;
        CloseStoneDropdown();
        Refresh();
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

        // El tamaño de referencia solo preveía tres tarjetas; se fuerza sitio para la fila extra
        // más el espacio que ocupa la fila de pestañas (ver CardTop).
        const float TabRowExtra = CardTop - 76f;
        var panelSize = new Vector2(size.x, Mathf.Max(size.y, 482f + RowExtra) + TabRowExtra);
        panel = UIBuild.Panel(canvas.transform, "AlchemyWorkshopPanel", panelSize, UITheme.Bg);

        titulo = UIBuild.TopLabel(panel.transform, "Title", UITheme.SizeTitle, 34f, -20f,
            TextAlignmentOptions.Left);

        recursos = UIBuild.TopLabel(panel.transform, "Resources", UITheme.SizeBody, 30f, -22f,
            TextAlignmentOptions.Right);
        recursos.color = UITheme.TextSoft;

        BuildTabs();

        float x = -(CardWidth + CardGap);
        piedra = CreateCard("Card_Stone", x, 0f, () => OnCraftStonePressed(selectedStoneTier));
        BuildStoneDropdown(piedra, x);
        armas = CreateCard("Card_Weapons", 0f, 0f, OnCraftWeaponPressed);
        reparar = CreateCard("Card_Repair", -x, 0f, OnRepairAllPressed);

        // Segunda fila, centrada bajo las tres tarjetas originales.
        float xPar = -(CardWidth + CardGap) * 0.5f;
        pocion = CreateCard("Card_Potion", xPar, RowExtra, OnCraftPotionPressed);
        mejora = CreateCard("Card_Upgrade", -xPar, RowExtra, OnUpgradeGearPressed);

        feedback = UIBuild.TopLabel(panel.transform, "Feedback", UITheme.SizeBody, 26f,
            -(CardTop + CardHeight + RowExtra + 8f), TextAlignmentOptions.Center);

        var cerrar = UIBuild.Button(panel.transform, "Btn_CloseWorkshop", string.Empty,
            Color.clear, new Vector2(240f, 46f), new Vector2(0f, -(panelSize.y - 62f)), Close);
        etiquetaCerrar = cerrar.GetComponentInChildren<TMP_Text>();
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

    // Botón "▾" en la ficha de piedra + lista flotante con las 4; clic fuera de la lista la cierra.
    private void BuildStoneDropdown(Ficha ficha, float cardX)
    {
        stoneDropdownTrigger = UIBuild.Button(ficha.titulo.transform.parent, "Btn_StoneTierToggle", "▾",
            UITheme.Neutral, new Vector2(28f, 22f), new Vector2(CardWidth * 0.5f - 24f, -12f), ToggleStoneDropdown);

        // Catcher a pantalla completa, igual que el fondo de un modal: cierra si se toca fuera.
        var catcherBtn = UIBuild.Button(panel.transform, "StoneDropdown_Catcher", string.Empty,
            Color.clear, Vector2.zero, Vector2.zero, CloseStoneDropdown);
        UIBuild.Stretch(catcherBtn.GetComponent<RectTransform>());
        stoneDropdownCatcher = catcherBtn.gameObject;

        // Centrado en pantalla como su propio popup: anclado a la tarjeta se salía por debajo
        // del panel (482 de alto) y tapaba la fila de pociones de abajo.
        var listGo = new GameObject("StoneDropdown_List", typeof(RectTransform));
        listGo.transform.SetParent(panel.transform, false);
        var lrt = listGo.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0.5f, 0.5f);
        lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(CardWidth, 4f * 34f);
        lrt.anchoredPosition = Vector2.zero;
        UITheme.Surface(listGo, UITheme.Card, UITheme.BorderCard, UITheme.RadiusCard);

        var tiers = (AscensionStoneTier[])System.Enum.GetValues(typeof(AscensionStoneTier));
        for (int i = 0; i < tiers.Length; i++)
        {
            var tier = tiers[i];
            var optGo = new GameObject("Option_" + tier, typeof(RectTransform), typeof(Image), typeof(Button));
            optGo.transform.SetParent(listGo.transform, false);
            var ort = optGo.GetComponent<RectTransform>();
            ort.anchorMin = new Vector2(0f, 1f);
            ort.anchorMax = new Vector2(1f, 1f);
            ort.pivot = new Vector2(0.5f, 1f);
            ort.sizeDelta = new Vector2(0f, 34f);
            ort.anchoredPosition = new Vector2(0f, -i * 34f);

            var img = UITheme.Surface(optGo, Color.clear, Color.clear, UITheme.RadiusItem);
            var optLabel = UIBuild.Label(optGo.transform, "Label", UITheme.SizeBody, TextAlignmentOptions.Left);
            UIBuild.Stretch(optLabel.rectTransform);
            optLabel.rectTransform.offsetMin = new Vector2(12f, 0f);
            optLabel.text = AscensionStoneTiers.DisplayName(tier);

            var optBtn = optGo.GetComponent<Button>();
            optBtn.targetGraphic = img;
            optBtn.onClick.AddListener(() => AudioManager.Play(SfxId.UiClick));
            optBtn.onClick.AddListener(() => SelectStoneTier(tier));

            stoneOptionButtons.Add(optBtn);
            stoneOptionLabels.Add(optLabel);
        }

        stoneDropdownList = listGo;

        // Encima de cualquier otra tarjeta, para que ninguna la tape.
        stoneDropdownCatcher.transform.SetAsLastSibling();
        stoneDropdownList.transform.SetAsLastSibling();

        stoneDropdownCatcher.SetActive(false);
        stoneDropdownList.SetActive(false);
    }

    // Tarjeta compacta: título arriba, cuerpo, coste pegado al botón y botón al pie.
    private Ficha CreateCard(string name, float x, float yOffset, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(panel.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(CardWidth, CardHeight);
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
            new Vector2(CardWidth - 32f, ButtonHeight),
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
