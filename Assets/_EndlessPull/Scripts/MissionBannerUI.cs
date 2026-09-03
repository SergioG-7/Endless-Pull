using System.Collections;
using TMPro;
using UnityEngine;

// Banner de misión: título + objetivo del piso, con fundido vía CanvasGroup. Vive justo el
// tiempo de la cuenta atrás previa al combate (WaveManager.CombatCountdown), para que se lea
// mientras la escuadra y la oleada siguen congeladas.
public class MissionBannerUI : MonoBehaviour
{
    [Tooltip("Gestor de oleadas: dispara el evento con el tipo de misión al empezar la cuenta atrás.")]
    [SerializeField] private WaveManager waves;

    [Tooltip("Canvas donde se monta el banner; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Segundos de fundido de entrada y de salida del banner.")]
    [SerializeField] private float fadeSeconds = 0.25f;

    [Tooltip("Segundos extra sobre la cuenta atrás para dar tiempo a leer el objetivo y si hay reto oculto.")]
    [SerializeField] private float extraReadSeconds = 1f;

    private CanvasGroup group;
    private TMP_Text titleLabel;
    private TMP_Text objectiveLabel;
    private TMP_Text hiddenLabel;
    private Coroutine routine;

    void Awake()
    {
        if (waves == null) waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        Build();
    }

    void OnEnable()
    {
        if (waves != null) waves.MissionStarted += OnMissionStarted;
    }

    void OnDisable()
    {
        if (waves != null) waves.MissionStarted -= OnMissionStarted;
    }

    // Banda centrada por encima del rótulo dramático de la cuenta atrás (3-2-1), para no pisarse.
    private void Build()
    {
        if (canvas == null) return;

        var go = new GameObject("MissionBanner_Panel", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(canvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.offsetMin = new Vector2(0f, 40f);
        rt.offsetMax = new Vector2(0f, 170f);

        group = go.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        // Tres franjas apiladas: título, objetivo y (nueva) aviso de reto oculto sí/no.
        titleLabel = UIBuild.Label(go.transform, "Title", UITheme.SizeTitle * 1.4f, TextAlignmentOptions.Center);
        var titleRt = titleLabel.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 0.615f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        titleLabel.fontStyle = FontStyles.Bold;

        objectiveLabel = UIBuild.Label(go.transform, "Objective", UITheme.SizeBody, TextAlignmentOptions.Center);
        var objRt = objectiveLabel.rectTransform;
        objRt.anchorMin = new Vector2(0f, 0.231f);
        objRt.anchorMax = new Vector2(1f, 0.615f);
        objRt.offsetMin = Vector2.zero;
        objRt.offsetMax = Vector2.zero;
        objectiveLabel.color = UITheme.TextSoft;

        hiddenLabel = UIBuild.Label(go.transform, "HiddenChallenge", UITheme.SizeBody * 0.85f, TextAlignmentOptions.Center);
        var hiddenRt = hiddenLabel.rectTransform;
        hiddenRt.anchorMin = new Vector2(0f, 0f);
        hiddenRt.anchorMax = new Vector2(1f, 0.231f);
        hiddenRt.offsetMin = Vector2.zero;
        hiddenRt.offsetMax = Vector2.zero;
        hiddenLabel.color = new Color(1f, 0.85f, 0.2f);
    }

    private void OnMissionStarted(FloorMissionType type, int floor)
    {
        if (group == null || waves == null) return;

        titleLabel.text = LocalizationManager.Get(TitleKey(type));
        titleLabel.color = ColorFor(type);
        objectiveLabel.text = string.Format(LocalizationManager.Get(ObjectiveKey(type)),
            Mathf.RoundToInt(waves.SurvivalDuration));
        hiddenLabel.text = LocalizationManager.Get(waves.HasHiddenChallenge ? "MISSION_HIDDEN_YES" : "MISSION_HIDDEN_NO");

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeRoutine(Mathf.Max(0.1f, waves.CombatCountdown + extraReadSeconds)));
    }

    // Entra, se sostiene y sale, todo dentro de la ventana de la cuenta atrás; corre en tiempo
    // real para no depender del timeScale (la oleada está congelada, pero el reloj sigue).
    private IEnumerator FadeRoutine(float duration)
    {
        float fade = Mathf.Min(fadeSeconds, duration * 0.4f);
        float hold = Mathf.Max(0f, duration - fade * 2f);

        yield return Fade(0f, 1f, fade);
        yield return new WaitForSecondsRealtime(hold);
        yield return Fade(1f, 0f, fade);
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        if (seconds <= 0f) { group.alpha = to; yield break; }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        group.alpha = to;
    }

    private static string TitleKey(FloorMissionType type) => type switch
    {
        FloorMissionType.Survival => "MISSION_TITLE_SURVIVAL",
        FloorMissionType.Escort => "MISSION_TITLE_ESCORT",
        FloorMissionType.BossHunt => "MISSION_TITLE_BOSSHUNT",
        _ => "MISSION_TITLE_SUBJUGATION"
    };

    private static string ObjectiveKey(FloorMissionType type) => type switch
    {
        FloorMissionType.Survival => "MISSION_OBJ_SURVIVAL",
        FloorMissionType.Escort => "MISSION_OBJ_ESCORT",
        FloorMissionType.BossHunt => "MISSION_OBJ_BOSSHUNT",
        _ => "MISSION_OBJ_SUBJUGATION"
    };

    private static Color ColorFor(FloorMissionType type) => type switch
    {
        FloorMissionType.Survival => new Color(1f, 0.55f, 0.25f),
        FloorMissionType.Escort => UITheme.Cyan,
        FloorMissionType.BossHunt => new Color(1f, 0.25f, 0.20f),
        _ => UITheme.Text
    };
}
