using System.Collections.Generic;
using UnityEngine;

// Cada efecto del juego; el canal se deduce del propio identificador.
public enum SfxId
{
    UiClick,
    UiOpen,
    UiClose,
    CardReveal,
    MeleeHit,
    ArrowShot,
    DecreeHeal,
    DecreeRegroup,
    Impact,
    Defeat
}

public enum AudioChannel
{
    UI,
    Combat
}

// Gestor de sonido con reserva de AudioSources. Si no hay clips importados sintetiza los
// suyos por código, así el juego suena desde el primer día y nunca hay referencias nulas.
public class AudioManager : MonoBehaviour
{
    // Un clip por identificador; se rellena desde el Inspector cuando haya audio de verdad.
    [System.Serializable]
    public class SfxEntry
    {
        [Tooltip("Efecto al que corresponde el clip.")]
        public SfxId id;

        [Tooltip("Clip importado; vacío usa el sintético de reserva.")]
        public AudioClip clip;
    }

    [Tooltip("Clips importados que sustituyen a los sintéticos.")]
    [SerializeField] private List<SfxEntry> clips = new List<SfxEntry>();

    [Tooltip("AudioSources de la reserva; marca el máximo de sonidos a la vez.")]
    [Range(4, 32)]
    [SerializeField] private int poolSize = 12;

    [Tooltip("Volumen del canal de interfaz.")]
    [Range(0f, 1f)]
    [SerializeField] private float uiVolume = 0.55f;

    [Tooltip("Volumen del canal de combate.")]
    [Range(0f, 1f)]
    [SerializeField] private float combatVolume = 0.7f;

    [Tooltip("Silencia todo sin tener que tocar los dos volúmenes.")]
    [SerializeField] private bool muted;

    [Tooltip("Segundos mínimos entre dos disparos del mismo efecto; corta el zumbido.")]
    [SerializeField] private float minInterval = 0.04f;

    [Tooltip("Frecuencia de muestreo de los clips sintéticos.")]
    [SerializeField] private int sampleRate = 44100;

    private static AudioManager instance;

    private readonly List<AudioSource> pool = new List<AudioSource>();
    private readonly Dictionary<SfxId, AudioClip> resolved = new Dictionary<SfxId, AudioClip>();
    private readonly Dictionary<SfxId, float> lastPlayed = new Dictionary<SfxId, float>();

    private int nextSource;

    public bool Muted => muted;
    public static bool IsMuted => instance != null && instance.muted;

    void Awake()
    {
        // Si ya había uno, este sobra: dos reservas competirían por los mismos sonidos.
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        BuildPool();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Start()
    {
        HookSceneButtons();
    }

    // Los botones montados en la escena no pasan por UIBuild: se les engancha el clic aquí.
    private void HookSceneButtons()
    {
        foreach (var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button == null) continue;

            var boton = button;
            boton.onClick.AddListener(() => Play(SfxId.UiClick));
        }
    }

    private void BuildPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"SfxSource_{i}");
            go.transform.SetParent(transform, false);

            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;

            // 2D por defecto: los efectos de interfaz no deben venir de ningún sitio.
            source.spatialBlend = 0f;
            pool.Add(source);
        }
    }

    // El primero libre; si todos suenan se reutiliza el siguiente en la rueda.
    private AudioSource Next()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            var source = pool[(nextSource + i) % pool.Count];
            if (source != null && !source.isPlaying)
            {
                nextSource = (nextSource + i + 1) % pool.Count;
                return source;
            }
        }

        var reciclado = pool[nextSource];
        nextSource = (nextSource + 1) % pool.Count;
        return reciclado;
    }

    public static AudioChannel ChannelOf(SfxId id)
        => id == SfxId.UiClick || id == SfxId.UiOpen || id == SfxId.UiClose || id == SfxId.CardReveal
           ? AudioChannel.UI
           : AudioChannel.Combat;

    // Punto de entrada estático: nadie necesita una referencia al gestor.
    public static void Play(SfxId id)
    {
        if (instance != null) instance.PlayInternal(id, Vector3.zero, false);
    }

    // Igual pero situado en el mundo; lo usan los golpes y los impactos.
    public static void PlayAt(SfxId id, Vector3 worldPosition)
    {
        if (instance != null) instance.PlayInternal(id, worldPosition, true);
    }

    public static void SetMuted(bool value)
    {
        if (instance != null) instance.muted = value;
    }

    public static bool ToggleMute()
    {
        if (instance == null) return false;

        instance.muted = !instance.muted;
        return instance.muted;
    }

    private void PlayInternal(SfxId id, Vector3 worldPosition, bool positional)
    {
        if (muted) return;

        // Dos golpes en el mismo frame suenan como uno roto: se deja pasar solo el primero.
        if (lastPlayed.TryGetValue(id, out float ultimo)
            && Time.unscaledTime - ultimo < minInterval) return;

        lastPlayed[id] = Time.unscaledTime;

        var clip = ClipFor(id);
        if (clip == null) return;

        var source = Next();
        if (source == null) return;

        source.clip = clip;
        source.volume = ChannelOf(id) == AudioChannel.UI ? uiVolume : combatVolume;
        source.spatialBlend = 0f;
        source.transform.position = positional ? worldPosition : transform.position;
        source.Play();
    }

    // Primero el clip importado; si no hay, el sintético, que se cachea al primer uso.
    private AudioClip ClipFor(SfxId id)
    {
        if (resolved.TryGetValue(id, out var cacheado)) return cacheado;

        AudioClip clip = null;
        foreach (var entry in clips)
            if (entry != null && entry.id == id && entry.clip != null) { clip = entry.clip; break; }

        if (clip == null) clip = Synthesize(id);

        resolved[id] = clip;
        return clip;
    }

    // Receta de cada efecto: forma de onda, tono, duración y cuánto ruido lleva encima.
    private AudioClip Synthesize(SfxId id)
    {
        float duracion;
        float desde;
        float hasta;
        float ruido;
        float decaimiento;
        bool cuadrada;

        switch (id)
        {
            case SfxId.UiClick:
                duracion = 0.06f; desde = 1200f; hasta = 900f; ruido = 0.05f; decaimiento = 40f; cuadrada = true;
                break;
            case SfxId.UiOpen:
                duracion = 0.18f; desde = 420f; hasta = 880f; ruido = 0f; decaimiento = 10f; cuadrada = false;
                break;
            case SfxId.UiClose:
                duracion = 0.18f; desde = 880f; hasta = 380f; ruido = 0f; decaimiento = 10f; cuadrada = false;
                break;
            case SfxId.CardReveal:
                duracion = 0.35f; desde = 660f; hasta = 1760f; ruido = 0.03f; decaimiento = 6f; cuadrada = false;
                break;
            case SfxId.MeleeHit:
                duracion = 0.14f; desde = 220f; hasta = 90f; ruido = 0.55f; decaimiento = 28f; cuadrada = true;
                break;
            case SfxId.ArrowShot:
                duracion = 0.16f; desde = 1500f; hasta = 500f; ruido = 0.35f; decaimiento = 22f; cuadrada = false;
                break;
            case SfxId.DecreeHeal:
                duracion = 0.45f; desde = 520f; hasta = 1040f; ruido = 0f; decaimiento = 5f; cuadrada = false;
                break;
            case SfxId.DecreeRegroup:
                duracion = 0.40f; desde = 300f; hasta = 600f; ruido = 0.02f; decaimiento = 6f; cuadrada = true;
                break;
            case SfxId.Impact:
                duracion = 0.22f; desde = 320f; hasta = 70f; ruido = 0.7f; decaimiento = 18f; cuadrada = true;
                break;
            default: // Defeat
                duracion = 0.6f; desde = 400f; hasta = 70f; ruido = 0.25f; decaimiento = 6f; cuadrada = false;
                break;
        }

        return Render(id.ToString(), duracion, desde, hasta, ruido, decaimiento, cuadrada);
    }

    // Barrido de tono con envolvente exponencial; el ruido le da cuerpo a los golpes.
    private AudioClip Render(string nombre, float duracion, float desde, float hasta,
                             float ruido, float decaimiento, bool cuadrada)
    {
        int muestras = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duracion));
        var datos = new float[muestras];

        var azar = new System.Random(nombre.GetHashCode());
        float fase = 0f;

        for (int i = 0; i < muestras; i++)
        {
            float t = (float)i / muestras;
            float frecuencia = Mathf.Lerp(desde, hasta, t);

            fase += 2f * Mathf.PI * frecuencia / sampleRate;
            if (fase > 2f * Mathf.PI) fase -= 2f * Mathf.PI;

            float onda = cuadrada ? (Mathf.Sin(fase) >= 0f ? 0.7f : -0.7f) : Mathf.Sin(fase);
            if (ruido > 0f) onda += ((float)azar.NextDouble() * 2f - 1f) * ruido;

            // Ataque muy corto para que no chasquee y caída exponencial hasta el silencio.
            float ataque = Mathf.Min(1f, i / (sampleRate * 0.004f));
            datos[i] = Mathf.Clamp(onda * ataque * Mathf.Exp(-decaimiento * t) * 0.6f, -1f, 1f);
        }

        var clip = AudioClip.Create("Sfx_" + nombre, muestras, 1, sampleRate, false);
        clip.SetData(datos, 0);
        return clip;
    }
}
