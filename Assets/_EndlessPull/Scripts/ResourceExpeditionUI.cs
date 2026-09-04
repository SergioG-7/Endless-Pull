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

    [Tooltip("Alto de cada fila de destino, en píxeles de UI.")]
    [SerializeField] private float rowHeight = UITheme.MinTouchTarget;

    [Tooltip("Tamaño del modal; el mismo rango que el resto de paneles del juego.")]
    [SerializeField] private Vector2 size = new Vector2(1100f, 700f);

    private static readonly ResourceExpeditionType[] Types =
        (ResourceExpeditionType[])System.Enum.GetValues(typeof(ResourceExpeditionType));

    private GameObject panel;
    private RectTransform content;
    private TMP_Text title;
    private TMP_Text bonusInfo;
    private TMP_Text status;
    private readonly Button[] buttons = new Button[Types.Length];
    private readonly TMP_Text[] buttonLabels = new TMP_Text[Types.Length];
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
            buttons[i].targetGraphic.color = !notBusy ? UITheme.Neutral
                : (isBonus ? UITheme.Amber : UITheme.Teal);
        }
    }

    private static string DestinationName(ResourceExpeditionType type) => ResourceExpeditionManager.DisplayName(type);
    private static string RewardName(ResourceExpeditionType type) => ResourceExpeditionManager.RewardName(type);

    private void Build()
    {
        if (canvas == null) return;

        // Mismo constructor que el resto de modales: fondo del tema, esquinas redondeadas y
        // aspa arriba a la derecha, en vez del Image plano con "Cerrar" morado abajo de antes.
        panel = UIBuild.Panel(canvas.transform, "ExpeditionPanel", size, UITheme.Bg);

        title = UIBuild.TopLabel(panel.transform, "Title", UITheme.SizeTitle, 34f, -20f,
            TextAlignmentOptions.Left);

        bonusInfo = UIBuild.TopLabel(panel.transform, "BonusInfo", UITheme.SizeBody, 28f, -60f,
            TextAlignmentOptions.Left);
        bonusInfo.color = UITheme.TextMuted;

        status = UIBuild.TopLabel(panel.transform, "Status", UITheme.SizeBody, 28f, -90f,
            TextAlignmentOptions.Left);
        status.color = UITheme.TextMuted;

        // Reclamar: solo aparece con cosecha pendiente, con el acento del tema.
        claimButton = UIBuild.Button(panel.transform, "Btn_ClaimExpedition", string.Empty,
            UITheme.AccentPick, new Vector2(size.x - 40f, rowHeight), new Vector2(0f, -126f),
            OnClaimPressed);
        claimLabel = claimButton.GetComponentInChildren<TMP_Text>();
        claimButton.gameObject.SetActive(false);

        var viewGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image),
                                    typeof(Mask), typeof(ScrollRect));
        viewGo.transform.SetParent(panel.transform, false);
        UITheme.Surface(viewGo, UITheme.BgPanel, UITheme.BorderSoft, UITheme.RadiusCard);
        viewGo.GetComponent<Mask>().showMaskGraphic = true;

        var vrt = viewGo.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = new Vector2(20f, 20f);
        vrt.offsetMax = new Vector2(-20f, -226f);

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewGo.transform, false);
        content = contentGo.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 100f);

        var layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewGo.GetComponent<ScrollRect>();
        scroll.viewport = vrt;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.scrollSensitivity = 34f;

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

            var image = UITheme.Surface(go, UITheme.Teal, UITheme.BorderSoft, UITheme.RadiusItem);

            var label = UIBuild.Label(go.transform, "Label", UITheme.SizeName, TextAlignmentOptions.Center);
            UIBuild.Stretch(label.rectTransform);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => AudioManager.Play(SfxId.UiClick));
            button.onClick.AddListener(() => OnDestinationPressed(type));
            ButtonPressFeedback.Attach(go);

            buttons[i] = button;
            buttonLabels[i] = label;
        }

        UIBuild.CloseButtonTopRight(panel.transform, Close);
    }
}
