using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tablón de contratos: una fila por misión con su progreso y su botón de cobro.
public class QuestBoardUI : MonoBehaviour
{
    [Tooltip("Canvas sobre el que se monta el tablón.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Gestor que lleva el progreso y paga las recompensas.")]
    [SerializeField] private QuestManager quests;

    [Tooltip("Tamaño del modal.")]
    [SerializeField] private Vector2 size = new Vector2(1100f, 700f);

    [Tooltip("Alto de cada fila de contrato.")]
    [SerializeField] private float rowHeight = 78f;

    private GameObject panel;
    private TMP_Text titulo;
    private TMP_Text closeLabel;
    private RectTransform lista;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (quests == null) quests = UnityEngine.Object.FindFirstObjectByType<QuestManager>();

        Build();
    }

    void OnEnable()
    {
        LocalizationManager.LanguageChanged += Rebuild;
        if (quests != null) quests.QuestsChanged += Rebuild;
    }

    void OnDisable()
    {
        LocalizationManager.LanguageChanged -= Rebuild;
        if (quests != null) quests.QuestsChanged -= Rebuild;
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Open()
    {
        if (panel == null) return;

        Rebuild();
        UIManager.OpenExclusive(panel);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    // Se regenera entera: son seis filas, no compensa diferenciar.
    private void Rebuild()
    {
        if (lista == null || quests == null) return;

        titulo.text = LocalizationManager.Get("UI_QUESTS");
        if (closeLabel != null) closeLabel.text = LocalizationManager.Get("UI_CLOSE");

        // DestroyImmediate y no Destroy: TryClaim dispara QuestsChanged (que repinta) y luego
        // OnClaimPressed repinta otra vez en el mismo frame; con el borrado diferido las filas
        // viejas seguían vivas y se apilaban sobre las nuevas.
        for (int i = lista.childCount - 1; i >= 0; i--)
            DestroyImmediate(lista.GetChild(i).gameObject);

        foreach (var quest in quests.Quests) CreateRow(quest);
    }

    private void CreateRow(Quest quest)
    {
        var fila = new GameObject("Quest_" + quest.kind + (quest.milestone ? "_M" : "_R"), typeof(RectTransform), typeof(Image));
        fila.transform.SetParent(lista, false);
        fila.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, rowHeight);

        UITheme.Surface(fila, UITheme.Card, UITheme.BorderSoft, UITheme.RadiusCard);

        int hecho = quests.ProgressOf(quest);
        bool completa = quests.IsComplete(quest);

        var texto = UIBuild.Label(fila.transform, "Text", UITheme.SizeBody, TextAlignmentOptions.Left);
        var trt = texto.rectTransform;
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = new Vector2(18f, 0f);
        trt.offsetMax = new Vector2(-300f, 0f);

        string colorProgreso = UITheme.Tag(completa ? UITheme.Cyan : UITheme.TextMuted);
        texto.text = $"<b>{QuestManager.Describe(quest)}</b>\n" +
                     $"<size={UITheme.SizeCaption}><color={colorProgreso}>{hecho}/{quest.target}</color>" +
                     $"<color={UITheme.Tag(UITheme.TextFaint)}>   ·   " +
                     $"{LocalizationManager.Get("UI_REWARD")}: {Recompensa(quest)}</color></size>";

        var boton = new GameObject("Btn_Claim", typeof(RectTransform), typeof(Image), typeof(Button));
        boton.transform.SetParent(fila.transform, false);

        var brt = boton.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(1f, 0.5f);
        brt.anchorMax = new Vector2(1f, 0.5f);
        brt.pivot = new Vector2(1f, 0.5f);
        brt.sizeDelta = new Vector2(240f, 44f);
        brt.anchoredPosition = new Vector2(-18f, 0f);

        bool sePuede = quests.CanClaim(quest);
        var imagen = UITheme.Surface(boton,
            quest.claimed ? UITheme.Neutral : (sePuede ? UITheme.AccentPick : UITheme.Neutral),
            UITheme.BorderCard, UITheme.RadiusButton);

        var etiqueta = UIBuild.Label(boton.transform, "Label", UITheme.SizeCaption, TextAlignmentOptions.Center);
        UIBuild.Stretch(etiqueta.rectTransform);
        etiqueta.text = LocalizationManager.Get(quest.claimed ? "UI_CLAIMED" : "UI_CLAIM");
        etiqueta.color = sePuede ? UITheme.Text : UITheme.TextFaint;

        var button = boton.GetComponent<Button>();
        button.targetGraphic = imagen;
        button.interactable = sePuede;
        button.onClick.AddListener(() => OnClaimPressed(quest));
    }

    private void OnClaimPressed(Quest quest)
    {
        if (quests != null) quests.TryClaim(quest);
        Rebuild();
    }

    private static string Recompensa(Quest quest)
    {
        var partes = new List<string>();
        if (quest.rewardGems > 0) partes.Add($"{quest.rewardGems} ◆");
        if (quest.rewardWood > 0) partes.Add($"{quest.rewardWood} {LocalizationManager.Get("UI_WOOD")}");
        if (quest.rewardIron > 0) partes.Add($"{quest.rewardIron} {LocalizationManager.Get("UI_IRON")}");
        if (quest.rewardFood > 0) partes.Add($"{quest.rewardFood} {LocalizationManager.Get("UI_FOOD")}");

        return string.Join("  ", partes);
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "QuestBoardPanel", size, UITheme.Bg);

        titulo = UIBuild.TopLabel(panel.transform, "Title", UITheme.SizeTitle, 36f, -22f,
            TextAlignmentOptions.Left);

        // Scroll: seis contratos entran, pero el tablón puede crecer.
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image),
                                      typeof(Mask), typeof(ScrollRect));
        viewport.transform.SetParent(panel.transform, false);

        var vrt = viewport.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = new Vector2(24f, 80f);
        vrt.offsetMax = new Vector2(-24f, -80f);
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.20f);
        viewport.GetComponent<Mask>().showMaskGraphic = true;

        var contenido = new GameObject("Content", typeof(RectTransform));
        contenido.transform.SetParent(viewport.transform, false);

        lista = contenido.GetComponent<RectTransform>();
        lista.anchorMin = new Vector2(0f, 1f);
        lista.anchorMax = new Vector2(1f, 1f);
        lista.pivot = new Vector2(0.5f, 1f);
        lista.anchoredPosition = Vector2.zero;
        lista.sizeDelta = new Vector2(0f, 100f);

        var layout = contenido.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        contenido.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewport.GetComponent<ScrollRect>();
        scroll.viewport = vrt;
        scroll.content = lista;
        scroll.horizontal = false;

        var btnClose = UIBuild.Button(panel.transform, "Btn_CloseQuests", LocalizationManager.Get("UI_CLOSE"),
            Color.clear, new Vector2(240f, 48f), new Vector2(0f, -(size.y - 62f)), Close);
        closeLabel = btnClose.GetComponentInChildren<TMP_Text>();
    }
}
