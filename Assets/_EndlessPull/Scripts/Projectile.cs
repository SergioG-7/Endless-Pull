using System.Collections.Generic;
using UnityEngine;

// Proyectil de las unidades a distancia: viaja hasta el objetivo y allí aplica el daño.
// Se reutiliza mediante una reserva estática (mismo patrón que AudioManager) en vez de destruirse.
public class Projectile : MonoBehaviour
{
    [Tooltip("Unidades por segundo a las que vuela.")]
    [SerializeField] private float speed = 14f;

    [Tooltip("Segundos tras los que se destruye aunque no haya llegado.")]
    [SerializeField] private float lifetime = 3f;

    [Tooltip("Distancia a la que se considera que ha impactado.")]
    [SerializeField] private float hitDistance = 0.25f;

    private Transform target;
    private int damage;
    private bool ignoresDefense;

    // Punto al que vuela un tiro esquivable: el sitio donde estaba el objetivo al disparar. Los
    // tiros de habilidad siguen persiguiendo al objetivo, que fallar el maná gastado se lee mal.
    private bool dodgeable;
    private Vector2 aimPoint;

    // Quien dispara: hace falta al impactar para cobrarle el robo de vida y su perforacion.
    private HeroController shooter;
    private HeroController heroVictim;
    private EnemyController enemyVictim;

    private static Sprite sharedSprite;

    // Cuenta de tiros esquivables que entran y que se pierden, para calibrar el daño a distancia.
    public static int DodgeableHits;
    public static int DodgeableMisses;

    // Solo los que dispara un héroe. Los totales de arriba mezclan sus tiros con los de los
    // tiradores enemigos, y el factor de daño a distancia se calibra con los del héroe.
    public static int HeroDodgeableHits;
    public static int HeroDodgeableMisses;

    // Reserva de proyectiles; crece bajo demanda y nunca se destruye (evita GC churn).
    private static readonly List<Projectile> pool = new List<Projectile>();
    private static int nextPoolIndex;

    // Cuelgan de aquí en vez de la raíz de la escena, para no parecer basura sin recoger.
    private static Transform poolRoot;

    // Con Enter Play Mode Options (sin recarga de dominio) los estaticos sobreviven al Stop,
    // pero los GameObjects no: la reserva se quedaba llena de referencias muertas que ya nunca
    // se reutilizaban y crecia en cada Play. Se vacia al arrancar cada partida.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPool()
    {
        pool.Clear();
        nextPoolIndex = 0;
        poolRoot = null;
        sharedSprite = null;
        DodgeableHits = 0;
        DodgeableMisses = 0;
        HeroDodgeableHits = 0;
        HeroDodgeableMisses = 0;
    }

    private static Transform PoolRoot()
    {
        if (poolRoot != null) return poolRoot;

        poolRoot = new GameObject("Pool_Projectiles").transform;
        return poolRoot;
    }

    // Valor de fábrica de lifetime, capturado antes de que Update() empiece a descontarlo.
    private float lifetimeDefault;

    void Awake() => lifetimeDefault = lifetime;

    // Disparo contra un enemigo; lo usan arqueros y magos del jugador.
    public static void Fire(Vector2 origin, EnemyController victim, int damage, Color color,
                            bool ignoresDefense = false, HeroController shooter = null, bool magic = false,
                            bool dodgeable = false)
    {
        var p = Create(origin, color, magic);
        if (p == null) return;

        p.enemyVictim = victim;
        p.target = victim.transform;
        p.damage = damage;
        p.ignoresDefense = ignoresDefense;
        p.shooter = shooter;
        p.dodgeable = dodgeable;
        p.aimPoint = victim.transform.position;
    }

    // Disparo contra un héroe; lo usan tiradores goblin y chamanes.
    public static void Fire(Vector2 origin, HeroController victim, int damage, Color color,
                            bool ignoresDefense = false, bool magic = false, bool dodgeable = false)
    {
        var p = Create(origin, color, magic);
        if (p == null) return;

        p.heroVictim = victim;
        p.target = victim.transform;
        p.damage = damage;
        p.ignoresDefense = ignoresDefense;
        p.dodgeable = dodgeable;
        p.aimPoint = victim.transform.position;
    }

void Update()
    {
        lifetime -= Time.deltaTime;

        // Si el objetivo cae antes de llegar, el proyectil se apaga sin hacer nada.
        if (target == null || lifetime <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        // Muro de la arena: cualquier proyectil que lo cruce se destruye, no debe escapar hacia la base.
        Vector2 aqui = transform.position;
        if (WaveManager.HasArenaBounds
            && (aqui.x < WaveManager.ArenaWallMin.x || aqui.x > WaveManager.ArenaWallMax.x
                || aqui.y < WaveManager.ArenaWallMin.y || aqui.y > WaveManager.ArenaWallMax.y))
        {
            gameObject.SetActive(false);
            return;
        }

        // Un tiro esquivable vuela al sitio donde estaba el objetivo, no al objetivo: apartarse
        // basta para que pase de largo. Los de habilidad siguen persiguiéndolo.
        Vector2 destino = dodgeable ? aimPoint : (Vector2)target.position;
        transform.position = Vector2.MoveTowards(transform.position, destino, speed * Time.deltaTime);

        // Se orienta hacia donde va; con un sprite alargado se nota el sentido del tiro.
        Vector2 delta = destino - (Vector2)transform.position;
        if (delta.sqrMagnitude > 0.0001f)
            transform.right = delta.normalized;

        // Impacta a quien pille de paso, esté donde esté ahora.
        if (((Vector2)target.position - (Vector2)transform.position).sqrMagnitude <= hitDistance * hitDistance)
        {
            Impact();
            return;
        }

        if (delta.magnitude > hitDistance) return;

        // Llegó al punto de mira y allí ya no había nadie.
        if (dodgeable) Miss();
        else Impact();
    }

    // Tiro esquivable en vuelo que va a por esa unidad y le llega dentro de la ventana dada. Lo
    // consulta la IA para apartarse; devuelve por dónde viene, para poder salirse de lado.
    public static bool IncomingTo(HeroController victim, float warningSeconds, out Vector2 flightDir)
    {
        flightDir = Vector2.zero;
        if (victim == null) return false;

        foreach (var p in pool)
        {
            if (p == null || !p.gameObject.activeSelf) continue;
            if (!p.dodgeable || p.heroVictim != victim) continue;

            Vector2 recorrido = p.aimPoint - (Vector2)p.transform.position;
            if (recorrido.magnitude > p.speed * warningSeconds) continue;

            flightDir = recorrido.sqrMagnitude > 0.0001f ? recorrido.normalized : Vector2.right;
            return true;
        }

        return false;
    }

    // El tiro se pierde: sin aviso en pantalla la bajada de daño parecería un bug.
    private void Miss()
    {
        if (damage > 0)
        {
            DodgeableMisses++;
            if (shooter != null) HeroDodgeableMisses++;
            DamageTextManager.Show(transform.position, LocalizationManager.Get("FX_DODGE"),
                                   new Color(0.75f, 0.78f, 0.85f));
        }

        gameObject.SetActive(false);
    }

    private void Impact()
    {
        if (dodgeable && damage > 0)
        {
            DodgeableHits++;
            if (shooter != null) HeroDodgeableHits++;
        }

        // Con daño 0 el proyectil es puro adorno: lo usan las habilidades de área.
        if (damage > 0)
        {
            if (enemyVictim != null)
            {
                // Perforacion y robo de vida del tirador se resuelven aqui, no al disparar.
                float perfora = shooter != null ? shooter.ArmorPierce : 0f;
                int antes = enemyVictim.CurrentHealth;

                // La baja es del tirador, no de quien tuviera el enemigo encima.
                EnemyController.SetAttacker(shooter);
                try { enemyVictim.TakeDamage(damage, ignoresDefense, perfora); }
                finally { EnemyController.SetAttacker(null); }

                if (shooter != null) shooter.StealLife(antes - enemyVictim.CurrentHealth);
            }
            else if (heroVictim != null) heroVictim.TakeDamage(damage, ignoresDefense);
        }

        gameObject.SetActive(false);
    }

    private static Projectile Create(Vector2 origin, Color color, bool magic = false)
    {
        AudioManager.PlayAt(magic ? SfxId.MagicBolt : SfxId.ArrowShot, origin);

        var p = GetFromPool();

        // Estado limpio: evita arrastrar objetivo/daño de un uso anterior de la reserva.
        p.target = null;
        p.heroVictim = null;
        p.enemyVictim = null;
        p.shooter = null;
        p.damage = 0;
        p.ignoresDefense = false;
        p.dodgeable = false;
        p.aimPoint = origin;
        p.lifetime = p.lifetimeDefault;

        var t = p.transform;
        t.position = origin;
        t.rotation = Quaternion.identity;
        t.localScale = new Vector3(0.55f, 0.18f, 1f);

        var sr = p.GetComponent<SpriteRenderer>();
        sr.sprite = Dart();
        sr.color = color;

        // Sin capa explícita el proyectil se quedaba en Default, que va por DEBAJO del fondo
        // pintado: la flecha volaba invisible. Va sobre las unidades y bajo el texto de daño.
        sr.sortingLayerID = SortingLayer.NameToID(YSorter.CombatLayer);
        sr.sortingOrder = YSorter.AboveUnitsOrder;

        p.gameObject.SetActive(true);
        return p;
    }

    // Busca un proyectil libre en la reserva; si todos están en uso crea uno nuevo y lo añade.
    private static Projectile GetFromPool()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            int idx = (nextPoolIndex + i) % pool.Count;
            var candidato = pool[idx];
            if (candidato != null && !candidato.gameObject.activeSelf)
            {
                nextPoolIndex = (idx + 1) % pool.Count;
                return candidato;
            }
        }

        var go = new GameObject("Projectile", typeof(SpriteRenderer), typeof(Projectile));
        go.transform.SetParent(PoolRoot(), false);
        var proj = go.GetComponent<Projectile>();
        pool.Add(proj);
        return proj;
    }

    // El proyecto no trae sprite de proyectil; se genera uno blanco y se tiñe al disparar.
    private static Sprite Dart()
    {
        if (sharedSprite != null) return sharedSprite;

        var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                tex.SetPixel(x, y, Color.white);

        tex.Apply();
        sharedSprite = Sprite.Create(tex, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
        return sharedSprite;
    }
}
