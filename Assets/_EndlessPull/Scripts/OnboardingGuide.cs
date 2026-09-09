using TMPro;
using UnityEngine;

// Cartel de arranque de una partida nueva y flechas que señalan a dónde ir primero.
// Solo sale una vez: el flag viaja en el guardado y se reinicia al borrar la partida.
public class OnboardingGuide : MonoBehaviour
{
    private enum Step { Waiting, Modal, Guiding, Done }

    [Tooltip("Canvas donde se monta el cartel; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Tamaño del cartel.")]
    [SerializeField] private Vector2 size = new Vector2(880f, 420f);

    [Tooltip("Menú principal; mientras esté abierto no se enseña nada.")]
    [SerializeField] private MainMenuUI mainMenu;

    [Tooltip("Altura de la flecha sobre el edificio señalado.")]
    [SerializeField] private float arrowHeight = 3.2f;

    [Tooltip("Recorrido del rebote de la flecha.")]
    [SerializeField] private float arrowBob = 0.35f;

    [Tooltip("Segundos entre comprobaciones del paso actual.")]
    [SerializeField] private float refreshSeconds = 0.5f;

    // Lo lee y lo escribe SaveManager; estático porque el guardado no conoce esta instancia.
    public static bool Seen;

    private GameObject panel;
    private TMP_Text titulo;
    private TMP_Text cuerpo;
    private TMP_Text botonTexto;

    // Las dos flechas se enseñan a la vez: el Altar y el Portal son los dos sitios que hay que
    // tocar en los primeros minutos, y no hay un orden obligatorio entre ellos.
    private Arrow altarArrow;
    private Arrow portalArrow;

    private Step step = Step.Waiting;
    private float timer;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (mainMenu == null) mainMenu = UnityEngine.Object.FindFirstObjectByType<MainMenuUI>();
    }

    void OnEnable() => LocalizationManager.LanguageChanged += Retranslate;
    void OnDisable() => LocalizationManager.LanguageChanged -= Retranslate;

    void Update()
    {
        if (step == Step.Done) return;

        altarArrow?.Bob(arrowBob);
        portalArrow?.Bob(arrowBob);

        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = refreshSeconds;

        if (step == Step.Waiting) TickWaiting();
        else if (step == Step.Guiding) TickGuiding();
    }

    // Se espera a que el menú principal se aparte: hasta entonces no se sabe si hay partida.
    private void TickWaiting()
    {
        if (Seen) { step = Step.Done; return; }
        if (!SaveManager.SavingAllowed) return;
        if (mainMenu != null && mainMenu.IsOpen) return;

        Open();
    }

    private void TickGuiding()
    {
        var waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();

        // En cuanto pisa la Torre el tutorial sobra: ya sabe por dónde se entra.
        if (waves != null && (waves.State != ExpeditionState.Idle || waves.HighestClearedFloor > 0))
        {
            Finish();
            return;
        }

        if (SummonAltar.Exists)
        {
            altarArrow ??= new Arrow(transform, arrowHeight);
            altarArrow.PointAt(SummonAltar.AltarPosition, "UI_ONBOARD_STEP_SUMMON");
        }

        portalArrow ??= new Arrow(transform, arrowHeight);
        portalArrow.PointAt(TowerGateway.Position, "UI_ONBOARD_STEP_PORTAL");
    }

    private void Open()
    {
        if (panel == null) Build();
        if (panel == null) { step = Step.Done; return; }

        Retranslate();
        panel.SetActive(true);
        step = Step.Modal;
    }

    private void OnAccept()
    {
        if (panel != null) panel.SetActive(false);
        step = Step.Guiding;
        TickGuiding();
    }

    private void Finish()
    {
        altarArrow?.Hide();
        portalArrow?.Hide();
        step = Step.Done;

        if (Seen) return;

        Seen = true;
        SaveManager.RequestSave();
    }

    private void Retranslate()
    {
        if (titulo != null) titulo.text = LocalizationManager.Get("UI_ONBOARD_TITLE");
        if (cuerpo != null) cuerpo.text = LocalizationManager.Get("UI_ONBOARD_BODY");
        if (botonTexto != null) botonTexto.text = LocalizationManager.Get("UI_ONBOARD_OK");
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "OnboardingGuide", size, UITheme.BgPanel);
        panel.SetActive(false);

        titulo = UIBuild.TopLabel(panel.transform, "Title", UIBuild.TitleSize, 60f, -24f,
            TextAlignmentOptions.Center);

        cuerpo = UIBuild.TopLabel(panel.transform, "Body", UIBuild.BodySize, 220f, -100f,
            TextAlignmentOptions.TopLeft);

        var boton = UIBuild.Button(panel.transform, "Btn_Ok", string.Empty, UITheme.Accent,
            new Vector2(260f, 64f), new Vector2(0f, -336f), OnAccept);

        botonTexto = boton.GetComponentInChildren<TMP_Text>();
    }

    // Flecha de mundo con su rótulo, montada por código para no cablear nada en escena.
    private class Arrow
    {
        private readonly Transform root;
        private readonly TextMeshPro caption;
        private readonly float height;
        private Vector3 anchor;

        public Arrow(Transform parent, float height)
        {
            this.height = height;

            var go = new GameObject("OnboardingArrow", typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            root = go.transform;

            var label = go.GetComponent<TextMeshPro>();
            label.text = "▼";
            label.fontSize = 8f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = UITheme.BarMorale;
            label.rectTransform.sizeDelta = new Vector2(6f, 3f);

            // Sin capa se queda en Default, que va detrás del fondo pintado.
            label.sortingLayerID = SortingLayer.NameToID(YSorter.CombatLayer);
            label.sortingOrder = YSorter.AboveUnitsOrder + 2000;

            var capGo = new GameObject("Caption", typeof(TextMeshPro));
            capGo.transform.SetParent(root, false);
            capGo.transform.localPosition = new Vector3(0f, 1.1f, 0f);

            caption = capGo.GetComponent<TextMeshPro>();
            caption.fontSize = 3.2f;
            caption.alignment = TextAlignmentOptions.Center;
            caption.color = UITheme.BarMorale;
            caption.rectTransform.sizeDelta = new Vector2(14f, 1.4f);
            caption.sortingLayerID = label.sortingLayerID;
            caption.sortingOrder = label.sortingOrder;
        }

        public void PointAt(Vector2 target, string captionKey)
        {
            anchor = new Vector3(target.x, target.y + height, 0f);
            root.position = anchor;
            caption.text = LocalizationManager.Get(captionKey);

            if (!root.gameObject.activeSelf) root.gameObject.SetActive(true);
        }

        // El rebote se recalcula desde el anclaje, no se acumula sobre la posición anterior.
        public void Bob(float amount)
        {
            if (!root.gameObject.activeSelf) return;

            root.position = anchor + Vector3.up * (Mathf.Sin(Time.unscaledTime * 3f) * amount);
        }

        public void Hide() => root.gameObject.SetActive(false);
    }
}
