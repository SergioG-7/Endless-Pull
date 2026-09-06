using System.Collections.Generic;
using UnityEngine;

// Cómo se perdió un héroe: sacrificado en la síntesis o caído en la Torre. Las dos son
// definitivas — el nombre queda ocupado y el gacha no vuelve a ofrecerlo.
public enum MemorialCause
{
    Synthesis,
    FallenInTower
}

// Ficha de honor de un héroe que ya no está; se guarda tal cual estaba al perderlo.
[System.Serializable]
public class MemorialRecord
{
    public string heroName = string.Empty;
    public int starRank = 1;
    public int level = 1;
    public int floor;
    public MemorialCause cause;

    // Último equipo que llevaba puesto, ya resuelto a texto: el asset puede desaparecer y la
    // ficha tiene que seguir leyéndose igual dentro de un año.
    public string lastEquipment = string.Empty;

    // Nombre del asset de HeroData, solo para recuperar el retrato en la Galería. Vacío en
    // registros antiguos: entonces se busca por nombre visible.
    public string heroAsset = string.Empty;

    public string CauseLabel()
        => cause == MemorialCause.Synthesis
           ? LocalizationManager.Get("UI_MEMORIAL_SYNTH")
           : string.Format(LocalizationManager.Get("UI_MEMORIAL_FALLEN"), floor);
}

// Registro de los héroes perdidos; lo pinta la Galería Memorial y lo guarda el SaveManager.
public class MemorialManager : MonoBehaviour
{
    private static MemorialManager instance;

    private readonly List<MemorialRecord> records = new List<MemorialRecord>();
    public IReadOnlyList<MemorialRecord> Records => records;

    // Cuantos se han perdido ya; lo consultan los bocadillos para hablar de los ausentes.
    public static int LostCount
    {
        get
        {
            if (instance == null) instance = Object.FindFirstObjectByType<MemorialManager>();
            return instance == null ? 0 : instance.records.Count;
        }
    }

    // Salta al añadir una ficha; la Galería se repinta sin sondear.
    public static event System.Action RecordsChanged;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Punto de entrada para los sistemas que no tienen referencia al manager.
    public static void Record(HeroController hero, MemorialCause cause, int floor = 0)
    {
        if (instance == null) instance = Object.FindFirstObjectByType<MemorialManager>();
        if (instance == null || hero == null || hero.Data == null) return;

        var progress = hero.GetComponent<HeroProgress>();

        instance.records.RemoveAll(f => f.heroName == hero.Data.heroName);

        instance.records.Add(new MemorialRecord
        {
            heroName = hero.Data.heroName,
            starRank = hero.StarRank,
            level = progress != null ? progress.Level : 1,
            floor = floor,
            cause = cause,
            lastEquipment = DescribeEquipment(hero),
            heroAsset = hero.Data != null ? hero.Data.name : string.Empty
        });

        RecordsChanged?.Invoke();
        Debug.Log($"[Memorial] {hero.Data.heroName} pasa a la Galería ({cause}).", instance);
    }

    private static string DescribeEquipment(HeroController hero)
    {
        var piezas = new List<string>();

        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            // El arma no se lista en la ficha; el resto del equipo sí.
            if (slot == EquipmentSlot.Weapon) continue;

            var pieza = hero.GetEquipped(slot);
            if (pieza != null && pieza.IsValid) piezas.Add(pieza.LocalizedName());
        }

        return piezas.Count > 0 ? string.Join(", ", piezas) : string.Empty;
    }

    // Nombres de todos los caídos. El gacha los suma a los vivos para que su hueco del
    // catálogo siga ocupado y no se les pueda volver a invocar.
    public static HashSet<string> MemorialNames()
    {
        var names = new HashSet<string>();

        if (instance == null) instance = Object.FindFirstObjectByType<MemorialManager>();
        if (instance == null) return names;

        foreach (var ficha in instance.records)
            if (!string.IsNullOrEmpty(ficha.heroName)) names.Add(ficha.heroName);

        return names;
    }

    // Las usa el SaveManager para conservar la Galería entre partidas.
    public List<MemorialRecord> Snapshot() => new List<MemorialRecord>(records);

    public void LoadRecords(List<MemorialRecord> saved)
    {
        records.Clear();
        if (saved != null) records.AddRange(saved);
        RecordsChanged?.Invoke();
    }
}
