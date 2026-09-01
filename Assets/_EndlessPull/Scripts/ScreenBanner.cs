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
    private TMP_Text compactLabel;

    private float timer;
    private float compactTimer;


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
        if (timer > 0f)
        {
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f && label != null) label.gameObject.SetActive(false);
        }

        if (compactTimer > 0f)
        {
            compactTimer -= Time.unscaledDeltaTime;
            if (compactTimer <= 0f && compactLabel != null) compactLabel.gameObject.SetActive(false);
        }
    }

    public static void Show(string text, float seconds, Color color)
    {
        if (instance != null) instance.Display(text, seconds, color);
    }


    // Aviso compacto arriba de pantalla, para mensajes largos que no caben en el rótulo
    // dramático de combate (fuente enorme, pensada solo para "3", "¡Lucha!", etc.).
    public static void ShowCompact(string text, float seconds, Color color)
    {
        if (instance != null) instance.DisplayCompact(text, seconds, color);
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

        // Banner compacto arriba de pantalla: fuente pequeña, para mensajes largos tipo toast.
        var goCompact = new GameObject("ScreenBanner_CompactLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        goCompact.transform.SetParent(canvas.transform, false);

        var rtCompact = goCompact.GetComponent<RectTransform>();
        rtCompact.anchorMin = new Vector2(0.5f, 1f);
        rtCompact.anchorMax = new Vector2(0.5f, 1f);
        rtCompact.pivot = new Vector2(0.5f, 1f);
        rtCompact.sizeDelta = new Vector2(720f, 40f);
        rtCompact.anchoredPosition = new Vector2(0f, -160f);

        compactLabel = goCompact.GetComponent<TextMeshProUGUI>();
        compactLabel.fontSize = fontSize * 0.28f;
        compactLabel.alignment = TextAlignmentOptions.Center;
        compactLabel.raycastTarget = false;
        compactLabel.fontStyle = FontStyles.Bold;
        goCompact.SetActive(false);
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


    private void DisplayCompact(string text, float seconds, Color color)
    {
        if (compactLabel == null) return;

        compactLabel.text = text;
        compactLabel.color = color;
        compactLabel.transform.SetAsLastSibling();
        compactLabel.gameObject.SetActive(true);
        compactTimer = Mathf.Max(0.1f, seconds);
    }

}
