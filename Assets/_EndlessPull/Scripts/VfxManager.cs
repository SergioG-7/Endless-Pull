using System.Collections.Generic;
using UnityEngine;

// Identificador de cada efecto de partículas pooled del juego.
public enum VfxId
{
    Heal,
    DecreeCast,
    CraftSuccess,
    VictoryChest,
    Impact,
    Death,
    LevelUp,
    Awaken
}

// Partículas puntuales sin sprite propio: reserva por tipo, nada de Instantiate/Destroy por golpe
// (mismo criterio que AudioManager/DamageTextManager). Presupuesto móvil: ráfaga única y corta de
// 12-20 partículas, máx. 3 copias solapadas por tipo (ver maxOverlapPerType).
public class VfxManager : MonoBehaviour
{
    [Tooltip("Copias máximas por tipo de efecto que se pueden solapar a la vez.")]
    [Range(1, 6)]
    [SerializeField] private int maxOverlapPerType = 3;

    private static VfxManager instance;
    private static Material sharedMaterial;

    private readonly Dictionary<VfxId, List<ParticleSystem>> pools = new Dictionary<VfxId, List<ParticleSystem>>();

    void Awake() => instance = this;

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Punto de entrada estático: combate, decretos, taller y cofres lo llaman sin referencia.
    public static void Play(VfxId id, Vector3 worldPosition)
    {
        Instance().PlayInternal(id, worldPosition);
    }

    // Si nadie lo puso en la escena se crea solo. Faltaba en Base.unity y los efectos no salían
    // por ningún lado, sin un mísero aviso: la llamada se perdía en silencio.
    private static VfxManager Instance()
    {
        if (instance != null) return instance;

        var existente = FindFirstObjectByType<VfxManager>();
        if (existente != null) { instance = existente; return instance; }

        instance = new GameObject("VfxManager").AddComponent<VfxManager>();
        return instance;
    }

    private void PlayInternal(VfxId id, Vector3 worldPosition)
    {
        var ps = GetFromPool(id);
        if (ps == null) return;

        ps.transform.position = worldPosition;
        ps.Play(true);
    }

    // Busca uno libre en la reserva del tipo; si todos suenan y no se llegó al tope, crea uno nuevo.
    private ParticleSystem GetFromPool(VfxId id)
    {
        if (!pools.TryGetValue(id, out var lista))
        {
            lista = new List<ParticleSystem>();
            pools[id] = lista;
        }

        foreach (var ps in lista)
            if (ps != null && !ps.isPlaying) return ps;

        if (lista.Count >= maxOverlapPerType) return lista[0];

        var creado = Build(id);
        lista.Add(creado);
        return creado;
    }

    private ParticleSystem Build(VfxId id)
    {
        var go = new GameObject($"Vfx_{id}");
        go.transform.SetParent(transform, false);

        var ps = go.AddComponent<ParticleSystem>();
        Configure(ps, id);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = SharedMaterial();

        // Sin esto se quedan en Default, que dibuja por detrás del fondo y no se veía nada.
        renderer.sortingLayerID = SortingLayer.NameToID("Characters");
        renderer.sortingOrder = 100;

        return ps;
    }

    // Un único material Unlit de partículas de URP, compartido por los cuatro efectos (sin sprites propios).
    private static Material SharedMaterial()
    {
        if (sharedMaterial != null) return sharedMaterial;

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        sharedMaterial = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));

        // Con la textura blanca de Unity cada partícula era un cuadrado duro; con el disco
        // degradado son chispas redondas que se apagan hacia el borde.
        var textura = SparkTexture();
        sharedMaterial.mainTexture = textura;
        if (sharedMaterial.HasProperty("_BaseMap")) sharedMaterial.SetTexture("_BaseMap", textura);

        // El material de URP nace opaco: sin esto el alfa se ignora y cada chispa es un
        // cuadrado duro. Mezcla por alfa, no aditiva: la aditiva quemaba el color a blanco.
        sharedMaterial.SetFloat("_Surface", 1f);
        sharedMaterial.SetFloat("_Blend", 0f);
        sharedMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        sharedMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        sharedMaterial.SetFloat("_ZWrite", 0f);
        sharedMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        sharedMaterial.DisableKeyword("_ALPHATEST_ON");
        sharedMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return sharedMaterial;
    }

    private static Texture2D sparkTexture;

    // Disco blanco con el alfa cayendo del centro al borde, generado a mano para no depender
    // de ningún asset importado.
    private static Texture2D SparkTexture()
    {
        if (sparkTexture != null) return sparkTexture;

        const int size = 32;
        sparkTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        var pixels = new Color[size * size];
        float centro = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distancia = new Vector2(x - centro, y - centro).magnitude / centro;
                float alpha = Mathf.Clamp01(1f - distancia);

                // Al cuadrado: el núcleo queda sólido y el halo se difumina rápido.
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
            }
        }

        sparkTexture.SetPixels(pixels);
        sparkTexture.Apply();
        return sparkTexture;
    }

    // Forma, color y ritmo de cada efecto; ráfaga única y corta, sin loop, pensada para el presupuesto móvil.
    private void Configure(ParticleSystem ps, VfxId id)
    {
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 1.2f;
        main.startSize = 0.28f;
        main.startLifetime = 0.6f;
        main.maxParticles = 24;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        int burstCount;
        Color color;

        switch (id)
        {
            case VfxId.Heal:
                burstCount = 12;
                color = new Color(0.4f, 0.95f, 0.5f);
                main.startSpeed = 0.8f;
                main.gravityModifier = -0.4f; // las chispas de curación suben en vez de caer
                break;

            case VfxId.DecreeCast:
                burstCount = 16;
                color = new Color(1f, 0.85f, 0.3f);
                main.startSpeed = 1.8f;
                shape.radius = 0.1f;
                break;

            case VfxId.Impact:
                // Chispazo seco en el punto de contacto: corto y pequeño, suena en cada golpe fuerte.
                burstCount = 8;
                color = new Color(1f, 0.95f, 0.75f);
                main.startSpeed = 2.6f;
                main.startSize = 0.22f;
                main.startLifetime = 0.3f;
                shape.radius = 0.12f;
                break;

            case VfxId.Death:
                // Estallido oscuro y hacia arriba de quien acaba de caer.
                burstCount = 18;
                color = new Color(0.85f, 0.25f, 0.3f);
                main.startSpeed = 1.6f;
                main.startLifetime = 0.75f;
                main.gravityModifier = 0.6f;
                shape.radius = 0.25f;
                break;

            case VfxId.LevelUp:
                // Columna dorada que sube: subida de nivel y ascensión.
                burstCount = 16;
                color = new Color(1f, 0.88f, 0.35f);
                main.startSpeed = 2f;
                main.startLifetime = 0.9f;
                main.gravityModifier = -0.7f;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 12f;
                shape.radius = 0.35f;
                break;

            case VfxId.Awaken:
                // Destello violeta al despertar una habilidad; pasa poco y se tiene que notar.
                burstCount = 22;
                color = new Color(0.72f, 0.45f, 1f);
                main.startSpeed = 2.4f;
                main.startLifetime = 0.85f;
                main.startSize = 0.32f;
                shape.radius = 0.2f;
                break;

            case VfxId.CraftSuccess:
                burstCount = 14;
                color = new Color(1f, 0.6f, 0.2f);
                main.startSpeed = 1.4f;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 25f;
                break;

            default: // VictoryChest
                burstCount = 20;
                color = new Color(1f, 0.92f, 0.4f);
                main.startSpeed = 2.2f;
                main.startLifetime = 0.9f;
                break;
        }

        main.startColor = color;

        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });
    }
}
