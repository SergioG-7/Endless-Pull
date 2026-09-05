using System.Collections.Generic;
using UnityEngine;

// Escuadra de asalto: quién sube a la torre y quién sale a recolectar.
public class PartyManager : MonoBehaviour
{
    [Tooltip("Héroes que caben en la escuadra.")]
    [SerializeField] private int maxPartySize = 4;

    [Tooltip("Héroes que caben en la escuadra de expedición de recursos.")]
    [SerializeField] private int maxExpeditionSize = 4;

    [Tooltip("Puestos de la formación, del slot 1 al 6; evitan que la escuadra se apile.")]
    [SerializeField] private Vector2[] formationSlots =
    {
        new Vector2(3.5f, 0f),
        new Vector2(2.5f, 0.8f),
        new Vector2(1.8f, -0.8f),
        new Vector2(1.0f, 0f),
        new Vector2(3.0f, 1.6f),
        new Vector2(2.3f, -1.6f)
    };

    [Tooltip("Pisos superados que suman un hueco de escuadra; con { 20, 40 } va de 4 a 6.")]
    [SerializeField] private int[] partySizeFloors = { 20, 40 };

    private readonly List<HeroController> party = new List<HeroController>();

    // Escuadra aparte para recolectar: subir la torre y granjear no se hacen a la vez.
    private readonly List<HeroController> expedition = new List<HeroController>();

    // Presets; cada uno guarda las dos escuadras por identidad de héroe. Se dimensionan desde
    // PresetCount para que subir la constante no deje arrays a medias.
    private readonly List<string>[] presetParty = NewPresetSlots();
    private readonly List<string>[] presetExpedition = NewPresetSlots();

    private static List<string>[] NewPresetSlots()
    {
        var slots = new List<string>[PresetCount];
        for (int i = 0; i < slots.Length; i++) slots[i] = new List<string>();
        return slots;
    }

    public IReadOnlyList<HeroController> Party => party;
    public IReadOnlyList<HeroController> ExpeditionSquad => expedition;
    // La escuadra crece con la Torre: un hueco más por cada piso clave superado, con el tope
    // puesto por los puestos de formación que existan.
    public int MaxPartySize
    {
        get
        {
            int extra = 0;
            if (partySizeFloors != null)
                foreach (int piso in partySizeFloors)
                    if (BaseBuilding.TowerFloor >= piso) extra++;

            int tope = formationSlots != null && formationSlots.Length > 0
                ? formationSlots.Length
                : maxPartySize;

            return Mathf.Min(maxPartySize + extra, tope);
        }
    }

    // Piso del próximo hueco de escuadra; 0 si ya no queda ninguno por delante.
    public int NextPartySizeFloor
    {
        get
        {
            if (partySizeFloors == null) return 0;

            foreach (int piso in partySizeFloors)
                if (BaseBuilding.TowerFloor < piso) return piso;

            return 0;
        }
    }
    public int MaxExpeditionSize => maxExpeditionSize;
    public const int PresetCount = 4;

    // Se dispara cuando cambia la escuadra.
    public event System.Action PartyChanged;

    void Update()
    {
        PruneParty();
    }

    // Puesto que le toca al héroe según su orden en la escuadra.
    public Vector2 FormationSlot(int slotIndex)
    {
        if (formationSlots == null || formationSlots.Length == 0) return Vector2.zero;

        return formationSlots[Mathf.Clamp(slotIndex, 0, formationSlots.Length - 1)];
    }

    public bool IsInParty(HeroController hero) => hero != null && party.Contains(hero);
    public bool IsInExpedition(HeroController hero) => hero != null && expedition.Contains(hero);
    public bool IsFull => party.Count >= MaxPartySize;
    public bool IsExpeditionFull => expedition.Count >= maxExpeditionSize;

    // Mete o saca al héroe de la escuadra de torre; devuelve true si se quedó dentro.
    public bool Toggle(HeroController hero)
    {
        if (hero == null) return false;

        if (party.Remove(hero))
        {
            PartyChanged?.Invoke();
            return false;
        }

        // Un puesto por héroe: si ya está de expedición, no sube a la torre (un edificio de
        // entrenamiento ya no bloquea, se desasigna solo al desplegar de verdad, ver WaveManager). a la torre.
        if (HeroAssignment.IsBusyElsewhere(hero, HeroDuty.TowerSquad))
        {
            Debug.LogWarning("[Escuadra] " + HeroAssignment.BusyWarning(hero), this);
            return false;
        }

        if (IsFull)
        {
            Debug.LogWarning($"[Escuadra] Ya hay {MaxPartySize} héroes asignados.", this);
            return false;
        }

        LeaveBuilding(hero);
        party.Add(hero);
        PartyChanged?.Invoke();
        return true;
    }

    // Un héroe que entra en una escuadra deja el edificio en el que estuviera asignado.
    private static void LeaveBuilding(HeroController hero)
    {
        if (hero == null || hero.AssignedBuilding == null) return;

        hero.AssignedBuilding.ToggleWorker(hero);
    }

    // Igual que Toggle, pero para la escuadra que sale a recolectar.
    public bool ToggleExpedition(HeroController hero)
    {
        if (hero == null) return false;

        if (HeroAssignment.IsLockedByExpedition(hero))
        {
            Debug.LogWarning("[Recolección] La escuadra está fuera; espera a que vuelva.", this);
            return IsInExpedition(hero);
        }

        if (expedition.Remove(hero))
        {
            PartyChanged?.Invoke();
            return false;
        }

        if (HeroAssignment.IsBusyElsewhere(hero, HeroDuty.Expedition))
        {
            Debug.LogWarning("[Recolección] " + HeroAssignment.BusyWarning(hero), this);
            return false;
        }

        if (IsExpeditionFull)
        {
            Debug.LogWarning($"[Recolección] Ya hay {maxExpeditionSize} héroes asignados.", this);
            return false;
        }

        LeaveBuilding(hero);
        expedition.Add(hero);
        PartyChanged?.Invoke();
        return true;
    }

    public void Clear()
    {
        party.Clear();
        PartyChanged?.Invoke();
    }

    public void ClearExpedition()
    {
        if (expedition.Count == 0) return;

        expedition.Clear();
        PartyChanged?.Invoke();
    }

    // Ids de la escuadra, para guardarla sin depender del orden del array.
    public List<string> PartyInstanceIds()
    {
        var ids = new List<string>();
        foreach (var hero in party)
            if (hero != null) ids.Add(hero.HeroInstanceId);

        return ids;
    }

    // Rehace la escuadra buscando por identidad entre los héroes que haya en escena.
    public void LoadParty(IList<string> ids, IList<HeroController> pool)
    {
        party.Clear();
        if (ids == null || pool == null) return;

        foreach (string id in ids)
        {
            foreach (var hero in pool)
            {
                if (hero == null || hero.HeroInstanceId != id) continue;

                if (!party.Contains(hero) && !IsFull) party.Add(hero);
                break;
            }
        }

        PartyChanged?.Invoke();
    }

    // Los muertos y los sacrificados no pueden seguir apuntados en ninguna de las dos.
    private void PruneParty()
    {
        bool changed = Prune(party) | Prune(expedition);
        if (changed) PartyChanged?.Invoke();
    }

    private static bool Prune(List<HeroController> list)
    {
        bool changed = false;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] != null) continue;

            list.RemoveAt(i);
            changed = true;
        }

        return changed;
    }

    // Ids de la escuadra de recolección, para guardarla igual que la de torre.
    public List<string> ExpeditionInstanceIds() => InstanceIds(expedition);

    private static List<string> InstanceIds(List<HeroController> list)
    {
        var ids = new List<string>();
        foreach (var hero in list)
            if (hero != null) ids.Add(hero.HeroInstanceId);

        return ids;
    }

    public void LoadExpedition(IList<string> ids, IList<HeroController> pool)
    {
        expedition.Clear();
        Fill(expedition, ids, pool, maxExpeditionSize);
        PartyChanged?.Invoke();
    }

    // Rellena una escuadra buscando por identidad entre los héroes disponibles.
    private static void Fill(List<HeroController> target, IList<string> ids,
                             IList<HeroController> pool, int max)
    {
        if (ids == null || pool == null) return;

        foreach (string id in ids)
        {
            foreach (var hero in pool)
            {
                if (hero == null || hero.HeroInstanceId != id) continue;

                if (!target.Contains(hero) && target.Count < max) target.Add(hero);
                break;
            }
        }
    }

    // Monta la escuadra de un preset sin tocar la activa; deja fuera a quien ya esté en la
    // escuadra de torre o en la de recolección, para que nadie salga dos veces.
    public List<HeroController> ResolvePreset(int index)
    {
        var squad = new List<HeroController>();
        if (index < 0 || index >= PresetCount) return squad;

        var pool = new List<HeroController>(
            UnityEngine.Object.FindObjectsByType<HeroController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None));

        pool.RemoveAll(h => h == null || party.Contains(h) || expedition.Contains(h));

        Fill(squad, presetParty[index], pool, MaxPartySize);
        return squad;
    }

    // Guarda las dos escuadras tal y como están en el preset indicado.
    public void SavePreset(int index)
    {
        if (index < 0 || index >= PresetCount) return;

        presetParty[index] = InstanceIds(party);
        presetExpedition[index] = InstanceIds(expedition);

        Debug.Log($"[Escuadra] Preset {index + 1} guardado: {presetParty[index].Count} en torre, " +
                  $"{presetExpedition[index].Count} en recolección.", this);
        SaveManager.RequestSave();
        PartyChanged?.Invoke();
    }

    // Recupera un preset; los héroes que ya tengan otro puesto se quedan fuera.
    public void ApplyPreset(int index)
    {
        if (index < 0 || index >= PresetCount) return;

        var pool = new List<HeroController>(
            UnityEngine.Object.FindObjectsByType<HeroController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None));

        // Con la recolección en marcha esa escuadra no se toca; la de torre sí se puede rehacer.
        var running = UnityEngine.Object.FindFirstObjectByType<ResourceExpeditionManager>();
        bool recolectando = running != null && running.IsRunning;

        // Se vacían antes de repartir: si no, la exclusividad rechazaría a los propios del preset.
        party.Clear();
        if (!recolectando) expedition.Clear();

        Fill(party, presetParty[index], pool, MaxPartySize);
        if (!recolectando) Fill(expedition, presetExpedition[index], pool, maxExpeditionSize);

        // Nadie puede estar en las dos a la vez; la de recolección manda porque puede estar fuera.
        for (int i = party.Count - 1; i >= 0; i--)
            if (expedition.Contains(party[i])) party.RemoveAt(i);

        // Currar en un edificio ya no echa a nadie del preset: se le saca del edificio.
        foreach (var hero in party) LeaveBuilding(hero);
        foreach (var hero in expedition) LeaveBuilding(hero);

        Debug.Log($"[Escuadra] Preset {index + 1} aplicado: {party.Count} en torre, " +
                  $"{expedition.Count} en recolección.", this);
        PartyChanged?.Invoke();
    }

    public bool HasPreset(int index)
        => index >= 0 && index < PresetCount
           && (presetParty[index].Count > 0 || presetExpedition[index].Count > 0);

    public List<string> PresetPartyIds(int index)
        => index >= 0 && index < PresetCount ? presetParty[index] : new List<string>();

    public List<string> PresetExpeditionIds(int index)
        => index >= 0 && index < PresetCount ? presetExpedition[index] : new List<string>();

    // La usa el SaveManager al cargar.
    public void LoadPreset(int index, List<string> partyIds, List<string> expeditionIds)
    {
        if (index < 0 || index >= PresetCount) return;

        presetParty[index] = partyIds ?? new List<string>();
        presetExpedition[index] = expeditionIds ?? new List<string>();
    }
}
