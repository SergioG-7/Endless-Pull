using System.Collections.Generic;
using UnityEngine;

// Tipos de zona interactiva de la base.
public enum BuildingType
{
    TrainingDummy,
    Canteen,
    RestArea,
    Farm
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

    private float harvestTimer;
    private EconomyManager economy;

    // Registro estático: evita que cada héroe escanee la escena entera.
    private static readonly List<BaseBuilding> all = new List<BaseBuilding>();
    public static IReadOnlyList<BaseBuilding> All => all;

    public BuildingType Type => type;
    public string BuildingName => buildingName;
    public float InteractionRadius => interactionRadius;
    public float TickInterval => tickInterval;
    public int Level => level;

    public int ExpPerTick => expPerTick * level;
    public int FoodPerHarvest => foodPerHarvest * level;
    public float HarvestInterval => harvestInterval;
    public int HealPerTick => healPerTick * level;
    public int NextWoodCost => woodCostPerLevel * level;
    public int NextIronCost => ironCostPerLevel * level;

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
        if (type == BuildingType.Farm)
        {
            economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
            harvestTimer = harvestInterval;
        }
    }

    // La granja produce sola, sin que nadie la visite.
    void Update()
    {
        if (type != BuildingType.Farm || economy == null) return;

        harvestTimer -= Time.deltaTime;
        if (harvestTimer > 0f) return;

        harvestTimer = harvestInterval;
        economy.AddFood(FoodPerHarvest);
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
