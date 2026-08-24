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

    [Tooltip("Economía de la que salen los materiales.")]
    [SerializeField] private EconomyManager economy;

    private int ascensionStones;

    public int AscensionStones => ascensionStones;
    public int WoodCost => woodCost;
    public int IronCost => ironCost;
    public float SuccessChance => successChance;

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

        bool success = Random.value < successChance;

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
