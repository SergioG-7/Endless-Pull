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

    [Tooltip("Arma con la que sale todo héroe nuevo: espada de madera.")]
    [SerializeField] private EquipmentData starterWeapon;

    [Tooltip("Gemas que cuesta cada tirada.")]
    [SerializeField] private int pullCost = 100;

    [Tooltip("Economía de la que se descuenta el coste.")]
    [SerializeField] private EconomyManager economy;

    public int PullCost => pullCost;
    public EquipmentData StarterWeapon => starterWeapon;

    // Todo héroe empieza empuñando algo: sin arma no entrena maestría ni elige subclase.
    public bool GrantStarterWeapon(HeroController hero)
    {
        if (hero == null || starterWeapon == null) return false;
        if (hero.GetEquipped(EquipmentSlot.Weapon) != null) return false;

        hero.Equip(starterWeapon);
        return true;
    }

    void Awake()
    {
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
    }

    void Start()
    {
        StartCoroutine(GrantStarterWeapons());
    }

    // Un frame de espera: el SaveManager restaura el equipo en su propio Start.
    // Cubre a los héroes puestos a mano en la escena y a las partidas viejas sin arma.
    private System.Collections.IEnumerator GrantStarterWeapons()
    {
        yield return null;

        if (starterWeapon == null) yield break;

        foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
            if (GrantStarterWeapon(hero))
                Debug.Log($"[Gacha] {hero.Data.heroName} empieza con {starterWeapon.equipName}.", hero);
    }

    // Busca en el catálogo por nombre de asset; la usa el SaveManager al restaurar el roster.
    public HeroData FindByAssetName(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return null;

        foreach (var hero in catalog)
            if (hero != null && hero.name == assetName) return hero;

        return null;
    }

    // Los invocados aterrizan en el Altar; si no hay altar, en el centro de la base de siempre.
    public Vector2 RandomSpawnPosition()
    {
        if (SummonAltar.TryGetSpawnPoint(out Vector2 altar)) return altar;

        return spawnCenter + new Vector2(
            UnityEngine.Random.Range(-spawnJitter.x, spawnJitter.x),
            UnityEngine.Random.Range(-spawnJitter.y, spawnJitter.y));
    }

    // Instancia un héroe ya elegido; la usan tanto la tirada como la carga de partida.
    public HeroController SpawnHero(HeroData heroData, HeroTrait heroTrait, Vector2 position)
    {
        if (heroData == null || heroPrefab == null) return null;

        var go = Instantiate(heroPrefab, position, Quaternion.identity);
        go.name = $"Hero_{heroData.heroName}_{heroData.starRank}Star";

        // Initialize corre tras Awake y antes del primer Start, así la barra ya lee bien.
        var hero = go.GetComponent<HeroController>();
        hero.Initialize(heroData, heroTrait, spawnCenter, wanderSize);
        return hero;
    }

    // Nombres de los héroes que ya están en la base; el catálogo no puede repetirlos.
    public HashSet<string> LivingHeroNames()
    {
        var names = new HashSet<string>();

        foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
            if (hero.Data != null) names.Add(hero.Data.heroName);

        return names;
    }

    // El catálogo menos los que ya se tienen. extraOwned son los sacados en la misma tanda,
    // que aún no están en la base porque el jugador no ha aceptado el resultado.
    public List<HeroData> AvailableHeroes(HashSet<string> extraOwned = null)
    {
        var owned = LivingHeroNames();
        var available = new List<HeroData>();

        foreach (var hero in catalog)
        {
            if (hero == null || owned.Contains(hero.heroName)) continue;
            if (extraOwned != null && extraOwned.Contains(hero.heroName)) continue;

            available.Add(hero);
        }

        return available;
    }

    public bool HasAvailableHeroes(HashSet<string> extraOwned = null)
        => AvailableHeroes(extraOwned).Count > 0;

    // Tirada ponderada sobre lo que queda por conseguir: primero la rareza, luego el héroe.
    public HeroData PerformPull(HashSet<string> extraOwned = null)
    {
        if (catalog == null || catalog.Count == 0)
        {
            Debug.LogError("[Gacha] El catálogo está vacío.", this);
            return null;
        }

        var available = AvailableHeroes(extraOwned);
        if (available.Count == 0)
        {
            Debug.LogWarning($"[Gacha] Ya tienes los {catalog.Count} héroes del catálogo.", this);
            return null;
        }

        int rank = RollRarity(available);
        HeroData pick = PickFromRank(available, rank);

        // Si esa rareza se agotó se cae a lo que quede libre, nunca al catálogo entero.
        if (pick == null) pick = available[UnityEngine.Random.Range(0, available.Count)];

        return pick;
    }

    public void SummonAndSpawnHero()
    {
        if (heroPrefab == null)
        {
            Debug.LogError("[Gacha] Falta el prefab del héroe.", this);
            return;
        }

        // Si ya están todos, la tirada se bloquea antes de cobrar nada.
        if (!HasAvailableHeroes())
        {
            Debug.LogWarning("[Gacha] Tirada bloqueada: el catálogo está completo.", this);
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

        // El rasgo sale aleatorio en cada invocación, no viene del HeroData.
        HeroTrait trait = HeroTraits.Random();

        var hero = SpawnHero(pulled, trait, RandomSpawnPosition());
        if (hero == null)
        {
            economy.Add(pullCost);
            return;
        }

        // Una o dos pasivas al azar; son innatas y ya no cambian.
        var passives = PassiveSkills.RandomSet();
        hero.SetPassives(passives);
        GrantStarterWeapon(hero);

        DamageTextManager.Show(hero.transform.position, "¡Nuevo Héroe Invocado!",
            new Color(1f, 0.9f, 0.4f));

        Debug.Log($"[Gacha] Pasivas de {pulled.heroName}: {PassiveSkills.Describe(passives)}.", hero.gameObject);
        Debug.Log($"[Gacha] Invocado {pulled.heroName} ({pulled.starRank}★) " +
                  $"[{HeroTraits.DisplayName(trait)}] con {hero.MaxHealth} PV, " +
                  $"{hero.MaxMP} MP y {hero.Attack} ATK.", hero.gameObject);

        SaveManager.RequestSave();
    }

    // Solo pesan las rarezas que aún tienen algún héroe libre.
    private int RollRarity(List<HeroData> available)
    {
        var weights = new float[rarityWeights.Length];
        float total = 0f;

        foreach (var hero in available)
        {
            int index = hero.starRank - 1;
            if (index < 0 || index >= weights.Length) continue;

            // Cada rareza suma su peso una sola vez, no una por héroe.
            if (weights[index] > 0f) continue;

            weights[index] = Mathf.Max(0f, rarityWeights[index]);
            total += weights[index];
        }

        // Si lo que queda solo tiene rarezas con peso 0, se sortea sin ponderar.
        if (total <= 0f) return available[UnityEngine.Random.Range(0, available.Count)].starRank;

        float roll = UnityEngine.Random.Range(0f, total);
        float acc = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            acc += weights[i];
            if (roll < acc) return i + 1;
        }

        return weights.Length;
    }

    private HeroData PickFromRank(List<HeroData> available, int rank)
    {
        var matches = new List<HeroData>();
        foreach (var hero in available)
            if (hero.starRank == rank) matches.Add(hero);

        if (matches.Count == 0) return null;
        return matches[UnityEngine.Random.Range(0, matches.Count)];
    }
}
