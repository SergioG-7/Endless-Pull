using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Taller de Alquimia: tres tarjetas compactas (piedras, armas y reparación). Todo se paga
// con madera, hierro y comida; las gemas quedan para el gacha y las recargas.
public class AlchemyWorkshopUI : MonoBehaviour
{
    [Tooltip("Canvas sobre el que se monta el modal.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Taller que forja las piedras y fabrica las armas.")]
    [SerializeField] private CraftingManager crafting;

    [Tooltip("Economía de la que salen los materiales.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Tamaño del modal.")]
    [SerializeField] private Vector2 size = new Vector2(1000f, 482f);

    [Tooltip("Color del texto cuando la operación sale bien.")]
    [SerializeField] private Color successColor = new Color(0.45f, 0.95f, 0.55f);

    private const float CardWidth = 300f;
    private const float CardHeight = 268f;
    private const float CardGap = 20f;

    // La tarjeta se reparte en cuatro bandas: título, cuerpo, coste y botón al pie.
    private const float CardTop = 76f;
    private const float ButtonHeight = 48f;

    private GameObject panel;
    private TMP_Text titulo;
    private TMP_Text recursos;
    private TMP_Text feedback;
    private TMP_Text etiquetaCerrar;

    // Una ficha por operación: cuerpo con el detalle, coste y su botón.
    private Ficha piedras;
    private Ficha armas;
    private Ficha reparar;

    private class Ficha
    {
        public TMP_Text titulo;
        public TMP_Text cuerpo;
        public TMP_Text coste;
        public Button boton;
        public TMP_Text etiqueta;
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();

        Build();
    }

    void OnEnable()
    {
        LocalizationManager.LanguageChanged += Refresh;
        if (crafting != null) crafting.CraftResolved += OnResolved;
    }

    void OnDisable()
    {
        LocalizationManager.LanguageChanged -= Refresh;
        if (crafting != null) crafting.CraftResolved -= OnResolved;
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    // Los materiales cambian solos con la granja: mientras esté abierto se repasa.
    void Update()
    {
        if (IsOpen) Refresh();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (panel == null) return;

        if (feedback != null) feedback.text = string.Empty;
        UIManager.OpenExclusive(panel);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void OnCraftStonePressed()
    {
        if (crafting != null) crafting.TryCraftAscensionStone();
        Refresh();
    }

    public void OnCraftWeaponPressed()
    {
        if (crafting != null) crafting.TryCraftWeapon();
        Refresh();
    }

    public void OnRepairAllPressed()
    {
        if (crafting != null) crafting.TryRepairAll();
        Refresh();
    }

    private void OnResolved(bool success, string message)
    {
        if (feedback == null) return;

        feedback.text = message;
        feedback.color = success ? successColor : UITheme.DangerLight;
    }

    private void Refresh()
    {
        if (titulo == null || crafting == null) return;

        titulo.text = LocalizationManager.Get("UI_WORKSHOP");

        int artesanos = crafting.Artisans;
        string rebaja = artesanos > 0
            ? $"   <color={UITheme.Tag(UITheme.Cyan)}>" +
              $"{LocalizationManager.Get("UI_ARTISANS")} {artesanos} " +
              $"(-{Mathf.RoundToInt((1f - crafting.CostFactor) * 100f)}%)</color>"
            : string.Empty;

        recursos.text = economy != null
            ? $"{LocalizationManager.Get("UI_WOOD")} <b>{economy.Wood}</b>   " +
              $"{LocalizationManager.Get("UI_IRON")} <b>{economy.Iron}</b>   " +
              $"{LocalizationManager.Get("UI_FOOD")} <b>{economy.Food}</b>{rebaja}"
            : string.Empty;

        // Forja de Piedras
        piedras.titulo.text = LocalizationManager.Get("UI_FORGE_STONES");
        piedras.cuerpo.text = $"{LocalizationManager.Get("UI_STONES_HELD")} " +
                              $"<b>{crafting.AscensionStones}</b>\n" +
                              $"{LocalizationManager.Get("UI_SUCCESS_CHANCE")} " +
                              $"<b>{crafting.EffectiveSuccessChance * 100f:0}%</b>";
        piedras.coste.text = Cost(crafting.WoodCost, crafting.IronCost, 0);
        SetButton(piedras, LocalizationManager.Get("UI_FORGE"), crafting.CanAttemptCraft, UITheme.Amber);

        // Fabricación de Armas
        armas.titulo.text = LocalizationManager.Get("UI_FORGE_WEAPONS");
        armas.cuerpo.text = LocalizationManager.Get("UI_FORGE_WEAPONS_HELP");
        armas.coste.text = Cost(crafting.WeaponWoodCost, crafting.WeaponIronCost, crafting.WeaponFoodCost);
        SetButton(armas, LocalizationManager.Get("UI_CRAFT_WEAPON"), crafting.CanCraftWeapon, UITheme.Teal);

        // Reparación de Equipo
        int desgaste = crafting.TotalWear();
        reparar.titulo.text = LocalizationManager.Get("UI_REPAIR_GEAR");
        reparar.cuerpo.text = $"{LocalizationManager.Get("UI_TOTAL_WEAR")} <b>{desgaste}</b>";
        reparar.coste.text = desgaste > 0
            ? Cost(crafting.RepairAllWoodCost(), crafting.RepairAllIronCost(), 0)
            : $"<color={UITheme.Tag(UITheme.TextFaint)}>" +
              $"{LocalizationManager.Get("UI_NOTHING_BROKEN")}</color>";

        bool puedeReparar = desgaste > 0 && economy != null
            && economy.CanAffordMaterials(crafting.RepairAllWoodCost(), crafting.RepairAllIronCost());
        SetButton(reparar, LocalizationManager.Get("UI_REPAIR_ALL"), puedeReparar, UITheme.DangerSoft);

        etiquetaCerrar.text = LocalizationManager.Get("UI_CLOSE");
    }

    // Coste en columnas: el recurso a la izquierda y la cifra siempre en la misma tabulación.
    private static string Cost(int wood, int iron, int food)
    {
        string texto = Row(LocalizationManager.Get("UI_WOOD"), wood)
                     + "\n" + Row(LocalizationManager.Get("UI_IRON"), iron);

        if (food > 0) texto += "\n" + Row(LocalizationManager.Get("UI_FOOD"), food);
        return texto;
    }

    private static string Row(string nombre, int cantidad)
        => $"<color={UITheme.Tag(UITheme.TextFaint)}>{nombre}</color><pos=62%><b>{cantidad}</b>";

    private static void SetButton(Ficha ficha, string texto, bool activo, Color color)
    {
        ficha.etiqueta.text = texto;
        ficha.boton.interactable = activo;
        ficha.boton.targetGraphic.color = activo ? color : UITheme.Neutral;
        ficha.etiqueta.color = activo ? UITheme.Text : UITheme.TextFaint;
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "AlchemyWorkshopPanel", size, UITheme.Bg);

        titulo = UIBuild.TopLabel(panel.transform, "Title", UITheme.SizeTitle, 34f, -20f,
            TextAlignmentOptions.Left);

        recursos = UIBuild.TopLabel(panel.transform, "Resources", UITheme.SizeBody, 30f, -22f,
            TextAlignmentOptions.Right);
        recursos.color = UITheme.TextSoft;

        float x = -(CardWidth + CardGap);
        piedras = CreateCard("Card_Stones", x, OnCraftStonePressed);
        armas = CreateCard("Card_Weapons", 0f, OnCraftWeaponPressed);
        reparar = CreateCard("Card_Repair", -x, OnRepairAllPressed);

        feedback = UIBuild.TopLabel(panel.transform, "Feedback", UITheme.SizeBody, 26f,
            -(CardTop + CardHeight + 8f), TextAlignmentOptions.Center);

        var cerrar = UIBuild.Button(panel.transform, "Btn_CloseWorkshop", string.Empty,
            Color.clear, new Vector2(240f, 46f), new Vector2(0f, -(size.y - 62f)), Close);
        etiquetaCerrar = cerrar.GetComponentInChildren<TMP_Text>();
    }

    // Tarjeta compacta: título arriba, cuerpo, coste pegado al botón y botón al pie.
    private Ficha CreateCard(string name, float x, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(panel.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(CardWidth, CardHeight);
        rt.anchoredPosition = new Vector2(x, -CardTop);

        UITheme.Surface(go, UITheme.Card, UITheme.BorderCard, UITheme.RadiusCard);

        var ficha = new Ficha
        {
            titulo = CardLabel(go.transform, "Title", UITheme.SizeValue, -14f, 26f,
                TextAlignmentOptions.Left),
            cuerpo = CardLabel(go.transform, "Body", UITheme.SizeBody, -46f, 62f,
                TextAlignmentOptions.TopLeft),
            coste = CardLabel(go.transform, "Cost", UITheme.SizeBody, -114f, 78f,
                TextAlignmentOptions.TopLeft)
        };

        ficha.titulo.fontStyle = FontStyles.Bold;
        ficha.cuerpo.color = UITheme.TextSoft;
        ficha.cuerpo.lineSpacing = 10f;
        ficha.coste.lineSpacing = 10f;

        // El botón se ancla al pie de la tarjeta, dentro de su borde.
        ficha.boton = UIBuild.Button(go.transform, "Btn_Action", string.Empty, UITheme.Amber,
            new Vector2(CardWidth - 32f, ButtonHeight),
            new Vector2(0f, -(CardHeight - 14f - ButtonHeight)), onClick);
        ficha.etiqueta = ficha.boton.GetComponentInChildren<TMP_Text>();
        ficha.etiqueta.fontSize = UITheme.SizeBody;
        ficha.etiqueta.fontStyle = FontStyles.Bold;

        return ficha;
    }

    private static TMP_Text CardLabel(Transform parent, string name, float size, float y,
                                      float height, TextAlignmentOptions align)
    {
        var tmp = UIBuild.Label(parent, name, size, align);

        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(16f, 0f);
        rt.offsetMax = new Vector2(-16f, 0f);
        rt.sizeDelta = new Vector2(-32f, height);
        rt.anchoredPosition = new Vector2(0f, y);
        return tmp;
    }
}
