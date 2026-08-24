using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Selector de torre: un botón por piso, hasta el siguiente al más alto superado.
public class TowerPanelUI : MonoBehaviour
{
    [Tooltip("Canvas donde se monta el panel; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Gestor de oleadas al que se le pide el piso y la expedición.")]
    [SerializeField] private WaveManager waves;

    [Tooltip("Escuadra de la que salen los intentos de torre.")]
    [SerializeField] private PartyManager party;

    [Tooltip("Alto de cada fila de piso, en píxeles de UI.")]
    [SerializeField] private float rowHeight = 64f;

    [Tooltip("Color de un piso ya superado, que solo se puede repetir.")]
    [SerializeField] private Color clearedColor = new Color(0.22f, 0.42f, 0.30f);

    [Tooltip("Color del piso nuevo, el único que da gemas.")]
    [SerializeField] private Color freshColor = new Color(0.55f, 0.42f, 0.15f);

    private GameObject panel;
    private RectTransform content;
    private TMP_Text title;
    private TMP_Text info;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (waves == null) waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
        if (party == null) party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();

        Build();
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (panel == null) return;

        UIManager.OpenExclusive(panel);
        Rebuild();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Rebuild()
    {
        if (content == null || waves == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        title.text = LocalizationManager.Get("UI_TOWER");
        info.text = LocalizationManager.Get("UI_ATTEMPTS") + ": "
                  + (party != null ? party.Energy + "/" + party.MaxEnergy : "-");

        int top = waves.HighestSelectableFloor;
        content.sizeDelta = new Vector2(content.sizeDelta.x, top * (rowHeight + 6f) + 12f);

        for (int floor = top; floor >= 1; floor--)
            CreateRow(floor);
    }

    private void CreateRow(int floor)
    {
        bool cleared = floor <= waves.HighestClearedFloor;

        string modo = cleared
            ? LocalizationManager.Get("UI_REPEAT")
            : LocalizationManager.Get("UI_FIRST_CLEAR");

        string text = LocalizationManager.Get("UI_FLOOR") + " " + floor + "   -   " + modo
                    + (cleared ? "   (0 " + LocalizationManager.Get("UI_GEMS") + ")" : string.Empty);

        var go = new GameObject("Btn_Floor_" + floor, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(content, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, rowHeight);

        var image = go.GetComponent<Image>();
        image.color = cleared ? clearedColor : freshColor;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = text;

        int chosen = floor;
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => OnFloorPressed(chosen));
    }

    // Elegir piso y salir en la misma pulsación: el panel se cierra para dejar ver el combate.
    private void OnFloorPressed(int floor)
    {
        if (waves == null || !waves.SelectFloor(floor)) return;

        Close();
        UIManager.CloseEverything();
        waves.StartFloorExpedition();
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = new GameObject("TowerPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(900f, 640f);
        prt.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.16f, 0.98f);

        title = NewLabel(panel.transform, "Title", 44f);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.sizeDelta = new Vector2(0f, 70f);
        title.rectTransform.anchoredPosition = Vector2.zero;

        info = NewLabel(panel.transform, "Info", 26f);
        info.rectTransform.anchorMin = new Vector2(0f, 1f);
        info.rectTransform.anchorMax = new Vector2(1f, 1f);
        info.rectTransform.pivot = new Vector2(0.5f, 1f);
        info.rectTransform.sizeDelta = new Vector2(0f, 40f);
        info.rectTransform.anchoredPosition = new Vector2(0f, -70f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image),
                                      typeof(Mask), typeof(ScrollRect));
        viewport.transform.SetParent(panel.transform, false);
        var vrt = viewport.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0f, 0f);
        vrt.anchorMax = new Vector2(1f, 1f);
        vrt.offsetMin = new Vector2(12f, 90f);
        vrt.offsetMax = new Vector2(-12f, -114f);
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
        viewport.GetComponent<Mask>().showMaskGraphic = true;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewport.transform, false);
        content = contentGo.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 100f);

        var layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewport.GetComponent<ScrollRect>();
        scroll.viewport = vrt;
        scroll.content = content;
        scroll.horizontal = false;

        var close = new GameObject("Btn_CloseTower", typeof(RectTransform), typeof(Image), typeof(Button));
        close.transform.SetParent(panel.transform, false);
        var crt = close.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0f);
        crt.anchorMax = new Vector2(0.5f, 0f);
        crt.pivot = new Vector2(0.5f, 0f);
        crt.sizeDelta = new Vector2(280f, 66f);
        crt.anchoredPosition = new Vector2(0f, 12f);
        close.GetComponent<Image>().color = new Color(0.32f, 0.28f, 0.36f);

        var closeLabel = NewLabel(close.transform, "Label", 26f);
        closeLabel.rectTransform.anchorMin = Vector2.zero;
        closeLabel.rectTransform.anchorMax = Vector2.one;
        closeLabel.rectTransform.offsetMin = Vector2.zero;
        closeLabel.rectTransform.offsetMax = Vector2.zero;
        closeLabel.text = LocalizationManager.Get("UI_CLOSE");

        var closeButton = close.GetComponent<Button>();
        closeButton.targetGraphic = close.GetComponent<Image>();
        closeButton.onClick.AddListener(Close);
    }

    private static TMP_Text NewLabel(Transform parent, string name, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }
}
