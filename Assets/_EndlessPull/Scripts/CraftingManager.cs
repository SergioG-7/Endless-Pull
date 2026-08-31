using System.Collections.Generic;
using UnityEngine;

// Piedra de Ascensión por tier: Menor (1★→2★) hasta Legendaria (4★→5★).
public enum AscensionStoneTier
{
    Menor,
    Media,
    Mayor,
    Legendaria
}

public static class AscensionStoneTiers
{
    public static string DisplayName(AscensionStoneTier tier)
    {
        switch (tier)
        {
            case AscensionStoneTier.Menor: return LocalizationManager.Get("UI_STONE_MENOR");
            case AscensionStoneTier.Media: return LocalizationManager.Get("UI_STONE_MEDIA");
            case AscensionStoneTier.Mayor: return LocalizationManager.Get("UI_STONE_MAYOR");
            case AscensionStoneTier.Legendaria: return LocalizationManager.Get("UI_STONE_LEGENDARIA");
        }
        return tier.ToString();
    }
}

// Taller de Alquimia: forja piedras y armas a cambio de materiales. Las gemas no entran aquí.
public class CraftingManager : MonoBehaviour
{
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

    [Tooltip("Comida que cuesta fabricar una poción de curación instantánea.")]
    [SerializeField] private int potionFoodCost = 20;

    [Tooltip("Madera que cuesta fabricar una poción de curación instantánea.")]
    [SerializeField] private int potionWoodCost = 10;

    [Tooltip("Madera que cuesta una mejora de equipo básico.")]
    [SerializeField] private int upgradeWoodCost = 50;

    [Tooltip("Hierro que cuesta una mejora de equipo básico.")]
    [SerializeField] private int upgradeIronCost = 50;

    [Tooltip("Comida que cuesta una mejora de equipo básico.")]
    [SerializeField] private int upgradeFoodCost = 30;

    [Tooltip("Ataque plano que suma cada mejora de equipo básico.")]
    [SerializeField] private int upgradeAttackBonus = 3;

    [Tooltip("Defensa plana que suma cada mejora de equipo básico.")]
    [SerializeField] private int upgradeDefenseBonus = 2;

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

    [Tooltip("Madera que cuesta forjar cada tier de Piedra: Menor, Media, Mayor, Legendaria.")]
    [SerializeField] private int[] stoneWoodCostByTier = { 30, 60, 120, 220 };

    [Tooltip("Hierro que cuesta forjar cada tier de Piedra: Menor, Media, Mayor, Legendaria.")]
    [SerializeField] private int[] stoneIronCostByTier = { 15, 35, 70, 140 };

    private readonly int[] stoneCounts = new int[4];
    private int healingPotions;

    public int StoneCount(AscensionStoneTier tier) => stoneCounts[(int)tier];
    public int[] StoneCounts => stoneCounts;
    public int HealingPotions => healingPotions;
    public float SuccessChance => successChance;

    // Los costes que se cobran de verdad ya llevan la rebaja de los artesanos.
    public int WeaponWoodCost => Discounted(weaponWoodCost);
    public int WeaponIronCost => Discounted(weaponIronCost);
    public int WeaponFoodCost => Discounted(weaponFoodCost);
    public int PotionWoodCost => Discounted(potionWoodCost);
    public int PotionFoodCost => Discounted(potionFoodCost);
    public int UpgradeWoodCost => ForgeDiscounted(upgradeWoodCost);
    public int UpgradeIronCost => ForgeDiscounted(upgradeIronCost);
    public int UpgradeFoodCost => ForgeDiscounted(upgradeFoodCost);

    // La Forja es la que desbloquea la mejora de equipo del roster; sin ella construida, no se puede mejorar.
    public bool ForgeUnlocked
    {
        get
        {
            foreach (var b in BaseBuilding.All)
                if (b != null && b.IsUnlocked && b.Type == BuildingType.Forge) return true;

            return false;
        }
    }

    // Rebaja propia de la Forja sobre el coste de mejora de equipo, aparte del descuento de artesanos.
    public float ForgeDiscount
    {
        get
        {
            float bonus = 0f;
            foreach (var b in BaseBuilding.All)
                if (b != null && b.IsUnlocked && b.Type == BuildingType.Forge) bonus += 0.05f * b.Level;

            return Mathf.Clamp01(bonus);
        }
    }

    private int ForgeDiscounted(int baseCost) => Mathf.CeilToInt(Discounted(baseCost) * (1f - ForgeDiscount));

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

    public int StoneWoodCost(AscensionStoneTier tier) => Discounted(stoneWoodCostByTier[(int)tier]);
    public int StoneIronCost(AscensionStoneTier tier) => Discounted(stoneIronCostByTier[(int)tier]);

    public bool CanCraftStone(AscensionStoneTier tier) => economy != null
        && economy.CanAffordMaterials(StoneWoodCost(tier), StoneIronCost(tier));

    public bool CanCraftWeapon => economy != null
        && craftableWeapons != null && craftableWeapons.Count > 0
        && economy.CanAffordMaterials(WeaponWoodCost, WeaponIronCost)
        && economy.CanAffordFood(WeaponFoodCost);

    public bool CanCraftPotion => economy != null
        && economy.CanAffordMaterials(PotionWoodCost, 0) && economy.CanAffordFood(PotionFoodCost);

    public bool CanUpgradeGear => ForgeUnlocked && economy != null
        && economy.CanAffordMaterials(UpgradeWoodCost, UpgradeIronCost)
        && economy.CanAffordFood(UpgradeFoodCost);

    // Se dispara con (éxito, mensaje) tras cada intento.
    public event System.Action<bool, string> CraftResolved;

    // Se dispara con el tier y el número de piedras de ese tier cada vez que cambia.
    public event System.Action<AscensionStoneTier, int> StonesChanged;

    // Se dispara con el número de pociones cada vez que cambia.
    public event System.Action<int> PotionsChanged;

    void Awake()
    {
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        if (shop == null) shop = UnityEngine.Object.FindFirstObjectByType<ShopManager>();

        CraftResolved += OnCraftResolved;
    }

    void OnDestroy() => CraftResolved -= OnCraftResolved;

    // Chispazo de partículas en el Taller cada vez que un crafteo sale bien (piedra, arma, poción, reparación...).
    private void OnCraftResolved(bool success, string message)
    {
        if (success) VfxManager.Play(VfxId.CraftSuccess, WorkshopPosition());
    }

    // Posición del primer Taller construido; sin ninguno en la base cae al origen del mundo.
    private Vector3 WorkshopPosition()
    {
        foreach (var b in BaseBuilding.All)
            if (b != null && b.Type == BuildingType.Workshop) return b.transform.position;

        return Vector3.zero;
    }

    // Los materiales se cobran siempre; solo el resultado va a suerte. Las gemas no entran.
    public bool TryCraftStone(AscensionStoneTier tier)
    {
        int madera = StoneWoodCost(tier);
        int hierro = StoneIronCost(tier);

        if (economy == null || !economy.TrySpendMaterials(madera, hierro))
        {
            Debug.LogWarning($"[Taller] Hacen falta {madera} madera y {hierro} hierro.", this);
            CraftResolved?.Invoke(false, LocalizationManager.Get("UI_NO_MATERIALS"));
            return false;
        }

        bool success = Random.value < EffectiveSuccessChance;

        if (success)
        {
            stoneCounts[(int)tier]++;
            StonesChanged?.Invoke(tier, stoneCounts[(int)tier]);

            Debug.Log($"[Taller] Piedra {tier} forjada. Tienes {stoneCounts[(int)tier]}.", this);
            CraftResolved?.Invoke(true, LocalizationManager.Get("UI_STONE_FORGED"));
            AudioManager.Play(SfxId.CraftSuccess);
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

    // La comida se cobra siempre que hay madera; fabricar una poción no falla.
    public bool TryCraftPotion()
    {
        int madera = PotionWoodCost;
        int comida = PotionFoodCost;

        if (economy == null || !economy.CanAffordMaterials(madera, 0) || !economy.CanAffordFood(comida))
        {
            Debug.LogWarning($"[Taller] Poción: hacen falta {madera} madera y {comida} comida.", this);
            CraftResolved?.Invoke(false, LocalizationManager.Get("UI_NO_MATERIALS"));
            return false;
        }

        economy.TrySpendMaterials(madera, 0);
        economy.TrySpendFood(comida);

        healingPotions++;
        PotionsChanged?.Invoke(healingPotions);

        Debug.Log($"[Taller] Poción de curación fabricada. Tienes {healingPotions}.", this);
        CraftResolved?.Invoke(true, LocalizationManager.Get("UI_POTION_CRAFTED"));

        SaveManager.RequestSave();
        return true;
    }

    // Cura al héroe al máximo y consume una poción del almacén.
    public bool TryUseHealingPotion(HeroController target)
    {
        if (target == null || healingPotions <= 0) return false;

        target.Heal(target.MaxHealth);

        healingPotions--;
        PotionsChanged?.Invoke(healingPotions);

        SaveManager.RequestSave();
        return true;
    }

    // Mejora a todo el roster de una vez: bonus plano de ataque/defensa a coste fijo,
    // así la tarjeta del Taller no necesita un selector de héroe.
    public bool TryUpgradeAllGear()
    {
        if (!ForgeUnlocked)
        {
            Debug.LogWarning("[Forja] Aún no está construida.", this);
            CraftResolved?.Invoke(false, LocalizationManager.Get("UI_FORGE_LOCKED"));
            return false;
        }

        int madera = UpgradeWoodCost;
        int hierro = UpgradeIronCost;
        int comida = UpgradeFoodCost;

        if (economy == null || !economy.CanAffordMaterials(madera, hierro) || !economy.CanAffordFood(comida))
        {
            Debug.LogWarning($"[Taller] Mejora: hacen falta {madera} madera, {hierro} hierro y {comida} comida.", this);
            CraftResolved?.Invoke(false, LocalizationManager.Get("UI_NO_MATERIALS"));
            return false;
        }

        economy.TrySpendMaterials(madera, hierro);
        economy.TrySpendFood(comida);

        int heroes = 0;
        foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
        {
            if (hero == null) continue;

            hero.ApplyGearUpgrade(upgradeAttackBonus, upgradeDefenseBonus);
            heroes++;
        }

        Debug.Log($"[Taller] Equipo mejorado para {heroes} héroe(s) (+{upgradeAttackBonus} ATQ, " +
                  $"+{upgradeDefenseBonus} DEF).", this);
        CraftResolved?.Invoke(true, LocalizationManager.Get("UI_ALL_GEAR_UPGRADED"));

        SaveManager.RequestSave();
        return true;
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
            CraftResolved?.Invoke(false, LocalizationManager.Get("UI_NOTHING_BROKEN"));
            return false;
        }

        if (economy == null || !economy.TrySpendMaterials(RepairWoodCost, RepairIronCost))
        {
            Debug.LogWarning($"[Taller] Reparar cuesta {RepairWoodCost} madera y {RepairIronCost} hierro.", this);
            CraftResolved?.Invoke(false, LocalizationManager.Get("UI_NO_REPAIR_MATERIALS"));
            return false;
        }

        var pieza = hero.GetEquipped(slot.Value);
        hero.RepairSlot(slot.Value);

        QuestManager.Report(QuestKind.RepairGear);
        Debug.Log($"[Taller] {pieza.equipName} de {hero.Data.heroName} reparada " +
                  $"({hero.DurabilityOf(slot.Value)}/{pieza.maxDurability}).", this);
        CraftResolved?.Invoke(true, string.Format(LocalizationManager.Get("UI_PIECE_REPAIRED"), pieza.equipName));

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
            CraftResolved?.Invoke(false, LocalizationManager.Get("UI_NO_REPAIR_MATERIALS"));
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
        CraftResolved?.Invoke(true, string.Format(LocalizationManager.Get("UI_PIECES_REPAIRED"), piezas));

        SaveManager.RequestSave();
        return true;
    }

    public bool HasStone(AscensionStoneTier tier) => stoneCounts[(int)tier] > 0;

    // La consume la ascensión de un héroe.
    public bool TryConsumeStone(AscensionStoneTier tier)
    {
        if (stoneCounts[(int)tier] <= 0) return false;

        stoneCounts[(int)tier]--;
        StonesChanged?.Invoke(tier, stoneCounts[(int)tier]);
        return true;
    }

    public void AddStones(AscensionStoneTier tier, int amount)
    {
        if (amount <= 0) return;

        stoneCounts[(int)tier] += amount;
        StonesChanged?.Invoke(tier, stoneCounts[(int)tier]);
    }

    // La usa el SaveManager al cargar. `savedMenor` absorbe también el zurrón genérico de saves
    // anteriores a los tiers (ver SaveManager.Load), así no se pierde progreso ya guardado.
    public void LoadStones(int savedMenor, int savedMedia, int savedMayor, int savedLegendaria)
    {
        stoneCounts[(int)AscensionStoneTier.Menor] = Mathf.Max(0, savedMenor);
        stoneCounts[(int)AscensionStoneTier.Media] = Mathf.Max(0, savedMedia);
        stoneCounts[(int)AscensionStoneTier.Mayor] = Mathf.Max(0, savedMayor);
        stoneCounts[(int)AscensionStoneTier.Legendaria] = Mathf.Max(0, savedLegendaria);

        for (int i = 0; i < stoneCounts.Length; i++)
            StonesChanged?.Invoke((AscensionStoneTier)i, stoneCounts[i]);
    }

    // La usa el SaveManager al cargar.
    public void LoadPotions(int saved)
    {
        healingPotions = Mathf.Max(0, saved);
        PotionsChanged?.Invoke(healingPotions);
    }
}
