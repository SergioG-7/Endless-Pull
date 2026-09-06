using UnityEngine;

// Fondo pintado de la base: el vacío dimensional detrás y la isla flotante encima. Sustituye al
// suelo y las calles que BaseDecor dibujaba por código con formas planas tintadas.
//
// Los dos sprites se escalan por tamaño en unidades de mundo, no por Pixels Per Unit del
// importador: así se recolocan desde el inspector sin reimportar la textura.
[ExecuteAlways]
public class BaseBackdrop : MonoBehaviour
{
    [Tooltip("Vacío dimensional; tiene que tapar la pantalla con el zoom más alejado.")]
    [SerializeField] private Sprite backgroundSprite;

    [Tooltip("Isla flotante recortada, con el vacío ya en transparente.")]
    [SerializeField] private Sprite islandSprite;

    [Tooltip("Centro del decorado, en unidades de mundo.")]
    [SerializeField] private Vector2 center = new Vector2(0f, -4f);

    [Tooltip("Tamaño del vacío del fondo; de sobra para el zoom máximo de la cámara.")]
    [SerializeField] private Vector2 backgroundSize = new Vector2(240f, 134f);

    [Tooltip("Tamaño de la isla; tiene que cubrir la rejilla de edificios entera.")]
    [SerializeField] private Vector2 islandSize = new Vector2(68f, 38f);

    [Tooltip("Zona pisable de la isla, sin contar el reborde de roca; nadie pasea fuera de aquí.")]
    [SerializeField] private Vector2 walkableSize = new Vector2(44f, 33f);

    [Tooltip("Desplazamiento de la zona pisable respecto al centro de la isla.")]
    [SerializeField] private Vector2 walkableOffset = Vector2.zero;

    // Límites de la isla en coordenadas de mundo. Estáticos por el mismo motivo que los de la
    // arena en WaveManager: los lee el héroe desde su Update sin buscar el componente.
    public static Vector2 WalkableMin { get; private set; }
    public static Vector2 WalkableMax { get; private set; }
    public static bool HasWalkableArea { get; private set; }

    [Tooltip("Orden de dibujado del vacío; por debajo de todo.")]
    [SerializeField] private int backgroundOrder = -200;

    [Tooltip("Orden de dibujado de la isla; por encima del vacío y por debajo de todo lo demás.")]
    [SerializeField] private int islandOrder = -150;

    // Otros fondos pintados del mundo (arena de la Torre, claro de recolección). Viven aquí y no
    // en su propio componente porque el montaje es el mismo: un sprite escalado a un tamaño de
    // mundo, en su sitio y en su orden.
    [System.Serializable]
    public class Layer
    {
        [Tooltip("Nombre del hijo que se crea; tiene que ser único.")]
        public string name = "Backdrop_Extra";

        public Sprite sprite;

        [Tooltip("Centro en coordenadas de mundo.")]
        public Vector2 center;

        [Tooltip("Tamaño en unidades de mundo; tiene que cubrir los límites de esa zona.")]
        public Vector2 size = new Vector2(30f, 17f);

        [Tooltip("Orden de dibujado dentro de la capa Environment.")]
        public int order = -150;
    }

    [Tooltip("Fondos de las demás zonas: arena de la Torre y claro de recolección.")]
    [SerializeField] private Layer[] extraLayers = new Layer[0];

    [Tooltip("Nombre de la capa que hace de suelo de la arena; es la que cambia con el bioma.")]
    [SerializeField] private string arenaLayerName = "Backdrop_Arena";

    [Tooltip("Suelo de la arena por bioma, en el orden de TowerBiome: Goblin, Minas, Cripta, Templo.")]
    [SerializeField] private Sprite[] arenaBiomeSprites = new Sprite[0];

    private static BaseBackdrop instance;

    // Suelo de la arena según el piso. Lo llama WaveManager al publicar el piso, y sustituye al
    // tinte plano de TowerBiome que antes pintaba el fondo de cámara.
    public static void ApplyFloor(int floor)
    {
        if (instance == null) return;

        instance.SetArenaBiome(TowerBiome.IndexForFloor(Mathf.Max(1, floor)));
    }

    private void SetArenaBiome(int index)
    {
        if (arenaBiomeSprites == null || arenaBiomeSprites.Length == 0) return;
        if (index < 0 || index >= arenaBiomeSprites.Length) return;

        var sprite = arenaBiomeSprites[index];
        if (sprite == null) return;

        var hijo = transform.Find(arenaLayerName);
        if (hijo == null) return;

        var sr = hijo.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        sr.sprite = sprite;

        // El tamaño está en unidades de mundo, no en píxeles: hay que reescalar por cada sprite,
        // que no todos vienen con la misma resolución.
        foreach (var capa in extraLayers)
            if (capa != null && capa.name == arenaLayerName)
                hijo.localScale = EscalaPara(sprite, capa.size);
    }

    private SpriteRenderer fondo;
    private SpriteRenderer isla;

    // Solo en OnEnable: hacerlo también en OnValidate provoca avisos de SendMessage al crear
    // los hijos desde el editor. Para reajustar el encuadre se llama a Build() a mano.
    void OnEnable()
    {
        instance = this;
        Build();
    }

    void OnDisable()
    {
        if (instance == this) instance = null;
    }

    // El vacío dimensional sigue a la cámara: es el fondo de TODO el mundo, no solo de la base.
    // Sin esto, al viajar a la arena (x=1000) o al claro (y=1000) salían barras de color plano.
    void LateUpdate()
    {
        if (fondo == null) return;

        var camara = Camera.main;
        if (camara == null) return;

        Vector3 pos = camara.transform.position;
        fondo.transform.position = new Vector3(pos.x, pos.y, fondo.transform.position.z);
    }

    // Idempotente: se la puede llamar desde el editor las veces que haga falta mientras se
    // ajusta el encuadre, sin que se acumulen copias del decorado.
    public void Build()
    {
        fondo = Ensure("Backdrop_Void", backgroundSprite, backgroundSize, backgroundOrder, fondo);
        isla = Ensure("Backdrop_Island", islandSprite, islandSize, islandOrder, isla);

        if (extraLayers != null)
            foreach (var capa in extraLayers)
                if (capa != null && capa.sprite != null)
                    Ensure(capa.name, capa.sprite, capa.size, capa.order, null, capa.center);

        PublishWalkableArea();
    }

    private void PublishWalkableArea()
    {
        Vector2 centro = center + walkableOffset;
        Vector2 media = walkableSize * 0.5f;

        WalkableMin = centro - media;
        WalkableMax = centro + media;
        HasWalkableArea = walkableSize.x > 0f && walkableSize.y > 0f;
    }

    // Con Enter Play Mode Options los estáticos sobreviven al Stop; se limpian al arrancar para
    // que una partida nueva no herede los límites de la anterior antes de que Build() corra.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetWalkableArea() => HasWalkableArea = false;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center + walkableOffset, new Vector3(walkableSize.x, walkableSize.y, 0f));
    }

    private SpriteRenderer Ensure(string nombre, Sprite sprite, Vector2 size, int order, SpriteRenderer cache)
        => Ensure(nombre, sprite, size, order, cache, center);

    private SpriteRenderer Ensure(string nombre, Sprite sprite, Vector2 size, int order,
                                  SpriteRenderer cache, Vector2 donde)
    {
        var hijo = cache != null ? cache.transform : transform.Find(nombre);

        if (hijo == null)
        {
            if (sprite == null) return null;

            var go = new GameObject(nombre, typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            hijo = go.transform;
        }

        var sr = hijo.GetComponent<SpriteRenderer>();
        if (sr == null) return null;

        sr.sprite = sprite;
        sr.sortingLayerName = "Environment";
        sr.sortingOrder = order;

        hijo.position = donde;
        hijo.localScale = EscalaPara(sprite, size);
        return sr;
    }

    // Escala que lleva el sprite al tamaño pedido en unidades de mundo, sea cual sea su PPU.
    private static Vector3 EscalaPara(Sprite sprite, Vector2 size)
    {
        if (sprite == null) return Vector3.one;

        Vector2 propio = sprite.bounds.size;
        if (propio.x <= 0f || propio.y <= 0f) return Vector3.one;

        return new Vector3(size.x / propio.x, size.y / propio.y, 1f);
    }
}
