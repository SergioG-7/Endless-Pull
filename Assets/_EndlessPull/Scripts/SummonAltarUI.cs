using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Modal del Altar: tirada simple o múltiple y revelación de las cartas obtenidas.
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

    [Tooltip("Descuento de la tirada múltiple, de 0 a 1.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float multiDiscount = 0.10f;

    [Tooltip("Tamaño del modal.")]
    [SerializeField] private Vector2 size = new Vector2(1200f, 760f);

    private GameObject panel;
    private TMP_Text titulo;
    private TMP_Text saldo;
    private TMP_Text aviso;
    private Button botonSimple;
    private Button botonMultiple;
    private TMP_Text etiquetaSimple;
    private TMP_Text etiquetaMultiple;
    private RectTransform rejilla;

    private readonly List<Carta> cartas = new List<Carta>();

    // Cada carta guarda su marco y su texto para poder darle la vuelta.
    private class Carta
    {
        public GameObject raiz;
        public Image marco;
        public TMP_Text texto;
        public HeroData datos;
        public bool revelada;
    }

    public int SingleCost => singleCost;

    // El descuento se aplica sobre el total, no sobre cada tirada.
    public int MultiCost => Mathf.RoundToInt(singleCost * multiPulls * (1f - multiDiscount));

    public bool IsOpen => panel != null && panel.activeSelf;

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

    public void Open()
    {
        if (panel == null) return;

        ClearCards();
        aviso.text = string.Empty;
        RefreshTexts();

        UIManager.OpenExclusive(panel);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void OnSinglePressed() => Pull(1, singleCost);
    public void OnMultiPressed() => Pull(multiPulls, MultiCost);

    // Cobra una vez y reparte todas las tiradas; si el catálogo se agota, se para y devuelve el resto.
    private void Pull(int cantidad, int coste)
    {
        if (gacha == null || economy == null) return;

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

        ClearCards();

        int hechas = 0;
        int devolver = 0;

        for (int i = 0; i < cantidad; i++)
        {
            // Sin héroes libres se corta: lo no gastado vuelve al saldo.
            if (!gacha.HasAvailableHeroes())
            {
                devolver = Mathf.RoundToInt((float)coste / cantidad * (cantidad - i));
                break;
            }

            var datos = gacha.PerformPull();
            if (datos == null) break;

            var heroe = gacha.SpawnHero(datos, HeroTraits.Random(), SpawnPoint());
            if (heroe != null) hechas++;

            CreateCard(datos);
        }

        if (devolver > 0) economy.Add(devolver);

        aviso.text = string.Empty;
        RefreshTexts();
        SaveManager.RequestSave();

        Debug.Log($"[Altar] {hechas} héroe(s) invocado(s) por {coste - devolver} gemas.", this);
    }

    private Vector2 SpawnPoint()
        => SummonAltar.TryGetSpawnPoint(out var punto) ? punto : gacha.RandomSpawnPosition();

    private void ClearCards()
    {
        foreach (var carta in cartas)
            if (carta.raiz != null) Destroy(carta.raiz);

        cartas.Clear();
    }

    // Boca abajo hasta que se toca; el aro se tiñe con la rareza al revelarse.
    private void CreateCard(HeroData datos)
    {
        var go = new GameObject($"Card_{cartas.Count}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(rejilla, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 250f);

        var fondo = UITheme.Surface(go, UITheme.Card, UITheme.Border, UITheme.RadiusCard);

        var texto = UIBuild.Label(go.transform, "Text", UITheme.SizeCaption, TextAlignmentOptions.Center);
        UIBuild.Stretch(texto.rectTransform);
        texto.rectTransform.offsetMin = new Vector2(10f, 10f);
        texto.rectTransform.offsetMax = new Vector2(-10f, -10f);
        texto.text = "?";
        texto.fontSize = UITheme.SizeTitle;
        texto.color = UITheme.TextMuted;

        var carta = new Carta
        {
            raiz = go,
            marco = go.transform.Find("Border").GetComponent<Image>(),
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

        var rareza = UITheme.Rarity(carta.datos.starRank);
        carta.marco.color = rareza;

        var estrellas = new System.Text.StringBuilder();
        for (int i = 0; i < carta.datos.starRank; i++) estrellas.Append('★');

        carta.texto.fontSize = UITheme.SizeName;
        carta.texto.color = UITheme.Text;
        carta.texto.text = $"<size={UITheme.SizeCaption}><color={UITheme.Tag(rareza)}>{estrellas}</color></size>\n" +
                           $"<b>{carta.datos.heroName}</b>\n" +
                           $"<size={UITheme.SizeCaption}><color={UITheme.Tag(UITheme.TextMuted)}>" +
                           $"{carta.datos.origin}</color></size>";
    }

    // Da la vuelta a todas de golpe; lo usa el botón de revelar y la verificación.
    public void RevealAll()
    {
        for (int i = 0; i < cartas.Count; i++) Reveal(i);
    }

    public int CardCount => cartas.Count;

    private void RefreshTexts()
    {
        if (titulo == null) return;

        titulo.text = LocalizationManager.Get("UI_SUMMON_ALTAR");

        if (economy != null)
            saldo.text = $"<color={UITheme.Tag(UITheme.Cyan)}>◆</color> <b>{economy.Gems}</b>";

        etiquetaSimple.text = $"{LocalizationManager.Get("UI_SUMMON_X1")}  ·  {singleCost} ◆";
        etiquetaMultiple.text = $"{LocalizationManager.Get("UI_SUMMON_X10")} ×{multiPulls}  ·  {MultiCost} ◆" +
                                $"   <size={UITheme.SizeCaption}><color={UITheme.Tag(UITheme.TextMuted)}>" +
                                $"{LocalizationManager.Get("UI_DISCOUNT")}</color></size>";

        bool puedeSimple = economy != null && economy.CanAfford(singleCost);
        bool puedeMultiple = economy != null && economy.CanAfford(MultiCost);

        botonSimple.interactable = puedeSimple;
        botonMultiple.interactable = puedeMultiple;
        botonSimple.targetGraphic.color = puedeSimple ? UITheme.AccentSoft : UITheme.Neutral;
        botonMultiple.targetGraphic.color = puedeMultiple ? UITheme.AccentPick : UITheme.Neutral;
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
        rejilla.sizeDelta = new Vector2(-48f, 530f);
        rejilla.anchoredPosition = new Vector2(0f, -80f);

        var grid = rejillaGo.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(190f, 250f);
        grid.spacing = new Vector2(14f, 14f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.UpperCenter;

        aviso = UIBuild.TopLabel(panel.transform, "Notice", UITheme.SizeBody, 30f, -620f,
            TextAlignmentOptions.Center);
        aviso.color = UITheme.DangerLight;

        botonSimple = UIBuild.Button(panel.transform, "Btn_SummonX1", string.Empty,
            UITheme.AccentSoft, new Vector2(520f, 60f), new Vector2(-280f, -(size.y - 140f)),
            OnSinglePressed);
        etiquetaSimple = botonSimple.GetComponentInChildren<TMP_Text>();

        botonMultiple = UIBuild.Button(panel.transform, "Btn_SummonX10", string.Empty,
            UITheme.AccentPick, new Vector2(520f, 60f), new Vector2(280f, -(size.y - 140f)),
            OnMultiPressed);
        etiquetaMultiple = botonMultiple.GetComponentInChildren<TMP_Text>();

        UIBuild.Button(panel.transform, "Btn_CloseSummon", LocalizationManager.Get("UI_CLOSE"),
            Color.clear, new Vector2(240f, 48f), new Vector2(0f, -(size.y - 66f)), Close);
    }
}
