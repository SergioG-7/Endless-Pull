using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Barra de vida del jefe, grande y centrada arriba del HUD. Se monta sola sobre el Canvas
// y se engancha al HealthChanged del jefe activo de WaveManager.CurrentBoss.
public class BossHealthBarUI : MonoBehaviour
{
    [Tooltip("Gestor de oleadas: de él sale el jefe activo (WaveManager.CurrentBoss).")]
    [SerializeField] private WaveManager waves;

    [Tooltip("Canvas donde se monta la barra; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Tamaño del panel completo (nombre + barra).")]
    [SerializeField] private Vector2 panelSize = new Vector2(560f, 66f);

    [Tooltip("Tamaño de la barra de vida en sí.")]
    [SerializeField] private Vector2 barSize = new Vector2(560f, 28f);

    [Tooltip("Posición del panel, anclado arriba y centrado. Por debajo del TopBar (que ocupa hasta y=-104) para no solaparse con los recursos ni el botón de menú.")]
    [SerializeField] private Vector2 panelPosition = new Vector2(0f, -120f);

    private RectTransform root;
    private Image fill;
    private TMP_Text nameLabel;
    private TMP_Text hpLabel;
    private EnemyController bound;

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

        var boss = waves.CurrentBoss;
        if (boss != bound) Rebind(boss);

        bool show = boss != null;
        if (root.gameObject.activeSelf != show) root.gameObject.SetActive(show);
    }

    private void Rebind(EnemyController boss)
    {
        Unbind();

        bound = boss;
        if (bound == null) return;

        bound.HealthChanged += OnHealthChanged;
        if (nameLabel != null) nameLabel.text = bound.Data != null ? bound.Data.enemyName : string.Empty;
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

        var go = new GameObject("BossHealthBar_Panel", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        root = go.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = panelSize;
        root.anchoredPosition = panelPosition;

        nameLabel = UIBuild.TopLabel(root, "BossName", UITheme.SizeName, 22f, 0f, TextAlignmentOptions.Center);

        fill = UIBuild.Bar(root, "HpBar", barSize, new Vector2(0f, -26f), UITheme.BarHP, out hpLabel);
        hpLabel.alignment = TextAlignmentOptions.Center;

        root.gameObject.SetActive(false);
    }
}
