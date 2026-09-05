using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Modal del Altar: tirada simple o múltiple, revelación de cartas y alta en el roster al aceptar.
public class SummonAltarUI : MonoBehaviour
{
    [Tooltip("Canvas sobre el que se monta el modal.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Gacha que sortea y crea a los héroes.")]
    [SerializeField] private GachaManager gacha;

    [Tooltip("Economía que cobra las tiradas.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Coste en gemas de la tirada simple.")]
    [SerializeField] private int singleCost = 150;

    [Tooltip("Tiradas que entran en la invocación múltiple.")]
    [SerializeField] private int multiPulls = 10;

    [Tooltip("Coste en gemas de la tirada múltiple.")]
    [SerializeField] private int multiCost = 1350;

    [Tooltip("Tamaño del modal.")]
    [SerializeField] private Vector2 size = new Vector2(1200f, 780f);

    private GameObject panel;
    private TMP_Text titulo;
    private TMP_Text saldo;
    private TMP_Text aviso;
    private Button botonSimple;
    private Button botonMultiple;
    private Button botonRevelar;
    private Button botonAceptar;
    private TMP_Text etiquetaSimple;
    private TMP_Text etiquetaMultiple;
    private TMP_Text etiquetaRevelar;
    private TMP_Text etiquetaAceptar;
    private TMP_Text etiquetaCerrar;
    private RectTransform rejilla;

    private readonly List<Carta> cartas = new List<Carta>();

    // Cada carta guarda su marco, su retrato y su texto para poder darle la vuelta.
    private class Carta
    {
        public GameObject raiz;
        public Image marco;
        public Image retrato;
        public Image retratoBorde;
        public TMP_Text inicial;
        public TMP_Text texto;
        public HeroData datos;
        public bool revelada;
    }

    public int SingleCost => singleCost;
    public int MultiCost => multiCost;
    public int CardCount => cartas.Count;
    public bool IsOpen => panel != null && panel.activeSelf;

    // Hay tirada sin resolver mientras queden cartas en la mesa.
    public bool HasPending => cartas.Count > 0;

    public bool AllRevealed
    {
        get
        {
            foreach (var carta in cartas)
                if (!carta.revelada) return false;

            return true;
        }
    }

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (gacha == null) gacha = UnityEngine.Object.FindFirstObjectByType<GachaManager>();
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();

        Build();
    }

    void OnEnable() => LocalizationManager.LanguageChanged += RefreshTexts;
    void OnDisable() => LocalizationManager.LanguageChanged -= RefreshTexts;

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (panel == null) return;

        aviso.text = string.Empty;
        RefreshTexts();

        UIManager.OpenExclusive(panel);
    }

    // Cerrar con cartas sin revelar se saltaría la animación de revelado: hay que revelar
    // primero. Con todo ya revelado, cerrar acepta la tirada antes de irse.
    public void Close()
    {
        if (HasPending && !AllRevealed) return;
        if (HasPending) Accept();
        if (panel != null) panel.SetActive(false);
    }

    public void OnSinglePressed() => Pull(1, singleCost);
    public void OnMultiPressed() => Pull(multiPulls, multiCost);

    // Cobra y reparte cartas boca abajo; nadie aparece en la base hasta que se acepte.
    private void Pull(int cantidad, int coste)
    {
        if (gacha == null || economy == null) return;

        // Con cartas boca abajo en la mesa no se permite otra tirada: se saltaría su animación.
        if (HasPending && !AllRevealed) return;

        // Una tanda ya revelada pero sin aceptar se acepta primero: si no, se perderían esos héroes.
        if (HasPending) Accept();

        if (!gacha.HasAvailableHeroes())
        {
            aviso.text = LocalizationManager.Get("UI_CATALOG_FULL");
            return;
        }

        if (!economy.CanAfford(coste))
        {
            aviso.text = LocalizationManager.Get("UI_NO_GEMS");
            return;
        }

        if (!economy.TrySpend(coste)) return;

        // Los sacados en esta misma tanda no pueden repetirse aunque aún no estén en la base.
        var sacados = new HashSet<string>();
        int devolver = 0;

        for (int i = 0; i < cantidad; i++)
        {
            // Sin héroes libres se corta: lo no gastado vuelve al saldo.
            if (!gacha.HasAvailableHeroes(sacados))
            {
                devolver = Mathf.RoundToInt((float)coste / cantidad * (cantidad - i));
                break;
            }

            var datos = gacha.PerformPull(sacados);
            if (datos == null) break;

            sacados.Add(datos.heroName);
            CreateCard(datos);
        }

        if (devolver > 0) economy.Add(devolver);

        aviso.text = string.Empty;
        RefreshTexts();

        Debug.Log($"[Altar] {cartas.Count} carta(s) en la mesa por {coste - devolver} gemas.", this);
    }

    // Aquí es donde los héroes entran de verdad al roster y aparecen en la base.
    public void Accept()
    {
        if (gacha == null || cartas.Count == 0) return;

        int altas = 0;
        foreach (var carta in cartas)
        {
            if (carta.datos == null) continue;

            var heroe = gacha.SpawnHero(carta.datos, HeroTraits.Random(), SpawnPoint());
            if (heroe == null) continue;

            heroe.SetPassives(PassiveSkills.RandomSet(heroe.StarRank));
            gacha.GrantStarterWeapon(heroe);
            altas++;
        }

        ClearCards();
        SaveManager.RequestSave();
        RefreshTexts();

        Debug.Log($"[Altar] {altas} héroe(s) dados de alta en el roster.", this);
    }

    private Vector2 SpawnPoint()
        => SummonAltar.TryGetSpawnPoint(out var punto) ? punto : gacha.RandomSpawnPosition();

    private void ClearCards()
    {
        foreach (var carta in cartas)
            if (carta.raiz != null) Destroy(carta.raiz);

        cartas.Clear();
    }

    // Boca abajo hasta que se toca; el retrato y el aro se tinen con la rareza al revelarse.
    private void CreateCard(HeroData datos)
    {
        var go = new GameObject($"Card_{cartas.Count}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(rejilla, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 230f);

        UITheme.Surface(go, UITheme.Card, UITheme.Border, UITheme.RadiusCard);

        // Hueco del arte: cuadro enmarcado con la inicial mientras no haya retratos.
        var retratoGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        retratoGo.transform.SetParent(go.transform, false);

        var prt = retratoGo.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 1f);
        prt.anchorMax = new Vector2(0.5f, 1f);
        prt.pivot = new Vector2(0.5f, 1f);
        prt.sizeDelta = new Vector2(104f, 104f);
        prt.anchoredPosition = new Vector2(0f, -14f);

        var retrato = UITheme.Surface(retratoGo, UITheme.Hex("262838"), UITheme.Border, UITheme.RadiusCard);
        retrato.raycastTarget = false;

        var inicial = UIBuild.Label(retratoGo.transform, "Initial", 40f, TextAlignmentOptions.Center);
        UIBuild.Stretch(inicial.rectTransform);
        inicial.text = "?";
        inicial.color = UITheme.TextFaint;

        var texto = UIBuild.Label(go.transform, "Text", UITheme.SizeCaption, TextAlignmentOptions.Top);
        var trt = texto.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.offsetMin = new Vector2(8f, 0f);
        trt.offsetMax = new Vector2(-8f, 0f);
        trt.sizeDelta = new Vector2(-16f, 100f);
        trt.anchoredPosition = new Vector2(0f, -124f);
        texto.lineSpacing = 6f;
        texto.color = UITheme.TextMuted;
        texto.text = LocalizationManager.Get("UI_REVEAL");

        var carta = new Carta
        {
            raiz = go,
            marco = go.transform.Find("Border").GetComponent<Image>(),
            retrato = retrato,
            retratoBorde = retratoGo.transform.Find("Border").GetComponent<Image>(),
            inicial = inicial,
            texto = texto,
            datos = datos,
            revelada = false
        };

        int indice = cartas.Count;
        go.GetComponent<Button>().onClick.AddListener(() => Reveal(indice));
        cartas.Add(carta);
    }

    public void Reveal(int indice)
    {
        if (indice < 0 || indice >= cartas.Count) return;

        var carta = cartas[indice];
        if (carta.revelada || carta.datos == null) return;

        carta.revelada = true;
        AudioManager.Play(SfxId.CardReveal);

        var rareza = UITheme.Rarity(carta.datos.starRank);
        carta.marco.color = rareza;

        // El retrato hereda el color de la rareza y dentro va el pixel art del héroe.
        carta.retratoBorde.color = rareza;

        var arte = UIBuild.HeroArt(carta.retrato.transform, carta.datos.bodySprite, 88f);

        // Sin sprite todavía, la inicial sigue haciendo de arte provisional.
        carta.inicial.gameObject.SetActive(arte == null);
        carta.inicial.color = rareza;
        carta.inicial.text = carta.datos.heroName.Substring(0, 1).ToUpperInvariant();

        var estrellas = new System.Text.StringBuilder();
        for (int i = 0; i < carta.datos.starRank; i++) estrellas.Append('★');

        carta.texto.fontSize = UITheme.SizeName;
        carta.texto.color = UITheme.Text;
        carta.texto.text = $"<size={UITheme.SizeValue}><color={UITheme.Tag(rareza)}>{estrellas}</color></size>\n" +
                           $"<b>{carta.datos.heroName}</b>\n" +
                           $"<size={UITheme.SizeCaption}><color={UITheme.Tag(UITheme.TextSoft)}>" +
                           $"{LocalizationManager.GetTitle(carta.datos.title)}</color></size>\n" +
                           $"<size={UITheme.SizeMicro}><color={UITheme.Tag(UITheme.TextFaint)}>" +
                           $"{LocalizationManager.GetOrigin(carta.datos.origin)}</color></size>";

        RefreshTexts();
    }

    // Da la vuelta a todas de golpe; lo usa el botón de revelar y la verificación.
    public void RevealAll()
    {
        for (int i = 0; i < cartas.Count; i++) Reveal(i);
    }

    private void RefreshTexts()
    {
        if (titulo == null) return;

        titulo.text = LocalizationManager.Get("UI_SUMMON_ALTAR");

        if (economy != null)
            saldo.text = $"<color={UITheme.Tag(UITheme.Cyan)}>◆</color> <b>{economy.Gems}</b>";

        etiquetaSimple.text = $"{LocalizationManager.Get("UI_SUMMON_X1")}\n" +
                              $"<size={UITheme.SizeCaption}>{singleCost} ◆</size>";
        etiquetaMultiple.text = $"{LocalizationManager.Get("UI_SUMMON_X10")} ×{multiPulls}\n" +
                                $"<size={UITheme.SizeCaption}>{multiCost} ◆</size>";
        etiquetaRevelar.text = LocalizationManager.Get("UI_REVEAL_ALL");
        etiquetaAceptar.text = LocalizationManager.Get("UI_ACCEPT");
        etiquetaCerrar.text = LocalizationManager.Get("UI_CLOSE");

        // Con cartas boca abajo esperando revelación, los botones de tirada se bloquean del todo.
        bool bloqueadoPorRevelar = HasPending && !AllRevealed;
        bool puedeSimple = economy != null && economy.CanAfford(singleCost) && !bloqueadoPorRevelar;
        bool puedeMultiple = economy != null && economy.CanAfford(multiCost) && !bloqueadoPorRevelar;

        botonSimple.interactable = puedeSimple;
        botonMultiple.interactable = puedeMultiple;
        botonSimple.targetGraphic.color = puedeSimple ? UITheme.AccentSoft : UITheme.Neutral;
        botonMultiple.targetGraphic.color = puedeMultiple ? UITheme.AccentPick : UITheme.Neutral;

        // Revelar solo si queda alguna boca abajo; aceptar solo con todas descubiertas.
        botonRevelar.interactable = HasPending && !AllRevealed;
        botonAceptar.interactable = HasPending && AllRevealed;
        botonRevelar.targetGraphic.color = botonRevelar.interactable ? UITheme.Teal : UITheme.Neutral;
        botonAceptar.targetGraphic.color = botonAceptar.interactable ? UITheme.Amber : UITheme.Neutral;
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "SummonAltarPanel", size, UITheme.Bg);

        titulo = UIBuild.TopLabel(panel.transform, "Title", UITheme.SizeTitle, 36f, -22f,
            TextAlignmentOptions.Left);

        saldo = UIBuild.TopLabel(panel.transform, "Gems", UITheme.SizeValue, 32f, -22f,
            TextAlignmentOptions.Right);

        // Rejilla de cartas: cinco por fila para que la tirada de diez entre en dos filas.
        var rejillaGo = new GameObject("Cards", typeof(RectTransform), typeof(GridLayoutGroup));
        rejillaGo.transform.SetParent(panel.transform, false);

        rejilla = rejillaGo.GetComponent<RectTransform>();
        rejilla.anchorMin = new Vector2(0f, 1f);
        rejilla.anchorMax = new Vector2(1f, 1f);
        rejilla.pivot = new Vector2(0.5f, 1f);
        rejilla.offsetMin = new Vector2(24f, 0f);
        rejilla.offsetMax = new Vector2(-24f, 0f);
        rejilla.sizeDelta = new Vector2(-48f, 490f);
        rejilla.anchoredPosition = new Vector2(0f, -72f);

        var grid = rejillaGo.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(190f, 230f);
        grid.spacing = new Vector2(14f, 14f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.UpperCenter;

        aviso = UIBuild.TopLabel(panel.transform, "Notice", UITheme.SizeBody, 28f, -574f,
            TextAlignmentOptions.Center);
        aviso.color = UITheme.DangerLight;

        // Cuatro acciones en una sola fila: tirar, tirar x10, revelar y aceptar.
        const float ancho = 270f;
        const float hueco = 12f;
        const float y = -618f;
        float x = -(ancho * 1.5f + hueco * 1.5f);

        botonSimple = UIBuild.Button(panel.transform, "Btn_SummonX1", string.Empty,
            UITheme.AccentSoft, new Vector2(ancho, 62f), new Vector2(x, y), OnSinglePressed);
        etiquetaSimple = botonSimple.GetComponentInChildren<TMP_Text>();

        x += ancho + hueco;
        botonMultiple = UIBuild.Button(panel.transform, "Btn_SummonX10", string.Empty,
            UITheme.AccentPick, new Vector2(ancho, 62f), new Vector2(x, y), OnMultiPressed);
        etiquetaMultiple = botonMultiple.GetComponentInChildren<TMP_Text>();

        x += ancho + hueco;
        botonRevelar = UIBuild.Button(panel.transform, "Btn_RevealAll", string.Empty,
            UITheme.Teal, new Vector2(ancho, 62f), new Vector2(x, y), RevealAll);
        etiquetaRevelar = botonRevelar.GetComponentInChildren<TMP_Text>();

        x += ancho + hueco;
        botonAceptar = UIBuild.Button(panel.transform, "Btn_AcceptSummon", string.Empty,
            UITheme.Amber, new Vector2(ancho, 62f), new Vector2(x, y), Accept);
        etiquetaAceptar = botonAceptar.GetComponentInChildren<TMP_Text>();

        var cerrar = UIBuild.Button(panel.transform, "Btn_CloseSummon", string.Empty,
            Color.clear, new Vector2(240f, 46f), new Vector2(0f, -700f), Close);
        etiquetaCerrar = cerrar.GetComponentInChildren<TMP_Text>();
    }
}
