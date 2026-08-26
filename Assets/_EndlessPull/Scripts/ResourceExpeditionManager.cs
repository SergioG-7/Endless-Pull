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
    [Tooltip("Segundos que dura una expedición de recursos (3-5 minutos recomendado).")]
    [SerializeField] private float durationSeconds = 240f;

    [Tooltip("Recurso base que trae la expedición, por héroe enviado.")]
    [SerializeField] private int rewardPerHero = 15;

    [Tooltip("Economía a la que se abona la cosecha.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Gestor del que sale la escuadra de recolección.")]
    [SerializeField] private PartyManager party;

    private ResourceExpeditionType currentType;
    private float remaining;
    private int heroesSent;

    // El temporizador puede llegar a cero antes de que el jugador reclame: los héroes
    // siguen bloqueados hasta el reclamo explícito, no se cobra sola la recompensa.
    private bool readyToClaim;

    // Ocupa el "slot" de expedición tanto contando atrás como esperando reclamo.
    public bool IsRunning => remaining > 0f || readyToClaim;
    public bool ReadyToClaim => readyToClaim;
    public float Remaining => Mathf.Max(0f, remaining);
    public float Duration => durationSeconds;
    public ResourceExpeditionType CurrentType => currentType;
    public int HeroesSent => heroesSent;

    // Se dispara con el mensaje de estado para que la UI lo muestre.
    public event System.Action<string> ExpeditionChanged;

    void Awake()
    {
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        if (party == null) party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();
    }

    void Update()
    {
        if (remaining <= 0f) return;

        remaining -= Time.deltaTime;
        if (remaining > 0f) return;

        remaining = 0f;
        readyToClaim = true;
        Report(string.Format(LocalizationManager.Get("UI_EXPEDITION_READY"), DisplayName(currentType)));
    }

    public static string DisplayName(ResourceExpeditionType type)
    {
        switch (type)
        {
            case ResourceExpeditionType.Forest: return LocalizationManager.Get("UI_FOREST");
            case ResourceExpeditionType.Mine: return LocalizationManager.Get("UI_MINE");
            case ResourceExpeditionType.Hunt: return LocalizationManager.Get("UI_HUNT");
        }
        return type.ToString();
    }

    public static string RewardName(ResourceExpeditionType type)
    {
        switch (type)
        {
            case ResourceExpeditionType.Forest: return LocalizationManager.Get("UI_WOOD");
            case ResourceExpeditionType.Mine: return LocalizationManager.Get("UI_IRON");
            case ResourceExpeditionType.Hunt: return LocalizationManager.Get("UI_FOOD");
        }
        return LocalizationManager.Get("UI_GEMS");
    }

    // Ya no consume intentos: solo hace falta escuadra libre y ningún slot ocupado.
    public bool CanStart => !IsRunning && party != null && party.ExpeditionSquad.Count > 0;

    public bool StartExpedition(ResourceExpeditionType type)
    {
        if (IsRunning)
        {
            Report(string.Format(LocalizationManager.Get("UI_EXPEDITION_BUSY"), DisplayName(currentType)));
            return false;
        }

        if (party == null || party.ExpeditionSquad.Count == 0)
        {
            Report(LocalizationManager.Get("UI_STATUS_ASSIGN_HEROES"));
            return false;
        }

        currentType = type;
        heroesSent = party.ExpeditionSquad.Count;
        remaining = durationSeconds;
        readyToClaim = false;

        Report(string.Format(LocalizationManager.Get("UI_EXPEDITION_SENT"),
            heroesSent, DisplayName(type), durationSeconds));
        return true;
    }

    // La cosecha escala con cuánta gente fue, no con cuánto se tardó. Llamado a mano por la UI:
    // el temporizador en cero solo avisa, no cobra sola.
    public bool ClaimReward()
    {
        if (!readyToClaim) return false;

        int amount = rewardPerHero * Mathf.Max(1, heroesSent);

        switch (currentType)
        {
            case ResourceExpeditionType.Forest: economy.AddMaterials(amount, 0); break;
            case ResourceExpeditionType.Mine: economy.AddMaterials(0, amount); break;
            case ResourceExpeditionType.Hunt: economy.AddFood(amount); break;
        }

        Report(string.Format(LocalizationManager.Get("UI_EXPEDITION_CLAIMED"),
            DisplayName(currentType), amount, RewardName(currentType)));

        readyToClaim = false;
        heroesSent = 0;

        // Al reclamar, la escuadra de recolección queda libre de golpe: no hace falta
        // que el jugador la desmarque a mano héroe por héroe.
        if (party != null) party.ClearExpedition();

        SaveManager.RequestSave();
        return true;
    }

    private void Report(string message)
    {
        Debug.Log($"[Recolección] {message}", this);
        ExpeditionChanged?.Invoke(message);
    }

    // La usa el SaveManager: recupera el temporizador exactamente donde se dejó.
    public void LoadState(int type, float savedRemaining, int savedHeroesSent, bool savedReadyToClaim)
    {
        currentType = (ResourceExpeditionType)type;
        remaining = Mathf.Max(0f, savedRemaining);
        heroesSent = Mathf.Max(0, savedHeroesSent);
        readyToClaim = savedReadyToClaim;
    }
}
