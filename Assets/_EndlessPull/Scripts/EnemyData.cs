using UnityEngine;

[CreateAssetMenu(fileName = "Enemy_New", menuName = "Endless Pull/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Tooltip("Nombre visible del enemigo.")]
    public string enemyName = "Goblin";

    [Tooltip("Vida máxima con la que arranca el enemigo.")]
    public int maxHealth = 50;

    [Tooltip("Daño base antes de aplicar la defensa rival.")]
    public int baseAttack = 8;

    [Tooltip("Defensa que se resta al daño recibido.")]
    public int baseDefense = 2;

    [Tooltip("Velocidad de movimiento en unidades por segundo.")]
    public float moveSpeed = 1.5f;

    [Tooltip("Segundos entre golpe y golpe.")]
    public float attackCooldown = 1f;
}
