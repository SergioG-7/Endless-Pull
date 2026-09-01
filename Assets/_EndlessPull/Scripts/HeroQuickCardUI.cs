using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ficha rápida que sale al tocar a un héroe en la base.
public class HeroQuickCardUI : MonoBehaviour
{
    [Tooltip("Canvas donde se monta la ficha; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Roster al que salta el botón 'Ver en Roster'.")]
    [SerializeField] private RosterUI roster;

    [Tooltip("Taller del que salen las pociones de curación.")]
    [SerializeField] private CraftingManager crafting;

    [Tooltip("Tienda de la que salen inventario y equipo.")]
    [SerializeField] private ShopManager shop;

    [Tooltip("Escuadra a la que se apunta o se saca al héroe.")]
    [SerializeField] private PartyManager party;

    [Tooltip("Modal de equipamiento manual que abre el botón Equipar.")]
    [SerializeField] private EquipmentSelectModalUI equipModal;

    [Tooltip("Tamaño de la ficha.")]
    [SerializeField] private Vector2 size = new Vector2(760f, 620f);

    private GameObject panel;
    private HeroController hero;

    private TMP_Text titulo;
    private TMP_Text subclase;
    private TMP_Text equipo;
    private TMP_Text bio;
    private TMP_Text estados;

    // Alto que ocupa el bloque de bio nuevo; empuja estados y botones hacia abajo esa misma medida.
    private const float BioBlockHeight = 78f;

    // Alto de la grilla de 6 acciones (3 filas x 2 columnas) que empuja los botones inferiores.
    private const float ActionsBlockHeight = 182f;
    private const float ActionBtnWidth = 168f;
    private const float ActionBtnHeight = 46f;
    private const float ActionBtnGap = 12f;

    private Image barHp, barMp, barMoral, barFatiga;
    private TMP_Text txtHp, txtMp, txtMoral, txtFatiga;
    private Button usePotionButton;
    private TMP_Text usePotionLabel;
    private Button btnLock, btnParty, btnEquip, btnUnequip, btnSubclass, btnRepair;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (roster == null) roster = UnityEngine.Object.FindFirstObjectByType<RosterUI>();
        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();
        if (shop == null) shop = UnityEngine.Object.FindFirstObjectByType<ShopManager>();
        if (party == null) party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();
        if (equipModal == null) equipModal = UnityEngine.Object.FindFirstObjectByType<EquipmentSelectModalUI>();

        Build();
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    // Se refresca mientras esté abierta: la vida y el maná cambian solos.
    void Update()
    {
        if (!IsOpen) return;

        if (hero == null) { Close(); return; }
        Refresh();
    }

    public void Show(HeroController target)
    {
        if (panel == null || target == null) return;

        hero = target;
        UIManager.OpenExclusive(panel);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        hero = null;
    }

    public void OnSeeInRosterPressed()
    {
        Close();
        if (roster != null) roster.Open();
    }

    public void OnUsePotionPressed()
    {
        if (crafting != null && hero != null) crafting.TryUseHealingPotion(hero);
    }

    private void Refresh()
    {
        var progress = hero.GetComponent<HeroProgress>();
        int nivel = progress != null ? progress.Level : 1;
        int tope = progress != null ? progress.MaxLevel : 0;

        var estrellas = new System.Text.StringBuilder();
        for (int i = 0; i < hero.StarRank; i++) estrellas.Append('★');

        titulo.text = $"{estrellas}  {hero.Data.heroName}   Nv. {nivel}/{tope}";
        titulo.color = HeroProgress.RarityColor(hero.StarRank);

        string oficio = hero.Subclass != HeroSubclass.None ? hero.SubclassName : "sin subclase";
        string puesto = hero.AssignedBuilding != null
            ? $"   ·   Trabaja en {hero.AssignedBuilding.BuildingName}"
            : string.Empty;
        subclase.text = $"{oficio}   ·   {HeroTraits.DisplayName(hero.Trait)}{puesto}";

        UIBuild.SetBar(barHp, hero.MaxHealth > 0 ? (float)hero.CurrentHealth / hero.MaxHealth : 0f);
        txtHp.text = string.Format(LocalizationManager.Get("UI_QUICKCARD_HP"), hero.CurrentHealth, hero.MaxHealth);

        UIBuild.SetBar(barMp, hero.MaxMP > 0 ? (float)hero.CurrentMP / hero.MaxMP : 0f);
        txtMp.text = string.Format(LocalizationManager.Get("UI_QUICKCARD_MP"), hero.CurrentMP, hero.MaxMP);

        UIBuild.SetBar(barMoral, hero.MoralePercent / 100f);
        txtMoral.text = string.Format(LocalizationManager.Get("UI_QUICKCARD_MORALE"),
            hero.MoralePercent, hero.MoodName).TrimEnd();

        UIBuild.SetBar(barFatiga, hero.Fatigue / 100f);
        txtFatiga.text = string.Format(LocalizationManager.Get("UI_QUICKCARD_FATIGUE"), hero.FatiguePercent) +
                         (hero.IsExhausted ? LocalizationManager.Get("UI_EXHAUSTED") : string.Empty);

        equipo.text = string.Format(LocalizationManager.Get("UI_QUICKCARD_GEAR"),
            hero.Attack, hero.Defense,
            Pieza(EquipmentSlot.Weapon), Pieza(EquipmentSlot.Shield),
            Pieza(EquipmentSlot.Armor), Pieza(EquipmentSlot.Accessory));

        bio.text = hero.Data.GetLocalizedBio();

        int pociones = crafting != null ? crafting.HealingPotions : 0;
        if (usePotionButton != null) usePotionButton.interactable = pociones > 0 && hero.CurrentHealth < hero.MaxHealth;
        if (usePotionLabel != null)
            usePotionLabel.text = string.Format(LocalizationManager.Get("UI_USE_POTION"), pociones);

        string activos = hero.Status.Describe();
        string apatia = hero.IsApathetic ? LocalizationManager.Get("UI_APATHETIC") : string.Empty;
        estados.text = (string.IsNullOrEmpty(activos) ? LocalizationManager.Get("UI_NO_STATUS") : activos) + apatia;

        RefreshActionButtons();
    }

    private void RefreshActionButtons()
    {
        bool inParty = party != null && party.IsInParty(hero);
        bool wearsGear = hero.Weapon != null || hero.Shield != null
                         || hero.Armor != null || hero.Accessory != null;
        bool puedeSubclase = hero.StarRank >= HeroSubclasses.MinStarRank;
        bool roto = hero.FirstBrokenSlot() != null;

        SetActionButton(btnLock, LocalizationManager.Get(hero.IsLocked ? "UI_LOCK" : "UI_UNLOCK"),
            hero.IsLocked ? UITheme.DangerSoft : UITheme.Neutral, true);
        SetActionButton(btnParty, LocalizationManager.Get(inParty ? "UI_IN_PARTY" : "UI_PARTY"),
            inParty ? UITheme.Amber : UITheme.Neutral, party != null);
        SetActionButton(btnEquip, LocalizationManager.Get("UI_EQUIP"), UITheme.Teal, shop != null);
        SetActionButton(btnUnequip, LocalizationManager.Get("UI_UNEQUIP"), UITheme.Teal, wearsGear);
        SetActionButton(btnSubclass, LocalizationManager.Get("UI_SUBCLASS"), UITheme.AccentSoft, puedeSubclase);
        SetActionButton(btnRepair, LocalizationManager.Get("UI_REPAIR"), UITheme.DangerSoft, roto && crafting != null);
    }

    private static void SetActionButton(Button button, string text, Color color, bool interactable)
    {
        if (button == null) return;

        var label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = text;
            label.color = interactable ? UITheme.Text : UITheme.TextFaint;
        }

        button.targetGraphic.color = interactable ? color : UITheme.Neutral;
        button.interactable = interactable;
    }

    private void OnLockClicked()
    {
        if (hero == null) return;

        hero.ToggleLock();

        SaveManager.RequestSave();
        Refresh();
    }

    private void OnPartyClicked()
    {
        if (hero == null || party == null) return;

        party.Toggle(hero);
        Refresh();
    }

    private void OnEquipClicked()
    {
        if (hero == null) return;

        if (equipModal != null) { equipModal.Open(hero); return; }
        if (shop == null) return;

        shop.EquipFromInventory(hero, shop.FirstEquippableFor(hero));
        Refresh();
    }

    private void OnUnequipClicked()
    {
        if (hero == null || shop == null) return;

        if (hero.Weapon != null) shop.UnequipToInventory(hero, EquipmentSlot.Weapon);
        else if (hero.Shield != null) shop.UnequipToInventory(hero, EquipmentSlot.Shield);
        else if (hero.Armor != null) shop.UnequipToInventory(hero, EquipmentSlot.Armor);
        else if (hero.Accessory != null) shop.UnequipToInventory(hero, EquipmentSlot.Accessory);

        Refresh();
    }

    // Rota entre las tres subclases del arquetipo del héroe.
    private void OnSubclassClicked()
    {
        if (hero == null) return;

        var progress = hero.GetComponent<HeroProgress>();
        if (progress != null) progress.CycleSubclass();
        Refresh();
    }

    private void OnRepairClicked()
    {
        if (hero == null) return;

        if (crafting != null) crafting.TryRepair(hero);
        Refresh();
    }

    private string Pieza(EquipmentSlot slot)
    {
        var item = hero.GetEquipped(slot);
        if (item == null) return LocalizationManager.Get("UI_GEAR_EMPTY");

        return hero.IsBroken(slot)
            ? string.Format(LocalizationManager.Get("UI_GEAR_PIECE_BROKEN"), item.equipName)
            : string.Format(LocalizationManager.Get("UI_GEAR_PIECE"), item.equipName,
                hero.DurabilityOf(slot), item.maxDurability);
    }

    private void Build()
    {
        if (canvas == null) return;

        // El tamaño de referencia se queda corto para el bloque de bio y la grilla de acciones; se fuerza un mínimo.
        var panelSize = new Vector2(size.x, Mathf.Max(size.y, 620f + BioBlockHeight + ActionsBlockHeight));
        panel = UIBuild.Panel(canvas.transform, "HeroQuickCard", panelSize, new Color(0.11f, 0.11f, 0.17f, 0.98f));

        titulo = UIBuild.TopLabel(panel.transform, "Title", UIBuild.TitleSize, 46f, -14f,
            TextAlignmentOptions.Left);
        subclase = UIBuild.TopLabel(panel.transform, "Subclass", UIBuild.NameSize, 32f, -62f,
            TextAlignmentOptions.Left);

        barHp = UIBuild.Bar(panel.transform, "Bar_HP", new Vector2(size.x - 40f, 34f), new Vector2(0f, -108f),
            new Color(0.80f, 0.25f, 0.25f), out txtHp);
        barMp = UIBuild.Bar(panel.transform, "Bar_MP", new Vector2(size.x - 40f, 34f), new Vector2(0f, -148f),
            new Color(0.25f, 0.45f, 0.85f), out txtMp);
        barMoral = UIBuild.Bar(panel.transform, "Bar_Morale", new Vector2(size.x - 40f, 34f), new Vector2(0f, -188f),
            new Color(0.85f, 0.70f, 0.25f), out txtMoral);
        barFatiga = UIBuild.Bar(panel.transform, "Bar_Fatigue", new Vector2(size.x - 40f, 34f), new Vector2(0f, -228f),
            new Color(0.55f, 0.45f, 0.35f), out txtFatiga);

        equipo = UIBuild.TopLabel(panel.transform, "Gear", UIBuild.BodySize, 160f, -280f,
            TextAlignmentOptions.TopLeft);
        bio = UIBuild.TopLabel(panel.transform, "Bio", UIBuild.BodySize, BioBlockHeight - 8f, -448f,
            TextAlignmentOptions.TopLeft);
        bio.color = UITheme.TextSoft;
        estados = UIBuild.TopLabel(panel.transform, "Status", UIBuild.BodySize, 40f, -448f - BioBlockHeight,
            TextAlignmentOptions.Left);

        // Grilla de 6 acciones (2 columnas x 3 filas) traída de HeroDetailModal.
        float actionsTop = -520f - BioBlockHeight;
        btnLock = BuildActionButton(panel.transform, "Btn_Lock", actionsTop, 0, OnLockClicked);
        btnParty = BuildActionButton(panel.transform, "Btn_Party", actionsTop, 1, OnPartyClicked);
        btnEquip = BuildActionButton(panel.transform, "Btn_Equip", actionsTop, 2, OnEquipClicked);
        btnUnequip = BuildActionButton(panel.transform, "Btn_Unequip", actionsTop, 3, OnUnequipClicked);
        btnSubclass = BuildActionButton(panel.transform, "Btn_Subclass", actionsTop, 4, OnSubclassClicked);
        btnRepair = BuildActionButton(panel.transform, "Btn_Repair", actionsTop, 5, OnRepairClicked);

        float botonesY = actionsTop - ActionsBlockHeight;
        usePotionButton = UIBuild.Button(panel.transform, "Btn_UsePotion",
            string.Format(LocalizationManager.Get("UI_USE_POTION"), 0),
            new Color(0.30f, 0.55f, 0.35f), new Vector2(220f, 62f), new Vector2(-240f, botonesY),
            OnUsePotionPressed);
        usePotionLabel = usePotionButton.GetComponentInChildren<TMP_Text>();

        UIBuild.Button(panel.transform, "Btn_SeeRoster", LocalizationManager.Get("UI_SEE_ROSTER"),
            new Color(0.35f, 0.30f, 0.60f), new Vector2(220f, 62f), new Vector2(0f, botonesY),
            OnSeeInRosterPressed);

        UIBuild.Button(panel.transform, "Btn_CloseCard", LocalizationManager.Get("UI_CLOSE"),
            new Color(0.32f, 0.28f, 0.36f), new Vector2(220f, 62f), new Vector2(240f, botonesY),
            Close);
    }

    // Un botón por acción; dos por fila, centrados bajo el bloque de estados.
    private Button BuildActionButton(Transform parent, string name, float topY, int slotIndex,
                                     UnityEngine.Events.UnityAction onClick)
    {
        int columna = slotIndex % 2;
        int fila = slotIndex / 2;

        float x = (columna - 0.5f) * (ActionBtnWidth + ActionBtnGap);
        float y = topY - fila * (ActionBtnHeight + ActionBtnGap);

        return UIBuild.Button(parent, name, string.Empty, UITheme.Neutral,
            new Vector2(ActionBtnWidth, ActionBtnHeight), new Vector2(x, y), onClick);
    }
}
