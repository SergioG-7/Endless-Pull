using System.Collections.Generic;
using UnityEngine;

// Claro de bosque donde se ve trabajar a la escuadra de recolección. Vive en su propio rincón
// del mundo, como la arena, y se construye por código igual que el resto del entorno.
// Antes los héroes enviados se desactivaban y desaparecían sin más.
public class ExpeditionMap : MonoBehaviour
{
    [Tooltip("Rincón del mundo donde vive el mapa. Va en vertical sobre la base a propósito: la arena está en (1000, 0) y el viaje de cámara pasaba por encima de ella.")]
    [SerializeField] private Vector2 mapCenter = new Vector2(0f, 1000f);

    [Tooltip("Terreno útil: los héroes pasean dentro y el decorado se reparte por ahí.")]
    [SerializeField] private Vector2 mapSize = new Vector2(24f, 16f);

    [Tooltip("Árboles del claro.")]
    [SerializeField] private int treeCount = 22;

    [Tooltip("Piedras sueltas del claro.")]
    [SerializeField] private int rockCount = 14;

    [Tooltip("Nodos de material a la vista a la vez.")]
    [SerializeField] private int nodeCount = 6;

    [Tooltip("Distancia a la que un héroe recoge un nodo.")]
    [SerializeField] private float gatherRadius = 0.9f;

    [Tooltip("Semilla del decorado; fija para que el claro sea siempre el mismo sitio.")]
    [SerializeField] private int scenerySeed = 20260905;

    [Tooltip("Zona de paseo de cada héroe dentro del claro; más pequeña que el mapa entero.")]
    [SerializeField] private Vector2 wanderSize = new Vector2(7f, 5f);

    private static ExpeditionMap instance;
    public static ExpeditionMap Instance
        => instance != null ? instance : (instance = Object.FindFirstObjectByType<ExpeditionMap>());

    public Vector2 Center => mapCenter;
    public Vector2 Size => mapSize;

    [Tooltip("Margen que el cerco deja por dentro del borde del claro.")]
    [SerializeField] private float wallInset = 1f;

    // Cerco del claro: nadie se sale de aquí, igual que las unidades no salen de la arena.
    public Vector2 WalkableMin => mapCenter - mapSize * 0.5f + Vector2.one * wallInset;
    public Vector2 WalkableMax => mapCenter + mapSize * 0.5f - Vector2.one * wallInset;

    // Está dentro del claro, con holgura de sobra para no soltar a quien se asome al borde.
    public bool Contains(Vector2 point)
    {
        Vector2 half = mapSize * 0.5f + Vector2.one * containsMargin;
        return Mathf.Abs(point.x - mapCenter.x) <= half.x
            && Mathf.Abs(point.y - mapCenter.y) <= half.y;
    }

    [Tooltip("Holgura alrededor del claro para seguir considerando que alguien está dentro.")]
    [SerializeField] private float containsMargin = 8f;

    // Héroes que están ahora mismo en el claro; se vacía al reclamar la expedición.
    private readonly List<HeroController> gatherers = new List<HeroController>();

    // Nodos de material vivos, con su renderer para poder recolocarlos al recogerse.
    private readonly List<SpriteRenderer> nodes = new List<SpriteRenderer>();
    private Transform sceneryRoot;

    void Awake()
    {
        if (instance == null) instance = this;
        BuildScenery();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // --- Entorno ---

    // Idempotente a propósito: se la puede llamar desde el editor para ver el claro sin entrar
    // en Play, y al arrancar no vuelve a plantar el bosque encima del que ya está.
    public void BuildScenery()
    {
        if (sceneryRoot != null) return;

        var yaHecho = transform.Find("ExpeditionScenery");
        if (yaHecho != null)
        {
            sceneryRoot = yaHecho;
            return;
        }

        var root = new GameObject("ExpeditionScenery");
        root.transform.SetParent(transform, false);
        root.transform.position = mapCenter;
        sceneryRoot = root.transform;

        // Semilla propia para no arrastrar el estado global de Random, que usa el gacha.
        var estadoPrevio = Random.state;
        Random.InitState(scenerySeed);

        Suelo();
        Cerco();
        for (int i = 0; i < treeCount; i++) Arbol(PuntoAlAzar(0.92f));
        for (int i = 0; i < rockCount; i++) Piedra(PuntoAlAzar(0.92f));

        Random.state = estadoPrevio;
    }

    // Centro de paseo que deja la zona entera dentro del cerco: si el héroe apunta fuera, el
    // clamp lo deja pegado al borde y parece que se haya atascado.
    private Vector2 CentroDePaseo()
    {
        Vector2 holgura = (mapSize * 0.5f) - (wanderSize * 0.5f) - Vector2.one * wallInset;
        holgura = Vector2.Max(holgura, Vector2.zero);

        return mapCenter + new Vector2(Random.Range(-holgura.x, holgura.x),
                                       Random.Range(-holgura.y, holgura.y));
    }

    private Vector2 PuntoAlAzar(float margen)
    {
        Vector2 half = mapSize * 0.5f * margen;
        return mapCenter + new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
    }

    private void Suelo()
        => Pieza("Env_ExpeditionGround", mapCenter, mapSize, new Color(0.20f, 0.28f, 0.20f), -20);

    // Marca en el suelo dónde para el cerco, igual que la valla de la arena: sin ella el héroe
    // se frena en un borde invisible.
    private void Cerco()
    {
        Vector2 min = WalkableMin;
        Vector2 max = WalkableMax;
        Vector2 centro = (min + max) * 0.5f;
        Vector2 medida = max - min;
        const float grosor = 0.18f;

        Pieza("Env_ClearingFenceN", new Vector2(centro.x, max.y), new Vector2(medida.x, grosor),
              new Color(0.15f, 0.20f, 0.15f), -19);
        Pieza("Env_ClearingFenceS", new Vector2(centro.x, min.y), new Vector2(medida.x, grosor),
              new Color(0.15f, 0.20f, 0.15f), -19);
        Pieza("Env_ClearingFenceE", new Vector2(max.x, centro.y), new Vector2(grosor, medida.y),
              new Color(0.15f, 0.20f, 0.15f), -19);
        Pieza("Env_ClearingFenceW", new Vector2(min.x, centro.y), new Vector2(grosor, medida.y),
              new Color(0.15f, 0.20f, 0.15f), -19);
    }

    // Copa y tronco: dos piezas, que es lo que hace que se lea como árbol y no como mancha.
    private void Arbol(Vector2 pos)
    {
        float escala = Random.Range(0.85f, 1.25f);

        // El tronco tiene que asomar por debajo de la copa (1,7 de alto centrada en pos) o el
        // árbol se queda en bola verde.
        Pieza("Env_Trunk", pos + Vector2.down * 0.95f * escala,
              new Vector2(0.34f, 1.1f) * escala, new Color(0.32f, 0.22f, 0.14f), -8);

        float verde = Random.Range(0.34f, 0.50f);
        Pieza("Env_Tree", pos, new Vector2(1.7f, 1.7f) * escala,
              new Color(0.13f, verde, 0.20f), -7, true);
    }

    private void Piedra(Vector2 pos)
    {
        float escala = Random.Range(0.6f, 1.1f);
        float gris = Random.Range(0.34f, 0.48f);

        Pieza("Env_Rock", pos, new Vector2(1.1f, 0.75f) * escala,
              new Color(gris, gris, gris + 0.03f), -9, true);
    }

    // El tamaño va en unidades de mundo, no en escala: los sprites de UI no miden una unidad,
    // así que fijar la escala a ojo daba manchas diminutas.
    private GameObject Pieza(string nombre, Vector2 pos, Vector2 tamano, Color color,
                             int orden, bool circulo = false)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(sceneryRoot, false);
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circulo ? UITheme.Circle : UITheme.Rounded;
        sr.color = color;
        sr.sortingOrder = orden;

        Vector2 propio = sr.sprite != null ? (Vector2)sr.sprite.bounds.size : Vector2.one;
        go.transform.localScale = new Vector3(
            propio.x > 0f ? tamano.x / propio.x : 1f,
            propio.y > 0f ? tamano.y / propio.y : 1f, 1f);

        return go;
    }

    // --- Recolección ---

    // La llama el ResourceExpeditionManager al salir la escuadra.
    public void Open(IEnumerable<HeroController> squad, ResourceExpeditionType type)
    {
        gatherers.Clear();

        foreach (var hero in squad)
        {
            if (hero == null) continue;

            hero.gameObject.SetActive(true);
            hero.EnterGathering(CentroDePaseo(), wanderSize);
            gatherers.Add(hero);
        }

        BuildNodes(type);
    }

    // Al reclamar la recompensa el claro se vacía; los héroes vuelven por su cuenta.
    public void Close()
    {
        gatherers.Clear();

        foreach (var node in nodes)
            if (node != null) Destroy(node.gameObject);

        nodes.Clear();
    }

    private void BuildNodes(ResourceExpeditionType type)
    {
        foreach (var node in nodes)
            if (node != null) Destroy(node.gameObject);

        nodes.Clear();

        Color color = NodeColor(type);
        for (int i = 0; i < nodeCount; i++)
        {
            var go = Pieza("Env_Node", PuntoAlAzar(0.85f), new Vector2(0.6f, 0.6f), color, -5, true);
            nodes.Add(go.GetComponent<SpriteRenderer>());
        }
    }

    private static Color NodeColor(ResourceExpeditionType type)
    {
        switch (type)
        {
            case ResourceExpeditionType.Forest: return new Color(0.55f, 0.40f, 0.20f);
            case ResourceExpeditionType.Mine: return new Color(0.62f, 0.66f, 0.72f);
            case ResourceExpeditionType.Hunt: return new Color(0.78f, 0.55f, 0.30f);
        }
        return UITheme.Accent;
    }

    void Update()
    {
        if (nodes.Count == 0 || gatherers.Count == 0) return;

        // Recoger es que el nodo salte a otro sitio cuando alguien le llega encima. No hace
        // falta un estado nuevo en el héroe: ya deambula, y así se lee igual de bien.
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node == null) continue;

            foreach (var hero in gatherers)
            {
                if (hero == null || !hero.gameObject.activeInHierarchy) continue;
                if (Vector2.Distance(hero.transform.position, node.transform.position) > gatherRadius) continue;

                DamageTextManager.Show(node.transform.position, "+", node.color);
                node.transform.position = PuntoAlAzar(0.85f);
                break;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 0.4f, 0.8f);
        Gizmos.DrawWireCube(mapCenter, mapSize);
    }
}
