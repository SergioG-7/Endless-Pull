using UnityEngine;

// Orden de dibujado por eje Y: lo que está más abajo en pantalla se pinta delante.
// Sin esto un héroe que pasa por delante de un edificio queda tapado por su tarjeta.
public class YSorter : MonoBehaviour
{
    [Tooltip("Renderers a ordenar; vacío coge todos los del objeto y sus hijos.")]
    [SerializeField] private SpriteRenderer[] renderers;

    [Tooltip("Cuánto pesa cada unidad de mundo en el orden; más alto separa más.")]
    [SerializeField] private int precision = 100;

    [Tooltip("Desplazamiento del orden dentro de su capa; separa héroes de edificios.")]
    [SerializeField] private int baseOrder;

    [Tooltip("Punto de los pies respecto al pivote; es lo que decide quién va delante.")]
    [SerializeField] private float footOffset;

    [Tooltip("Solo reordena si el objeto se ha movido; los edificios se quedan quietos.")]
    [SerializeField] private bool onlyWhenMoving = true;

    // Los hijos conservan su desnivel original para no aplastar la pila del prefab.
    private int[] offsets;
    private float lastY = float.NaN;

    void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>(true);

        offsets = new int[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            offsets[i] = renderers[i] != null ? renderers[i].sortingOrder : 0;

        Sort();
    }

    void LateUpdate()
    {
        if (onlyWhenMoving && Mathf.Approximately(transform.position.y, lastY)) return;

        Sort();
    }

    private void Sort()
    {
        if (renderers == null) return;

        lastY = transform.position.y;

        // Se niega la Y: cuanto más abajo, mayor el orden y por tanto más al frente.
        int orden = baseOrder - Mathf.RoundToInt((lastY + footOffset) * precision);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            renderers[i].sortingOrder = orden + offsets[i];
        }
    }
}
