using UnityEngine;

// Qué bestiario tiene disponible un tramo de la Torre. Cada hueco de la oleada sortea uno del
// pool, así que la variedad sale del contenido de la banda y no de una cadena de condiciones.
[System.Serializable]
public class FloorBand
{
    [Tooltip("Piso a partir del cual entra este tramo; manda el de fromFloor más alto que no lo supere.")]
    public int fromFloor = 1;

    [Tooltip("Enemigos que pueden salir en este tramo.")]
    public EnemyData[] pool = new EnemyData[0];
}
