using System.Collections.Generic;
using UnityEngine;

public class GachaManager : MonoBehaviour
{
    [Tooltip("Catálogo de héroes invocables; se agrupan solos por starRank.")]
    [SerializeField] private List<HeroData> catalog = new List<HeroData>();

    [Tooltip("Prefab base del héroe que se instancia en cada invocación.")]
    [SerializeField] private GameObject heroPrefab;

    [Tooltip("Pesos por rareza, del índice 0 = 1★ al índice 4 = 5★.")]
    [SerializeField] private float[] rarityWeights = { 60f, 30f, 10f, 0f, 0f };

    [Tooltip("Centro de la base donde aparecen los héroes invocados.")]
    [SerializeField] private Vector2 spawnCenter = Vector2.zero;

    [Tooltip("Dispersión aleatoria alrededor del centro al aparecer.")]
    [SerializeField] private Vector2 spawnJitter = new Vector2(2.5f, 1.5f);

    [Tooltip("Área de paseo que se asigna a cada héroe invocado.")]
    [SerializeField] private Vector2 wanderSize = new Vector2(8f, 5f);

    [Tooltip("Gemas que cuesta cada tirada.")]
    [SerializeField] private int pullCost = 100;

    [Tooltip("Economía de la que se descuenta el coste.")]
    [SerializeField] private EconomyManager economy;

    public int PullCost => pullCost;

    void Awake()
    {
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
    }

    // Tirada ponderada: primero la rareza, luego un héroe cualquiera de esa rareza.
    public HeroData PerformPull()
    {
        if (catalog == null || catalog.Count == 0)
        {
            Debug.LogError("[Gacha] El catálogo está vacío.", this);
            return null;
        }

        int rank = RollRarity();
        HeroData pick = PickFromRank(rank);

        // Si esa rareza no tiene héroes registrados, se cae al catálogo entero.
        if (pick == null)
        {
            pick = catalog[UnityEngine.Random.Range(0, catalog.Count)];
            Debug.LogWarning($"[Gacha] Sin héroes de {rank}★, se usa {pick.heroName} ({pick.starRank}★).", this);
        }

        return pick;
    }

    public void SummonAndSpawnHero()
    {
        if (heroPrefab == null)
        {
            Debug.LogError("[Gacha] Falta el prefab del héroe.", this);
            return;
        }

        // Sin gemas no hay tirada; el cobro va antes de sortear nada.
        if (economy == null || !economy.TrySpend(pullCost))
        {
            int saldo = economy != null ? economy.Gems : 0;
            Debug.LogWarning($"[Gacha] Tirada bloqueada: cuesta {pullCost} y hay {saldo} gemas.", this);
            return;
        }

        HeroData pulled = PerformPull();

        // Si el sorteo falla ya se habia cobrado, así que se devuelve el importe.
        if (pulled == null)
        {
            economy.Add(pullCost);
            return;
        }

        Vector2 pos = spawnCenter + new Vector2(
            UnityEngine.Random.Range(-spawnJitter.x, spawnJitter.x),
            UnityEngine.Random.Range(-spawnJitter.y, spawnJitter.y));

        var go = Instantiate(heroPrefab, pos, Quaternion.identity);
        go.name = $"Hero_{pulled.heroName}_{pulled.starRank}Star";

        // El rasgo sale aleatorio en cada invocación, no viene del HeroData.
        HeroTrait trait = HeroTraits.Random();

        // Initialize corre tras Awake y antes del primer Start, así la barra ya lee bien.
        var hero = go.GetComponent<HeroController>();
        hero.Initialize(pulled, trait, spawnCenter, wanderSize);

        Debug.Log($"[Gacha] Invocado {pulled.heroName} ({pulled.starRank}★) " +
                  $"[{HeroTraits.DisplayName(trait)}] con {hero.MaxHealth} PV y {hero.Attack} ATK.", go);
    }

    // Devuelve el starRank sorteado según los pesos configurados.
    private int RollRarity()
    {
        float total = 0f;
        foreach (float w in rarityWeights) total += Mathf.Max(0f, w);

        if (total <= 0f) return 1;

        float roll = UnityEngine.Random.Range(0f, total);
        float acc = 0f;

        for (int i = 0; i < rarityWeights.Length; i++)
        {
            acc += Mathf.Max(0f, rarityWeights[i]);
            if (roll < acc) return i + 1;
        }

        return rarityWeights.Length;
    }

    private HeroData PickFromRank(int rank)
    {
        var matches = new List<HeroData>();
        foreach (var hero in catalog)
            if (hero != null && hero.starRank == rank) matches.Add(hero);

        if (matches.Count == 0) return null;
        return matches[UnityEngine.Random.Range(0, matches.Count)];
    }
}
