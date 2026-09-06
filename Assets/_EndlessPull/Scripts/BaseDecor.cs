using UnityEngine;

// Decorado del recinto: suelo, calles, plaza, farolas y arbolado para que la base se lea como una
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

    [Tooltip("Cuántas manchas de terreno se siembran; a más, más textura y más SpriteRenderers.")]
    [SerializeField] private int groundPatches = 210;

    [Tooltip("Adoquines sueltos por calle; solo textura, no cambian el tránsito.")]
    [SerializeField] private int cobblesPerStreet = 22;

    // Suelo: dos verdes de tierra pisada para romper el plano de color.
    private static readonly Color SoilDark = new Color(0.098f, 0.118f, 0.100f);
    private static readonly Color SoilLight = new Color(0.168f, 0.198f, 0.148f);
    private static readonly Color GrassTuft = new Color(0.165f, 0.235f, 0.150f);

    // Calzada: losa de piedra clara sobre la tierra oscura, con la junta MÁS OSCURA que la
    // losa. Al revés (lecho oscuro y bordillo claro) la calle desaparecía contra el terreno.
    private static readonly Color RoadBed = new Color(0.245f, 0.248f, 0.262f);
    private static readonly Color Kerb = new Color(0.150f, 0.152f, 0.168f);
    private static readonly Color Cobble = new Color(0.288f, 0.292f, 0.308f);

    private static readonly Color SquareStone = new Color(0.300f, 0.295f, 0.310f);
    private static readonly Color SquareInlay = new Color(0.365f, 0.355f, 0.360f);
    private static readonly Color FountainRim = new Color(0.420f, 0.415f, 0.420f);
    private static readonly Color Fountain = new Color(0.200f, 0.420f, 0.545f);
    private static readonly Color FountainGlint = new Color(0.560f, 0.760f, 0.840f);

    private static readonly Color LampPost = new Color(0.235f, 0.215f, 0.190f);
    private static readonly Color LampLight = new Color(0.98f, 0.86f, 0.52f);
    private static readonly Color LampGlow = new Color(0.98f, 0.84f, 0.48f, 0.038f);

    private static readonly Color Trunk = new Color(0.245f, 0.170f, 0.115f);
    private static readonly Color Shadow = new Color(0f, 0f, 0f, 0.28f);
    private static readonly Color Rock = new Color(0.250f, 0.255f, 0.265f);
    private static readonly Color Flower = new Color(0.780f, 0.420f, 0.470f);

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

        Terreno();
        Calles();
        Plaza();
        Farolas();
        Arbolado();
        Detalle();

        Random.state = estadoPrevio;
    }

    // Manchas de tierra y hierba sobre el suelo base: es lo que quita la sensación de fondo
    // plano tintado. Van por debajo de todo lo demás salvo el propio suelo.
    private void Terreno()
    {
        for (int i = 0; i < groundPatches; i++)
        {
            Vector2 pos = PuntoDelRecinto(1.5f);
            float tamano = Random.Range(1.1f, 3.2f);
            Color color = Random.value < 0.55f ? SoilDark : SoilLight;

            Pieza("Decor_SoilPatch", pos, new Vector2(tamano, tamano * Random.Range(0.55f, 0.85f)),
                  color, -9, true);
        }

        // Matas de hierba pequeñas y más saturadas, para que el verde no salga solo del arbolado.
        for (int i = 0; i < groundPatches / 3; i++)
        {
            Vector2 pos = PuntoDelRecinto(1f);
            float tamano = Random.Range(0.5f, 1.3f);
            Pieza("Decor_GrassTuft", pos, new Vector2(tamano, tamano * 0.7f), GrassTuft, -9, true);
        }
    }

    // Rejilla de calles: una por cada fila y columna de edificios, así que todo edificio da a
    // una. Cada calle es lecho + dos bordillos + adoquines sueltos.
    private void Calles()
    {
        float ancho = groundsMax.x - groundsMin.x;
        float alto = groundsMax.y - groundsMin.y;
        float centroX = (groundsMin.x + groundsMax.x) * 0.5f;
        float centroY = (groundsMin.y + groundsMax.y) * 0.5f;
        float bordillo = 0.22f;

        foreach (float fila in rows)
        {
            Pieza("Decor_StreetH", new Vector2(centroX, fila), new Vector2(ancho, streetWidth), RoadBed, -8);
            Pieza("Decor_KerbH", new Vector2(centroX, fila + streetWidth * 0.5f),
                  new Vector2(ancho, bordillo), Kerb, -7);
            Pieza("Decor_KerbH", new Vector2(centroX, fila - streetWidth * 0.5f),
                  new Vector2(ancho, bordillo), Kerb, -7);

            for (int i = 0; i < cobblesPerStreet; i++)
                Adoquin(new Vector2(Random.Range(groundsMin.x, groundsMax.x),
                                    fila + Random.Range(-streetWidth * 0.35f, streetWidth * 0.35f)));
        }

        foreach (float columna in columns)
        {
            Pieza("Decor_StreetV", new Vector2(columna, centroY), new Vector2(streetWidth, alto), RoadBed, -8);
            Pieza("Decor_KerbV", new Vector2(columna + streetWidth * 0.5f, centroY),
                  new Vector2(bordillo, alto), Kerb, -7);
            Pieza("Decor_KerbV", new Vector2(columna - streetWidth * 0.5f, centroY),
                  new Vector2(bordillo, alto), Kerb, -7);

            for (int i = 0; i < cobblesPerStreet; i++)
                Adoquin(new Vector2(columna + Random.Range(-streetWidth * 0.35f, streetWidth * 0.35f),
                                    Random.Range(groundsMin.y, groundsMax.y)));
        }
    }

    private void Adoquin(Vector2 pos)
    {
        float lado = Random.Range(0.28f, 0.52f);
        Pieza("Decor_Cobble", pos, new Vector2(lado, lado * Random.Range(0.7f, 1f)), Cobble, -7);
    }

    // Plaza central con su fuente: el punto de encuentro del canon. Sin rótulo a propósito —
    // se lee por lo que es, no por una etiqueta encima. El empedrado va por anillos y radios.
    private void Plaza()
    {
        Pieza("Decor_Square", squareCenter, new Vector2(9.6f, 7.4f), SquareStone, -6, true);
        Anillo("Decor_SquareRing", squareCenter, new Vector2(8.2f, 6.2f), SquareInlay, -6);
        Anillo("Decor_SquareRingInner", squareCenter, new Vector2(5.4f, 4.2f), SquareInlay, -6);

        // Radios del empedrado: ocho juntas que salen del centro hacia el borde de la plaza.
        for (int i = 0; i < 8; i++)
        {
            float angulo = i * Mathf.PI * 0.25f;
            var dir = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo) * 0.76f);

            var pieza = Pieza("Decor_SquareJoint", squareCenter + dir * 3.4f,
                              new Vector2(2.6f, 0.10f), SquareInlay, -6);
            pieza.transform.rotation = Quaternion.Euler(0f, 0f, angulo * Mathf.Rad2Deg);
        }

        Pieza("Decor_FountainRim", squareCenter, new Vector2(3.0f, 2.5f), FountainRim, -5, true);
        Pieza("Decor_FountainWater", squareCenter, new Vector2(2.2f, 1.8f), Fountain, -5, true);
        Pieza("Decor_FountainGlint", squareCenter + new Vector2(-0.45f, 0.34f),
              new Vector2(0.7f, 0.42f), FountainGlint, -4, true);
    }

    // Una farola en cada cruce de calles: es lo que más ata la rejilla y la hace leer como calles
    // y no como franjas de color. El charco de luz es lo que las hace parecer encendidas.
    private void Farolas()
    {
        foreach (float columna in columns)
        {
            foreach (float fila in rows)
            {
                // En las esquinas del cruce, no en medio: si no, estorban el paso de los héroes.
                Vector2 pos = new Vector2(columna + streetWidth * 0.75f, fila + streetWidth * 0.75f);
                if (pos.x > groundsMax.x || pos.y > groundsMax.y) continue;

                Pieza("Decor_LampGlow", pos + Vector2.down * 0.55f, new Vector2(2.3f, 1.6f), LampGlow, -6, true);
                Pieza("Decor_LampShadow", pos + Vector2.down * 0.86f, new Vector2(0.7f, 0.24f), Shadow, -4, true);
                Pieza("Decor_LampPost", pos + Vector2.down * 0.32f, new Vector2(0.16f, 0.9f), LampPost, -3);
                Pieza("Decor_LampHead", pos + Vector2.up * 0.18f, new Vector2(0.42f, 0.42f), LampLight, -2, true);
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

    // Un árbol es sombra + tronco + tres masas de copa solapadas de verdes distintos. Con una
    // sola bola verde se leía como una chincheta; con tres, como una copa.
    private void Arbol(Vector2 pos)
    {
        float escala = Random.Range(0.85f, 1.35f);
        bool arbusto = Random.value < 0.22f;

        Pieza("Decor_TreeShadow", pos + Vector2.down * (arbusto ? 0.5f : 1.15f) * escala,
              new Vector2(1.6f, 0.5f) * escala, Shadow, -4, true);

        if (!arbusto)
            Pieza("Decor_Trunk", pos + Vector2.down * 0.8f * escala,
                  new Vector2(0.3f, 0.95f) * escala, Trunk, -3);

        // La copa se aclara hacia arriba: es lo que da volumen sin necesitar sprite propio.
        float tono = Random.Range(-0.03f, 0.03f);
        Masa(pos + new Vector2(-0.42f, -0.18f) * escala, 1.28f * escala, Verde(0.255f + tono));
        Masa(pos + new Vector2(0.44f, -0.10f) * escala, 1.20f * escala, Verde(0.290f + tono));
        Masa(pos + new Vector2(0.02f, 0.34f) * escala, 1.42f * escala, Verde(0.355f + tono));
    }

    private void Masa(Vector2 pos, float tamano, Color color)
        => Pieza("Decor_Foliage", pos, new Vector2(tamano, tamano * 0.92f), color, -2, true);

    private static Color Verde(float verde) => new Color(verde * 0.48f, verde, verde * 0.42f);

    // Maleza y piedras sueltas en los huecos entre edificios: es el detalle que hace que el
    // recinto no parezca una cuadrícula vacía entre calle y calle.
    private void Detalle()
    {
        for (int i = 0; i < 46; i++)
        {
            Vector2 pos = PuntoDelRecinto(1.2f);
            if (EstaEnCalle(pos) || EnLaPlaza(pos)) continue;

            float rodada = Random.value;
            if (rodada < 0.45f)
            {
                float tamano = Random.Range(0.55f, 1.05f);
                Pieza("Decor_Bush", pos, new Vector2(tamano, tamano * 0.8f),
                      Verde(Random.Range(0.20f, 0.27f)), -2, true);
            }
            else if (rodada < 0.78f)
            {
                float tamano = Random.Range(0.30f, 0.62f);
                Pieza("Decor_Rock", pos, new Vector2(tamano, tamano * 0.72f), Rock, -3, true);
            }
            else
            {
                for (int f = 0; f < 3; f++)
                    Pieza("Decor_Flower", pos + Random.insideUnitCircle * 0.45f,
                          new Vector2(0.17f, 0.17f), Flower, -2, true);
            }
        }
    }

    // Punto al azar dentro del recinto, con un margen para no pegarse a la valla.
    private Vector2 PuntoDelRecinto(float margen) => new Vector2(
        Random.Range(groundsMin.x + margen, groundsMax.x - margen),
        Random.Range(groundsMin.y + margen, groundsMax.y - margen));

    private bool EstaEnCalle(Vector2 pos)
    {
        float mitad = streetWidth * 0.5f + 0.5f;

        foreach (float fila in rows) if (Mathf.Abs(pos.y - fila) < mitad) return true;
        foreach (float columna in columns) if (Mathf.Abs(pos.x - columna) < mitad) return true;

        return false;
    }

    private bool EnLaPlaza(Vector2 pos)
        => Mathf.Abs(pos.x - squareCenter.x) < 5.6f && Mathf.Abs(pos.y - squareCenter.y) < 4.4f;

    private GameObject Anillo(string nombre, Vector2 pos, Vector2 tamano, Color color, int orden)
    {
        var go = Pieza(nombre, pos, tamano, color, orden);
        go.GetComponent<SpriteRenderer>().sprite = UITheme.CircleRing;
        Escalar(go, tamano);
        return go;
    }

    // El tamaño va en unidades de mundo: los sprites de UI no miden una unidad, así que fijar la
    // escala a ojo deja el decorado en manchas diminutas.
    private GameObject Pieza(string nombre, Vector2 pos, Vector2 tamano, Color color,
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

        Escalar(go, tamano);
        return go;
    }

    private static void Escalar(GameObject go, Vector2 tamano)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        Vector2 propio = sr.sprite != null ? (Vector2)sr.sprite.bounds.size : Vector2.one;

        go.transform.localScale = new Vector3(
            propio.x > 0f ? tamano.x / propio.x : 1f,
            propio.y > 0f ? tamano.y / propio.y : 1f, 1f);
    }
}
