using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
    [SerializeField] private Vector2 arenaView = new Vector2(1000f, 0f);

    [Tooltip("Segundos que tarda el viaje entre base y arena.")]
    [SerializeField] private float travelSeconds = 1.1f;

    [Tooltip("Segundos que espera la cámara tras pulsar Empezar Torre antes de cortar a la arena, para que la escuadra llegue a pie al Portal.")]
    [SerializeField] private float arenaEntryDelay = 1.5f;


    [Tooltip("Tamaño ortográfico en la vista de base; crece para que quepan el hub y los 3 cuadrantes.")]
    [SerializeField] private float baseOrthographicSize = 12f;

    [Tooltip("Tamaño ortográfico en la vista de arena; se mantiene el encuadre de combate actual.")]
    [SerializeField] private float arenaOrthographicSize = 8.5f;

    [Tooltip("Centro y tamaño del hub central; siempre entra en el límite de cámara aunque no haya cuadrantes desbloqueados.")]
    [SerializeField] private Vector2 hubBoundsCenter = new Vector2(0f, -1f);
    [SerializeField] private Vector2 hubBoundsSize = new Vector2(12f, 14f);

    // Muralla perimetral a ±20u del centro (Fase 37): el máximo deja ver la base entera sin salirse del suelo.
    [Tooltip("Zoom ortográfico mínimo y máximo permitido al manipular la vista de base.")]
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 26f;

    // x15 respecto al valor original: 3-4 giros de rueda deben cubrir todo el rango de zoom.
    [Tooltip("Sensibilidad de la rueda del ratón al hacer zoom en la vista de base.")]
    [SerializeField] private float mouseZoomSpeed = 7.5f;

    [Tooltip("Sensibilidad del pellizco táctil al hacer zoom en la vista de base.")]
    [SerializeField] private float pinchZoomSpeed = 0.1f;

    [Tooltip("Píxeles de arrastre antes de considerar el gesto un paneo y no un toque/clic corto.")]
    [SerializeField] private float dragScreenDeadzone = 8f;

    private Vector2 origin;
    private Vector2 destination;
    private float travelTimer;

    // Negativo = sin cuenta atrás pendiente; cuenta en tiempo real, igual que TriggerShake.
    private float pendingArenaDelay = -1f;


    private float sizeOrigin;
    private float sizeDestination;

    // Posición "real" de la cámara sin el temblor; el temblor se suma encima solo al escribir el transform.
    private Vector2 basePosition;
    private float shakeTimer;
    private float shakeDurationTotal;
    private float shakeMagnitude;

    // Solo se admite zoom/paneo manual mientras el jugador está viendo la base, quieta y sin viajar.
    private bool inBaseView = true;
    private bool dragActive;
    private Vector2 dragStartScreen;
    private Vector2 lastDragScreen;
    private float previousPinchDistance;

    // Color de fondo de la base; se restaura al volver de la arena tras el tinte de bioma.
    private Color baseBackgroundColor;

    private static CameraDirector instance;

    void Awake()
    {
        if (target == null) target = Camera.main;
        if (waves == null) waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();

        destination = baseView;
        origin = baseView;
        basePosition = baseView;
        travelTimer = travelSeconds;

        if (target != null) baseBackgroundColor = target.backgroundColor;

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

void Update()
    {
        HandleBaseViewControls();

        if (pendingArenaDelay >= 0f)
        {
            pendingArenaDelay -= Time.unscaledDeltaTime;
            if (pendingArenaDelay <= 0f)
            {
                pendingArenaDelay = -1f;
                GoToArena();
                ApplyBiomeTint();
                if (arenaGround != null) arenaGround.gameObject.SetActive(true);
            }
        }
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

    [Tooltip("Suelo de la arena de combate; se tiñe con TowerBiome.Tint y se apaga en la vista de base.")]
    [SerializeField] private SpriteRenderer arenaGround;

    public void GoToArena() => TravelTo(ArenaPoint, arenaOrthographicSize);
    public void GoToBase() => TravelTo(baseView, baseOrthographicSize);

private void OnExpeditionChanged(ExpeditionState state, string message)
    {
        inBaseView = state != ExpeditionState.InProgress;

        if (state == ExpeditionState.InProgress)
        {
            // No cortar al instante: la escuadra aún está caminando desde sus zonas hasta el Portal.
            pendingArenaDelay = arenaEntryDelay;
        }
        else
        {
            pendingArenaDelay = -1f;
            GoToBase();
            if (target != null) target.backgroundColor = baseBackgroundColor;
            if (arenaGround != null) arenaGround.gameObject.SetActive(false);
        }
    }

    // Placeholder sin arte final (ver decisión de diseño): tiñe el fondo de cámara y el suelo de
    // la arena según TowerBiome.IndexForFloor, en vez de dejar solo el color de fondo plano.
    private void ApplyBiomeTint()
    {
        if (target == null || waves == null) return;

        int biomeIndex = TowerBiome.IndexForFloor(waves.CurrentFloor);
        if (biomeIndex < 0 || biomeIndex >= TowerBiome.Tint.Length) return;

        target.backgroundColor = TowerBiome.Tint[biomeIndex];

        // El suelo va un punto más claro que el fondo para que se note como superficie, no como vacío.
        if (arenaGround != null)
            arenaGround.color = TowerBiome.Tint[biomeIndex] + new Color(0.06f, 0.06f, 0.06f);
    }

    // Zoom (rueda/pellizco) y paneo (arrastre) manuales; solo activos en la vista de base, quieta y sin viajar.
    private void HandleBaseViewControls()
    {
        if (target == null || !inBaseView || IsTravelling) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            dragActive = false;
            return;
        }

        bool changed = false;

        float sizeDelta = ReadOrthoSizeDelta();
        if (Mathf.Abs(sizeDelta) > 0.0001f)
        {
            target.orthographicSize = Mathf.Clamp(target.orthographicSize + sizeDelta, minZoom, maxZoom);
            changed = true;
        }

        if (HandleDragPan()) changed = true;

        if (changed) ClampToUnlockedBounds();
    }

    private float ReadOrthoSizeDelta()
    {
        float sizeDelta = 0f;

        // Rueda del ratón: hacia arriba acerca (tamaño ortográfico menor).
        if (Mouse.current != null)
            sizeDelta -= Mouse.current.scroll.ReadValue().y * mouseZoomSpeed;

        var touch = Touchscreen.current;
        if (touch != null && touch.touches.Count >= 2
            && touch.touches[0].press.isPressed && touch.touches[1].press.isPressed)
        {
            float distance = Vector2.Distance(touch.touches[0].position.ReadValue(), touch.touches[1].position.ReadValue());
            if (previousPinchDistance > 0f) sizeDelta -= (distance - previousPinchDistance) * pinchZoomSpeed;
            previousPinchDistance = distance;
        }
        else
        {
            previousPinchDistance = 0f;
        }

        return sizeDelta;
    }

    // Arrastre con un dedo o botón izquierdo; devuelve true si movió la cámara este frame.
    private bool HandleDragPan()
    {
        Vector2 screenPos;
        bool pressed, pressedThisFrame;

        var touch = Touchscreen.current;
        bool singleTouch = touch != null && touch.touches.Count == 1 && touch.primaryTouch.press.isPressed;

        if (singleTouch)
        {
            screenPos = touch.primaryTouch.position.ReadValue();
            pressed = true;
            pressedThisFrame = touch.primaryTouch.press.wasPressedThisFrame;
        }
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            screenPos = Mouse.current.position.ReadValue();
            pressed = true;
            pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
        }
        else
        {
            dragActive = false;
            return false;
        }

        if (pressedThisFrame)
        {
            dragStartScreen = screenPos;
            lastDragScreen = screenPos;
            dragActive = false;
            return false;
        }

        if (!pressed) return false;

        // Por debajo del deadzone se trata como un tap/clic corto, no como paneo.
        if (!dragActive && Vector2.Distance(screenPos, dragStartScreen) < dragScreenDeadzone) return false;

        dragActive = true;

        Vector2 worldNow = target.ScreenToWorldPoint(screenPos);
        Vector2 worldLast = target.ScreenToWorldPoint(lastDragScreen);
        basePosition -= worldNow - worldLast;
        origin = basePosition;
        destination = basePosition;
        lastDragScreen = screenPos;
        return true;
    }

    // Restringe basePosition a los límites del hub más los cuadrantes ya desbloqueados (decisión de diseño).
    private void ClampToUnlockedBounds()
    {
        var bounds = new Bounds(hubBoundsCenter, new Vector3(hubBoundsSize.x, hubBoundsSize.y, 0f));
        foreach (var quadrant in QuadrantController.All)
            if (quadrant != null && quadrant.IsUnlocked)
                bounds.Encapsulate(quadrant.ZoneBounds);

        float halfHeight = target.orthographicSize;
        float halfWidth = halfHeight * target.aspect;

        float minX = bounds.min.x + halfWidth;
        float maxX = bounds.max.x - halfWidth;
        float minY = bounds.min.y + halfHeight;
        float maxY = bounds.max.y - halfHeight;

        // Si el encuadre es más grande que la zona desbloqueada, pivota sobre el centro real de la
        // base (baseView) en vez de bounds.center, que se desplaza al encapsular cuadrantes
        // desbloqueados de forma asimétrica y cortaba el lateral izquierdo del campamento.
        float clampedX = minX <= maxX ? Mathf.Clamp(basePosition.x, minX, maxX) : baseView.x;
        float clampedY = minY <= maxY ? Mathf.Clamp(basePosition.y, minY, maxY) : baseView.y;

        basePosition = new Vector2(clampedX, clampedY);
        origin = basePosition;
        destination = basePosition;
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
