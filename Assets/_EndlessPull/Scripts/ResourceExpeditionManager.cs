using System.Collections.Generic;
using UnityEngine;

// Destinos de recolección; cada uno trae un recurso distinto. Rift va al final para no
// romper el índice guardado de partidas viejas (SaveManager lo persiste como int).
public enum ResourceExpeditionType
{
    Forest,
    Mine,
    Hunt,
    Rift
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

    [Tooltip("Torre, para escalar la recompensa según el piso más alto superado.")]
    [SerializeField] private WaveManager waves;

    [Tooltip("Taller, para las Piedras de Ascensión que suelta Minas Profundas.")]
    [SerializeField] private CraftingManager crafting;

    [Tooltip("EXP por héroe que da Tierras de Caza, antes del multiplicador de piso/bono diario.")]
    [SerializeField] private int expPerHero = 8;

    [Tooltip("Piedras de Ascensión (tier Menor) por cada 2 héroes enviados a Minas Profundas.")]
    [SerializeField] private float stonesPerHero = 0.5f;

    [Tooltip("Las gemas de la Grieta Dimensional valen más por unidad que madera/hierro/comida; se aplica sobre el mismo cálculo base.")]
    [SerializeField] private float riftGemFactor = 0.5f;

    [Tooltip("Multiplicador de recompensa cuando el destino elegido es el bono rotativo del día.")]
    [SerializeField] private float dailyBonusMultiplier = 1.5f;

    [Tooltip("Segundos hasta colocar a la escuadra en el claro; lo que tardan en cruzar el Portal.")]
    [SerializeField] private float mapEntryDelay = 2f;

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
        if (waves == null) waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();
    }

    // Rotación semanal: qué destino da el bono x1.5 hoy. Por ahora los otros
    // tres siguen siendo elegibles igual (pedido explícito: "que se vean todas para poder
    // probarlas"), esto solo decide cuál luce la etiqueta de bono y cobra el multiplicador.
    public static ResourceExpeditionType TodaysBonusType(System.DateTime now) => now.DayOfWeek switch
    {
        System.DayOfWeek.Monday or System.DayOfWeek.Wednesday => ResourceExpeditionType.Mine,
        System.DayOfWeek.Tuesday or System.DayOfWeek.Thursday => ResourceExpeditionType.Forest,
        System.DayOfWeek.Friday or System.DayOfWeek.Saturday => ResourceExpeditionType.Hunt,
        _ => ResourceExpeditionType.Rift
    };

    public static ResourceExpeditionType TodaysBonusType() => TodaysBonusType(System.DateTime.Now);

    public float DailyBonusMultiplier => dailyBonusMultiplier;

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
            case ResourceExpeditionType.Rift: return LocalizationManager.Get("UI_RIFT");
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

    // Escala con el piso más alto superado: 5% extra por piso (Piso 1 = x1, Piso 21 = x2).
    public float ProgressMultiplier => 1f + 0.05f * Mathf.Max(0, (waves != null ? waves.HighestClearedFloor : 1) - 1);

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

        foreach (var hero in party.ExpeditionSquad)
        {
            if (hero == null) continue;

            // Desacoplado de Fase 39: si estaba currando en un edificio, se desasigna solo al
            // salir de verdad, sin bloquear antes al meterlo en la escuadra.
            if (hero.AssignedBuilding != null) hero.AssignedBuilding.ToggleWorker(hero);
            hero.SendOnExpedition();
        }

        // El claro los recoge cuando terminan de salir por el Portal; sin él, siguen
        // desapareciendo como antes.
        if (ExpeditionMap.Instance != null)
            StartCoroutine(OpenMapWhenGone(new List<HeroController>(party.ExpeditionSquad), type));

        Report(string.Format(LocalizationManager.Get("UI_EXPEDITION_SENT"),
            heroesSent, DisplayName(type), durationSeconds));
        return true;
    }

    // Los héroes tardan un momento en llegar al Portal (ExpeditionDepartRoutine): hasta que no
    // se han ido de la base no tiene sentido colocarlos en el claro.
    private System.Collections.IEnumerator OpenMapWhenGone(List<HeroController> squad,
                                                           ResourceExpeditionType type)
    {
        yield return new WaitForSeconds(mapEntryDelay);

        if (ExpeditionMap.Instance != null) ExpeditionMap.Instance.Open(squad, type);
    }

    // La cosecha escala con cuánta gente fue, no con cuánto se tardó. Llamado a mano por la UI:
    // el temporizador en cero solo avisa, no cobra sola.
    public bool ClaimReward()
    {
        if (!readyToClaim) return false;

        QuestManager.Report(QuestKind.CompleteExpedition);

        bool bonus = currentType == TodaysBonusType();
        float mult = ProgressMultiplier * (bonus ? dailyBonusMultiplier : 1f);
        int sent = Mathf.Max(1, heroesSent);

        int amount = Mathf.RoundToInt(rewardPerHero * sent * mult
            * (currentType == ResourceExpeditionType.Rift ? riftGemFactor : 1f));

        // Toast de resumen antes de sumar el material: el jugador ve exactamente qué ganó,
        // no solo el número final del recurso ya actualizado en el TopBar.
        string resumen = string.Format(LocalizationManager.Get("UI_EXPEDITION_SUMMARY_TOAST"),
            amount, RewardName(currentType));
        ScreenBanner.Show(resumen, 2.2f, new Color(0.55f, 0.85f, 0.55f));
        AudioManager.Play(SfxId.Reward);

        switch (currentType)
        {
            case ResourceExpeditionType.Forest: economy.AddMaterials(amount, 0); break;
            case ResourceExpeditionType.Mine: economy.AddMaterials(0, amount); break;
            case ResourceExpeditionType.Hunt: economy.AddFood(amount); break;
            case ResourceExpeditionType.Rift: economy.Add(amount); break;
        }

        // Recompensa secundaria por destino (Minas: Piedra de Ascensión; Caza: EXP a la escuadra
        // enviada), con su propio toast compacto arriba para no mezclarse con el resumen principal.
        if (currentType == ResourceExpeditionType.Mine && crafting != null)
        {
            int stones = Mathf.Max(1, Mathf.RoundToInt(sent * stonesPerHero));
            crafting.AddStones(AscensionStoneTier.Menor, stones);
            ScreenBanner.ShowCompact(string.Format(LocalizationManager.Get("UI_EXPEDITION_SUMMARY_TOAST"),
                stones, LocalizationManager.Get("UI_STONE_MENOR")), 2f, new Color(0.75f, 0.75f, 0.85f));
        }
        else if (currentType == ResourceExpeditionType.Hunt && party != null)
        {
            int expGain = Mathf.Max(1, Mathf.RoundToInt(expPerHero * mult));
            foreach (var hero in party.ExpeditionSquad)
                hero?.GetComponent<HeroProgress>()?.AddEXP(expGain);
            ScreenBanner.ShowCompact(string.Format(LocalizationManager.Get("UI_EXPEDITION_SUMMARY_TOAST"),
                expGain, "EXP"), 2f, new Color(0.75f, 0.75f, 0.85f));
        }

        Report(string.Format(LocalizationManager.Get("UI_EXPEDITION_CLAIMED"),
            DisplayName(currentType), amount, RewardName(currentType)));

        readyToClaim = false;
        heroesSent = 0;

        // El claro se vacía antes de devolverlos: si no, seguirían paseando por el bosque
        // mientras el Portal los escupe en la base.
        if (ExpeditionMap.Instance != null) ExpeditionMap.Instance.Close();

        // La escuadra se mantiene asignada tras reclamar: vuelve visible por el Portal y
        // queda lista para la siguiente ronda sin que el jugador tenga que reasignarla.
        if (party != null)
            foreach (var hero in party.ExpeditionSquad)
                if (hero != null) hero.ReturnFromExpedition();

        SaveManager.RequestSave();
        return true;
    }

    private void Report(string message)
    {
        Debug.Log($"[Recolección] {message}", this);
        ExpeditionChanged?.Invoke(message);
    }

    // Progresión offline: adelanta el temporizador los segundos que el juego estuvo cerrado.
    // Devuelve true si la expedición terminó durante ese rato y quedó lista para reclamar.
    public bool AdvanceOffline(float seconds)
    {
        if (seconds <= 0f || remaining <= 0f) return false;

        remaining -= seconds;
        if (remaining > 0f) return false;

        remaining = 0f;
        readyToClaim = true;
        return true;
    }

    // La usa el SaveManager: recupera el temporizador exactamente donde se dejó.
    public void LoadState(int type, float savedRemaining, int savedHeroesSent, bool savedReadyToClaim)
    {
        currentType = (ResourceExpeditionType)type;
        remaining = Mathf.Max(0f, savedRemaining);
        heroesSent = Mathf.Max(0, savedHeroesSent);
        readyToClaim = savedReadyToClaim;
    }

    void OnEnable() => SaveManager.RosterLoaded += RestoreMapOnLoad;

    void OnDisable() => SaveManager.RosterLoaded -= RestoreMapOnLoad;

    // Al cargar una partida con recolección a medias, la escuadra tiene que volver al claro:
    // se guardó estando fuera, y sin esto el mapa quedaría vacío hasta la siguiente salida.
    private void RestoreMapOnLoad()
    {
        if (remaining <= 0f || readyToClaim) return;
        if (ExpeditionMap.Instance == null || party == null) return;

        ExpeditionMap.Instance.Open(new List<HeroController>(party.ExpeditionSquad), currentType);
    }
}
