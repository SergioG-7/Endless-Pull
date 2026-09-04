using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Minijuego de martillo de la forja: un cursor recorre la barra de lado a lado y hay que
// pararlo dentro de la zona crítica. Es opcional; acertar añade un afijo a la pieza recién
// forjada, y fallar no quita nada: la pieza sale igual, solo sin bonus.
public class HammerMinigameUI : MonoBehaviour
{
    [Tooltip("Canvas donde se monta el panel; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Forja que decide y escribe el afijo si el golpe acierta.")]
    [SerializeField] private CraftingManager crafting;

    [Tooltip("Tamaño del modal.")]
    [SerializeField] private Vector2 size = new Vector2(880f, 420f);

    [Tooltip("Ancho de la barra de golpeo, en unidades de canvas.")]
    [SerializeField] private float barWidth = 760f;

    [Tooltip("Recorridos completos de la barra por segundo.")]
    [SerializeField] private float speed = 0.85f;

    [Tooltip("Mitad del ancho de la zona crítica, en tanto por uno de la barra.")]
    [Range(0.02f, 0.30f)]
    [SerializeField] private float critHalfWidth = 0.09f;

    private GameObject panel;
    private TMP_Text titulo;
    private TMP_Text ayuda;
    private TMP_Text resultado;
    private RectTransform cursor;
    private RectTransform zonaCritica;
    private Button botonGolpe;
    private TMP_Text etiquetaGolpe;

    private EquipmentInstance pieza;
    private System.Action alCerrar;

    private bool corriendo;
    private float recorrido;
    private float centroCritico = 0.5f;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();

        Build();
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    void OnEnable() => LocalizationManager.LanguageChanged += OnLanguageChanged;
    void OnDisable() => LocalizationManager.LanguageChanged -= OnLanguageChanged;

    private void OnLanguageChanged()
    {
        if (IsOpen) RefreshTexts();
    }

    // Con unscaledDeltaTime: el modal se abre sobre la base y no depende de timeScale.
    void Update()
    {
        if (!corriendo) return;

        recorrido += Time.unscaledDeltaTime * speed;

        // PingPong da el vaivén sin tener que llevar la dirección a mano.
        float t = Mathf.PingPong(recorrido, 1f);
        cursor.anchoredPosition = new Vector2((t - 0.5f) * barWidth, 0f);
    }

    // pieza es la copia recién forjada; alCerrar devuelve el foco a la Forja.
    public void Show(EquipmentInstance target, System.Action onClose)
    {
        if (panel == null || target == null || !target.IsValid) return;

        pieza = target;
        alCerrar = onClose;

        // La zona crítica cambia de sitio cada vez: si no, se acierta de memoria.
        centroCritico = Random.Range(0.22f, 0.78f);
        zonaCritica.anchoredPosition = new Vector2((centroCritico - 0.5f) * barWidth, 0f);
        zonaCritica.sizeDelta = new Vector2(critHalfWidth * 2f * barWidth, 54f);

        recorrido = Random.value;
        corriendo = true;

        resultado.text = string.Empty;
        botonGolpe.interactable = true;

        RefreshTexts();
        UIManager.OpenExclusive(panel);
    }

    public void Close()
    {
        corriendo = false;
        pieza = null;

        if (panel != null) panel.SetActive(false);

        var volver = alCerrar;
        alCerrar = null;
        volver?.Invoke();
    }

    private void RefreshTexts()
    {
        titulo.text = LocalizationManager.Get("UI_HAMMER_TITLE");
        ayuda.text = LocalizationManager.Get("UI_HAMMER_HELP");
        etiquetaGolpe.text = LocalizationManager.Get("UI_HAMMER_STRIKE");
    }

    private void OnStrikePressed()
    {
        if (!corriendo || pieza == null) return;

        corriendo = false;
        botonGolpe.interactable = false;

        float t = Mathf.PingPong(recorrido, 1f);
        float distancia = Mathf.Abs(t - centroCritico);

        if (distancia > critHalfWidth)
        {
            resultado.text = LocalizationManager.Get("UI_HAMMER_MISS");
            resultado.color = UITheme.TextMuted;
            AudioManager.Play(SfxId.UiClose);
            return;
        }

        // Cuanto más al centro, mejor el afijo: justo en el borde vale poco, en el centro todo.
        float calidad = 1f - distancia / critHalfWidth;

        if (crafting != null && crafting.ApplyForgedAffix(pieza, calidad))
        {
            resultado.text = string.Format(LocalizationManager.Get("UI_HAMMER_HIT"),
                pieza.ForgedAffixLabel());
            resultado.color = UITheme.Text;
            AudioManager.Play(SfxId.CraftSuccess);
            return;
        }

        resultado.text = LocalizationManager.Get("UI_HAMMER_MISS");
        resultado.color = UITheme.TextMuted;
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "HammerMinigamePanel", size, UITheme.Bg);

        titulo = UIBuild.TopLabel(panel.transform, "Title", UITheme.SizeTitle, 34f, -20f,
            TextAlignmentOptions.Left);

        ayuda = UIBuild.TopLabel(panel.transform, "Help", UITheme.SizeBody, 28f, -62f,
            TextAlignmentOptions.Left);
        ayuda.color = UITheme.TextMuted;

        // Carril de la barra.
        var carril = new GameObject("Track", typeof(RectTransform), typeof(Image));
        carril.transform.SetParent(panel.transform, false);
        UITheme.Surface(carril, UITheme.BgPanel, UITheme.BorderSoft, UITheme.RadiusPill);

        var trt = carril.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 1f);
        trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(barWidth, 54f);
        trt.anchoredPosition = new Vector2(0f, -150f);

        // Zona crítica, dentro del carril.
        var zona = new GameObject("CritZone", typeof(RectTransform), typeof(Image));
        zona.transform.SetParent(carril.transform, false);
        UITheme.Surface(zona, UITheme.Amber, UITheme.BorderSoft, UITheme.RadiusItem);
        zonaCritica = zona.GetComponent<RectTransform>();
        zonaCritica.anchorMin = new Vector2(0.5f, 0.5f);
        zonaCritica.anchorMax = new Vector2(0.5f, 0.5f);
        zonaCritica.pivot = new Vector2(0.5f, 0.5f);

        // Cursor que recorre el carril.
        var cur = new GameObject("Cursor", typeof(RectTransform), typeof(Image));
        cur.transform.SetParent(carril.transform, false);
        UITheme.Surface(cur, UITheme.Accent, UITheme.BorderStrong, UITheme.RadiusItem);
        cursor = cur.GetComponent<RectTransform>();
        cursor.anchorMin = new Vector2(0.5f, 0.5f);
        cursor.anchorMax = new Vector2(0.5f, 0.5f);
        cursor.pivot = new Vector2(0.5f, 0.5f);
        cursor.sizeDelta = new Vector2(10f, 66f);

        resultado = UIBuild.TopLabel(panel.transform, "Result", UITheme.SizeName, 34f, -210f,
            TextAlignmentOptions.Center);

        botonGolpe = UIBuild.Button(panel.transform, "Btn_Strike", string.Empty,
            UITheme.AccentPick, new Vector2(320f, 64f), new Vector2(0f, -262f), OnStrikePressed);
        etiquetaGolpe = botonGolpe.GetComponentInChildren<TMP_Text>();

        UIBuild.CloseButtonTopRight(panel.transform, Close);
    }
}
