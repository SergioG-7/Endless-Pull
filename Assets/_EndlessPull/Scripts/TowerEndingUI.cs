using TMPro;
using UnityEngine;

// Pantalla que sale al despejar el último piso de la Torre: es la meta del juego y hasta ahora
// terminaba igual que un piso cualquiera. El texto es provisional, a la espera del desenlace.
public class TowerEndingUI : MonoBehaviour
{
    [Tooltip("Canvas donde se monta la pantalla; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Gestor de la Torre del que llega el aviso de piso despejado.")]
    [SerializeField] private WaveManager waves;

    [Tooltip("Tamaño de la pantalla de final.")]
    [SerializeField] private Vector2 size = new Vector2(960f, 520f);

    private GameObject panel;
    private TMP_Text titulo;
    private TMP_Text cuerpo;
    private TMP_Text botonTexto;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (waves == null) waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
    }

    void OnEnable()
    {
        if (waves != null) waves.FloorCleared += OnFloorCleared;
        LocalizationManager.LanguageChanged += Retranslate;
    }

    void OnDisable()
    {
        if (waves != null) waves.FloorCleared -= OnFloorCleared;
        LocalizationManager.LanguageChanged -= Retranslate;
    }

    private void OnFloorCleared(FloorRewardInfo info)
    {
        if (waves == null || info.floor < waves.FinalFloor) return;

        if (panel == null) Build();
        if (panel == null) return;

        Retranslate();
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    private void OnClose()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Retranslate()
    {
        if (titulo != null) titulo.text = LocalizationManager.Get("UI_ENDING_TITLE");
        if (cuerpo != null) cuerpo.text = LocalizationManager.Get("UI_ENDING_BODY");
        if (botonTexto != null) botonTexto.text = LocalizationManager.Get("UI_CLOSE");
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "TowerEnding", size, UITheme.BgPanel);
        panel.SetActive(false);

        titulo = UIBuild.TopLabel(panel.transform, "Title", UIBuild.TitleSize, 70f, -28f,
            TextAlignmentOptions.Center);
        titulo.color = UITheme.BarMorale;

        cuerpo = UIBuild.TopLabel(panel.transform, "Body", UIBuild.BodySize, 300f, -120f,
            TextAlignmentOptions.Top);

        var boton = UIBuild.Button(panel.transform, "Btn_Close", string.Empty, UITheme.Accent,
            new Vector2(260f, 64f), new Vector2(0f, -436f), OnClose);

        botonTexto = boton.GetComponentInChildren<TMP_Text>();
    }
}
