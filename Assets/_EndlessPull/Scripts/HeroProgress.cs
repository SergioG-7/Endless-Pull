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

    [Tooltip("Gemas que cuesta ascender según la rareza actual: índice 0 = 1★→2★ ... índice 3 = 4★→5★.")]
    [SerializeField] private int[] ascendGemCostByStar = { 100, 250, 500, 1000 };

    // Tier de Piedra que exige cada salto, en el mismo orden: 1★→2★ Menor ... 4★→5★ Legendaria.
    private static readonly AscensionStoneTier[] AscendStoneTierByStar =
    {
        AscensionStoneTier.Menor, AscensionStoneTier.Media,
        AscensionStoneTier.Mayor, AscensionStoneTier.Legendaria
    };

    [Tooltip("Factor por el que se multiplican las bases del héroe al ascender.")]
    [SerializeField] private float ascensionStatMultiplier = 1.4f;

    public float AscensionStatMultiplier => ascensionStatMultiplier;

    [Tooltip("Etiqueta flotante que muestra el nivel sobre la barra.")]
    [SerializeField] private TMP_Text levelLabel;

    [Tooltip("0 = pasivo, deja pasar el golpe en área del jefe con tal de seguir pegando. 1 = prudente, siempre esquiva.")]
    [SerializeField, Range(0f, 1f)] private float aggression = 0.5f;

    [Tooltip("0 = no esquiva ni mantiene distancia con arco/báculo. 1 = esquiva el golpe en área y kitea al máximo.")]
    [SerializeField, Range(0f, 1f)] private float safeDistance = 0.5f;

    [Tooltip("Vida restante del enemigo por debajo de la cual se guarda la habilidad para no desperdiciarla en un rematador básico.")]
    [SerializeField, Range(0f, 1f)] private float skillThreshold = 0.15f;

    public float Aggression => aggression;
    public float SafeDistance => safeDistance;
    public float SkillThreshold => skillThreshold;

    private int currentEXP;
    private HeroController hero;

    public int Level => level;
    public int CurrentEXP => currentEXP;
    public int MaxEXP => Mathf.RoundToInt(baseMaxEXP * Mathf.Pow(expGrowthPerLevel, level - 1));

    // El tope de nivel sale de la rareza actual: 1★ Nv.10, 2★ Nv.20, y así hasta 5★ Nv.50.
    public int MaxLevel => hero != null ? Mathf.Max(1, hero.StarRank) * levelsPerStar : levelsPerStar;
    public bool IsMaxLevel => level >= MaxLevel;

    // Sube con la rareza: 1★→2★ es barato, 4★→5★ cuesta el doble que 3★→4★.
    public int AscendGemCost => CostForStar(hero != null ? hero.StarRank : 1);

    // La Piedra que exige el salto actual: 1★→2★ Menor ... 4★→5★ Legendaria.
    public AscensionStoneTier AscendStoneTier => TierForStar(hero != null ? hero.StarRank : 1);

    private int CostForStar(int starRank)
    {
        int index = Mathf.Clamp(starRank - 1, 0, ascendGemCostByStar.Length - 1);
        return ascendGemCostByStar[index];
    }

    private static AscensionStoneTier TierForStar(int starRank)
    {
        int index = Mathf.Clamp(starRank - 1, 0, AscendStoneTierByStar.Length - 1);
        return AscendStoneTierByStar[index];
    }

    // Solo se asciende a tope de nivel, por debajo de 5★ y con gemas y piedra del tier exacto en mano.
    public bool CanAscend(EconomyManager economy, CraftingManager crafting)
        => IsMaxLevel
           && hero != null && hero.StarRank < 5
           && economy != null && economy.CanAfford(AscendGemCost)
           && crafting != null && crafting.HasStone(AscendStoneTier);

    // Color por rareza del rótulo flotante: 1* gris, 2* verde, 3* azul, 4* morado, 5* dorado.
    public static Color RarityColor(int starRank) => UITheme.Rarity(starRank);

    public event System.Action<int> LevelChanged;
    public event System.Action<int, int> EXPChanged;

    // Se dispara al completar una ascensión; lo escucha el Archivo del Santuario.
    public static event System.Action<HeroController, int> HeroAscended;

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

    // La usa el SaveManager; separado de LoadState para no tocar la firma que ya usan los saves viejos.
    public void LoadTactics(float savedAggression, float savedSafeDistance, float savedSkillThreshold)
    {
        aggression = Mathf.Clamp01(savedAggression);
        safeDistance = Mathf.Clamp01(savedSafeDistance);
        skillThreshold = Mathf.Clamp01(savedSkillThreshold);
    }

    // Desde el roster/ficha: el jugador ajusta la personalidad de combate del héroe.
    public void SetTactics(float newAggression, float newSafeDistance, float newSkillThreshold)
    {
        LoadTactics(newAggression, newSafeDistance, newSkillThreshold);
        SaveManager.RequestSave();
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
            Debug.LogWarning($"[Ascensión] Faltan recursos: {AscendGemCost} gemas " +
                             $"y 1 Piedra {AscendStoneTier}.", this);
            return false;
        }

        economy.TrySpend(AscendGemCost);
        crafting.TryConsumeStone(AscendStoneTier);
        AudioManager.Play(SfxId.Ascension);

        hero.ApplyAscension(ascensionStatMultiplier);
        GrantSubclassIfDue();
        HeroAscended?.Invoke(hero, hero.StarRank);

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

    // A partir de 3 estrellas el héroe se especializa en el arquetipo del arma que empuña.
    public void GrantSubclassIfDue()
    {
        if (hero == null || hero.StarRank < HeroSubclasses.MinStarRank) return;
        if (hero.Subclass != HeroSubclass.None) return;

        var arquetipo = hero.EquippedWeaponType;
        if (HeroSubclasses.OptionsFor(arquetipo).Length == 0) arquetipo = WeaponType.Sword;

        // Con modal en escena elige el jugador; el sorteo solo cubre que no lo haya.
        if (SubclassSelectionUI.Offer(hero, arquetipo)) return;

        var elegida = HeroSubclasses.RandomFor(arquetipo);
        hero.SetSubclass(elegida);

        Debug.Log($"[Subclase] {hero.Data.heroName} se especializa como {HeroSubclasses.DisplayName(elegida)} " +
                  $"({WeaponTypes.DisplayName(arquetipo)}).", this);
    }

    // Cambio manual desde el roster; solo entre las tres del mismo arquetipo.
    public bool CycleSubclass()
    {
        if (hero == null || hero.StarRank < HeroSubclasses.MinStarRank) return false;

        var arquetipo = hero.Subclass != HeroSubclass.None
            ? HeroSubclasses.ArchetypeOf(hero.Subclass)
            : hero.EquippedWeaponType;

        var opciones = HeroSubclasses.OptionsFor(arquetipo);
        if (opciones.Length == 0) return false;

        int indice = 0;
        for (int i = 0; i < opciones.Length; i++)
            if (opciones[i] == hero.Subclass) indice = i + 1;

        hero.SetSubclass(opciones[indice % opciones.Length]);
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
            levelLabel.text = $"{LocalizationManager.Get("UI_LEVEL_ABBR")} {level}";
            return;
        }

        // Insignia bajo el token: estrellas y nombre con el color de rareza, nivel en blanco.
        var estrellas = new System.Text.StringBuilder();
        for (int i = 0; i < hero.StarRank; i++) estrellas.Append('★');

        string rareza = UITheme.Tag(RarityColor(hero.StarRank));
        levelLabel.text = $"<color={rareza}>{estrellas} {hero.Data.heroName}</color>" +
                          $" · <color={UITheme.Tag(UITheme.Text)}>{LocalizationManager.Get("UI_LEVEL_ABBR")} {level}</color>";
        levelLabel.color = UITheme.Text;
    }
}
