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

    [Tooltip("Ficha de héroe a la que se vuelve al cerrar este modal.")]
    [SerializeField] private HeroQuickCardUI quickCard;

    public void Open(HeroController target)
    {
        if (panel == null || target == null) return;

        hero = target;
        UIManager.OpenExclusive(panel);
        Refresh();
    }

    // Este modal solo lo abre la ficha de héroe: al cerrarlo, vuelve a ella. Sin esto se
    // cerraba a secas y el jugador acababa en la base, perdiendo el hilo de dónde estaba.
    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        hero = null;

        if (quickCard == null) quickCard = UnityEngine.Object.FindFirstObjectByType<HeroQuickCardUI>();
        if (quickCard != null) quickCard.Reopen();
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

        // Habilidades activas: todas las del repertorio, una por línea y en el mismo formato
        // compacto que las pasivas, que con tres habilidades el bloque de antes no cabía.
        if (hero.Skills.Count == 0)
        {
            activeBody.text = hero.Skill != null
                ? $"<b>{hero.Skill.GetDisplayName()}</b>: {hero.Skill.GetDescription()}"
                : string.Empty;
        }
        else
        {
            var activas = new StringBuilder();
            foreach (var aprendida in hero.Skills)
            {
                if (aprendida == null) continue;
                if (activas.Length > 0) activas.Append('\n');

                string papel = ActiveSkills.RoleName(ActiveSkills.RoleOf(aprendida.ability));

                activas.Append($"<b>{aprendida.GetDisplayName()}</b>  " +
                               $"<color={UITheme.Tag(UITheme.Accent)}>[{papel}]</color>: " +
                               $"{aprendida.GetDescription()}  " +
                               $"<color={UITheme.Tag(UITheme.TextMuted)}>{aprendida.mpCost} MP · " +
                               $"{aprendida.cooldown:0.#}s</color>");
            }
            activeBody.text = activas.ToString();
        }

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

        // Pericia de arma: una por línea, incluida la del arma que lleva puesta aunque esté a
        // cero. Sin párrafo explicativo: ocupaba más que la propia lista.
        string maestria = hero.Mastery.DescribeLines(hero.EquippedWeaponType);
        masteryBody.text = string.IsNullOrEmpty(maestria)
            ? LocalizationManager.Get("UI_MASTERY_NONE")
            : maestria;
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
        activeBody = UIBuild.TopLabel(panel.transform, "ActiveBody", UIBuild.BodySize, 190f, y,
            TextAlignmentOptions.TopLeft);
        activeBody.color = UITheme.TextSoft;
        y -= 206f;

        sectionPassiveTitle = UIBuild.TopLabel(panel.transform, "SectionPassive", UIBuild.NameSize, 30f, y,
            TextAlignmentOptions.Left);
        sectionPassiveTitle.color = UITheme.AccentPick;
        y -= 34f;
        passiveBody = UIBuild.TopLabel(panel.transform, "PassiveBody", UIBuild.BodySize, 210f, y,
            TextAlignmentOptions.TopLeft);
        passiveBody.color = UITheme.TextSoft;
        y -= 226f;

        sectionMasteryTitle = UIBuild.TopLabel(panel.transform, "SectionMastery", UIBuild.NameSize, 30f, y,
            TextAlignmentOptions.Left);
        sectionMasteryTitle.color = UITheme.AccentPick;
        y -= 34f;
        masteryBody = UIBuild.TopLabel(panel.transform, "MasteryBody", UIBuild.BodySize, 130f, y,
            TextAlignmentOptions.TopLeft);
        masteryBody.color = UITheme.TextSoft;

        RefreshStaticLabels();
        panel.SetActive(false);
    }
}
