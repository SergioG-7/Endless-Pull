using System.Collections.Generic;
using UnityEngine;

// Taller de Alquimia: forja piedras y armas a cambio de materiales. Las gemas no entran aquí.
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

    [Tooltip("Armas que sabe fabricar el taller; sale una al azar de la lista.")]
    [SerializeField] private List<EquipmentData> craftableWeapons = new List<EquipmentData>();

    [Tooltip("Madera que cuesta fabricar un arma.")]
    [SerializeField] private int weaponWoodCost = 60;

    [Tooltip("Hierro que cuesta fabricar un arma.")]
    [SerializeField] private int weaponIronCost = 45;

    [Tooltip("Comida que consume la cuadrilla mientras fabrica un arma.")]
    [SerializeField] private int weaponFoodCost = 20;

    [Tooltip("Rebaja de materiales por cada artesano asignado al Taller, en tanto por uno.")]
    [SerializeField] private float artisanDiscount = 0.05f;

    [Tooltip("Rebaja extra por cada nivel acumulado entre los artesanos.")]
    [SerializeField] private float artisanLevelDiscount = 0.005f;

    [Tooltip("Probabilidad extra de forja por cada artesano asignado al Taller.")]
    [SerializeField] private float artisanSuccessBonus = 0.03f;

    [Tooltip("Almacén al que van las armas recién fabricadas.")]
    [SerializeField] private ShopManager shop;

    [Tooltip("Economía de la que salen los materiales.")]
    [SerializeField] private EconomyManager economy;

    private int ascensionStones;

    public int AscensionStones => ascensionStones;
    public float SuccessChance => successChance;

    // Los costes que se cobran de verdad ya llevan la rebaja de los artesanos.
    public int WoodCost => Discounted(woodCost);
    public int IronCost => Discounted(ironCost);
    public int WeaponWoodCost => Discounted(weaponWoodCost);
    public int WeaponIronCost => Discounted(weaponIronCost);
    public int WeaponFoodCost => Discounted(weaponFoodCost);

    // Héroes asignados a un Taller; de ahí salen la rebaja y la probabilidad extra.
    public int Artisans
    {
        get
        {
            int total = 0;
            foreach (var b in BaseBuilding.All)
                if (b != null && b.Type == BuildingType.Workshop) total += b.Workers.Count;

            return total;
        }
    }

    // Suma de niveles de los artesanos: un veterano rebaja más que un recluta.
    public int ArtisanSkill
    {
        get
        {
            int total = 0;
            foreach (var b in BaseBuilding.All)
            {
                if (b == null || b.Type != BuildingType.Workshop) continue;

                foreach (var hero in b.Workers)
                {
                    if (hero == null) continue;

                    var progress = hero.GetComponent<HeroProgress>();
                    total += progress != null ? progress.Level : 1;
                }
            }

            return total;
        }
    }

    // Nunca baja del 50%: por muchos artesanos que haya, forjar sigue costando.
    public float CostFactor => Mathf.Clamp(
        1f - artisanDiscount * Artisans - artisanLevelDiscount * ArtisanSkill, 0.5f, 1f);

    private int Discounted(int baseCost) => Mathf.CeilToInt(baseCost * CostFactor);

    // Un Taller mejorado y con artesanos dentro mejora la probabilidad de forja.
    public float EffectiveSuccessChance
    {
        get
        {
            float bonus = 0f;
            foreach (var b in BaseBuilding.All)
                if (b != null && b.IsUnlocked && b.Type == BuildingType.Workshop)
                    bonus += 0.05f * b.Level;

            return Mathf.Clamp01(successChance + bonus + artisanSuccessBonus * Artisans);
        }
    }

    public bool CanAttemptCraft => economy != null && economy.CanAffordMaterials(WoodCost, IronCost);

    public bool CanCraftWeapon => economy != null
        && craftableWeapons != null && craftableWeapons.Count > 0
        && economy.CanAffordMaterials(WeaponWoodCost, WeaponIronCost)
        && economy.CanAffordFood(WeaponFoodCost);

    // Se dispara con (éxito, mensaje) tras cada intento.
    public event System.Action<bool, string> CraftResolved;

    // Se dispara con el número de piedras cada vez que cambia.
    public event System.Action<int> StonesChanged;

    void Awake()
    {
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        if (shop == null) shop = UnityEngine.Object.FindFirstObjectByType<ShopManager>();
    }

    // Los materiales se cobran siempre; solo el resultado va a suerte. Las gemas no entran.
    public bool TryCraftAscensionStone()
    {
        int madera = WoodCost;
        int hierro = IronCost;

        if (economy == null || !economy.TrySpendMaterials(madera, hierro))
        {
            Debug.LogWarning($"[Taller] Hacen falta {madera} madera y {hierro} hierro.", this);
            CraftResolved?.Invoke(false, LocalizationManager.Get("UI_NO_MATERIALS"));
            return false;
        }

        bool success = Random.value < EffectiveSuccessChance;

        if (success)
        {
            ascensionStones++;
            StonesChanged?.Invoke(ascensionStones);

            Debug.Log($"[Taller] Piedra de Ascensión forjada. Tienes {ascensionStones}.", this);
            CraftResolved?.Invoke(true, LocalizationManager.Get("UI_STONE_FORGED"));
        }
        else
        {
            Debug.LogWarning($"[Taller] Crafteo fallido: se pierden {madera} madera y {hierro} hierro.", this);
            CraftResolved?.Invoke(false, LocalizationManager.Get("UI_CRAFT_FAILED"));
        }

        SaveManager.RequestSave();
        return success;
    }

    // Fabricar un arma no falla: cuesta madera, hierro y comida, y la pieza va al almacén.
    public EquipmentData TryCraftWeapon()
    {
        if (craftableWeapons == null || craftableWeapons.Count == 0)
        {
            CraftResolved?.Invoke(false, LocalizationManager.Get("UI_NO_RECIPES"));
            return null;
        }

        int madera = WeaponWoodCost;
        int hierro = WeaponIronCost;
        int comida = WeaponFoodCost;

        if (economy == null || !economy.CanAffordMaterials(madera, hierro)
            || !economy.CanAffordFood(comida))
        {
            Debug.LogWarning($"[Taller] Fabricar cuesta {madera} madera, {hierro} hierro " +
                             $"y {comida} comida.", this);
            CraftResolved?.Invoke(false, LocalizationManager.Get("UI_NO_MATERIALS"));
            return null;
        }

        // Los tres recursos se cobran juntos: la comida solo tras asegurar madera y hierro.
        if (!economy.TrySpendMaterials(madera, hierro)) return null;
        economy.TrySpendFood(comida);

        var pieza = craftableWeapons[Random.Range(0, craftableWeapons.Count)];
        if (shop != null) shop.AddToInventory(pieza);

        Debug.Log($"[Taller] Fabricada {pieza.ShortLabel()} por {madera} madera, " +
                  $"{hierro} hierro y {comida} comida.", this);
        CraftResolved?.Invoke(true, pieza.equipName);

        SaveManager.RequestSave();
        return pieza;
    }

    public int RepairWoodCost => Discounted(repairWoodCost);
    public int RepairIronCost => Discounted(repairIronCost);

    public bool CanAffordRepair => economy != null && economy.CanAffordMaterials(RepairWoodCost, RepairIronCost);

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

        if (economy == null || !economy.TrySpendMaterials(RepairWoodCost, RepairIronCost))
        {
            Debug.LogWarning($"[Taller] Reparar cuesta {RepairWoodCost} madera y {RepairIronCost} hierro.", this);
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
    public int RepairAllWoodCost() => Discounted(Mathf.CeilToInt(TotalWear() * repairWoodCostPerPoint));
    public int RepairAllIronCost() => Discounted(Mathf.CeilToInt(TotalWear() * repairIronCostPerPoint));

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
