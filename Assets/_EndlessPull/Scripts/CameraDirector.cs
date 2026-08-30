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

    void Awake()
    {
        if (target == null) target = Camera.main;
        if (waves == null) waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();

        destination = baseView;
        origin = baseView;
        travelTimer = travelSeconds;
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
        if (target == null || travelTimer >= travelSeconds) return;

        travelTimer += Time.deltaTime;

        // SmoothStep para que arranque y frene suave en vez de a tirones.
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(travelTimer / travelSeconds));
        Vector2 pos = Vector2.Lerp(origin, destination, t);
        target.transform.position = new Vector3(pos.x, pos.y, target.transform.position.z);

        if (target.orthographic) target.orthographicSize = Mathf.Lerp(sizeOrigin, sizeDestination, t);
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

        origin = target.transform.position;
        destination = point;
        sizeOrigin = target.orthographic ? target.orthographicSize : size;
        sizeDestination = size;
        travelTimer = 0f;
    }

    private void SnapTo(Vector2 point, float size)
    {
        if (target == null) return;

        target.transform.position = new Vector3(point.x, point.y, target.transform.position.z);
        if (target.orthographic) target.orthographicSize = size;

        origin = point;
        destination = point;
        sizeOrigin = size;
        sizeDestination = size;
        travelTimer = travelSeconds;
    }
}
