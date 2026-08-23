using System.Collections.Generic;
using UnityEngine;

// Tipos de zona interactiva de la base.
public enum BuildingType
{
    TrainingDummy,
    Canteen,
    RestArea
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

    [Tooltip("Vida por tick en cantina y zona de descanso, en nivel 1.")]
    [SerializeField] private int healPerTick = 6;

    [Tooltip("Segundos que se queda un héroe en cada visita.")]
    [SerializeField] private Vector2 visitDuration = new Vector2(8f, 12f);

    [Tooltip("Nivel del edificio; escala el efecto por tick.")]
    [SerializeField] private int level = 1;

    [Tooltip("Madera que cuesta la siguiente mejora, por nivel actual.")]
    [SerializeField] private int woodCostPerLevel = 20;

    [Tooltip("Hierro que cuesta la siguiente mejora, por nivel actual.")]
    [SerializeField] private int ironCostPerLevel = 10;

    // Registro estático: evita que cada héroe escanee la escena entera.
    private static readonly List<BaseBuilding> all = new List<BaseBuilding>();
    public static IReadOnlyList<BaseBuilding> All => all;

    public BuildingType Type => type;
    public string BuildingName => buildingName;
    public float InteractionRadius => interactionRadius;
    public float TickInterval => tickInterval;
    public int Level => level;

    public int ExpPerTick => expPerTick * level;
    public int HealPerTick => healPerTick * level;
    public int NextWoodCost => woodCostPerLevel * level;
    public int NextIronCost => ironCostPerLevel * level;

    public event System.Action<int> LevelChanged;

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
                var progress = hero.GetComponent<HeroProgress>();
                if (progress == null) return false;
                progress.AddEXP(ExpPerTick);
                return true;

            case BuildingType.Canteen:
            case BuildingType.RestArea:
                // El glotón saca un 50% más de la cantina.
                int heal = Mathf.RoundToInt(HealPerTick * HeroTraits.HealMultiplier(hero.Trait, type));
                return hero.Heal(heal) > 0;
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
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = type == BuildingType.TrainingDummy ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
