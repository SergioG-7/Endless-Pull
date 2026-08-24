using TMPro;
using UnityEngine;

// Nivel y experiencia de un héroe concreto; los bonus son por instancia, nunca tocan el HeroData.
public class HeroProgress : MonoBehaviour
{
    [Tooltip("Nivel actual del héroe.")]
    [SerializeField] private int level = 1;

    [Tooltip("EXP necesaria para pasar del nivel 1 al 2.")]
    [SerializeField] private int baseMaxEXP = 20;

    [Tooltip("Cuánto sube el requisito de EXP por cada nivel.")]
    [SerializeField] private float expGrowthPerLevel = 1.5f;

    [Tooltip("Porcentaje de vida máxima que se gana por nivel.")]
    [SerializeField] private float healthGrowthPerLevel = 0.10f;

    [Tooltip("Ataque plano que se gana por nivel.")]
    [SerializeField] private int attackGrowthPerLevel = 2;

    [Tooltip("Niveles que da cada estrella: el tope es estrellas x este valor.")]
    [SerializeField] private int levelsPerStar = 10;

    [Tooltip("Gemas que cuesta ascender una estrella.")]
    [SerializeField] private int ascendGemCost = 500;

    [Tooltip("Piedras de Ascensión que cuesta subir una estrella.")]
    [SerializeField] private int ascendStoneCost = 1;

    [Tooltip("Factor por el que se multiplican las bases del héroe al ascender.")]
    [SerializeField] private float ascensionStatMultiplier = 1.4f;

    [Tooltip("Etiqueta flotante que muestra el nivel sobre la barra.")]
    [SerializeField] private TMP_Text levelLabel;

    private int currentEXP;
    private HeroController hero;

    public int Level => level;
    public int CurrentEXP => currentEXP;
    public int MaxEXP => Mathf.RoundToInt(baseMaxEXP * Mathf.Pow(expGrowthPerLevel, level - 1));

    // El tope de nivel sale de la rareza actual: 1★ Nv.10, 2★ Nv.20, y así hasta 5★ Nv.50.
    public int MaxLevel => hero != null ? Mathf.Max(1, hero.StarRank) * levelsPerStar : levelsPerStar;
    public bool IsMaxLevel => level >= MaxLevel;

    public int AscendGemCost => ascendGemCost;
    public int AscendStoneCost => ascendStoneCost;

    // Solo se asciende a tope de nivel, por debajo de 5★ y con gemas y piedra en mano.
    public bool CanAscend(EconomyManager economy, CraftingManager crafting)
        => IsMaxLevel
           && hero != null && hero.StarRank < 5
           && economy != null && economy.CanAfford(ascendGemCost)
           && crafting != null && crafting.AscensionStones >= ascendStoneCost;

    public event System.Action<int> LevelChanged;
    public event System.Action<int, int> EXPChanged;

    void Awake()
    {
        hero = GetComponent<HeroController>();
    }

    void Start()
    {
        RefreshLabel();
    }

    // La usa el SaveManager: rehace el nivel aplicando los bonus uno a uno, como si hubiera subido.
    public void LoadState(int savedLevel, int savedEXP)
    {
        savedLevel = Mathf.Max(1, savedLevel);

        while (level < savedLevel)
        {
            level++;
            if (hero != null) hero.ApplyLevelUpBonus(healthGrowthPerLevel, attackGrowthPerLevel);
        }

        currentEXP = Mathf.Max(0, savedEXP);

        RefreshLabel();
        LevelChanged?.Invoke(level);
        EXPChanged?.Invoke(currentEXP, MaxEXP);
    }

    public void AddEXP(int amount)
    {
        if (amount <= 0) return;

        currentEXP += amount;

        // While y no if: una recompensa grande puede dar varios niveles de golpe.
        while (level < MaxLevel && currentEXP >= MaxEXP)
        {
            currentEXP -= MaxEXP;
            LevelUp();
        }

        // A tope de rareza la EXP deja de acumularse; hay que ascender para seguir.
        if (level >= MaxLevel) currentEXP = 0;

        EXPChanged?.Invoke(currentEXP, MaxEXP);
    }

    // Sube una estrella: cobra, escala las bases del héroe y devuelve el nivel a 1.
    public bool AscendHero(EconomyManager economy, CraftingManager crafting)
    {
        if (!IsMaxLevel)
        {
            Debug.LogWarning($"[Ascensión] {name} necesita llegar al Nv. {MaxLevel} antes de ascender.", this);
            return false;
        }

        if (hero == null || hero.StarRank >= 5)
        {
            Debug.LogWarning($"[Ascensión] {name} ya está en la rareza máxima.", this);
            return false;
        }

        // Se comprueba todo antes de cobrar nada, para no dejar el pago a medias.
        if (!CanAscend(economy, crafting))
        {
            Debug.LogWarning($"[Ascensión] Faltan recursos: {ascendGemCost} gemas " +
                             $"y {ascendStoneCost} Piedra(s) de Ascensión.", this);
            return false;
        }

        economy.TrySpend(ascendGemCost);
        for (int i = 0; i < ascendStoneCost; i++) crafting.TryConsumeStone();

        hero.ApplyAscension(ascensionStatMultiplier);

        level = 1;
        currentEXP = 0;

        RefreshLabel();
        LevelChanged?.Invoke(level);
        EXPChanged?.Invoke(currentEXP, MaxEXP);

        Debug.Log($"[Ascensión] {hero.Data.heroName} asciende a {hero.StarRank}★ " +
                  $"(x{hero.AscensionMultiplier:0.00} bases, tope Nv. {MaxLevel}).", this);

        SaveManager.RequestSave();
        return true;
    }

    private void LevelUp()
    {
        level++;

        int healthGain = hero != null
            ? hero.ApplyLevelUpBonus(healthGrowthPerLevel, attackGrowthPerLevel)
            : 0;

        RefreshLabel();
        LevelChanged?.Invoke(level);

        string who = hero != null && hero.Data != null ? hero.Data.heroName : name;
        Debug.Log($"[Nivel] ¡Subida de Nivel! {who} -> Nv. {level} (+{healthGain} PV máx, +{attackGrowthPerLevel} ATK)", this);
    }

    // El rótulo flotante identifica a la unidad de un vistazo en la base.
    private void RefreshLabel()
    {
        if (levelLabel == null) return;

        if (hero == null || hero.Data == null)
        {
            levelLabel.text = $"Nv. {level}";
            return;
        }

        levelLabel.text = $"{hero.Data.heroName} [{hero.StarRank}*] Nv.{level}";
    }
}
