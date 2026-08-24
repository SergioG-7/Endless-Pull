using UnityEngine;

// Habilidad activa del héroe: cuesta maná y entra en enfriamiento tras usarse.
[System.Serializable]
public class HeroSkill
{
    [Tooltip("Nombre visible de la habilidad.")]
    public string skillName = "Golpe Potente";

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
}
