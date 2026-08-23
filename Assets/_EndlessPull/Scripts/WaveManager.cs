using System.Collections.Generic;
using UnityEngine;

// Estado de la expedición del piso actual.
public enum ExpeditionState
{
    Idle,
    InProgress,
    Won,
    Lost
}

public class WaveManager : MonoBehaviour
{
    [Tooltip("Prefab de enemigo que se usa para poblar la oleada.")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Datos base del enemigo, antes de escalar por piso.")]
    [SerializeField] private EnemyData enemyData;

    [Tooltip("Economía a la que se abona la recompensa del piso.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Piso en el que está la expedición ahora mismo.")]
    [SerializeField] private int currentFloor = 1;

    [Tooltip("Enemigos del piso 1; cada piso suma uno más.")]
    [SerializeField] private int baseEnemyCount = 2;

    [Tooltip("Incremento de vida y ataque por cada piso superado.")]
    [SerializeField] private float statGrowthPerFloor = 0.2f;

    [Tooltip("Gemas que da superar un piso.")]
    [SerializeField] private int floorReward = 100;

    [Tooltip("Madera que da superar el piso 1; escala con el piso.")]
    [SerializeField] private int woodReward = 20;

    [Tooltip("Hierro que da superar el piso 1; escala con el piso.")]
    [SerializeField] private int ironReward = 10;

    [Tooltip("Centro de la zona de aparición, a la derecha de la arena.")]
    [SerializeField] private Vector2 spawnAreaCenter = new Vector2(6f, 0f);

    [Tooltip("Ancho y alto de la zona de aparición.")]
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(2f, 3f);

    private readonly List<EnemyController> wave = new List<EnemyController>();
    private ExpeditionState state = ExpeditionState.Idle;

    public int CurrentFloor => currentFloor;
    public ExpeditionState State => state;
    public int EnemyCountForFloor => baseEnemyCount + (currentFloor - 1);
    public float StatMultiplierForFloor => 1f + statGrowthPerFloor * (currentFloor - 1);

    public int AliveEnemies
    {
        get { PruneWave(); return wave.Count; }
    }

    // Se dispara con (estado, mensaje) para que la UI muestre el feedback.
    public event System.Action<ExpeditionState, string> ExpeditionChanged;

    void Awake()
    {
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
    }

    public void StartFloorExpedition()
    {
        if (state == ExpeditionState.InProgress)
        {
            Debug.LogWarning("[Expedición] Ya hay una en curso.", this);
            return;
        }

        if (enemyPrefab == null || enemyData == null)
        {
            Debug.LogError("[Expedición] Falta el prefab o los datos del enemigo.", this);
            return;
        }

        if (CountAliveHeroes() == 0)
        {
            Report(ExpeditionState.Idle, "Necesitas al menos un héroe");
            return;
        }

        DespawnWave();

        int count = EnemyCountForFloor;
        float mult = StatMultiplierForFloor;

        for (int i = 0; i < count; i++)
        {
            Vector2 half = spawnAreaSize * 0.5f;
            Vector2 pos = spawnAreaCenter + new Vector2(
                Random.Range(-half.x, half.x),
                Random.Range(-half.y, half.y));

            var go = Instantiate(enemyPrefab, pos, Quaternion.identity);
            go.name = $"Enemy_F{currentFloor}_{i + 1}";

            var enemy = go.GetComponent<EnemyController>();
            enemy.Initialize(enemyData, mult);
            wave.Add(enemy);
        }

        Report(ExpeditionState.InProgress,
            $"Piso {currentFloor}: {count} enemigos (x{mult:0.00})");
    }

    void Update()
    {
        if (state != ExpeditionState.InProgress) return;

        if (AliveEnemies == 0)
        {
            int cleared = currentFloor;
            int woodGain = woodReward * cleared;
            int ironGain = ironReward * cleared;

            economy?.Add(floorReward);
            economy?.AddMaterials(woodGain, ironGain);

            currentFloor++;
            Report(ExpeditionState.Won,
                $"Piso {cleared} superado  +{floorReward} gemas, +{woodGain} madera, +{ironGain} hierro");
            return;
        }

        if (CountAliveHeroes() == 0)
        {
            DespawnWave();
            Report(ExpeditionState.Lost, $"Expedición fallida en el piso {currentFloor}");
        }
    }

    private int CountAliveHeroes()
        => UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None).Length;

    // Quita de la lista los enemigos ya destruidos.
    private void PruneWave()
    {
        for (int i = wave.Count - 1; i >= 0; i--)
            if (wave[i] == null) wave.RemoveAt(i);
    }

    private void DespawnWave()
    {
        foreach (var e in wave)
            if (e != null) Destroy(e.gameObject);

        wave.Clear();
    }

    private void Report(ExpeditionState newState, string message)
    {
        state = newState;
        Debug.Log($"[Expedición] {message}", this);
        ExpeditionChanged?.Invoke(state, message);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
    }
}
