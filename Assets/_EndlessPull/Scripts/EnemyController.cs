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

    [Tooltip("Distancia a la que deja de acercarse y empieza a golpear.")]
    [SerializeField] private float attackRange = 1.1f;

    [Tooltip("Cada cuántos segundos vuelve a buscar héroes cercanos.")]
    [SerializeField] private float scanInterval = 0.25f;

    private EnemyState state = EnemyState.Idle;
    private int currentHealth;
    private float attackTimer;
    private float scanTimer;
    private HeroController target;

    // Multiplicador de piso: escala vida y ataque sin tocar el EnemyData compartido.
    private float statMultiplier = 1f;

    public EnemyData Data => data;
    public EnemyState State => state;
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
        ScanForHeroes();

        switch (state)
        {
            case EnemyState.Approach: TickApproach(); break;
            case EnemyState.Attack: TickAttack(); break;
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

        // Ya está en rango: no avanza ni un pixel, así se evita el temblor.
        if (distance <= attackRange)
        {
            state = EnemyState.Attack;
            attackTimer = data.attackCooldown;   // no golpea nada más llegar
            return;
        }

        // Se para justo en el borde del rango en vez de meterse encima del héroe.
        float step = Mathf.Min(data.moveSpeed * Time.deltaTime, distance - attackRange);
        transform.position = Vector2.MoveTowards(
            transform.position,
            target.transform.position,
            step);
    }

    private void TickAttack()
    {
        if (target == null) { state = EnemyState.Idle; return; }

        // Si el héroe se aleja, vuelve a perseguirlo.
        if (Vector2.Distance(transform.position, target.transform.position) > attackRange)
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
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
