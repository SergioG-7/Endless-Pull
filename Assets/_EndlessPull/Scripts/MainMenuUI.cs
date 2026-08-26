using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pantalla de título. Se monta a sí misma sobre el canvas, así no hay nada que cablear en escena.
public class MainMenuUI : MonoBehaviour
{
    private const string VolumeKey = "EndlessPull.MasterVolume";

    [Tooltip("Canvas donde se monta el menú; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Guardado del que salen 'Continuar' y 'Nueva Partida'.")]
    [SerializeField] private SaveManager saves;

    [Tooltip("Color de fondo de la pantalla de título.")]
    [SerializeField] private Color backgroundColor = new Color(0.06f, 0.05f, 0.10f, 1f);

    [Tooltip("Color de los botones activos.")]
    [SerializeField] private Color buttonColor = new Color(0.30f, 0.26f, 0.52f);

    [Tooltip("Color de un botón que no se puede pulsar.")]
    [SerializeField] private Color disabledColor = new Color(0.26f, 0.26f, 0.30f);

    [Tooltip("Color del idioma seleccionado.")]
    [SerializeField] private Color selectedColor = new Color(0.70f, 0.55f, 0.20f);

    private GameObject root;
    private GameObject optionsPanel;
    private Button continueButton;
    private TMP_Text continueLabel;
    private TMP_Text titleLabel;
    private TMP_Text optionsTitle;
    private TMP_Text volumeLabel;
    private Slider volumeSlider;

    private readonly List<Button> languageButtons = new List<Button>();
    private readonly Dictionary<TMP_Text, string> boundLabels = new Dictionary<TMP_Text, string>();

    public bool IsOpen => root != null && root.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (saves == null) saves = UnityEngine.Object.FindFirstObjectByType<SaveManager>();

        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        Build();
    }

    void OnEnable() => LocalizationManager.LanguageChanged += RefreshTexts;
    void OnDisable() => LocalizationManager.LanguageChanged -= RefreshTexts;

    // El menú manda desde el primer frame: el juego arranca congelado detrás.
    void Start()
    {
        Open();
    }

    public void Open()
    {
        if (root == null) return;

        root.SetActive(true);
        root.transform.SetAsLastSibling();
        optionsPanel.SetActive(false);
        // Mientras el menu manda, nadie ha elegido partida todavia: guardar pisaria el JSON de disco.
        SaveManager.SavingAllowed = false;
        Time.timeScale = 0f;

        bool hasSave = saves != null && saves.HasSave;
        continueButton.interactable = hasSave;
        continueButton.targetGraphic.color = hasSave ? buttonColor : disabledColor;

        RefreshTexts();
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
        SaveManager.SavingAllowed = true;
        Time.timeScale = 1f;
    }

    // Continuar: se carga el JSON de disco sobre la escena tal cual.
    public void OnContinuePressed()
    {
        if (saves == null || !saves.HasSave) return;

        saves.Load();
        Close();
    }

    // Nueva Partida: se borra el guardado y se juega la escena tal y como está autorizada.
    public void OnNewGamePressed()
    {
        if (saves != null) saves.DeleteSave();

        Debug.Log("[Menú] Nueva partida: base limpia con los recursos iniciales de la escena.", this);
        Close();
    }

    public void OnOptionsPressed() => optionsPanel.SetActive(true);
    public void OnOptionsBackPressed() => optionsPanel.SetActive(false);

    private void OnLanguagePressed(GameLanguage language)
    {
        LocalizationManager.SetLanguage(language);
        RefreshTexts();
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
        RefreshVolumeLabel();
    }

    // Un solo sitio donde se reescribe todo: al abrir y al cambiar de idioma.
    private void RefreshTexts()
    {
        foreach (var pair in boundLabels)
            if (pair.Key != null) pair.Key.text = LocalizationManager.Get(pair.Value);

        if (titleLabel != null) titleLabel.text = LocalizationManager.Get("UI_TITLE");

        if (continueLabel != null && saves != null && !saves.HasSave)
            continueLabel.text = LocalizationManager.Get("UI_NO_SAVE");

        for (int i = 0; i < languageButtons.Count; i++)
        {
            bool active = (int)LocalizationManager.Current == i;
            languageButtons[i].targetGraphic.color = active ? selectedColor : buttonColor;
        }

        RefreshVolumeLabel();
    }

    private void RefreshVolumeLabel()
    {
        if (volumeLabel == null || volumeSlider == null) return;

        volumeLabel.text = LocalizationManager.Get("UI_MASTER_VOLUME")
                           + "   " + Mathf.RoundToInt(volumeSlider.value * 100f) + "%";
    }

    private void Build()
    {
        if (canvas == null) return;

        root = NewPanel("MainMenu", canvas.transform, backgroundColor);

        titleLabel = NewLabel(root.transform, "Title", 110f, TextAlignmentOptions.Center);
        Stretch(titleLabel.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 0.82f));
        titleLabel.fontStyle = FontStyles.Bold;

        continueButton = NewMenuButton(root.transform, "Btn_Continue", "UI_CONTINUE", 0, OnContinuePressed);
        continueLabel = continueButton.GetComponentInChildren<TMP_Text>();
        NewMenuButton(root.transform, "Btn_NewGame", "UI_START", 1, OnNewGamePressed);
        NewMenuButton(root.transform, "Btn_Options", "UI_OPTIONS", 2, OnOptionsPressed);

        BuildOptions();
    }

    private void BuildOptions()
    {
        optionsPanel = NewPanel("OptionsPanel", root.transform, new Color(0.10f, 0.09f, 0.16f, 0.98f));

        optionsTitle = NewLabel(optionsPanel.transform, "OptionsTitle", 60f, TextAlignmentOptions.Center);
        Stretch(optionsTitle.rectTransform, new Vector2(0f, 0.78f), new Vector2(1f, 0.92f));
        boundLabels[optionsTitle] = "UI_OPTIONS";

        var langTitle = NewLabel(optionsPanel.transform, "LanguageTitle", 40f, TextAlignmentOptions.Center);
        Stretch(langTitle.rectTransform, new Vector2(0f, 0.66f), new Vector2(1f, 0.74f));
        boundLabels[langTitle] = "UI_LANGUAGE";

        // Los tres idiomas van escritos en su propio idioma, no traducidos.
        for (int i = 0; i < 3; i++)
        {
            var language = (GameLanguage)i;
            var button = NewButton(optionsPanel.transform, "Btn_Lang_" + language,
                LocalizationManager.NameOf(language), () => OnLanguagePressed(language));

            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.56f);
            rt.anchorMax = new Vector2(0.5f, 0.56f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300f, 80f);
            rt.anchoredPosition = new Vector2((i - 1) * 320f, 0f);

            languageButtons.Add(button);
        }

        volumeLabel = NewLabel(optionsPanel.transform, "VolumeLabel", 40f, TextAlignmentOptions.Center);
        Stretch(volumeLabel.rectTransform, new Vector2(0f, 0.40f), new Vector2(1f, 0.48f));

        volumeSlider = BuildVolumeSlider(optionsPanel.transform);

        var back = NewButton(optionsPanel.transform, "Btn_OptionsBack",
            LocalizationManager.Get("UI_BACK"), OnOptionsBackPressed);
        var brt = back.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.14f);
        brt.anchorMax = new Vector2(0.5f, 0.14f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(420f, 90f);
        brt.anchoredPosition = Vector2.zero;
        boundLabels[back.GetComponentInChildren<TMP_Text>()] = "UI_BACK";

        optionsPanel.SetActive(false);
    }

    private Slider BuildVolumeSlider(Transform parent)
    {
        var go = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.31f);
        rt.anchorMax = new Vector2(0.5f, 0.31f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(700f, 46f);
        rt.anchoredPosition = Vector2.zero;

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(go.transform, false);
        Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        background.GetComponent<Image>().color = new Color(0.20f, 0.20f, 0.26f);

        var fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        Stretch(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        fill.GetComponent<Image>().color = selectedColor;

        var slider = go.GetComponent<Slider>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fill.GetComponent<Image>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(PlayerPrefs.GetFloat(VolumeKey, 1f));
        slider.onValueChanged.AddListener(OnVolumeChanged);
        return slider;
    }

    private Button NewMenuButton(Transform parent, string name, string key, int index,
                                 UnityEngine.Events.UnityAction onClick)
    {
        var button = NewButton(parent, name, LocalizationManager.Get(key), onClick);

        var rt = button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.45f);
        rt.anchorMax = new Vector2(0.5f, 0.45f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520f, 96f);
        rt.anchoredPosition = new Vector2(0f, -index * 116f);

        boundLabels[button.GetComponentInChildren<TMP_Text>()] = key;
        return button;
    }

    private Button NewButton(Transform parent, string name, string text,
                             UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = buttonColor;

        var label = NewLabel(go.transform, "Label", 38f, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, Vector2.zero, Vector2.one);
        label.text = text;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        return button;
    }

    private static GameObject NewPanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        go.GetComponent<Image>().color = color;
        go.transform.SetAsLastSibling();
        return go;
    }

    private static TMP_Text NewLabel(Transform parent, string name, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
