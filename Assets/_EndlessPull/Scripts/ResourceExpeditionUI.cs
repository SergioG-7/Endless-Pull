using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panel de recolección: un destino por fila (scrollable, mismo patrón que TowerPanelUI) para
// que añadir nuevos destinos a la rotación no rompa el layout, con su recurso y el bono diario.
public class ResourceExpeditionUI : MonoBehaviour
{
    [Tooltip("Canvas donde se monta el panel; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Gestor que lleva la cuenta atrás y reparte la cosecha.")]
    [SerializeField] private ResourceExpeditionManager expeditions;

    [Tooltip("Escuadra de la que salen los intentos de torre.")]
    [SerializeField] private PartyManager party;

    [Tooltip("Modal de escuadra: confirma quién sale antes de arrancar la expedición.")]
    [SerializeField] private SquadManagementUI squadUI;

    [Tooltip("Color de un destino disponible.")]
    [SerializeField] private Color readyColor = new Color(0.25f, 0.45f, 0.35f);

    [Tooltip("Color de un destino disponible que además da el bono del día.")]
    [SerializeField] private Color bonusColor = new Color(0.55f, 0.42f, 0.15f);

    [Tooltip("Color de un destino que ahora mismo no se puede elegir.")]
    [SerializeField] private Color busyColor = new Color(0.28f, 0.28f, 0.32f);

    [Tooltip("Alto de cada fila de destino, en píxeles de UI.")]
    [SerializeField] private float rowHeight = UITheme.MinTouchTarget;

    private static readonly ResourceExpeditionType[] Types =
        (ResourceExpeditionType[])System.Enum.GetValues(typeof(ResourceExpeditionType));

    private GameObject panel;
    private RectTransform content;
    private TMP_Text title;
    private TMP_Text bonusInfo;
    private TMP_Text status;
    private readonly Button[] buttons = new Button[Types.Length];
    private readonly TMP_Text[] buttonLabels = new TMP_Text[Types.Length];
    private TMP_Text closeLabel;
    private Button claimButton;
    private TMP_Text claimLabel;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (expeditions == null) expeditions = UnityEngine.Object.FindFirstObjectByType<ResourceExpeditionManager>();
        if (party == null) party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();
        if (squadUI == null) squadUI = UnityEngine.Object.FindFirstObjectByType<SquadManagementUI>();

        Build();
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    // El panel se refresca solo: la cuenta atrás corre aunque no se toque nada.
    void Update()
    {
        if (IsOpen) Refresh();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (panel == null) return;

        // Flujo idéntico a la Torre (TowerPanelUI.OnFloorPressed): este panel siempre se abre
        // primero para elegir destino, y OnDestinationPressed es el único sitio que visita la
        // confirmación de escuadra (una sola vez).
        UIManager.OpenExclusive(panel);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void OnDestinationPressed(ResourceExpeditionType type)
    {
        if (expeditions == null) return;

        if (squadUI != null)
        {
            Close();
            squadUI.OpenForConfirm(false, () => expeditions.StartExpedition(type));
            return;
        }

        // Sin modal de escuadra en escena, se mantiene el arranque directo de antes.
        expeditions.StartExpedition(type);
        Refresh();
    }

    private void OnClaimPressed()
    {
        if (expeditions == null) return;

        expeditions.ClaimReward();
        Refresh();
    }

    private void Refresh()
    {
        title.text = LocalizationManager.Get("UI_EXPEDITIONS");
        closeLabel.text = LocalizationManager.Get("UI_CLOSE");
        claimLabel.text = LocalizationManager.Get("UI_CLAIM_REWARD");

        var bonusType = ResourceExpeditionManager.TodaysBonusType();
        float bonusMult = expeditions != null ? expeditions.DailyBonusMultiplier : 1f;
        bonusInfo.text = string.Format(LocalizationManager.Get("UI_EXPEDITION_TODAY_BONUS"),
            ResourceExpeditionManager.DisplayName(bonusType), bonusMult);

        bool running = expeditions != null && expeditions.IsRunning;
        bool ready = expeditions != null && expeditions.ReadyToClaim;

        // Solo depende de si ya hay una recolección en curso, no de si la escuadra ya tiene
        // gente asignada: igual que el piso de Torre, la escuadra se revisa/completa en la
        // pantalla de confirmación que se abre justo después (OnDestinationPressed), no aquí.
        bool notBusy = expeditions != null && !expeditions.IsRunning;

        status.text = ready
            ? string.Format(LocalizationManager.Get("UI_EXPEDITION_READY"), DestinationName(expeditions.CurrentType))
            : running
                ? DestinationName(expeditions.CurrentType) + "   " + expeditions.Remaining.ToString("0") + "s"
                : party != null
                    ? LocalizationManager.Get("UI_GATHER_SQUAD") + ": " + party.ExpeditionSquad.Count + "/" + party.MaxExpeditionSize
                    : string.Empty;

        // Con recompensa lista, el hueco de destinos se convierte en el botón de reclamo.
        claimButton.gameObject.SetActive(ready);

        for (int i = 0; i < Types.Length; i++)
        {
            var type = Types[i];
            bool isBonus = type == bonusType;

            buttonLabels[i].text = DestinationName(type) + "   ->   " + RewardName(type)
                + (isBonus ? LocalizationManager.Get("UI_EXPEDITION_BONUS_TAG") : string.Empty);
            buttons[i].gameObject.SetActive(!ready);
            buttons[i].interactable = notBusy;
            buttons[i].targetGraphic.color = !notBusy ? busyColor : (isBonus ? bonusColor : readyColor);
        }
    }

    private static string DestinationName(ResourceExpeditionType type) => ResourceExpeditionManager.DisplayName(type);
    private static string RewardName(ResourceExpeditionType type) => ResourceExpeditionManager.RewardName(type);

    private void Build()
    {
        if (canvas == null) return;

        panel = new GameObject("ExpeditionPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = UITheme.ModalSize;
        prt.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.14f, 0.98f);

        title = NewLabel(panel.transform, "Title", 44f);
        Place(title.rectTransform, 1f, new Vector2(0f, 60f), new Vector2(0f, 0f));

        bonusInfo = NewLabel(panel.transform, "BonusInfo", 24f);
        Place(bonusInfo.rectTransform, 1f, new Vector2(0f, 34f), new Vector2(0f, -60f));

        status = NewLabel(panel.transform, "Status", 26f);
        Place(status.rectTransform, 1f, new Vector2(0f, 36f), new Vector2(0f, -96f));

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image),
                                      typeof(Mask), typeof(ScrollRect));
        viewport.transform.SetParent(panel.transform, false);
        var vrt = viewport.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0f, 0f);
        vrt.anchorMax = new Vector2(1f, 1f);
        vrt.offsetMin = new Vector2(12f, 96f);
        vrt.offsetMax = new Vector2(-12f, -134f);
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
        layout.spacing = 8f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewport.GetComponent<ScrollRect>();
        scroll.viewport = vrt;
        scroll.content = content;
        scroll.horizontal = false;

        for (int i = 0; i < Types.Length; i++)
        {
            var type = Types[i];

            var go = new GameObject("Btn_" + type, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(content, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, rowHeight);

            go.GetComponent<Image>().color = readyColor;

            var label = NewLabel(go.transform, "Label", 30f);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.onClick.AddListener(() => AudioManager.Play(SfxId.UiClick));
            button.onClick.AddListener(() => OnDestinationPressed(type));
            ButtonPressFeedback.Attach(go);

            buttons[i] = button;
            buttonLabels[i] = label;
        }

        var claim = new GameObject("Btn_ClaimExpedition", typeof(RectTransform), typeof(Image), typeof(Button));
        claim.transform.SetParent(panel.transform, false);
        var clrt = claim.GetComponent<RectTransform>();
        clrt.anchorMin = new Vector2(0.5f, 1f);
        clrt.anchorMax = new Vector2(0.5f, 1f);
        clrt.pivot = new Vector2(0.5f, 1f);
        clrt.sizeDelta = new Vector2(760f, rowHeight);
        clrt.anchoredPosition = new Vector2(0f, -104f);
        claim.GetComponent<Image>().color = new Color(0.55f, 0.42f, 0.15f);

        claimLabel = NewLabel(claim.transform, "Label", 30f);
        claimLabel.rectTransform.anchorMin = Vector2.zero;
        claimLabel.rectTransform.anchorMax = Vector2.one;
        claimLabel.rectTransform.offsetMin = Vector2.zero;
        claimLabel.rectTransform.offsetMax = Vector2.zero;

        claimButton = claim.GetComponent<Button>();
        claimButton.targetGraphic = claim.GetComponent<Image>();
        claimButton.onClick.AddListener(() => AudioManager.Play(SfxId.UiClick));
        claimButton.onClick.AddListener(OnClaimPressed);
        ButtonPressFeedback.Attach(claim);
        claim.SetActive(false);

        var close = new GameObject("Btn_CloseExpedition", typeof(RectTransform), typeof(Image), typeof(Button));
        close.transform.SetParent(panel.transform, false);
        var crt = close.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0f);
        crt.anchorMax = new Vector2(0.5f, 0f);
        crt.pivot = new Vector2(0.5f, 0f);
        crt.sizeDelta = new Vector2(280f, UITheme.MinTouchTarget);
        crt.anchoredPosition = new Vector2(0f, 12f);
        close.GetComponent<Image>().color = new Color(0.32f, 0.28f, 0.36f);

        closeLabel = NewLabel(close.transform, "Label", 26f);
        closeLabel.rectTransform.anchorMin = Vector2.zero;
        closeLabel.rectTransform.anchorMax = Vector2.one;
        closeLabel.rectTransform.offsetMin = Vector2.zero;
        closeLabel.rectTransform.offsetMax = Vector2.zero;

        var closeButton = close.GetComponent<Button>();
        closeButton.targetGraphic = close.GetComponent<Image>();
        closeButton.onClick.AddListener(() => AudioManager.Play(SfxId.UiClick));
        closeButton.onClick.AddListener(Close);
        ButtonPressFeedback.Attach(close);
    }

    private static void Place(RectTransform rt, float anchorY, Vector2 size, Vector2 position)
    {
        rt.anchorMin = new Vector2(0f, anchorY);
        rt.anchorMax = new Vector2(1f, anchorY);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
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
