using UnityEngine;
using UnityEngine.InputSystem;

// Arena cerrada de entrenamiento: coloca las unidades del episodio y permite acelerar la simulación.
public class GymManager : MonoBehaviour
{
    [Tooltip("Prefab del enemigo que se instancia en la arena.")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Datos del enemigo contra el que entrena el agente.")]
    [SerializeField] private EnemyData enemyData;

    [Tooltip("Multiplicador de vida y ataque del enemigo de entrenamiento.")]
    [SerializeField] private float enemyMultiplier = 1f;

    [Tooltip("Entrena contra un jefe, con golpe circular y aviso previo.")]
    [SerializeField] private bool enemyIsBoss = true;

    [Tooltip("Escala visual del jefe de entrenamiento.")]
    [SerializeField] private float bossScale = 1.6f;

    [Tooltip("Punto donde arranca el héroe en cada episodio.")]
    [SerializeField] private Vector2 heroSpawn = new Vector2(-4f, 0f);

    [Tooltip("Punto donde arranca el enemigo en cada episodio.")]
    [SerializeField] private Vector2 enemySpawn = new Vector2(4f, 0f);

    [Tooltip("Media anchura y altura de la arena; el agente no puede salir de ahí.")]
    [SerializeField] private Vector2 arenaHalfSize = new Vector2(9f, 4.5f);

    [Tooltip("Velocidad de simulación en modo rápido.")]
    [SerializeField] private float fastTimeScale = 10f;

    [Tooltip("Arranca ya en modo rápido, para entrenar sin tocar nada.")]
    [SerializeField] private bool startFast;

    private EnemyController enemy;
    private bool fast;
    private int episodes;

    public Vector2 HeroSpawn => heroSpawn;
    public Vector2 EnemySpawn => enemySpawn;
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
    }

    public void ToggleFastMode() => SetFast(!fast);

    // fixedDeltaTime acompaña a timeScale, o la física y las decisiones se desincronizan al acelerar.
    public void SetFast(bool value)
    {
        fast = value;
        Time.timeScale = fast ? Mathf.Max(1f, fastTimeScale) : 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

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

        if (hero != null)
        {
            hero.transform.position = heroSpawn;
            hero.ResetForEpisode();
        }

        if (enemyPrefab == null || enemyData == null)
        {
            Debug.LogError("[Gym] Falta el prefab o los datos del enemigo de entrenamiento.", this);
            return null;
        }

        var go = Instantiate(enemyPrefab, enemySpawn, Quaternion.identity);
        go.name = "Gym_Enemy";

        enemy = go.GetComponent<EnemyController>();
        enemy.Initialize(enemyData, enemyMultiplier);
        if (enemyIsBoss) enemy.MakeBoss(bossScale);

        return enemy;
    }

    // Mantiene al agente dentro de la arena; devuelve la posición ya recortada.
    public Vector2 Clamp(Vector2 position)
        => new Vector2(
            Mathf.Clamp(position.x, -arenaHalfSize.x, arenaHalfSize.x),
            Mathf.Clamp(position.y, -arenaHalfSize.y, arenaHalfSize.y));

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(arenaHalfSize.x * 2f, arenaHalfSize.y * 2f, 0f));
    }
}
