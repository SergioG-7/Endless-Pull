using UnityEngine;

// Destinos de recolección; cada uno trae un recurso distinto.
public enum ResourceExpeditionType
{
    Forest,
    Mine,
    Hunt
}

// Expediciones de granjeo: la escuadra se va un rato y vuelve con material, sin combate.
public class ResourceExpeditionManager : MonoBehaviour
{
    [Tooltip("Segundos que dura una expedición de recursos.")]
    [SerializeField] private float durationSeconds = 20f;

    [Tooltip("Recurso base que trae la expedición, por héroe enviado.")]
    [SerializeField] private int rewardPerHero = 15;

    [Tooltip("Comida que consume salir a recolectar.")]
    [SerializeField] private int foodCost = 5;

    [Tooltip("Economía a la que se abona la cosecha.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Escuadra que se manda a recolectar.")]
    [SerializeField] private PartyManager party;

    private ResourceExpeditionType currentType;
    private float remaining;
    private int heroesSent;

    public bool IsRunning => remaining > 0f;
    public float Remaining => Mathf.Max(0f, remaining);
    public float Duration => durationSeconds;
    public ResourceExpeditionType CurrentType => currentType;

    // Se dispara con el mensaje de estado para que la UI lo muestre.
    public event System.Action<string> ExpeditionChanged;

    void Awake()
    {
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        if (party == null) party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();
    }

    void Update()
    {
        if (!IsRunning) return;

        remaining -= Time.deltaTime;
        if (remaining > 0f) return;

        remaining = 0f;
        Collect();
    }

    public static string DisplayName(ResourceExpeditionType type)
    {
        switch (type)
        {
            case ResourceExpeditionType.Forest: return "Bosque";
            case ResourceExpeditionType.Mine: return "Mina";
            case ResourceExpeditionType.Hunt: return "Cacería";
        }
        return type.ToString();
    }

    public static string RewardName(ResourceExpeditionType type)
    {
        switch (type)
        {
            case ResourceExpeditionType.Forest: return "Madera";
            case ResourceExpeditionType.Mine: return "Hierro";
            case ResourceExpeditionType.Hunt: return "Comida";
        }
        return "recursos";
    }

    public bool CanStart => !IsRunning
        && party != null && party.Party.Count > 0
        && economy != null && economy.CanAffordFood(foodCost);

    public bool StartExpedition(ResourceExpeditionType type)
    {
        if (IsRunning)
        {
            Report($"La escuadra ya está en {DisplayName(currentType)}.");
            return false;
        }

        if (party == null || party.Party.Count == 0)
        {
            Report("Asigna héroes a la escuadra antes de salir a recolectar.");
            return false;
        }

        if (economy == null || !economy.TrySpendFood(foodCost))
        {
            Report($"Hacen falta {foodCost} de comida para salir.");
            return false;
        }

        currentType = type;
        heroesSent = party.Party.Count;
        remaining = durationSeconds;

        Report($"Escuadra de {heroesSent} enviada a {DisplayName(type)} ({durationSeconds:0}s).");
        return true;
    }

    // La cosecha escala con cuánta gente fue, no con cuánto se tardó.
    private void Collect()
    {
        int amount = rewardPerHero * Mathf.Max(1, heroesSent);

        switch (currentType)
        {
            case ResourceExpeditionType.Forest: economy.AddMaterials(amount, 0); break;
            case ResourceExpeditionType.Mine: economy.AddMaterials(0, amount); break;
            case ResourceExpeditionType.Hunt: economy.AddFood(amount); break;
        }

        Report($"Vuelta de {DisplayName(currentType)}: +{amount} {RewardName(currentType)}.");
        SaveManager.RequestSave();
    }

    private void Report(string message)
    {
        Debug.Log($"[Recolección] {message}", this);
        ExpeditionChanged?.Invoke(message);
    }
}
