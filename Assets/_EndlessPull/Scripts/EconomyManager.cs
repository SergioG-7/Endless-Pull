using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    [Tooltip("Gemas con las que arranca la partida.")]
    [SerializeField] private int startingGems = 300;

    [Tooltip("Madera inicial para construir y mejorar.")]
    [SerializeField] private int startingWood = 0;

    [Tooltip("Hierro inicial para construir y mejorar.")]
    [SerializeField] private int startingIron = 0;

    private int gems;
    private int wood;
    private int iron;

    public int Gems => gems;
    public int Wood => wood;
    public int Iron => iron;

    // Se dispara con el saldo nuevo cada vez que cambian las gemas.
    public event System.Action<int> GemsChanged;

    // Se dispara con (madera, hierro) cada vez que cambian los materiales.
    public event System.Action<int, int> MaterialsChanged;

    void Awake()
    {
        gems = startingGems;
        wood = startingWood;
        iron = startingIron;
    }

    public bool CanAfford(int amount) => gems >= amount;

    // Descuenta solo si hay saldo; devuelve false si no llega.
    public bool TrySpend(int amount)
    {
        if (amount < 0 || gems < amount) return false;

        gems -= amount;
        GemsChanged?.Invoke(gems);
        return true;
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;

        gems += amount;
        GemsChanged?.Invoke(gems);
    }

    public bool CanAffordMaterials(int woodAmount, int ironAmount)
        => wood >= woodAmount && iron >= ironAmount;

    // Cobra madera y hierro de forma atómica: o los dos, o ninguno.
    public bool TrySpendMaterials(int woodAmount, int ironAmount)
    {
        if (woodAmount < 0 || ironAmount < 0) return false;
        if (!CanAffordMaterials(woodAmount, ironAmount)) return false;

        wood -= woodAmount;
        iron -= ironAmount;
        MaterialsChanged?.Invoke(wood, iron);
        return true;
    }

    public void AddMaterials(int woodAmount, int ironAmount)
    {
        if (woodAmount <= 0 && ironAmount <= 0) return;

        wood += Mathf.Max(0, woodAmount);
        iron += Mathf.Max(0, ironAmount);
        MaterialsChanged?.Invoke(wood, iron);
    }
}
