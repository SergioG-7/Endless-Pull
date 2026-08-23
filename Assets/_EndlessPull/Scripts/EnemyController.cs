using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Tooltip("Datos del enemigo: stats y velocidad.")]
    [SerializeField] private EnemyData data;

    private int currentHealth;

    public EnemyData Data => data;
    public int CurrentHealth => currentHealth;

    void Start()
    {
        if (data == null)
        {
            Debug.LogError($"[EnemyController] '{name}' no tiene EnemyData asignado.", this);
            enabled = false;
            return;
        }

        currentHealth = data.maxHealth;
    }

    public void TakeDamage(int amount)
    {
        int finalDamage = Mathf.Max(1, amount - data.baseDefense);
        currentHealth -= finalDamage;

        Debug.Log($"[Enemy] {data.enemyName} recibe {finalDamage} ({currentHealth}/{data.maxHealth})", this);

        if (currentHealth <= 0)
        {
            Debug.Log($"[Enemy] {data.enemyName} destruido.", this);
            Destroy(gameObject);
        }
    }
}
