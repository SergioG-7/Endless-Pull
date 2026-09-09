using System.Collections.Generic;
using UnityEngine;

// Contratos del Maestro; cada uno mira un contador distinto del juego.
// Los valores nuevos van SIEMPRE al final: el guardado serializa el numero, no el nombre.
public enum QuestKind
{
    ReachFloor,
    HeroLevel,
    AscendHero,
    AssignWorkers,
    RepairGear,
    CureFatigue,
    KillEnemies,
    ClearFloors,
    SummonHero,
    CompleteExpedition,
    UpgradeBuilding,
    WinBossFloor,
    ForgeGear,

    // Siempre al final: el save y el YAML guardan el numero, no el nombre.
    AwakenSkill,
    WinNoLosses,
    WinHiddenChallenge,
    CraftPotion,
    HarvestFood,
    ForgeBond
}

[System.Serializable]
public class Quest
{
    public QuestKind kind;
    public int target;
    public int rewardGems;
    public int rewardWood;
    public int rewardIron;
    public int rewardFood;
    public bool claimed;

    // Contador acumulado de lo que no se puede leer del estado actual (reparaciones, curas).
    public int progress;

    // Los hitos se cobran una vez y se quedan; los demas se renuevan al cobrarlos.
    public bool milestone;
}

// Tablon de contratos: mide el progreso y entrega la recompensa.
// Los hitos son de un solo uso; los contratos rotativos se renuevan al cobrarse.
public class QuestManager : MonoBehaviour
{
    [Tooltip("Economía a la que van las recompensas.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Gestor de oleadas del que se lee el piso alcanzado.")]
    [SerializeField] private WaveManager waves;

    [Tooltip("Cuántos contratos rotativos hay activos a la vez.")]
    [SerializeField] private int rotatingSlots = 3;

    [Tooltip("Hitos de un solo uso; se leen del estado del mundo, no de un contador.")]
    [SerializeField]
    private List<Quest> quests = new List<Quest>
    {
        new Quest { kind = QuestKind.ReachFloor,    target = 5,  rewardGems = 300, milestone = true },
        new Quest { kind = QuestKind.HeroLevel,     target = 10, rewardGems = 200, rewardWood = 80,  milestone = true },
        new Quest { kind = QuestKind.AscendHero,    target = 3,  rewardGems = 400, rewardIron = 60,  milestone = true },
        new Quest { kind = QuestKind.AssignWorkers, target = 2,  rewardWood = 150, rewardIron = 100, milestone = true }
    };

    public IReadOnlyList<Quest> Quests => quests;

    // Se dispara al reclamar o al cambiar un contador; el tablón se repinta con esto.
    public event System.Action QuestsChanged;

    private static QuestManager instance;

    // Plantilla de contrato rotativo: objetivo y pago base, escalados luego por piso.
    private struct Template
    {
        public QuestKind kind;
        public int baseTarget;
        public int gems, wood, iron, food;

        public Template(QuestKind kind, int baseTarget, int gems, int wood, int iron, int food)
        {
            this.kind = kind; this.baseTarget = baseTarget;
            this.gems = gems; this.wood = wood; this.iron = iron; this.food = food;
        }
    }

    private static readonly Template[] Pool =
    {
        new Template(QuestKind.KillEnemies,       120,  90,   0,   0,  40),
        new Template(QuestKind.ClearFloors,         8, 110,  60,   0,   0),
        new Template(QuestKind.RepairGear,          5,  70,   0,  70,   0),
        new Template(QuestKind.CureFatigue,       400,  60,   0,   0,  60),
        new Template(QuestKind.SummonHero,          3,  70,  50,  50,   0),
        new Template(QuestKind.CompleteExpedition,  2, 100,  80,  40,   0),
        new Template(QuestKind.UpgradeBuilding,     1, 120, 100,  60,   0),
        new Template(QuestKind.WinBossFloor,        2, 180,   0, 110,   0),
        new Template(QuestKind.ForgeGear,           3,  85,   0,  90,   0),

        // Contratos que piden jugar de una manera concreta, no solo repetir la misma accion.
        new Template(QuestKind.AwakenSkill,         2, 130,   0,  40,   0),
        new Template(QuestKind.WinNoLosses,         3, 140,  70,   0,   0),
        new Template(QuestKind.WinHiddenChallenge,  2, 200,   0,  80,   0),
        new Template(QuestKind.CraftPotion,         6,  60,  60,   0,  40),
        new Template(QuestKind.HarvestFood,       400,  70,  70,   0,   0),
        new Template(QuestKind.ForgeBond,           2, 110,   0,   0,  80)
    };

    // Catalogo de hitos. La lista de arriba solo sirve para una escena sin guardar: el save
    // reemplaza `quests` entera, asi que los hitos que falten se anaden desde aqui y llegan
    // tambien a las partidas empezadas.
    private static readonly Quest[] MilestoneCatalog =
    {
        new Quest { kind = QuestKind.ReachFloor,    target = 5,  rewardGems = 300,  milestone = true },
        new Quest { kind = QuestKind.ReachFloor,    target = 20, rewardGems = 700,  rewardIron = 150, milestone = true },
        new Quest { kind = QuestKind.ReachFloor,    target = 40, rewardGems = 1500, rewardIron = 300, milestone = true },
        new Quest { kind = QuestKind.ReachFloor,    target = 100, rewardGems = 5000, rewardIron = 1000, milestone = true },
        new Quest { kind = QuestKind.HeroLevel,     target = 10, rewardGems = 200,  rewardWood = 80,  milestone = true },
        new Quest { kind = QuestKind.HeroLevel,     target = 30, rewardGems = 600,  rewardWood = 250, milestone = true },
        new Quest { kind = QuestKind.AscendHero,    target = 3,  rewardGems = 400,  rewardIron = 60,  milestone = true },
        new Quest { kind = QuestKind.AscendHero,    target = 5,  rewardGems = 900,  rewardIron = 200, milestone = true },
        new Quest { kind = QuestKind.AssignWorkers, target = 2,  rewardWood = 150,  rewardIron = 100, milestone = true },
        new Quest { kind = QuestKind.AssignWorkers, target = 10, rewardWood = 400,  rewardIron = 300, milestone = true }
    };

    // Anade los hitos del catalogo que no esten ya en el tablon, cobrados o no.
    private void EnsureMilestones()
    {
        foreach (var plantilla in MilestoneCatalog)
        {
            bool existe = false;
            foreach (var quest in quests)
            {
                if (quest == null || !quest.milestone) continue;
                if (quest.kind != plantilla.kind || quest.target != plantilla.target) continue;

                existe = true;
                break;
            }

            if (existe) continue;

            quests.Add(new Quest
            {
                kind = plantilla.kind,
                target = plantilla.target,
                rewardGems = plantilla.rewardGems,
                rewardWood = plantilla.rewardWood,
                rewardIron = plantilla.rewardIron,
                rewardFood = plantilla.rewardFood,
                milestone = true
            });
        }
    }

    void Awake()
    {
        instance = this;
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        if (waves == null) waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
    }

    // El save se restaura antes; aquí solo se rellenan los huecos que falten.
    void Start()
    {
        EnsureMilestones();
        FillRotatingSlots();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Los contadores que no se pueden deducir del estado los avisa quien los provoca.
    public static void Report(QuestKind kind, int amount = 1)
    {
        if (instance == null || amount <= 0) return;

        bool cambio = false;
        foreach (var quest in instance.quests)
        {
            if (quest.kind != kind || quest.claimed) continue;
            if (!UsesCounter(kind)) continue;

            quest.progress += amount;
            cambio = true;
        }

        if (cambio) instance.QuestsChanged?.Invoke();
    }

    // Los hitos leen el mundo; los rotativos, un contador que sube con lo que hace el jugador.
    private static bool UsesCounter(QuestKind kind)
    {
        switch (kind)
        {
            case QuestKind.ReachFloor:
            case QuestKind.HeroLevel:
            case QuestKind.AscendHero:
            case QuestKind.AssignWorkers:
                return false;
            default:
                return true;
        }
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

        if (economy != null)
        {
            if (quest.rewardGems > 0) economy.Add(quest.rewardGems);
            if (quest.rewardWood > 0 || quest.rewardIron > 0)
                economy.AddMaterials(quest.rewardWood, quest.rewardIron);
            if (quest.rewardFood > 0) economy.AddFood(quest.rewardFood);
        }

        Debug.Log($"[Tablon] Contrato '{Describe(quest)}' cobrado: {quest.rewardGems} gemas, " +
                  $"{quest.rewardWood} madera, {quest.rewardIron} hierro, {quest.rewardFood} comida.", this);

        // El hito se queda cobrado; el rotativo libera su hueco y entra uno nuevo.
        if (quest.milestone) quest.claimed = true;
        else quests.Remove(quest);

        FillRotatingSlots();

        QuestsChanged?.Invoke();
        SaveManager.RequestSave();
        return true;
    }

    // Rellena los huecos rotativos evitando repetir un tipo que ya esté en el tablón.
    private void FillRotatingSlots()
    {
        int activos = 0;
        foreach (var quest in quests) if (!quest.milestone) activos++;

        int intentos = 0;
        while (activos < rotatingSlots && intentos < 60)
        {
            intentos++;
            var plantilla = Pool[Random.Range(0, Pool.Length)];

            bool repetido = false;
            foreach (var quest in quests)
                if (!quest.milestone && quest.kind == plantilla.kind) { repetido = true; break; }

            if (repetido) continue;

            quests.Add(Build(plantilla, ScaleTier()));
            activos++;
        }
    }

    // Los contratos escalan con el piso más alto limpiado: más exigentes y mejor pagados.
    private int ScaleTier()
    {
        int piso = waves != null ? waves.HighestClearedFloor : 0;
        return Mathf.Clamp(piso / 10, 0, 8);
    }

    private static Quest Build(Template plantilla, int tier)
    {
        float objetivo = 1f + tier * 0.5f;
        float pago = 1f + tier * 0.45f;

        return new Quest
        {
            kind = plantilla.kind,
            target = Mathf.Max(1, Mathf.RoundToInt(plantilla.baseTarget * objetivo)),
            rewardGems = Mathf.RoundToInt(plantilla.gems * pago),
            rewardWood = Mathf.RoundToInt(plantilla.wood * pago),
            rewardIron = Mathf.RoundToInt(plantilla.iron * pago),
            rewardFood = Mathf.RoundToInt(plantilla.food * pago),
            milestone = false
        };
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
            case QuestKind.KillEnemies: clave = "Q_KILL"; break;
            case QuestKind.ClearFloors: clave = "Q_CLEAR"; break;
            case QuestKind.SummonHero: clave = "Q_SUMMON"; break;
            case QuestKind.CompleteExpedition: clave = "Q_EXPEDITION"; break;
            case QuestKind.UpgradeBuilding: clave = "Q_BUILDING"; break;
            case QuestKind.WinBossFloor: clave = "Q_BOSS"; break;
            case QuestKind.ForgeGear: clave = "Q_FORGE"; break;
            case QuestKind.AwakenSkill: clave = "Q_AWAKEN"; break;
            case QuestKind.WinNoLosses: clave = "Q_NOLOSS"; break;
            case QuestKind.WinHiddenChallenge: clave = "Q_CHALLENGE"; break;
            case QuestKind.CraftPotion: clave = "Q_POTION"; break;
            case QuestKind.HarvestFood: clave = "Q_HARVEST"; break;
            case QuestKind.ForgeBond: clave = "Q_BOND"; break;
            default: clave = "Q_RESTED"; break;
        }

        return LocalizationManager.Get(clave).Replace("{0}", quest.target.ToString());
    }

    // --- Guardado -----------------------------------------------------------------------

    public List<QuestSaveData> Capture()
    {
        var salida = new List<QuestSaveData>();
        foreach (var quest in quests)
        {
            if (quest == null) continue;
            salida.Add(new QuestSaveData
            {
                kind = (int)quest.kind,
                target = quest.target,
                rewardGems = quest.rewardGems,
                rewardWood = quest.rewardWood,
                rewardIron = quest.rewardIron,
                rewardFood = quest.rewardFood,
                claimed = quest.claimed,
                progress = quest.progress,
                milestone = quest.milestone
            });
        }

        return salida;
    }

    // Una partida vieja no trae contratos: se deja el tablón por defecto en vez de vaciarlo.
    public void Restore(List<QuestSaveData> guardados)
    {
        if (guardados == null || guardados.Count == 0) return;

        quests.Clear();
        foreach (var dato in guardados)
        {
            if (dato == null) continue;
            quests.Add(new Quest
            {
                kind = (QuestKind)dato.kind,
                target = dato.target,
                rewardGems = dato.rewardGems,
                rewardWood = dato.rewardWood,
                rewardIron = dato.rewardIron,
                rewardFood = dato.rewardFood,
                claimed = dato.claimed,
                progress = dato.progress,
                milestone = dato.milestone
            });
        }

        EnsureMilestones();
        FillRotatingSlots();
        QuestsChanged?.Invoke();
    }

    // --- Lectura del mundo --------------------------------------------------------------

    // Incluye inactivos: un héroe en expedición o en la Torre sigue contando para el hito.
    private static int MaxHeroLevel()
    {
        int mejor = 0;
        foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroProgress>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (hero != null && hero.Level > mejor) mejor = hero.Level;

        return mejor;
    }

    private static int MaxStarRank()
    {
        int mejor = 0;
        foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
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
