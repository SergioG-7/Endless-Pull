using UnityEngine;

// Orden de dibujado por eje Y: lo que está más abajo en pantalla se pinta delante.
// Sin esto un héroe que pasa por delante de un edificio queda tapado por su tarjeta.
public class YSorter : MonoBehaviour
{
    // Capa de todo lo que se dibuja sobre el combate. Default va por DEBAJO de Environment, así
    // que un sprite creado por código sin capa acaba detrás del fondo pintado y no se ve.
    public const string CombatLayer = "Characters";

    // Marcas de suelo (aviso de golpe en área, Suelo Marcado): bajo las unidades, cuyo orden sale
    // de Sort() y ronda el ±1500 en la arena.
    public const int GroundMarkOrder = -10000;

    // Proyectiles y rótulos de estado: sobre las unidades y bajo el texto de daño (20000).
    public const int AboveUnitsOrder = 10000;

    [Tooltip("Renderers a ordenar; vacío coge todos los del objeto y sus hijos.")]
    // Renderer y no SpriteRenderer: los rótulos de TextMeshPro tienen MeshRenderer, y si se
    // quedan fuera el cuerpo del edificio acaba tapando su propio nombre.
    [SerializeField] private Renderer[] renderers;

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
            renderers = GetComponentsInChildren<Renderer>(true);

        offsets = new int[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            offsets[i] = renderers[i] != null ? renderers[i].sortingOrder : 0;

        Sort();
    }

    // Para los objetos que se ordenan por código (los edificios): fija los parámetros y reordena
    // ya, porque Awake se ha ejecutado con los valores por defecto al añadir el componente.
    public void Configure(int order, float foot, bool moving)
    {
        baseOrder = order;
        footOffset = foot;
        onlyWhenMoving = moving;
        lastY = float.NaN;
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
