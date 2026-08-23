using UnityEngine;

// Estados del héroe: ciclo tranquilo en la base o persecución y ataque.
public enum HeroState
{
    BaseIdle,
    BaseWander,
    CombatApproach,
    CombatAttack
}

public class HeroController : MonoBehaviour
{
    [Tooltip("Datos del héroe: stats, rareza y velocidad.")]
    [SerializeField] private HeroData data;

    [Tooltip("Centro del área de la base por la que pasea.")]
    [SerializeField] private Vector2 baseAreaCenter = Vector2.zero;

    [Tooltip("Ancho y alto del área de paseo, en unidades.")]
    [SerializeField] private Vector2 baseAreaSize = new Vector2(8f, 5f);

    [Tooltip("Distancia a la que se considera que ya llegó al destino.")]
    [SerializeField] private float arriveThreshold = 0.05f;

    [Tooltip("Segundos mínimos y máximos de descanso al llegar.")]
    [SerializeField] private Vector2 restTimeRange = new Vector2(1f, 3f);

    [Tooltip("Radio en el que el héroe detecta enemigos y entra en combate.")]
    [SerializeField] private float detectionRange = 6f;

    [Tooltip("Distancia a la que deja de acercarse y empieza a golpear.")]
    [SerializeField] private float attackRange = 1.2f;

    [Tooltip("Segundos entre golpe y golpe.")]
    [SerializeField] private float attackCooldown = 1f;

    [Tooltip("Cada cuántos segundos vuelve a buscar enemigos cercanos.")]
    [SerializeField] private float scanInterval = 0.25f;

    private HeroState state = HeroState.BaseIdle;
    private int currentHealth;
    private Vector2 wanderTarget;
    private float restTimer;
    private float attackTimer;
    private float scanTimer;
    private EnemyController target;

    public HeroData Data => data;
    public int CurrentHealth => currentHealth;
    public HeroState State => state;

    void Start()
    {
        if (data == null)
        {
            Debug.LogError($"[HeroController] '{name}' no tiene HeroData asignado.", this);
            enabled = false;
            return;
        }

        currentHealth = data.maxHealth;
        EnterBaseWander();
    }

    void Update()
    {
        ScanForEnemies();

        switch (state)
        {
            case HeroState.BaseIdle: TickBaseIdle(); break;
            case HeroState.BaseWander: TickBaseWander(); break;
            case HeroState.CombatApproach: TickCombatApproach(); break;
            case HeroState.CombatAttack: TickCombatAttack(); break;
        }
    }

    // Busca el enemigo más cercano cada scanInterval y decide entrar o salir de combate.
    private void ScanForEnemies()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f) return;
        scanTimer = scanInterval;

        EnemyController nearest = FindNearestEnemy();

        if (nearest != null && target == null)
        {
            target = nearest;
            state = HeroState.CombatApproach;
        }
        else if (nearest == null && IsInCombat())
        {
            EnterBaseWander();
        }
        else if (nearest != null)
        {
            target = nearest;
        }
    }

    private EnemyController FindNearestEnemy()
    {
        var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        EnemyController nearest = null;
        float bestSqr = detectionRange * detectionRange;

        foreach (var enemy in enemies)
        {
            float sqr = ((Vector2)(enemy.transform.position - transform.position)).sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private bool IsInCombat()
        => state == HeroState.CombatApproach || state == HeroState.CombatAttack;

    private void TickBaseIdle()
    {
        restTimer -= Time.deltaTime;
        if (restTimer <= 0f) EnterBaseWander();
    }

    private void TickBaseWander()
    {
        MoveTowards(wanderTarget);

        if (Vector2.Distance(transform.position, wanderTarget) <= arriveThreshold)
        {
            restTimer = Random.Range(restTimeRange.x, restTimeRange.y);
            state = HeroState.BaseIdle;
        }
    }

    private void TickCombatApproach()
    {
        if (target == null) { EnterBaseWander(); return; }

        MoveTowards(target.transform.position);

        if (Vector2.Distance(transform.position, target.transform.position) <= attackRange)
        {
            state = HeroState.CombatAttack;
            attackTimer = 0f;   // el primer golpe sale sin esperar
        }
    }

    private void TickCombatAttack()
    {
        if (target == null) { EnterBaseWander(); return; }

        // Si se aleja, vuelve a perseguirlo.
        if (Vector2.Distance(transform.position, target.transform.position) > attackRange)
        {
            state = HeroState.CombatApproach;
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        attackTimer = attackCooldown;
        target.TakeDamage(data.baseAttack);
    }

    private void EnterBaseWander()
    {
        target = null;
        state = HeroState.BaseWander;
        PickNewWanderTarget();
    }

    private void PickNewWanderTarget()
    {
        Vector2 half = baseAreaSize * 0.5f;
        wanderTarget = baseAreaCenter + new Vector2(
            Random.Range(-half.x, half.x),
            Random.Range(-half.y, half.y));
    }

    private void MoveTowards(Vector2 destination)
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            destination,
            data.moveSpeed * Time.deltaTime);
    }

    public void TakeDamage(int amount)
    {
        int finalDamage = Mathf.Max(1, amount - data.baseDefense);
        currentHealth -= finalDamage;

        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    // Cura sin pasarse de la vida máxima; devuelve lo que realmente se curó.
    public int Heal(int amount)
    {
        if (amount <= 0 || data == null) return 0;

        int before = currentHealth;
        currentHealth = Mathf.Min(data.maxHealth, currentHealth + amount);
        return currentHealth - before;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(baseAreaCenter, baseAreaSize);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
