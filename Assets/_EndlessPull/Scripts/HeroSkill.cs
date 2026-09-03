using UnityEngine;

// Habilidad activa del héroe: cuesta maná y entra en enfriamiento tras usarse.
[System.Serializable]
public class HeroSkill
{
    [Tooltip("Nombre visible de la habilidad; solo se usa si no hay subclase detrás.")]
    public string skillName = "Golpe Potente";

    [Tooltip("Subclase de la que sale la habilidad; de ella se saca el nombre localizado.")]
    public HeroSubclass subclass = HeroSubclass.None;

    [Tooltip("Maná que consume cada uso.")]
    public int mpCost = 20;

    [Tooltip("Segundos de enfriamiento entre usos.")]
    public float cooldown = 5f;

    [Tooltip("Multiplicador de daño sobre el ataque efectivo del héroe.")]
    public float damageMultiplier = 2f;

    private float cooldownTimer;

    public float CooldownRemaining => Mathf.Max(0f, cooldownTimer);
    public bool IsReady => cooldownTimer <= 0f;

    // La llama el héroe cada frame para descontar el enfriamiento.
    public void Tick(float deltaTime)
    {
        if (cooldownTimer > 0f) cooldownTimer -= deltaTime;
    }

    public bool CanCast(int currentMP) => IsReady && currentMP >= mpCost;

    // Arranca el enfriamiento; el maná lo descuenta quien lanza la habilidad. reductionFactor
    // (0-1) viene del refinamiento de entrenamiento del héroe (ver HeroProgress.SkillRefinement).
    public void PutOnCooldown(float reductionFactor = 0f)
        => cooldownTimer = cooldown * Mathf.Clamp01(1f - reductionFactor);

    public int DamageFrom(int attack) => Mathf.Max(1, Mathf.RoundToInt(attack * damageMultiplier));

    // El nombre visible sale del diccionario; el campo serializado es solo el respaldo interno
    // (nunca se muestra directamente, para que el héroe sin subclase también salga localizado).
    public string GetDisplayName()
    {
        if (subclass == HeroSubclass.None) return LocalizationManager.Get("UI_SKILL_BASIC_STRIKE");

        string localizado = HeroSubclasses.SkillName(subclass);
        return string.IsNullOrEmpty(localizado) ? skillName : localizado;
    }

    // Lo que hace la habilidad más allá del daño, ya localizado.
    public string GetDescription()
        => subclass == HeroSubclass.None
            ? LocalizationManager.Get("UI_SKILL_BASIC_STRIKE_DESC")
            : HeroSubclasses.DescribeSkill(subclass);
}
