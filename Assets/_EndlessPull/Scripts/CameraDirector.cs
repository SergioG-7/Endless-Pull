using UnityEngine;

// Lleva la cámara de la base a la arena y de vuelta, sin cortes bruscos.
public class CameraDirector : MonoBehaviour
{
    [Tooltip("Cámara que se mueve; si se deja vacía se coge la principal.")]
    [SerializeField] private Camera target;

    [Tooltip("Gestor de oleadas que avisa de cuándo empieza y acaba el combate.")]
    [SerializeField] private WaveManager waves;

    [Tooltip("Punto al que mira la cámara mientras se gestiona la base.")]
    [SerializeField] private Vector2 baseView = new Vector2(0f, 0f);

    [Tooltip("Punto de combate de reserva; con un WaveManager en escena manda el suyo.")]
    [SerializeField] private Vector2 arenaView = new Vector2(40f, 0f);

    [Tooltip("Segundos que tarda el viaje entre base y arena.")]
    [SerializeField] private float travelSeconds = 1.1f;

    [Tooltip("Tamaño ortográfico en la vista de base; crece para que quepan el hub y los 3 cuadrantes.")]
    [SerializeField] private float baseOrthographicSize = 12f;

    [Tooltip("Tamaño ortográfico en la vista de arena; se mantiene el encuadre de combate actual.")]
    [SerializeField] private float arenaOrthographicSize = 8.5f;

    private Vector2 origin;
    private Vector2 destination;
    private float travelTimer;

    private float sizeOrigin;
    private float sizeDestination;

    // Posición "real" de la cámara sin el temblor; el temblor se suma encima solo al escribir el transform.
    private Vector2 basePosition;
    private float shakeTimer;
    private float shakeDurationTotal;
    private float shakeMagnitude;

    private static CameraDirector instance;

    void Awake()
    {
        if (target == null) target = Camera.main;
        if (waves == null) waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();

        destination = baseView;
        origin = baseView;
        basePosition = baseView;
        travelTimer = travelSeconds;

        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Punto de entrada estático: el combate dispara el temblor sin necesitar referencia a la cámara.
    public static void Shake(float duration, float magnitude)
    {
        if (instance != null) instance.TriggerShake(duration, magnitude);
    }

    private void TriggerShake(float duration, float magnitude)
    {
        // Un golpe flojo no pisa a uno fuerte que ya estuviera temblando.
        if (shakeTimer > 0f && magnitude < shakeMagnitude) return;

        shakeTimer = duration;
        shakeDurationTotal = duration;
        shakeMagnitude = Mathf.Clamp(magnitude, 0f, 1f);
    }

    // Decaimiento lineal con tiempo real: se nota el temblor incluso durante el hitstop.
    private Vector2 TickShake()
    {
        if (shakeTimer <= 0f) return Vector2.zero;

        shakeTimer -= Time.unscaledDeltaTime;
        float t = shakeDurationTotal > 0f ? Mathf.Clamp01(shakeTimer / shakeDurationTotal) : 0f;
        return UnityEngine.Random.insideUnitCircle * shakeMagnitude * t;
    }

    void OnEnable()
    {
        if (waves != null) waves.ExpeditionChanged += OnExpeditionChanged;
    }

    void OnDisable()
    {
        if (waves != null) waves.ExpeditionChanged -= OnExpeditionChanged;
    }

    void Start()
    {
        SnapTo(baseView, baseOrthographicSize);
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (travelTimer < travelSeconds)
        {
            travelTimer += Time.deltaTime;

            // SmoothStep para que arranque y frene suave en vez de a tirones.
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(travelTimer / travelSeconds));
            basePosition = Vector2.Lerp(origin, destination, t);

            if (target.orthographic) target.orthographicSize = Mathf.Lerp(sizeOrigin, sizeDestination, t);
        }

        // El temblor se aplica siempre encima de basePosition, viajando o parada la cámara.
        Vector2 shakeOffset = TickShake();
        target.transform.position = new Vector3(
            basePosition.x + shakeOffset.x, basePosition.y + shakeOffset.y, target.transform.position.z);
    }

    public bool IsTravelling => travelTimer < travelSeconds;

    // Lo manda el WaveManager para que la cámara no acabe encuadrando una arena vacía.
    private Vector2 ArenaPoint => waves != null ? waves.ArenaFocus : arenaView;

    public void GoToArena() => TravelTo(ArenaPoint, arenaOrthographicSize);
    public void GoToBase() => TravelTo(baseView, baseOrthographicSize);

    private void OnExpeditionChanged(ExpeditionState state, string message)
    {
        if (state == ExpeditionState.InProgress) GoToArena();
        else GoToBase();
    }

    private void TravelTo(Vector2 point, float size)
    {
        if (target == null) return;

        // Se parte de basePosition, no del transform: así un temblor en curso no se cuela como origen.
        origin = basePosition;
        destination = point;
        sizeOrigin = target.orthographic ? target.orthographicSize : size;
        sizeDestination = size;
        travelTimer = 0f;
    }

    private void SnapTo(Vector2 point, float size)
    {
        if (target == null) return;

        basePosition = point;
        target.transform.position = new Vector3(point.x, point.y, target.transform.position.z);
        if (target.orthographic) target.orthographicSize = size;

        origin = point;
        destination = point;
        sizeOrigin = size;
        sizeDestination = size;
        travelTimer = travelSeconds;
    }
}
