using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Modal de victoria: cofre animado + desglose de botín. Se dispara con WaveManager.FloorCleared,
// que ya trae el desglose exacto (no vuelve a calcular nada de economía).
public class TowerRewardUI : MonoBehaviour
{
    [Tooltip("Gestor de oleadas: dispara el desglose de recompensa al superar un piso.")]
    [SerializeField] private WaveManager waves;

    [Tooltip("Canvas donde se monta el modal; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Tamaño del panel del modal.")]
    [SerializeField] private Vector2 panelSize = new Vector2(420f, 440f);

    [Tooltip("Segundos que tarda el cofre en abrirse (rebote de escala).")]
    [SerializeField] private float chestPopSeconds = 0.35f;

    private RectTransform backdrop;
    private RectTransform panel;
    private TMP_Text titleLabel;
    private RectTransform chestIcon;
    private TMP_Text gemsLine;
    private TMP_Text materialsLine;
    private TMP_Text expLine;

    void Awake()
    {
        if (waves == null) waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        Build();
    }

    void OnEnable()
    {
        if (waves != null) waves.FloorCleared += OnFloorCleared;
    }

    void OnDisable()
    {
        if (waves != null) waves.FloorCleared -= OnFloorCleared;
    }

    private void OnFloorCleared(FloorRewardInfo info)
    {
        if (backdrop == null) return;

        titleLabel.text = info.bossFloor
            ? LocalizationManager.Get("UI_BOSS_CHEST_TITLE")
            : LocalizationManager.Get("UI_CHEST_TITLE");

        gemsLine.text = $"<color={UITheme.Tag(UITheme.Cyan)}>◆</color> {LocalizationManager.Get("UI_GEMS")}   <b>+{info.gems}</b>";
        materialsLine.text =
            $"<color={UITheme.Tag(UITheme.Hex("A8895C"))}>■</color> {LocalizationManager.Get("UI_WOOD")}   <b>+{info.wood}</b>   " +
            $"<color={UITheme.Tag(UITheme.Hex("9397AB"))}>■</color> {LocalizationManager.Get("UI_IRON")}   <b>+{info.iron}</b>";
        expLine.text = $"<color={UITheme.Tag(UITheme.Accent)}>★</color> {LocalizationManager.Get("UI_EXP_GAINED")}   <b>+{info.exp}</b>";

        backdrop.gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(PopChest());
    }

    private IEnumerator PopChest()
    {
        chestIcon.localScale = Vector3.one * 0.4f;

        float elapsed = 0f;
        while (elapsed < chestPopSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / chestPopSeconds);

            // Rebote simple: pasa de largo del 1 y vuelve, sin traer un tween externo.
            float overshoot = Mathf.Sin(t * Mathf.PI * 0.5f);
            float scale = Mathf.Lerp(0.4f, 1.08f, overshoot) - (t >= 1f ? 0.08f : 0f);
            chestIcon.localScale = Vector3.one * scale;
            yield return null;
        }

        chestIcon.localScale = Vector3.one;
    }

    private void Close()
    {
        if (backdrop != null) backdrop.gameObject.SetActive(false);
    }

    private void Build()
    {
        if (canvas == null) return;

        var backdropGo = new GameObject("TowerReward_Backdrop", typeof(RectTransform), typeof(Image));
        backdropGo.transform.SetParent(canvas.transform, false);
        backdrop = backdropGo.GetComponent<RectTransform>();
        UIBuild.Stretch(backdrop);
        var backdropImage = backdropGo.GetComponent<Image>();
        backdropImage.color = UITheme.Backdrop;

        var panelGo = UIBuild.Panel(backdrop, "TowerReward_Panel", panelSize, UITheme.Card);
        panel = panelGo.GetComponent<RectTransform>();

        titleLabel = UIBuild.TopLabel(panel, "Title", UITheme.SizeTitle, 32f, -16f, TextAlignmentOptions.Center);

        // Sin sprite de cofre real (generación IA no disponible en este proyecto): silueta
        // armada con 3 piezas en vez del cuadrado liso de antes.
        var chestGo = new GameObject("ChestIcon", typeof(RectTransform));
        chestGo.transform.SetParent(panel, false);
        chestIcon = chestGo.GetComponent<RectTransform>();
        chestIcon.anchorMin = new Vector2(0.5f, 0.5f);
        chestIcon.anchorMax = new Vector2(0.5f, 0.5f);
        chestIcon.pivot = new Vector2(0.5f, 0.5f);
        chestIcon.sizeDelta = new Vector2(120f, 120f);
        chestIcon.anchoredPosition = new Vector2(0f, 48f);

        ChestPiece(chestIcon, "Body", new Vector2(110f, 70f), new Vector2(0f, -15f), UITheme.Hex("D9A441"));
        ChestPiece(chestIcon, "Lid", new Vector2(100f, 40f), new Vector2(0f, 35f), UITheme.Hex("E8C169"));
        ChestPiece(chestIcon, "Seam", new Vector2(114f, 8f), new Vector2(0f, 5f), UITheme.Hex("8A5A22"));
        ChestPiece(chestIcon, "Lock", new Vector2(20f, 20f), new Vector2(0f, 5f), UITheme.Hex("5A3A1A"));

        gemsLine = MakeLine(panel, "GemsLine", -18f);
        materialsLine = MakeLine(panel, "MaterialsLine", -50f);
        expLine = MakeLine(panel, "ExpLine", -82f);

        UIBuild.Button(panel, "ContinueButton", LocalizationManager.Get("UI_CHEST_CONTINUE"),
            UITheme.Neutral, new Vector2(200f, 48f), new Vector2(0f, -panelSize.y + 64f), Close);

        backdrop.gameObject.SetActive(false);
    }

    private static void ChestPiece(Transform parent, string name, Vector2 size, Vector2 pos, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var image = go.GetComponent<Image>();
        image.sprite = UITheme.Rounded;
        image.type = Image.Type.Sliced;
        image.color = color;
    }

    private static TMP_Text MakeLine(Transform parent, string name, float y)
    {
        var label = UIBuild.Label(parent, name, UITheme.SizeBody, TextAlignmentOptions.Center);
        var rt = label.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(380f, 28f);
        // +170 (antes +130): con +130 la línea de EXP quedaba con el botón Continuar, que se
        // ancla a un offset fijo desde el borde inferior independiente del alto del panel.
        rt.anchoredPosition = new Vector2(0f, y + 170f);
        return label;
    }
}
