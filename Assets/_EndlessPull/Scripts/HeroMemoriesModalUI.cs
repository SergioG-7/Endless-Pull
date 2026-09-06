using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Pantalla de Recuerdos de un héroe: su origen, su bio y los recuerdos que va desbloqueando al
// ascender. Vive aparte de la ficha rápida a propósito — pegados debajo de la bio empujaban las
// barras de vida/maná/moral fuera del panel.
public class HeroMemoriesModalUI : MonoBehaviour
{
    [SerializeField] private Canvas canvas;

    [Tooltip("Tamaño del panel, en píxeles de UI.")]
    [SerializeField] private Vector2 size = new Vector2(640f, 740f);

    private GameObject panel;
    private HeroController hero;
    private TMP_Text titulo;
    private TMP_Text cabecera;
    private TMP_Text cuerpo;
    private RectTransform content;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
        Build();
        if (panel != null) panel.SetActive(false);
    }

    void OnEnable() => LocalizationManager.LanguageChanged += OnLanguageChanged;

    void OnDisable() => LocalizationManager.LanguageChanged -= OnLanguageChanged;

    private void OnLanguageChanged()
    {
        if (IsOpen) Refresh();
    }

    public void Open(HeroController target)
    {
        if (panel == null || target == null) return;

        hero = target;
        Refresh();
        UIManager.OpenExclusive(panel);
    }

    // Vuelve a la ficha rápida en vez de dejar al jugador en el mundo, igual que el modal de
    // Habilidades: es de donde se entra.
    public void Close()
    {
        if (panel != null) panel.SetActive(false);

        var ficha = Object.FindFirstObjectByType<HeroQuickCardUI>();
        if (ficha != null && hero != null) ficha.Show(hero);
    }

    private void Refresh()
    {
        if (hero == null || hero.Data == null) return;

        titulo.text = LocalizationManager.Get("UI_MEMORIES");

        var estrellas = new System.Text.StringBuilder();
        for (int i = 0; i < hero.StarRank; i++) estrellas.Append('★');

        int nivel = hero.GetComponent<HeroProgress>()?.Level ?? 1;

        // Quién es: nombre, rareza, nivel, origen y título, antes de los recuerdos en sí.
        var cab = new System.Text.StringBuilder();
        cab.Append($"<color={UITheme.Tag(UITheme.Rarity(hero.StarRank))}>{estrellas}</color>  ");
        cab.Append($"<b>{hero.Data.heroName}</b>  ");
        cab.Append($"<size={UITheme.SizeCaption}><color={UITheme.Tag(UITheme.TextMuted)}>" +
                   $"{LocalizationManager.Get("UI_LEVEL_ABBR")}{nivel}</color></size>");

        string origen = LocalizationManager.GetOrigin(hero.Data.origin);
        if (!string.IsNullOrEmpty(origen))
            cab.Append($"\n<size={UITheme.SizeCaption}><color={UITheme.Tag(UITheme.TextSoft)}>" +
                       $"{origen}</color></size>");

        cabecera.text = cab.ToString();

        // Bio primero y recuerdos después: la bio es lo que ya sabes de él, los recuerdos lo que
        // te vas ganando.
        var texto = new System.Text.StringBuilder();
        // La bio va por GetLocalizedBio(): leer hero.Data.bio directo dejaba el trasfondo en
        // español aunque el panel se refrescara al cambiar de idioma.
        string trasfondo = hero.Data.GetLocalizedBio();
        if (!string.IsNullOrEmpty(trasfondo))
            texto.Append(trasfondo + "\n\n");

        texto.Append(hero.MemoriesReport());
        texto.Append(DeedsReport());
        texto.Append(BondsReport());
        cuerpo.text = texto.ToString();

        // El panel crece con el texto; sin esto los héroes con muchos recuerdos se cortaban.
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    // Lo que lleva hecho: es lo que abre los recuerdos que no dependen de la rareza, así que
    // tiene que verse en la misma pantalla que los recuerdos sellados.
    private string DeedsReport()
        => $"\n\n<b>{LocalizationManager.Get("UI_DEEDS")}</b>\n" +
           $"<color={UITheme.Tag(UITheme.TextSoft)}>" +
           string.Format(LocalizationManager.Get("UI_DEEDS_ROW"),
                         hero.FloorsCleared, hero.EnemiesSlain) + "</color>\n";

    // Con quién ha sobrevivido pisos: los vínculos cerrados primero, y debajo el compañero al
    // que más cerca está de vincularse, que es lo que empuja a repetir escuadra.
    private string BondsReport()
    {
        var bonds = hero.GetComponent<HeroBonds>();
        if (bonds == null) return string.Empty;

        var entradas = bonds.Entries();
        var cuerpo = new System.Text.StringBuilder();

        foreach (var entrada in entradas)
        {
            if (!entrada.bonded) continue;

            var color = entrada.fallen ? UITheme.TextFaint : UITheme.Accent2;
            cuerpo.Append($"<color={UITheme.Tag(color)}>");
            cuerpo.Append(string.Format(LocalizationManager.Get("UI_BOND_ROW"),
                                        entrada.otherName, entrada.floors));
            if (entrada.fallen) cuerpo.Append("  " + LocalizationManager.Get("UI_BOND_FALLEN"));
            cuerpo.Append("</color>\n");
        }

        // El más cercano al umbral, para que se vea a cuánto está el próximo vínculo.
        foreach (var entrada in entradas)
        {
            if (entrada.bonded || entrada.fallen) continue;

            cuerpo.Append($"<color={UITheme.Tag(UITheme.TextFaint)}>");
            cuerpo.Append(string.Format(LocalizationManager.Get("UI_BOND_PROGRESS"),
                                        entrada.otherName, entrada.floors, bonds.BondThreshold));
            cuerpo.Append("</color>\n");
            break;
        }

        // Sin nada que contar no se pinta ni el título: un apartado vacío estorba más que ayuda.
        if (cuerpo.Length == 0) return string.Empty;

        return $"\n\n<b>{LocalizationManager.Get("UI_BONDS")}</b>\n" + cuerpo;
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "HeroMemoriesModal", size,
                              new Color(0.11f, 0.11f, 0.17f, 0.98f));

        titulo = UIBuild.TopLabel(panel.transform, "Title", UITheme.SizeTitle, 34f, -20f,
                                  TextAlignmentOptions.Left);

        cabecera = UIBuild.TopLabel(panel.transform, "Header", UITheme.SizeName, 30f, -64f,
                                    TextAlignmentOptions.Left);

        UIBuild.CloseButtonTopRight(panel.transform, Close);

        // Lista con scroll: los recuerdos de un 5★ no caben de una.
        var viewGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image),
                                    typeof(Mask), typeof(ScrollRect));
        viewGo.transform.SetParent(panel.transform, false);
        UITheme.Surface(viewGo, UITheme.BgPanel, UITheme.BorderSoft, UITheme.RadiusCard);
        viewGo.GetComponent<Mask>().showMaskGraphic = true;

        var vrt = viewGo.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = new Vector2(24f, 24f);
        vrt.offsetMax = new Vector2(-24f, -128f);

        var bodyGo = new GameObject("Body", typeof(RectTransform));
        bodyGo.transform.SetParent(viewGo.transform, false);
        content = bodyGo.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        var scroll = viewGo.GetComponent<ScrollRect>();
        scroll.viewport = vrt;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.scrollSensitivity = 30f;

        var layout = bodyGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 16, 16);
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        bodyGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        cuerpo = UIBuild.Label(content, "MemoriesBody", UITheme.SizeBody, TextAlignmentOptions.TopLeft);
        cuerpo.textWrappingMode = TextWrappingModes.Normal;
    }
}
