using UnityEngine;

// Punto de encuentro de la escuadra en la base: donde aterriza al volver de la Torre y desde
// donde arranca su paseo, en vez de aparecer disperso alrededor de baseAreaCenter. Un único
// GameObject en la escena marca la posición; el resto del código solo lee TowerGateway.Position.
public class TowerGateway : MonoBehaviour
{
    [Tooltip("Radio que los héroes dejan libre alrededor del Portal al pasear; tiene que cubrir la ilustración, no el punto.")]
    [SerializeField] private float keepOutRadius = 3.6f;

    private static TowerGateway instance;

    // Hueco alrededor del Portal. Con el marcador de un punto valía 1,3; con la ilustración de
    // 8 unidades de ancho, los héroes se paseaban por dentro de la torre.
    public static float KeepOut => instance != null ? instance.keepOutRadius : 1.3f;

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
        Gizmos.DrawWireSphere(transform.position, keepOutRadius);
    }
}
