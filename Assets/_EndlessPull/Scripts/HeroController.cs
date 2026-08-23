using System;
using UnityEngine;

// Estados del héroe: ciclo tranquilo en la base, visita a un edificio, o persecución y ataque.
public enum HeroState
{
    BaseIdle,
    BaseWander,
    Training,
    CombatApproach,
    CombatAttack
}

public class HeroController : MonoBehaviour, IHealthOwner
{
    [Tooltip("Datos del héroe: stats, rareza y velocidad.")]
    [SerializeField] private HeroData data;

    [Tooltip("Personalidad: decide preferencias de edificio y bonus de combate.")]
    [SerializeField] private HeroTrait trait = HeroTrait.Diligent;

    [Tooltip("Centro del área de la base por la que pasea.")]
    [SerializeField] private Vector2 baseAreaCenter = Vector2.zero;

    [Tooltip("Ancho y alto del área de paseo, en unidades.")]
    [SerializeField] private Vector2 baseAreaSize = new Vector2(8f, 5f);

    [Tooltip("Distancia a la que se considera que ya llegó al destino.")]
    [SerializeField] private float arriveThreshold = 0.05f;

    [Tooltip("Segundos mínimos y máximos de descanso al llegar.")]
    [SerializeField] private Vector2 restTimeRange = new Vector2(1f, 3f);

    [Tooltip("Probabilidad de ir a un edificio en vez de a un punto al azar.")]
    [Range(0f, 1f)]
    [SerializeField] private float buildingVisitChance = 0.6f;

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

    // Bonus por instancia que aporta el nivel; el HeroData compartido no se toca nunca.
    private int bonusMaxHealth;
    private int bonusAttack;

    private BaseBuilding destinationBuilding;
    private BaseBuilding currentBuilding;
    private float visitTimer;
    private float buildingTickTimer;

    public HeroData Data => data;
    public HeroState State => state;
    public HeroTrait Trait => trait;
    public BaseBuilding CurrentBuilding => currentBuilding;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => data != null ? data.maxHealth + bonusMaxHealth : 0;
    public int Attack => data != null ? data.baseAttack + bonusAttack + HeroTraits.AttackBonus(trait) : 0;
    public float EffectiveDetectionRange => detectionRange * HeroTraits.DetectionMultiplier(trait);
    public event Action<int, int> HealthChanged;

    // La vida se fija en Awake para que la barra ya la lea válida en su Start.
    void Awake()
    {
        if (data != null) currentHealth = MaxHealth;
    }

    // La usa el gacha: asigna los datos justo tras instanciar, antes del primer Start.
    public void Initialize(HeroData heroData, HeroTrait heroTrait, Vector2 wanderCenter, Vector2 wanderSize)
    {
        data = heroData;
        trait = heroTrait;
        baseAreaCenter = wanderCenter;
        baseAreaSize = wanderSize;

        currentHealth = MaxHealth;
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    // La llama HeroProgress al subir de nivel; devuelve la vida máxima ganada.
    public int ApplyLevelUpBonus(float healthPercent, int attackFlat)
    {
        int gain = Mathf.Max(1, Mathf.RoundToInt(MaxHealth * healthPercent));

        bonusMaxHealth += gain;
        bonusAttack += attackFlat;

        // Al subir de nivel se restaura la salud.
        currentHealth = MaxHealth;
        HealthChanged?.Invoke(currentHealth, MaxHealth);

        return gain;
    }

    void Start()
    {
        if (data == null)
        {
            Debug.LogError($"[HeroController] '{name}' no tiene HeroData asignado.", this);
            enabled = false;
            return;
        }

        EnterBaseWander();
    }

    void Update()
    {
        ScanForEnemies();

        switch (state)
        {
            case HeroState.BaseIdle: TickBaseIdle(); break;
            case HeroState.BaseWander: TickBaseWander(); break;
            case HeroState.Training: TickBuildingVisit(); break;
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
            // El combate interrumpe cualquier actividad de base, entrenamiento incluido.
            currentBuilding = null;
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
        var enemies = UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        EnemyController nearest = null;
        float range = EffectiveDetectionRange;
        float bestSqr = range * range;

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

        if (Vector2.Distance(transform.position, wanderTarget) > arriveThreshold) return;

        // Si el destino era un edificio y ya está dentro, se pone a usarlo.
        if (destinationBuilding != null && destinationBuilding.IsInside(transform.position))
        {
            EnterBuildingVisit(destinationBuilding);
            return;
        }

        // El perezoso se queda parado bastante más rato.
        restTimer = UnityEngine.Random.Range(restTimeRange.x, restTimeRange.y)
                    * HeroTraits.IdleMultiplier(trait);
        state = HeroState.BaseIdle;
    }

    private void EnterBuildingVisit(BaseBuilding building)
    {
        currentBuilding = building;
        destinationBuilding = null;
        state = HeroState.Training;

        visitTimer = building.RandomVisitDuration();
        buildingTickTimer = building.TickInterval;

        Debug.Log($"[Base] {data.heroName} empieza a usar {building.BuildingName}.", this);
    }

    private void TickBuildingVisit()
    {
        if (currentBuilding == null) { EnterBaseWander(); return; }

        visitTimer -= Time.deltaTime;
        buildingTickTimer -= Time.deltaTime;

        if (buildingTickTimer <= 0f)
        {
            buildingTickTimer = currentBuilding.TickInterval;
            currentBuilding.ApplyTick(this);
        }

        if (visitTimer <= 0f)
        {
            currentBuilding = null;
            EnterBaseWander();
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
        target.TakeDamage(Attack);
    }

    private void EnterBaseWander()
    {
        target = null;
        currentBuilding = null;
        state = HeroState.BaseWander;
        PickNewWanderTarget();
    }

    private void PickNewWanderTarget()
    {
        destinationBuilding = null;

        // A veces el héroe decide ir a un edificio en vez de vagar sin rumbo.
        float chance = buildingVisitChance * HeroTraits.VisitChanceMultiplier(trait);
        if (UnityEngine.Random.value < chance)
        {
            var building = PickRandomBuilding();
            if (building != null)
            {
                destinationBuilding = building;

                // Se planta en un punto al azar dentro del radio, no todos en el mismo pixel.
                Vector2 offset = UnityEngine.Random.insideUnitCircle * (building.InteractionRadius * 0.6f);
                wanderTarget = (Vector2)building.transform.position + offset;
                return;
            }
        }

        Vector2 half = baseAreaSize * 0.5f;
        wanderTarget = baseAreaCenter + new Vector2(
            UnityEngine.Random.Range(-half.x, half.x),
            UnityEngine.Random.Range(-half.y, half.y));
    }

    // Sorteo ponderado: cada rasgo tira más hacia unos edificios que hacia otros.
    private BaseBuilding PickRandomBuilding()
    {
        var all = BaseBuilding.All;
        if (all == null || all.Count == 0) return null;

        float total = 0f;
        foreach (var b in all) total += HeroTraits.BuildingWeight(trait, b.Type);
        if (total <= 0f) return null;

        float roll = UnityEngine.Random.Range(0f, total);
        float acc = 0f;

        foreach (var b in all)
        {
            acc += HeroTraits.BuildingWeight(trait, b.Type);
            if (roll < acc) return b;
        }

        return all[all.Count - 1];
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
        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        HealthChanged?.Invoke(currentHealth, MaxHealth);

        Debug.Log($"[Hero] {data.heroName} recibe {finalDamage} ({currentHealth}/{MaxHealth})", this);

        if (currentHealth <= 0)
        {
            // Permadeath: el héroe no vuelve.
            Debug.Log($"[Hero] {data.heroName} ha muerto.", this);
            Destroy(gameObject);
        }
    }

    // Cura sin pasarse de la vida máxima; devuelve lo que realmente se curó.
    public int Heal(int amount)
    {
        if (amount <= 0 || data == null) return 0;

        int before = currentHealth;
        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);

        int healed = currentHealth - before;
        if (healed > 0) HealthChanged?.Invoke(currentHealth, MaxHealth);

        return healed;
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
