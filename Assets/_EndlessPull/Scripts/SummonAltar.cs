using UnityEngine;

// Punto de la base donde aterrizan los héroes recién invocados.
public class SummonAltar : MonoBehaviour
{
    [Tooltip("Desplazamiento sobre el altar al que sale el héroe.")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, -1.2f);

    [Tooltip("Dispersión para que dos invocaciones seguidas no se solapen.")]
    [SerializeField] private Vector2 spawnJitter = new Vector2(0.8f, 0.4f);

    private static SummonAltar instance;

    public static bool Exists => instance != null;

    void OnEnable() => instance = this;

    void OnDisable()
    {
        if (instance == this) instance = null;
    }

    public Vector2 SpawnPoint
        => (Vector2)transform.position + spawnOffset + new Vector2(
            Random.Range(-spawnJitter.x, spawnJitter.x),
            Random.Range(-spawnJitter.y, spawnJitter.y));

    // La usa el gacha; si no hay altar en la escena devuelve false y se usa el punto de siempre.
    public static bool TryGetSpawnPoint(out Vector2 point)
    {
        if (instance == null)
        {
            point = Vector2.zero;
            return false;
        }

        point = instance.SpawnPoint;
        return true;
    }

    public static Vector3 AltarPosition
        => instance != null ? instance.transform.position : Vector3.zero;
}
