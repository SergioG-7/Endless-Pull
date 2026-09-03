using System.Text;
using TMPro;
using UnityEngine;

// Modal aparte de la ficha rápida: habilidad activa, pasivas innatas y pericia de arma, cada
// una con su explicación corta y en los 3 idiomas. Vive fuera de HeroQuickCardUI porque un
// héroe con varias pasivas desbordaba el texto metido ahí dentro.
public class HeroSkillsModalUI : MonoBehaviour
{
    [Tooltip("Canvas donde se monta el modal; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Tamaño del modal.")]
    [SerializeField] private Vector2 size = new Vector2(640f, 740f);

    private GameObject panel;
    private HeroController hero;
    private TMP_Text titulo;
    private TMP_Text sectionActiveTitle;
    private TMP_Text activeBody;
    private TMP_Text sectionPassiveTitle;
    private TMP_Text passiveBody;
    private TMP_Text sectionMasteryTitle;
    private TMP_Text masteryBody;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        Build();
    }

    void OnEnable() => LocalizationManager.LanguageChanged += OnLanguageChanged;
    void OnDisable() => LocalizationManager.LanguageChanged -= OnLanguageChanged;

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void OnLanguageChanged()
    {
        RefreshStaticLabels();
        if (IsOpen) Refresh();
    }

    public void Open(HeroController target)
    {
        if (panel == null || target == null) return;

        hero = target;
        UIManager.OpenExclusive(panel);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        hero = null;
    }

    private void RefreshStaticLabels()
    {
        titulo.text = LocalizationManager.Get("UI_SKILLS_MODAL_TITLE");
        sectionActiveTitle.text = LocalizationManager.Get("UI_SKILLS_SECTION_ACTIVE");
        sectionPassiveTitle.text = LocalizationManager.Get("UI_SKILLS_SECTION_PASSIVE");
        sectionMasteryTitle.text = LocalizationManager.Get("UI_SKILLS_SECTION_MASTERY");
    }

    private void Refresh()
    {
        if (hero == null) return;

        // Habilidad activa: nombre + descripción + coste/enfriamiento, igual que la oferta de subclase.
        if (hero.Skill != null)
        {
            activeBody.text = $"<b>{hero.Skill.GetDisplayName()}</b>\n{hero.Skill.GetDescription()}\n" +
                               $"{hero.Skill.mpCost} MP · {hero.Skill.cooldown:0.#}s";
        }
        else activeBody.text = string.Empty;

        // Pasivas: una por línea, con su explicación — no solo el nombre.
        if (hero.Passives.Count == 0)
        {
            passiveBody.text = LocalizationManager.Get("UI_NO_PASSIVES");
        }
        else
        {
            var sb = new StringBuilder();
            foreach (var passive in hero.Passives)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append($"<b>{PassiveSkills.DisplayName(passive)}</b>: {PassiveSkills.Description(passive)}");
            }
            passiveBody.text = sb.ToString();
        }

        // Pericia de arma: nivel y rango por tipo de arma entrenado, más la explicación general.
        string maestria = hero.Mastery.Describe();
        masteryBody.text = (maestria == "-" ? LocalizationManager.Get("UI_MASTERY_NONE") : maestria) +
                            "\n\n" + LocalizationManager.Get("UI_MASTERY_EXPLANATION");
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "HeroSkillsModal", size, new Color(0.11f, 0.11f, 0.17f, 0.98f));
        UIBuild.CloseButtonTopRight(panel.transform, Close);

        titulo = UIBuild.TopLabel(panel.transform, "Title", UIBuild.TitleSize, 40f, -22f,
            TextAlignmentOptions.Center);

        float y = -84f;

        sectionActiveTitle = UIBuild.TopLabel(panel.transform, "SectionActive", UIBuild.NameSize, 30f, y,
            TextAlignmentOptions.Left);
        sectionActiveTitle.color = UITheme.AccentPick;
        y -= 34f;
        activeBody = UIBuild.TopLabel(panel.transform, "ActiveBody", UIBuild.BodySize, 100f, y,
            TextAlignmentOptions.TopLeft);
        activeBody.color = UITheme.TextSoft;
        y -= 116f;

        sectionPassiveTitle = UIBuild.TopLabel(panel.transform, "SectionPassive", UIBuild.NameSize, 30f, y,
            TextAlignmentOptions.Left);
        sectionPassiveTitle.color = UITheme.AccentPick;
        y -= 34f;
        passiveBody = UIBuild.TopLabel(panel.transform, "PassiveBody", UIBuild.BodySize, 140f, y,
            TextAlignmentOptions.TopLeft);
        passiveBody.color = UITheme.TextSoft;
        y -= 156f;

        sectionMasteryTitle = UIBuild.TopLabel(panel.transform, "SectionMastery", UIBuild.NameSize, 30f, y,
            TextAlignmentOptions.Left);
        sectionMasteryTitle.color = UITheme.AccentPick;
        y -= 34f;
        masteryBody = UIBuild.TopLabel(panel.transform, "MasteryBody", UIBuild.BodySize, 190f, y,
            TextAlignmentOptions.TopLeft);
        masteryBody.color = UITheme.TextSoft;

        RefreshStaticLabels();
        panel.SetActive(false);
    }
}
