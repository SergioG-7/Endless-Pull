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

    private Image barHp, barMp, barMoral, barFatiga;
    private TMP_Text txtHp, txtMp, txtMoral, txtFatiga;
    private Button usePotionButton;
    private TMP_Text usePotionLabel;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (roster == null) roster = UnityEngine.Object.FindFirstObjectByType<RosterUI>();
        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();

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
        if (usePotionLabel != null) usePotionLabel.text = $"Usar Poción ({pociones})";

        string activos = hero.Status.Describe();
        string apatia = hero.IsApathetic ? LocalizationManager.Get("UI_APATHETIC") : string.Empty;
        estados.text = (string.IsNullOrEmpty(activos) ? LocalizationManager.Get("UI_NO_STATUS") : activos) + apatia;
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

        // El tamaño de referencia se queda corto para el bloque de bio nuevo; se fuerza un mínimo.
        var panelSize = new Vector2(size.x, Mathf.Max(size.y, 620f + BioBlockHeight));
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

        float botonesY = -520f - BioBlockHeight;
        usePotionButton = UIBuild.Button(panel.transform, "Btn_UsePotion", "Usar Poción",
            new Color(0.30f, 0.55f, 0.35f), new Vector2(220f, 62f), new Vector2(-240f, botonesY),
            OnUsePotionPressed);
        usePotionLabel = usePotionButton.GetComponentInChildren<TMP_Text>();

        UIBuild.Button(panel.transform, "Btn_SeeRoster", "Ver en Roster",
            new Color(0.35f, 0.30f, 0.60f), new Vector2(220f, 62f), new Vector2(0f, botonesY),
            OnSeeInRosterPressed);

        UIBuild.Button(panel.transform, "Btn_CloseCard", "Cerrar",
            new Color(0.32f, 0.28f, 0.36f), new Vector2(220f, 62f), new Vector2(240f, botonesY),
            Close);
    }
}
