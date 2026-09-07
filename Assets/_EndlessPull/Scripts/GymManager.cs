using UnityEngine;
using UnityEngine.InputSystem;

// Arena cerrada de entrenamiento: coloca las unidades del episodio y permite acelerar la simulación.
public class GymManager : MonoBehaviour
{
    [Tooltip("Prefab del enemigo que se instancia en la arena.")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Enemigo de reserva si no hay ningún sorteo configurado.")]
    [SerializeField] private EnemyData enemyData;

    [Tooltip("Enemigos normales del entrenamiento; se sortea uno por episodio.")]
    [SerializeField] private EnemyData[] trainingPool;

    [Tooltip("Jefes del entrenamiento, con sus mecánicas; salen según bossChance.")]
    [SerializeField] private EnemyData[] bossPool;

    [Tooltip("Probabilidad de que el episodio sea contra un jefe en vez de un enemigo normal.")]
    [SerializeField, Range(0f, 1f)] private float bossChance = 0.25f;

    [Tooltip("Número de jefe más alto que se sortea; marca hasta dónde llegan las mecánicas dobles.")]
    [SerializeField] private int trainedBossRange = 16;

    [Tooltip("Sortea también el arma del héroe en cada episodio; sin esto solo aprende de melé.")]
    [SerializeField] private bool randomizeHeroWeapon = true;

    [Tooltip("Separación mínima y máxima entre héroe y enemigo al empezar el episodio.")]
    [SerializeField] private Vector2 startingGap = new Vector2(3f, 11f);

    [Tooltip("Multiplicador de vida y ataque del enemigo de entrenamiento.")]
    [SerializeField] private float enemyMultiplier = 1f;

    [Tooltip("Entrena contra un jefe, con golpe circular y aviso previo.")]
    [SerializeField] private bool enemyIsBoss = true;

    [Tooltip("Escala visual del jefe de entrenamiento.")]
    [SerializeField] private float bossScale = 1.6f;

    [Tooltip("Punto donde arranca el héroe en cada episodio.")]
    [SerializeField] private Vector2 heroSpawn = new Vector2(-4f, 0f);

    [Tooltip("Punto donde arranca el enemigo en cada episodio.")]
    [SerializeField] private Vector2 enemySpawn = new Vector2(4f, 0f);

    [Tooltip("Media anchura y altura de la arena; el agente no puede salir de ahí.")]
    [SerializeField] private Vector2 arenaHalfSize = new Vector2(9f, 4.5f);

    [Tooltip("Velocidad de simulación en modo rápido.")]
    [SerializeField] private float fastTimeScale = 10f;

    [Tooltip("Arranca ya en modo rápido, para entrenar sin tocar nada.")]
    [SerializeField] private bool startFast;

    [Tooltip("Segundos de simulación entre pulsos del decreto Reagruparse simulado, para que el " +
             "agente aprenda a reaccionar a las órdenes del comandante igual que en combate real.")]
    [SerializeField] private float regroupPulseInterval = 12f;

    [Tooltip("Duración del pulso, mismo criterio que MasterCommander.Regroup.")]
    [SerializeField] private float regroupPulseDuration = 2f;

    [Tooltip("Retroceso instantáneo del pulso.")]
    [SerializeField] private float regroupPulseRetreat = 1.5f;

    private EnemyController enemy;
    private HeroController currentHero;
    private float regroupPulseTimer;
    private bool fast;
    private int episodes;

    public Vector2 HeroSpawn => heroSpawn;
    public Vector2 EnemySpawn => enemySpawn;
    public Vector2 ArenaHalfSize => arenaHalfSize;
    public EnemyController Enemy => enemy;
    public int Episodes => episodes;
    public bool IsFast => fast;

    void Start()
    {
        SetFast(startFast);
    }

    // La tecla F alterna la velocidad; en el editor hace falta foco en la Game View.
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame) ToggleFastMode();

        TickRegroupPulse();
    }

    // Simula el decreto Reagruparse a intervalos, para que HeroAgent entrene la penalización de
    // desobedecerlo (ver decreeDisobeyPenalty). Time.deltaTime ya viene escalado por timeScale,
    // así que el pulso se acelera solo junto con el resto de la simulación en modo rápido.
    private void TickRegroupPulse()
    {
        if (currentHero == null || currentHero.IsDead) return;

        regroupPulseTimer -= Time.deltaTime;
        if (regroupPulseTimer > 0f) return;

        regroupPulseTimer = regroupPulseInterval;
        currentHero.ApplyDefensiveStance(regroupPulseDuration, regroupPulseRetreat);
    }

    public void ToggleFastMode() => SetFast(!fast);

    // fixedDeltaTime acompaña a timeScale, o la física y las decisiones se desincronizan al acelerar.
    public void SetFast(bool value)
    {
        fast = value;
        Time.timeScale = fast ? Mathf.Max(1f, fastTimeScale) : 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        Debug.Log($"[Gym] Simulación x{Time.timeScale:0.#}", this);
    }

    // Cada episodio recicla el enemigo: se destruye el anterior y se instancia uno limpio.
    public EnemyController ResetArena(HeroController hero)
    {
        episodes++;

        if (enemy != null)
        {
            enemy.gameObject.SetActive(false);
            Destroy(enemy.gameObject);
        }

        // La marca del suelo sobrevive al jefe que la puso: el episodio nuevo empieza limpio.
        foreach (var zona in UnityEngine.Object.FindObjectsByType<BossGroundZone>(FindObjectsSortMode.None))
        {
            zona.gameObject.SetActive(false);
            Destroy(zona.gameObject);
        }

        if (hero != null)
        {
            hero.transform.position = heroSpawn;

            if (randomizeHeroWeapon) SortearArma(hero);

            // Sin desplegar, el jefe no lo ve: tanto su objetivo como la marca del suelo y el
            // golpe circular solo miran a héroes que estén en la arena.
            hero.SetDeployed(true);
            hero.ResetForEpisode();
        }

        currentHero = hero;
        regroupPulseTimer = regroupPulseInterval;

        // Un jefe cada pocas veces; el resto del tiempo, enemigos normales. Entrenar solo contra
        // el jefe enseñaba a esquivar el golpe circular y nada más: ni acercarse a un tirador, ni
        // mantener la distancia con uno rápido, ni aguantar a uno lento y duro.
        bool esJefe = bossPool != null && bossPool.Length > 0
                      && UnityEngine.Random.value < bossChance;

        var datos = esJefe ? Sortear(bossPool) : Sortear(trainingPool);
        if (datos == null) datos = enemyData;

        if (enemyPrefab == null || datos == null)
        {
            Debug.LogError("[Gym] Falta el prefab o los datos del enemigo de entrenamiento.", this);
            return null;
        }

        // La distancia de salida cambia en cada episodio: así aprende a cerrar hueco y a
        // mantenerlo, en vez de memorizar una sola apertura.
        float hueco = UnityEngine.Random.Range(startingGap.x, startingGap.y);
        Vector2 puesto = new Vector2(heroSpawn.x + hueco, enemySpawn.y);

        var go = Instantiate(enemyPrefab, Clamp(puesto), Quaternion.identity);
        go.name = esJefe ? "Gym_Boss" : "Gym_Enemy";

        enemy = go.GetComponent<EnemyController>();
        enemy.Initialize(datos, enemyMultiplier);

        if (esJefe || enemyIsBoss && bossPool == null)
        {
            enemy.MakeBoss(bossScale);

            // Las mecánicas salen de la MISMA rotación que la Torre, sorteando número de jefe: así
            // entrena contra las parejas que se va a encontrar de verdad, sueltas o dobles.
            int numeroDeJefe = UnityEngine.Random.Range(1, trainedBossRange + 1);
            enemy.SetMechanics(BossMechanics.Primary(numeroDeJefe),
                               BossMechanics.Secondary(numeroDeJefe));
        }

        return enemy;
    }

    private static readonly WeaponType[] Arquetipos =
    {
        WeaponType.Sword, WeaponType.Spear, WeaponType.Shield,
        WeaponType.Bow, WeaponType.Staff, WeaponType.Mace
    };

    // La subclase es lo que decide el arquetipo de arma de un héroe sin equipo puesto, que es el
    // caso del muñeco del gimnasio. Se limpia el repertorio antes o acaba acumulando una habilidad
    // por episodio.
    private static void SortearArma(HeroController hero)
    {
        var arquetipo = Arquetipos[UnityEngine.Random.Range(0, Arquetipos.Length)];

        hero.ClearAbilities();
        hero.SetSubclass(HeroSubclasses.RandomFor(arquetipo, HeroSubclasses.MinStarRank));
    }

    private static EnemyData Sortear(EnemyData[] pool)
    {
        if (pool == null || pool.Length == 0) return null;

        // Un hueco vacío en el array del inspector no puede tumbar el episodio.
        for (int intento = 0; intento < 8; intento++)
        {
            var elegido = pool[UnityEngine.Random.Range(0, pool.Length)];
            if (elegido != null) return elegido;
        }

        return null;
    }

    // Mantiene al agente dentro de la arena; devuelve la posición ya recortada.
    public Vector2 Clamp(Vector2 position)
        => new Vector2(
            Mathf.Clamp(position.x, -arenaHalfSize.x, arenaHalfSize.x),
            Mathf.Clamp(position.y, -arenaHalfSize.y, arenaHalfSize.y));

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(arenaHalfSize.x * 2f, arenaHalfSize.y * 2f, 0f));
    }
}
