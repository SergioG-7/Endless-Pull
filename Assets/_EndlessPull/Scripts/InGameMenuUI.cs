using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Menú de pausa accesible durante la partida (botón "☰" del TopBar): reanudar, volumen de
// interfaz/combate, idioma ES/EN/JA y guardar y salir al título. Pausa con Time.timeScale,
// igual que la pantalla de título (MainMenuUI), así que ambos conviven sin pisarse.
public class InGameMenuUI : MonoBehaviour
{
    [Tooltip("Canvas sobre el que se monta el modal.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Gestor de audio cuyos volúmenes controlan los sliders.")]
    [SerializeField] private AudioManager audioManager;

    [Tooltip("Guardado que se vuelca a disco al pulsar 'Guardar y salir'.")]
    [SerializeField] private SaveManager saves;

    [Tooltip("Pantalla de título a la que se vuelve tras guardar y salir.")]
    [SerializeField] private MainMenuUI mainMenu;

    [Tooltip("Tamaño del modal.")]
    [SerializeField] private Vector2 size = new Vector2(560f, 560f);

    [Tooltip("Color del idioma seleccionado.")]
    [SerializeField] private Color selectedColor = new Color(0.70f, 0.55f, 0.20f);

    private GameObject panel;
    private TMP_Text titulo;
    private TMP_Text uiVolumeLabel;
    private TMP_Text combatVolumeLabel;
    private Slider uiVolumeSlider;
    private Slider combatVolumeSlider;
    private readonly Button[] languageButtons = new Button[3];

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (audioManager == null) audioManager = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
        if (saves == null) saves = UnityEngine.Object.FindFirstObjectByType<SaveManager>();
        if (mainMenu == null) mainMenu = UnityEngine.Object.FindFirstObjectByType<MainMenuUI>();

        Build();
    }

    void OnEnable() => LocalizationManager.LanguageChanged += RefreshTexts;
    void OnDisable() => LocalizationManager.LanguageChanged -= RefreshTexts;

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Open()
    {
        if (panel == null || mainMenu != null && mainMenu.IsOpen) return;

        UIManager.OpenExclusive(panel);
        Time.timeScale = 0f;
        RefreshTexts();
    }

    public void OnResumePressed()
    {
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
    }

    // Guarda de inmediato y vuelve al título; el título deja el juego en pausa igual que este menú.
    public void OnQuitPressed()
    {
        saves?.Save();
        if (panel != null) panel.SetActive(false);
        mainMenu?.Open();
    }

    private void OnLanguagePressed(GameLanguage language)
    {
        LocalizationManager.SetLanguage(language);
        RefreshTexts();
    }

    private void OnUiVolumeChanged(float value)
    {
        if (audioManager != null) audioManager.UIVolume = value;
        RefreshVolumeLabels();
    }

    private void OnCombatVolumeChanged(float value)
    {
        if (audioManager != null) audioManager.CombatVolume = value;
        RefreshVolumeLabels();
    }

    private void RefreshTexts()
    {
        titulo.text = LocalizationManager.Get("UI_IN_GAME_MENU");

        for (int i = 0; i < languageButtons.Length; i++)
        {
            bool active = (int)LocalizationManager.Current == i;
            languageButtons[i].targetGraphic.color = active ? selectedColor : UITheme.Neutral;
        }

        RefreshVolumeLabels();
    }

    private void RefreshVolumeLabels()
    {
        if (audioManager == null) return;

        uiVolumeLabel.text = $"{LocalizationManager.Get("UI_UI_VOLUME")}   " +
                             $"{Mathf.RoundToInt(audioManager.UIVolume * 100f)}%";
        combatVolumeLabel.text = $"{LocalizationManager.Get("UI_COMBAT_VOLUME")}   " +
                                 $"{Mathf.RoundToInt(audioManager.CombatVolume * 100f)}%";
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "InGameMenuPanel", size, UITheme.Bg);

        titulo = UIBuild.TopLabel(panel.transform, "Title", UITheme.SizeTitle, 40f, -24f,
            TextAlignmentOptions.Center);

        UIBuild.Button(panel.transform, "Btn_Resume", LocalizationManager.Get("UI_RESUME"),
            UITheme.Amber, new Vector2(360f, 64f), new Vector2(0f, -100f), OnResumePressed);

        float volumeY = -200f;
        uiVolumeLabel = UIBuild.TopLabel(panel.transform, "UiVolumeLabel", UITheme.SizeBody, 26f,
            volumeY, TextAlignmentOptions.Center);
        uiVolumeSlider = BuildSlider(panel.transform, "UiVolumeSlider", volumeY - 34f, OnUiVolumeChanged);

        float combatY = volumeY - 90f;
        combatVolumeLabel = UIBuild.TopLabel(panel.transform, "CombatVolumeLabel", UITheme.SizeBody, 26f,
            combatY, TextAlignmentOptions.Center);
        combatVolumeSlider = BuildSlider(panel.transform, "CombatVolumeSlider", combatY - 34f, OnCombatVolumeChanged);

        if (audioManager != null)
        {
            uiVolumeSlider.SetValueWithoutNotify(audioManager.UIVolume);
            combatVolumeSlider.SetValueWithoutNotify(audioManager.CombatVolume);
        }

        float langY = combatY - 100f;
        var langTitle = UIBuild.TopLabel(panel.transform, "LanguageTitle", UITheme.SizeBody, 26f,
            langY, TextAlignmentOptions.Center);
        langTitle.text = LocalizationManager.Get("UI_LANGUAGE");
        langTitle.color = UITheme.TextSoft;

        for (int i = 0; i < 3; i++)
        {
            var language = (GameLanguage)i;
            float x = (i - 1) * 170f;
            var button = UIBuild.Button(panel.transform, "Btn_Lang_" + language,
                LocalizationManager.NameOf(language), UITheme.Neutral, new Vector2(150f, 56f),
                new Vector2(x, langY - 46f), () => OnLanguagePressed(language));
            languageButtons[i] = button;
        }

        UIBuild.Button(panel.transform, "Btn_QuitToMenu", LocalizationManager.Get("UI_QUIT_TO_MENU"),
            UITheme.DangerSoft, new Vector2(360f, 60f), new Vector2(0f, -(size.y - 60f)), OnQuitPressed);

        panel.SetActive(false);
    }

    // Slider manual: UIBuild no trae uno, y aquí solo hace falta el track + relleno + handle.
    private Slider BuildSlider(Transform parent, string name, float y, UnityEngine.Events.UnityAction<float> onChanged)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(420f, 28f);
        rt.anchoredPosition = new Vector2(0f, y);

        var track = UITheme.Surface(go, UITheme.Track, Color.clear, 6f);

        var fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        UIBuild.Stretch(fillArea.GetComponent<RectTransform>());

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        UIBuild.Stretch(fill.GetComponent<RectTransform>());
        UITheme.Surface(fill, UITheme.Amber, Color.clear, 6f);

        var slider = go.GetComponent<Slider>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = track;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.onValueChanged.AddListener(onChanged);
        return slider;
    }
}
