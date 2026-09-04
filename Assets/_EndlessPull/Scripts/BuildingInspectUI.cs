using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ficha del edificio: lo que produce, quién trabaja dentro y su mejora individual.
public class BuildingInspectUI : MonoBehaviour
{
    [Tooltip("Canvas donde se monta la ficha; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Economía que paga las mejoras.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Tamaño de la ficha; solo se usa el ancho, el alto se ajusta a los botones visibles.")]
    [SerializeField] private Vector2 size = new Vector2(880f, 700f);

    // La ficha ya no lleva la lista de héroes: el alto se recorta a los botones que de verdad
    // se enseñan, para que no quede un hueco muerto debajo del último.
    private const float SecondRowY = -200f;
    private const float RowStep = 64f;
    private const float BottomMargin = 24f;
    private const float ButtonHeight = 56f;

    private GameObject panel;
    private BaseBuilding building;

    private TMP_Text titulo;
    private TMP_Text ocupacion;
    private TMP_Text produccion;
    [Tooltip("Taller que repara el equipo desgastado.")]
    [SerializeField] private CraftingManager crafting;

    private Button botonMejora;
    private Button botonReparar;
    private Button botonAbrir;
    private Button botonAsignar;
    private TMP_Text etiquetaReparar;
    private TMP_Text etiquetaMejora;
    private TMP_Text etiquetaAsignar;

    private SquadManagementUI squadUI;
    private SanctuaryArchiveUI archiveUI;
    private WorkerAssignUI assignUI;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();

        Build();
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    void OnEnable() => LocalizationManager.LanguageChanged += OnLanguageChanged;
    void OnDisable() => LocalizationManager.LanguageChanged -= OnLanguageChanged;

    // La ficha solo se refrescaba al abrirla (Show()); si el idioma cambiaba con la ficha ya
    // abierta, título/ocupación/producción se quedaban en el idioma anterior.
    private void OnLanguageChanged()
    {
        if (IsOpen) Refresh();
    }

    public void Show(BaseBuilding target)
    {
        if (panel == null || target == null) return;

        building = target;
        UIManager.OpenExclusive(panel);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        building = null;
    }

    public void OnUpgradePressed()
    {
        if (building == null) return;

        building.TryUpgrade(economy);
        Refresh();
    }

    private void Refresh()
    {
        if (building == null) return;

        titulo.text = string.Format(LocalizationManager.Get("UI_BUILDING_TITLE"),
            BuildingTypes.DisplayName(building.Type), building.Level);
        ocupacion.text = string.Format(LocalizationManager.Get("UI_OCCUPANCY"),
            building.CurrentOccupants, building.Capacity,
            BuildingTypes.DisplayName(building.Type));
        produccion.text = string.Format(LocalizationManager.Get("UI_PER_TICK"), BeneficioPorTick());

        // El tope de nivel lo abre la Torre, no los materiales: si está topado se dice qué piso falta.
        bool tope = !building.CanUpgrade;
        bool puede = !tope && economy != null
                     && economy.CanAffordMaterials(building.NextWoodCost, building.NextIronCost);
        botonMejora.interactable = puede;
        botonMejora.targetGraphic.color = puede
            ? new Color(0.30f, 0.52f, 0.32f)
            : new Color(0.28f, 0.28f, 0.32f);
        etiquetaMejora.text = tope
            ? string.Format(LocalizationManager.Get("UI_UPGRADE_BUILDING_CAP"), building.NextLevelFloor)
            : string.Format(LocalizationManager.Get("UI_UPGRADE_BUILDING"),
                building.NextWoodCost, building.NextIronCost);

        RefreshMaintenance();
        RefreshOpenButton();
        RefreshAssignButton();
        ResizeToContent();
    }

    // El alto se recorta al botón visible más bajo; sin esto la ficha de una granja (que no
    // enseña mantenimiento) dejaba una franja vacía debajo del botón de asignar.
    private void ResizeToContent()
    {
        float masBajo = SecondRowY;

        foreach (var boton in new[] { botonReparar, botonAbrir, botonAsignar })
        {
            if (boton == null || !boton.gameObject.activeSelf) continue;

            float y = boton.GetComponent<RectTransform>().anchoredPosition.y;
            if (y < masBajo) masBajo = y;
        }

        var rt = panel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size.x, -masBajo + ButtonHeight + BottomMargin);
    }

    // Sala de Guerra y Archivo no usan trabajadores fijos: ahí el botón de asignar no aparece.
    private void RefreshAssignButton()
    {
        if (botonAsignar == null) return;

        bool usaTrabajadores = building.Type != BuildingType.WarRoom
                               && building.Type != BuildingType.Archive;

        if (botonAsignar.gameObject.activeSelf != usaTrabajadores)
            botonAsignar.gameObject.SetActive(usaTrabajadores);
        if (!usaTrabajadores) return;

        // Va pegado al de mejorar, como el de Sala de Guerra; solo baja un hueco en el Taller,
        // que es el único que además enseña la fila de mantenimiento.
        bool hayMantenimiento = botonReparar != null && botonReparar.gameObject.activeSelf;
        var rt = botonAsignar.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0f, hayMantenimiento ? SecondRowY - RowStep : SecondRowY);

        etiquetaAsignar.text = string.Format(LocalizationManager.Get("UI_ASSIGN_STAFF"),
            building.Workers.Count, building.Capacity);
    }

    // El panel de asignación es exclusivo, así que cierra esta ficha; al cerrarse la reabre.
    public void OnAssignPressed()
    {
        if (building == null) return;

        if (assignUI == null) assignUI = UnityEngine.Object.FindFirstObjectByType<WorkerAssignUI>();
        if (assignUI == null) return;

        var objetivo = building;
        assignUI.Show(objetivo, () => Show(objetivo));
    }

    // Sala de Guerra y Archivo abren su propio panel dedicado en vez de solo mostrar texto.
    private void RefreshOpenButton()
    {
        if (botonAbrir == null) return;

        bool esSalaDeGuerra = building.Type == BuildingType.WarRoom;
        bool esArchivo = building.Type == BuildingType.Archive;
        bool visible = esSalaDeGuerra || esArchivo;

        if (botonAbrir.gameObject.activeSelf != visible) botonAbrir.gameObject.SetActive(visible);
        if (!visible) return;

        var etiqueta = botonAbrir.GetComponentInChildren<TMP_Text>();
        etiqueta.text = esSalaDeGuerra
            ? LocalizationManager.Get("UI_SQUADS")
            : LocalizationManager.Get("UI_ARCHIVE_TITLE");
    }

    public void OnOpenPressed()
    {
        if (building == null) return;

        if (building.Type == BuildingType.WarRoom)
        {
            if (squadUI == null) squadUI = UnityEngine.Object.FindFirstObjectByType<SquadManagementUI>();
            squadUI?.Open();
        }
        else if (building.Type == BuildingType.Archive)
        {
            if (archiveUI == null) archiveUI = UnityEngine.Object.FindFirstObjectByType<SanctuaryArchiveUI>();
            archiveUI?.Show();
        }
    }

    // Sección "Mantenimiento de Equipo": el coste sube con el desgaste acumulado.
    private void RefreshMaintenance()
    {
        if (botonReparar == null) return;

        bool esTaller = building.Type == BuildingType.Workshop;
        if (botonReparar.gameObject.activeSelf != esTaller) botonReparar.gameObject.SetActive(esTaller);
        if (!esTaller) return;

        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();
        if (crafting == null) { botonReparar.gameObject.SetActive(false); return; }

        int desgaste = crafting.TotalWear();
        int madera = crafting.RepairAllWoodCost();
        int hierro = crafting.RepairAllIronCost();

        bool hayQueReparar = desgaste > 0;
        bool sePuedePagar = hayQueReparar && economy != null
                            && economy.CanAffordMaterials(madera, hierro);

        botonReparar.interactable = sePuedePagar;
        botonReparar.targetGraphic.color = sePuedePagar ? UITheme.AccentSoft : UITheme.Neutral;

        etiquetaReparar.text = hayQueReparar
            ? $"{LocalizationManager.Get("UI_MAINTENANCE")}: " +
              $"{LocalizationManager.Get("UI_REPAIR_ALL")}   ({madera}M / {hierro}H)"
            : LocalizationManager.Get("UI_NOTHING_BROKEN");
    }

    public void OnRepairAllPressed()
    {
        if (crafting != null) crafting.TryRepairAll();
        Refresh();
    }

    // Lo que aporta cada tick, en los términos propios de cada tipo.
    private string BeneficioPorTick()
    {
        switch (building.Type)
        {
            case BuildingType.TrainingDummy:
                return string.Format(LocalizationManager.Get("UI_PROD_TRAINING"),
                    building.ExpPerTick, building.TickInterval.ToString("0.#"));
            case BuildingType.Canteen:
            case BuildingType.RestArea:
                return string.Format(LocalizationManager.Get("UI_PROD_REST"),
                    building.HealPerTick, building.MoralePerTick.ToString("0.#"), building.TickInterval.ToString("0.#"));
            case BuildingType.Farm:
                return string.Format(LocalizationManager.Get("UI_PROD_FARM"),
                    building.FoodPerHarvest, building.HarvestInterval.ToString("0.#"));
            case BuildingType.Workshop:
                return string.Format(LocalizationManager.Get("UI_PROD_WORKSHOP"), building.Level * 5);
            case BuildingType.ManaWell:
                return string.Format(LocalizationManager.Get("UI_PROD_MANAWELL"),
                    building.ManaPerVisitTick, building.TickInterval.ToString("0.#"), building.PassiveManaPerSecond.ToString("0.#"));
            case BuildingType.Forge:
                if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();
                return crafting != null && crafting.ForgeUnlocked
                    ? string.Format(LocalizationManager.Get("UI_PROD_FORGE"), (crafting.ForgeDiscount * 100f).ToString("0"))
                    : LocalizationManager.Get("UI_FORGE_LOCKED");
            case BuildingType.WarRoom:
                return string.Format(LocalizationManager.Get("UI_PROD_WARROOM"), Mathf.Min(building.Level * 4f, 50f).ToString("0"));
            case BuildingType.Archive:
                return LocalizationManager.Get("UI_ARCHIVE_HINT");
        }
        return "-";
    }


    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "BuildingInspect", new Vector2(size.x, -SecondRowY + ButtonHeight + BottomMargin),
            new Color(0.10f, 0.13f, 0.12f, 0.98f));

        titulo = UIBuild.TopLabel(panel.transform, "Title", UIBuild.TitleSize, 46f, -14f,
            TextAlignmentOptions.Left);
        ocupacion = UIBuild.TopLabel(panel.transform, "Occupancy", UIBuild.NameSize, 32f, -62f,
            TextAlignmentOptions.Left);
        produccion = UIBuild.TopLabel(panel.transform, "Output", UIBuild.BodySize, 30f, -98f,
            TextAlignmentOptions.Left);

        botonMejora = UIBuild.Button(panel.transform, "Btn_Upgrade", "Mejorar Edificio",
            new Color(0.30f, 0.52f, 0.32f), new Vector2(size.x - 40f, 60f), new Vector2(0f, -136f),
            OnUpgradePressed);
        etiquetaMejora = botonMejora.GetComponentInChildren<TMP_Text>();

        // Mantenimiento: solo tiene sentido en el taller, así que se enseña y esconde según el tipo.
        botonReparar = UIBuild.Button(panel.transform, "Btn_RepairAll", string.Empty,
            UITheme.AccentSoft, new Vector2(size.x - 40f, ButtonHeight), new Vector2(0f, SecondRowY),
            OnRepairAllPressed);
        etiquetaReparar = botonReparar.GetComponentInChildren<TMP_Text>();

        // Sala de Guerra/Archivo: abre su propio panel en vez del texto de mantenimiento.
        botonAbrir = UIBuild.Button(panel.transform, "Btn_OpenBuilding", string.Empty,
            UITheme.AccentSoft, new Vector2(size.x - 40f, ButtonHeight), new Vector2(0f, SecondRowY),
            OnOpenPressed);

        // Asignar Personal: abre el panel compartido en vez de listar aquí el roster entero.
        botonAsignar = UIBuild.Button(panel.transform, "Btn_AssignStaff", string.Empty,
            UITheme.AccentSoft, new Vector2(size.x - 40f, ButtonHeight), new Vector2(0f, SecondRowY),
            OnAssignPressed);
        etiquetaAsignar = botonAsignar.GetComponentInChildren<TMP_Text>();

        UIBuild.CloseButtonTopRight(panel.transform, Close);
    }
}
