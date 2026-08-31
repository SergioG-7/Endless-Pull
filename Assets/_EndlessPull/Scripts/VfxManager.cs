using System.Collections.Generic;
using UnityEngine;

// Identificador de cada efecto de partículas pooled del juego.
public enum VfxId
{
    Heal,
    DecreeCast,
    CraftSuccess,
    VictoryChest
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
        if (instance != null) instance.PlayInternal(id, worldPosition);
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

        return ps;
    }

    // Un único material Unlit de partículas de URP, compartido por los cuatro efectos (sin sprites propios).
    private static Material SharedMaterial()
    {
        if (sharedMaterial != null) return sharedMaterial;

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        sharedMaterial = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
        return sharedMaterial;
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
        main.startSize = 0.18f;
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
