using TMPro;
using UnityEngine;

// Rótulo grande centrado en pantalla para los avisos de combate.
public class ScreenBanner : MonoBehaviour
{
    [Tooltip("Canvas donde se monta el rótulo; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Tamaño de letra del aviso.")]
    [SerializeField] private float fontSize = 96f;

    private static ScreenBanner instance;
    private TMP_Text label;
    private float timer;

    void Awake()
    {
        instance = this;
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        Build();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Corre con unscaledDeltaTime: el aviso tiene que verse aunque el juego esté en pausa.
    void Update()
    {
        if (timer <= 0f) return;

        timer -= Time.unscaledDeltaTime;
        if (timer <= 0f && label != null) label.gameObject.SetActive(false);
    }

    public static void Show(string text, float seconds, Color color)
    {
        if (instance != null) instance.Display(text, seconds, color);
    }

    private void Build()
    {
        if (canvas == null) return;

        var go = new GameObject("ScreenBanner_Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(canvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.offsetMin = new Vector2(0f, -90f);
        rt.offsetMax = new Vector2(0f, 90f);

        label = go.GetComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.fontStyle = FontStyles.Bold;
        go.SetActive(false);
    }

    private void Display(string text, float seconds, Color color)
    {
        if (label == null) return;

        label.text = text;
        label.color = color;
        label.transform.SetAsLastSibling();
        label.gameObject.SetActive(true);
        timer = Mathf.Max(0.1f, seconds);
    }
}
