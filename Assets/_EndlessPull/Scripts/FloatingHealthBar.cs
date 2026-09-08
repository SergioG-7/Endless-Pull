using UnityEngine;

// Barra de vida flotante reutilizable: escucha el evento del IHealthOwner del mismo GameObject.
public class FloatingHealthBar : MonoBehaviour
{
    [Tooltip("Pivote anclado al borde izquierdo; se escala en X según la vida.")]
    [SerializeField] private Transform fillPivot;

    [Tooltip("Renderer del relleno, se tinta según la vida restante.")]
    [SerializeField] private SpriteRenderer fillRenderer;

    [Tooltip("Color con la vida llena.")]
    [SerializeField] private Color fullColor = new Color(0.30f, 0.80f, 0.35f);

    [Tooltip("Color al que vira cuando la vida baja.")]
    [SerializeField] private Color lowColor = new Color(0.85f, 0.25f, 0.25f);

    [Tooltip("Si se oculta la barra cuando la vida está al máximo.")]
    [SerializeField] private bool hideWhenFull = false;

    // La barra de maná se cuelga debajo copiando esta geometría.
    public Transform FillPivot => fillPivot;

    private IHealthOwner owner;

    void Awake()
    {
        owner = GetComponent<IHealthOwner>();

        if (owner == null)
            Debug.LogError($"[FloatingHealthBar] '{name}' no tiene ningún IHealthOwner.", this);
    }

    void OnEnable()
    {
        if (owner != null) owner.HealthChanged += Redraw;
    }

    void OnDisable()
    {
        if (owner != null) owner.HealthChanged -= Redraw;
    }

    void Start()
    {
        // Los controllers fijan la vida en Awake, así que aquí ya es válida.
        if (owner != null) Redraw(owner.CurrentHealth, owner.MaxHealth);
    }

    private void Redraw(int current, int max)
    {
        if (fillPivot == null || max <= 0) return;

        float ratio = Mathf.Clamp01((float)current / max);

        Vector3 scale = fillPivot.localScale;
        scale.x = ratio;
        fillPivot.localScale = scale;

        if (fillRenderer != null)
            fillRenderer.color = Color.Lerp(lowColor, fullColor, ratio);

        if (hideWhenFull)
            fillPivot.parent.gameObject.SetActive(ratio < 1f);
    }
}
