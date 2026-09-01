using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Identidad de cada cuadrante direccional de la base; casa con los flags del save.
public enum QuadrantId
{
    East,
    South,
    West,
    North
}

// Zona de la base bloqueada hasta cierto piso: silueta con candado que se revela una sola vez.
public class QuadrantController : MonoBehaviour
{
    [Tooltip("Identidad del cuadrante; enlaza con el flag persistente en el save.")]
    [SerializeField] private QuadrantId id;

    [Tooltip("Piso de torre a partir del cual se desbloquea este cuadrante.")]
    [SerializeField] private int requiredFloor;

    [Tooltip("Edificios reales de este cuadrante; vacío mientras no haya contenido asignado.")]
    [SerializeField] private BaseBuilding[] buildings = new BaseBuilding[0];

    [Tooltip("Silueta oscurecida que cubre el cuadrante mientras está bloqueado.")]
    [SerializeField] private SpriteRenderer veil;

    [Tooltip("Icono de candado, hijo del veil.")]
    [SerializeField] private SpriteRenderer lockIcon;

    [Tooltip("Etiqueta 'Piso N' flotante sobre el candado.")]
    [SerializeField] private TextMeshPro floorLabel;

    [Tooltip("Segundos del pop de revelado (mismo criterio sin tweening que TowerRewardUI).")]
    [SerializeField] private float revealPopSeconds = 0.35f;

    [Tooltip("Área de paseo que se asigna a los héroes repartidos en este cuadrante.")]
    [SerializeField] private Vector2 wanderSize = new Vector2(6f, 4f);

    // Registro estático: el WorldInteractionManager recorre esta lista, igual que BaseBuilding.All.
    private static readonly List<QuadrantController> all = new List<QuadrantController>();
    public static IReadOnlyList<QuadrantController> All => all;

    private WaveManager waves;

    // Ya se reprodujo el pop de revelado alguna vez; evita repetirlo en cargas posteriores.
    private bool revealed;

    public QuadrantId Id => id;
    public int RequiredFloor => requiredFloor;
    public bool Revealed => revealed;
    public Vector2 WanderSize => wanderSize;

    // Reutiliza el piso que ya publica BaseBuilding; el cuadrante no lleva su propio estado de piso.
    public bool IsUnlocked => BaseBuilding.TowerFloor >= requiredFloor;

    // Límites reales del cuadrante (posición del veil aunque esté desactivado); los usa CameraDirector para el clamp.
    public Bounds ZoneBounds => veil != null ? veil.bounds : new Bounds(transform.position, Vector3.one);

    void Awake()
    {
        waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
    }

    void OnEnable()
    {
        all.Add(this);
        if (waves != null) waves.FloorCleared += OnFloorCleared;
    }

    void OnDisable()
    {
        all.Remove(this);
        if (waves != null) waves.FloorCleared -= OnFloorCleared;
    }

void Start()
    {
        // El veil/lockIcon ya no se pintan (Fase 39: cada edificio muestra su propio candado);
        // se dejan invisibles pero activos para que ContainsPoint siga detectando el toque.
        if (veil != null) veil.enabled = false;
        if (lockIcon != null) lockIcon.enabled = false;

        if (floorLabel != null)
        {
            floorLabel.color = UITheme.Text;
            floorLabel.text = string.Format(LocalizationManager.Get("UI_QUADRANT_FLOOR_LABEL"), requiredFloor);
        }

        RefreshVisual();
    }

    // Activa o apaga silueta/candado/etiqueta según el piso; nunca toca los edificios reales.
// Activa o apaga solo la etiqueta según el piso; el veil/lockIcon quedan siempre invisibles
    // (Fase 39: la sombra de zona y el candado flotante se sustituyeron por el candado por edificio).
    public void RefreshVisual()
    {
        bool locked = !IsUnlocked;

        if (floorLabel != null) floorLabel.gameObject.SetActive(locked);
    }

    // Bounds reales del veil; si está inactivo (cuadrante ya desbloqueado) no hay nada que tocar.
// Usa los bounds del veil (invisible pero activo) solo mientras el cuadrante siga bloqueado.
    public bool ContainsPoint(Vector2 point)
        => veil != null && !IsUnlocked && veil.bounds.Contains(point);

    // La usa el SaveManager al cargar la partida, cotejando por QuadrantId.
    public void LoadRevealed(bool alreadyRevealed)
    {
        revealed = alreadyRevealed;
        RefreshVisual();
    }

    public static QuadrantController Find(QuadrantId quadrantId)
    {
        foreach (var q in all)
            if (q != null && q.id == quadrantId) return q;

        return null;
    }

    private void OnFloorCleared(FloorRewardInfo info)
    {
        if (revealed || !info.firstClear || info.floor != requiredFloor) return;

        StopAllCoroutines();
        StartCoroutine(RevealRoutine());
    }

    // Pop de escala 0.4->1.08->1.0, mismo criterio sin librería de tweening que TowerRewardUI.PopChest.
    private IEnumerator RevealRoutine()
    {
        var targets = new List<Transform> { transform };
        foreach (var building in buildings)
            if (building != null) targets.Add(building.transform);

        var originalScales = new Vector3[targets.Count];
        for (int i = 0; i < targets.Count; i++) originalScales[i] = targets[i].localScale;

        float elapsed = 0f;
        while (elapsed < revealPopSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / revealPopSeconds);

            float overshoot = Mathf.Sin(t * Mathf.PI * 0.5f);
            float scale = Mathf.Lerp(0.4f, 1.08f, overshoot) - (t >= 1f ? 0.08f : 0f);

            for (int i = 0; i < targets.Count; i++)
                targets[i].localScale = originalScales[i] * scale;

            yield return null;
        }

        for (int i = 0; i < targets.Count; i++) targets[i].localScale = originalScales[i];

        revealed = true;
        RefreshVisual();
        SaveManager.RequestSave();
    }
}
