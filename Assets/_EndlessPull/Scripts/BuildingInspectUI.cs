using System.Collections.Generic;
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

    [Tooltip("Tamaño de la ficha.")]
    [SerializeField] private Vector2 size = new Vector2(880f, 700f);

    [Tooltip("Alto de cada fila de héroe asignable.")]
    [SerializeField] private float rowHeight = 52f;

    private GameObject panel;
    private BaseBuilding building;

    private TMP_Text titulo;
    private TMP_Text ocupacion;
    private TMP_Text produccion;
    private RectTransform lista;
    private GameObject listaViewport;
    [Tooltip("Taller que repara el equipo desgastado.")]
    [SerializeField] private CraftingManager crafting;

    private Button botonMejora;
    private Button botonReparar;
    private Button botonAbrir;
    private TMP_Text etiquetaReparar;
    private TMP_Text etiquetaMejora;

    private SquadManagementUI squadUI;
    private SanctuaryArchiveUI archiveUI;

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

    private void OnWorkerPressed(HeroController hero)
    {
        if (building == null) return;

        building.ToggleWorker(hero);
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

        bool puede = economy != null && economy.CanAffordMaterials(building.NextWoodCost, building.NextIronCost);
        botonMejora.interactable = puede;
        botonMejora.targetGraphic.color = puede
            ? new Color(0.30f, 0.52f, 0.32f)
            : new Color(0.28f, 0.28f, 0.32f);
        etiquetaMejora.text = string.Format(LocalizationManager.Get("UI_UPGRADE_BUILDING"),
            building.NextWoodCost, building.NextIronCost);

        RefreshMaintenance();
        RefreshOpenButton();

        // Sala de Guerra y Archivo no usan trabajadores fijos: la lista de asignación
        // se quedaba debajo del botón de abrir panel, duplicando el acceso a Escuadras.
        bool usaTrabajadores = building.Type != BuildingType.WarRoom && building.Type != BuildingType.Archive;
        if (listaViewport != null) listaViewport.SetActive(usaTrabajadores);
        if (usaTrabajadores) RebuildWorkerList();
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

    private void RebuildWorkerList()
    {
        for (int i = lista.childCount - 1; i >= 0; i--)
            Destroy(lista.GetChild(i).gameObject);

        var heroes = new List<HeroController>(
            UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None));

        // Se ordena por rango para que la jerarquía se vea en el propio listado.
        heroes.Sort((a, b) => BaseBuilding.Rank(b).CompareTo(BaseBuilding.Rank(a)));

        lista.sizeDelta = new Vector2(lista.sizeDelta.x, heroes.Count * (rowHeight + 6f) + 8f);

        foreach (var hero in heroes)
        {
            if (hero == null || hero.Data == null) continue;

            bool dentro = building.IsWorker(hero);
            bool hayHueco = building.Workers.Count < building.Capacity;

            string etiquetaAsignar = LocalizationManager.Get(dentro ? "BTN_UNASSIGN" : "BTN_ASSIGN");
            var fila = UIBuild.Button(lista, "Row_" + hero.name,
                $"{hero.Data.heroName}  {hero.StarRank}★  ·  {etiquetaAsignar}",
                dentro ? new Color(0.30f, 0.52f, 0.32f) : new Color(0.28f, 0.28f, 0.34f),
                new Vector2(0f, rowHeight), Vector2.zero, () => OnWorkerPressed(hero));

            var rt = fila.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, rowHeight);

            fila.interactable = dentro || hayHueco;
        }
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "BuildingInspect", size, new Color(0.10f, 0.13f, 0.12f, 0.98f));

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
            UITheme.AccentSoft, new Vector2(size.x - 40f, 56f), new Vector2(0f, -200f),
            OnRepairAllPressed);
        etiquetaReparar = botonReparar.GetComponentInChildren<TMP_Text>();

        // Sala de Guerra/Archivo: abre su propio panel en vez del texto de mantenimiento.
        botonAbrir = UIBuild.Button(panel.transform, "Btn_OpenBuilding", string.Empty,
            UITheme.AccentSoft, new Vector2(size.x - 40f, 56f), new Vector2(0f, -200f),
            OnOpenPressed);

        // Lista de asignación con scroll: el roster puede ser largo.
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image),
                                      typeof(Mask), typeof(ScrollRect));
        viewport.transform.SetParent(panel.transform, false);
        listaViewport = viewport;

        var vrt = viewport.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = new Vector2(20f, 90f);
        vrt.offsetMax = new Vector2(-20f, -210f);
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

        var layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewport.GetComponent<ScrollRect>();
        scroll.viewport = vrt;
        scroll.content = lista;
        scroll.horizontal = false;

        UIBuild.CloseButtonTopRight(panel.transform, Close);
    }
}
