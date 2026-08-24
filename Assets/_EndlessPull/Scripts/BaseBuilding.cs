using System.Collections.Generic;
using UnityEngine;

// Tipos de zona interactiva de la base.
public enum BuildingType
{
    TrainingDummy,
    Canteen,
    RestArea,
    Farm,
    Workshop
}

// Nombres visibles de los tipos de edificio.
public static class BuildingTypes
{
    public static string DisplayName(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.TrainingDummy: return "Campo de Entrenamiento";
            case BuildingType.Canteen: return "Cantina";
            case BuildingType.RestArea: return "Zona de Descanso";
            case BuildingType.Farm: return "Granja";
            case BuildingType.Workshop: return "Taller";
        }
        return type.ToString();
    }
}

public class BaseBuilding : MonoBehaviour
{
    [Tooltip("Qué hace el edificio con el héroe que lo visita.")]
    [SerializeField] private BuildingType type = BuildingType.TrainingDummy;

    [Tooltip("Nombre visible del edificio.")]
    [SerializeField] private string buildingName = "Campo de Entrenamiento";

    [Tooltip("Radio en el que el héroe se considera dentro del edificio.")]
    [SerializeField] private float interactionRadius = 1.5f;

    [Tooltip("Segundos entre cada efecto aplicado al héroe.")]
    [SerializeField] private float tickInterval = 2f;

    [Tooltip("EXP por tick en el campo de entrenamiento, en nivel 1.")]
    [SerializeField] private int expPerTick = 5;

    [Tooltip("Puntos de maestría de arma por tick de entrenamiento.")]
    [SerializeField] private int masteryPerTrainingTick = 2;

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

    [Tooltip("Piso de torre a partir del cual existe este edificio; 0 = desde el principio.")]
    [SerializeField] private int requiredFloor;

    [Tooltip("Producción extra por nivel, en tanto por uno acumulativo.")]
    [SerializeField] private float extraPerLevel = 0.15f;

    [Tooltip("Tope de ocupantes por edificio, por muy alto que sea el nivel.")]
    [SerializeField] private int capacityCap = 4;

    private float harvestTimer;
    private EconomyManager economy;

    // Trabajadores fijos asignados a mano desde la ficha del edificio.
    private readonly List<HeroController> workers = new List<HeroController>();

    // Registro estático: evita que cada héroe escanee la escena entera.
    private static readonly List<BaseBuilding> all = new List<BaseBuilding>();
    public static IReadOnlyList<BaseBuilding> All => all;

    public BuildingType Type => type;
    public string BuildingName => buildingName;
    public float InteractionRadius => interactionRadius;
    public float TickInterval => tickInterval;
    public int Level => level;

    // El nivel no suma lineal: cada nivel rinde algo más que el anterior.
    public float LevelFactor => level * (1f + extraPerLevel * (level - 1));

    public int ExpPerTick => Mathf.RoundToInt(expPerTick * LevelFactor);
    public int FoodPerHarvest => Mathf.RoundToInt(foodPerHarvest * LevelFactor);
    public float HarvestInterval => harvestInterval;
    public int HealPerTick => Mathf.RoundToInt(healPerTick * LevelFactor);
    public float MoralePerTick => moralePerTick;
    public int NextWoodCost => woodCostPerLevel * level;
    public int NextIronCost => ironCostPerLevel * level;

    public int RequiredFloor => requiredFloor;
    public bool IsUnlocked => TowerFloor >= requiredFloor;

    // Piso actual de la torre; lo publica el WaveManager para no consultarlo por edificio.
    public static int TowerFloor { get; private set; } = 1;

    public static void SetTowerFloor(int floor)
    {
        TowerFloor = Mathf.Max(1, floor);

        // Los edificios que aún no tocan se apagan; los que ya tocan aparecen.
        foreach (var b in all)
            if (b != null) b.RefreshUnlock();
    }

    // A partir del piso 5 cada instalación admite el doble de gente.
    public static int FloorCapacity => TowerFloor >= 5 ? 2 : 1;

    public int Capacity => Mathf.Min(capacityCap, FloorCapacity + (level - 1));

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
    public bool ToggleWorker(HeroController hero)
    {
        if (hero == null) return false;

        if (workers.Remove(hero))
        {
            hero.SetAssignedBuilding(null);
            LevelChanged?.Invoke(level);
            SaveManager.RequestSave();
            return false;
        }

        if (workers.Count >= Capacity)
        {
            Debug.LogWarning($"[Edificio] {buildingName} está al completo ({workers.Count}/{Capacity}).", this);
            return false;
        }

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
    {
        if (hero == null) return -1;

        var party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();
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
        // Se apaga el renderer y el rótulo, pero el componente sigue vivo para el guardado.
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = IsUnlocked;
        foreach (var t in GetComponentsInChildren<TMPro.TextMeshPro>(true)) t.enabled = IsUnlocked;
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

    void Start()
    {
        RefreshUnlock();

        if (type == BuildingType.Farm)
        {
            economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
            harvestTimer = harvestInterval;
        }
    }

    // La granja produce sola, sin que nadie la visite.
    void Update()
    {
        if (type != BuildingType.Farm || economy == null || !IsUnlocked) return;

        harvestTimer -= Time.deltaTime;
        if (harvestTimer > 0f) return;

        harvestTimer = harvestInterval;

        // Cada trabajador asignado suma media cosecha extra.
        PruneWorkers();
        float factor = 1f + workers.Count * 0.5f;
        economy.AddFood(Mathf.RoundToInt(FoodPerHarvest * factor));
    }

    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    public bool IsInside(Vector2 position)
        => ((Vector2)transform.position - position).sqrMagnitude <= interactionRadius * interactionRadius;

    public float RandomVisitDuration()
        => Random.Range(visitDuration.x, visitDuration.y);

    // Aplica el efecto del edificio al héroe; devuelve true si hizo algo.
    public bool ApplyTick(HeroController hero)
    {
        if (hero == null) return false;

        switch (type)
        {
            case BuildingType.TrainingDummy:
                // Entrenar da EXP y, si lleva arma, maestría con ese tipo.
                hero.AddMasteryPoints(masteryPerTrainingTick);

                var progress = hero.GetComponent<HeroProgress>();
                if (progress == null) return false;
                progress.AddEXP(ExpPerTick);
                return true;

            case BuildingType.Canteen:
            case BuildingType.RestArea:
                // Descansar sube la moral aunque ya esté a tope de vida.
                hero.AddMorale(moralePerTick);

                // El glotón saca un 50% más de la cantina.
                int heal = Mathf.RoundToInt(HealPerTick * HeroTraits.HealMultiplier(hero.Trait, type));
                hero.Heal(heal);
                return true;
        }

        return false;
    }

    // Mejora el edificio si hay materiales; sube el efecto por tick.
    public bool TryUpgrade(EconomyManager economy)
    {
        if (economy == null) return false;

        int wood = NextWoodCost;
        int iron = NextIronCost;

        if (!economy.TrySpendMaterials(wood, iron))
        {
            Debug.LogWarning($"[Edificio] {buildingName} necesita {wood} madera y {iron} hierro.", this);
            return false;
        }

        level++;
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
