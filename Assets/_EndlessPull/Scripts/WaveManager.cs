using System.Collections.Generic;
using UnityEngine;

// Estado de la expedición del piso actual.
public enum ExpeditionState
{
    Idle,
    InProgress,
    Won,
    Lost
}

public class WaveManager : MonoBehaviour
{
    [Tooltip("Prefab de enemigo que se usa para poblar la oleada.")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Datos base del enemigo, antes de escalar por piso.")]
    [SerializeField] private EnemyData enemyData;

    [Tooltip("Economía a la que se abona la recompensa del piso.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Escuadra que sube a la torre; solo estos héroes combaten.")]
    [SerializeField] private PartyManager party;

    [Tooltip("Datos del jefe que aparece en los pisos marcados.")]
    [SerializeField] private EnemyData bossData;

    [Tooltip("Datos del tirador que se queda en retaguardia.")]
    [SerializeField] private EnemyData archerData;

    [Tooltip("Uno de cada cuántos enemigos de la oleada es tirador.")]
    [SerializeField] private int archerEveryNth = 3;

    [Tooltip("Moral que pierde la escuadra al ordenar retirada.")]
    [SerializeField] private float retreatMoraleLoss = 10f;

    [Tooltip("Cada cuántos pisos toca jefe.")]
    [SerializeField] private int bossEveryFloors = 5;

    [Tooltip("Escala visual del jefe frente a un enemigo normal.")]
    [SerializeField] private float bossScale = 1.8f;

    [Tooltip("Multiplicador sobre las stats del asset del jefe; a 1 manda el asset tal cual.")]
    [SerializeField] private float bossStatMultiplier = 1f;

    [Tooltip("Comida que consume cada expedición.")]
    [SerializeField] private int foodPerExpedition = 10;

    [Tooltip("Moral que pierden los héroes al volver sin haber comido.")]
    [SerializeField] private float malnutritionMoraleLoss = 15f;

    [Tooltip("Gemas extra del cofre que suelta el jefe.")]
    [SerializeField] private int bossChestGems = 300;

    [Tooltip("Madera y hierro extra del cofre que suelta el jefe.")]
    [SerializeField] private int bossChestMaterials = 60;

    [Tooltip("Punto al que se despliega la escuadra al empezar la expedición.")]
    [SerializeField] private Vector2 deployPoint = new Vector2(3f, 0f);

    [Tooltip("Piso en el que está la expedición ahora mismo.")]
    [SerializeField] private int currentFloor = 1;

    [Tooltip("Enemigos del piso 1; cada piso suma uno más.")]
    [SerializeField] private int baseEnemyCount = 2;

    [Tooltip("Incremento de vida y ataque por cada piso superado.")]
    [SerializeField] private float statGrowthPerFloor = 0.2f;

    [Tooltip("Gemas que da superar un piso.")]
    [SerializeField] private int floorReward = 100;

    [Tooltip("Moral que gana cada héroe vivo al superar un piso.")]
    [SerializeField] private float moraleRewardOnWin = 10f;

    [Tooltip("Madera que da superar el piso 1; escala con el piso.")]
    [SerializeField] private int woodReward = 20;

    [Tooltip("Hierro que da superar el piso 1; escala con el piso.")]
    [SerializeField] private int ironReward = 10;

    [Tooltip("Centro de la zona de aparición, a la derecha de la arena.")]
    [SerializeField] private Vector2 spawnAreaCenter = new Vector2(6f, 0f);

    [Tooltip("Ancho y alto de la zona de aparición.")]
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(2f, 3f);

    private readonly List<EnemyController> wave = new List<EnemyController>();
    private readonly List<HeroController> deployed = new List<HeroController>();
    private ExpeditionState state = ExpeditionState.Idle;

    // Se apunta al salir: si no hubo comida, la moral lo paga al volver.
    private bool underfed;
    private bool bossFloor;

    public int CurrentFloor => currentFloor;
    public ExpeditionState State => state;
    public int EnemyCountForFloor => baseEnemyCount + (currentFloor - 1);
    public bool IsBossFloor => bossEveryFloors > 0 && currentFloor % bossEveryFloors == 0;
    public float StatMultiplierForFloor => 1f + statGrowthPerFloor * (currentFloor - 1);

    public int AliveEnemies
    {
        get { PruneWave(); return wave.Count; }
    }

    // Se dispara con (estado, mensaje) para que la UI muestre el feedback.
    public event System.Action<ExpeditionState, string> ExpeditionChanged;

    // Se dispara con el piso nuevo; evita que la UI dependa del orden de los Start.
    public event System.Action<int> FloorChanged;

    // La usa el SaveManager al cargar una partida.
    public void LoadFloor(int savedFloor)
    {
        currentFloor = Mathf.Max(1, savedFloor);
        FloorChanged?.Invoke(currentFloor);
    }

    void Awake()
    {
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        if (party == null) party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();
    }

    public void StartFloorExpedition()
    {
        if (state == ExpeditionState.InProgress)
        {
            Debug.LogWarning("[Expedición] Ya hay una en curso.", this);
            return;
        }

        if (enemyPrefab == null || enemyData == null)
        {
            Debug.LogError("[Expedición] Falta el prefab o los datos del enemigo.", this);
            return;
        }

        if (party == null || party.Party.Count == 0)
        {
            Report(ExpeditionState.Idle, "Asigna héroes a la escuadra");
            return;
        }

        if (!party.TryConsumeEnergy())
        {
            Report(ExpeditionState.Idle, $"Sin intentos de torre ({party.Energy}/{party.MaxEnergy})");
            return;
        }

        // La comida se cobra al salir; sin despensa la expedición sale igual, pero pasa factura.
        underfed = economy == null || !economy.TrySpendFood(foodPerExpedition);
        if (underfed) Debug.LogWarning("[Expedición] Sin comida: la escuadra volverá desnutrida.", this);

        DespawnWave();
        DeployParty();

        int count = EnemyCountForFloor;
        float mult = StatMultiplierForFloor;

        for (int i = 0; i < count; i++)
        {
            Vector2 half = spawnAreaSize * 0.5f;
            Vector2 pos = spawnAreaCenter + new Vector2(
                Random.Range(-half.x, half.x),
                Random.Range(-half.y, half.y));

            // Uno de cada tres sale tirador: obliga a priorizar objetivos en vez de pegar al más cercano.
            bool esTirador = archerData != null && archerEveryNth > 0 && (i + 1) % archerEveryNth == 0;
            var datos = esTirador ? archerData : enemyData;

            // Los tiradores aparecen más atrás, coherente con su alcance.
            if (esTirador) pos += new Vector2(2f, 0f);

            var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
            go.name = esTirador
                ? $"Enemy_Archer_F{currentFloor}_{i + 1}"
                : $"Enemy_F{currentFloor}_{i + 1}";

            var enemy = go.GetComponent<EnemyController>();
            enemy.Initialize(datos, mult);
            wave.Add(enemy);
        }

        // El jefe se suma a la oleada normal del piso.
        bossFloor = IsBossFloor && bossData != null;
        if (bossFloor) SpawnBoss();

        string extra = bossFloor ? " + JEFE" : string.Empty;
        Report(ExpeditionState.InProgress,
            $"Piso {currentFloor}: {count} enemigos{extra} (x{mult:0.00})  " +
            $"Intentos {party.Energy}/{party.MaxEnergy}");
    }

    // Solo la escuadra viaja a la torre; el resto se queda en la base.
    private void DeployParty()
    {
        deployed.Clear();

        int slot = 0;
        foreach (var hero in party.Party)
        {
            if (hero == null) continue;

            // Cada puesto tiene su sitio: apilados, el golpe circular del jefe se los lleva a todos.
            hero.transform.position = party.FormationSlot(slot);
            hero.SetDeployed(true);
            deployed.Add(hero);
            slot++;
        }

        Debug.Log($"[Expedición] Escuadra desplegada: {deployed.Count} héroe(s).", this);
    }

    // Retirada de emergencia: se cancela el piso, la escuadra vuelve viva y sin premio.
    public bool RetreatExpedition()
    {
        if (state != ExpeditionState.InProgress)
        {
            Debug.LogWarning("[Retirada] No hay ninguna expedición en curso.", this);
            return false;
        }

        int rescatados = CountAliveDeployed();

        foreach (var hero in deployed)
            if (hero != null) hero.LoseMorale(retreatMoraleLoss);

        DespawnWave();
        RecallParty();
        bossFloor = false;

        Report(ExpeditionState.Idle,
            $"Retirada del piso {currentFloor}: {rescatados} héroe(s) a salvo, sin recompensa");
        return true;
    }

    // Los devuelve a la base y les quita el estado de combate.
    private void RecallParty()
    {
        foreach (var hero in deployed)
        {
            if (hero == null) continue;

            hero.SetDeployed(false);
            if (underfed) hero.LoseMorale(malnutritionMoraleLoss);
        }

        if (underfed && deployed.Count > 0)
            Debug.LogWarning($"[Expedición] Desnutrición: -{malnutritionMoraleLoss} moral a la escuadra.", this);

        deployed.Clear();
        underfed = false;
    }

    // El jefe no escala con el piso: sus números son los del asset, para poder ajustarlo a mano.
    private void SpawnBoss()
    {
        var go = Instantiate(enemyPrefab, spawnAreaCenter + new Vector2(1.5f, 0f), Quaternion.identity);
        go.name = $"Enemy_Boss_F{currentFloor}";

        var boss = go.GetComponent<EnemyController>();
        boss.Initialize(bossData, bossStatMultiplier);
        boss.MakeBoss(bossScale);
        wave.Add(boss);

        Debug.Log($"[Jefe] {bossData.enemyName} aparece en el piso {currentFloor} " +
                  $"con {boss.MaxHealth} PV.", this);
    }

    // Botín garantizado por tumbar al jefe.
    private void GrantBossChest()
    {
        economy?.Add(bossChestGems);
        economy?.AddMaterials(bossChestMaterials, bossChestMaterials);

        Debug.Log($"[Cofre] Botín del jefe: +{bossChestGems} gemas, " +
                  $"+{bossChestMaterials} madera y +{bossChestMaterials} hierro.", this);
    }

    void Update()
    {
        if (state != ExpeditionState.InProgress) return;

        if (AliveEnemies == 0)
        {
            int cleared = currentFloor;
            int woodGain = woodReward * cleared;
            int ironGain = ironReward * cleared;

            economy?.Add(floorReward);
            economy?.AddMaterials(woodGain, ironGain);

            if (bossFloor) GrantBossChest();
            bossFloor = false;

            // Ganar sube la moral de todo el que siga en pie.
            foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
                hero.AddMorale(moraleRewardOnWin);

            RecallParty();

            currentFloor++;
            FloorChanged?.Invoke(currentFloor);
            Report(ExpeditionState.Won,
                $"Piso {cleared} superado  +{floorReward} gemas, +{woodGain} madera, +{ironGain} hierro");
            return;
        }

        // Se pierde cuando cae toda la escuadra, no cuando cae todo el roster.
        if (CountAliveDeployed() == 0)
        {
            DespawnWave();
            RecallParty();
            bossFloor = false;
            Report(ExpeditionState.Lost, $"Expedición fallida en el piso {currentFloor}");
        }
    }

    private int CountAliveDeployed()
    {
        int alive = 0;
        foreach (var hero in deployed)
            if (hero != null) alive++;

        return alive;
    }

    private int CountAliveHeroes()
        => UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None).Length;

    // Quita de la lista los enemigos ya destruidos.
    private void PruneWave()
    {
        for (int i = wave.Count - 1; i >= 0; i--)
            if (wave[i] == null) wave.RemoveAt(i);
    }

    private void DespawnWave()
    {
        foreach (var e in wave)
            if (e != null) Destroy(e.gameObject);

        wave.Clear();
    }

    private void Report(ExpeditionState newState, string message)
    {
        state = newState;
        Debug.Log($"[Expedición] {message}", this);
        ExpeditionChanged?.Invoke(state, message);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
    }
}
