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
    [SerializeField] private float expGrowthPerLevel = 1.10f;

    [Tooltip("Porcentaje de la vida base que se gana por nivel.")]
    [SerializeField] private float healthGrowthPerLevel = 0.06f;

    [Tooltip("Porcentaje del ataque base que se gana por nivel; el grueso del daño lo pone el equipo.")]
    [SerializeField] private float attackGrowthPerLevel = 0.05f;

    [Tooltip("Porcentaje del maná base que se gana por nivel; sin esto todos se quedaban en el 50 del asset.")]
    [SerializeField] private float manaGrowthPerLevel = 0.06f;

    [Tooltip("Tope de nivel por rareza, del 1★ al 7★; la curva del manhwa no es lineal.")]
    [SerializeField] private int[] levelCapByStar = { 10, 20, 40, 60, 80, 99, 110 };

    [Tooltip("Niveles por estrella para rarezas fuera de la tabla de arriba.")]
    [SerializeField] private int levelsPerStar = 10;

    [Tooltip("Techo de pericia técnica del 1★ al 7★; a menos estrellas, más lejos llega el muñeco.")]
    [SerializeField] private float[] refinementCapByStar = { 2.2f, 2f, 1.75f, 1.5f, 1.25f, 1f, 1f };

    [Tooltip("Gemas que cuesta ascender según la rareza actual: índice 0 = 1★→2★ ... índice 5 = 6★→7★.")]
    [SerializeField] private int[] ascendGemCostByStar = { 100, 250, 500, 1000, 1800, 3000 };

    // Tier de Piedra que exige cada salto, en el mismo orden: 1★→2★ Menor ... 6★→7★ Celestial.
    private static readonly AscensionStoneTier[] AscendStoneTierByStar =
    {
        AscensionStoneTier.Menor, AscensionStoneTier.Media,
        AscensionStoneTier.Mayor, AscensionStoneTier.Legendaria,
        AscensionStoneTier.Trascendente, AscensionStoneTier.Celestial
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

    // Refinamiento de habilidad (0-1): crece entrenando en el muñeco, baja el enfriamiento y
    // sube la precisión de la habilidad activa. Nunca baja por su cuenta.
    private float skillRefinement;
    public float SkillRefinement => skillRefinement;

    public int Level => level;
    public int CurrentEXP => currentEXP;
    public int MaxEXP
    {
        get
        {
            double requisito = baseMaxEXP * System.Math.Pow(expGrowthPerLevel, level - 1);
            return (int)System.Math.Min(requisito, int.MaxValue);
        }
    }

    // El tope de nivel sale de la rareza actual: 1★ Nv.10, 2★ Nv.20, 3★ Nv.40 y así hasta 7★ Nv.110.
    // Tope por rareza leído de la tabla; fuera de ella se cae al reparto lineal de siempre.
    public int MaxLevel
    {
        get
        {
            int estrellas = hero != null ? Mathf.Max(1, hero.StarRank) : 1;

            return levelCapByStar != null && estrellas <= levelCapByStar.Length
                ? levelCapByStar[estrellas - 1]
                : estrellas * levelsPerStar;
        }
    }
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

    // Solo se asciende a tope de nivel, por debajo de 7★ y con gemas y piedra del tier exacto en mano.
    public bool CanAscend(EconomyManager economy, CraftingManager crafting)
        => IsMaxLevel
           && hero != null && hero.StarRank < 7
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
            if (hero != null) hero.ApplyLevelUpBonus(healthGrowthPerLevel, attackGrowthPerLevel, manaGrowthPerLevel);
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

    // La usa el muñeco de entrenamiento (BaseBuilding); la carga el SaveManager al restaurar.
    public void AddSkillRefinement(float amount)
    {
        if (amount <= 0f) return;
        skillRefinement = Mathf.Clamp(skillRefinement + amount, 0f, RefinementCap);
    }

    public void LoadSkillRefinement(float saved) => skillRefinement = Mathf.Clamp(saved, 0f, RefinementCap);

    // Techo de pericia técnica INVERTIDO por rareza: el 1★ llega mucho más alto que el 6★, así
    // que las horas de muñeco compensan las estrellas que no tiene. Del 6★ hacia arriba se queda
    // en el 1,0 de siempre, para no tocar lo que ya estaba equilibrado.
    public float RefinementCap
    {
        get
        {
            int estrellas = hero != null ? hero.StarRank : 1;
            if (refinementCapByStar == null || refinementCapByStar.Length == 0) return 1f;

            int indice = Mathf.Clamp(estrellas - 1, 0, refinementCapByStar.Length - 1);
            return Mathf.Max(1f, refinementCapByStar[indice]);
        }
    }

    public void AddEXP(int amount)
    {
        if (amount <= 0) return;

        // Crecimiento Monstruoso y compañía: la pasiva multiplica lo que entra.
        if (hero != null)
            amount = Mathf.Max(1, Mathf.RoundToInt(amount * PassiveSkills.ExpMultiplier(hero.Passives)));

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

        if (hero == null || hero.StarRank >= 7)
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

        // El nivel NO se reinicia al ascender: lo que sube es el tope por rareza. Reiniciarlo
        // tiraba a la basura todo lo peleado y hacía que ascender fuese un castigo.
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
    public void GrantSubclassIfDue(bool allowUiOffer = true)
    {
        if (hero == null || hero.StarRank < HeroSubclasses.MinStarRank) return;
        if (hero.Subclass != HeroSubclass.None) return;

        var arquetipo = hero.EquippedWeaponType;
        if (HeroSubclasses.OptionsFor(arquetipo).Length == 0) arquetipo = WeaponType.Sword;

        // Con modal en escena elige el jugador; el sorteo solo cubre que no lo haya.
        if (allowUiOffer && SubclassSelectionUI.Offer(hero, arquetipo)) return;

        int nacimiento = hero.Data != null ? hero.Data.starRank : hero.StarRank;
        var elegida = HeroSubclasses.RandomFor(arquetipo, nacimiento);
        hero.SetSubclass(elegida);

        Debug.Log($"[Subclase] {hero.Data.heroName} se especializa como {HeroSubclasses.DisplayName(elegida)} " +
                  $"({WeaponTypes.DisplayName(arquetipo)}).", this);
    }

    private void LevelUp()
    {
        level++;

        int healthGain = hero != null
            ? hero.ApplyLevelUpBonus(healthGrowthPerLevel, attackGrowthPerLevel, manaGrowthPerLevel)
            : 0;

        RefreshLabel();
        LevelChanged?.Invoke(level);

        string who = hero != null && hero.Data != null ? hero.Data.heroName : name;
        int atk = hero != null ? hero.Attack : 0;
        Debug.Log($"[Nivel] ¡Subida de Nivel! {who} -> Nv. {level} (+{healthGain} PV máx, ATK {atk})", this);
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
