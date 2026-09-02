using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pestaña activa del Santuario.
public enum SanctuaryTab
{
    Ascension,
    Synthesis
}

// Santuario: Ascensión (sube la rareza de un héroe con gemas + Piedra del tier exacto) y
// Síntesis (sacrifica un héroe para dar EXP a otro), migradas fuera del Roster. Comparte una
// lista de héroes a la izquierda; a la derecha vive la vista previa según la pestaña activa.
public class SanctuaryUI : MonoBehaviour
{
    [Tooltip("Canvas sobre el que se monta el modal.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Economía que paga las ascensiones.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Taller del que salen las Piedras de Ascensión.")]
    [SerializeField] private CraftingManager crafting;

    [Tooltip("Gestor de síntesis: primer clic fija el Receptor, el segundo previsualiza el sacrificio.")]
    [SerializeField] private SynthesisManager synthesis;

    [Tooltip("Tamaño del modal.")]
    [SerializeField] private Vector2 size = new Vector2(1100f, 640f);

    [Tooltip("Segundos entre refrescos de la lista mientras el panel está abierto.")]
    [SerializeField] private float refreshInterval = 0.5f;

    private const float ListWidth = 520f;
    private const float RowHeight = 60f;
    private const float RightX = ListWidth + 48f;

    private GameObject panel;
    private RectTransform content;
    private TMP_Text titulo;
    private Button tabAscend, tabSynth;
    private TMP_Text tabAscendLabel, tabSynthLabel;

    // Columna derecha: retrato + nombre comparten las dos pestañas; el resto cambia de sentido.
    private Image previewPortrait;
    private TMP_Text previewName;
    private TMP_Text previewBody;
    private Button previewAction;
    private TMP_Text previewActionLabel;
    private TMP_Text previewHint;

    // Miniatura del héroe elegido como sacrificio en Síntesis; solo visible tras el segundo clic.
    private Image previewFodderPortrait;

    private SanctuaryTab activeTab = SanctuaryTab.Ascension;
    private HeroController selectedAscendHero;

    // Sacrificio elegido en Síntesis, pendiente de confirmar en el overlay (no muta nada aún).
    private HeroController previewFodder;
    private float refreshTimer;

    private class RowWidgets
    {
        public GameObject root;
        public Button button;
        public Image portraitFrame;
        public TMP_Text info;
        public HeroController hero;
    }

    private readonly List<RowWidgets> pool = new List<RowWidgets>();
    private readonly List<HeroController> scratch = new List<HeroController>();

    // Confirmación previa al sacrificio: quién se iba a sacrificar mientras el jugador decide,
    // sin tocar el estado de SynthesisManager todavía (irreversible una vez confirmado).
    private GameObject synthConfirmOverlay;
    private TMP_Text synthConfirmMessage;
    private HeroController pendingSynthTarget;
    private HeroController pendingSynthFodder;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();
        if (synthesis == null) synthesis = UnityEngine.Object.FindFirstObjectByType<SynthesisManager>();

        Build();
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    void OnEnable() => LocalizationManager.LanguageChanged += OnLanguageChanged;
    void OnDisable() => LocalizationManager.LanguageChanged -= OnLanguageChanged;

    // Título/pestañas se fijaban una vez en Build() y la lista solo se refresca por su timer
    // interno mientras el panel está abierto: sin esto, cambiar de idioma con el Santuario abierto
    // dejaba título y pestañas en el idioma anterior.
    private void OnLanguageChanged()
    {
        titulo.text = LocalizationManager.Get("UI_SANCTUARY");
        tabAscendLabel.text = LocalizationManager.Get("UI_ASCENSION_TAB");
        tabSynthLabel.text = LocalizationManager.Get("UI_SYNTHESIS_TAB");

        if (!IsOpen) return;
        RefreshList();
        RefreshRightPanel();
    }

    void Update()
    {
        if (!IsOpen) return;

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer > 0f) return;

        refreshTimer = refreshInterval;
        RefreshList();
        RefreshRightPanel();
    }

    public void Open()
    {
        if (panel == null) return;

        UIManager.OpenExclusive(panel);
        refreshTimer = refreshInterval;
        RefreshTabs();
        RefreshList();
        RefreshRightPanel();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        previewFodder = null;
        CloseSynthConfirm();
    }

    private void OnAscendTabPressed() => SetTab(SanctuaryTab.Ascension);
    private void OnSynthTabPressed() => SetTab(SanctuaryTab.Synthesis);

    private void SetTab(SanctuaryTab tab)
    {
        activeTab = tab;
        previewFodder = null;
        previewFodderPortrait.gameObject.SetActive(false);
        RefreshTabs();
        RefreshRightPanel();
    }

    private void RefreshTabs()
    {
        bool ascend = activeTab == SanctuaryTab.Ascension;

        tabAscend.targetGraphic.color = ascend ? UITheme.AccentPick : Color.clear;
        tabAscendLabel.color = ascend ? UITheme.Text : UITheme.TextFaint;

        tabSynth.targetGraphic.color = !ascend ? UITheme.AccentPick : Color.clear;
        tabSynthLabel.color = !ascend ? UITheme.Text : UITheme.TextFaint;
    }

    // Misma estrategia de pooling que RosterUI: actualizar filas ya creadas, no recrearlas,
    // para no caer de FPS con un roster de 50+ héroes.
    private void RefreshList()
    {
        if (content == null) return;

        var todos = UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None);

        scratch.Clear();
        foreach (var hero in todos)
            if (hero != null && hero.Data != null) scratch.Add(hero);

        scratch.Sort(CompareHeroes);

        for (int i = 0; i < scratch.Count; i++)
        {
            if (i >= pool.Count) pool.Add(CreateRow());

            var row = pool[i];
            row.root.SetActive(true);
            UpdateRow(row, scratch[i]);
        }

        for (int i = scratch.Count; i < pool.Count; i++)
            pool[i].root.SetActive(false);
    }

    private static int CompareHeroes(HeroController a, HeroController b)
    {
        if (a == null || a.Data == null) return 1;
        if (b == null || b.Data == null) return -1;

        int primary = b.StarRank.CompareTo(a.StarRank);
        if (primary != 0) return primary;

        return string.Compare(a.Data.heroName, b.Data.heroName, System.StringComparison.Ordinal);
    }

    private RowWidgets CreateRow()
    {
        var go = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(content, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, RowHeight);
        var cardImage = UITheme.Surface(go, UITheme.Card, UITheme.BorderSoft, UITheme.RadiusCard);

        var row = new RowWidgets { root = go };

        // 14px de margen (igual que RosterUI.CardPadX) para que el retrato no quede pegado al aro de la card.
        row.portraitFrame = BuildRowPortrait(go.transform, 14f);

        row.info = new GameObject("Info", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        row.info.transform.SetParent(go.transform, false);
        var irt = row.info.rectTransform;
        irt.anchorMin = new Vector2(0f, 0f);
        irt.anchorMax = new Vector2(1f, 1f);
        irt.pivot = new Vector2(0f, 0.5f);
        irt.offsetMin = new Vector2(14f + RowHeight - 12f, 4f);
        irt.offsetMax = new Vector2(-10f, -4f);
        row.info.fontSize = UITheme.SizeSmall;
        row.info.alignment = TextAlignmentOptions.Left;
        row.info.color = UITheme.Text;
        row.info.raycastTarget = false;

        // Nombres largos o idiomas con más caracteres no deben cortarse en la fila fija de 60px.
        row.info.enableAutoSizing = true;
        row.info.fontSizeMin = UITheme.SizeSmall * 0.75f;
        row.info.fontSizeMax = UITheme.SizeSmall;

        row.button = go.GetComponent<Button>();
        row.button.targetGraphic = cardImage;

        return row;
    }

    private Image BuildRowPortrait(Transform card, float x)
    {
        const float portraitSize = RowHeight - 12f;

        var go = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(card, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(portraitSize, portraitSize);
        rt.anchoredPosition = new Vector2(x, 0f);

        var image = UITheme.Surface(go, UITheme.Hex("262838"), UITheme.BorderSoft, UITheme.RadiusCard);
        image.raycastTarget = false;
        return image;
    }

    private void UpdateRow(RowWidgets row, HeroController hero)
    {
        row.hero = hero;

        var progress = hero.GetComponent<HeroProgress>();
        int level = progress != null ? progress.Level : 1;

        var stars = new StringBuilder();
        for (int i = 0; i < hero.StarRank; i++) stars.Append('★');

        var rareza = HeroProgress.RarityColor(hero.StarRank);
        var borde = row.portraitFrame.transform.Find("Border");
        UIBuild.HeroArt(row.portraitFrame.transform, hero.Data.bodySprite, RowHeight - 20f);

        bool esRol = (activeTab == SanctuaryTab.Ascension && hero == selectedAscendHero)
                     || (activeTab == SanctuaryTab.Synthesis && synthesis != null && synthesis.Target == hero);

        if (borde != null) borde.GetComponent<Image>().color = esRol ? UITheme.Amber : rareza;
        row.portraitFrame.transform.parent.GetComponent<Image>().color =
            esRol ? UITheme.Hex("332A14") : UITheme.Card;

        string estado = hero.IsLocked ? $"  <color={UITheme.Tag(UITheme.DangerLight)}>🔒</color>" : string.Empty;

        row.info.text = $"<color={UITheme.Tag(rareza)}><b>{stars}</b></color>  {hero.Data.heroName}{estado}\n" +
                        $"<size={UITheme.SizeSmall}><color={UITheme.Tag(UITheme.TextMuted)}>Nv.{level}</color></size>";

        row.button.onClick.RemoveAllListeners();
        row.button.onClick.AddListener(() => AudioManager.Play(SfxId.UiClick));
        row.button.onClick.AddListener(() => OnRowClicked(row.hero));
    }

    private void OnRowClicked(HeroController hero)
    {
        if (hero == null) return;

        if (activeTab == SanctuaryTab.Ascension)
        {
            selectedAscendHero = hero;
            RefreshList();
            RefreshRightPanel();
            return;
        }

        // Síntesis: primer clic fija el Receptor; el segundo, sobre otro héroe libre, lo
        // previsualiza en el panel derecho (la confirmación final vive en el overlay).
        if (synthesis == null) return;

        if (synthesis.HasTarget && synthesis.Target != hero && !hero.IsLocked)
        {
            previewFodder = hero;
            RefreshList();
            RefreshRightPanel();
            return;
        }

        previewFodder = null;
        synthesis.SelectHero(hero);
        RefreshList();
        RefreshRightPanel();
    }

    private void RefreshRightPanel()
    {
        if (activeTab == SanctuaryTab.Ascension) RefreshAscendPreview();
        else RefreshSynthPreview();
    }

    private void RefreshAscendPreview()
    {
        var hero = selectedAscendHero;
        if (hero == null || hero.Data == null)
        {
            selectedAscendHero = null;
            SetPreviewEmpty(LocalizationManager.Get("UI_SELECT_HERO"));
            return;
        }

        previewHint.gameObject.SetActive(false);
        previewPortrait.gameObject.SetActive(true);
        UIBuild.HeroArt(previewPortrait.transform, hero.Data.bodySprite, 88f);

        var progress = hero.GetComponent<HeroProgress>();
        var rareza = HeroProgress.RarityColor(hero.StarRank);
        previewName.text = $"<color={UITheme.Tag(rareza)}>{hero.Data.heroName}</color>";

        if (hero.StarRank >= 5)
        {
            previewBody.text = LocalizationManager.Get("UI_ASCEND_MAX_RARITY");
            SetPreviewAction(LocalizationManager.Get("UI_ASCEND"), false);
            return;
        }

        if (progress != null && !progress.IsMaxLevel)
        {
            previewBody.text = string.Format(LocalizationManager.Get("UI_ASCEND_NEED_LEVEL"), progress.MaxLevel);
            SetPreviewAction(LocalizationManager.Get("UI_ASCEND"), false);
            return;
        }

        var tier = progress != null ? progress.AscendStoneTier : AscensionStoneTier.Menor;
        int gemCost = progress != null ? progress.AscendGemCost : 0;
        float multiplier = progress != null ? progress.AscensionStatMultiplier : 1f;
        int gemsHeld = economy != null ? economy.Gems : 0;
        int stonesHeld = crafting != null ? crafting.StoneCount(tier) : 0;
        bool faltanGemas = gemsHeld < gemCost;
        bool faltaPiedra = stonesHeld < 1;

        // Antes solo mostraba el coste y desactivaba el botón sin decir POR QUÉ: el jugador veía
        // un héroe a Nv. tope "bloqueado" sin saber si le faltaban gemas o la Piedra exacta.
        previewBody.text =
            string.Format(LocalizationManager.Get("UI_ASCEND_PREVIEW"), hero.StarRank, hero.StarRank + 1, multiplier) +
            $"\n<color={UITheme.Tag(faltanGemas ? UITheme.DangerLight : UITheme.TextFaint)}>" +
            string.Format(LocalizationManager.Get("UI_ASCEND_NEED_GEMS"), gemsHeld, gemCost) + "</color>" +
            $"\n<color={UITheme.Tag(faltaPiedra ? UITheme.DangerLight : UITheme.TextFaint)}>" +
            string.Format(LocalizationManager.Get("UI_ASCEND_NEED_STONE"), AscensionStoneTiers.DisplayName(tier), stonesHeld) + "</color>";

        bool canAscend = progress != null && progress.CanAscend(economy, crafting);
        SetPreviewAction(LocalizationManager.Get("UI_ASCEND"), canAscend);
    }

    private void RefreshSynthPreview()
    {
        var target = synthesis != null ? synthesis.Target : null;
        if (target == null || target.Data == null)
        {
            previewFodder = null;
            previewHint.gameObject.SetActive(false);
            previewPortrait.gameObject.SetActive(false);
            previewFodderPortrait.gameObject.SetActive(false);
            previewName.text = string.Empty;
            previewBody.text = LocalizationManager.Get("UI_SELECT_HERO");
            SetPreviewAction(LocalizationManager.Get("UI_CANCEL"), false);
            return;
        }

        previewPortrait.gameObject.SetActive(true);
        UIBuild.HeroArt(previewPortrait.transform, target.Data.bodySprite, 88f);

        var rareza = HeroProgress.RarityColor(target.StarRank);
        previewName.text = $"<color={UITheme.Tag(UITheme.Amber)}>{LocalizationManager.Get("UI_RECEIVER")}</color>  " +
                           $"<color={UITheme.Tag(rareza)}>{target.Data.heroName}</color>";

        // Sin sacrificio elegido todavía: solo el hueco a la espera del segundo clic.
        if (previewFodder == null || previewFodder.Data == null)
        {
            previewFodderPortrait.gameObject.SetActive(false);
            previewBody.text = $"<color={UITheme.Tag(UITheme.TextFaint)}>{LocalizationManager.Get("UI_SACRIFICE")}</color>\n" +
                               LocalizationManager.Get("UI_SELECT_HERO");
            SetPreviewAction(LocalizationManager.Get("UI_CANCEL"), true);
            return;
        }

        // Sacrificio elegido: se muestran ambos héroes y el EXP exacto antes de pedir confirmación.
        previewFodderPortrait.gameObject.SetActive(true);
        UIBuild.HeroArt(previewFodderPortrait.transform, previewFodder.Data.bodySprite, 88f);

        var fodderRareza = HeroProgress.RarityColor(previewFodder.StarRank);
        var fodderStars = new StringBuilder();
        for (int i = 0; i < previewFodder.StarRank; i++) fodderStars.Append('★');

        int exp = synthesis.ExpFrom(previewFodder);
        previewBody.text = $"<color={UITheme.Tag(UITheme.TextFaint)}>{LocalizationManager.Get("UI_SACRIFICE")}</color>\n" +
                           $"<color={UITheme.Tag(fodderRareza)}><b>{fodderStars}</b></color>  {previewFodder.Data.heroName}\n" +
                           $"<size={UITheme.SizeSmall}><color={UITheme.Tag(UITheme.Amber)}>+{exp} " +
                           $"{LocalizationManager.Get("UI_EXP_GAINED")}</color></size>";

        SetPreviewAction(LocalizationManager.Get("UI_SYNTH"), true);
    }

    private void SetPreviewEmpty(string hint)
    {
        previewPortrait.gameObject.SetActive(false);
        previewFodderPortrait.gameObject.SetActive(false);
        previewName.text = string.Empty;
        previewBody.text = string.Empty;
        previewHint.text = hint;
        previewHint.gameObject.SetActive(true);
        SetPreviewAction(LocalizationManager.Get("UI_ASCEND"), false);
    }

    private void SetPreviewAction(string text, bool interactable)
    {
        previewActionLabel.text = text;
        previewAction.interactable = interactable;
        previewAction.targetGraphic.color = interactable ? UITheme.Amber : UITheme.Neutral;
        previewActionLabel.color = interactable ? UITheme.Text : UITheme.TextFaint;
    }

    // El botón de la derecha hace de acción principal según la pestaña: Ascender, o Cancelar
    // la selección de Síntesis (el sacrificio en sí solo ocurre tras el modal de confirmación).
    private void OnPreviewActionPressed()
    {
        if (activeTab == SanctuaryTab.Ascension)
        {
            if (selectedAscendHero == null) return;

            var progress = selectedAscendHero.GetComponent<HeroProgress>();
            if (progress != null) progress.AscendHero(economy, crafting);

            RefreshList();
            RefreshRightPanel();
            return;
        }

        // Con sacrificio ya previsualizado, este botón abre el overlay de confirmación final;
        // sin sacrificio elegido, cancela por completo la selección del receptor.
        if (previewFodder != null && synthesis != null && synthesis.HasTarget)
        {
            ShowSynthConfirm(synthesis.Target, previewFodder);
            return;
        }

        previewFodder = null;
        synthesis?.ClearTarget();
        RefreshList();
        RefreshRightPanel();
    }

    private void ShowSynthConfirm(HeroController target, HeroController fodder)
    {
        if (synthConfirmOverlay == null || target == null || fodder == null) return;

        pendingSynthTarget = target;
        pendingSynthFodder = fodder;

        int exp = synthesis != null ? synthesis.ExpFrom(fodder) : 0;
        string fodderName = fodder.Data != null ? fodder.Data.heroName : fodder.name;
        string targetName = target.Data != null ? target.Data.heroName : target.name;

        synthConfirmMessage.text = string.Format(
            LocalizationManager.Get("UI_SYNTH_CONFIRM_MSG"), fodderName, exp, targetName);

        synthConfirmOverlay.SetActive(true);
        synthConfirmOverlay.transform.SetAsLastSibling();
    }

    private void OnConfirmSynthPressed()
    {
        if (synthesis != null && pendingSynthTarget != null && pendingSynthFodder != null)
            synthesis.Synthesize(pendingSynthTarget, pendingSynthFodder);

        previewFodder = null;
        CloseSynthConfirm();
        RefreshList();
        RefreshRightPanel();
    }

    private void OnCancelSynthPressed() => CloseSynthConfirm();

    private void CloseSynthConfirm()
    {
        pendingSynthTarget = null;
        pendingSynthFodder = null;
        if (synthConfirmOverlay != null) synthConfirmOverlay.SetActive(false);
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "SanctuaryPanel", size, UITheme.Bg);

        titulo = UIBuild.TopLabel(panel.transform, "Title", UITheme.SizeTitle, 34f, -20f,
            TextAlignmentOptions.Left);
        titulo.text = LocalizationManager.Get("UI_SANCTUARY");
        // Deja sitio al botón [X] (48px + 16px de margen).
        titulo.rectTransform.offsetMax = new Vector2(-72f, titulo.rectTransform.offsetMax.y);

        UIBuild.CloseButtonTopRight(panel.transform, Close);

        BuildTabs();
        BuildList();
        BuildRightPanel();
        BuildSynthConfirm();

        panel.SetActive(false);
    }

    private void BuildTabs()
    {
        tabAscend = UIBuild.Button(panel.transform, "Tab_Ascension", LocalizationManager.Get("UI_ASCENSION_TAB"),
            Color.clear, new Vector2(160f, 36f), new Vector2(24f, -64f), OnAscendTabPressed);
        var atrt = tabAscend.GetComponent<RectTransform>();
        atrt.anchorMin = new Vector2(0f, 1f);
        atrt.anchorMax = new Vector2(0f, 1f);
        atrt.pivot = new Vector2(0f, 1f);
        atrt.anchoredPosition = new Vector2(24f, -64f);
        UITheme.Surface(tabAscend.gameObject, Color.clear, UITheme.BorderStrong, UITheme.RadiusButton);
        tabAscend.targetGraphic = tabAscend.GetComponent<Image>();
        tabAscendLabel = tabAscend.GetComponentInChildren<TMP_Text>();

        tabSynth = UIBuild.Button(panel.transform, "Tab_Synthesis", LocalizationManager.Get("UI_SYNTHESIS_TAB"),
            Color.clear, new Vector2(160f, 36f), new Vector2(24f + 160f + 10f, -64f), OnSynthTabPressed);
        var strt = tabSynth.GetComponent<RectTransform>();
        strt.anchorMin = new Vector2(0f, 1f);
        strt.anchorMax = new Vector2(0f, 1f);
        strt.pivot = new Vector2(0f, 1f);
        strt.anchoredPosition = new Vector2(24f + 160f + 10f, -64f);
        UITheme.Surface(tabSynth.gameObject, Color.clear, UITheme.BorderStrong, UITheme.RadiusButton);
        tabSynth.targetGraphic = tabSynth.GetComponent<Image>();
        tabSynthLabel = tabSynth.GetComponentInChildren<TMP_Text>();
    }

    // Scroll vertical con pooling: mismo patrón de SideMenuUI (mask + layout + content size fitter).
    private void BuildList()
    {
        const float listTop = -112f;
        const float listBottom = 24f;

        var viewGo = new GameObject("ListViewport", typeof(RectTransform), typeof(Image),
                                    typeof(Mask), typeof(ScrollRect));
        viewGo.transform.SetParent(panel.transform, false);

        var vrt = viewGo.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0f, 0f);
        vrt.anchorMax = new Vector2(0f, 1f);
        vrt.pivot = new Vector2(0f, 1f);
        vrt.offsetMin = new Vector2(24f, listBottom);
        vrt.offsetMax = new Vector2(24f + ListWidth, listTop);

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
        // Margen holgado en las 4 direcciones: con 20/10/6/6 los textos largos («Lv.30が必要です»,
            // nombres de héroe) y la tarjeta quedaban apretados contra los bordes del modal.
            layout.padding = new RectOffset(35, 35, 20, 20);
            layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        bodyGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void BuildRightPanel()
    {
        float x = RightX;

        var frame = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(panel.transform, false);
        var frt = frame.GetComponent<RectTransform>();
        frt.anchorMin = new Vector2(0f, 1f);
        frt.anchorMax = new Vector2(0f, 1f);
        frt.pivot = new Vector2(0f, 1f);
        frt.sizeDelta = new Vector2(96f, 96f);
        frt.anchoredPosition = new Vector2(x, -112f);
        previewPortrait = UITheme.Surface(frame, UITheme.Hex("262838"), UITheme.BorderSoft, UITheme.RadiusCard);
        previewPortrait.raycastTarget = false;

        // Miniatura del sacrificio en Síntesis: misma tarjeta que el Receptor (tamaño y anclaje),
        // justo a su lado, para que ambos lean como el mismo tipo de ficha. Oculta hasta el 2º clic.
        var fodderFrame = new GameObject("FodderPortrait", typeof(RectTransform), typeof(Image));
        fodderFrame.transform.SetParent(panel.transform, false);
        var ffrt = fodderFrame.GetComponent<RectTransform>();
        ffrt.anchorMin = new Vector2(0f, 1f);
        ffrt.anchorMax = new Vector2(0f, 1f);
        ffrt.pivot = new Vector2(0f, 1f);
        ffrt.sizeDelta = new Vector2(96f, 96f);
        ffrt.anchoredPosition = new Vector2(x + 96f + 12f, -112f);
        previewFodderPortrait = UITheme.Surface(fodderFrame, UITheme.Hex("262838"), UITheme.DangerLight, UITheme.RadiusCard);
        previewFodderPortrait.raycastTarget = false;
        previewFodderPortrait.gameObject.SetActive(false);

        // Debajo de las dos tarjetas (Receptor + Sacrificio, 96px cada una): así el nombre nunca
        // queda detrás de la tarjeta del sacrificio, que ahora ocupa el mismo ancho que antes.
        previewName = UIBuild.Label(panel.transform, "PreviewName", UITheme.SizeName, TextAlignmentOptions.Left);
        AnchorTopLeft(previewName.rectTransform, x, -220f, size.x - RightX - 38f, 32f);

        previewBody = UIBuild.Label(panel.transform, "PreviewBody", UITheme.SizeBody, TextAlignmentOptions.TopLeft);
        previewBody.color = UITheme.TextSoft;
        previewBody.lineSpacing = 12f;
        AnchorTopLeft(previewBody.rectTransform, x, -256f, size.x - RightX - 38f, 188f);

        previewHint = UIBuild.Label(panel.transform, "PreviewHint", UITheme.SizeBody, TextAlignmentOptions.TopLeft);
        previewHint.color = UITheme.TextFaint;
        AnchorTopLeft(previewHint.rectTransform, x, -140f, size.x - RightX - 38f, 60f);

        previewAction = UIBuild.Button(panel.transform, "Btn_PreviewAction", string.Empty, UITheme.Amber,
            new Vector2(220f, 52f), new Vector2(x, -(size.y - 62f)), OnPreviewActionPressed);
        var part = previewAction.GetComponent<RectTransform>();
        part.anchorMin = new Vector2(0f, 1f);
        part.anchorMax = new Vector2(0f, 1f);
        part.pivot = new Vector2(0f, 1f);
        part.anchoredPosition = new Vector2(x, -(size.y - 62f));
        previewActionLabel = previewAction.GetComponentInChildren<TMP_Text>();
    }

    private static void AnchorTopLeft(RectTransform rt, float x, float y, float width, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(x, y);
    }

    // Fondo oscurecido + caja centrada, montado sobre el propio panel del modal.
    private void BuildSynthConfirm()
    {
        synthConfirmOverlay = new GameObject("SynthConfirmOverlay", typeof(RectTransform), typeof(Image));
        synthConfirmOverlay.transform.SetParent(panel.transform, false);
        UIBuild.Stretch(synthConfirmOverlay.GetComponent<RectTransform>());
        synthConfirmOverlay.GetComponent<Image>().color = UITheme.Hex("05060A", 0.72f);

        var box = UIBuild.Panel(synthConfirmOverlay.transform, "SynthConfirmBox",
            new Vector2(460f, 260f), UITheme.BgPanel);

        var titulo = UIBuild.TopLabel(box.transform, "Title", UITheme.SizeName, 30f, -20f,
            TextAlignmentOptions.Center);
        titulo.text = LocalizationManager.Get("UI_SYNTH_CONFIRM_TITLE");

        synthConfirmMessage = UIBuild.TopLabel(box.transform, "Message", UITheme.SizeBody, 130f, -56f,
            TextAlignmentOptions.Center);
        synthConfirmMessage.color = UITheme.TextSoft;

        UIBuild.Button(box.transform, "Btn_ConfirmSynth", LocalizationManager.Get("UI_SYNTH"),
            UITheme.DangerSoft, new Vector2(180f, 48f), new Vector2(-100f, -200f), OnConfirmSynthPressed);

        UIBuild.Button(box.transform, "Btn_CancelSynth", LocalizationManager.Get("UI_CANCEL"),
            UITheme.Neutral, new Vector2(180f, 48f), new Vector2(100f, -200f), OnCancelSynthPressed);

        synthConfirmOverlay.SetActive(false);
    }
}
