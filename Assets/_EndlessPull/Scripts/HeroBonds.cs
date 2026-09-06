using System.Collections.Generic;
using UnityEngine;

// Vínculos entre héroes: cuenta con quién ha sobrevivido pisos cada uno. Al pasar del umbral,
// los dos quedan vinculados; si uno cae, al otro se le viene el mundo encima.
//
// Vive en el mismo GameObject que HeroController y se guarda con el resto de la ficha.
public class HeroBonds : MonoBehaviour
{
    [Tooltip("Pisos sobrevividos juntos que hacen falta para que el vínculo cuente.")]
    [SerializeField] private int bondThreshold = 5;

    [Tooltip("Moral que pierde el héroe cuando muere alguien con quien estaba vinculado.")]
    [SerializeField] private float moraleLossOnBondDeath = 35f;

    // Nombre del compañero -> pisos limpiados juntos. Por nombre y no por referencia: el héroe
    // muerto ya no existe como objeto, pero su vínculo tiene que seguir doliendo.
    private readonly Dictionary<string, int> floorsTogether = new Dictionary<string, int>();

    private HeroController hero;

    void Awake() => hero = GetComponent<HeroController>();

    public int BondThreshold => bondThreshold;

    public int FloorsWith(string otherName)
        => otherName != null && floorsTogether.TryGetValue(otherName, out int pisos) ? pisos : 0;

    public bool IsBondedTo(string otherName) => FloorsWith(otherName) >= bondThreshold;

    // Compañeros que ya pasan el umbral; lo usa la ficha del héroe y la Galería.
    public List<string> BondedNames()
    {
        var salida = new List<string>();
        foreach (var par in floorsTogether)
            if (par.Value >= bondThreshold) salida.Add(par.Key);

        return salida;
    }

    // Un compañero y los pisos que llevan juntos; lo pinta la ficha del héroe.
    public struct BondEntry
    {
        public string otherName;
        public int floors;
        public bool bonded;
        public bool fallen;
    }

    // Vínculos ordenados de más a menos pisos juntos; los ya vinculados primero.
    public List<BondEntry> Entries()
    {
        var caidos = MemorialManager.MemorialNames();
        var salida = new List<BondEntry>();

        foreach (var par in floorsTogether)
            salida.Add(new BondEntry
            {
                otherName = par.Key,
                floors = par.Value,
                bonded = par.Value >= bondThreshold,
                fallen = caidos != null && caidos.Contains(par.Key)
            });

        salida.Sort((a, b) => b.floors.CompareTo(a.floors));
        return salida;
    }

    // Quién sigue vivo y estaba vinculado con el caído; lo pinta la Galería.
    public static List<string> MournersOf(string fallenName)
    {
        var salida = new List<string>();
        if (string.IsNullOrEmpty(fallenName)) return salida;

        // Incluye inactivos: un héroe de expedición o de Torre sigue de luto.
        foreach (var bonds in UnityEngine.Object.FindObjectsByType<HeroBonds>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (bonds == null || !bonds.IsBondedTo(fallenName)) continue;

            // GetComponent y no el campo cacheado: un héroe que nunca llegó a activarse no ha
            // pasado por Awake y tendría 'hero' a null.
            var doliente = bonds.GetComponent<HeroController>();
            if (doliente == null || doliente.Data == null) continue;
            if (doliente.Data.heroName == fallenName) continue;

            salida.Add(doliente.Data.heroName);
        }

        return salida;
    }

    // ¿Alguno de sus vínculos está ya en la Galería? Pesa mucho más que el duelo genérico.
    public bool HasFallenBond()
    {
        var caidos = MemorialManager.MemorialNames();
        if (caidos == null || caidos.Count == 0) return false;

        foreach (var par in floorsTogether)
            if (par.Value >= bondThreshold && caidos.Contains(par.Key)) return true;

        return false;
    }

    // Al despejar un piso, todo el que siga en pie suma un piso con cada compañero vivo.
    public static void RecordFloorCleared(IReadOnlyList<HeroController> squad)
    {
        if (squad == null) return;

        for (int i = 0; i < squad.Count; i++)
        {
            var uno = squad[i];
            if (uno == null || uno.CurrentHealth <= 0 || uno.Data == null) continue;

            var bonds = uno.GetComponent<HeroBonds>();
            if (bonds == null) continue;

            for (int j = 0; j < squad.Count; j++)
            {
                if (j == i) continue;

                var otro = squad[j];
                if (otro == null || otro.CurrentHealth <= 0 || otro.Data == null) continue;

                bonds.Add(otro.Data.heroName, 1);
            }
        }
    }

    private void Add(string otherName, int cantidad)
    {
        if (string.IsNullOrEmpty(otherName)) return;

        floorsTogether.TryGetValue(otherName, out int actual);
        floorsTogether[otherName] = actual + cantidad;
    }

    // Muere un héroe: quien estuviera vinculado con él se hunde y lo dice en voz alta.
    public static void ReportDeath(string fallenName)
    {
        if (string.IsNullOrEmpty(fallenName)) return;

        foreach (var bonds in UnityEngine.Object.FindObjectsByType<HeroBonds>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (bonds == null || !bonds.IsBondedTo(fallenName)) continue;

            var doliente = bonds.hero;
            if (doliente == null) continue;

            doliente.LoseMorale(bonds.moraleLossOnBondDeath);
            doliente.Bark(1f, "BOND_LOST_1", "BOND_LOST_2", "BOND_LOST_3");

            Debug.Log($"[Vínculo] {doliente.Data.heroName} pierde a {fallenName}: " +
                      $"-{bonds.moraleLossOnBondDeath} moral (queda {doliente.MoralePercent}).", doliente);
        }
    }

    // --- Guardado ---------------------------------------------------------------------

    public List<HeroBondSaveData> Capture()
    {
        var salida = new List<HeroBondSaveData>();
        foreach (var par in floorsTogether)
            salida.Add(new HeroBondSaveData { otherName = par.Key, floors = par.Value });

        return salida;
    }

    public void Restore(List<HeroBondSaveData> guardados)
    {
        floorsTogether.Clear();
        if (guardados == null) return;

        foreach (var dato in guardados)
        {
            if (dato == null || string.IsNullOrEmpty(dato.otherName)) continue;
            floorsTogether[dato.otherName] = dato.floors;
        }
    }
}
