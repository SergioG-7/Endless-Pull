using UnityEngine;

// NPC de escolta (Friacis Al Lagner): no lucha, pero su vida marca la derrota de la misión.
public class EscortNpc : MonoBehaviour
{
    [Tooltip("Vida máxima del NPC de escolta.")]
    [SerializeField] private int maxHealth = 220;

    [Tooltip("Nombre visible en el HUD y los banners.")]
    [SerializeField] private string npcName = "Friacis Al Lagner";

    private int currentHealth;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public string NpcName => npcName;
    public bool IsDefeated => currentHealth <= 0;

    public event System.Action<int, int> HealthChanged;
    public event System.Action Defeated;

    void Awake() => currentHealth = maxHealth;

    // La usa WaveManager al desplegarla, para poder escalar su vida por piso sin tocar el prefab.
    public void Initialize(int hp)
    {
        maxHealth = Mathf.Max(1, hp);
        currentHealth = maxHealth;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (IsDefeated || amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0) Defeated?.Invoke();
    }
}
