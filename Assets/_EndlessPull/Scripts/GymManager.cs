using System.Collections.Generic;
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

    [Tooltip("Sortea el perfil táctico, las pasivas y el ánimo del héroe en cada episodio.")]
    [SerializeField] private bool randomizeHeroTraits = true;

    [Tooltip("Pasivas de las que se le reparten 0-2 por episodio; son las que mueven el vector.")]
    [SerializeField] private PassiveSkill[] traitPassivePool =
    {
        PassiveSkill.Bloodlust, PassiveSkill.Bulwark, PassiveSkill.Tactician, PassiveSkill.Scout,
        PassiveSkill.Duelist, PassiveSkill.Ambusher, PassiveSkill.MentalFortitude,
        PassiveSkill.Leadership, PassiveSkill.Executioner, PassiveSkill.Indomitable,
        PassiveSkill.Strategist, PassiveSkill.Vanguard, PassiveSkill.Bannerman,
        PassiveSkill.Lonewolf, PassiveSkill.Adaptability
    };

    [Tooltip("Separación mínima y máxima entre héroe y enemigo al empezar el episodio.")]
    [SerializeField] private Vector2 startingGap = new Vector2(3f, 11f);

    [Tooltip("Multiplicador de vida y ataque del enemigo de entrenamiento.")]
    [SerializeField] private float enemyMultiplier = 1f;

    [Tooltip("Entrena contra un jefe, con golpe circular y aviso previo.")]
    [SerializeField] private bool enemyIsBoss = true;

    [Tooltip("Escala visual del jefe de entrenamiento.")]
    [SerializeField] private float bossScale = 1.6f;

    [Tooltip("Puesto de salida del héroe, relativo al centro de la arena.")]
    [SerializeField] private Vector2 heroSpawn = new Vector2(-4f, 0f);

    [Tooltip("Puesto de salida del enemigo, relativo al centro de la arena.")]
    [SerializeField] private Vector2 enemySpawn = new Vector2(4f, 0f);

    [Tooltip("Media anchura y altura de la arena; el agente no puede salir de ahí.")]
    [SerializeField] private Vector2 arenaHalfSize = new Vector2(9f, 4.5f);

    [Tooltip("Holgura alrededor de la arena para dar por suya una marca de suelo del jefe.")]
    [SerializeField] private float zoneMargin = 6f;

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

    // La arena se sitúa en el transform del GymManager, no en el origen del mundo: así se
    // duplica el objeto entero para entrenar en varias a la vez sin que se pisen las posiciones.
    public Vector2 Origin => transform.position;

    public Vector2 HeroSpawn => Origin + heroSpawn;
    public Vector2 EnemySpawn => Origin + enemySpawn;
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

    // timeScale sube el ritmo SIN tocar fixedDeltaTime. Escalarlo también dejaba pasos de física
    // de 0,4 s: los proyectiles saltaban 4,6 unidades por frame y el aviso del jefe se perdía
    // entero. El caudal de muestras se saca duplicando arenas, no subiendo más el timeScale.
    public void SetFast(bool value)
    {
        fast = value;
        Time.timeScale = fast ? Mathf.Max(1f, fastTimeScale) : 1f;

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
        // Solo las de esta arena, o borraría las de las vecinas a media rotación.
        foreach (var zona in UnityEngine.Object.FindObjectsByType<BossGroundZone>(FindObjectsSortMode.None))
        {
            if (!Contains(zona.transform.position)) continue;
            zona.gameObject.SetActive(false);
            Destroy(zona.gameObject);
        }

        if (hero != null)
        {
            hero.transform.position = Origin + heroSpawn;

            // Antes del reset: las pasivas cambian la vida máxima y el reset la deja a tope.
            if (randomizeHeroWeapon) SortearArma(hero);
            if (randomizeHeroTraits) SortearRasgos(hero);

            // Sin desplegar, el jefe no lo ve: tanto su objetivo como la marca del suelo y el
            // golpe circular solo miran a héroes que estén en la arena.
            hero.SetDeployed(true);
            hero.ResetForEpisode();

            // Después del reset, que ahí la fatiga vuelve a 0 y la moral a la inicial.
            if (randomizeHeroTraits) SortearAnimo(hero);
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
        Vector2 puesto = Origin + new Vector2(heroSpawn.x + hueco, enemySpawn.y);

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

    // El vector de rasgos que ve la red sale de la personalidad, las pasivas, la moral, la fatiga
    // y las horas de muñeco. Sin sortearlo, sus tres canales valían 0,5 en TODOS los episodios: la
    // red aprendía a ignorarlos y en la Torre le llegarían valores que no ha visto nunca.
    private void SortearRasgos(HeroController hero)
    {
        // LoadTactics y no SetTactics: el segundo pide guardar partida, y aquí eso son miles de
        // escrituras del savegame real por tanda.
        var progress = hero.GetComponent<HeroProgress>();
        if (progress != null)
        {
            progress.LoadTactics(UnityEngine.Random.value, UnityEngine.Random.value,
                                 UnityEngine.Random.Range(0.05f, 0.45f));
            progress.LoadSkillRefinement(UnityEngine.Random.Range(0f, progress.RefinementCap));
        }

        var sorteadas = new List<PassiveSkill>();
        if (traitPassivePool != null && traitPassivePool.Length > 0)
        {
            int cuantas = UnityEngine.Random.Range(0, 3);
            for (int i = 0; i < cuantas; i++)
            {
                var candidata = traitPassivePool[UnityEngine.Random.Range(0, traitPassivePool.Length)];
                if (!sorteadas.Contains(candidata)) sorteadas.Add(candidata);
            }
        }

        hero.SetPassives(sorteadas);
    }

    // Fatiga y moral son las que hunden templanza y cooperación; sin variarlas esos dos canales
    // no bajan nunca de 0,5 por mucho que se sortee lo demás.
    private static void SortearAnimo(HeroController hero)
    {
        hero.AddFatigue(UnityEngine.Random.Range(0f, 60f));
        hero.LoseMorale(UnityEngine.Random.Range(0f, 45f));
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
    {
        Vector2 local = position - Origin;
        return Origin + new Vector2(
            Mathf.Clamp(local.x, -arenaHalfSize.x, arenaHalfSize.x),
            Mathf.Clamp(local.y, -arenaHalfSize.y, arenaHalfSize.y));
    }

    // Si un punto es de esta arena, con holgura para la marca del suelo del jefe.
    public bool Contains(Vector2 position)
    {
        Vector2 local = position - Origin;
        return Mathf.Abs(local.x) <= arenaHalfSize.x + zoneMargin
            && Mathf.Abs(local.y) <= arenaHalfSize.y + zoneMargin;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position,
                            new Vector3(arenaHalfSize.x * 2f, arenaHalfSize.y * 2f, 0f));
    }
}
