using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Desglose de la recompensa de un piso superado; lo consume el modal de cofre.
public struct FloorRewardInfo
{
    public int floor;
    public bool firstClear;
    public bool bossFloor;
    public int gems;
    public int wood;
    public int iron;
    public int exp;
}

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

    [Tooltip("Segundos de preparación táctica antes de que empiece el combate (cuenta atrás 3-2-1-¡Lucha!).")]
    [SerializeField] private float combatCountdown = 3f;

    [Tooltip("Piso más alto superado; manda sobre qué pisos se pueden elegir.")]
    [SerializeField] private int highestClearedFloor;

    [Tooltip("Parte de los materiales que da repetir un piso ya superado.")]
    [SerializeField] private float repeatMaterialFactor = 0.4f;

    [Tooltip("EXP por piso que reparte una victoria entre la escuadra.")]
    [SerializeField] private int expReward = 15;

    [Tooltip("Alcance a partir del cual un enemigo aparece más atrás, por ser de rango.")]
    [SerializeField] private float rangedSpawnThreshold = 3f;

    [Tooltip("Piso a partir del cual los enemigos pegan mucho más fuerte.")]
    [SerializeField] private int hardFloorFrom = 4;

    [Tooltip("Ataque extra por cada piso desde el piso duro, en tanto por uno.")]
    [SerializeField] private float hardAttackGrowth = 0.18f;

    [Tooltip("Vida extra por cada piso desde el piso duro, en tanto por uno.")]
    [SerializeField] private float hardHealthGrowth = 0.10f;

    [Tooltip("Separación entre la línea de vanguardia y la de retaguardia enemiga.")]
    [SerializeField] private float lineSpacing = 2.2f;

    [Tooltip("Vida a partir de la cual un enemigo se considera tanque y va delante.")]
    [SerializeField] private int tankHealthThreshold = 70;

    [Tooltip("Bonus de ATK y DEF por compartir origen con la escuadra, en tanto por uno.")]
    [SerializeField] private float originSynergyBonus = 0.05f;

    [Tooltip("Durabilidad que pierde cada pieza equipada por expedición.")]
    [SerializeField] private int wearPerExpedition = 1;

    [Tooltip("Máximo de héroes que aterrizan en el Portal al volver; el resto va directo a su zona.")]
    [SerializeField] private int gatewayVisibleCap = 6;

    [Tooltip("Uno de cada cuántos enemigos de la oleada es tirador.")]
    [SerializeField] private int archerEveryNth = 3;

    [Tooltip("Moral que pierde la escuadra al ordenar retirada.")]
    [SerializeField] private float retreatMoraleLoss = 10f;

    [Tooltip("Cada cuántos pisos toca jefe.")]
    [SerializeField] private int bossEveryFloors = 5;

    [Tooltip("Escala visual del jefe frente a un enemigo normal.")]
    [SerializeField] private float bossScale = 1.8f;

    [Tooltip("Piso al que están calibradas las stats base del jefe; ahí sus multiplicadores valen 1.")]
    [Min(1)] [SerializeField] private int bossCalibrationFloor = 5;

    [Tooltip("Comida que consume cada expedición.")]
    [SerializeField] private int foodPerExpedition = 10;

    [Tooltip("Moral que pierden los héroes al volver sin haber comido.")]
    [SerializeField] private float malnutritionMoraleLoss = 15f;

    [Tooltip("Gemas extra del cofre que suelta el jefe.")]
    [SerializeField] private int bossChestGems = 300;

    [Tooltip("Madera y hierro extra del cofre que suelta el jefe.")]
    [SerializeField] private int bossChestMaterials = 60;

    [Tooltip("Taller del que sale la Piedra de Ascensión que suelta el cofre de jefe.")]
    [SerializeField] private CraftingManager crafting;

    [Tooltip("Último piso de cada tier en el cofre de jefe (Menor, Media, Mayor); por encima cae Legendaria.")]
    [SerializeField] private int[] stoneTierFloorCap = { 5, 10, 15 };

    [Tooltip("Segundos de pausa dramática antes de la cuenta atrás cuando aparece el jefe.")]
    [SerializeField] private float bossArrivalPause = 0.5f;

    [Tooltip("Segundos que se enseña el aviso '¡JEFE!' en pantalla.")]
    [SerializeField] private float bossArrivalBannerSeconds = 1.2f;

    [Tooltip("Origen de la arena; la base queda lejos para que no se mezclen las dos zonas.")]
    [SerializeField] private Vector2 arenaCenter = new Vector2(40f, 0f);

    [Tooltip("Desplazamiento de la formación de héroes respecto al origen de la arena; los deja en el extremo izquierdo, lejos de los enemigos, para que las unidades a distancia tengan hueco real de tiro.")]
    [SerializeField] private Vector2 heroSpawnOffset = new Vector2(-9.5f, 0f);

    [Tooltip("Medio ancho/alto del muro de la arena; cualquier proyectil que lo cruce se destruye para no escapar hacia la base.")]
    [SerializeField] private Vector2 arenaWallHalfExtents = new Vector2(15f, 7f);

    public static Vector2 ArenaWallMin { get; private set; }
    public static Vector2 ArenaWallMax { get; private set; }



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

    [Tooltip("Centro de la zona de aparición, relativo al origen de la arena. Se aleja lo suficiente de la formación de la escuadra (hasta x=3.5) para que nadie se toque durante la cuenta atrás.")]
    [SerializeField] private Vector2 spawnAreaCenter = new Vector2(9f, 0f);

    [Tooltip("Ancho y alto de la zona de aparición.")]
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(2f, 3f);

    private readonly List<EnemyController> wave = new List<EnemyController>();
    private readonly List<HeroController> deployed = new List<HeroController>();
    private ExpeditionState state = ExpeditionState.Idle;

    // Preparación táctica: la oleada está en escena pero congelada.
    private float countdownTimer;

    // Último número entero mostrado en el rótulo de cuenta atrás; evita repintar cada frame.
    private int lastCountdownTick = -1;

    // Se apunta al salir: si no hubo comida, la moral lo paga al volver.
    private bool underfed;
    private bool bossFloor;
    private EnemyController currentBoss;

    // El jefe vivo de esta expedición; null fuera de piso de jefe o si ya cayó.
    public EnemyController CurrentBoss => currentBoss != null ? currentBoss : null;

    public int CurrentFloor => currentFloor;
    public int HighestClearedFloor => highestClearedFloor;
    public int HighestSelectableFloor => highestClearedFloor + 1;
    public bool IsCountingDown => countdownTimer > 0f;
    public ExpeditionState State => state;
    public int EnemyCountForFloor => baseEnemyCount + (currentFloor - 1);
    public bool IsBossFloor => bossEveryFloors > 0 && currentFloor % bossEveryFloors == 0;
    public float StatMultiplierForFloor(int floor) => 1f + statGrowthPerFloor * (floor - 1)
                                            + hardHealthGrowth * HardFloorsFor(floor);

    // Pisos por encima del umbral duro; a partir de ahí el rusheo automático deja de valer.
    public int HardFloors => HardFloorsFor(currentFloor);

    private int HardFloorsFor(int floor) => Mathf.Max(0, floor - hardFloorFrom + 1);

    // El ataque sube más deprisa que la vida: obliga a provocar, curar y retirarse a tiempo.
    public float AttackMultiplierForFloor(int floor) => 1f + hardAttackGrowth * HardFloorsFor(floor);

    // Punto medio entre la línea de la escuadra y la de los enemigos; ahí encuadra la cámara.
public Vector2 ArenaFocus => arenaCenter + new Vector2((heroSpawnOffset.x + spawnAreaCenter.x) * 0.5f, spawnAreaCenter.y);

    public int AliveEnemies
    {
        get { PruneWave(); return wave.Count; }
    }

    // Vida agregada de la escuadra desplegada (0-1); 1 si no hay nadie fuera. Lo usa el auto-retirada.
    public float DeployedHealthRatio
    {
        get
        {
            int cur = 0, max = 0;
            foreach (var hero in deployed)
            {
                if (hero == null) continue;
                cur += hero.CurrentHealth;
                max += hero.MaxHealth;
            }
            return max > 0 ? (float)cur / max : 1f;
        }
    }

    // Se dispara con (estado, mensaje) para que la UI muestre el feedback.
    public event System.Action<ExpeditionState, string> ExpeditionChanged;

    // Se dispara con el piso nuevo; evita que la UI dependa del orden de los Start.
    public event System.Action<int> FloorChanged;

    // Se dispara al superar un piso, con el desglose exacto del botín; lo consume el modal de cofre.
    public event System.Action<FloorRewardInfo> FloorCleared;

    // Se dispara al aparecer un jefe (true) y al resolverse su piso (false); lo usa AudioManager
    // para el stem musical aditivo y el ducking de aviso de jefe (design/audio/audio-torre-dinamica.md §8.3).
    public event System.Action<bool> BossStateChanged;

    // Solo invoca el evento en una transición real; evita disparos duplicados.
    private void SetBossFloor(bool value)
    {
        if (bossFloor == value) return;
        bossFloor = value;
        BossStateChanged?.Invoke(bossFloor);
    }

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
        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();

        ArenaWallMin = arenaCenter - arenaWallHalfExtents;
        ArenaWallMax = arenaCenter + arenaWallHalfExtents;

        DrawArenaFence();
    }

    // Valla perimetral visible: marco de piedra/madera oscura integrado con el bioma, no una
    // línea de depuración. Marca en el suelo dónde el clamp físico detiene a las unidades.
    private void DrawArenaFence()
    {
        var fenceGO = new GameObject("ArenaFence");
        var line = fenceGO.AddComponent<LineRenderer>();
        line.loop = true;
        line.positionCount = 4;
        line.useWorldSpace = true;
        line.widthMultiplier = 0.25f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = line.endColor = new Color(0.12f, 0.16f, 0.21f, 0.75f);
        line.SetPosition(0, new Vector3(ArenaWallMin.x, ArenaWallMin.y, 0f));
        line.SetPosition(1, new Vector3(ArenaWallMax.x, ArenaWallMin.y, 0f));
        line.SetPosition(2, new Vector3(ArenaWallMax.x, ArenaWallMax.y, 0f));
        line.SetPosition(3, new Vector3(ArenaWallMin.x, ArenaWallMax.y, 0f));
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
            Report(ExpeditionState.Idle, LocalizationManager.Get("UI_STATUS_ASSIGN_HEROES"));
            return;
        }

        // La comida se cobra al salir; sin despensa la expedición sale igual, pero pasa factura.
        underfed = economy == null || !economy.TrySpendFood(foodPerExpedition);
        if (underfed) Debug.LogWarning("[Expedición] Sin comida: la escuadra volverá desnutrida.", this);

        DespawnWave();
        DeployParty();

        int count = EnemyCountForFloor;
        float mult = StatMultiplierForFloor(currentFloor);
        float atk = AttackMultiplierForFloor(currentFloor);

        for (int i = 0; i < count; i++)
        {
            Vector2 half = spawnAreaSize * 0.5f;
            Vector2 pos = arenaCenter + spawnAreaCenter + new Vector2(
                Random.Range(-half.x, half.x),
                Random.Range(-half.y, half.y));

            var datos = PickEnemyData(i);

            // Cada rol en su línea: los tanques delante y los de rango detrás.
            pos += new Vector2(LineOffset(datos), 0f);

            var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
            go.name = $"Enemy_{SafeName(datos)}_F{currentFloor}_{i + 1}";

            var enemy = go.GetComponent<EnemyController>();
            enemy.Initialize(datos, mult, atk);
            wave.Add(enemy);
        }

        // El jefe se suma a la oleada normal del piso; el evento se dispara dentro de SpawnBoss().
        bossFloor = IsBossFloor && bossData != null;
        if (bossFloor) SpawnBoss();

        // Preparación táctica: la oleada ya está puesta, pero no se mueve hasta que pase la cuenta atrás.
        // En piso de jefe hay una pausa dramática y un aviso en pantalla antes de arrancar la cuenta atrás.
        if (bossFloor) StartCoroutine(BossArrivalThenCountdown());
        else BeginCountdown();

        string extra = bossFloor ? LocalizationManager.Get("UI_BOSS_TAG") : string.Empty;
        string duro = HardFloors > 0 ? $"  ATK x{atk:0.00}" : string.Empty;
        Report(ExpeditionState.InProgress, string.Format(
            LocalizationManager.Get("UI_STATUS_FLOOR_START"),
            currentFloor, count, extra, mult, duro, party.Party.Count, party.MaxPartySize));
    }

    // Desplazamiento en X según el rol: negativo acerca a la escuadra, positivo aleja.
    private float LineOffset(EnemyData datos)
    {
        if (datos == null) return 0f;

        // Arqueros y chamanes se quedan en retaguardia, a cubierto de la primera línea.
        if (datos.attackRange >= rangedSpawnThreshold) return lineSpacing;

        // Los que aguantan hacen de muro por delante del resto del cuerpo a cuerpo.
        if (datos.maxHealth >= tankHealthThreshold) return -lineSpacing * 0.5f;

        return 0f;
    }

    // Composición por piso: el goblin es el relleno y el resto entra según a qué altura estemos.
    private EnemyData PickEnemyData(int index)
    {
        if (archerData != null && archerEveryNth > 0 && (index + 1) % archerEveryNth == 0) return archerData;

        // Los orcos aguantan: en los pisos duros salen siempre, no solo en los pares.
        bool orcosSiempre = currentFloor >= hardFloorFrom;
        if (orcData != null && (orcosSiempre || currentFloor % 2 == 0) && index % 3 == 0) return orcData;

        if (currentFloor >= 3)
        {
            // A partir del piso duro hay un chamán de cada cuatro: más curas y más control.
            int cadaChaman = currentFloor >= hardFloorFrom ? 4 : 5;
            if (shamanData != null && index % cadaChaman == cadaChaman - 1) return shamanData;
            if (skeletonData != null && index % 2 == 1) return skeletonData;
        }

        return enemyData;
    }

    private static string SafeName(EnemyData data)
        => data != null && !string.IsNullOrEmpty(data.enemyName)
            ? data.enemyName.Replace(" ", string.Empty)
            : "Enemy";

    // Pausa dramática (tiempo real, sin depender del timeScale de combate) + aviso "¡JEFE!" antes de la cuenta atrás.
    private IEnumerator BossArrivalThenCountdown()
    {
        ScreenBanner.Show(LocalizationManager.Get("UI_BOSS_ARRIVAL"), bossArrivalBannerSeconds,
            new Color(1f, 0.25f, 0.20f));

        yield return new WaitForSecondsRealtime(bossArrivalPause);
        BeginCountdown();
    }

    // Congela oleada y escuadra; la cuenta atrás numérica (3-2-1-¡Lucha!) da tiempo a leer el campo.
    private void BeginCountdown()
    {
        countdownTimer = Mathf.Max(0f, combatCountdown);
        lastCountdownTick = Mathf.CeilToInt(countdownTimer);

        foreach (var enemy in wave)
            if (enemy != null) enemy.SetFrozen(countdownTimer > 0f);

        foreach (var hero in deployed)
            if (hero != null) hero.SetFrozen(countdownTimer > 0f);

        if (countdownTimer > 0f && lastCountdownTick > 0)
            ScreenBanner.Show(lastCountdownTick.ToString(), 1f, new Color(1f, 0.85f, 0.35f));
    }

    private void ReleaseWave()
    {
        foreach (var enemy in wave)
            if (enemy != null) enemy.SetFrozen(false);

        foreach (var hero in deployed)
            if (hero != null)
            {
                hero.SetFrozen(false);
                hero.AcquireNearestTargetNow();
            }

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

            // Desacoplado de Fase 39: si estaba currando en un edificio, se desasigna solo al
            // desplegar de verdad, sin bloquear antes al meterlo en la escuadra de Torre.
            if (hero.AssignedBuilding != null) hero.AssignedBuilding.ToggleWorker(hero);

            // Sale por el Portal de la Torre y camina hasta su puesto en formación; cada puesto
            // tiene su sitio: apilados, el golpe circular del jefe se los lleva a todos.
            hero.DeployViaGateway(arenaCenter + heroSpawnOffset + party.FormationSlot(slot));
            deployed.Add(hero);
            slot++;
        }

        ApplyOriginSynergy();
        Debug.Log($"[Expedición] Escuadra desplegada: {deployed.Count} héroe(s).", this);
    }

    // Origen compartido por la escuadra y cuántos lo aprovechan; lo lee el HUD de combate.
    public string ActiveSynergyOrigin { get; private set; }
    public int ActiveSynergyCount { get; private set; }
    public float OriginSynergyBonus => originSynergyBonus;

    // Compartir tierra natal con alguien de la escuadra da un empujón mientras dure el combate.
    private void ApplyOriginSynergy()
    {
        ActiveSynergyOrigin = null;
        ActiveSynergyCount = 0;

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
            if (!acompanado) continue;

            // El indicador enseña el origen que más gente comparte.
            ActiveSynergyCount++;
            if (ActiveSynergyOrigin == null) ActiveSynergyOrigin = hero.Data.origin;

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
        SetBossFloor(false);
        countdownTimer = 0f;

        Report(ExpeditionState.Idle, string.Format(
            LocalizationManager.Get("UI_STATUS_RETREAT"), currentFloor, rescatados));
        return true;
    }

    // Los devuelve a la base y les quita el estado de combate.
    private void RecallParty()
    {
        int gatewayCount = 0;

        foreach (var hero in deployed)
        {
            if (hero == null) continue;

            bool viaGateway = gatewayCount < gatewayVisibleCap;
            if (viaGateway) gatewayCount++;

            hero.SetDeployed(false, viaGateway);
            hero.SetOriginSynergy(0f);
            hero.SetFrozen(false);
            hero.Status.Clear();
            hero.WearEquipment(wearPerExpedition);
            if (underfed) hero.LoseMorale(malnutritionMoraleLoss);
        }

        if (underfed && deployed.Count > 0)
            Debug.LogWarning($"[Expedición] Desnutrición: -{malnutritionMoraleLoss} moral a la escuadra.", this);

        deployed.Clear();
        underfed = false;
        ActiveSynergyOrigin = null;
        ActiveSynergyCount = 0;
    }

    // El jefe escala igual que el relleno, pero ancla sus proporciones al piso de calibración.
    private void SpawnBoss()
    {
        var go = Instantiate(enemyPrefab, arenaCenter + spawnAreaCenter + new Vector2(1.5f, 0f), Quaternion.identity);
        go.name = $"Enemy_Boss_F{currentFloor}";

        // Los multiplicadores del jefe son relativos a su piso de calibración: mantiene fijas
        // sus proporciones de vida/ataque respecto al relleno, sin importar cuánto suba el piso.
        float bossHpMult = StatMultiplierForFloor(currentFloor) / StatMultiplierForFloor(bossCalibrationFloor);
        float bossAtkMult = AttackMultiplierForFloor(currentFloor) / AttackMultiplierForFloor(bossCalibrationFloor);

        var boss = go.GetComponent<EnemyController>();
        boss.Initialize(bossData, bossHpMult, bossAtkMult);
        boss.MakeBoss(bossScale);
        wave.Add(boss);
        currentBoss = boss;

        Debug.Log($"[Jefe] {bossData.enemyName} aparece en el piso {currentFloor} " +
                  $"con {boss.MaxHealth} PV.", this);

        BossStateChanged?.Invoke(true);
    }

    // Botín garantizado por tumbar al jefe: gemas, materiales y una Piedra de Ascensión cuyo
    // tier sube con la franja de piso, igual que los biomas (1-5 Menor ... 16+ Legendaria).
    private void GrantBossChest(int floor)
    {
        economy?.Add(bossChestGems);
        economy?.AddMaterials(bossChestMaterials, bossChestMaterials);

        var tier = StoneTierForFloor(floor);
        crafting?.AddStones(tier, 1);

        VfxManager.Play(VfxId.VictoryChest, (Vector3)ArenaFocus);

        Debug.Log($"[Cofre] Botín del jefe: +{bossChestGems} gemas, " +
                  $"+{bossChestMaterials} madera, +{bossChestMaterials} hierro y +1 Piedra {tier}.", this);
    }

    private AscensionStoneTier StoneTierForFloor(int floor)
    {
        if (floor <= stoneTierFloorCap[0]) return AscensionStoneTier.Menor;
        if (floor <= stoneTierFloorCap[1]) return AscensionStoneTier.Media;
        if (floor <= stoneTierFloorCap[2]) return AscensionStoneTier.Mayor;
        return AscensionStoneTier.Legendaria;
    }

    void Update()
    {
        if (state != ExpeditionState.InProgress) return;

        // Mientras dura la preparación no se pelea ni se resuelve nada.
        if (countdownTimer > 0f)
        {
            countdownTimer -= Time.deltaTime;

            // Repinta el rótulo solo al cruzar cada segundo entero: 3... 2... 1...
            int tick = Mathf.CeilToInt(countdownTimer);
            if (tick > 0 && tick != lastCountdownTick)
            {
                lastCountdownTick = tick;
                ScreenBanner.Show(tick.ToString(), 1f, new Color(1f, 0.85f, 0.35f));
            }

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
            bool bossChest = bossFloor && firstClear;

            if (gemGain > 0) economy?.Add(gemGain);
            economy?.AddMaterials(woodGain, ironGain);

            if (bossChest) GrantBossChest(cleared);

            int expGain = Mathf.Max(1, expReward * cleared);
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

            AudioManager.Play(SfxId.Victory);
            FloorCleared?.Invoke(new FloorRewardInfo
            {
                floor = cleared,
                firstClear = firstClear,
                bossFloor = bossChest,
                gems = gemGain + (bossChest ? bossChestGems : 0),
                wood = woodGain + (bossChest ? bossChestMaterials : 0),
                iron = ironGain + (bossChest ? bossChestMaterials : 0),
                exp = expGain
            });

            Report(ExpeditionState.Won, string.Format(
                LocalizationManager.Get("UI_STATUS_WON"), cleared, modo, gemGain, woodGain, ironGain));
            SetBossFloor(false);
            return;
        }

        // Se pierde cuando cae toda la escuadra, no cuando cae todo el roster.
        if (CountAliveDeployed() == 0)
        {
            DespawnWave();
            RecallParty();
            SetBossFloor(false);
            countdownTimer = 0f;
            AudioManager.Play(SfxId.Defeat);
            Report(ExpeditionState.Lost, string.Format(
                LocalizationManager.Get("UI_STATUS_LOST"), currentFloor));
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
        currentBoss = null;
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
