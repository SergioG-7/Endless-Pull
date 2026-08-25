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

    private readonly List<EquipmentData> inventory = new List<EquipmentData>();

    public IReadOnlyList<EquipmentData> Inventory => inventory;
    public int EquipmentPullCost => equipmentPullCost;

    // Se dispara cuando cambia el inventario o el equipo de alguien.
    public event System.Action InventoryChanged;

    void Awake()
    {
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
    }

    public EquipmentData FindByAssetName(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return null;

        foreach (var item in equipmentCatalog)
            if (item != null && item.name == assetName) return item;

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
        AddToInventory(picked);

        Debug.Log($"[Tienda] Comprado {picked.ShortLabel()}. Inventario: {inventory.Count} pieza(s).", this);
        SaveManager.RequestSave();
        return picked;
    }

    public void AddToInventory(EquipmentData item)
    {
        if (item == null) return;

        inventory.Add(item);
        InventoryChanged?.Invoke();
    }

    public bool RemoveFromInventory(EquipmentData item)
    {
        if (item == null || !inventory.Remove(item)) return false;

        InventoryChanged?.Invoke();
        return true;
    }

    // Saca la pieza del inventario y la pone al héroe; lo que sale del hueco vuelve al almacén.
    public bool EquipFromInventory(HeroController hero, EquipmentData item)
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

    // Primera pieza del inventario que le sirve al héroe; la usa el botón "Equipar".
    public EquipmentData FirstEquippableFor(HeroController hero)
    {
        if (hero == null) return null;

        // El hueco de arma manda: sin arma, un escudo en la mano principal no sirve de nada.
        if (hero.GetEquipped(EquipmentSlot.Weapon) == null)
            foreach (var item in inventory)
                if (item != null && item.slotType == EquipmentSlot.Weapon) return item;

        // Luego lo que rellena cualquier otro hueco vacío, y si no, la primera pieza.
        foreach (var item in inventory)
            if (item != null && hero.GetEquipped(item.slotType) == null) return item;

        return inventory.Count > 0 ? inventory[0] : null;
    }

    // Devuelve la pieza equipada de vuelta al almacén; la usa el SaveManager al reconstruir.
    public void ClearInventory()
    {
        inventory.Clear();
        InventoryChanged?.Invoke();
    }
}
