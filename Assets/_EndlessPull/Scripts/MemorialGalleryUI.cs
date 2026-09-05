using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Galería Memorial: panel de honores con los héroes que ya no están, su último equipo y cómo
// se perdieron. Construida por código como el resto de modales, sin cableado en el Inspector.
public class MemorialGalleryUI : MonoBehaviour
{
    [SerializeField] private Canvas canvas;

    [Tooltip("Tamaño del panel, en píxeles de UI.")]
    [SerializeField] private Vector2 size = new Vector2(980f, 700f);

    private GameObject panel;
    private TMP_Text titulo;
    private RectTransform content;

    // Una fila por ficha; se reutilizan para no destruir y recrear en cada refresco.
    private readonly List<TMP_Text> pool = new List<TMP_Text>();

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
        Build();
        if (panel != null) panel.SetActive(false);
    }

    void OnEnable()
    {
        MemorialManager.RecordsChanged += Refresh;
        LocalizationManager.LanguageChanged += OnLanguageChanged;
    }

    void OnDisable()
    {
        MemorialManager.RecordsChanged -= Refresh;
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        if (IsOpen) Refresh();
    }

    public void Open()
    {
        if (panel == null) return;

        Refresh();
        UIManager.OpenExclusive(panel);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Refresh()
    {
        if (content == null) return;

        titulo.text = LocalizationManager.Get("BLD_GALLERY");

        var memorial = Object.FindFirstObjectByType<MemorialManager>();
        var fichas = memorial != null ? memorial.Records : new List<MemorialRecord>();

        // Sin nadie perdido todavía, una sola línea explicándolo en vez de una lista vacía.
        int filas = Mathf.Max(1, fichas.Count);
        while (pool.Count < filas) pool.Add(NewRow());

        for (int i = 0; i < pool.Count; i++)
        {
            bool usada = i < filas;
            pool[i].gameObject.SetActive(usada);
            if (!usada) continue;

            if (fichas.Count == 0)
            {
                pool[i].text = LocalizationManager.Get("UI_MEMORIAL_EMPTY");
                pool[i].color = UITheme.TextMuted;
                continue;
            }

            var ficha = fichas[i];
            var estrellas = new System.Text.StringBuilder();
            for (int e = 0; e < ficha.starRank; e++) estrellas.Append('★');

            string equipo = string.IsNullOrEmpty(ficha.lastEquipment)
                ? string.Empty
                : $"\n<size={UITheme.SizeCaption}><color={UITheme.Tag(UITheme.TextFaint)}>" +
                  $"{ficha.lastEquipment}</color></size>";

            pool[i].color = UITheme.Text;
            pool[i].text = $"<color={UITheme.Tag(UITheme.Rarity(ficha.starRank))}>{estrellas}</color>  " +
                           $"<b>{ficha.heroName}</b>  " +
                           $"<size={UITheme.SizeCaption}>{LocalizationManager.Get("UI_LEVEL_ABBR")}{ficha.level}</size>\n" +
                           $"<size={UITheme.SizeCaption}><color={UITheme.Tag(UITheme.TextMuted)}>" +
                           $"{ficha.CauseLabel()}</color></size>{equipo}";
        }
    }

    private TMP_Text NewRow()
    {
        var fila = UIBuild.Label(content, "MemorialRow", UITheme.SizeName, TextAlignmentOptions.TopLeft);
        var rt = fila.rectTransform;
        rt.sizeDelta = new Vector2(0f, 78f);
        return fila;
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "MemorialPanel", size, UITheme.Bg);
        titulo = UIBuild.TopLabel(panel.transform, "Title", UITheme.SizeTitle, 34f, -20f,
            TextAlignmentOptions.Left);
        UIBuild.CloseButtonTopRight(panel.transform, Close);

        var viewGo = new GameObject("ListViewport", typeof(RectTransform), typeof(Image),
                                    typeof(Mask), typeof(ScrollRect));
        viewGo.transform.SetParent(panel.transform, false);

        var vrt = viewGo.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = new Vector2(24f, 24f);
        vrt.offsetMax = new Vector2(-24f, -92f);

        UITheme.Surface(viewGo, UITheme.Hex("15151E"), UITheme.BorderSoft, UITheme.RadiusCard);
        viewGo.GetComponent<Mask>().showMaskGraphic = true;

        var bodyGo = new GameObject("ListBody", typeof(RectTransform));
        bodyGo.transform.SetParent(viewGo.transform, false);
        content = bodyGo.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;

        var scroll = viewGo.GetComponent<ScrollRect>();
        scroll.viewport = vrt;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.scrollSensitivity = 30f;

        var layout = bodyGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 18, 18);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        bodyGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }
}
