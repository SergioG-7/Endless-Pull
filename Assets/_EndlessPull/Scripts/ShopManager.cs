using System.Collections.Generic;
using UnityEngine;

// Tienda de equipo y almacén común de las piezas que no lleva nadie puesto.
public class ShopManager : MonoBehaviour
{
    [Tooltip("Piezas que puede soltar la tienda.")]
    [SerializeField] private List<EquipmentData> equipmentCatalog = new List<EquipmentData>();

    [Tooltip("Gemas que cuesta una tirada de equipo.")]
    [SerializeField] private int equipmentPullCost = 150;

    [Tooltip("Economía de la que se descuenta el coste.")]
    [SerializeField] private EconomyManager economy;

    // El almacén guarda instancias, no assets: dos "Espada de Hierro" pueden tener distinto
    // desgaste, distinto nivel de mejora y distinto afijo forjado.
    [Tooltip("Forja: sus recetas también cuentan como piezas conocidas al cargar la partida.")]
    [SerializeField] private CraftingManager crafting;

    [Tooltip("Gacha: de él sale el arma inicial, que tampoco vende la tienda.")]
    [SerializeField] private GachaManager gacha;

    private readonly List<EquipmentInstance> inventory = new List<EquipmentInstance>();

    public IReadOnlyList<EquipmentInstance> Inventory => inventory;
    public int EquipmentPullCost => equipmentPullCost;

    // Se dispara cuando cambia el inventario o el equipo de alguien.
    public event System.Action InventoryChanged;

    void Awake()
    {
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
    }

    // No basta con el catálogo de la tienda: el arma inicial y las recetas de forja son piezas
    // que existen en partidas guardadas y que la tienda no vende. Sin mirar ahí, al cargar se
    // perdían (y con ellas su desgaste, su nivel de mejora y su afijo forjado).
    public EquipmentData FindByAssetName(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return null;

        foreach (var item in equipmentCatalog)
            if (item != null && item.name == assetName) return item;

        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();
        if (crafting != null && crafting.CraftableWeapons != null)
            foreach (var item in crafting.CraftableWeapons)
                if (item != null && item.name == assetName) return item;

        if (gacha == null) gacha = UnityEngine.Object.FindFirstObjectByType<GachaManager>();
        if (gacha != null && gacha.StarterWeapon != null && gacha.StarterWeapon.name == assetName)
            return gacha.StarterWeapon;

        // El gacha reparte del array, no de la suelta: mirando solo StarterWeapon, todo héroe con
        // arco corto, lanza de práctica o báculo de aprendiz perdía el arma al cargar la partida.
        if (gacha != null && gacha.StarterWeapons != null)
            foreach (var arma in gacha.StarterWeapons)
                if (arma != null && arma.name == assetName) return arma;

        return null;
    }

    // Tirada de tienda: cobra y suelta una pieza al azar del catálogo.
    public EquipmentData PullEquipment()
    {
        if (equipmentCatalog == null || equipmentCatalog.Count == 0)
        {
            Debug.LogError("[Tienda] El catálogo de equipo está vacío.", this);
            return null;
        }

        if (economy == null || !economy.TrySpend(equipmentPullCost))
        {
            int saldo = economy != null ? economy.Gems : 0;
            Debug.LogWarning($"[Tienda] Compra bloqueada: cuesta {equipmentPullCost} y hay {saldo} gemas.", this);
            return null;
        }

        var picked = equipmentCatalog[Random.Range(0, equipmentCatalog.Count)];
        var instancia = AddToInventory(picked);

        Debug.Log($"[Tienda] Comprado {instancia.ShortLabel()}. Inventario: {inventory.Count} pieza(s).", this);
        SaveManager.RequestSave();
        return picked;
    }

    // Envuelve el asset en una instancia nueva y entera, y la mete en el almacén.
    public EquipmentInstance AddToInventory(EquipmentData item)
    {
        if (item == null) return null;

        var instancia = new EquipmentInstance(item);
        AddToInventory(instancia);
        return instancia;
    }

    public void AddToInventory(EquipmentInstance item)
    {
        if (item == null || !item.IsValid) return;

        inventory.Add(item);
        InventoryChanged?.Invoke();
    }

    public bool RemoveFromInventory(EquipmentInstance item)
    {
        if (item == null || !inventory.Remove(item)) return false;

        InventoryChanged?.Invoke();
        return true;
    }

    // Saca la pieza del inventario y la pone al héroe; lo que sale del hueco vuelve al almacén.
    public bool EquipFromInventory(HeroController hero, EquipmentInstance item)
    {
        if (hero == null || item == null) return false;
        if (!RemoveFromInventory(item)) return false;

        var replaced = hero.Equip(item);
        if (replaced != null) inventory.Add(replaced);

        InventoryChanged?.Invoke();
        Debug.Log($"[Equipo] {hero.Data.heroName} equipa {item.ShortLabel()}.", this);
        SaveManager.RequestSave();
        return true;
    }

    public bool UnequipToInventory(HeroController hero, EquipmentSlot slot)
    {
        if (hero == null) return false;

        var removed = hero.Unequip(slot);
        if (removed == null) return false;

        inventory.Add(removed);
        InventoryChanged?.Invoke();
        Debug.Log($"[Equipo] {hero.Data.heroName} se quita {removed.ShortLabel()}.", this);
        SaveManager.RequestSave();
        return true;
    }

    // Cuánto vale una pieza PARA ESTE HÉROE. Sirve para comparar candidatas del mismo hueco,
    // no como número absoluto: mezcla las cifras ya escaladas por nivel de mejora con los
    // afijos, y premia el arma cuyo tipo ya domina el héroe.
    public float ScoreFor(HeroController hero, EquipmentInstance item)
    {
        if (item == null || !item.IsValid) return float.MinValue;

        // Una pieza rota no aporta nada, pero sigue siendo mejor que el hueco vacío.
        if (item.IsBroken) return 0.01f;

        float score = item.BonusATK * 2f + item.BonusDEF * 2f + item.BonusHP * 0.4f;

        foreach (EquipmentAffix affix in System.Enum.GetValues(typeof(EquipmentAffix)))
        {
            if (affix == EquipmentAffix.None) continue;
            score += item.AffixValue(affix) * 0.5f;
        }

        // Maestría: con el arma que ya sabe manejar pega más que con una mejor "en papel".
        if (hero != null && item.SlotType == EquipmentSlot.Weapon)
            score *= hero.Mastery.DamageMultiplier(item.WeaponType);

        return score;
    }

    // Mejor pieza del almacén para ese hueco, o null si ninguna supera a la que ya lleva.
    public EquipmentInstance BestUpgradeFor(HeroController hero, EquipmentSlot slot)
    {
        if (hero == null) return null;

        float actual = ScoreFor(hero, hero.GetEquipped(slot));
        if (hero.GetEquipped(slot) == null) actual = 0f;

        EquipmentInstance mejor = null;
        float mejorScore = actual;

        foreach (var item in inventory)
        {
            if (item == null || item.SlotType != slot) continue;

            float score = ScoreFor(hero, item);
            if (score <= mejorScore) continue;

            mejorScore = score;
            mejor = item;
        }

        return mejor;
    }

    // Autoequipar: rellena los CUATRO huecos de una pasada con lo mejor que haya para este
    // héroe. Es idempotente — si ya lleva lo mejor, volver a pulsarlo no cambia nada, en vez
    // de ir cambiando piezas por otras peores como hacía la versión de una en una.
    public int AutoEquipBest(HeroController hero)
    {
        if (hero == null) return 0;

        int cambios = 0;
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            var mejor = BestUpgradeFor(hero, slot);
            if (mejor == null) continue;

            if (EquipFromInventory(hero, mejor)) cambios++;
        }

        if (cambios > 0)
        {
            Debug.Log($"[Equipo] Autoequipado {hero.Data.heroName}: {cambios} pieza(s) mejoradas.", this);
            SaveManager.RequestSave();
        }

        return cambios;
    }

    // Primera pieza del inventario que le sirve al héroe; se conserva para quien la use.
    public EquipmentInstance FirstEquippableFor(HeroController hero)
    {
        if (hero == null) return null;

        // El hueco de arma manda: sin arma, un escudo en la mano principal no sirve de nada.
        if (hero.GetEquipped(EquipmentSlot.Weapon) == null)
            foreach (var item in inventory)
                if (item != null && item.SlotType == EquipmentSlot.Weapon) return item;

        // Luego lo que rellena cualquier otro hueco vacío, y si no, la primera pieza.
        foreach (var item in inventory)
            if (item != null && hero.GetEquipped(item.SlotType) == null) return item;

        return inventory.Count > 0 ? inventory[0] : null;
    }

    // Devuelve la pieza equipada de vuelta al almacén; la usa el SaveManager al reconstruir.
    public void ClearInventory()
    {
        inventory.Clear();
        InventoryChanged?.Invoke();
    }
}
