using System;
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

    [Tooltip("Cada cuántos segundos vuelve a buscar héroes cercanos.")]
    [SerializeField] private float scanInterval = 0.25f;

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

    private EnemyState state = EnemyState.Idle;
    private int currentHealth;
    private float attackTimer;
    private float scanTimer;
    private HeroController target;

    // Multiplicador de piso: escala vida y ataque sin tocar el EnemyData compartido.
    private float statMultiplier = 1f;

    // Los jefes pegan además un golpe en área cada pocos segundos.
    private bool isBoss;
    private float slamTimer;

    // Aviso previo: el golpe se ve venir, para que dé tiempo a reagrupar.
    private bool windingUp;
    private float windupTimer;
    private SpriteRenderer body;
    private Color baseTint = Color.white;

    public EnemyData Data => data;
    public EnemyState State => state;
    public bool IsBoss => isBoss;
    public bool IsWindingUp => windingUp;

    // El alcance sale del asset; el campo del componente solo cubre datos antiguos.
    public float AttackRange => data != null && data.attackRange > 0f ? data.attackRange : attackRange;
    public float StatMultiplier => statMultiplier;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => data != null ? Mathf.RoundToInt(data.maxHealth * statMultiplier) : 0;
    public int Attack => data != null ? Mathf.RoundToInt(data.baseAttack * statMultiplier) : 0;
    public event Action<int, int> HealthChanged;

    void Awake()
    {
        if (data != null) currentHealth = MaxHealth;
    }

    // La usa el WaveManager: asigna datos y escalado tras instanciar, antes del primer Start.
    public void Initialize(EnemyData enemyData, float multiplier)
    {
        data = enemyData;
        statMultiplier = Mathf.Max(0.01f, multiplier);

        currentHealth = MaxHealth;
        HealthChanged?.Invoke(currentHealth, MaxHealth);
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
        if (isBoss) TickBossSlam();

        ScanForHeroes();

        switch (state)
        {
            case EnemyState.Approach: TickApproach(); break;
            case EnemyState.Attack: TickAttack(); break;
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
            if (body != null) body.color = baseTint;
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

        DamageTextManager.Show(transform.position, "¡CARGANDO GOLPE!", new Color(1f, 0.3f, 0.25f));
        Debug.Log($"[Jefe] {data.enemyName} carga el golpe: {bossSlamWindup}s para reaccionar.", this);
    }

    private void ExecuteSlam()
    {
        int damage = Mathf.Max(1, Mathf.RoundToInt(Attack * bossSlamDamageFactor));
        float radiusSqr = bossSlamRadius * bossSlamRadius;
        int hits = 0;

        foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
        {
            if (((Vector2)(hero.transform.position - transform.position)).sqrMagnitude > radiusSqr) continue;

            hero.TakeDamage(damage);
            hits++;
        }

        if (hits > 0)
        {
            DamageTextManager.Show(transform.position, "¡GOLPE!", new Color(1f, 0.4f, 0.3f));
            Debug.Log($"[Jefe] {data.enemyName} sacude a {hits} héroe(s) por {damage}.", this);
        }
    }

    // Busca el héroe más cercano cada scanInterval y decide entrar o salir de combate.
    private void ScanForHeroes()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f) return;
        scanTimer = scanInterval;

        HeroController nearest = FindNearestHero();

        if (nearest == null)
        {
            // El héroe murió o se alejó: limpia la referencia y vuelve a esperar.
            target = null;
            state = EnemyState.Idle;
            return;
        }

        target = nearest;
        if (state == EnemyState.Idle) state = EnemyState.Approach;
    }

    private HeroController FindNearestHero()
    {
        var heroes = UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None);
        HeroController nearest = null;
        float bestSqr = detectionRange * detectionRange;

        foreach (var hero in heroes)
        {
            float sqr = ((Vector2)(hero.transform.position - transform.position)).sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                nearest = hero;
            }
        }

        return nearest;
    }

    private void TickApproach()
    {
        if (target == null) { state = EnemyState.Idle; return; }

        float distance = Vector2.Distance(transform.position, target.transform.position);
        float range = AttackRange;

        // Ya está en rango: no avanza ni un pixel, así se evita el temblor.
        if (distance <= range)
        {
            state = EnemyState.Attack;
            attackTimer = data.attackCooldown;   // no golpea nada más llegar
            return;
        }

        // Se para justo en el borde del rango en vez de meterse encima del héroe.
        float step = Mathf.Min(data.moveSpeed * Time.deltaTime, distance - range);
        transform.position = Vector2.MoveTowards(
            transform.position,
            target.transform.position,
            step);
    }

    private void TickAttack()
    {
        if (target == null) { state = EnemyState.Idle; return; }

        // Si el héroe se aleja, vuelve a perseguirlo.
        if (Vector2.Distance(transform.position, target.transform.position) > AttackRange)
        {
            state = EnemyState.Approach;
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        attackTimer = data.attackCooldown;
        target.TakeDamage(Attack);
    }

    public void TakeDamage(int amount)
    {
        int finalDamage = Mathf.Max(1, amount - data.baseDefense);
        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        HealthChanged?.Invoke(currentHealth, MaxHealth);

        DamageTextManager.ShowDamage(transform.position, finalDamage);
        Debug.Log($"[Enemy] {data.enemyName} recibe {finalDamage} ({currentHealth}/{MaxHealth})", this);

        if (currentHealth <= 0)
        {
            Debug.Log($"[Enemy] {data.enemyName} destruido.", this);
            Destroy(gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
    }
}
