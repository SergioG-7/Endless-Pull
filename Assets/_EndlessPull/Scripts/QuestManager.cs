using System.Collections.Generic;
using UnityEngine;

// Contratos del Maestro; cada uno mira un contador distinto del juego.
public enum QuestKind
{
    ReachFloor,
    HeroLevel,
    AscendHero,
    AssignWorkers,
    RepairGear,
    CureFatigue
}

[System.Serializable]
public class Quest
{
    public QuestKind kind;
    public int target;
    public int rewardGems;
    public int rewardWood;
    public int rewardIron;
    public bool claimed;

    // Contador acumulado de lo que no se puede leer del estado actual (reparaciones, curas).
    public int progress;
}

// Tablón de contratos: mide el progreso y entrega la recompensa una sola vez.
public class QuestManager : MonoBehaviour
{
    [Tooltip("Economía a la que van las recompensas.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Gestor de oleadas del que se lee el piso alcanzado.")]
    [SerializeField] private WaveManager waves;

    [Tooltip("Contratos disponibles en el tablón.")]
    [SerializeField]
    private List<Quest> quests = new List<Quest>
    {
        new Quest { kind = QuestKind.ReachFloor,    target = 5,  rewardGems = 300, rewardWood = 0,   rewardIron = 0 },
        new Quest { kind = QuestKind.HeroLevel,     target = 10, rewardGems = 200, rewardWood = 80,  rewardIron = 0 },
        new Quest { kind = QuestKind.AscendHero,    target = 3,  rewardGems = 400, rewardWood = 0,   rewardIron = 60 },
        new Quest { kind = QuestKind.AssignWorkers, target = 2,  rewardGems = 0,   rewardWood = 150, rewardIron = 100 },
        new Quest { kind = QuestKind.RepairGear,    target = 3,  rewardGems = 150, rewardWood = 0,   rewardIron = 80 },
        new Quest { kind = QuestKind.CureFatigue,   target = 1,  rewardGems = 100, rewardWood = 60,  rewardIron = 0 }
    };

    public IReadOnlyList<Quest> Quests => quests;

    // Se dispara al reclamar o al cambiar un contador; el tablón se repinta con esto.
    public event System.Action QuestsChanged;

    private static QuestManager instance;

    void Awake()
    {
        instance = this;
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        if (waves == null) waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Los contadores que no se pueden deducir del estado los avisa quien los provoca.
    public static void Report(QuestKind kind, int amount = 1)
    {
        if (instance == null) return;

        bool cambio = false;
        foreach (var quest in instance.quests)
        {
            if (quest.kind != kind || quest.claimed) continue;

            quest.progress += amount;
            cambio = true;
        }

        if (cambio) instance.QuestsChanged?.Invoke();
    }

    // Progreso actual: unos se leen del mundo y otros del contador acumulado.
    public int ProgressOf(Quest quest)
    {
        if (quest == null) return 0;

        switch (quest.kind)
        {
            case QuestKind.ReachFloor:
                return waves != null ? waves.HighestClearedFloor : 0;

            case QuestKind.HeroLevel:
                return MaxHeroLevel();

            case QuestKind.AscendHero:
                return MaxStarRank();

            case QuestKind.AssignWorkers:
                return AssignedWorkers();
        }

        return quest.progress;
    }

    public bool IsComplete(Quest quest)
        => quest != null && ProgressOf(quest) >= quest.target;

    public bool CanClaim(Quest quest)
        => quest != null && !quest.claimed && IsComplete(quest);

    public bool TryClaim(Quest quest)
    {
        if (!CanClaim(quest)) return false;

        quest.claimed = true;

        if (economy != null)
        {
            if (quest.rewardGems > 0) economy.Add(quest.rewardGems);
            if (quest.rewardWood > 0 || quest.rewardIron > 0)
                economy.AddMaterials(quest.rewardWood, quest.rewardIron);
        }

        Debug.Log($"[Tablón] Contrato '{Describe(quest)}' cobrado: " +
                  $"{quest.rewardGems} gemas, {quest.rewardWood} madera, {quest.rewardIron} hierro.", this);

        QuestsChanged?.Invoke();
        SaveManager.RequestSave();
        return true;
    }

    // Texto del contrato con su cifra objetivo ya sustituida.
    public static string Describe(Quest quest)
    {
        if (quest == null) return string.Empty;

        string clave;
        switch (quest.kind)
        {
            case QuestKind.ReachFloor: clave = "Q_FLOOR"; break;
            case QuestKind.HeroLevel: clave = "Q_LEVEL"; break;
            case QuestKind.AscendHero: clave = "Q_ASCEND"; break;
            case QuestKind.AssignWorkers: clave = "Q_WORKERS"; break;
            case QuestKind.RepairGear: clave = "Q_REPAIR"; break;
            default: clave = "Q_RESTED"; break;
        }

        return LocalizationManager.Get(clave).Replace("{0}", quest.target.ToString());
    }

    private static int MaxHeroLevel()
    {
        int mejor = 0;
        foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroProgress>(FindObjectsSortMode.None))
            if (hero != null && hero.Level > mejor) mejor = hero.Level;

        return mejor;
    }

    private static int MaxStarRank()
    {
        int mejor = 0;
        foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
            if (hero != null && hero.StarRank > mejor) mejor = hero.StarRank;

        return mejor;
    }

    private static int AssignedWorkers()
    {
        int total = 0;
        foreach (var building in BaseBuilding.All)
            if (building != null) total += building.Workers.Count;

        return total;
    }
}
