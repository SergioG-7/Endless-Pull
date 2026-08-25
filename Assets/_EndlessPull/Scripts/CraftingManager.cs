using UnityEngine;

// Taller: convierte materiales en Piedras de Ascensión, con riesgo de perderlos.
public class CraftingManager : MonoBehaviour
{
    [Tooltip("Madera que cuesta cada intento de crafteo.")]
    [SerializeField] private int woodCost = 40;

    [Tooltip("Hierro que cuesta cada intento de crafteo.")]
    [SerializeField] private int ironCost = 40;

    [Tooltip("Probabilidad de que el intento salga bien.")]
    [Range(0f, 1f)]
    [SerializeField] private float successChance = 0.7f;

    [Tooltip("Madera que cuesta reparar una pieza rota.")]
    [SerializeField] private int repairWoodCost = 15;

    [Tooltip("Hierro que cuesta reparar una pieza rota.")]
    [SerializeField] private int repairIronCost = 15;

    [Tooltip("Madera por cada punto de durabilidad perdido al reparar todo.")]
    [SerializeField] private float repairWoodCostPerPoint = 2f;

    [Tooltip("Hierro por cada punto de durabilidad perdido al reparar todo.")]
    [SerializeField] private float repairIronCostPerPoint = 2f;

    [Tooltip("Economía de la que salen los materiales.")]
    [SerializeField] private EconomyManager economy;

    private int ascensionStones;

    public int AscensionStones => ascensionStones;
    public int WoodCost => woodCost;
    public int IronCost => ironCost;
    public float SuccessChance => successChance;

    // Un Taller construido y mejorado en la base mejora la probabilidad de forja.
    public float EffectiveSuccessChance
    {
        get
        {
            float bonus = 0f;
            foreach (var b in BaseBuilding.All)
                if (b != null && b.IsUnlocked && b.Type == BuildingType.Workshop)
                    bonus += 0.05f * b.Level;

            return Mathf.Clamp01(successChance + bonus);
        }
    }

    public bool CanAttemptCraft => economy != null && economy.CanAffordMaterials(woodCost, ironCost);

    // Se dispara con (éxito, mensaje) tras cada intento.
    public event System.Action<bool, string> CraftResolved;

    // Se dispara con el número de piedras cada vez que cambia.
    public event System.Action<int> StonesChanged;

    void Awake()
    {
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
    }

    // Los materiales se cobran siempre; solo el resultado va a suerte.
    public bool TryCraftAscensionStone()
    {
        if (economy == null || !economy.TrySpendMaterials(woodCost, ironCost))
        {
            Debug.LogWarning($"[Taller] Hacen falta {woodCost} madera y {ironCost} hierro.", this);
            CraftResolved?.Invoke(false, "Faltan materiales");
            return false;
        }

        bool success = Random.value < EffectiveSuccessChance;

        if (success)
        {
            ascensionStones++;
            StonesChanged?.Invoke(ascensionStones);

            Debug.Log($"[Taller] Piedra de Ascensión forjada. Tienes {ascensionStones}.", this);
            CraftResolved?.Invoke(true, "¡Piedra forjada!");
        }
        else
        {
            Debug.LogWarning($"[Taller] Crafteo fallido: se pierden {woodCost} madera y {ironCost} hierro.", this);
            CraftResolved?.Invoke(false, "¡FALLO DE CRAFTEO!");
        }

        SaveManager.RequestSave();
        return success;
    }

    public int RepairWoodCost => repairWoodCost;
    public int RepairIronCost => repairIronCost;

    public bool CanAffordRepair => economy != null && economy.CanAffordMaterials(repairWoodCost, repairIronCost);

    // Repara la primera pieza rota del héroe; la reparación no falla, solo cuesta.
    public bool TryRepair(HeroController hero)
    {
        if (hero == null) return false;

        var slot = hero.FirstBrokenSlot();
        if (slot == null)
        {
            CraftResolved?.Invoke(false, "No hay nada roto");
            return false;
        }

        if (economy == null || !economy.TrySpendMaterials(repairWoodCost, repairIronCost))
        {
            Debug.LogWarning($"[Taller] Reparar cuesta {repairWoodCost} madera y {repairIronCost} hierro.", this);
            CraftResolved?.Invoke(false, "Faltan materiales para reparar");
            return false;
        }

        var pieza = hero.GetEquipped(slot.Value);
        hero.RepairSlot(slot.Value);

        QuestManager.Report(QuestKind.RepairGear);
        Debug.Log($"[Taller] {pieza.equipName} de {hero.Data.heroName} reparada " +
                  $"({hero.DurabilityOf(slot.Value)}/{pieza.maxDurability}).", this);
        CraftResolved?.Invoke(true, $"{pieza.equipName} reparada");

        SaveManager.RequestSave();
        return true;
    }

    // Puntos de durabilidad que le faltan a todo el equipo de todos los héroes.
    public int TotalWear()
    {
        int total = 0;

        foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
        {
            if (hero == null) continue;

            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                var item = hero.GetEquipped(slot);
                if (item == null) continue;

                total += Mathf.Max(0, item.maxDurability - hero.DurabilityOf(slot));
            }
        }

        return total;
    }

    // El coste sale del desgaste acumulado, redondeando hacia arriba por punto perdido.
    public int RepairAllWoodCost() => Mathf.CeilToInt(TotalWear() * repairWoodCostPerPoint);
    public int RepairAllIronCost() => Mathf.CeilToInt(TotalWear() * repairIronCostPerPoint);

    // Deja como nuevo todo el equipo de la base de una sola vez.
    public bool TryRepairAll()
    {
        int desgaste = TotalWear();
        if (desgaste <= 0)
        {
            CraftResolved?.Invoke(false, LocalizationManager.Get("UI_NOTHING_BROKEN"));
            return false;
        }

        int madera = RepairAllWoodCost();
        int hierro = RepairAllIronCost();

        if (economy == null || !economy.TrySpendMaterials(madera, hierro))
        {
            Debug.LogWarning($"[Taller] Reparar todo cuesta {madera} madera y {hierro} hierro.", this);
            CraftResolved?.Invoke(false, "Faltan materiales para reparar");
            return false;
        }

        int piezas = 0;
        foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
        {
            if (hero == null) continue;

            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
                if (hero.RepairSlot(slot)) piezas++;
        }

        QuestManager.Report(QuestKind.RepairGear, piezas);
        Debug.Log($"[Taller] Reparadas {piezas} pieza(s) por {madera} madera y {hierro} hierro.", this);
        CraftResolved?.Invoke(true, $"{piezas} pieza(s) reparada(s)");

        SaveManager.RequestSave();
        return true;
    }

    public bool HasStone => ascensionStones > 0;

    // La consume la ascensión de un héroe.
    public bool TryConsumeStone()
    {
        if (ascensionStones <= 0) return false;

        ascensionStones--;
        StonesChanged?.Invoke(ascensionStones);
        return true;
    }

    public void AddStones(int amount)
    {
        if (amount <= 0) return;

        ascensionStones += amount;
        StonesChanged?.Invoke(ascensionStones);
    }

    // La usa el SaveManager al cargar.
    public void LoadStones(int saved)
    {
        ascensionStones = Mathf.Max(0, saved);
        StonesChanged?.Invoke(ascensionStones);
    }
}
