using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Barra de vida de la NPC de escolta (Friacis), debajo de la del jefe para no solaparse
// cuando ambas coinciden (no pasa hoy, pero deja hueco por si un piso futuro las junta).
// Mismo patrón que BossHealthBarUI: se monta sola y se engancha a WaveManager.CurrentEscort.
public class EscortHealthBarUI : MonoBehaviour
{
    [Tooltip("Gestor de oleadas: de él sale la NPC de escolta activa (WaveManager.CurrentEscort).")]
    [SerializeField] private WaveManager waves;

    [Tooltip("Canvas donde se monta la barra; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Tamaño del panel completo (nombre + barra).")]
    [SerializeField] private Vector2 panelSize = new Vector2(420f, 60f);

    [Tooltip("Tamaño de la barra de vida en sí.")]
    [SerializeField] private Vector2 barSize = new Vector2(420f, 22f);

    [Tooltip("Posición del panel, anclado arriba y centrado. Por debajo de BossHealthBar_Panel (hasta y=-246) para no solaparse con ella.")]
    [SerializeField] private Vector2 panelPosition = new Vector2(0f, -256f);

    [Tooltip("Segundos que se espera tras aparecer la escolta antes de mostrar la barra, para que dé tiempo a que la cámara llegue a la arena.")]
    [SerializeField] private float showDelay = 2.6f;

    private RectTransform root;
    private Image fill;
    private TMP_Text nameLabel;
    private TMP_Text hpLabel;
    private EscortNpc bound;
    private float boundTimer;

    void Awake()
    {
        if (waves == null) waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        Build();
    }

    void OnDisable()
    {
        Unbind();
    }

    void Update()
    {
        if (waves == null || root == null) return;

        var escort = waves.CurrentEscort;
        if (escort != bound) Rebind(escort);

        if (escort != null) boundTimer += Time.deltaTime;

        bool show = escort != null && boundTimer >= showDelay;
        if (root.gameObject.activeSelf != show) root.gameObject.SetActive(show);
    }

    private void Rebind(EscortNpc escort)
    {
        Unbind();

        bound = escort;
        boundTimer = 0f;
        if (bound == null) return;

        bound.HealthChanged += OnHealthChanged;
        if (nameLabel != null) nameLabel.text = bound.NpcName;
        OnHealthChanged(bound.CurrentHealth, bound.MaxHealth);
    }

    private void Unbind()
    {
        if (bound != null) bound.HealthChanged -= OnHealthChanged;
        bound = null;
    }

    private void OnHealthChanged(int current, int max)
    {
        UIBuild.SetBar(fill, max > 0 ? (float)current / max : 0f);
        if (hpLabel != null) hpLabel.text = $"{current}/{max}";
    }

    private void Build()
    {
        if (canvas == null) return;

        var go = new GameObject("EscortHealthBar_Panel", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        root = go.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = panelSize;
        root.anchoredPosition = panelPosition;

        nameLabel = UIBuild.TopLabel(root, "EscortName", UITheme.SizeName, 20f, 0f, TextAlignmentOptions.Center);

        fill = UIBuild.Bar(root, "HpBar", barSize, new Vector2(0f, -24f), UITheme.BarHP, out hpLabel);
        hpLabel.alignment = TextAlignmentOptions.Center;

        root.gameObject.SetActive(false);
    }
}
