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

    [Tooltip("Etiqueta flotante que muestra el nivel sobre la barra.")]
    [SerializeField] private TMP_Text levelLabel;

    private int currentEXP;
    private HeroController hero;

    public int Level => level;
    public int CurrentEXP => currentEXP;
    public int MaxEXP => Mathf.RoundToInt(baseMaxEXP * Mathf.Pow(expGrowthPerLevel, level - 1));

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

    public void AddEXP(int amount)
    {
        if (amount <= 0) return;

        currentEXP += amount;

        // While y no if: una recompensa grande puede dar varios niveles de golpe.
        while (currentEXP >= MaxEXP)
        {
            currentEXP -= MaxEXP;
            LevelUp();
        }

        EXPChanged?.Invoke(currentEXP, MaxEXP);
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

    private void RefreshLabel()
    {
        if (levelLabel != null) levelLabel.text = $"Nv. {level}";
    }
}
