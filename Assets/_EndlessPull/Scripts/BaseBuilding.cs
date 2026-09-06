using System.Collections.Generic;
using UnityEngine;

// Tipos de zona interactiva de la base.
public enum BuildingType
{
    TrainingDummy,
    Canteen,
    RestArea,
    Farm,
    Workshop,
    ManaWell,
    Forge,
    WarRoom,
    Archive,
    Lodging,
    WoodworkingShop,
    MetalProcessing
}

// Nombres visibles de los tipos de edificio.
public static class BuildingTypes
{
    public static string DisplayName(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.TrainingDummy: return LocalizationManager.Get("BLD_TRAINING");
            case BuildingType.Canteen: return LocalizationManager.Get("BLD_CANTEEN");
            case BuildingType.RestArea: return LocalizationManager.Get("BLD_RESTAREA");
            case BuildingType.Farm: return LocalizationManager.Get("BLD_FARM");
            case BuildingType.Workshop: return LocalizationManager.Get("BLD_WORKSHOP");
            case BuildingType.ManaWell: return LocalizationManager.Get("BLD_MANAWELL");
            case BuildingType.Forge: return LocalizationManager.Get("BLD_FORGE");
            case BuildingType.WarRoom: return LocalizationManager.Get("BLD_WARROOM");
            case BuildingType.Archive: return LocalizationManager.Get("BLD_ARCHIVE");
            case BuildingType.Lodging: return LocalizationManager.Get("BLD_LODGING");
            case BuildingType.WoodworkingShop: return LocalizationManager.Get("BLD_WOODWORKING");
            case BuildingType.MetalProcessing: return LocalizationManager.Get("BLD_METALWORKS");
        }
        return type.ToString();
    }

    // Tinte del sprite por tipo: todos comparten el mismo sprite genérico (UI_Rounded),
    // sin esto se ven como cajas idénticas y confunden Taller/Forja o Descanso/Pozo de Maná.
    public static Color AccentColor(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.TrainingDummy: return new Color(0.75f, 0.35f, 0.30f);
            case BuildingType.Canteen: return new Color(0.85f, 0.55f, 0.25f);
            case BuildingType.RestArea: return new Color(0.35f, 0.65f, 0.55f);
            case BuildingType.Farm: return new Color(0.40f, 0.65f, 0.35f);
            case BuildingType.Workshop: return new Color(0.45f, 0.55f, 0.70f);
            case BuildingType.ManaWell: return new Color(0.30f, 0.55f, 0.85f);
            case BuildingType.Forge: return new Color(0.80f, 0.40f, 0.20f);
            case BuildingType.WarRoom: return new Color(0.70f, 0.25f, 0.30f);
            case BuildingType.Archive: return new Color(0.55f, 0.50f, 0.60f);
            case BuildingType.Lodging: return new Color(0.45f, 0.40f, 0.65f);
            case BuildingType.WoodworkingShop: return new Color(0.55f, 0.42f, 0.25f);
            case BuildingType.MetalProcessing: return new Color(0.50f, 0.52f, 0.56f);
        }
        return Color.white;
    }
}

public class BaseBuilding : MonoBehaviour
{
    [Tooltip("Qué hace el edificio con el héroe que lo visita.")]
    [SerializeField] private BuildingType type = BuildingType.TrainingDummy;

    [Tooltip("Nombre visible del edificio.")]
    [SerializeField] private string buildingName = "Campo de Entrenamiento";

    [Tooltip("Clave de localización del nombre; vacío usa el nombre genérico del tipo.")]
    [SerializeField] private string nameKey = string.Empty;

    [Tooltip("Catálogo de ilustraciones por tipo; sin él (o sin sprite para este tipo) se queda el bloque tintado.")]
    [SerializeField] private BuildingArt art;

    [Tooltip("Categoría del campo de entrenamiento: 1 normal, 2 avanzado, 3 élite. Escala lo que da por tick.")]
    [SerializeField, Range(1, 3)] private int trainingTier = 1;

    [Tooltip("Estrellas mínimas para poder usar el edificio; 0 = abierto a todos.")]
    [SerializeField] private int minStarRank;

    [Tooltip("Radio en el que el héroe se considera dentro del edificio.")]
    [SerializeField] private float interactionRadius = 4f;

    [Tooltip("Margen desde el borde del edificio al que se pegan los puntos de llegada.")]
    [SerializeField] private float slotInset = 0.7f;

    [Tooltip("Puntos de llegada repartidos alrededor del edificio.")]
    [Range(4, 8)]
    [SerializeField] private int slotCount = 8;

    [Tooltip("Segundos entre cada efecto aplicado al héroe.")]
    [SerializeField] private float tickInterval = 2f;

    [Tooltip("EXP por tick en el campo de entrenamiento, en nivel 1. Simbólica a propósito: el nivel se gana en la Torre, no dejando al héroe en el muñeco.")]
    [SerializeField] private int expPerTick = 1;

    [Tooltip("Puntos de maestría de arma por tick de entrenamiento.")]
    [SerializeField] private int masteryPerTrainingTick = 2;

    [Tooltip("Refinamiento de habilidad (0-1) que gana un héroe por tick de entrenamiento; baja el enfriamiento y sube la precisión de su habilidad activa.")]
    [SerializeField] private float skillRefinementPerTrainingTick = 0.01f;

    [Tooltip("Probabilidad por tick de que entrenar despierte una pasiva; muy por debajo del 15% por piso de la Torre.")]
    [SerializeField, Range(0f, 1f)] private float trainingAwakeningChance = 0.005f;

    [Tooltip("Vida por tick en cantina y zona de descanso, en nivel 1.")]
    [SerializeField] private int healPerTick = 6;

    [Tooltip("Moral por tick en cantina y zona de descanso.")]
    [SerializeField] private float moralePerTick = 5f;

    [Tooltip("Segundos que se queda un héroe en cada visita.")]
    [SerializeField] private Vector2 visitDuration = new Vector2(8f, 12f);

    [Tooltip("Nivel del edificio; escala el efecto por tick.")]
    [SerializeField] private int level = 1;

    [Tooltip("Madera que cuesta la siguiente mejora, por nivel actual.")]
    [SerializeField] private int woodCostPerLevel = 20;

    [Tooltip("Hierro que cuesta la siguiente mejora, por nivel actual.")]
    [SerializeField] private int ironCostPerLevel = 10;

    [Tooltip("Comida que produce la granja en cada cosecha, en nivel 1.")]
    [SerializeField] private int foodPerHarvest = 5;

    [Tooltip("Segundos entre cosechas de la granja.")]
    [SerializeField] private float harvestInterval = 10f;

    [Tooltip("Material que produce por cosecha la Carpintería (madera) o la Fundición (hierro).")]
    [SerializeField] private int materialPerHarvest = 3;

    [Tooltip("Fatiga que quita cada tick en los Dormitorios; dormir cansa menos que descansar de pie.")]
    [SerializeField] private float fatigueRecoveryPerTick = 20f;

    [Tooltip("MP que restaura el Pozo de Maná a quien lo visita, en nivel 1.")]
    [SerializeField] private int manaPerVisitTick = 8;

    [Tooltip("MP por segundo que el Pozo de Maná regenera a sus trabajadores fijos sin necesidad de visita.")]
    [SerializeField] private float passiveManaPerSecond = 1f;

    [Tooltip("Piso de torre a partir del cual existe este edificio; 0 = desde el principio.")]
    [SerializeField] private int requiredFloor;

    [Tooltip("Producción extra por nivel, en tanto por uno acumulativo.")]
    [SerializeField] private float extraPerLevel = 0.15f;

    [Tooltip("Tope de ocupantes por edificio, por muy alto que sea el nivel.")]
    [SerializeField] private int capacityCap = 4;

    [Tooltip("Plazas extra de salida; los sitios de descanso y entrenamiento admiten más gente.")]
    [SerializeField] private int capacityBonus;

    private float harvestTimer;
    private EconomyManager economy;

    // Candado por edificio: null si nunca estuvo bloqueado (requiredFloor == 0).
    private GameObject lockOverlay;
    private TMPro.TextMeshPro lockLabel;


    // Trabajadores fijos asignados a mano desde la ficha del edificio.
    private readonly List<HeroController> workers = new List<HeroController>();

    // Registro estático: evita que cada héroe escanee la escena entera.
    private static readonly List<BaseBuilding> all = new List<BaseBuilding>();
    public static IReadOnlyList<BaseBuilding> All => all;

    public BuildingType Type => type;
    public string BuildingName => buildingName;

    // Nombre para la interfaz: el propio del edificio si lo tiene registrado, y si no el genérico
    // del tipo. Sin esto los tres campos de entrenamiento salían con el mismo rótulo.
    public string DisplayName => string.IsNullOrEmpty(nameKey)
        ? BuildingTypes.DisplayName(type)
        : LocalizationManager.Get(nameKey);

    public int MinStarRank => minStarRank;

    // Cada categoría rinde el doble que la anterior: normal x1, avanzado x2, élite x4.
    public float TrainingFactor => Mathf.Pow(2f, Mathf.Clamp(trainingTier, 1, 3) - 1);

    // Los campos buenos son para los de rango: un 1★ no entrena donde los 5★.
    public bool AllowsHero(HeroController hero)
        => minStarRank <= 0 || (hero != null && hero.StarRank >= minStarRank);
    public float InteractionRadius => interactionRadius;
    public float TickInterval => tickInterval;
    public int Level => level;

    // El nivel no suma lineal: cada nivel rinde algo más que el anterior.
    public float LevelFactor => level * (1f + extraPerLevel * (level - 1));

    public int ExpPerTick => Mathf.RoundToInt(expPerTick * LevelFactor);

    // Lo que da de verdad el campo: la categoría multiplica sobre el nivel del edificio, así que
    // el élite rinde el cuádruple que el normal al mismo nivel.
    public int TrainingExpPerTick => Mathf.Max(1, Mathf.RoundToInt(expPerTick * LevelFactor * TrainingFactor));
    public int TrainingMasteryPerTick => Mathf.Max(1, Mathf.RoundToInt(masteryPerTrainingTick * TrainingFactor));
    public int FoodPerHarvest => Mathf.RoundToInt(foodPerHarvest * LevelFactor);
    public int MaterialPerHarvest => Mathf.RoundToInt(materialPerHarvest * LevelFactor);

    // Los tres que producen solos con el tiempo, sin que nadie los visite.
    public bool IsProducer => type == BuildingType.Farm
                              || type == BuildingType.WoodworkingShop
                              || type == BuildingType.MetalProcessing;
    public float HarvestInterval => harvestInterval;
    public int ManaPerVisitTick => Mathf.RoundToInt(manaPerVisitTick * LevelFactor);
    public float FatigueRecoveryPerTick => fatigueRecoveryPerTick * LevelFactor;
    public float PassiveManaPerSecond => passiveManaPerSecond * LevelFactor;
    public int HealPerTick => Mathf.RoundToInt(healPerTick * LevelFactor);
    public float MoralePerTick => moralePerTick;
    // El coste crece con el triangular del nivel, no en linea recta: la produccion por nivel es
    // cuadratica, y un coste lineal dejaba que la base se pagase sus propias mejoras sola.
    public int NextWoodCost => woodCostPerLevel * level * (level + 1) / 2;
    public int NextIronCost => ironCostPerLevel * level * (level + 1) / 2;

    public int RequiredFloor => requiredFloor;
    public bool IsUnlocked => TowerFloor >= requiredFloor;

    // Piso actual de la torre; lo publica el WaveManager para no consultarlo por edificio.
    public static int TowerFloor { get; private set; } = 1;

    // Salta cuando Townia sube de nivel al superar un hito; lleva el nivel nuevo.
    public static event System.Action<int> TownLevelChanged;

    public static void SetTowerFloor(int floor)
    {
        int nivelAntes = TownLevel;
        TowerFloor = Mathf.Max(1, floor);

        // Los edificios que aún no tocan se apagan; los que ya tocan aparecen.
        foreach (var b in all)
            if (b != null) b.RefreshUnlock();

        if (TownLevel > nivelAntes) TownLevelChanged?.Invoke(TownLevel);
    }

    // Pisos de Torre que hacen subir Townia un nivel; sin tope, como el lobby del manhwa.
    public const int FloorsPerTownLevel = 10;

    // Nivel de Townia: 1 de salida y uno más cada FloorsPerTownLevel pisos superados.
    public static int TownLevel => 1 + Mathf.Max(0, TowerFloor) / FloorsPerTownLevel;

    // Piso del próximo hito de Townia; es también el que abre la siguiente tanda de edificios.
    public static int NextTownMilestone => TownLevel * FloorsPerTownLevel;

    // Sitios por instalación: uno por nivel de Townia. Los muñecos de un campo de entrenamiento
    // salen de aquí, así que subir Townia es lo que permite entrenar a más gente a la vez.
    public static int FloorCapacity => TownLevel;

    // Nivel maximo por edificio: la base crece al subir la Torre, un nivel mas cada 5 pisos.
    // Los materiales son el coste secundario, no la puerta.
    public const int FloorsPerLevelCap = 5;
    public static int MaxLevel => 1 + TowerFloor / FloorsPerLevelCap;

    // Piso que hace falta superar para el siguiente nivel; 0 si ya no hay tope que esperar.
    public int NextLevelFloor => level < MaxLevel ? 0 : level * FloorsPerLevelCap;
    public bool CanUpgrade => level < MaxLevel;

    public int Capacity => Mathf.Min(capacityCap, FloorCapacity + (level - 1) + capacityBonus);

    public IReadOnlyList<HeroController> Workers => workers;

    // Ocupación real: los asignados más quien esté de visita ahora mismo.
    public int CurrentOccupants
    {
        get
        {
            PruneWorkers();
            int count = workers.Count;

            foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
                if (hero != null && hero.CurrentBuilding == this && !workers.Contains(hero)) count++;

            return count;
        }
    }

    public bool IsWorker(HeroController hero) => hero != null && workers.Contains(hero);
    public bool HasRoom => CurrentOccupants < Capacity;

    // Asignar y desasignar desde la ficha del edificio.
    // Por qué se rechazó la última asignación; lo pinta el panel de asignación en rojo.
    public string LastRefusal { get; private set; } = string.Empty;

    public bool ToggleWorker(HeroController hero)
    {
        if (hero == null) return false;

        LastRefusal = string.Empty;

        if (workers.Remove(hero))
        {
            hero.SetAssignedBuilding(null);
            LevelChanged?.Invoke(level);
            SaveManager.RequestSave();
            return false;
        }

        // Fase 39: asignar a un edificio ya no comprueba escuadra/expedición (desacoplado a
        // propósito, ver HeroAssignment.IsBusyElsewhere) — un héroe puede currar aquí y estar en
        // una escuadra a la vez; se desasigna solo al desplegar esa escuadra de verdad.


        // Puerta de rango antes que el aforo: si no tiene rango da igual que quede sitio, y
        // decir "está lleno" cuando el motivo es otro solo confunde.
        if (!AllowsHero(hero))
        {
            LastRefusal = string.Format(LocalizationManager.Get("UI_BUILDING_RANK_LOCKED"),
                DisplayName, minStarRank);
            Debug.LogWarning($"[Edificio] {LastRefusal}", this);
            return false;
        }

        if (workers.Count >= Capacity)
        {
            LastRefusal = string.Format(LocalizationManager.Get("UI_ASSIGN_FULL"), DisplayName);
            Debug.LogWarning($"[Edificio] {buildingName} está al completo ({workers.Count}/{Capacity}).", this);
            return false;
        }

        // Unicidad de trabajador (roadmap Fase 40 §3): asignarse aquí desasigna del edificio previo.
        if (hero.AssignedBuilding != null && hero.AssignedBuilding != this)
            hero.AssignedBuilding.ToggleWorker(hero);

        workers.Add(hero);
        hero.SetAssignedBuilding(this);
        LevelChanged?.Invoke(level);
        SaveManager.RequestSave();
        return true;
    }

    // La usa el SaveManager al restaurar la partida.
    public void LoadWorker(HeroController hero)
    {
        if (hero == null || workers.Contains(hero)) return;

        workers.Add(hero);
        hero.SetAssignedBuilding(this);
    }

    private void PruneWorkers()
    {
        for (int i = workers.Count - 1; i >= 0; i--)
            if (workers[i] == null) workers.RemoveAt(i);
    }

    // Jerarquía: escuadra primero, luego estrellas y luego nivel.
    public static int Rank(HeroController hero)
        => Rank(hero, UnityEngine.Object.FindFirstObjectByType<PartyManager>());

    // Sobrecarga con la escuadra ya resuelta: ordenar un roster grande hacía una búsqueda de
    // PartyManager por héroe, y eso solo costaba 75 ms al abrir el panel de asignación.
    public static int Rank(HeroController hero, PartyManager party)
    {
        if (hero == null) return -1;

        int enEscuadra = party != null && party.IsInParty(hero) ? 1000 : 0;

        var progress = hero.GetComponent<HeroProgress>();
        return enEscuadra + hero.StarRank * 100 + (progress != null ? progress.Level : 1);
    }

    // Deja entrar si hay hueco; si no, echa al de menor rango cuando el que llega manda más.
    public bool TryAdmit(HeroController hero)
    {
        if (hero == null || !IsUnlocked) return false;
        if (IsWorker(hero) || HasRoom) return true;

        HeroController peor = null;
        int peorRango = int.MaxValue;

        foreach (var other in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
        {
            if (other == null || other == hero || other.CurrentBuilding != this) continue;
            if (IsWorker(other)) continue;

            int rango = Rank(other);
            if (rango >= peorRango) continue;

            peorRango = rango;
            peor = other;
        }

        if (peor == null || Rank(hero) <= peorRango) return false;

        peor.EvictFromBuilding();
        Debug.Log($"[Jerarquía] {hero.Data.heroName} desplaza a {peor.Data.heroName} de {buildingName}.", this);
        return true;
    }

private void RefreshUnlock()
    {
        bool unlocked = IsUnlocked;

        // Se apaga el sprite/rótulo reales; el candado (si existe) toma su sitio.
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (lockOverlay != null && sr.transform.IsChildOf(lockOverlay.transform)) continue;

            // Con ilustración puesta, el bloque tintado se queda apagado siempre.
            if (artSprite != null && (sr == bodySprite || sr.gameObject.name == "Frame"))
            {
                sr.enabled = false;
                continue;
            }

            // El edificio bloqueado no desaparece: se queda en penumbra bajo el candado. Antes se
            // apagaba entero y solo quedaban el rótulo y el candado flotando sobre el suelo, que
            // es lo que parecía un edificio invisible.
            if (artSprite != null && sr == artSprite)
            {
                sr.enabled = true;
                bool hayNiebla = art != null && art.lockedFog != null;
                sr.color = unlocked || hayNiebla ? Color.white : lockedTint;
                continue;
            }

            sr.enabled = unlocked;
        }

        foreach (var t in GetComponentsInChildren<TMPro.TextMeshPro>(true))
            if (t != lockLabel) t.enabled = unlocked;

        if (lockOverlay != null)
        {
            lockOverlay.SetActive(!unlocked);
            FitLockOverlay();
        }
    }


    // Candado con "Desbloquea en Piso N" centrado en el edificio; no revela nombre ni tipo.
    [Tooltip("Tinte del edificio mientras está bloqueado; en penumbra, no apagado.")]
    [SerializeField] private Color lockedTint = new Color(0.30f, 0.32f, 0.40f, 1f);

    // El candado se dimensiona a la huella real: con las ilustraciones, el recuadro de 6,8x4,3
    // se quedaba corto y dejaba medio edificio fuera del velo.
    private void FitLockOverlay()
    {
        if (lockOverlay == null) return;

        var veloTr = lockOverlay.transform.Find(VeilChildName);
        if (veloTr == null) return;

        var velo = veloTr.GetComponent<SpriteRenderer>();
        if (velo == null || velo.sprite == null) return;

        Vector2 huella = Footprint;
        Vector2 propio = velo.sprite.bounds.size;
        if (propio.x <= 0f || propio.y <= 0f) return;

        // El velo es un hijo aparte para poder escalarlo sin deformar el rótulo, que cuelga del
        // mismo padre. La niebla desborda un poco la huella: así no se ve el corte del edificio.
        float desborde = art != null ? Mathf.Max(1f, art.lockedFogOverflow) : 1f;
        veloTr.localScale = new Vector3(huella.x * desborde / propio.x,
                                        huella.y * desborde / propio.y, 1f);
    }

    private const string VeilChildName = "LockVeil";

    private void BuildLockOverlay()
    {
        var baseSr = GetComponent<SpriteRenderer>();
        if (baseSr == null) return;

        // El padre no dibuja nada: solo sostiene el velo (que se escala) y el rótulo (que no).
        var badgeGO = new GameObject("LockBadge");
        badgeGO.transform.SetParent(transform, false);
        lockOverlay = badgeGO;

        bool hayNiebla = art != null && art.lockedFog != null;

        var veilGO = new GameObject(VeilChildName);
        veilGO.transform.SetParent(badgeGO.transform, false);
        var veilSr = veilGO.AddComponent<SpriteRenderer>();
        veilSr.sprite = hayNiebla ? art.lockedFog : baseSr.sprite;
        veilSr.sortingLayerID = baseSr.sortingLayerID;
        veilSr.sortingOrder = baseSr.sortingOrder + 1;
        veilSr.color = hayNiebla ? Color.white : UITheme.GlassDeep;

        // El icono de candado era otro rectángulo redondeado de relleno: con la niebla sobra.
        if (!hayNiebla)
        {
            var iconGO = new GameObject("LockIcon");
            iconGO.transform.SetParent(badgeGO.transform, false);
            iconGO.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            iconGO.transform.localScale = Vector3.one * 0.6f;
            var iconSr = iconGO.AddComponent<SpriteRenderer>();
            iconSr.sprite = baseSr.sprite;
            iconSr.sortingLayerID = baseSr.sortingLayerID;
            iconSr.sortingOrder = veilSr.sortingOrder + 1;
            iconSr.color = UITheme.TextSoft;
        }

        var labelGO = new GameObject("LockLabel");
        labelGO.transform.SetParent(badgeGO.transform, false);
        labelGO.transform.localPosition = new Vector3(0f, -0.3f, 0f);
        lockLabel = labelGO.AddComponent<TMPro.TextMeshPro>();
        lockLabel.text = string.Format(LocalizationManager.Get("UI_QUADRANT_LOCKED_TAP"), requiredFloor);
        lockLabel.alignment = TMPro.TextAlignmentOptions.Center;
        lockLabel.enableAutoSizing = true;
        lockLabel.fontSizeMin = 2f;
        lockLabel.fontSizeMax = 2.4f;
        lockLabel.color = UITheme.Text;
        lockLabel.sortingLayerID = baseSr.sortingLayerID;
        lockLabel.sortingOrder = baseSr.sortingOrder + 3;
        lockLabel.enableWordWrapping = true;
        lockLabel.rectTransform.sizeDelta = new Vector2(3.2f, 1.2f);
    }


    // Identificador estable para el guardado: el nombre del objeto en la escena.
    public string SaveId => name;

    public event System.Action<int> LevelChanged;

    // La usa el SaveManager al cargar una partida.
    public void LoadLevel(int savedLevel)
    {
        level = Mathf.Max(1, savedLevel);
        LevelChanged?.Invoke(level);
    }

    void Awake()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = BuildingTypes.AccentColor(type);

        var arte = BuildArt();

        // El pie de la ilustración manda si la hay; si no, el borde inferior del bloque.
        SortByFoot(arte != null ? arte : sr);
    }

    // Ilustración del edificio; pública para poder montarla también desde el editor. Va en un hijo y no en el renderer de la raíz a propósito: la raíz
    // no se puede escalar sin deformar también el rótulo del nombre, y su bloque de 6,8x4,3 sigue
    // haciendo falta apagado, porque de él salen la huella (Footprint), los clics y los puestos.
    public SpriteRenderer BuildArt()
    {
        if (art == null) return null;

        var entrada = art.For(type);
        if (entrada == null) return null;

        // Antes de nada: de aquí sale la capa de ordenación. Sin resolverlo primero, el hijo
        // nacía en la capa Default y se dibujaba DEBAJO de la isla.
        if (bodySprite == null) bodySprite = GetComponent<SpriteRenderer>();

        var hijo = transform.Find(ArtChildName);
        if (hijo == null)
        {
            var go = new GameObject(ArtChildName, typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            hijo = go.transform;
        }

        artSprite = hijo.GetComponent<SpriteRenderer>();
        artSprite.sprite = entrada.sprite;
        artSprite.sortingLayerID = bodySprite != null ? bodySprite.sortingLayerID : artSprite.sortingLayerID;
        artSprite.sortingOrder = 0;

        float ancho = art.WidthFor(entrada);
        Vector2 propio = entrada.sprite.bounds.size;
        float escala = propio.x > 0f ? ancho / propio.x : 1f;
        hijo.localScale = new Vector3(escala, escala, 1f);

        // El bloque tintado se apaga, pero el componente se queda: Footprint lo lee. El marco
        // fino de alrededor se va con él: con ilustración queda un recuadro flotando.
        if (bodySprite != null) bodySprite.enabled = false;

        var marco = transform.Find("Frame");
        if (marco != null)
        {
            var marcoSr = marco.GetComponent<SpriteRenderer>();
            if (marcoSr != null) marcoSr.enabled = false;
        }

        PlaceNameLabel(artSprite.sprite.bounds.size.y * escala, ancho);
        return artSprite;
    }

    // El rótulo del nombre caía encima de la ilustración: se baja hasta justo debajo de su pie y
    // se le da el ancho del edificio, para que quepa a un tamaño legible sobre el arte.
    private void PlaceNameLabel(float alturaArte, float anchoArte)
    {
        foreach (var loc in GetComponentsInChildren<LocalizedText>(true))
        {
            var rt = loc.GetComponent<RectTransform>();
            if (rt == null) continue;

            rt.sizeDelta = new Vector2(anchoArte, nameLabelHeight);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -alturaArte * 0.5f - nameLabelGap);

            var texto = loc.GetComponent<TMPro.TMP_Text>();
            if (texto != null) texto.fontStyle |= TMPro.FontStyles.Bold;
        }
    }

    [Tooltip("Alto de la caja del rótulo del nombre; con el texto más grande hace falta sitio.")]
    [SerializeField] private float nameLabelHeight = 2.2f;

    [Tooltip("Hueco entre el pie de la ilustración y el rótulo del nombre.")]
    [SerializeField] private float nameLabelGap = 0.35f;

    private const string ArtChildName = "Building_Art";
    private SpriteRenderer artSprite;

    // Los edificios tenían orden de dibujado fijo, así que un héroe que pasaba por delante (más
    // abajo en pantalla) quedaba tapado por el bloque del edificio. Se ordenan por la misma regla
    // que los héroes, tomando como pie el borde inferior del sprite. El rótulo del nombre entra
    // en el mismo lote porque YSorter ordena Renderer, no solo SpriteRenderer.
    private void SortByFoot(SpriteRenderer sr)
    {
        if (sr == null || GetComponent<YSorter>() != null) return;

        float pie = sr.bounds.min.y - transform.position.y;
        gameObject.AddComponent<YSorter>().Configure(0, pie, false);
    }

void Start()
    {
        if (requiredFloor > 0) BuildLockOverlay();
        RefreshUnlock();

        if (IsProducer)
        {
            economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
            harvestTimer = harvestInterval;
        }
    }

    void Update()
    {
        if (!IsUnlocked) return;

        if (IsProducer && economy != null) TickProduction();
        else if (type == BuildingType.ManaWell) TickManaWell();
    }

    // Granja, Carpintería y Fundición producen solas, sin que nadie las visite.
    private void TickProduction()
    {
        harvestTimer -= Time.deltaTime;
        if (harvestTimer > 0f) return;

        harvestTimer = harvestInterval;

        // Cada trabajador asignado suma media cosecha extra.
        PruneWorkers();
        float factor = 1f + workers.Count * 0.5f;

        switch (type)
        {
            case BuildingType.Farm:
                economy.AddFood(Mathf.RoundToInt(FoodPerHarvest * factor));
                break;
            case BuildingType.WoodworkingShop:
                economy.AddMaterials(Mathf.RoundToInt(MaterialPerHarvest * factor), 0);
                break;
            case BuildingType.MetalProcessing:
                economy.AddMaterials(0, Mathf.RoundToInt(MaterialPerHarvest * factor));
                break;
        }
    }

    // El Pozo de Maná recarga a sus trabajadores fijos aunque nadie lo esté visitando ahora mismo.
    private void TickManaWell()
    {
        if (workers.Count == 0) return;

        PruneWorkers();
        foreach (var worker in workers)
            if (worker != null) worker.RestoreMP(PassiveManaPerSecond * Time.deltaTime);
    }

    void OnEnable()
    {
        all.Add(this);
        LocalizationManager.LanguageChanged += RefreshLockLabel;
    }

    void OnDisable()
    {
        all.Remove(this);
        LocalizationManager.LanguageChanged -= RefreshLockLabel;
    }

    // Releído en cada cambio de idioma; el candado se construye una sola vez en Start().
    private void RefreshLockLabel()
    {
        if (lockLabel == null) return;
        lockLabel.text = string.Format(LocalizationManager.Get("UI_QUADRANT_LOCKED_TAP"), requiredFloor);
    }

    // Huella real del edificio en unidades de mundo. Los huecos y la llegada se miden sobre
    // ella y no sobre un círculo: con el círculo los asignados paseaban fuera del bloque.
    private SpriteRenderer bodySprite;

    public Vector2 Footprint
    {
        get
        {
            // Con ilustración puesta manda su tamaño: si no, los héroes se quedaban paseando en
            // el rectángulo viejo de 6,8x4,3 mientras el edificio dibujado era mucho más grande.
            if (artSprite != null && artSprite.sprite != null)
            {
                Vector2 arte = artSprite.sprite.bounds.size;
                Vector3 escalaArte = artSprite.transform.lossyScale;
                return new Vector2(Mathf.Abs(arte.x * escalaArte.x), Mathf.Abs(arte.y * escalaArte.y));
            }

            if (bodySprite == null) bodySprite = GetComponent<SpriteRenderer>();
            if (bodySprite == null || bodySprite.sprite == null)
                return new Vector2(interactionRadius * 2f, interactionRadius * 2f);

            Vector2 tamano = bodySprite.drawMode == SpriteDrawMode.Simple
                ? (Vector2)bodySprite.sprite.bounds.size
                : bodySprite.size;

            Vector3 escala = transform.lossyScale;
            return new Vector2(Mathf.Abs(tamano.x * escala.x), Mathf.Abs(tamano.y * escala.y));
        }
    }

    public bool IsInside(Vector2 position)
    {
        Vector2 media = Footprint * 0.5f;
        Vector2 delta = position - (Vector2)transform.position;
        return Mathf.Abs(delta.x) <= media.x && Mathf.Abs(delta.y) <= media.y;
    }

    // Puntos de llegada en rejilla dentro del bloque; así dos héroes no caminan al mismo sitio
    // y ninguno se queda paseando por fuera.
    private readonly Dictionary<int, HeroController> slotOwner = new Dictionary<int, HeroController>();

    public Vector2 SlotPosition(int index)
    {
        int total = Mathf.Max(1, slotCount);
        Vector2 huella = Footprint;
        Vector2 util = new Vector2(Mathf.Max(0.5f, huella.x - slotInset * 2f),
                                   Mathf.Max(0.5f, huella.y - slotInset * 2f));

        // Columnas proporcionales a lo ancho del bloque: en uno apaisado salen más por fila.
        int columnas = Mathf.Clamp(
            Mathf.RoundToInt(Mathf.Sqrt(total * util.x / Mathf.Max(0.01f, util.y))), 1, total);
        int filas = Mathf.Max(1, Mathf.CeilToInt(total / (float)columnas));

        int i = ((index % total) + total) % total;
        int columna = i % columnas;
        int fila = i / columnas;

        float x = columnas > 1 ? (columna / (float)(columnas - 1) - 0.5f) * util.x : 0f;
        float y = filas > 1 ? (0.5f - fila / (float)(filas - 1)) * util.y : 0f;

        return (Vector2)transform.position + new Vector2(x, y);
    }

    // Reserva el hueco libre más cercano al héroe. Sin huecos devuelve un punto del anillo
    // exterior, NO el centro: caer al centro plantaba a los sobrantes justo encima del sprite
    // del edificio y lo dejaba imposible de clicar con la base llena.
    public Vector2 ClaimSlot(HeroController hero)
    {
        TryClaimSlot(hero, out Vector2 destino);
        return destino;
    }

    // Igual, pero dice si de verdad consiguió hueco: quien pueda elegir otro sitio (el paseo
    // sin rumbo) debería irse a otra parte en vez de hacer corrillo alrededor.
    public bool TryClaimSlot(HeroController hero, out Vector2 position)
    {
        if (hero == null) { position = transform.position; return false; }

        ReleaseSlot(hero);
        PruneSlots();

        int mejor = -1;
        float mejorDist = float.MaxValue;

        for (int i = 0; i < slotCount; i++)
        {
            if (slotOwner.ContainsKey(i)) continue;

            float d = ((Vector2)hero.transform.position - SlotPosition(i)).sqrMagnitude;
            if (d >= mejorDist) continue;

            mejorDist = d;
            mejor = i;
        }

        if (mejor < 0)
        {
            position = OuterRingPosition(hero);
            return false;
        }

        slotOwner[mejor] = hero;
        position = SlotPosition(mejor);
        return true;
    }

    // Anillo de espera, más ancho que los huecos y por el lado desde el que llega el héroe:
    // ni tapa el edificio ni obliga a rodearlo para quedarse esperando.
    private Vector2 OuterRingPosition(HeroController hero)
    {
        Vector2 centro = transform.position;
        Vector2 desde = (Vector2)hero.transform.position - centro;
        Vector2 direccion = desde.sqrMagnitude > 0.0001f ? desde.normalized : Vector2.up;

        Vector2 fuera = Footprint * 0.5f + Vector2.one * 1.2f;
        return centro + new Vector2(direccion.x * fuera.x, direccion.y * fuera.y);
    }

    public void ReleaseSlot(HeroController hero)
    {
        if (hero == null) return;

        int ocupado = -1;
        foreach (var par in slotOwner)
            if (par.Value == hero) { ocupado = par.Key; break; }

        if (ocupado >= 0) slotOwner.Remove(ocupado);
    }

    // Un héroe destruido dejaría su hueco bloqueado para siempre.
    private void PruneSlots()
    {
        var muertos = new List<int>();
        foreach (var par in slotOwner)
            if (par.Value == null) muertos.Add(par.Key);

        foreach (var clave in muertos) slotOwner.Remove(clave);
    }

    // Libera el hueco que tuviera este héroe en cualquier edificio de la base.
    public static void ReleaseSlotEverywhere(HeroController hero)
    {
        foreach (var building in All)
            if (building != null) building.ReleaseSlot(hero);
    }

    public float RandomVisitDuration()
        => Random.Range(visitDuration.x, visitDuration.y);

    // Aplica el efecto del edificio al héroe; devuelve true si hizo algo.
    public bool ApplyTick(HeroController hero)
    {
        if (hero == null) return false;

        switch (type)
        {
            case BuildingType.TrainingDummy:
                // Entrenar da maestría con el arma que lleve; la categoría del campo multiplica.
                hero.AddMasteryPoints(TrainingMasteryPerTick);

                var progress = hero.GetComponent<HeroProgress>();
                if (progress == null) return false;

                // EXP simbólica: el nivel se gana en la Torre. Lo que se saca aquí es maestría,
                // refinamiento y pasivas. Con afecto al máximo sube un poco.
                progress.AddEXP(Mathf.RoundToInt(TrainingExpPerTick * (1f + hero.AffinityExpBonus)));
                progress.AddSkillRefinement(skillRefinementPerTrainingTick * TrainingFactor);

                // Entrenar también despierta, mucho más despacio que pelear en la Torre; a
                // veces sale una pasiva y a veces una habilidad activa nueva.
                if (Random.value < trainingAwakeningChance * TrainingFactor)
                {
                    if (Random.value < 0.35f) ActiveSkills.TryAwaken(hero);
                    else PassiveSkills.TryAwaken(hero);
                }
                return true;

            case BuildingType.Canteen:
            case BuildingType.RestArea:
                // Descansar sube la moral aunque ya esté a tope de vida.
                hero.AddMorale(moralePerTick);

                // El glotón saca un 50% más de la cantina.
                int heal = Mathf.RoundToInt(HealPerTick * HeroTraits.HealMultiplier(hero.Trait, type));
                hero.Heal(heal);
                return true;

            case BuildingType.ManaWell:
                // Único sitio donde vuelve el maná: no se regenera solo ni con el resto de
                // edificios. Flujo de Maná rinde aquí, que es donde queda algo que multiplicar.
                hero.RestoreMP(ManaPerVisitTick * PassiveSkills.ManaRegenMultiplier(hero.Passives));
                return true;

            case BuildingType.Lodging:
                // Dormir es lo único que quita fatiga de verdad; la zona de descanso solo cura
                // y sube moral. Aquí se recupera el héroe exhausto que ya no rinde en la Torre.
                hero.RecoverFatigue(fatigueRecoveryPerTick * LevelFactor);
                hero.AddMorale(moralePerTick);
                hero.Heal(HealPerTick);
                return true;
        }

        return false;
    }

    // Comida que la granja habría cosechado con el juego cerrado; no la abona, solo la calcula.
    public int OfflineHarvest(float seconds)
        => type == BuildingType.Farm ? OfflineYield(seconds, FoodPerHarvest) : 0;

    // Lo mismo para los dos que dan material: madera la Carpintería, hierro la Fundición.
    public int OfflineWood(float seconds)
        => type == BuildingType.WoodworkingShop ? OfflineYield(seconds, MaterialPerHarvest) : 0;

    public int OfflineIron(float seconds)
        => type == BuildingType.MetalProcessing ? OfflineYield(seconds, MaterialPerHarvest) : 0;

    private int OfflineYield(float seconds, int porCosecha)
    {
        if (!IsUnlocked || harvestInterval <= 0f) return 0;

        int cosechas = Mathf.FloorToInt(seconds / harvestInterval);
        if (cosechas <= 0) return 0;

        PruneWorkers();
        return Mathf.RoundToInt(porCosecha * (1f + workers.Count * 0.5f) * cosechas);
    }

    // Descanso y maná que sus trabajadores fijos habrían recuperado estando el juego cerrado.
    // El campo de entrenamiento queda fuera a propósito: la EXP no se regala sin jugar.
    public int OfflineRecover(float seconds)
    {
        if (!IsUnlocked || seconds <= 0f) return 0;
        if (type != BuildingType.Canteen && type != BuildingType.RestArea
            && type != BuildingType.ManaWell) return 0;

        PruneWorkers();
        if (workers.Count == 0) return 0;

        // Se aplica el total de una vez en vez de simular tick a tick: son las mismas cuentas
        // y evita miles de iteraciones por cada héroe tras una noche entera fuera.
        int ticks = tickInterval > 0f ? Mathf.FloorToInt(seconds / tickInterval) : 0;

        foreach (var worker in workers)
        {
            if (worker == null) continue;

            if (type == BuildingType.ManaWell)
            {
                worker.RestoreMP(PassiveManaPerSecond * seconds);
                continue;
            }

            if (ticks <= 0) continue;

            worker.AddMorale(moralePerTick * ticks);
            worker.Heal(Mathf.RoundToInt(HealPerTick * HeroTraits.HealMultiplier(worker.Trait, type) * ticks));
        }

        return workers.Count;
    }

    // Mejora el edificio si hay materiales; sube el efecto por tick.
    public bool TryUpgrade(EconomyManager economy)
    {
        if (economy == null) return false;

        if (!CanUpgrade)
        {
            Debug.LogWarning($"[Edificio] {buildingName} está en el tope de nivel {MaxLevel}; supera el piso {NextLevelFloor} de la Torre.", this);
            return false;
        }

        int wood = NextWoodCost;
        int iron = NextIronCost;

        if (!economy.TrySpendMaterials(wood, iron))
        {
            Debug.LogWarning($"[Edificio] {buildingName} necesita {wood} madera y {iron} hierro.", this);
            return false;
        }

        level++;
        QuestManager.Report(QuestKind.UpgradeBuilding);
        Debug.Log($"[Edificio] {buildingName} mejorado a nivel {level}.", this);
        LevelChanged?.Invoke(level);

        SaveManager.RequestSave();
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = type == BuildingType.TrainingDummy ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
