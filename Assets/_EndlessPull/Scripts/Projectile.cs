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

    // Quien dispara: hace falta al impactar para cobrarle el robo de vida y su perforacion.
    private HeroController shooter;
    private HeroController heroVictim;
    private EnemyController enemyVictim;

    private static Sprite sharedSprite;

    // Reserva de proyectiles; crece bajo demanda y nunca se destruye (evita GC churn).
    private static readonly List<Projectile> pool = new List<Projectile>();
    private static int nextPoolIndex;

    // Cuelgan de aquí en vez de la raíz de la escena, para no parecer basura sin recoger.
    private static Transform poolRoot;

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
                            bool ignoresDefense = false, HeroController shooter = null, bool magic = false)
    {
        var p = Create(origin, color, magic);
        if (p == null) return;

        p.enemyVictim = victim;
        p.target = victim.transform;
        p.damage = damage;
        p.ignoresDefense = ignoresDefense;
        p.shooter = shooter;
    }

    // Disparo contra un héroe; lo usan tiradores goblin y chamanes.
    public static void Fire(Vector2 origin, HeroController victim, int damage, Color color,
                            bool ignoresDefense = false, bool magic = false)
    {
        var p = Create(origin, color, magic);
        if (p == null) return;

        p.heroVictim = victim;
        p.target = victim.transform;
        p.damage = damage;
        p.ignoresDefense = ignoresDefense;
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
        if (aqui.x < WaveManager.ArenaWallMin.x || aqui.x > WaveManager.ArenaWallMax.x
            || aqui.y < WaveManager.ArenaWallMin.y || aqui.y > WaveManager.ArenaWallMax.y)
        {
            gameObject.SetActive(false);
            return;
        }

        Vector2 destino = target.position;
        transform.position = Vector2.MoveTowards(transform.position, destino, speed * Time.deltaTime);

        // Se orienta hacia donde va; con un sprite alargado se nota el sentido del tiro.
        Vector2 delta = destino - (Vector2)transform.position;
        if (delta.sqrMagnitude > 0.0001f)
            transform.right = delta.normalized;

        if (delta.magnitude > hitDistance) return;

        Impact();
    }

    private void Impact()
    {
        // Con daño 0 el proyectil es puro adorno: lo usan las habilidades de área.
        if (damage > 0)
        {
            if (enemyVictim != null)
            {
                // Perforacion y robo de vida del tirador se resuelven aqui, no al disparar.
                float perfora = shooter != null ? shooter.ArmorPierce : 0f;
                int antes = enemyVictim.CurrentHealth;

                enemyVictim.TakeDamage(damage, ignoresDefense, perfora);

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
        p.lifetime = p.lifetimeDefault;

        var t = p.transform;
        t.position = origin;
        t.rotation = Quaternion.identity;
        t.localScale = new Vector3(0.55f, 0.18f, 1f);

        var sr = p.GetComponent<SpriteRenderer>();
        sr.sprite = Dart();
        sr.color = color;
        sr.sortingOrder = 30;

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
