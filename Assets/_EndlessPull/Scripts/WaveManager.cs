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

    [Tooltip("Orco tanque; sale en los pisos pares.")]
    [SerializeField] private EnemyData orcData;

    [Tooltip("Esqueleto rápido; sale a partir del piso 3.")]
    [SerializeField] private EnemyData skeletonData;

    [Tooltip("Chamán oscuro de ataque mágico a distancia; sale a partir del piso 3.")]
    [SerializeField] private EnemyData shamanData;

    [Tooltip("Segundos de preparación táctica antes de que los enemigos se muevan.")]
    [SerializeField] private float combatCountdown = 1.5f;

    [Tooltip("Piso más alto superado; manda sobre qué pisos se pueden elegir.")]
    [SerializeField] private int highestClearedFloor;

    [Tooltip("Parte de los materiales que da repetir un piso ya superado.")]
    [SerializeField] private float repeatMaterialFactor = 0.4f;

    [Tooltip("EXP por piso que reparte una victoria entre la escuadra.")]
    [SerializeField] private int expReward = 15;

    [Tooltip("Alcance a partir del cual un enemigo aparece más atrás, por ser de rango.")]
    [SerializeField] private float rangedSpawnThreshold = 3f;

    [Tooltip("Bonus de ATK y DEF por compartir origen con la escuadra, en tanto por uno.")]
    [SerializeField] private float originSynergyBonus = 0.05f;

    [Tooltip("Durabilidad que pierde cada pieza equipada por expedición.")]
    [SerializeField] private int wearPerExpedition = 1;

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

    [Tooltip("Origen de la arena; la base queda lejos para que no se mezclen las dos zonas.")]
    [SerializeField] private Vector2 arenaCenter = new Vector2(40f, 0f);

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

    [Tooltip("Centro de la zona de aparición, relativo al origen de la arena.")]
    [SerializeField] private Vector2 spawnAreaCenter = new Vector2(6f, 0f);

    [Tooltip("Ancho y alto de la zona de aparición.")]
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(2f, 3f);

    private readonly List<EnemyController> wave = new List<EnemyController>();
    private readonly List<HeroController> deployed = new List<HeroController>();
    private ExpeditionState state = ExpeditionState.Idle;

    // Preparación táctica: la oleada está en escena pero congelada.
    private float countdownTimer;

    // Se apunta al salir: si no hubo comida, la moral lo paga al volver.
    private bool underfed;
    private bool bossFloor;

    public int CurrentFloor => currentFloor;
    public int HighestClearedFloor => highestClearedFloor;
    public int HighestSelectableFloor => highestClearedFloor + 1;
    public bool IsCountingDown => countdownTimer > 0f;
    public ExpeditionState State => state;
    public int EnemyCountForFloor => baseEnemyCount + (currentFloor - 1);
    public bool IsBossFloor => bossEveryFloors > 0 && currentFloor % bossEveryFloors == 0;
    public float StatMultiplierForFloor => 1f + statGrowthPerFloor * (currentFloor - 1);

    // Punto medio entre la línea de la escuadra y la de los enemigos; ahí encuadra la cámara.
    public Vector2 ArenaFocus => arenaCenter + new Vector2(spawnAreaCenter.x * 0.5f, spawnAreaCenter.y);

    public int AliveEnemies
    {
        get { PruneWave(); return wave.Count; }
    }

    // Se dispara con (estado, mensaje) para que la UI muestre el feedback.
    public event System.Action<ExpeditionState, string> ExpeditionChanged;

    // Se dispara con el piso nuevo; evita que la UI dependa del orden de los Start.
    public event System.Action<int> FloorChanged;

    // La usa el SaveManager al cargar una partida.
    public void LoadProgress(int savedFloor, int savedHighest)
    {
        highestClearedFloor = Mathf.Max(0, savedHighest);
        currentFloor = Mathf.Clamp(savedFloor, 1, HighestSelectableFloor);
        PublishFloor();
    }

    // Los edificios miran el piso alcanzado para su capacidad y su desbloqueo.
    private void PublishFloor()
    {
        BaseBuilding.SetTowerFloor(Mathf.Max(currentFloor, highestClearedFloor + 1));
        FloorChanged?.Invoke(currentFloor);
    }

    // Selector de torre: se puede repetir cualquier piso superado, o intentar el siguiente.
    public bool SelectFloor(int floor)
    {
        if (state == ExpeditionState.InProgress)
        {
            Debug.LogWarning("[Torre] No se puede cambiar de piso con una expedición en curso.", this);
            return false;
        }

        if (floor < 1 || floor > HighestSelectableFloor) return false;

        currentFloor = floor;
        PublishFloor();
        return true;
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
            Vector2 pos = arenaCenter + spawnAreaCenter + new Vector2(
                Random.Range(-half.x, half.x),
                Random.Range(-half.y, half.y));

            var datos = PickEnemyData(i);

            // Los de rango aparecen más atrás, coherente con su alcance.
            if (datos != null && datos.attackRange >= rangedSpawnThreshold) pos += new Vector2(2f, 0f);

            var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
            go.name = $"Enemy_{SafeName(datos)}_F{currentFloor}_{i + 1}";

            var enemy = go.GetComponent<EnemyController>();
            enemy.Initialize(datos, mult);
            wave.Add(enemy);
        }

        // El jefe se suma a la oleada normal del piso.
        bossFloor = IsBossFloor && bossData != null;
        if (bossFloor) SpawnBoss();

        // Preparación táctica: la oleada ya está puesta, pero no se mueve hasta que pase la cuenta atrás.
        BeginCountdown();

        string extra = bossFloor ? " + JEFE" : string.Empty;
        Report(ExpeditionState.InProgress,
            $"Piso {currentFloor}: {count} enemigos{extra} (x{mult:0.00})  " +
            $"Intentos {party.Energy}/{party.MaxEnergy}");
    }

    // Composición por piso: el goblin es el relleno y el resto entra según a qué altura estemos.
    private EnemyData PickEnemyData(int index)
    {
        if (archerData != null && archerEveryNth > 0 && (index + 1) % archerEveryNth == 0) return archerData;

        // Los orcos aguantan: se reservan para los pisos pares.
        if (orcData != null && currentFloor % 2 == 0 && index % 3 == 0) return orcData;

        if (currentFloor >= 3)
        {
            if (shamanData != null && index % 5 == 4) return shamanData;
            if (skeletonData != null && index % 2 == 1) return skeletonData;
        }

        return enemyData;
    }

    private static string SafeName(EnemyData data)
        => data != null && !string.IsNullOrEmpty(data.enemyName)
            ? data.enemyName.Replace(" ", string.Empty)
            : "Enemy";

    // Congela la oleada y avisa; da tiempo a leer el campo antes de que empiece el follón.
    private void BeginCountdown()
    {
        countdownTimer = Mathf.Max(0f, combatCountdown);

        foreach (var enemy in wave)
            if (enemy != null) enemy.SetFrozen(countdownTimer > 0f);

        if (countdownTimer > 0f)
            ScreenBanner.Show(LocalizationManager.Get("UI_READY"), countdownTimer, new Color(1f, 0.85f, 0.35f));
    }

    private void ReleaseWave()
    {
        foreach (var enemy in wave)
            if (enemy != null) enemy.SetFrozen(false);

        ScreenBanner.Show(LocalizationManager.Get("UI_ENGAGE"), 1f, new Color(1f, 0.45f, 0.30f));
        Debug.Log("[Expedición] Fin de la preparación: la oleada se mueve.", this);
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
            hero.transform.position = arenaCenter + party.FormationSlot(slot);
            hero.SetDeployed(true);
            deployed.Add(hero);
            slot++;
        }

        ApplyOriginSynergy();
        Debug.Log($"[Expedición] Escuadra desplegada: {deployed.Count} héroe(s).", this);
    }

    // Compartir tierra natal con alguien de la escuadra da un empujón mientras dure el combate.
    private void ApplyOriginSynergy()
    {
        foreach (var hero in deployed)
        {
            if (hero == null || hero.Data == null) continue;

            bool acompanado = false;
            foreach (var other in deployed)
            {
                if (other == null || other == hero || other.Data == null) continue;
                if (other.Data.origin != hero.Data.origin) continue;

                acompanado = true;
                break;
            }

            hero.SetOriginSynergy(acompanado ? originSynergyBonus : 0f);
            if (acompanado)
                Debug.Log($"[Sinergia] {hero.Data.heroName} pelea junto a los suyos " +
                          $"({hero.Data.origin}): +{originSynergyBonus:P0} ATK/DEF.", this);
        }
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
        countdownTimer = 0f;

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
            hero.SetOriginSynergy(0f);
            hero.Status.Clear();
            hero.WearEquipment(wearPerExpedition);
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
        var go = Instantiate(enemyPrefab, arenaCenter + spawnAreaCenter + new Vector2(1.5f, 0f), Quaternion.identity);
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

        // Mientras dura la preparación no se pelea ni se resuelve nada.
        if (countdownTimer > 0f)
        {
            countdownTimer -= Time.deltaTime;
            if (countdownTimer > 0f) return;

            countdownTimer = 0f;
            ReleaseWave();
        }

        if (AliveEnemies == 0)
        {
            int cleared = currentFloor;

            // La primera vez paga gemas y abre el piso siguiente; repetir solo da EXP y algo de material.
            bool firstClear = cleared > highestClearedFloor;
            float factor = firstClear ? 1f : repeatMaterialFactor;

            int gemGain = firstClear ? floorReward : 0;
            int woodGain = Mathf.Max(1, Mathf.RoundToInt(woodReward * cleared * factor));
            int ironGain = Mathf.Max(1, Mathf.RoundToInt(ironReward * cleared * factor));

            if (gemGain > 0) economy?.Add(gemGain);
            economy?.AddMaterials(woodGain, ironGain);

            if (bossFloor && firstClear) GrantBossChest();
            bossFloor = false;

            GrantCombatExp(cleared);

            // Ganar sube la moral de todo el que siga en pie.
            foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
                hero.AddMorale(moraleRewardOnWin);

            RecallParty();

            if (firstClear)
            {
                highestClearedFloor = cleared;
                currentFloor = cleared + 1;
            }

            PublishFloor();

            string modo = firstClear
                ? LocalizationManager.Get("UI_FIRST_CLEAR")
                : LocalizationManager.Get("UI_REPEAT");

            Report(ExpeditionState.Won,
                $"Piso {cleared} superado ({modo})  +{gemGain} gemas, +{woodGain} madera, +{ironGain} hierro");
            return;
        }

        // Se pierde cuando cae toda la escuadra, no cuando cae todo el roster.
        if (CountAliveDeployed() == 0)
        {
            DespawnWave();
            RecallParty();
            bossFloor = false;
            countdownTimer = 0f;
            Report(ExpeditionState.Lost, $"Expedición fallida en el piso {currentFloor}");
        }
    }

    // La EXP la reparte el piso, y la cobran los que estaban peleando aunque hayan caído después.
    private void GrantCombatExp(int clearedFloor)
    {
        int amount = Mathf.Max(1, expReward * clearedFloor);
        int cobraron = 0;

        foreach (var hero in deployed)
        {
            if (hero == null) continue;

            var progress = hero.GetComponent<HeroProgress>();
            if (progress == null) continue;

            progress.AddEXP(amount);
            cobraron++;
        }

        if (cobraron > 0) Debug.Log($"[Expedición] +{amount} EXP para {cobraron} héroe(s).", this);
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
        Gizmos.DrawWireCube(arenaCenter + spawnAreaCenter, spawnAreaSize);
    }
}
