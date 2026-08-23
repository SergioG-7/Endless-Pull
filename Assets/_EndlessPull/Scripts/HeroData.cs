using UnityEngine;

[CreateAssetMenu(fileName = "Hero_New", menuName = "Endless Pull/Hero Data")]
public class HeroData : ScriptableObject
{
    [Tooltip("Nombre visible del héroe.")]
    public string heroName = "Loki";

    [Tooltip("Rareza del héroe, de 1 a 5 estrellas.")]
    [Range(1, 5)]
    public int starRank = 1;

    [Tooltip("Vida máxima con la que arranca el héroe.")]
    public int maxHealth = 100;

    [Tooltip("Daño base antes de aplicar la defensa rival.")]
    public int baseAttack = 15;

    [Tooltip("Defensa que se resta al daño recibido.")]
    public int baseDefense = 5;

    [Tooltip("Velocidad de movimiento en unidades por segundo.")]
    public float moveSpeed = 2.5f;
}
