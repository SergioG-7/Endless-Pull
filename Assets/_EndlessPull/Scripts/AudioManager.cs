using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Cada efecto del juego; el canal se deduce del propio identificador.
public enum SfxId
{
    UiClick,
    UiOpen,
    UiClose,
    CardReveal,
    MeleeHit,
    ArrowShot,
    MagicBolt,
    DecreeHeal,
    DecreeRegroup,
    Impact,
    Defeat,
    Victory,
    DecreeFocusFire,
    DecreeRetreat,
    Critical,
    Ascension,
    CraftSuccess,
    Error,
    Potion,
    Reward,
    HeroHurt
}

public enum AudioChannel
{
    UI,
    Combat
}

// Grupo de mezcla al que se enruta cada SfxId (design/audio/audio-torre-dinamica.md §5.1).
// BossAlert/Ambience/Music no los usa ningún SfxId hoy: son grupos reservados para el stinger
// de jefe y el sistema de stems musicales, ninguno implementado todavía (fuera de este alcance).
public enum MixCategory
{
    Decree,
    BossAlert,
    Combat,
    Ambience,
    Music,
    UI
}

// Gestor de sonido con reserva de AudioSources. Si no hay clips importados sintetiza los
// suyos por código, así el juego suena desde el primer día y nunca hay referencias nulas.
public class AudioManager : MonoBehaviour
{
    private const string UiVolumeKey = "EndlessPull.UiVolume";
    private const string CombatVolumeKey = "EndlessPull.CombatVolume";
    private const string BgmVolumeKey = "EndlessPull.BgmVolume";

    // Una entrada por variante de un mismo efecto; varias entradas con el mismo id son
    // round-robin entre sí (gap 8: evita migrar a ScriptableObject para solo 3 casos de variantes,
    // ver decisión en design/audio/audio-torre-dinamica.md §8).
    [System.Serializable]
    public class SfxEntry
    {
        [Tooltip("Efecto al que corresponde esta variante.")]
        public SfxId id;

        [Tooltip("-1 = todos los biomas; usa TowerBiome.IndexForFloor para variantes de bioma (ej. MeleeHit en Minas).")]
        public int biomeIndex = -1;

        [Tooltip("Clip importado; vacío usa el sintético de reserva.")]
        public AudioClip clip;
    }

    // Presupuesto de memoria (§7.1, sin assets reales todavía): al importar clips de verdad,
    // configurar el Load Type en el Inspector del audio -- Decompress On Load para SFX de
    // decreto/combate/UI (<30KB, cortos y frecuentes); Compressed In Memory para el stinger de
    // aviso de jefe (<100KB, evento raro); Streaming para música y ambiente (loops largos).
    [Tooltip("Clips importados que sustituyen a los sintéticos; varias entradas del mismo id rotan entre sí.")]
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

    [Tooltip("Música de fondo en la base; se detecta sola por nombre de archivo (ver AutoDetectClips).")]
    [SerializeField] private AudioClip bgmBaseClip;

    [Tooltip("Música de fondo en combate; se detecta sola por nombre de archivo (ver AutoDetectClips).")]
    [SerializeField] private AudioClip bgmCombatClip;

    [Tooltip("Volumen de la música de fondo.")]
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.4f;

    [Tooltip("Segundos de crossfade al cambiar de música (base <-> combate).")]
    [SerializeField] private float bgmCrossfadeSeconds = 1.5f;

    [Tooltip("Mixer con los grupos Decrees/BossAlert/CombatSFX/Ambience/Music/UI (Fase 30, §5.1).")]
    [SerializeField] private AudioMixer mixer;

    [Tooltip("Grupo de salida de los decretos del Maestro.")]
    [SerializeField] private AudioMixerGroup decreesGroup;

    [Tooltip("Grupo de salida del SFX de combate.")]
    [SerializeField] private AudioMixerGroup combatGroup;

    [Tooltip("Grupo de salida de la interfaz.")]
    [SerializeField] private AudioMixerGroup uiGroup;

    [Tooltip("Grupo de salida de la música de fondo.")]
    [SerializeField] private AudioMixerGroup musicGroup;

    [Tooltip("Caída en dB de música y ambiente al disparar un decreto (techo de audibilidad, §4).")]
    [SerializeField] private float decreeDuckDb = -8f;

    [Tooltip("Duración de ataque/hold/liberación del duck de decreto, en segundos.")]
    [SerializeField] private Vector3 decreeDuckTiming = new Vector3(0.05f, 0.2f, 0.3f);

    [Tooltip("Caída en dB solo de música al aparecer un jefe (el ambiente no se toca, §4).")]
    [SerializeField] private float bossDuckDb = -6f;

    [Tooltip("Duración de ataque/hold/liberación del duck de aviso de jefe, en segundos.")]
    [SerializeField] private Vector3 bossDuckTiming = new Vector3(0.1f, 0.6f, 1f);

    private static AudioManager instance;

    private readonly List<AudioSource> pool = new List<AudioSource>();
    private readonly Dictionary<string, AudioClip> synthCache = new Dictionary<string, AudioClip>();
    private readonly Dictionary<SfxId, float> lastPlayed = new Dictionary<SfxId, float>();
    private readonly Dictionary<SfxId, int> variantCursor = new Dictionary<SfxId, int>();
    private readonly List<AudioClip> variantScratch = new List<AudioClip>();
    private readonly Dictionary<string, Coroutine> duckRoutines = new Dictionary<string, Coroutine>();
    private readonly Dictionary<string, float> duckHoldUntil = new Dictionary<string, float>();

    private int nextSource;
    private WaveManager waves;
    private int currentBiomeIndex;

    // Dos AudioSources en crossfade: mientras una suena a volumen pleno, la otra sube desde 0.
    private AudioSource bgmSourceA;
    private AudioSource bgmSourceB;
    private AudioSource activeBgm;
    private AudioClip currentBgmClip;
    private Coroutine bgmCrossfadeRoutine;

    // Efectos largos/repetitivos (ej. golpeo de yunque) se cortan con fade-out en vez de sonar enteros.
    private static readonly Dictionary<SfxId, float> MaxDurationSeconds = new Dictionary<SfxId, float>
    {
        { SfxId.CraftSuccess, 3f }
    };

    public bool Muted => muted;
    public static bool IsMuted => instance != null && instance.muted;

    // Volumen por canal; el slider del menú in-game los lee y escribe directamente.
    public float UIVolume
    {
        get => uiVolume;
        set
        {
            uiVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(UiVolumeKey, uiVolume);
            PlayerPrefs.Save();
        }
    }

    public float CombatVolume
    {
        get => combatVolume;
        set
        {
            combatVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(CombatVolumeKey, combatVolume);
            PlayerPrefs.Save();
        }
    }

    public float BGMVolume
    {
        get => bgmVolume;
        set
        {
            bgmVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
            PlayerPrefs.Save();
            if (activeBgm != null) activeBgm.volume = bgmVolume;
        }
    }

    void Awake()
    {
        // Si ya había uno, este sobra: dos reservas competirían por los mismos sonidos.
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;

        // Los volúmenes de fábrica son los del Inspector; el jugador los pisa desde el menú.
        uiVolume = PlayerPrefs.GetFloat(UiVolumeKey, uiVolume);
        combatVolume = PlayerPrefs.GetFloat(CombatVolumeKey, combatVolume);

        BuildPool();
        BuildBgmSources();

        bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, bgmVolume);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;

        if (waves != null)
        {
            waves.FloorChanged -= OnFloorChanged;
            waves.BossStateChanged -= OnBossStateChanged;
            waves.ExpeditionChanged -= OnExpeditionChanged;
        }
    }

    void Start()
    {
        HookSceneButtons();

        // Misma estrategia que HookSceneButtons(): una sola localización, sin referencia serializada.
        waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
        if (waves != null)
        {
            currentBiomeIndex = TowerBiome.IndexForFloor(waves.CurrentFloor);
            waves.FloorChanged += OnFloorChanged;
            waves.BossStateChanged += OnBossStateChanged;
            waves.ExpeditionChanged += OnExpeditionChanged;
        }

        PlayBgm(bgmBaseClip);
    }

    private void OnExpeditionChanged(ExpeditionState state, string message)
        => PlayBgm(state == ExpeditionState.InProgress ? bgmCombatClip : bgmBaseClip);

    private void OnFloorChanged(int floor) => currentBiomeIndex = TowerBiome.IndexForFloor(floor);

    // Jefe presente: duck de música (aviso de jefe, §4/§5.1). Sin WaveManager en escena
    // (menú, tests) esto nunca se dispara y el bioma se queda en el valor por defecto (Goblin).
    private void OnBossStateChanged(bool alive)
    {
        if (alive) DuckGroup("MusicDuck", bossDuckDb, bossDuckTiming.x, bossDuckTiming.y, bossDuckTiming.z);
    }

    // Los botones montados en la escena no pasan por UIBuild: se les engancha el clic aquí.
    // Excluye los botones de MasterActionBar (gap 7): ya reproducen su propio SfxId.DecreeX
    // y apilar UiClick encima duplicaría el sonido en el mismo frame.
    private void HookSceneButtons()
    {
        foreach (var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button == null || MasterActionBar.IsDecreeButton(button)) continue;

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

    private void BuildBgmSources()
    {
        bgmSourceA = NewBgmSource("BgmSource_A");
        bgmSourceB = NewBgmSource("BgmSource_B");
        activeBgm = bgmSourceA;
    }

    private AudioSource NewBgmSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.outputAudioMixerGroup = GroupFor(MixCategory.Music);
        return source;
    }

    // Cambia la música de fondo con crossfade; llamar con el mismo clip o null no hace nada.
    private void PlayBgm(AudioClip clip)
    {
        if (clip == null || clip == currentBgmClip) return;
        currentBgmClip = clip;

        var entrante = activeBgm == bgmSourceA ? bgmSourceB : bgmSourceA;
        var saliente = activeBgm;

        entrante.clip = clip;
        entrante.volume = 0f;
        entrante.Play();
        activeBgm = entrante;

        if (bgmCrossfadeRoutine != null) StopCoroutine(bgmCrossfadeRoutine);
        bgmCrossfadeRoutine = StartCoroutine(CrossfadeBgm(entrante, saliente));
    }

    private IEnumerator CrossfadeBgm(AudioSource entrante, AudioSource saliente)
    {
        float t = 0f;
        float volumenSaliente = saliente != null ? saliente.volume : 0f;

        while (t < bgmCrossfadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float frac = Mathf.Clamp01(t / bgmCrossfadeSeconds);

            entrante.volume = Mathf.Lerp(0f, bgmVolume, frac);
            if (saliente != null) saliente.volume = Mathf.Lerp(volumenSaliente, 0f, frac);
            yield return null;
        }

        entrante.volume = bgmVolume;
        if (saliente != null) { saliente.volume = 0f; saliente.Stop(); }
        bgmCrossfadeRoutine = null;
    }

    // Efectos largos (yunque, etc.): se dejan sonar hasta maxSeconds y luego se apagan con fundido,
    // en vez de reproducir el archivo entero.
    private IEnumerator FadeOutAndStop(AudioSource source, AudioClip clip, float maxSeconds)
    {
        float startVolume = source.volume;

        float t = 0f;
        while (t < maxSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;

            // Otro sonido reutilizó esta fuente antes de tiempo: no le toques el volumen.
            if (source.clip != clip) yield break;
        }

        float fade = 0f;
        const float fadeSeconds = 0.3f;
        while (fade < fadeSeconds)
        {
            fade += Time.unscaledDeltaTime;
            if (source.clip != clip) yield break;

            source.volume = Mathf.Lerp(startVolume, 0f, fade / fadeSeconds);
            yield return null;
        }

        if (source.clip == clip) source.Stop();
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
           || id == SfxId.Ascension || id == SfxId.CraftSuccess || id == SfxId.Error
           || id == SfxId.Potion || id == SfxId.Reward
           ? AudioChannel.UI
           : AudioChannel.Combat;

    // A qué grupo del AudioMixer se enruta cada efecto (§5.1); independiente del volumen lineal de ChannelOf.
    public static MixCategory MixCategoryOf(SfxId id)
    {
        switch (id)
        {
            case SfxId.DecreeHeal:
            case SfxId.DecreeFocusFire:
            case SfxId.DecreeRegroup:
            case SfxId.DecreeRetreat:
                return MixCategory.Decree;
            case SfxId.UiClick:
            case SfxId.UiOpen:
            case SfxId.UiClose:
            case SfxId.CardReveal:
            case SfxId.Ascension:
            case SfxId.CraftSuccess:
            case SfxId.Error:
            case SfxId.Potion:
            case SfxId.Reward:
                return MixCategory.UI;
            default:
                return MixCategory.Combat;
        }
    }

    private AudioMixerGroup GroupFor(MixCategory category)
    {
        switch (category)
        {
            case MixCategory.Decree: return decreesGroup;
            case MixCategory.UI: return uiGroup;
            case MixCategory.Music: return musicGroup;
            default: return combatGroup;
        }
    }

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

        var categoria = MixCategoryOf(id);

        source.clip = clip;
        source.outputAudioMixerGroup = GroupFor(categoria);
        source.volume = ChannelOf(id) == AudioChannel.UI ? uiVolume : combatVolume;
        source.spatialBlend = 0f;
        source.transform.position = positional ? worldPosition : transform.position;
        source.Play();

        if (MaxDurationSeconds.TryGetValue(id, out float maxSeconds) && clip.length > maxSeconds)
            StartCoroutine(FadeOutAndStop(source, clip, maxSeconds));

        // Ducking de decreto: música + ambiente, disparado desde el mismo Play() que ya suena
        // hoy el SfxId.DecreeX (§5.1, gap 4 eliminado: no hace falta tocar MasterCommander).
        if (categoria == MixCategory.Decree)
        {
            DuckGroup("MusicDuck", decreeDuckDb, decreeDuckTiming.x, decreeDuckTiming.y, decreeDuckTiming.z);
            DuckGroup("AmbienceDuck", decreeDuckDb, decreeDuckTiming.x, decreeDuckTiming.y, decreeDuckTiming.z);
        }
    }

    // Re-disparable: si el parámetro ya está agachado, extiende el hold en vez de encolar un duck nuevo (§5.1).
    private void DuckGroup(string param, float dB, float attack, float hold, float release)
    {
        if (mixer == null) return;

        duckHoldUntil[param] = Time.unscaledTime + hold;

        if (duckRoutines.TryGetValue(param, out var activa) && activa != null) return;
        duckRoutines[param] = StartCoroutine(DuckRoutine(param, dB, attack, release));
    }

    private IEnumerator DuckRoutine(string param, float dB, float attack, float release)
    {
        yield return AnimateMixerParam(param, 0f, dB, attack);

        while (Time.unscaledTime < duckHoldUntil[param])
            yield return null;

        yield return AnimateMixerParam(param, dB, 0f, release);
        duckRoutines[param] = null;
    }

    private IEnumerator AnimateMixerParam(string param, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            mixer.SetFloat(param, to);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            mixer.SetFloat(param, Mathf.Lerp(from, to, t / duration));
            yield return null;
        }

        mixer.SetFloat(param, to);
    }

    // Variantes reales primero (importadas); si no hay ninguna, el sintético de reserva.
    private AudioClip ClipFor(SfxId id)
    {
        var variantes = VariantsFor(id);
        if (variantes.Count > 0) return variantes[NextVariantIndex(id, variantes.Count)];

        return SynthesizedClip(id);
    }

    // Prioriza variantes específicas del bioma activo (ej. MeleeHit en Minas); si no hay, usa las genéricas.
    private List<AudioClip> VariantsFor(SfxId id)
    {
        variantScratch.Clear();
        var genericas = new List<AudioClip>();

        foreach (var entry in clips)
        {
            if (entry == null || entry.id != id || entry.clip == null) continue;

            if (entry.biomeIndex == currentBiomeIndex) variantScratch.Add(entry.clip);
            else if (entry.biomeIndex < 0) genericas.Add(entry.clip);
        }

        return variantScratch.Count > 0 ? variantScratch : genericas;
    }

    private int NextVariantIndex(SfxId id, int count)
    {
        int idx = variantCursor.TryGetValue(id, out var actual) ? actual % count : 0;
        variantCursor[id] = (idx + 1) % count;
        return idx;
    }

    // El sintético se cachea por id; MeleeHit además se cachea por bioma (varía su sweep en Minas).
    private AudioClip SynthesizedClip(SfxId id)
    {
        string clave = id == SfxId.MeleeHit ? $"MeleeHit_{currentBiomeIndex}" : id.ToString();

        if (synthCache.TryGetValue(clave, out var cacheado)) return cacheado;

        var clip = Synthesize(id);
        synthCache[clave] = clip;
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
                // Minas desplaza el sweep de 220→90 a 320→150 Hz para no pisar el cello/contrabajo
                // del bioma (mitigación de contenido descrita en el doc de audio, §5).
                if (currentBiomeIndex == 1)
                { duracion = 0.14f; desde = 320f; hasta = 150f; ruido = 0.55f; decaimiento = 28f; cuadrada = true; }
                else
                { duracion = 0.14f; desde = 220f; hasta = 90f; ruido = 0.55f; decaimiento = 28f; cuadrada = true; }
                break;
            case SfxId.ArrowShot:
                duracion = 0.16f; desde = 1500f; hasta = 500f; ruido = 0.35f; decaimiento = 22f; cuadrada = false;
                break;
            case SfxId.MagicBolt:
                duracion = 0.22f; desde = 700f; hasta = 1900f; ruido = 0.10f; decaimiento = 9f; cuadrada = false;
                break;
            case SfxId.Victory:
                duracion = 0.7f; desde = 500f; hasta = 1300f; ruido = 0f; decaimiento = 3f; cuadrada = false;
                break;
            case SfxId.DecreeHeal:
                duracion = 0.45f; desde = 520f; hasta = 1040f; ruido = 0f; decaimiento = 5f; cuadrada = false;
                break;
            case SfxId.DecreeRegroup:
                duracion = 0.40f; desde = 300f; hasta = 600f; ruido = 0.02f; decaimiento = 6f; cuadrada = true;
                break;
            case SfxId.DecreeFocusFire:
                duracion = 0.35f; desde = 600f; hasta = 1100f; ruido = 0.03f; decaimiento = 6f; cuadrada = false;
                break;
            case SfxId.DecreeRetreat:
                duracion = 0.5f; desde = 700f; hasta = 300f; ruido = 0.05f; decaimiento = 5f; cuadrada = false;
                break;
            case SfxId.Critical:
                // Único SFX de combate con distorsión reservada (§2/§4.1): el más "sucio" del set.
                duracion = 0.12f; desde = 1800f; hasta = 2600f; ruido = 0.15f; decaimiento = 15f; cuadrada = true;
                break;
            case SfxId.Ascension:
                duracion = 1.1f; desde = 440f; hasta = 880f; ruido = 0f; decaimiento = 2f; cuadrada = false;
                break;
            case SfxId.CraftSuccess:
                duracion = 0.18f; desde = 1000f; hasta = 1400f; ruido = 0.08f; decaimiento = 20f; cuadrada = true;
                break;
            case SfxId.Impact:
                duracion = 0.22f; desde = 320f; hasta = 70f; ruido = 0.7f; decaimiento = 18f; cuadrada = true;
                break;
            case SfxId.Error:
                duracion = 0.22f; desde = 220f; hasta = 140f; ruido = 0.05f; decaimiento = 14f; cuadrada = true;
                break;
            case SfxId.Potion:
                duracion = 0.30f; desde = 500f; hasta = 1000f; ruido = 0.04f; decaimiento = 8f; cuadrada = false;
                break;
            case SfxId.Reward:
                duracion = 0.40f; desde = 700f; hasta = 1500f; ruido = 0.02f; decaimiento = 6f; cuadrada = false;
                break;
            case SfxId.HeroHurt:
                duracion = 0.18f; desde = 260f; hasta = 110f; ruido = 0.4f; decaimiento = 20f; cuadrada = false;
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

#if UNITY_EDITOR
    // Escanea Assets/_EndlessPull/Audio y asigna cada clip por coincidencia de texto en su nombre
    // (roadmap Fase 40 §1). Sin instalador Python/Node: solo Editor de Unity, botón del Inspector.
    private static readonly (string[] palabras, SfxId id)[] SfxKeywordRules =
    {
        (new[] { "click", "tap", "button", "select", "tick" }, SfxId.UiClick),
        (new[] { "open", "window", "popup" }, SfxId.UiOpen),
        (new[] { "close", "dismiss", "back" }, SfxId.UiClose),
        (new[] { "error", "deny", "locked", "cancel", "fail" }, SfxId.Error),
        (new[] { "anvil", "craft", "forge", "hammer", "smith", "yunque", "martillo" }, SfxId.CraftSuccess),
        (new[] { "reward", "coin", "gold", "gem", "loot" }, SfxId.Reward),
        (new[] { "heal", "potion", "magic_heal", "recovery" }, SfxId.Potion),
        (new[] { "summon", "ascend", "portal", "level_up", "fanfare" }, SfxId.Ascension),
        (new[] { "hurt" }, SfxId.HeroHurt),
        (new[] { "hit", "slash", "sword", "strike", "punch" }, SfxId.MeleeHit),
        (new[] { "shoot", "arrow", "bow", "cast", "magic_shot" }, SfxId.ArrowShot),
        (new[] { "victory", "win" }, SfxId.Victory),
        // "lose" fuera de la lista a propósito: es substring de "close" (falso positivo real,
        // ver SFX_UiOpenClose.wav) -- "defeat"/"game_over" ya cubren el grupo sin ese riesgo.
        (new[] { "defeat", "game_over" }, SfxId.Defeat),
    };

    private static readonly string[] BgmBaseKeywords = { "base", "ambient", "peace", "village", "town" };
    private static readonly string[] BgmCombatKeywords = { "combat", "battle", "tower", "fight", "arena" };

    [ContextMenu("Auto-detectar clips de Assets/_EndlessPull/Audio")]
    public void AutoDetectClips()
    {
        var guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_EndlessPull/Audio" });
        int nuevos = 0;

        foreach (var guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            string nombre = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) continue;

            if (ContainsAny(nombre, BgmBaseKeywords)) { bgmBaseClip = clip; continue; }
            if (ContainsAny(nombre, BgmCombatKeywords)) { bgmCombatClip = clip; continue; }

            foreach (var regla in SfxKeywordRules)
            {
                if (!ContainsAny(nombre, regla.palabras)) continue;
                if (clips.Exists(e => e.id == regla.id && e.clip == clip)) continue;

                clips.Add(new SfxEntry { id = regla.id, biomeIndex = -1, clip = clip });
                nuevos++;
            }
        }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[AudioManager] Auto-detección: {nuevos} entrada(s) nueva(s), " +
                  $"BGM base={bgmBaseClip}, BGM combate={bgmCombatClip}.", this);
    }

    private static bool ContainsAny(string texto, string[] palabras)
    {
        foreach (var palabra in palabras)
            if (texto.Contains(palabra)) return true;
        return false;
    }
#endif
}
