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

    [Tooltip("Taller del que salen las pociones de curación y de maná.")]
    [SerializeField] private CraftingManager crafting;

    [Tooltip("Tienda de la que sale el equipamiento.")]
    [SerializeField] private ShopManager shop;

    [Tooltip("Economía de la que sale la comida del botón Regalar.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Comida que cuesta cada regalo.")]
    [SerializeField] private int giftFoodCost = 15;

    [Tooltip("Afecto que da cada regalo (0-100).")]
    [SerializeField] private float giftAffinityGain = 5f;

    [Tooltip("Modal de equipamiento manual que abre el botón Equipar (también permite desequipar).")]
    [SerializeField] private EquipmentSelectModalUI equipModal;

    [Tooltip("Tamaño de la ficha.")]
    [SerializeField] private Vector2 size = new Vector2(760f, 620f);

    [Tooltip("Lado del retrato del héroe en la cabecera.")]
    [SerializeField] private float portraitSize = 140f;

    private GameObject panel;
    private HeroController hero;

    // Marca si esta ficha se abrió desde el Roster: al cerrar con [X] hay que volver ahí.
    private bool returnToRoster;

    private GameObject portraitFrame;
    private TMP_Text titulo;
    private TMP_Text subclase;
    private TMP_Text bio;
    private TMP_Text equipo;
    private TMP_Text estados;

    // Alto de la cabecera (retrato + nombre/clase/bio a su lado).
    private const float HeaderHeight = 176f;
    private const float HeaderTextInset = 180f;

    private const float ActionBtnWidth = 168f;
    private const float ActionBtnHeight = 46f;
    private const float ActionBtnGap = 12f;

    private Image barHp, barMp, barMoral, barFatiga, barAfinidad;
    private TMP_Text txtHp, txtMp, txtMoral, txtFatiga, txtAfinidad;
    private Button usePotionHpButton, usePotionMpButton;
    private TMP_Text potionHpLabel, potionMpLabel;
    private Button btnLock, btnEquip, btnGift;
    private TMP_Text seeRosterLabel;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (roster == null) roster = UnityEngine.Object.FindFirstObjectByType<RosterUI>();
        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();
        if (shop == null) shop = UnityEngine.Object.FindFirstObjectByType<ShopManager>();
        if (equipModal == null) equipModal = UnityEngine.Object.FindFirstObjectByType<EquipmentSelectModalUI>();
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();

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

    public void Show(HeroController target) => Show(target, false);

    // fromRoster marca que el [X] debe reabrir el Roster al cerrar esta ficha.
    public void Show(HeroController target, bool fromRoster)
    {
        if (panel == null || target == null) return;

        hero = target;
        returnToRoster = fromRoster;
        UIManager.OpenExclusive(panel);
        Refresh();
    }

    // La usa el modal de Equipar al cerrarse: reabre la ficha sin perder el héroe ni el
    // origen (Roster o mundo), porque UIManager.OpenExclusive solo desactiva el GameObject
    // y nunca llama a Close().
    public void Reopen()
    {
        if (panel == null || hero == null) return;

        UIManager.OpenExclusive(panel);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        hero = null;

        if (returnToRoster)
        {
            returnToRoster = false;
            if (roster != null) roster.Open();
        }
    }

    public void OnSeeInRosterPressed()
    {
        returnToRoster = false;
        Close();
        if (roster != null) roster.Open();
    }

    public void OnUseHpPotionPressed()
    {
        if (crafting != null && hero != null) crafting.TryUseHealingPotion(hero);
    }

    public void OnUseMpPotionPressed()
    {
        if (crafting != null && hero != null) crafting.TryUseManaPotion(hero);
    }

    private void Refresh()
    {
        var progress = hero.GetComponent<HeroProgress>();
        int nivel = progress != null ? progress.Level : 1;
        int tope = progress != null ? progress.MaxLevel : 0;

        var rareza = HeroProgress.RarityColor(hero.StarRank);
        var borde = portraitFrame.transform.Find("Border");
        if (borde != null) borde.GetComponent<Image>().color = rareza;
        UIBuild.HeroArt(portraitFrame.transform, hero.Data.bodySprite, portraitSize - 24f);

        var estrellas = new System.Text.StringBuilder();
        for (int i = 0; i < hero.StarRank; i++) estrellas.Append('★');

        titulo.text = $"{estrellas}  {hero.Data.heroName}   Nv. {nivel}/{tope}";
        titulo.color = rareza;

        string oficio = hero.Subclass != HeroSubclass.None ? hero.SubclassName : LocalizationManager.Get("UI_NO_SUBCLASS");
        string puesto = hero.AssignedBuilding != null
            ? string.Format(LocalizationManager.Get("UI_WORKS_AT"), BuildingTypes.DisplayName(hero.AssignedBuilding.Type))
            : string.Empty;
        subclase.text = $"{oficio}   ·   {HeroTraits.DisplayName(hero.Trait)}{puesto}";

        bio.text = hero.Data.GetLocalizedBio();

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

        UIBuild.SetBar(barAfinidad, hero.Affinity / 100f);
        txtAfinidad.text = string.Format(LocalizationManager.Get("UI_QUICKCARD_AFFINITY"), Mathf.RoundToInt(hero.Affinity));

        equipo.text = string.Format(LocalizationManager.Get("UI_QUICKCARD_GEAR"),
            hero.Attack, hero.Defense,
            Pieza(EquipmentSlot.Weapon), Pieza(EquipmentSlot.Shield),
            Pieza(EquipmentSlot.Armor), Pieza(EquipmentSlot.Accessory));

        int pocionesHp = crafting != null ? crafting.TotalHealingPotions : 0;
        if (usePotionHpButton != null) usePotionHpButton.interactable = pocionesHp > 0 && hero.CurrentHealth < hero.MaxHealth;
        if (potionHpLabel != null)
            potionHpLabel.text = string.Format(LocalizationManager.Get("UI_USE_POTION"), pocionesHp);

        int pocionesMp = crafting != null ? crafting.TotalManaPotions : 0;
        if (usePotionMpButton != null) usePotionMpButton.interactable = pocionesMp > 0 && hero.CurrentMP < hero.MaxMP;
        if (potionMpLabel != null)
            potionMpLabel.text = string.Format(LocalizationManager.Get("UI_USE_MANA_POTION"), pocionesMp);

        string activos = hero.Status.Describe();
        string apatia = hero.IsApathetic ? LocalizationManager.Get("UI_APATHETIC") : string.Empty;
        estados.text = (string.IsNullOrEmpty(activos) ? LocalizationManager.Get("UI_NO_STATUS") : activos) + apatia;

        RefreshActionButtons();
    }

    private void RefreshActionButtons()
    {
        bool puedeRegalar = economy != null && economy.Food >= giftFoodCost && hero.Affinity < 100f;

        SetActionButton(btnLock, LocalizationManager.Get(hero.IsLocked ? "UI_LOCK" : "UI_UNLOCK"),
            hero.IsLocked ? UITheme.DangerSoft : UITheme.Neutral, true);
        SetActionButton(btnEquip, LocalizationManager.Get("UI_EQUIP"), UITheme.Teal, shop != null || equipModal != null);
        SetActionButton(btnGift, string.Format(LocalizationManager.Get("UI_GIFT"), giftFoodCost),
            UITheme.AccentSoft, puedeRegalar);

        if (seeRosterLabel != null) seeRosterLabel.text = LocalizationManager.Get("UI_SEE_ROSTER");
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

    // El modal de equipamiento maneja equipar y desequipar la pieza seleccionada: ya no hace
    // falta un botón "Unequip" aparte en la ficha.
    private void OnEquipClicked()
    {
        if (hero == null) return;

        if (equipModal != null) { equipModal.Open(hero); return; }
        if (shop == null) return;

        shop.EquipFromInventory(hero, shop.FirstEquippableFor(hero));
        Refresh();
    }

    // Comida especial a cambio de afecto (ATK a partir de 50, EXP de entrenamiento al máximo).
    // Sustituye al viejo botón de cambiar subclase manualmente — la subclase se sigue asignando
    // sola (al azar en la invocación o a elegir al ascender), sin botón propio.
    private void OnGiftClicked()
    {
        if (hero == null || economy == null) return;
        if (!economy.TrySpendFood(giftFoodCost)) return;

        hero.AddAffinity(giftAffinityGain);
        SaveManager.RequestSave();
        Refresh();
    }

    private string Pieza(EquipmentSlot slot)
    {
        var item = hero.GetEquipped(slot);
        if (item == null) return LocalizationManager.Get("UI_GEAR_EMPTY");

        return hero.IsBroken(slot)
            ? string.Format(LocalizationManager.Get("UI_GEAR_PIECE_BROKEN"), item.LocalizedName())
            : string.Format(LocalizationManager.Get("UI_GEAR_PIECE"), item.LocalizedName(),
                hero.DurabilityOf(slot), item.maxDurability);
    }

private void Build()
    {
        if (canvas == null) return;

        var panelSize = new Vector2(size.x, Mathf.Max(size.y, 780f));
        panel = UIBuild.Panel(canvas.transform, "HeroQuickCard", panelSize, new Color(0.11f, 0.11f, 0.17f, 0.98f));

        UIBuild.CloseButtonTopRight(panel.transform, Close);

        // Cabecera: retrato grande a la izquierda, nombre/clase/estrellas/nivel y bio a su lado.
        portraitFrame = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portraitFrame.transform.SetParent(panel.transform, false);
        var prt = portraitFrame.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 1f);
        prt.anchorMax = new Vector2(0f, 1f);
        prt.pivot = new Vector2(0f, 1f);
        prt.sizeDelta = new Vector2(portraitSize, portraitSize);
        prt.anchoredPosition = new Vector2(24f, -24f);
        UITheme.Surface(portraitFrame, UITheme.Card, UITheme.Border, UITheme.RadiusCard);

        titulo = HeaderLabel("Title", UIBuild.TitleSize, -20f, 34f);
        subclase = HeaderLabel("Subclass", UIBuild.NameSize, -56f, 26f);
        bio = HeaderLabel("Bio", UIBuild.BodySize, -86f, 82f);
        bio.color = UITheme.TextSoft;

        barHp = UIBuild.Bar(panel.transform, "Bar_HP", new Vector2(size.x - 40f, 34f), new Vector2(0f, -(HeaderHeight)),
            new Color(0.80f, 0.25f, 0.25f), out txtHp);
        barMp = UIBuild.Bar(panel.transform, "Bar_MP", new Vector2(size.x - 40f, 34f), new Vector2(0f, -(HeaderHeight + 40f)),
            new Color(0.25f, 0.45f, 0.85f), out txtMp);
        barMoral = UIBuild.Bar(panel.transform, "Bar_Morale", new Vector2(size.x - 40f, 34f), new Vector2(0f, -(HeaderHeight + 80f)),
            new Color(0.85f, 0.70f, 0.25f), out txtMoral);
        barFatiga = UIBuild.Bar(panel.transform, "Bar_Fatigue", new Vector2(size.x - 40f, 34f), new Vector2(0f, -(HeaderHeight + 120f)),
            new Color(0.55f, 0.45f, 0.35f), out txtFatiga);
        barAfinidad = UIBuild.Bar(panel.transform, "Bar_Affinity", new Vector2(size.x - 40f, 34f), new Vector2(0f, -(HeaderHeight + 160f)),
            new Color(0.85f, 0.45f, 0.65f), out txtAfinidad);

        float gearY = -(HeaderHeight + 208f);
        equipo = UIBuild.TopLabel(panel.transform, "Gear", UIBuild.BodySize, 90f, gearY, TextAlignmentOptions.TopLeft);

        float statusY = gearY - 98f;
        estados = UIBuild.TopLabel(panel.transform, "Status", UIBuild.BodySize, 34f, statusY, TextAlignmentOptions.Left);

        // Grid 3x2, los 6 botones al mismo tamaño exacto: fila 1 Bloquear/Equipar/Subclase,
        // fila 2 Poción HP/Poción MP/Ver en Roster.
        float row1Y = statusY - 50f;
        float row2Y = row1Y - ActionBtnHeight - ActionBtnGap;

        btnLock = BuildActionButton(panel.transform, "Btn_Lock", row1Y, 0, OnLockClicked);
        btnEquip = BuildActionButton(panel.transform, "Btn_Equip", row1Y, 1, OnEquipClicked);
        btnGift = BuildActionButton(panel.transform, "Btn_Gift", row1Y, 2, OnGiftClicked);

        float xHp = GridX(0);
        float xMp = GridX(1);
        float xRoster = GridX(2);

        usePotionHpButton = UIBuild.Button(panel.transform, "Btn_UsePotionHp",
            string.Format(LocalizationManager.Get("UI_USE_POTION"), 0),
            new Color(0.30f, 0.55f, 0.35f), new Vector2(ActionBtnWidth, ActionBtnHeight), new Vector2(xHp, row2Y),
            OnUseHpPotionPressed);
        potionHpLabel = usePotionHpButton.GetComponentInChildren<TMP_Text>();

        usePotionMpButton = UIBuild.Button(panel.transform, "Btn_UsePotionMp",
            string.Format(LocalizationManager.Get("UI_USE_MANA_POTION"), 0),
            new Color(0.25f, 0.40f, 0.60f), new Vector2(ActionBtnWidth, ActionBtnHeight), new Vector2(xMp, row2Y),
            OnUseMpPotionPressed);
        potionMpLabel = usePotionMpButton.GetComponentInChildren<TMP_Text>();

        var btnSeeRoster = UIBuild.Button(panel.transform, "Btn_SeeRoster", LocalizationManager.Get("UI_SEE_ROSTER"),
            new Color(0.35f, 0.30f, 0.60f), new Vector2(ActionBtnWidth, ActionBtnHeight), new Vector2(xRoster, row2Y),
            OnSeeInRosterPressed);
        seeRosterLabel = btnSeeRoster.GetComponentInChildren<TMP_Text>();
    }


    // Posición X de una columna del grid 3x2 (0=izquierda, 1=centro, 2=derecha).
    private static float GridX(int column) => (column - 1) * (ActionBtnWidth + ActionBtnGap);


    // Etiqueta de cabecera, con el hueco del retrato descontado por la izquierda.
    // Margen derecho de la cabecera: deja sitio al botón [X] (48px + 16px de margen) para que
    // Título/Subclase no se metan debajo de él.
    private const float HeaderRightMargin = 72f;

    private TMP_Text HeaderLabel(string name, float fontSize, float y, float height)
    {
        var tmp = UIBuild.Label(panel.transform, name, fontSize, TextAlignmentOptions.TopLeft);
        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        // offsetMin/offsetMax bastan para un rect estirado horizontalmente (anchorMin.x != anchorMax.x):
        // NO tocar sizeDelta.x/anchoredPosition.x después, o se pisa el ancho ya calculado aquí.
        rt.offsetMin = new Vector2(HeaderTextInset, y - height);
        rt.offsetMax = new Vector2(-HeaderRightMargin, y);
        return tmp;
    }

    // Un botón por acción, en una única fila centrada.
    private Button BuildActionButton(Transform parent, string name, float topY, int slotIndex,
                                     UnityEngine.Events.UnityAction onClick)
    {
        float x = (slotIndex - 1) * (ActionBtnWidth + ActionBtnGap);

        return UIBuild.Button(parent, name, string.Empty, UITheme.Neutral,
            new Vector2(ActionBtnWidth, ActionBtnHeight), new Vector2(x, topY), onClick);
    }
}
