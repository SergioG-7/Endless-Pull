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

    [Tooltip("Distancia a la que deja de acercarse y empieza a atacar.")]
    public float attackRange = 1.1f;

    [Tooltip("Velocidad de movimiento en unidades por segundo.")]
    public float moveSpeed = 1.5f;

    [Tooltip("Segundos entre golpe y golpe.")]
    public float attackCooldown = 1f;

    [Tooltip("Ataque mágico: el daño entra sin restar la defensa del héroe.")]
    public bool magicAttack;
}
