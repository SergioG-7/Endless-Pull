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

    [Tooltip("Alcance a partir del cual el enemigo dispara en vez de golpear de cerca.")]
    [SerializeField] private float rangedThreshold = 3f;

    [Tooltip("Cada cuántos segundos vuelve a buscar héroes cercanos.")]
    [SerializeField] private float scanInterval = 0.25f;

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

    // Aviso previo: el golpe se ve venir, para que dé tiempo a reagrupar.
    private bool windingUp;
    private float windupTimer;
    private SpriteRenderer body;
    private Color baseTint = Color.white;

    private SpriteRenderer telegraph;
    private static Sprite sharedCircle;

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

    public int CurrentHealth => currentHealth;
    public int MaxHealth => data != null ? Mathf.RoundToInt(data.maxHealth * statMultiplier) : 0;
    public int Attack => data != null
        ? Mathf.RoundToInt(data.baseAttack * statMultiplier * attackMultiplier)
        : 0;

    public float AttackMultiplier => attackMultiplier;
    public event Action<int, int> HealthChanged;

    void Awake()
    {
        if (data != null) currentHealth = MaxHealth;
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

        DamageTextManager.Show(transform.position, "¡CARGANDO GOLPE!", new Color(1f, 0.3f, 0.25f));
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

            // Se dibuja por debajo de todo el mundo, como una marca en el suelo.
            telegraph.sortingOrder = -50;
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

        target = nearest;
        if (state == EnemyState.Idle) state = EnemyState.Approach;
    }

    // Un tanque solo puede sujetar a maxAggroPerTank enemigos; el resto va a por la retaguardia.
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
        return retaguardia != null ? retaguardia : masCercano;
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
        float velocidad = data.moveSpeed * Status.SpeedMultiplier;
        float step = Mathf.Min(velocidad * Time.deltaTime, distance - range);
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

        // Tiradores y chamanes disparan: el golpe tarda en llegar y se ve venir.
        if (AttackRange >= rangedThreshold)
        {
            Color tinte = data.magicAttack ? new Color(0.65f, 0.35f, 0.95f) : new Color(0.85f, 0.80f, 0.55f);
            Projectile.Fire(transform.position, target, Attack, tinte, data.magicAttack);
            return;
        }

        target.TakeDamage(Attack, data.magicAttack);
    }

    public void TakeDamage(int amount) => TakeDamage(amount, false, 0f);

    public void TakeDamage(int amount, bool ignoresDefense) => TakeDamage(amount, ignoresDefense, 0f);

    // El daño que ignora armadura entra entero; armorPierce recorta solo una parte de la defensa.
    public void TakeDamage(int amount, bool ignoresDefense, float armorPierce)
    {
        int defensa = Mathf.RoundToInt(data.baseDefense * (1f - Mathf.Clamp01(armorPierce)));
        int finalDamage = ignoresDefense ? Mathf.Max(1, amount) : Mathf.Max(1, amount - defensa);
        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        HealthChanged?.Invoke(currentHealth, MaxHealth);

        DamageTextManager.ShowDamage(transform.position, finalDamage);
        AudioManager.PlayAt(SfxId.Impact, transform.position);
        Debug.Log($"[Enemy] {data.enemyName} recibe {finalDamage} ({currentHealth}/{MaxHealth})", this);

        if (currentHealth <= 0)
        {
            AudioManager.PlayAt(SfxId.Defeat, transform.position);
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
