using UnityEngine;

// Decorado del recinto: calles, plaza, farolas y arbolado para que la base se lea como una
// ciudad pequeña y no como edificios flotando sobre un rectángulo. Se construye por código con
// las primitivas de siempre, igual que el claro de recolección.
//
// Canon (design/lore/referencia-pick-me-up.md): el lobby tiene una Square central que hace de
// punto de encuentro, y los edificios se reparten alrededor.
public class BaseDecor : MonoBehaviour
{
    [Tooltip("Columnas de la rejilla de edificios; por ahí pasan las calles verticales.")]
    [SerializeField] private float[] columns = { -14.4f, -4.8f, 4.8f, 14.4f };

    [Tooltip("Filas de la rejilla de edificios; por ahí pasan las calles horizontales.")]
    [SerializeField] private float[] rows = { 6.8f, 1.4f, -4f, -9.4f, -14.8f };

    [Tooltip("Centro de la plaza; el hueco libre del medio del recinto.")]
    [SerializeField] private Vector2 squareCenter = new Vector2(0f, -4f);

    [Tooltip("Ancho de las calles.")]
    [SerializeField] private float streetWidth = 2.6f;

    [Tooltip("Recinto útil; las calles no se salen de aquí.")]
    [SerializeField] private Vector2 groundsMin = new Vector2(-19f, -19f);
    [SerializeField] private Vector2 groundsMax = new Vector2(19f, 12f);

    [Tooltip("Semilla del arbolado, para que la base sea siempre la misma.")]
    [SerializeField] private int seed = 424242;

    private static readonly Color Street = new Color(0.20f, 0.21f, 0.26f);
    private static readonly Color SquareStone = new Color(0.26f, 0.26f, 0.31f);
    private static readonly Color Fountain = new Color(0.24f, 0.40f, 0.52f);
    private static readonly Color LampPost = new Color(0.30f, 0.28f, 0.24f);
    private static readonly Color LampLight = new Color(0.95f, 0.82f, 0.45f);
    private static readonly Color Foliage = new Color(0.17f, 0.34f, 0.22f);
    private static readonly Color Trunk = new Color(0.30f, 0.21f, 0.14f);

    private Transform root;

    void Awake() => Build();

    // Idempotente: se la puede llamar desde el editor para ver la base sin entrar en Play, y al
    // arrancar no vuelve a construir el decorado encima del que ya está.
    public void Build()
    {
        if (root != null) return;

        var existente = transform.Find("BaseDecor");
        if (existente != null) { root = existente; return; }

        var go = new GameObject("BaseDecor");
        go.transform.SetParent(transform, false);
        root = go.transform;

        var estadoPrevio = Random.state;
        Random.InitState(seed);

        Calles();
        Plaza();
        Farolas();
        Arbolado();

        Random.state = estadoPrevio;
    }

    // Rejilla de calles: una por cada fila y columna de edificios, así que todo edificio da a
    // una. Van por debajo de todo salvo el suelo.
    private void Calles()
    {
        float ancho = groundsMax.x - groundsMin.x;
        float alto = groundsMax.y - groundsMin.y;
        float centroX = (groundsMin.x + groundsMax.x) * 0.5f;
        float centroY = (groundsMin.y + groundsMax.y) * 0.5f;

        foreach (float fila in rows)
            Pieza("Decor_StreetH", new Vector2(centroX, fila),
                  new Vector2(ancho, streetWidth), Street, -8);

        foreach (float columna in columns)
            Pieza("Decor_StreetV", new Vector2(columna, centroY),
                  new Vector2(streetWidth, alto), Street, -8);
    }

    // Plaza central con su fuente: el punto de encuentro del canon. Sin rótulo a propósito —
    // se lee por lo que es, no por una etiqueta encima.
    private void Plaza()
    {
        Pieza("Decor_Square", squareCenter, new Vector2(8.4f, 6.4f), SquareStone, -7, true);
        Pieza("Decor_FountainRim", squareCenter, new Vector2(2.6f, 2.2f),
              new Color(0.34f, 0.34f, 0.38f), -6, true);
        Pieza("Decor_FountainWater", squareCenter, new Vector2(1.8f, 1.5f), Fountain, -5, true);
    }

    // Una farola en cada cruce de calles: es lo que más ata la rejilla y la hace leer como calles
    // y no como franjas de color.
    private void Farolas()
    {
        foreach (float columna in columns)
        {
            foreach (float fila in rows)
            {
                // En las esquinas del cruce, no en medio: si no, estorban el paso de los héroes.
                Vector2 pos = new Vector2(columna + streetWidth * 0.75f, fila + streetWidth * 0.75f);
                if (pos.x > groundsMax.x || pos.y > groundsMax.y) continue;

                Pieza("Decor_LampPost", pos + Vector2.down * 0.32f,
                      new Vector2(0.16f, 0.9f), LampPost, -4);
                Pieza("Decor_LampLight", pos + Vector2.up * 0.18f,
                      new Vector2(0.42f, 0.42f), LampLight, -3, true);
            }
        }
    }

    // Arbolado del perímetro: rompe el borde recto del recinto sin meterse entre los edificios.
    private void Arbolado()
    {
        const int porLado = 9;

        for (int i = 0; i < porLado; i++)
        {
            float t = (i + 0.5f) / porLado;
            float x = Mathf.Lerp(groundsMin.x, groundsMax.x, t);

            Arbol(new Vector2(x + Random.Range(-0.5f, 0.5f), groundsMax.y + Random.Range(0f, 1.4f)));
            Arbol(new Vector2(x + Random.Range(-0.5f, 0.5f), groundsMin.y - Random.Range(0f, 1.4f)));
        }

        for (int i = 0; i < porLado; i++)
        {
            float t = (i + 0.5f) / porLado;
            float y = Mathf.Lerp(groundsMin.y, groundsMax.y, t);

            Arbol(new Vector2(groundsMin.x - Random.Range(0f, 1.4f), y + Random.Range(-0.5f, 0.5f)));
            Arbol(new Vector2(groundsMax.x + Random.Range(0f, 1.4f), y + Random.Range(-0.5f, 0.5f)));
        }
    }

    private void Arbol(Vector2 pos)
    {
        float escala = Random.Range(0.8f, 1.2f);

        Pieza("Decor_Trunk", pos + Vector2.down * 0.8f * escala,
              new Vector2(0.3f, 0.95f) * escala, Trunk, -4);

        float verde = Random.Range(0.30f, 0.42f);
        Pieza("Decor_Foliage", pos, new Vector2(1.5f, 1.5f) * escala,
              new Color(Foliage.r, verde, Foliage.b), -3, true);
    }

    // El tamaño va en unidades de mundo: los sprites de UI no miden una unidad, así que fijar la
    // escala a ojo deja el decorado en manchas diminutas.
    private void Pieza(string nombre, Vector2 pos, Vector2 tamano, Color color,
                       int orden, bool circulo = false)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(root, false);
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circulo ? UITheme.Circle : UITheme.Rounded;
        sr.color = color;

        // Misma capa que el resto del mundo; en 'Default' quedaba detrás del suelo pasara lo
        // que pasara. Los órdenes caen entre el suelo base (-10) y los edificios (0).
        sr.sortingLayerName = "Environment";
        sr.sortingOrder = orden;

        Vector2 propio = sr.sprite != null ? (Vector2)sr.sprite.bounds.size : Vector2.one;
        go.transform.localScale = new Vector3(
            propio.x > 0f ? tamano.x / propio.x : 1f,
            propio.y > 0f ? tamano.y / propio.y : 1f, 1f);
    }
}
