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

    [Tooltip("Punto al que mira la cámara durante el combate.")]
    [SerializeField] private Vector2 arenaView = new Vector2(40f, 0f);

    [Tooltip("Segundos que tarda el viaje entre base y arena.")]
    [SerializeField] private float travelSeconds = 1.1f;

    private Vector2 origin;
    private Vector2 destination;
    private float travelTimer;

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
        SnapTo(baseView);
    }

    void LateUpdate()
    {
        if (target == null || travelTimer >= travelSeconds) return;

        travelTimer += Time.deltaTime;

        // SmoothStep para que arranque y frene suave en vez de a tirones.
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(travelTimer / travelSeconds));
        Vector2 pos = Vector2.Lerp(origin, destination, t);
        target.transform.position = new Vector3(pos.x, pos.y, target.transform.position.z);
    }

    public bool IsTravelling => travelTimer < travelSeconds;

    public void GoToArena() => TravelTo(arenaView);
    public void GoToBase() => TravelTo(baseView);

    private void OnExpeditionChanged(ExpeditionState state, string message)
    {
        if (state == ExpeditionState.InProgress) GoToArena();
        else GoToBase();
    }

    private void TravelTo(Vector2 point)
    {
        if (target == null) return;

        origin = target.transform.position;
        destination = point;
        travelTimer = 0f;
    }

    private void SnapTo(Vector2 point)
    {
        if (target == null) return;

        target.transform.position = new Vector3(point.x, point.y, target.transform.position.z);
        origin = point;
        destination = point;
        travelTimer = travelSeconds;
    }
}
