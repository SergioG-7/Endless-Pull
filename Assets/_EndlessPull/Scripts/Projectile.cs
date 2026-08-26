using UnityEngine;

// Proyectil de las unidades a distancia: viaja hasta el objetivo y allí aplica el daño.
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
            Destroy(gameObject);
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

        Destroy(gameObject);
    }

    private static Projectile Create(Vector2 origin, Color color, bool magic = false)
    {
        AudioManager.PlayAt(magic ? SfxId.MagicBolt : SfxId.ArrowShot, origin);

        var go = new GameObject("Projectile", typeof(SpriteRenderer), typeof(Projectile));
        go.transform.position = origin;
        go.transform.localScale = new Vector3(0.55f, 0.18f, 1f);

        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = Dart();
        sr.color = color;
        sr.sortingOrder = 30;

        return go.GetComponent<Projectile>();
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
