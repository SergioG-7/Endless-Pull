using System;
using System.Collections;
using UnityEngine;

// Estados del enemigo: espera, persigue al héroe o le golpea.
public enum EnemyState
{
    Idle,
    Approach,
    Attack
}

public class EnemyController : MonoBehaviour, IHealthOwner
{
    [Tooltip("Datos del enemigo: stats y velocidad.")]
    [SerializeField] private EnemyData data;

    [Tooltip("Radio en el que el enemigo detecta héroes.")]
    [SerializeField] private float detectionRange = 5f;

    [Tooltip("Alcance de reserva si el EnemyData no trae uno propio.")]
    [SerializeField] private float attackRange = 1.1f;

    [Tooltip("Hasta dónde cierra de más al llegar, en tanto por uno del alcance; deja margen.")]
    [SerializeField] private float approachOvershoot = 0.85f;

    [Tooltip("Cuánto tiene que alejarse el héroe, en tanto por uno del alcance, para dejar de pegar.")]
    [SerializeField] private float attackExitFactor = 1.20f;

    [Tooltip("Alcance a partir del cual el enemigo dispara en vez de golpear de cerca.")]
    [SerializeField] private float rangedThreshold = 3f;

    [Tooltip("Recorte del daño a distancia en tanto por uno; los tiradores no deben poder oneshotear.")]
    [SerializeField] private float rangedDamageMultiplier = 0.7f;

    [Tooltip("Cada cuántos segundos vuelve a buscar héroes cercanos.")]
    [SerializeField] private float scanInterval = 0.25f;

    [Tooltip("Vida a partir de la cual el enemigo se considera línea frontal (orcos/jefes) y nunca retrocede.")]
    [SerializeField] private float tankHealthThreshold = 70f;

    [Tooltip("Fracción del alcance por debajo de la cual tiradores y chamanes se retiran del héroe.")]
    [SerializeField] private float safeDistanceFactor = 0.55f;

    [Tooltip("Daño acumulado en la ventana de ráfaga, como fracción de la vida máxima, que dispara el micro-paso de recolocación.")]
    [SerializeField] private float burstDamageThreshold = 0.12f;

    [Tooltip("Segundos que dura la ventana en la que se suma el daño de ráfaga.")]
    [SerializeField] private float burstWindowSeconds = 0.6f;

    [Tooltip("Distancia del micro-paso de recolocación al encajar una ráfaga.")]
    [SerializeField] private float repositionStep = 0.4f;

    [Tooltip("Enemigos que puede sujetar un mismo tanque antes de que el resto flanquee.")]
    [SerializeField] private int maxAggroPerTank = 2;

    [Tooltip("Radio del golpe circular del jefe.")]
    [SerializeField] private float bossSlamRadius = 3f;

    [Tooltip("Segundos entre golpes circulares del jefe.")]
    [SerializeField] private float bossSlamInterval = 5f;

    [Tooltip("Daño del golpe circular en tanto por uno sobre el ataque normal.")]
    [SerializeField] private float bossSlamDamageFactor = 0.8f;

    [Tooltip("Segundos de aviso antes de que caiga el golpe circular.")]
    [SerializeField] private float bossSlamWindup = 1.2f;

    [Tooltip("Tinte del jefe mientras carga el golpe.")]
    [SerializeField] private Color bossWindupTint = new Color(1f, 0.25f, 0.20f);

    [Tooltip("Vida de la barrera del jefe, en tanto por uno de su vida máxima.")]
    [SerializeField] private float barrierFraction = 0.25f;

    [Tooltip("Vida a la que el jefe con Barrera vuelve a levantarla una segunda vez.")]
    [SerializeField] private float barrierRaiseAgainAt = 0.5f;

    [Tooltip("Tinte del jefe mientras la barrera aguanta.")]
    [SerializeField] private Color barrierTint = new Color(0.45f, 0.70f, 1f);

    [Tooltip("Refuerzos que invoca el jefe en cada umbral de vida.")]
    [SerializeField] private int summonAdds = 2;

    [Tooltip("Fracciones de vida a las que el jefe invoca refuerzos.")]
    [SerializeField] private float[] summonThresholds = { 0.66f, 0.33f };

    [Tooltip("Segundos entre zonas del suelo.")]
    [SerializeField] private float zoneInterval = 8f;

    [Tooltip("Aviso previo antes de que la zona empiece a quemar.")]
    [SerializeField] private float zoneWindup = 1.3f;

    [Tooltip("Segundos que la zona sigue haciendo daño una vez activa.")]
    [SerializeField] private float zoneDuration = 4f;

    [Tooltip("Radio de la zona del suelo.")]
    [SerializeField] private float zoneRadius = 2.4f;

    [Tooltip("Daño por segundo dentro de la zona, en tanto por uno del ataque del jefe.")]
    [SerializeField] private float zoneDamageFactor = 0.45f;

    [Tooltip("Vida por debajo de la cual el jefe entra en frenesí.")]
    [SerializeField] private float frenzyThreshold = 0.30f;

    [Tooltip("Ataque del jefe en frenesí.")]
    [SerializeField] private float frenzyAttackMultiplier = 1.35f;

    [Tooltip("Enfriamiento de golpe en frenesí; por debajo de 1 pega más a menudo.")]
    [SerializeField] private float frenzyCooldownFactor = 0.6f;

    [Tooltip("Fracción del daño que el jefe se cura mientras dura el frenesí.")]
    [SerializeField] private float frenzyLifeSteal = 0.25f;

    [Tooltip("Tinte del jefe en frenesí.")]
    [SerializeField] private Color frenzyTint = new Color(1f, 0.45f, 0.35f);

    [Tooltip("Fracción de la vida máxima en un solo golpe a partir de la cual se ve el flash blanco.")]
    [SerializeField] private float hitFlashThreshold = 0.12f;

    [Tooltip("Segundos que dura el flash blanco al recibir un golpe fuerte o una ráfaga.")]
    [SerializeField] private float hitFlashDuration = 0.08f;

    // Congelado durante la cuenta atras previa al combate: ni piensa ni se mueve.
    private bool frozen;

    private EnemyState state = EnemyState.Idle;
    private int currentHealth;
    private float attackTimer;
    private float scanTimer;
    private HeroController target;

    // Multiplicador de piso: escala vida y ataque sin tocar el EnemyData compartido.
    private float statMultiplier = 1f;

    // A partir de cierto piso el ataque sube más deprisa que la vida; de ahí el multiplicador aparte.
    private float attackMultiplier = 1f;

    // Los jefes pegan además un golpe en área cada pocos segundos.
    private bool isBoss;
    private float slamTimer;

    // Mecánicas del jefe: una fija por rotación y, en los jefes altos, una segunda encima.
    private BossMechanic mechanicA = BossMechanic.None;
    private BossMechanic mechanicB = BossMechanic.None;

    private float barrierHealth;
    private bool barrierRaisedTwice;
    private int summonsDone;
    private float zoneTimer;
    private bool frenzied;
    private float frenzyMultiplier = 1f;

    public bool HasBarrier => barrierHealth > 0f;
    public bool IsFrenzied => frenzied;

    // Refuerzo invocado por un jefe: la escuadra los atiende antes para que no se acumulen.
    public bool IsBossAdd { get; private set; }

    public void MarkBossAdd() => IsBossAdd = true;

    public bool HasMechanic(BossMechanic mechanic)
        => mechanic != BossMechanic.None && (mechanicA == mechanic || mechanicB == mechanic);

    // La llama el WaveManager justo después de MakeBoss, con lo que le toque a ese jefe.
    public void SetMechanics(BossMechanic first, BossMechanic second)
    {
        mechanicA = first;
        mechanicB = second;

        if (HasMechanic(BossMechanic.Barrier)) RaiseBarrier();
        if (HasMechanic(BossMechanic.GroundZone)) zoneTimer = zoneInterval;
    }

    // Aviso previo: el golpe se ve venir, para que dé tiempo a reagrupar.
    private bool windingUp;
    private float windupTimer;
    private SpriteRenderer body;
    private Color baseTint = Color.white;
    private LPCAnimator animator;

    private SpriteRenderer telegraph;
    private static Sprite sharedCircle;

    // Acumula el daño reciente para detectar ráfagas y disparar el micro-paso de recolocación.
    private float recentBurstDamage;
    private float burstWindowTimer;
    private Coroutine flashRoutine;

    public EnemyData Data => data;
    public EnemyState State => state;
    public bool IsBoss => isBoss;
    public bool IsWindingUp => windingUp;
    public bool IsFrozen => frozen;

    // Lo lee el agente para saber a qué distancia deja de alcanzarle el golpe circular.
    public float SlamRadius => bossSlamRadius;

    // Lo lee el aggro para contar cuántos enemigos apuntan ya a un mismo tanque.
    public HeroController CurrentTarget => target;

    private StatusEffectManager status;

    public StatusEffectManager Status
    {
        get
        {
            if (status == null) status = StatusEffectManager.For(gameObject);
            return status;
        }
    }

    // Provocación del Paladín: mientras dure, este enemigo no mira a nadie más.
    private HeroController taunter;
    private float tauntTimer;

    public void Taunt(HeroController by, float duration)
    {
        taunter = by;
        tauntTimer = duration;
        target = by;
        if (state == EnemyState.Idle) state = EnemyState.Approach;
    }

    // Empujón: se separa del origen sin atravesar nada, que aquí no hay colisiones.
    public void PushBack(Vector2 from, float distance)
    {
        Vector2 direccion = ((Vector2)transform.position - from).normalized;
        transform.position = (Vector2)transform.position + direccion * distance;
    }

    // La usa el WaveManager mientras corre la preparacion tactica.
    public void SetFrozen(bool value) => frozen = value;

    // El alcance sale del asset; el campo del componente solo cubre datos antiguos.
    public float AttackRange => data != null && data.attackRange > 0f ? data.attackRange : attackRange;
    public float StatMultiplier => statMultiplier;

    // Tiradores y chamanes: mismo criterio que ya usaba TickAttack para elegir disparo en vez de golpe.
    public bool IsRanged => AttackRange >= rangedThreshold;

    // Orcos y jefes: línea frontal, nunca kitean aunque cambien los datos del enemigo.
    public bool IsFrontline => isBoss || (data != null && data.maxHealth >= tankHealthThreshold);

    public int CurrentHealth => currentHealth;
    public int MaxHealth => data != null ? Mathf.RoundToInt(data.maxHealth * statMultiplier) : 0;
    public int Attack => data != null
        ? Mathf.RoundToInt(data.baseAttack * attackMultiplier * frenzyMultiplier * (IsRanged ? rangedDamageMultiplier : 1f))
        : 0;

    public float AttackMultiplier => attackMultiplier;

    // En frenesí el jefe golpea más a menudo; el resto del tiempo es el valor del asset.
    private float AttackCooldown
        => data != null ? data.attackCooldown * (frenzied ? frenzyCooldownFactor : 1f) : 1f;

    // Intervención del Maestro: mientras dure, la armadura de este enemigo no descuenta nada.
    private float armorBrokenUntil;

    public void SetArmorBroken(float seconds)
        => armorBrokenUntil = Mathf.Max(armorBrokenUntil, Time.time + Mathf.Max(0f, seconds));

    public bool IsArmorBroken => Time.time < armorBrokenUntil;
    public event Action<int, int> HealthChanged;

    void Awake()
    {
        animator = GetComponent<LPCAnimator>();
        body = GetComponent<SpriteRenderer>();
        if (body != null) baseTint = body.color;

        if (data != null)
        {
            currentHealth = MaxHealth;
            ApplyBodySprite();
        }
    }

    // Pone el sprite LPC del enemigo y le pasa los 36 recortes al animador; sin hoja se queda
    // con el cuadro rojo de reserva del prefab.
    private void ApplyBodySprite()
    {
        if (data == null || data.walkSheet == null || animator == null) return;
        animator.SetFrames(LPCAnimator.SliceWalkSheet(data.walkSheet));
    }

    // La usa el WaveManager: asigna datos y escalado tras instanciar, antes del primer Start.
    public void Initialize(EnemyData enemyData, float multiplier)
        => Initialize(enemyData, multiplier, 1f);

    public void Initialize(EnemyData enemyData, float multiplier, float attackScale)
    {
        data = enemyData;
        statMultiplier = Mathf.Max(0.01f, multiplier);
        attackMultiplier = Mathf.Max(0.01f, attackScale);

        currentHealth = MaxHealth;
        HealthChanged?.Invoke(currentHealth, MaxHealth);
        ApplyBodySprite();
    }

    // La usa el WaveManager en los pisos de jefe: escala el cuerpo y activa el golpe en área.
    public void MakeBoss(float visualScale)
    {
        isBoss = true;
        slamTimer = bossSlamInterval;
        transform.localScale *= visualScale;

        body = GetComponent<SpriteRenderer>();
        if (body != null) baseTint = body.color;
    }

    void Start()
    {
        if (data == null)
        {
            Debug.LogError($"[EnemyController] '{name}' no tiene EnemyData asignado.", this);
            enabled = false;
        }
    }

    void Update()
    {
        if (frozen) return;

        // Aturdido se queda quieto; el aviso del golpe se congela con él.
        if (Status.IsStunned) return;

        if (tauntTimer > 0f) tauntTimer -= Time.deltaTime;
        if (burstWindowTimer > 0f) burstWindowTimer -= Time.deltaTime;

        if (isBoss)
        {
            TickBossSlam();
            TickBossMechanics();
        }

        ScanForHeroes();

        switch (state)
        {
            case EnemyState.Approach: TickApproach(); break;
            case EnemyState.Attack: TickAttack(); break;
        }

        // Muro físico: en la arena ninguna unidad puede salir de sus límites. En el gimnasio no hay
        // arena declarada, y recortar contra unos límites de tamaño cero clavaba al enemigo en (0,0).
        if (WaveManager.HasArenaBounds)
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, WaveManager.ArenaWallMin.x, WaveManager.ArenaWallMax.x);
            pos.y = Mathf.Clamp(pos.y, WaveManager.ArenaWallMin.y, WaveManager.ArenaWallMax.y);
            transform.position = pos;
        }
    }

    // Golpe circular: primero avisa, y solo después pega. Ese hueco es la ventana de reacción.
    private void TickBossSlam()
    {
        if (windingUp)
        {
            windupTimer -= Time.deltaTime;
            if (windupTimer > 0f) return;

            windingUp = false;
            if (body != null) body.color = CurrentTint;
            ShowTelegraph(false);
            ExecuteSlam();
            return;
        }

        slamTimer -= Time.deltaTime;
        if (slamTimer > 0f) return;

        slamTimer = bossSlamInterval;
        StartWindup();
    }

    private void StartWindup()
    {
        windingUp = true;
        windupTimer = bossSlamWindup;

        if (body != null) body.color = bossWindupTint;
        ShowTelegraph(true);

        DamageTextManager.Show(transform.position, LocalizationManager.Get("FX_SLAM_CHARGING"), new Color(1f, 0.3f, 0.25f));
        Debug.Log($"[Jefe] {data.enemyName} carga el golpe: {bossSlamWindup}s para reaccionar.", this);
    }

    // Círculo rojo en el suelo: marca el radio exacto del golpe antes de que caiga.
    private void ShowTelegraph(bool visible)
    {
        if (telegraph == null)
        {
            if (!visible) return;

            var go = new GameObject("SlamTelegraph", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);

            telegraph = go.GetComponent<SpriteRenderer>();
            telegraph.sprite = CircleSprite();
            telegraph.color = new Color(1f, 1f, 1f, 0.9f);

            // Marca de suelo: en Characters (la capa Default queda DEBAJO del fondo pintado y no
            // se veía), con orden muy negativo para quedar bajo las unidades.
            telegraph.sortingLayerID = SortingLayer.NameToID(YSorter.CombatLayer);
            telegraph.sortingOrder = YSorter.GroundMarkOrder;
        }

        // El jefe va escalado, así que el círculo compensa para medir unidades reales.
        float escala = Mathf.Max(0.01f, transform.localScale.x);
        telegraph.transform.localScale = Vector3.one * (bossSlamRadius * 2f / escala);
        telegraph.gameObject.SetActive(visible);
    }

    // Se genera una vez y la comparten todos los jefes; el proyecto no trae sprite circular.
    private static Sprite CircleSprite()
    {
        if (sharedCircle != null) return sharedCircle;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radio = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radio, radio));

                // Borde marcado y relleno tenue: se lee de un vistazo sin tapar el combate.
                float alpha = d > radio ? 0f : d > radio - 6f ? 0.85f : 0.22f;
                tex.SetPixel(x, y, new Color(1f, 0.20f, 0.15f, alpha));
            }
        }

        tex.Apply();
        sharedCircle = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return sharedCircle;
    }

    // --- Mecánicas de jefe (rotación por número de jefe, ver BossMechanics) ---

    // Zona del suelo y entrada en frenesí; la invocación cuelga de los umbrales de vida.
    private void TickBossMechanics()
    {
        if (HasMechanic(BossMechanic.GroundZone))
        {
            zoneTimer -= Time.deltaTime;
            if (zoneTimer <= 0f)
            {
                zoneTimer = zoneInterval;
                StartCoroutine(GroundZoneRoutine());
            }
        }

        if (!frenzied && HasMechanic(BossMechanic.Frenzy) && MaxHealth > 0
            && (float)currentHealth / MaxHealth <= frenzyThreshold)
            EnterFrenzy();
    }

    // Umbrales de vida: invocar refuerzos y volver a levantar la barrera una segunda vez.
    private void CheckBossThresholds()
    {
        if (!isBoss || MaxHealth <= 0 || currentHealth <= 0) return;

        float ratio = (float)currentHealth / MaxHealth;

        if (HasMechanic(BossMechanic.Summon) && summonThresholds != null
            && summonsDone < summonThresholds.Length && ratio <= summonThresholds[summonsDone])
        {
            summonsDone++;
            SummonReinforcements();
        }

        if (HasMechanic(BossMechanic.Barrier) && !barrierRaisedTwice && ratio <= barrierRaiseAgainAt)
        {
            barrierRaisedTwice = true;
            RaiseBarrier();
        }
    }

    private void SummonReinforcements()
    {
        int llegan = WaveManager.SummonBossAdds(transform.position, summonAdds);
        if (llegan <= 0) return;

        AnnounceMechanic(BossMechanic.Summon);
        Debug.Log($"[Jefe] {data.enemyName} invoca {llegan} refuerzo(s).", this);
    }

    private void RaiseBarrier()
    {
        barrierHealth = Mathf.Max(1f, MaxHealth * barrierFraction);

        if (body != null) body.color = barrierTint;
        AnnounceMechanic(BossMechanic.Barrier);
        Debug.Log($"[Jefe] {data.enemyName} levanta su barrera ({barrierHealth:0} de aguante).", this);
    }

    // Tinte que le toca al jefe fuera del aviso de golpe: barrera, frenesí o el suyo de siempre.
    private Color CurrentTint
        => barrierHealth > 0f ? barrierTint : frenzied ? frenzyTint : baseTint;

    private void BreakBarrier()
    {
        barrierHealth = 0f;

        if (body != null) body.color = CurrentTint;
        DamageTextManager.Show(transform.position, LocalizationManager.Get("FX_BARRIER_BROKEN"), barrierTint);
        AudioManager.PlayAt(SfxId.Impact, transform.position);
    }

    private void EnterFrenzy()
    {
        frenzied = true;
        frenzyMultiplier = frenzyAttackMultiplier;

        if (body != null) body.color = CurrentTint;
        AnnounceMechanic(BossMechanic.Frenzy);
        Debug.Log($"[Jefe] {data.enemyName} entra en frenesí.", this);
    }

    // Cura del robo de vida del frenesí; nunca por encima del máximo.
    private void HealBoss(int amount)
    {
        if (amount <= 0 || currentHealth <= 0) return;

        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    // Marca en el suelo bajo un héroe: primero avisa, luego quema a quien siga dentro.
    private IEnumerator GroundZoneRoutine()
    {
        var victima = target != null ? target : NearestDeployedHero();
        if (victima == null) yield break;

        Vector2 centro = victima.transform.position;

        var go = new GameObject("BossGroundZone", typeof(SpriteRenderer), typeof(BossGroundZone));
        go.transform.position = centro;
        go.transform.localScale = Vector3.one * (zoneRadius * 2f);

        var marca = go.GetComponent<SpriteRenderer>();
        marca.sprite = CircleSprite();
        marca.color = new Color(0.85f, 0.35f, 1f, 0.55f);
        marca.sortingLayerID = SortingLayer.NameToID(YSorter.CombatLayer);
        marca.sortingOrder = YSorter.GroundMarkOrder;

        // Registrada ya durante el aviso: los héroes tienen que poder salir antes de que queme.
        var zona = go.GetComponent<BossGroundZone>();
        zona.Setup(centro, zoneRadius, zoneWindup + zoneDuration + 0.5f);

        AnnounceMechanic(BossMechanic.GroundZone);
        yield return new WaitForSeconds(zoneWindup);

        if (marca != null) marca.color = new Color(0.95f, 0.25f, 0.85f, 0.80f);
        if (zona != null) zona.Arm();

        int porSegundo = Mathf.Max(1, Mathf.RoundToInt(Attack * zoneDamageFactor));
        float restante = zoneDuration;
        float radioSqr = zoneRadius * zoneRadius;

        while (restante > 0f)
        {
            yield return new WaitForSeconds(1f);
            restante -= 1f;

            foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
            {
                if (!hero.IsDeployed || hero.CurrentHealth <= 0) continue;
                if (((Vector2)hero.transform.position - centro).sqrMagnitude > radioSqr) continue;

                hero.TakeDamage(porSegundo, true);
            }
        }

        if (go != null) Destroy(go);
    }

    private HeroController NearestDeployedHero()
    {
        HeroController cerca = null;
        float mejor = float.MaxValue;

        foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
        {
            if (!hero.IsDeployed || hero.CurrentHealth <= 0) continue;

            float d = ((Vector2)(hero.transform.position - transform.position)).sqrMagnitude;
            if (d >= mejor) continue;

            mejor = d;
            cerca = hero;
        }

        return cerca;
    }

    // Rótulo sobre el jefe: sin esto la mecánica pasa desapercibida entre los números de daño.
    private void AnnounceMechanic(BossMechanic mechanic)
        => ScreenBanner.ShowCompact(
            $"{data.enemyName}: {BossMechanics.DisplayName(mechanic)} - {BossMechanics.Description(mechanic)}",
            3f, UITheme.Danger);

    private void ExecuteSlam()
    {
        int damage = Mathf.Max(1, Mathf.RoundToInt(Attack * bossSlamDamageFactor));
        float radiusSqr = bossSlamRadius * bossSlamRadius;
        int hits = 0;

        foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
        {
            if (!hero.IsDeployed || hero.CurrentHealth <= 0) continue;
            if (((Vector2)(hero.transform.position - transform.position)).sqrMagnitude > radiusSqr) continue;

            hero.TakeDamage(damage);
            hits++;
        }

        if (hits > 0)
        {
            DamageTextManager.Show(transform.position, LocalizationManager.Get("FX_SLAM"), new Color(1f, 0.4f, 0.3f));
            Debug.Log($"[Jefe] {data.enemyName} sacude a {hits} héroe(s) por {damage}.", this);
        }
    }

    // Busca el héroe más cercano cada scanInterval y decide entrar o salir de combate.
    private void ScanForHeroes()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f) return;
        scanTimer = scanInterval;

        // La provocación manda mientras dure y el provocador siga en pie.
        if (tauntTimer > 0f && taunter != null)
        {
            target = taunter;
            if (state == EnemyState.Idle) state = EnemyState.Approach;
            return;
        }

        HeroController nearest = FindNearestHero();

        if (nearest == null)
        {
            // El héroe murió o se alejó: limpia la referencia y vuelve a esperar.
            target = null;
            state = EnemyState.Idle;
            return;
        }

        // El retardo del primer golpe se cobra al FIJAR objetivo, no al entrar en rango: si no,
        // cada vez que el héroe se apartaba un pelo el enfriamiento volvía a empezar de cero.
        if (target != nearest) attackTimer = AttackCooldown;

        target = nearest;
        if (state == EnemyState.Idle) state = EnemyState.Approach;
    }

    // Un tanque solo puede sujetar a maxAggroPerTank enemigos. Sin tanque libre, los de rango
    // flanquean a la retaguardia y los cuerpo a cuerpo van al más cercano.
    private HeroController FindNearestHero()
    {
        var heroes = UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None);
        float rangeSqr = detectionRange * detectionRange;

        HeroController tanqueLibre = null;
        HeroController masCercano = null;
        HeroController retaguardia = null;

        float mejorTanque = rangeSqr;
        float mejorCercano = rangeSqr;
        float masLejos = -1f;

        foreach (var hero in heroes)
        {
            // Solo escuadra desplegada en la expedición: ignora héroes ociosos en la base.
            if (!hero.IsDeployed || hero.CurrentHealth <= 0) continue;

            float sqr = ((Vector2)(hero.transform.position - transform.position)).sqrMagnitude;
            if (sqr > rangeSqr) continue;

            if (sqr < mejorCercano) { mejorCercano = sqr; masCercano = hero; }

            // El tanque cuenta a los que ya le apuntan, sin contarse a sí mismo dos veces.
            if (hero.IsTank)
            {
                int sujetos = hero.Threat;
                if (target == hero) sujetos--;

                if (sujetos < maxAggroPerTank && sqr < mejorTanque)
                {
                    mejorTanque = sqr;
                    tanqueLibre = hero;
                }
                continue;
            }

            // Flanquear es ir a por el de más atrás, no a por el que tienes delante.
            if (sqr > masLejos) { masLejos = sqr; retaguardia = hero; }
        }

        if (tanqueLibre != null) return tanqueLibre;

        // Solo flanquea quien tiene alcance para hacerlo. Un cuerpo a cuerpo persiguiendo al
        // héroe más lejano nunca llegaba: el héroe corre a 2,5 y él a 1,5, así que la primera
        // línea lo interceptaba y lo mataba de camino sin que llegase a golpear a nadie.
        if (IsRanged && retaguardia != null) return retaguardia;

        return masCercano != null ? masCercano : retaguardia;
    }

    private void TickApproach()
    {
        if (target == null) { state = EnemyState.Idle; return; }

        float distance = Vector2.Distance(transform.position, target.transform.position);
        float range = AttackRange;

        // Ya está en rango: no avanza ni un pixel, así se evita el temblor. El enfriamiento NO se
        // reinicia aquí; se cobró al fijar objetivo.
        if (distance <= range)
        {
            state = EnemyState.Attack;
            return;
        }

        // Cierra un poco por dentro del borde en vez de quedarse clavado justo encima de él:
        // parándose en el borde exacto, cualquier paso del héroe lo sacaba del rango.
        float parada = range * Mathf.Clamp(approachOvershoot, 0.1f, 1f);
        float velocidad = data.moveSpeed * Status.SpeedMultiplier;
        float step = Mathf.Min(velocidad * Time.deltaTime, distance - parada);
        transform.position = Vector2.MoveTowards(
            transform.position,
            target.transform.position,
            step);
    }

    private void TickAttack()
    {
        if (target == null) { state = EnemyState.Idle; return; }

        float distance = Vector2.Distance(transform.position, target.transform.position);

        // Si el héroe se aleja, vuelve a perseguirlo — pero con holgura. Sin ella, el empujón de
        // TickPersonalSpace y las recolocaciones del coreógrafo lo sacaban del rango un frame de
        // cada dos, y así el enfriamiento no llegaba a cumplirse nunca: enemigos encima del héroe
        // que no le pegaban en todo el piso.
        if (distance > AttackRange * Mathf.Max(1f, attackExitFactor))
        {
            state = EnemyState.Approach;
            return;
        }

        // Tiradores y chamanes no dejan que el héroe se les pegue: si entra en la zona de
        // seguridad, se retiran en vez de quedarse quietos. Orcos y jefes (línea frontal)
        // nunca entran aquí porque IsRanged es falso para ellos.
        if (IsRanged && !IsFrontline)
        {
            float safeDistance = AttackRange * safeDistanceFactor;
            if (distance < safeDistance)
            {
                Vector2 alejarse = ((Vector2)transform.position - (Vector2)target.transform.position).normalized;
                float velocidad = data.moveSpeed * Status.SpeedMultiplier;
                transform.position = (Vector2)transform.position + alejarse * velocidad * Time.deltaTime;
            }
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        attackTimer = AttackCooldown;

        // Tiradores y chamanes disparan: el golpe tarda en llegar y se ve venir.
        if (AttackRange >= rangedThreshold)
        {
            Color tinte = data.magicAttack ? new Color(0.65f, 0.35f, 0.95f) : new Color(0.85f, 0.80f, 0.55f);
            Projectile.Fire(transform.position, target, Attack, tinte, data.magicAttack,
                            magic: data.magicAttack, dodgeable: true);
            return;
        }

        if (animator != null) animator.PlayAttackLunge(target.transform.position);
        if (isBoss) CombatFeelManager.OnBossImpact();

        int golpe = Attack;
        target.TakeDamage(golpe, data.magicAttack);

        // En frenesí el jefe se cura de lo que reparte; es lo que obliga a matarlo deprisa.
        if (frenzied) HealBoss(Mathf.RoundToInt(golpe * frenzyLifeSteal));
    }

    // Quién está repartiendo el golpe ahora mismo. Las habilidades y los proyectiles no pasan por
    // StrikeEnemy, así que sin esto sus bajas no se le apuntaban a nadie. Se arma justo alrededor
    // del golpe y se desarma siempre, incluso si la rama de la habilidad revienta.
    private static HeroController currentAttacker;

    public static void SetAttacker(HeroController hero) => currentAttacker = hero;

    public void TakeDamage(int amount) => TakeDamage(amount, false, 0f);

    public void TakeDamage(int amount, bool ignoresDefense) => TakeDamage(amount, ignoresDefense, 0f);

    // El daño que ignora armadura entra entero; armorPierce recorta solo una parte de la defensa.
    public void TakeDamage(int amount, bool ignoresDefense, float armorPierce)
    {
        // Congelado en la cuenta atrás: invulnerabilidad estricta, sin excepciones por origen del golpe.
        if (frozen) return;

        int defensa = IsArmorBroken
            ? 0
            : Mathf.RoundToInt(data.baseDefense * (1f - Mathf.Clamp01(armorPierce)));
        int finalDamage = ignoresDefense
            ? Mathf.Max(1, amount)
            : Mathf.Max(Mathf.RoundToInt(amount * CombatTuning.MinDamageFraction), amount - defensa, 1);
        // La barrera se come el golpe entero: hasta que no cae, la vida no baja.
        if (barrierHealth > 0f)
        {
            barrierHealth -= finalDamage;
            DamageTextManager.Show(transform.position, finalDamage.ToString(), barrierTint);

            if (barrierHealth <= 0f) BreakBarrier();
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        HealthChanged?.Invoke(currentHealth, MaxHealth);
        TrackBurstDamage(finalDamage);
        CheckBossThresholds();

        // Golpe grande de un solo tirón: el flash blanco lo delata igual que a una ráfaga acumulada.
        if (MaxHealth > 0 && finalDamage >= MaxHealth * hitFlashThreshold) HitFlash();

        DamageTextManager.ShowDamage(transform.position, finalDamage);
        AudioManager.PlayAt(SfxId.Impact, transform.position);
        Debug.Log($"[Enemy] {data.enemyName} recibe {finalDamage} ({currentHealth}/{MaxHealth})", this);

        if (currentHealth <= 0)
        {
            AudioManager.PlayAt(SfxId.Defeat, transform.position);
            Debug.Log($"[Enemy] {data.enemyName} destruido.", this);
            QuestManager.Report(QuestKind.KillEnemies);
            if (currentAttacker != null) currentAttacker.CreditKill();
            Destroy(gameObject);
        }
    }

    // Ráfaga de daño en poco tiempo: el enemigo se recoloca un paso corto para no quedarse
    // plantado bajo fuego concentrado. Se aleja del objetivo actual como aproximación de "atacante".
    private void TrackBurstDamage(int finalDamage)
    {
        if (burstWindowTimer <= 0f) recentBurstDamage = 0f;

        recentBurstDamage += finalDamage;
        burstWindowTimer = burstWindowSeconds;

        if (target != null && recentBurstDamage >= MaxHealth * burstDamageThreshold)
        {
            PushBack(target.transform.position, repositionStep);
            HitFlash();
            recentBurstDamage = 0f;
            burstWindowTimer = 0f;
        }
    }

    // Flash blanco breve; restaura el tinte que hubiera justo antes (normal o de carga del jefe).
    private void HitFlash()
    {
        if (body == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        Color before = body.color;
        body.color = Color.white;
        yield return new WaitForSeconds(hitFlashDuration);
        body.color = before;
        flashRoutine = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
    }
}
