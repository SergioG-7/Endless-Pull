using UnityEngine;

// Punto de encuentro de la escuadra en la base: donde aterriza al volver de la Torre y desde
// donde arranca su paseo, en vez de aparecer disperso alrededor de baseAreaCenter. Un único
// GameObject en la escena marca la posición; el resto del código solo lee TowerGateway.Position.
public class TowerGateway : MonoBehaviour
{
    private static TowerGateway instance;

    // Antes de que exista el GameObject en la escena (o si nunca se coloca), cae al origen
    // de la base, que es lo que se usaba de todas formas.
    public static Vector2 Position => instance != null ? (Vector2)instance.transform.position : Vector2.zero;

    void Awake()
    {
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.55f, 0.35f, 0.95f);
        Gizmos.DrawWireSphere(transform.position, 0.6f);
    }
}
