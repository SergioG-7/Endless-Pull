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

    // Sub-Misión Oculta ganada en este piso, si la había; None si no. Se guarda el tipo (no el
    // texto ya formateado) para poder reconstruir el mensaje en el idioma activo si cambia después.
    public HiddenChallengeType hiddenChallengeType;
    public int hiddenChallengeGems;
}

// Estado de la expedición del piso actual.
public enum ExpeditionState
{
    Idle,
    InProgress,
    Won,
    Lost
}

// Tipo de resultado de la última batalla, para regenerar el banner con el idioma activo
// en vez de guardar el string ya formateado (se quedaba congelado si el jugador cambiaba
// de idioma después de retirarse o perder).
public enum BattleResultType
{
    Cleared,
    Defeat,
    Retreat
}

public struct LastBattleResult
{
    public int Floor;
    public BattleResultType ResultType;
    public int SurvivorsCount;
}

// Tipo de objetivo del piso. Subjugation es el 100% procedural de siempre (valor por defecto,
// índice 0, para retrocompatibilidad); el resto son pisos guionizados dentro del rediseño 1-20
// (más los dos de escolta pura, 15 y 25). El Piso 10 sigue siendo Escort de cara al enum, pero
// se distingue como asedio (más dps sobre Friacis) vía IsSiegeFloor(), no como valor aparte.
public enum FloorMissionType
{
    Subjugation,
    Survival,
    Escort,
    BossHunt
}

// Sub-Misión Oculta: reto secundario por piso, sorteado en silencio; solo se revela
// con un toast si se cumple. None = este piso no tiene reto asignado.
public enum HiddenChallengeType
{
    None,
    SpeedClear,
    Assassinate,
    NoHeroDown,
    EscortUnharmed
}

public class WaveManager : MonoBehaviour
{
    [Tooltip("Prefab de enemigo que se usa para poblar la oleada.")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Datos base del enemigo, antes de escalar por piso.")]
    [SerializeField] private EnemyData enemyData;

    [Tooltip("Bestiario por tramo de piso. Si está vacío se usa el reparto antiguo por campos sueltos.")]
    [SerializeField] private FloorBand[] bestiary = new FloorBand[0];

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

    [Tooltip("Coreografía de entrada: reconocimiento y formación antes de pelear.")]
    [SerializeField] private BattleChoreographer choreographer;

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

    // El cupo es por escuadrón: con dos desplegados solo volvía por el Portal el primero,
    // porque el tope contaba sobre `deployed`, que es la unión de todos.
    private int GatewayCapForRecall => gatewayVisibleCap * Mathf.Max(1, squads.Count);

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

    [Tooltip("Fracción de vida por debajo de la cual un héroe desplegado se auto-cura con una poción, si le queda cupo.")]
    [SerializeField] private float autoPotionHealthRatio = 0.35f;

    [Tooltip("Máximo de auto-curaciones por héroe y expedición.")]
    [SerializeField] private int autoPotionUsesPerExpedition = 3;

    [Tooltip("Segundos mínimos entre cada auto-curación del mismo héroe, para que no se beba el inventario de golpe.")]
    [SerializeField] private float autoPotionCooldown = 3f;

    [Tooltip("Fracción de maná por debajo de la cual un héroe desplegado se bebe una poción de maná, si le queda cupo.")]
    [SerializeField] private float autoManaPotionRatio = 0.25f;

    [Tooltip("Máximo de pociones de maná por héroe y expedición; mismo cupo que las de vida.")]
    [SerializeField] private int autoManaPotionUsesPerExpedition = 3;

    [Tooltip("Fracción de vida por debajo de la cual sobrevivir a un combate cuenta como situación crítica, de cara al Despertar de Habilidades.")]
    [Range(0f, 1f)]
    [SerializeField] private float criticalHealthRatioForAwakening = 0.20f;

    [Tooltip("Probabilidad de que un héroe elegible (sobrevivió crítico, o mató al jefe) despierte una pasiva nueva al superar el piso.")]
    [Range(0f, 1f)]
    [SerializeField] private float skillAwakeningChance = 0.15f;

    [Tooltip("De cada despertar, qué parte cambia la habilidad activa en vez de dar una pasiva.")]
    [SerializeField, Range(0f, 1f)] private float abilityAwakeningShare = 0.35f;

    [Tooltip("Último piso de cada tier en el cofre de jefe (Menor, Media, Mayor); por encima cae Legendaria.")]
    [SerializeField] private int[] stoneTierFloorCap = { 5, 10, 15 };

    [Tooltip("Piezas del conjunto del Guardián, en el orden de los pisos de wardenDropFloors.")]
    [SerializeField] private EquipmentData[] wardenDrops;

    [Tooltip("Piso que suelta cada pieza del Guardián; la primera vez cae segura.")]
    [SerializeField] private int[] wardenDropFloors = { 10, 20, 30, 40 };

    [Tooltip("Probabilidad de que una pieza del Guardián vuelva a caer una vez ya conseguida.")]
    [Range(0f, 1f)]
    [SerializeField] private float wardenRepeatChance = 0.15f;

    [SerializeField] private ShopManager shop;

    // Piezas del Guardián ya conseguidas alguna vez; las guarda el SaveManager por nombre de asset.
    private readonly HashSet<string> wardenGranted = new HashSet<string>();

    [Tooltip("Segundos de pausa dramática antes de la cuenta atrás cuando aparece el jefe.")]
    [SerializeField] private float bossArrivalPause = 0.5f;

    [Tooltip("Segundos que se enseña el aviso '¡JEFE!' en pantalla.")]
    [SerializeField] private float bossArrivalBannerSeconds = 1.2f;

    // ===== Revamp de pisos 1-20 + escolta (Friacis Al Lagner) =====

    [Tooltip("Jefe único del Piso 20 (Halgiraph); si está vacío, el Piso 20 cae al jefe genérico.")]
    [SerializeField] private EnemyData halgiraphData;

    [Tooltip("Prefab de la NPC de escolta (Friacis) para los pisos 10/15/25.")]
    [SerializeField] private EscortNpc escortPrefab;

    [Tooltip("Vida de Friacis al desplegarse.")]
    [SerializeField] private int escortMaxHealth = 220;

    [Tooltip("Desgaste por segundo sobre Friacis mientras quede algún enemigo vivo en el Piso 10 (asedio, más presión que la escolta pura).")]
    [SerializeField] private float siegeDamagePerSecond = 6f;

    [Tooltip("Desgaste por segundo sobre Friacis mientras quede algún enemigo vivo en los Pisos 15/25 (escolta pura).")]
    [SerializeField] private float escortDamagePerSecond = 3f;

    [Tooltip("Radio en el que un enemigo vivo cuenta como amenaza real para Friacis.")]
    [SerializeField] private float escortThreatRadius = 3.5f;

    [Tooltip("Segundos que hay que aguantar en el Piso 5 (Filtro de Supervivencia).")]
    [SerializeField] private float survivalDuration = 60f;

    [Tooltip("Cada cuántos segundos entra una oleada nueva mientras dure la Supervivencia.")]
    [SerializeField] private float survivalRespawnInterval = 4f;

    [Tooltip("Enemigos que entran de golpe en cada oleada de Supervivencia, en el piso base (Piso 5).")]
    [SerializeField] private int survivalWaveBaseSize = 2;

    [Tooltip("Enemigos extra por oleada de Supervivencia por cada piso por encima del piso base.")]
    [SerializeField] private int survivalWaveGrowthPerFloor = 1;

    [Tooltip("Segundos límite del reto oculto de despeje rápido en pisos normales (Subyugación).")]
    [SerializeField] private float hiddenSpeedClearSeconds = 45f;

    [Tooltip("Segundos límite del reto oculto de despeje rápido en piso de jefe.")]
    [SerializeField] private float hiddenBossSpeedClearSeconds = 90f;

    [Tooltip("Gemas extra que da un reto oculto superado, además de la Piedra de Ascensión.")]
    [SerializeField] private int hiddenChallengeGemBonus = 50;

    [Tooltip("Probabilidad de que un piso tenga reto oculto en cada intento, mientras no se le haya ganado ya uno antes.")]
    [Range(0f, 1f)]
    [SerializeField] private float hiddenChallengeChance = 0.10f;

    [Tooltip("Origen de la arena; la base queda lejos para que no se mezclen las dos zonas.")]
    [SerializeField] private Vector2 arenaCenter = new Vector2(500f, 0f);

    [Tooltip("Desplazamiento de la formación de héroes respecto al origen de la arena; los deja en el extremo izquierdo, lejos de los enemigos, para que las unidades a distancia tengan hueco real de tiro.")]
    [SerializeField] private Vector2 heroSpawnOffset = new Vector2(-9.5f, 0f);

    [Tooltip("Pisos de jefe guionizado que despliegan más de un escuadrón; el resto va con uno.")]
    [SerializeField] private int[] multiSquadFloors = { 20 };

    [Tooltip("Presets de los que salen los escuadrones de apoyo, por orden. Índice 0 = Preset 1.")]
    [SerializeField] private int[] supportSquadPresets = { 1 };

    [Tooltip("Desplazamiento de cada escuadrón respecto al punto de despliegue; el primero es la escuadra activa.")]
    [SerializeField] private Vector2[] squadOffsets = { Vector2.zero, new Vector2(0f, -3.5f), new Vector2(0f, 3.5f) };

    [Tooltip("Medio ancho/alto del muro de la arena; cualquier proyectil que lo cruce se destruye para no escapar hacia la base.")]
    [SerializeField] private Vector2 arenaWallHalfExtents = new Vector2(15f, 7f);

    // Gestor activo de la Torre. Lo necesita el jefe para meter sus refuerzos en la oleada
    // en curso; sin esto tendria que instanciar enemigos por su cuenta y quedarian sueltos.
    private static WaveManager active;

    public static Vector2 ArenaWallMin { get; private set; }
    public static Vector2 ArenaWallMax { get; private set; }

    // Sin WaveManager en escena (el gimnasio) los límites valen (0,0) y recortar contra ellos deja
    // a todo el mundo clavado en el origen. Quien recorte tiene que preguntar primero.
    public static bool HasArenaBounds => ArenaWallMax.x > ArenaWallMin.x
                                         && ArenaWallMax.y > ArenaWallMin.y;



    [Tooltip("Piso en el que está la expedición ahora mismo.")]
    [SerializeField] private int currentFloor = 1;

    [Tooltip("Enemigos del piso 1; cada piso suma uno más.")]
    [SerializeField] private int baseEnemyCount = 4;

    [Tooltip("Enemigos vivos como mucho a la vez; el resto de la oleada entra como refuerzo.")]
    [Min(1)]
    [SerializeField] private int maxConcurrentEnemies = 12;

    [Tooltip("Segundos entre tandas de refuerzo en el piso 1; se acorta según se sube.")]
    [SerializeField] private float reinforcementInterval = 1.5f;

    [Tooltip("Cuánto se acorta el hueco entre tandas por cada piso.")]
    [SerializeField] private float reinforcementIntervalPerFloor = 0.02f;

    [Tooltip("Suelo del hueco entre tandas; por debajo dejan de verse llegar.")]
    [SerializeField] private float minReinforcementInterval = 0.5f;

    [Tooltip("Refuerzos que entran juntos en el piso 1; una tanda no es un enemigo suelto.")]
    [Min(1)]
    [SerializeField] private int reinforcementBatch = 2;

    [Tooltip("Pisos que hacen falta para que entre un refuerzo más por tanda.")]
    [SerializeField] private int reinforcementFloorsPerExtra = 10;

    [Tooltip("Cuánto más atrás de la zona de aparición entran los refuerzos, por la retaguardia enemiga.")]
    [SerializeField] private float reinforcementBackOffset = 4f;

    [Tooltip("Crecimiento compuesto de vida y ataque por cada piso; el poder del héroe también es multiplicativo.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float statCompoundGrowth = 0.08f;

    [Tooltip("Piso a partir del cual la subida por piso se aplana; antes de él manda la de arriba.")]
    [Min(1)]
    [SerializeField] private int growthSoftenFloor = 40;

    [Tooltip("Subida compuesta por piso pasado el aplanado.")]
    [SerializeField] private float lateCompoundGrowth = 0.025f;

    [Tooltip("Último piso de la Torre; llegar ahí es la meta del juego.")]
    [Min(1)]
    [SerializeField] private int finalFloor = 100;

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

    // Ranura de consumible: cupo de auto-curaciones por héroe, compartido con el stock
    // global de pociones (sin inventario propio por héroe); se reinicia en cada despliegue.
    private readonly Dictionary<HeroController, int> potionUsesThisExpedition = new Dictionary<HeroController, int>();

    // Momento (Time.time) a partir del cual el héroe puede volver a auto-curarse con poción.
    private readonly Dictionary<HeroController, float> potionCooldownUntil = new Dictionary<HeroController, float>();

    // Mismo reparto para las de maná: cupo propio de 3 y su propio enfriamiento.
    private readonly Dictionary<HeroController, int> manaPotionUsesThisExpedition = new Dictionary<HeroController, int>();
    private readonly Dictionary<HeroController, float> manaPotionCooldownUntil = new Dictionary<HeroController, float>();

    // Héroes desplegados que en algún momento de esta expedición cayeron por debajo del umbral
    // crítico y siguen en pie: candidatos al Despertar de Habilidades al superar el piso.
    private readonly HashSet<HeroController> survivedCritical = new HashSet<HeroController>();
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

    // Objetivo del piso actual, NPC de escolta viva y cronómetro de Supervivencia.
    private FloorMissionType currentMissionType = FloorMissionType.Subjugation;
    private bool currentIsSiege;
    private EscortNpc currentEscort;
    private float survivalTimer;
    private float survivalRespawnTimer;
    private float escortDamageAccumulator;

    // Sub-Misión Oculta del piso actual: sorteada al empezar, se resuelve en silencio y solo
    // se revela con un toast si se cumple al superar el piso.
    private HiddenChallengeType currentChallenge;
    private EnemyController challengeTarget;
    private float challengeSpeedLimit;
    private float combatElapsed;
    private int deployedStartCount;
    private int deployedStartMaxHealth;
    private bool heroDownOccurred;

    // Quién se ha quedado en este piso para siempre; se resume al terminar, gane o pierda.
    private readonly List<string> fallenThisFloor = new List<string>();

    // Resto de la oleada que aún no ha entrado por el tope de simultáneos, con el escalado del
    // piso ya resuelto para que un refuerzo salga idéntico a los que salieron al principio.
    private readonly List<EnemyData> pendingReinforcements = new List<EnemyData>();
    private float reinforcementTimer;
    private bool regrouping;
    private float waveStatMultiplier = 1f;
    private float waveAttackMultiplier = 1f;
    private int waveSpawnedTotal;

    // Pisos que ya han pagado su reto oculto alguna vez; no se vuelve a sortear ahí, para que no
    // se pueda farmear el mismo piso en bucle por materiales/gemas gratis.
    private readonly HashSet<int> hiddenChallengeAwardedFloors = new HashSet<int>();

    public FloorMissionType CurrentMissionType => currentMissionType;
    public EscortNpc CurrentEscort => currentEscort != null ? currentEscort : null;

    // El banner de misión solo necesita saber SI hay reto este piso, nunca cuál es.
    public bool HasHiddenChallenge => currentChallenge != HiddenChallengeType.None;

    // La usa el SaveManager para guardar/cargar qué pisos ya pagaron su reto oculto.
    public IEnumerable<int> HiddenChallengeAwardedFloors => hiddenChallengeAwardedFloors;

    public void LoadHiddenChallengeAwardedFloors(IEnumerable<int> floors)
    {
        hiddenChallengeAwardedFloors.Clear();
        if (floors == null) return;
        foreach (int floor in floors) hiddenChallengeAwardedFloors.Add(floor);
    }

    // Segundos de preparación táctica del piso (cuenta atrás previa al combate); lo lee el
    // banner de misión para durar justo lo que tarda en soltarse la oleada.
    public float CombatCountdown => combatCountdown;

    // Segundos totales que hay que aguantar en un piso de Supervivencia (Piso 5).
    public float SurvivalDuration => survivalDuration;

    // Segundos que quedan del cronómetro de Supervivencia; 0 fuera de ese tipo de piso.
    public float SurvivalTimeRemaining => Mathf.Max(0f, survivalTimer);

    // Se dispara al empezar la cuenta atrás de un piso, con el tipo de misión y el número de
    // piso; lo consume el banner de misión para mostrar título y objetivo.
    public event System.Action<FloorMissionType, int> MissionStarted;

    // Único punto de verdad de qué le toca a cada piso; Subjugation cae siempre al camino
    // procedural de toda la vida. Los pisos 5/10/15/20/25 dejan de usar el jefe genérico
    // recurrente (antes caían todos en IsBossFloor) para tener su propio objetivo guionizado.
    private FloorMissionType MissionTypeFor(int floor)
    {
        switch (floor)
        {
            case 5: return FloorMissionType.Survival;
            case 10: return FloorMissionType.Escort;
            case 15: return FloorMissionType.Escort;
            case 20: return FloorMissionType.BossHunt;
            case 25: return FloorMissionType.Escort;
        }

        if (floor <= 25) return FloorMissionType.Subjugation;

        // Pasado el tramo guionizado los tipos rotan en ciclos de diez pisos. Sin esto, de cada
        // cinco pisos cuatro eran "mata a todos" idénticos, y Supervivencia y Escolta no volvían
        // a salir nunca: se quedaban en los pisos 5 y 10/15/25 y sus retos ocultos con ellas.
        // El último piso es el jefe final aunque la cadencia de jefes cambie.
        if (floor >= finalFloor || IsBossFloorFor(floor)) return FloorMissionType.BossHunt;

        int enLaDecena = floor % 10;
        if (enLaDecena == 3) return FloorMissionType.Survival;
        if (enLaDecena == 8) return FloorMissionType.Escort;

        return FloorMissionType.Subjugation;
    }

    // El Piso 10 es asedio (más dps sobre Friacis que una escolta pura); los demás pisos de
    // Escort (15/25) no presionan tan fuerte. Vive fuera del enum a propósito (ver comentario ahí).
    private static bool IsSiegeFloor(int floor) => floor == 10;

    // Lectura pública de a qué tipo cae un piso sin tener que empezarlo; la usa Isel
    // para avisar de qué le espera al jugador en el próximo piso seleccionable.
    public FloorMissionType PeekMissionType(int floor) => MissionTypeFor(floor);

    public int CurrentFloor => currentFloor;
    public int HighestClearedFloor => highestClearedFloor;
    public int HighestSelectableFloor => Mathf.Min(highestClearedFloor + 1, finalFloor);
    public bool IsCountingDown => countdownTimer > 0f;
    public ExpeditionState State => state;
    public int EnemyCountForFloor => baseEnemyCount + (currentFloor - 1);
    public bool IsBossFloor => IsBossFloorFor(currentFloor);

    // Con el piso por parametro: MissionTypeFor tambien responde por pisos que aun no se juegan,
    // que es lo que consulta el selector de la Torre para avisar de lo que viene.
    private bool IsBossFloorFor(int floor) => bossEveryFloors > 0 && floor % bossEveryFloors == 0;
    // Base compuesta: en linea recta la Torre se aplanaba frente al equipo, la maestria y los
    // niveles del heroe, que si multiplican entre si.
    // Dos tramos: al 8 % por piso el enemigo del 100 salía a x2000 y el héroe al máximo llega a
    // x90, así que el final era inalcanzable por aritmética. El tramo 1-40 se deja como estaba
    // y solo se aplana el de arriba, que es el que no se había jugado.
    private float CompoundFor(int floor)
    {
        int tope = Mathf.Max(1, floor);
        int tramoBase = Mathf.Min(tope, growthSoftenFloor) - 1;
        float valor = Mathf.Pow(1f + statCompoundGrowth, Mathf.Max(0, tramoBase));

        if (tope > growthSoftenFloor)
            valor *= Mathf.Pow(1f + lateCompoundGrowth, tope - growthSoftenFloor);

        return valor;
    }

    public int FinalFloor => finalFloor;
    public bool IsFinalFloor => currentFloor >= finalFloor;

    public float StatMultiplierForFloor(int floor) => CompoundFor(floor)
                                            + hardHealthGrowth * HardFloorsFor(floor);

    // Pisos por encima del umbral duro; a partir de ahí el rusheo automático deja de valer.
    public int HardFloors => HardFloorsFor(currentFloor);

    private int HardFloorsFor(int floor) => Mathf.Max(0, floor - hardFloorFrom + 1);

    // El ataque sube más deprisa que la vida: obliga a provocar, curar y retirarse a tiempo.
    public float AttackMultiplierForFloor(int floor) => CompoundFor(floor)
                                            + hardAttackGrowth * HardFloorsFor(floor);

    // Punto medio entre la línea de la escuadra y la de los enemigos; ahí encuadra la cámara.
public Vector2 ArenaFocus => arenaCenter + new Vector2((heroSpawnOffset.x + spawnAreaCenter.x) * 0.5f, spawnAreaCenter.y);

    public int AliveEnemies
    {
        get { PruneWave(); return wave.Count; }
    }

    // Vida agregada de la escuadra desplegada (0-1); 1 si no hay nadie fuera. Lo usa el auto-retirada.
    // El denominador es el de la escuadra AL SALIR, no el de los que siguen en pie: los caídos se
    // destruyen y salían de la cuenta, así que la media SUBÍA cada vez que moría alguien.
    public float DeployedHealthRatio
    {
        get
        {
            int cur = 0;
            foreach (var hero in deployed)
                if (hero != null) cur += hero.CurrentHealth;

            return deployedStartMaxHealth > 0 ? (float)cur / deployedStartMaxHealth : 1f;
        }
    }

    // La media tapa al que se está muriendo: con cuatro sanos y uno agonizando sale 0,8 y la
    // retirada automática no salta. Esta mira al que peor está.
    public float LowestDeployedHealthRatio
    {
        get
        {
            float peor = 1f;
            foreach (var hero in deployed)
            {
                if (hero == null || hero.MaxHealth <= 0) continue;
                peor = Mathf.Min(peor, (float)hero.CurrentHealth / hero.MaxHealth);
            }
            return peor;
        }
    }

    // Ha caído alguien en este piso. Con permadeath ya es motivo de sobra para volverse.
    public bool AnyHeroFallenThisFloor => heroDownOccurred;

    // Se dispara con (estado, mensaje) para que la UI muestre el feedback.
    public event System.Action<ExpeditionState, string> ExpeditionChanged;

    // Solo Retirada/Derrota (Victoria ya tiene su propio FloorCleared con el desglose de recompensa).
    public event System.Action<LastBattleResult> BattleResultReported;

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

    // Los edificios miran el piso REALMENTE SUPERADO, nunca el que solo se puede intentar —
    // si no, seleccionar un piso nuevo ya lo desbloqueaba aunque se perdiera.
    private void PublishFloor()
    {
        BaseBuilding.SetTowerFloor(highestClearedFloor);
        BaseBackdrop.ApplyFloor(currentFloor);
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
        if (shop == null) shop = UnityEngine.Object.FindFirstObjectByType<ShopManager>();

        active = this;

        ArenaWallMin = arenaCenter - arenaWallHalfExtents;
        ArenaWallMax = arenaCenter + arenaWallHalfExtents;

        DrawArenaFence();

        // BaseBuilding.TowerFloor es estático y sobrevive a la recarga de escena de Nueva
        // Partida (solo un reload de dominio lo resetea) — sin este aviso en Awake, un piso
        // alto de la partida anterior deja todos los edificios desbloqueados de salida.
        // SaveManager.Load() (en Start, tras todos los Awake) lo corrige después si hay guardado.
        PublishFloor();
    }

    void OnEnable() => HeroController.HeroFallen += OnHeroFallen;

    void OnDisable() => HeroController.HeroFallen -= OnHeroFallen;

    private void OnHeroFallen(string heroName)
    {
        if (!string.IsNullOrEmpty(heroName)) fallenThisFloor.Add(heroName);
    }

    // Resumen de bajas del piso, gane o pierda: el banner del momento se lo puede haber comido
    // el ritmo del combate, así que al volver se dice otra vez y con la lista entera.
    private void ReportFallen()
    {
        if (fallenThisFloor.Count == 0) return;

        ScreenBanner.ShowCompact(
            string.Format(LocalizationManager.Get("UI_FLOOR_FALLEN_SUMMARY"),
                          string.Join(", ", fallenThisFloor)),
            4f, UITheme.Danger);

        fallenThisFloor.Clear();
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

        // Insubordinación: un héroe con la moral por los suelos, vida residual o equipo roto se
        // niega a entrar al Portal hasta que el Master lo resuelva (Cantina/Taller).
        foreach (var hero in party.Party)
        {
            if (hero == null || !hero.IsInsubordinate) continue;

            string motivo = LocalizationManager.Get(hero.InsubordinationReasonKey());
            Report(ExpeditionState.Idle, string.Format(
                LocalizationManager.Get("UI_STATUS_INSUBORDINATE"), hero.Data.heroName, motivo));
            return;
        }

        // La comida se cobra al salir; sin despensa la expedición sale igual, pero pasa factura.
        underfed = economy == null || !economy.TrySpendFood(foodPerExpedition);
        if (underfed) Debug.LogWarning("[Expedición] Sin comida: la escuadra volverá desnutrida.", this);

        DespawnWave();
        DeployParty();

        currentMissionType = MissionTypeFor(currentFloor);
        FloorRules.RollForFloor(currentFloor, IsBossFloor);
        currentIsSiege = IsSiegeFloor(currentFloor);
        survivalTimer = survivalDuration;
        survivalRespawnTimer = survivalRespawnInterval;
        escortDamageAccumulator = 0f;

        // El puesto se aguanta solo mientras el campo esté vacío; en cuanto asome un enemigo lo
        // suelta UpdateRegroup. Antes Supervivencia lo mantenía el piso entero y la escuadra
        // veía pasar a los enemigos que tenía al lado sin pegarles.
        bool hold = false;
        regrouping = false;
        fallenThisFloor.Clear();
        foreach (var hero in deployed)
            if (hero != null) hero.SetHoldPosition(hold);

        int count = EnemyCountForFloor;
        float mult = StatMultiplierForFloor(currentFloor);
        float atk = AttackMultiplierForFloor(currentFloor);

        // Oleada de élites: menos enemigos a la vez pero cada uno mucho más duro. El recorte de
        // simultáneos es lo que la hace legible; sin él solo serían los mismos con más números.
        if (FloorRules.IsEliteWave)
        {
            mult *= FloorRules.EliteHealthMultiplier;
            atk *= FloorRules.EliteDamageMultiplier;
        }

        waveStatMultiplier = mult;
        waveAttackMultiplier = atk;
        waveSpawnedTotal = 0;
        pendingReinforcements.Clear();
        reinforcementTimer = reinforcementInterval;

        // Solo entra de golpe lo que cabe en el tope; el resto espera turno. Sin esto, un piso
        // alto suelta decenas de enemigos a la vez y la escuadra cae antes de poder leer nada.
        int deGolpe = Mathf.Min(count, ConcurrentCapNow);

        for (int i = 0; i < count; i++)
        {
            var datos = PickEnemyData(i);
            if (i < deGolpe) SpawnEnemy(datos);
            else pendingReinforcements.Add(datos);
        }

        AssignHiddenChallenge();

        // El jefe se suma a la oleada normal del piso; el evento se dispara dentro de SpawnBoss().
        // Piso 20: jefe único Halgiraph en vez del genérico recurrente, si está asignado.
        EnemyData chosenBoss = currentFloor == 20 && halgiraphData != null ? halgiraphData : bossData;
        bossFloor = currentMissionType == FloorMissionType.BossHunt && chosenBoss != null;
        if (bossFloor) SpawnBoss(chosenBoss);

        // Friacis acompaña la oleada en los pisos de escolta (15/25 puros y el asedio del 10).
        bool escortFloor = currentMissionType == FloorMissionType.Escort;
        if (escortFloor) SpawnEscort();

        // Preparación táctica: la oleada ya está puesta, pero no se mueve hasta que pase la cuenta atrás.
        // En piso de jefe hay una pausa dramática y un aviso en pantalla antes de arrancar la cuenta atrás.
        if (bossFloor) StartCoroutine(BossArrivalThenCountdown());
        else BeginCountdown();

        string extra = bossFloor ? LocalizationManager.Get("UI_BOSS_TAG")
                      : escortFloor ? LocalizationManager.Get("UI_ESCORT_TAG")
                      : string.Empty;
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

    // Banda del bestiario que toca a este piso: la de mayor fromFloor que no lo supere.
    private FloorBand BandForFloor(int floor)
    {
        FloorBand mejor = null;

        foreach (var band in bestiary)
        {
            if (band == null || band.pool == null || band.pool.Length == 0) continue;
            if (band.fromFloor > floor) continue;
            if (mejor == null || band.fromFloor > mejor.fromFloor) mejor = band;
        }

        return mejor;
    }

    // Composición por piso: el goblin es el relleno y el resto entra según a qué altura estemos.
    private EnemyData PickEnemyData(int index)
    {
        // Con bestiario configurado manda él: cada hueco sortea del pool de su tramo, que es lo
        // que hace que dos pisos seguidos no traigan la misma fila de goblins.
        var band = BandForFloor(currentFloor);
        if (band != null)
        {
            var elegido = band.pool[Random.Range(0, band.pool.Length)];
            if (elegido != null) return elegido;
        }

        // Pisos 1-4 ("Pruebas de Caza"): solo goblins, sin orcos/tiradores/chamanes todavía
        // — el filtro de verdad empieza en el Piso 5.
        if (currentFloor <= 4) return enemyData;

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

    // Grupo de enemigos de golpe cada intervalo, no de uno en uno, con más integrantes cuanto
    // más alto el piso, para mantener la presión hasta agotar el cronómetro.
    // Un enemigo de la oleada del piso, con el escalado ya resuelto en SpawnWave. Lo usan tanto
    // el reparto inicial como los refuerzos que van entrando al abrirse hueco.
    private void SpawnEnemy(EnemyData datos, float backOffsetX = 0f)
    {
        if (enemyPrefab == null || datos == null) return;

        Vector2 half = spawnAreaSize * 0.5f;
        Vector2 pos = arenaCenter + spawnAreaCenter + new Vector2(
            Random.Range(-half.x, half.x),
            Random.Range(-half.y, half.y));

        // Cada rol en su línea: los tanques delante y los de rango detrás.
        // El refuerzo entra además desde más atrás, por la retaguardia de los suyos.
        pos += new Vector2(LineOffset(datos) + backOffsetX, 0f);

        waveSpawnedTotal++;
        var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
        go.name = $"Enemy_{SafeName(datos)}_F{currentFloor}_{waveSpawnedTotal}";

        var enemy = go.GetComponent<EnemyController>();
        enemy.Initialize(datos, waveStatMultiplier, waveAttackMultiplier);
        wave.Add(enemy);
    }

    // Refuerzos que entran juntos: uno más cada reinforcementFloorsPerExtra pisos, para que un
    // piso alto se note en la presión y no solo en las cifras de los enemigos.
    // Aforo de enemigos a la vez, ya recortado si el piso trae oleada de élites.
    private int ConcurrentCapNow
        => Mathf.Max(1, maxConcurrentEnemies
                        - (FloorRules.IsEliteWave ? FloorRules.EliteSimultaneousPenalty : 0));

    private int ReinforcementBatchNow
        => reinforcementBatch + (reinforcementFloorsPerExtra > 0
            ? currentFloor / reinforcementFloorsPerExtra
            : 0);

    // El hueco entre tandas se acorta con el piso, con suelo para que sigan viéndose llegar.
    private float ReinforcementIntervalNow
        => Mathf.Max(minReinforcementInterval,
                     reinforcementInterval - currentFloor * reinforcementIntervalPerFloor);

    // Deja entrar refuerzos por tandas mientras quede sitio bajo el tope de simultáneos. Entran
    // por detrás de la zona de aparición, no en medio del combate que ya está en marcha.
    private void TickReinforcements()
    {
        if (pendingReinforcements.Count == 0) return;

        PruneWave();
        int hueco = ConcurrentCapNow - wave.Count;
        if (hueco <= 0) return;

        reinforcementTimer -= Time.deltaTime;
        if (reinforcementTimer > 0f) return;

        reinforcementTimer = ReinforcementIntervalNow;

        int tanda = Mathf.Min(ReinforcementBatchNow, hueco, pendingReinforcements.Count);
        for (int i = 0; i < tanda; i++)
        {
            SpawnEnemy(pendingReinforcements[0], reinforcementBackOffset);
            pendingReinforcements.RemoveAt(0);
        }
    }

    // Sitio de la escuadra cuando no hay a quien pegar: alrededor de Friacis si la hay que
    // escoltar, y si no el punto de despliegue del piso.
    // Sin nadie a quien escoltar la escuadra se rehace DONDE ESTÁ: mandarla al punto de
    // despliegue la hacía cruzar la arena entera de vuelta a la izquierda entre tanda y tanda de
    // refuerzos, y volver a hacer todo el camino cuando entraban los siguientes.
    private Vector2 RallyPoint => currentEscort != null
        ? (Vector2)currentEscort.transform.position
        : SquadCenter();

    private Vector2 SquadCenter()
    {
        Vector2 suma = Vector2.zero;
        int vivos = 0;

        foreach (var hero in deployed)
        {
            if (hero == null || hero.CurrentHealth <= 0) continue;

            suma += (Vector2)hero.transform.position;
            vivos++;
        }

        return vivos > 0 ? suma / vivos : arenaCenter + heroSpawnOffset;
    }

    // Sin nadie a quien pegar, la escuadra no puede seguir avanzando sola hacia el lado enemigo:
    // vuelve al punto de reunión y rehace la línea. Escoltando eso pasa siempre que el campo
    // queda despejado, no solo entre tandas de refuerzo: el sitio de la guardia es con Friacis.
    private void UpdateRegroup()
    {
        // En Supervivencia los enemigos llegan por tandas con huecos de por medio: el hueco es
        // el único momento en que tiene sentido rehacer la línea.
        bool esperando = AliveEnemies == 0
                         && (pendingReinforcements.Count > 0 || currentEscort != null
                             || currentMissionType == FloorMissionType.Survival);

        if (esperando != regrouping)
        {
            regrouping = esperando;

            foreach (var hero in deployed)
                if (hero != null) hero.SetHoldPosition(esperando);

            if (choreographer != null)
            {
                if (esperando) choreographer.BeginRegroup(RallyPoint);
                else choreographer.EndRegroup();
            }
        }

        // Si el escoltado llegase a moverse, el punto de reunión lo sigue en vez de quedarse
        // donde apareció.
        if (regrouping && currentEscort != null && choreographer != null)
            choreographer.SetRallyPoint(RallyPoint);
    }

    private void SpawnSurvivalWave()
    {
        int size = Mathf.Max(1, survivalWaveBaseSize + survivalWaveGrowthPerFloor * Mathf.Max(0, currentFloor - 5));
        for (int i = 0; i < size; i++) SpawnSurvivalReinforcement();
    }

    // Un enemigo más, con el mismo escalado y línea que la oleada inicial; lo llama
    // SpawnSurvivalWave() tantas veces como integrantes tenga la oleada de ese piso.
    private void SpawnSurvivalReinforcement()
    {
        if (enemyPrefab == null) return;

        float mult = StatMultiplierForFloor(currentFloor);
        float atk = AttackMultiplierForFloor(currentFloor);
        var datos = PickEnemyData(wave.Count);

        Vector2 half = spawnAreaSize * 0.5f;
        Vector2 pos = arenaCenter + spawnAreaCenter + new Vector2(
            Random.Range(-half.x, half.x),
            Random.Range(-half.y, half.y));
        pos += new Vector2(LineOffset(datos), 0f);

        var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
        go.name = $"Enemy_{SafeName(datos)}_F{currentFloor}_Survival{wave.Count + 1}";

        var enemy = go.GetComponent<EnemyController>();
        enemy.Initialize(datos, mult, atk);
        wave.Add(enemy);
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
        MissionStarted?.Invoke(currentMissionType, currentFloor);

        // La regla del piso hay que cantarla antes de la cuenta atrás: si el jugador no sabe que
        // no se cura, la regla es una trampa en vez de una decisión.
        if (FloorRules.Current != FloorRule.None)
            ScreenBanner.ShowCompact(
                $"{FloorRules.DisplayName(FloorRules.Current)} - {FloorRules.Description(FloorRules.Current)}",
                4f, new Color(0.75f, 0.55f, 0.95f));

        // Reconocimiento y formación antes de los golpes; mientras dure, nadie pelea.
        if (choreographer == null) choreographer = GetComponent<BattleChoreographer>();
        if (choreographer != null) choreographer.BeginEncounter();

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
    // Solo los jefes guionizados sacan más de un escuadrón; el resto de pisos va con la escuadra
    // activa y nada más, como siempre.
    public bool IsMultiSquadFloor(int floor)
    {
        if (multiSquadFloors == null) return false;

        foreach (int piso in multiSquadFloors)
            if (piso == floor) return true;

        return false;
    }

    // Escuadrones que pide un piso: la escuadra activa más los de apoyo. La usan el panel de
    // Torre e Isel para avisar antes de entrar, y así poder preparar los presets.
    public int SquadCountForFloor(int floor)
        => IsMultiSquadFloor(floor) && supportSquadPresets != null
           ? 1 + supportSquadPresets.Length
           : 1;

    // Escuadrones desplegados ahora mismo, el primero es la escuadra activa. Lo lee el HUD.
    private readonly List<List<HeroController>> squads = new List<List<HeroController>>();
    public IReadOnlyList<List<HeroController>> Squads => squads;

    // Solo lectura para quien necesite actuar sobre el combate en curso.
    public IReadOnlyList<HeroController> Deployed => deployed;
    public IReadOnlyList<EnemyController> Wave => wave;

    private void DeployParty()
    {
        deployed.Clear();
        squads.Clear();
        potionUsesThisExpedition.Clear();
        potionCooldownUntil.Clear();
        manaPotionUsesThisExpedition.Clear();
        manaPotionCooldownUntil.Clear();
        survivedCritical.Clear();

        DeploySquad(new List<HeroController>(party.Party), 0);

        // En los pisos de jefe guionizado salen además los escuadrones de apoyo, cada uno con su
        // propia formación en su propio sitio de la arena.
        if (IsMultiSquadFloor(currentFloor) && supportSquadPresets != null)
        {
            foreach (int preset in supportSquadPresets)
            {
                var apoyo = party.ResolvePreset(preset);
                if (apoyo.Count > 0) DeploySquad(apoyo, squads.Count);
            }
        }

        ApplyOriginSynergy();
        Debug.Log($"[Expedición] {squads.Count} escuadrón(es) desplegado(s), " +
                  $"{deployed.Count} héroe(s) en total.", this);
    }

    private void DeploySquad(List<HeroController> squad, int squadIndex)
    {
        var desplegados = new List<HeroController>();

        // Sin desplazamiento propio los escuadrones aterrizarían unos encima de otros.
        Vector2 offset = squadOffsets != null && squadOffsets.Length > 0
            ? squadOffsets[Mathf.Clamp(squadIndex, 0, squadOffsets.Length - 1)]
            : Vector2.zero;

        int slot = 0;
        foreach (var hero in squad)
        {
            if (hero == null || deployed.Contains(hero)) continue;

            // Desacoplado de Fase 39: si estaba currando en un edificio, se desasigna solo al
            // desplegar de verdad, sin bloquear antes al meterlo en la escuadra de Torre.
            if (hero.AssignedBuilding != null) hero.AssignedBuilding.ToggleWorker(hero);

            // Sale por el Portal de la Torre y camina hasta su puesto en formación; cada puesto
            // tiene su sitio: apilados, el golpe circular del jefe se los lleva a todos.
            hero.DeployViaGateway(arenaCenter + heroSpawnOffset + offset + party.FormationSlot(slot));
            deployed.Add(hero);
            desplegados.Add(hero);
            slot++;
        }

        squads.Add(desplegados);
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
        LogFloorPace("retirada");
        ReportFallen();

        foreach (var hero in deployed)
            if (hero != null) hero.LoseMorale(retreatMoraleLoss);

        DespawnWave();
        RecallParty();
        SetBossFloor(false);
        countdownTimer = 0f;

        Report(ExpeditionState.Idle, string.Format(
            LocalizationManager.Get("UI_STATUS_RETREAT"), currentFloor, rescatados));
        BattleResultReported?.Invoke(new LastBattleResult
        {
            Floor = currentFloor,
            ResultType = BattleResultType.Retreat,
            SurvivorsCount = rescatados
        });
        return true;
    }

    // Los devuelve a la base y les quita el estado de combate.
    private void RecallParty()
    {
        // Sin esto la base heredaba la niebla o el suelo drenante del último piso.
        FloorRules.Clear();

        // Los cuerpos de los caídos se quedan en la arena todo el piso; al cerrarlo se retiran,
        // o el siguiente asalto empezaría con los muertos del anterior tirados por el suelo.
        DeathFall.ClearLingering();

        int gatewayCount = 0;
        regrouping = false;

        // Cerrar el encuentro es lo que devuelve al coreografo a Idle. Sin esto se quedaba en
        // Engaging para siempre tras el primer piso: seguia repartiendo puestos cada 3 s y
        // HasFrontLine no volvia a false, asi que la base entera formaba en linea.
        if (choreographer != null)
        {
            choreographer.EndRegroup();
            choreographer.EndEncounter();
        }

        foreach (var hero in deployed)
        {
            if (hero == null) continue;

            bool viaGateway = gatewayCount < GatewayCapForRecall;
            if (viaGateway) gatewayCount++;

            // Se marca antes de recogerlo: al pisar la base se manda solo a descansar si le
            // falta algo. Solo la escuadra, que es la que vuelve gastada de la Torre.
            hero.RequestRestOnReturn();
            hero.SetDeployed(false, viaGateway);
            hero.SetOriginSynergy(0f);
            hero.SetFrozen(false);
            hero.SetHoldPosition(false);
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
    // Recibe qué EnemyData usar (Halgiraph en el Piso 20, el genérico en el resto) en vez de
    // leer directamente el campo bossData, para no bifurcar el resto del método.
    private void SpawnBoss(EnemyData bossToSpawn)
    {
        var go = Instantiate(enemyPrefab, arenaCenter + spawnAreaCenter + new Vector2(1.5f, 0f), Quaternion.identity);
        go.name = $"Enemy_Boss_F{currentFloor}";

        // Los multiplicadores del jefe son relativos a su piso de calibración: mantiene fijas
        // sus proporciones de vida/ataque respecto al relleno, sin importar cuánto suba el piso.
        float bossHpMult = StatMultiplierForFloor(currentFloor) / StatMultiplierForFloor(bossCalibrationFloor);
        float bossAtkMult = AttackMultiplierForFloor(currentFloor) / AttackMultiplierForFloor(bossCalibrationFloor);

        var boss = go.GetComponent<EnemyController>();
        boss.Initialize(bossToSpawn, bossHpMult, bossAtkMult);
        boss.MakeBoss(bossScale);

        // Mecánica propia: rota por número de jefe, no por bioma (el Templo es todo el 16+, así
        // que atarla al bioma dejaba a todos los jefes altos con la misma).
        int indice = BossMechanics.IndexForFloor(currentFloor, bossEveryFloors);
        var primera = BossMechanics.Primary(indice);
        var segunda = BossMechanics.Secondary(indice);
        boss.SetMechanics(primera, segunda);

        wave.Add(boss);
        currentBoss = boss;

        Debug.Log($"[Jefe] {bossToSpawn.enemyName} aparece en el piso {currentFloor} " +
                  $"con {boss.MaxHealth} PV; mecánica {primera}" +
                  (segunda != BossMechanic.None ? $" + {segunda}" : string.Empty) + ".", this);

        BossStateChanged?.Invoke(true);
    }

    // Refuerzos que invoca un jefe con la mecánica de Invocación. Entran en la oleada del piso
    // como uno más: cuentan para el recuento y se limpian al acabar el piso, igual que el resto.
    public static int SummonBossAdds(Vector2 origin, int count)
    {
        if (active == null || active.enemyPrefab == null || count <= 0) return 0;
        if (active.state != ExpeditionState.InProgress) return 0;

        var datos = active.enemyData;
        if (datos == null) return 0;

        int puestos = 0;
        for (int i = 0; i < count; i++)
        {
            Vector2 pos = origin + UnityEngine.Random.insideUnitCircle.normalized * bossAddSpawnRadius;
            pos.x = Mathf.Clamp(pos.x, ArenaWallMin.x, ArenaWallMax.x);
            pos.y = Mathf.Clamp(pos.y, ArenaWallMin.y, ArenaWallMax.y);

            active.waveSpawnedTotal++;
            var go = Instantiate(active.enemyPrefab, pos, Quaternion.identity);
            go.name = $"Enemy_BossAdd_F{active.currentFloor}_{active.waveSpawnedTotal}";

            var add = go.GetComponent<EnemyController>();
            add.Initialize(datos, active.waveStatMultiplier, active.waveAttackMultiplier);
            add.MarkBossAdd();
            active.wave.Add(add);
            puestos++;
        }

        return puestos;
    }

    // Distancia a la que aparecen los refuerzos alrededor del jefe.
    private const float bossAddSpawnRadius = 2.2f;

    // Friacis aparece cerca de la escuadra (no en la línea enemiga) y su vida se reporta al
    // HUD vía EscortHealthBarUI, igual que el jefe.
    private void SpawnEscort()
    {
        if (escortPrefab == null) return;

        Vector2 pos = arenaCenter + heroSpawnOffset + new Vector2(1.2f, -1.5f);
        currentEscort = Instantiate(escortPrefab, pos, Quaternion.identity);
        currentEscort.Initialize(escortMaxHealth);
        currentEscort.Defeated += OnEscortDefeated;

        Debug.Log($"[Escolta] {currentEscort.NpcName} desplegada en el piso {currentFloor} " +
                  $"con {currentEscort.MaxHealth} PV.", this);
    }

    // Cualquier enemigo vivo a menos de radius de point cuenta como amenaza real.
    private bool EnemyNear(Vector2 point, float radius)
    {
        foreach (var enemy in wave)
        {
            if (enemy == null || enemy.CurrentHealth <= 0) continue;
            if (Vector2.Distance(enemy.transform.position, point) <= radius) return true;
        }
        return false;
    }

    private void DespawnEscort()
    {
        if (currentEscort == null) return;

        currentEscort.Defeated -= OnEscortDefeated;
        Destroy(currentEscort.gameObject);
        currentEscort = null;
    }

    // Si Friacis cae, la misión fracasa de inmediato como retirada/derrota.
    private void OnEscortDefeated()
    {
        if (state != ExpeditionState.InProgress) return;

        string npcName = currentEscort != null ? currentEscort.NpcName : "Friacis";
        int floor = currentFloor;
        LogFloorPace("escolta caída");

        DespawnWave();
        RecallParty();
        SetBossFloor(false);
        countdownTimer = 0f;
        AudioManager.Play(SfxId.Defeat);

        Report(ExpeditionState.Lost, string.Format(
            LocalizationManager.Get("UI_STATUS_ESCORT_LOST"), npcName, floor));
        BattleResultReported?.Invoke(new LastBattleResult
        {
            Floor = floor,
            ResultType = BattleResultType.Defeat,
            SurvivorsCount = CountAliveDeployed()
        });
    }

    // Botín garantizado por tumbar al jefe: gemas, materiales y una Piedra de Ascensión cuyo
    // tier sube con la franja de piso, igual que los biomas (1-5 Menor ... 16+ Legendaria).
    private void GrantBossChest(int floor)
    {
        economy?.Add(bossChestGems);
        economy?.AddMaterials(bossChestMaterials, bossChestMaterials);

        var tier = StoneTierForFloor(floor);
        crafting?.AddStones(tier, 1);

        GrantWardenDrop(floor);

        VfxManager.Play(VfxId.VictoryChest, (Vector3)ArenaFocus);

        Debug.Log($"[Cofre] Botín del jefe: +{bossChestGems} gemas, " +
                  $"+{bossChestMaterials} madera, +{bossChestMaterials} hierro y +1 Piedra {tier}.", this);
    }

    // Pieza del Guardián del piso: segura la primera vez, y luego repetible por sorteo.
    // Es equipo marcado dropOnly, así que la Forja no puede sacarlo de ninguna gama.
    private void GrantWardenDrop(int floor)
    {
        if (shop == null || wardenDrops == null) return;

        for (int i = 0; i < wardenDrops.Length && i < wardenDropFloors.Length; i++)
        {
            var pieza = wardenDrops[i];
            if (pieza == null || wardenDropFloors[i] != floor) continue;

            bool primera = wardenGranted.Add(pieza.name);
            if (!primera && Random.value >= wardenRepeatChance) return;

            shop.AddToInventory(pieza);
            Debug.Log($"[Cofre] {pieza.LocalizedName()} del conjunto del Guardián" +
                      $"{(primera ? " (primera vez)" : " (repetida)")}.", this);
            return;
        }
    }

    // Las usa el SaveManager para conservar qué piezas del Guardián ya salieron.
    public IEnumerable<string> WardenGranted => wardenGranted;

    public void LoadWardenGranted(IEnumerable<string> nombres)
    {
        wardenGranted.Clear();
        if (nombres == null) return;

        foreach (var nombre in nombres)
            if (!string.IsNullOrEmpty(nombre)) wardenGranted.Add(nombre);
    }

    private AscensionStoneTier StoneTierForFloor(int floor)
    {
        if (floor <= stoneTierFloorCap[0]) return AscensionStoneTier.Menor;
        if (floor <= stoneTierFloorCap[1]) return AscensionStoneTier.Media;
        if (floor <= stoneTierFloorCap[2]) return AscensionStoneTier.Mayor;
        return AscensionStoneTier.Legendaria;
    }

    // Sortea el reto oculto del piso según su tipo de misión; nunca se anuncia el contenido,
    // solo su existencia (MissionBannerUI lee HasHiddenChallenge). Se llama con la oleada inicial
    // ya en escena (sin el jefe) para poder elegir un "explorador" objetivo del reto de asesinato.
    private void AssignHiddenChallenge()
    {
        challengeTarget = null;
        combatElapsed = 0f;
        heroDownOccurred = false;
        deployedStartCount = CountAliveDeployed();

        deployedStartMaxHealth = 0;
        foreach (var hero in deployed)
            if (hero != null) deployedStartMaxHealth += hero.MaxHealth;
        currentChallenge = HiddenChallengeType.None;

        // Ya se ganó un reto oculto en este piso antes, o no ha tocado esta vez: sin reto.
        if (hiddenChallengeAwardedFloors.Contains(currentFloor)) return;
        if (Random.value >= hiddenChallengeChance) return;

        switch (currentMissionType)
        {
            case FloorMissionType.Survival:
                currentChallenge = HiddenChallengeType.NoHeroDown;
                break;
            case FloorMissionType.Escort:
                currentChallenge = HiddenChallengeType.EscortUnharmed;
                break;
            case FloorMissionType.BossHunt:
                currentChallenge = HiddenChallengeType.SpeedClear;
                challengeSpeedLimit = hiddenBossSpeedClearSeconds;
                break;
            default:
                if (wave.Count > 0 && Random.value < 0.5f)
                {
                    currentChallenge = HiddenChallengeType.Assassinate;
                    challengeTarget = wave[Random.Range(0, wave.Count)];
                }
                else
                {
                    currentChallenge = HiddenChallengeType.SpeedClear;
                    challengeSpeedLimit = hiddenSpeedClearSeconds;
                }
                break;
        }
    }

    // Se comprueba solo en el instante de superar el piso, antes de desmontar escolta/escuadra.
    private bool HiddenChallengeCleared()
    {
        switch (currentChallenge)
        {
            case HiddenChallengeType.SpeedClear: return combatElapsed <= challengeSpeedLimit;
            case HiddenChallengeType.Assassinate: return challengeTarget == null;
            case HiddenChallengeType.NoHeroDown: return !heroDownOccurred;
            case HiddenChallengeType.EscortUnharmed:
                return currentEscort != null && currentEscort.CurrentHealth >= currentEscort.MaxHealth;
            default: return false;
        }
    }

    public static string ChallengeWonKey(HiddenChallengeType type) => type switch
    {
        HiddenChallengeType.SpeedClear => "CHALLENGE_SPEED_CLEAR_WON",
        HiddenChallengeType.Assassinate => "CHALLENGE_ASSASSINATE_WON",
        HiddenChallengeType.NoHeroDown => "CHALLENGE_NO_HERO_DOWN_WON",
        HiddenChallengeType.EscortUnharmed => "CHALLENGE_ESCORT_UNHARMED_WON",
        _ => string.Empty
    };

    // Ranura de consumible: sin inventario propio por héroe, cada desplegado se cura
    // solo con la poción más débil disponible del stock global si su vida cae por debajo del
    // umbral, hasta un cupo por héroe y expedición.
    private void TickAutoPotions()
    {
        if (crafting == null) return;

        foreach (var hero in deployed)
        {
            if (hero == null || hero.MaxHealth <= 0) continue;

            float ratio = (float)hero.CurrentHealth / hero.MaxHealth;

            // Marca al héroe como candidato al Despertar de Habilidades si sigue en pie tras
            // rozar la muerte; se resuelve de verdad solo al superar el piso.
            if (ratio <= criticalHealthRatioForAwakening) survivedCritical.Add(hero);

            if (ratio >= autoPotionHealthRatio) continue;

            potionUsesThisExpedition.TryGetValue(hero, out int used);
            if (used >= autoPotionUsesPerExpedition) continue;

            potionCooldownUntil.TryGetValue(hero, out float readyAt);
            if (Time.time < readyAt) continue;

            if (crafting.TryUseHealingPotion(hero))
            {
                potionUsesThisExpedition[hero] = used + 1;
                potionCooldownUntil[hero] = Time.time + autoPotionCooldown;
            }
        }

        TickAutoManaPotions();
    }

    // Igual que las de vida pero mirando el maná: sin esto, un héroe que gasta su habilidad se
    // quedaba seco el resto de la expedición, porque el maná ya no se regenera solo.
    private void TickAutoManaPotions()
    {
        foreach (var hero in deployed)
        {
            if (hero == null || hero.MaxMP <= 0) continue;
            if ((float)hero.CurrentMP / hero.MaxMP >= autoManaPotionRatio) continue;

            manaPotionUsesThisExpedition.TryGetValue(hero, out int used);
            if (used >= autoManaPotionUsesPerExpedition) continue;

            manaPotionCooldownUntil.TryGetValue(hero, out float readyAt);
            if (Time.time < readyAt) continue;

            if (crafting.TryUseManaPotion(hero))
            {
                manaPotionUsesThisExpedition[hero] = used + 1;
                manaPotionCooldownUntil[hero] = Time.time + autoPotionCooldown;
            }
        }
    }

    // Da a cada héroe elegible (sobrevivió crítico esta expedición, o el piso era de jefe y
    // cayó) una tirada de Despertar de Habilidades al superar el piso.
    private void TryAwakenSkills(bool bossKilled)
    {
        foreach (var hero in deployed)
        {
            if (hero == null || hero.CurrentHealth <= 0) continue;
            if (!bossKilled && !survivedCritical.Contains(hero)) continue;
            if (Random.value >= skillAwakeningChance) continue;

            // Una parte de los despertares cambia la habilidad activa en vez de dar una pasiva.
            if (Random.value < abilityAwakeningShare) ActiveSkills.TryAwaken(hero);
            else PassiveSkills.TryAwaken(hero);
        }
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

            // La escuadra sigue reconociendo o colocándose: se estira la preparación en vez de
            // soltar la oleada. Hay que mantener el reloj por encima de 0 o este bloque deja de
            // entrar y la oleada no se suelta nunca.
            if (choreographer != null && !choreographer.CombatReady)
            {
                countdownTimer = 0.1f;
                return;
            }

            countdownTimer = 0f;
            ReleaseWave();
        }

        combatElapsed += Time.deltaTime;
        if (!heroDownOccurred && CountAliveDeployed() < deployedStartCount) heroDownOccurred = true;

        TickAutoPotions();
        TickReinforcements();
        UpdateRegroup();

        int aliveNow = AliveEnemies;

        // Piso 5 (Filtro de Supervivencia): el reloj corre mientras haya combate, y
        // entra un enemigo nuevo cada tanto para que la oleada nunca se vacíe antes de tiempo.
        if (currentMissionType == FloorMissionType.Survival)
        {
            survivalTimer -= Time.deltaTime;

            survivalRespawnTimer -= Time.deltaTime;
            if (survivalRespawnTimer <= 0f)
            {
                survivalRespawnTimer = survivalRespawnInterval;
                SpawnSurvivalWave();
            }
        }

        // Pisos 10/15/25 (asedio/escolta): Friacis solo se desgasta si hay enemigos vivos cerca
        // de verdad (todavía no le apuntan como a un héroe, pero al menos el daño depende de
        // que la amenaza esté encima suyo, no de un cronómetro ciego).
        if (currentEscort != null && EnemyNear(currentEscort.transform.position, escortThreatRadius))
        {
            float dps = currentIsSiege ? siegeDamagePerSecond : escortDamagePerSecond;
            escortDamageAccumulator += dps * Time.deltaTime;
            int chip = Mathf.FloorToInt(escortDamageAccumulator);
            if (chip > 0)
            {
                escortDamageAccumulator -= chip;
                currentEscort.TakeDamage(chip);
            }
        }

        // El piso no está despejado mientras queden refuerzos por entrar, aunque no haya nadie
        // vivo en pantalla en este instante.
        bool normalClear = currentMissionType != FloorMissionType.Survival
                           && aliveNow == 0 && pendingReinforcements.Count == 0;
        bool survivalClear = currentMissionType == FloorMissionType.Survival && survivalTimer <= 0f;

        if (normalClear || survivalClear)
        {
            int cleared = currentFloor;
            LogFloorPace("victoria");

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

            // Sub-Misión Oculta: se resuelve aquí, con la escolta todavía viva si la había,
            // antes de que DespawnEscort()/RecallParty() borren el estado que hace falta leer.
            // Se anuncia dentro del mismo mensaje de piso superado, no en un toast aparte.
            HiddenChallengeType challengeWonType = HiddenChallengeType.None;
            if (currentChallenge != HiddenChallengeType.None && HiddenChallengeCleared())
            {
                crafting?.AddStones(StoneTierForFloor(cleared), 1);
                economy?.Add(hiddenChallengeGemBonus);
                hiddenChallengeAwardedFloors.Add(cleared);
                challengeWonType = currentChallenge;
            }
            currentChallenge = HiddenChallengeType.None;

            string challengeText = challengeWonType != HiddenChallengeType.None
                ? "  |  " + string.Format(LocalizationManager.Get(ChallengeWonKey(challengeWonType)), hiddenChallengeGemBonus)
                : string.Empty;

            int expGain = Mathf.Max(1, expReward * cleared);
            GrantCombatExp(cleared);

            // Ganar sube la moral SOLO de la escuadra que peleó. Antes barría el roster entero,
            // así que repetir un piso fácil mantenía a toda la base con la moral llena gratis y
            // la insubordinación no llegaba a dispararse nunca.
            foreach (var hero in deployed)
                if (hero != null && hero.CurrentHealth > 0) hero.AddMorale(moraleRewardOnWin);

            // Despertar de Habilidades: se resuelve con la escuadra todavía desplegada (antes
            // de RecallParty(), que vacía `deployed`).
            TryAwakenSkills(bossChest);

            // Hoja de servicios y vínculos: tienen que resolverse AQUÍ, con la escuadra todavía
            // desplegada. Estaban después de RecallParty(), que vacía `deployed`, así que
            // recorrían una lista vacía y no sumaban nunca.
            var enPie = new List<HeroController>();
            foreach (var hero in deployed)
                if (hero != null && hero.CurrentHealth > 0) enPie.Add(hero);

            HeroBonds.RecordFloorCleared(deployed);
            foreach (var hero in enPie) hero.RecordFloorCleared();

            // Friacis (si la había) no muere al ganar, pero su barra debe desaparecer con el
            // resto de la oleada — si no, se queda visible en el HUD de vuelta en la base.
            DespawnEscort();
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

            // Lo que se quedó en este piso al caer alguien vuelve al almacén al despejarlo.
            int rescatadas = LostGearManager.Reclaim(cleared, shop);
            if (rescatadas > 0)
                ScreenBanner.ShowCompact(
                    string.Format(LocalizationManager.Get("UI_GEAR_RECOVERED"), cleared, rescatadas),
                    3f, UITheme.Accent);

            ReportFallen();

            // Uno de los que siguen en pie lo celebra; con todos hablando no se leería nada.
            if (enPie.Count > 0)
                enPie[Random.Range(0, enPie.Count)].Bark(1f,
                    "BATTLE_VICTORY_1", "BATTLE_VICTORY_2", "BATTLE_VICTORY_3", "BATTLE_VICTORY_4");

            AudioManager.Play(SfxId.Victory);
            QuestManager.Report(QuestKind.ClearFloors);
            if (bossChest) QuestManager.Report(QuestKind.WinBossFloor);
            if (!heroDownOccurred) QuestManager.Report(QuestKind.WinNoLosses);
            if (challengeWonType != HiddenChallengeType.None)
                QuestManager.Report(QuestKind.WinHiddenChallenge);
            FloorCleared?.Invoke(new FloorRewardInfo
            {
                floor = cleared,
                firstClear = firstClear,
                bossFloor = bossChest,
                gems = gemGain + (bossChest ? bossChestGems : 0),
                wood = woodGain + (bossChest ? bossChestMaterials : 0),
                iron = ironGain + (bossChest ? bossChestMaterials : 0),
                exp = expGain,
                hiddenChallengeType = challengeWonType,
                hiddenChallengeGems = hiddenChallengeGemBonus
            });

            Report(ExpeditionState.Won, string.Format(
                LocalizationManager.Get("UI_STATUS_WON"), cleared, modo, gemGain, woodGain, ironGain, challengeText));
            SetBossFloor(false);
            return;
        }

        // Se pierde cuando cae toda la escuadra, no cuando cae todo el roster.
        if (CountAliveDeployed() == 0)
        {
            LogFloorPace("derrota");
            ReportFallen();
            DespawnWave();
            RecallParty();
            SetBossFloor(false);
            countdownTimer = 0f;
            AudioManager.Play(SfxId.Defeat);
            Report(ExpeditionState.Lost, string.Format(
                LocalizationManager.Get("UI_STATUS_LOST"), currentFloor));
            BattleResultReported?.Invoke(new LastBattleResult
            {
                Floor = currentFloor,
                ResultType = BattleResultType.Defeat,
                SurvivorsCount = 0
            });
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
        pendingReinforcements.Clear();
        currentBoss = null;
        DespawnEscort();
    }

    // Cronómetro de piso: la única lectura fiable del ritmo real de combate. Sale por consola
    // con lo que hace falta para saber por qué duró lo que duró, no solo cuánto.
    private void LogFloorPace(string desenlace)
    {
        PruneWave();
        Debug.Log($"[Ritmo] Piso {currentFloor} | {desenlace} | {combatElapsed:0.0}s | "
                  + $"oleada {waveSpawnedTotal + pendingReinforcements.Count} "
                  + $"(vivos {wave.Count}, sin entrar {pendingReinforcements.Count}) | "
                  + $"héroes {CountAliveDeployed()}/{deployedStartCount} | "
                  + $"multVida x{waveStatMultiplier:0.00} multATK x{waveAttackMultiplier:0.00}", this);
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
