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

    // Arranca el enfriamiento; el maná lo descuenta quien lanza la habilidad.
    public void PutOnCooldown() => cooldownTimer = cooldown;

    public int DamageFrom(int attack) => Mathf.Max(1, Mathf.RoundToInt(attack * damageMultiplier));

    // El nombre visible sale del diccionario; el campo serializado es solo el respaldo.
    public string GetDisplayName()
    {
        if (subclass == HeroSubclass.None) return skillName;

        string localizado = HeroSubclasses.SkillName(subclass);
        return string.IsNullOrEmpty(localizado) ? skillName : localizado;
    }

    // Lo que hace la habilidad más allá del daño, ya localizado.
    public string GetDescription()
        => subclass == HeroSubclass.None ? string.Empty : HeroSubclasses.DescribeSkill(subclass);
}
