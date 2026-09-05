using System.Collections.Generic;
using UnityEngine;

// Equipo que se quedó en un piso al caer su portador. Se guarda por piso: volver a superarlo
// lo devuelve al almacén.
[System.Serializable]
public class LostGearStash
{
    public int floor;
    public List<EquipmentInstanceSaveData> items = new List<EquipmentInstanceSaveData>();
}

// Alijos de equipo perdido, uno por piso. Con la muerte permanente el héroe no vuelve, pero lo
// que llevaba puesto no se evapora: se queda donde cayó hasta que la Torre se despeje otra vez.
public class LostGearManager : MonoBehaviour
{
    private static LostGearManager instance;

    // Un alijo por piso; dos caídas en el mismo piso acumulan en el mismo montón.
    private readonly List<LostGearStash> stashes = new List<LostGearStash>();

    // Salta al dejar o recuperar un alijo; lo consume la UI que quiera pintarlos.
    public static event System.Action StashesChanged;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private static LostGearManager Instance
    {
        get
        {
            if (instance == null) instance = Object.FindFirstObjectByType<LostGearManager>();
            return instance;
        }
    }

    // Piezas que esperan en un piso concreto; 0 si ahí no cayó nadie.
    public static int CountAt(int floor)
    {
        var stash = Instance == null ? null : Instance.Find(floor, false);
        return stash == null ? 0 : stash.items.Count;
    }

    // La llama HeroController al morir, con el héroe todavía entero para poder leerle el equipo.
    public static void DropFrom(HeroController hero, int floor)
    {
        if (Instance == null || hero == null || floor <= 0) return;

        var stash = Instance.Find(floor, true);
        int antes = stash.items.Count;

        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            var pieza = hero.GetEquipped(slot);
            if (pieza == null || !pieza.IsValid) continue;

            stash.items.Add(SaveManager.ToSaveData(pieza));
        }

        if (stash.items.Count == antes) return;

        StashesChanged?.Invoke();
        Debug.Log($"[Botín perdido] {stash.items.Count - antes} pieza(s) se quedan en el piso {floor}.", Instance);
    }

    // La llama el WaveManager al superar un piso: lo que hubiera ahí vuelve al almacén.
    // Devuelve cuántas piezas se recuperaron.
    public static int Reclaim(int floor, ShopManager shop)
    {
        if (Instance == null || shop == null) return 0;

        var stash = Instance.Find(floor, false);
        if (stash == null || stash.items.Count == 0) return 0;

        int recuperadas = 0;
        foreach (var saved in stash.items)
        {
            var pieza = SaveManager.FromSaveData(saved, shop);
            if (pieza == null) continue;

            shop.AddToInventory(pieza);
            recuperadas++;
        }

        Instance.stashes.Remove(stash);
        StashesChanged?.Invoke();
        return recuperadas;
    }

    private LostGearStash Find(int floor, bool crear)
    {
        foreach (var stash in stashes)
            if (stash.floor == floor) return stash;

        if (!crear) return null;

        var nuevo = new LostGearStash { floor = floor };
        stashes.Add(nuevo);
        return nuevo;
    }

    // --- Persistencia; mismo patrón que MemorialManager ---

    public List<LostGearStash> Snapshot() => new List<LostGearStash>(stashes);

    public void LoadStashes(List<LostGearStash> saved)
    {
        stashes.Clear();
        if (saved == null) return;

        foreach (var stash in saved)
            if (stash != null && stash.items != null && stash.items.Count > 0) stashes.Add(stash);

        StashesChanged?.Invoke();
    }
}
