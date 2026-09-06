using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Forja: se elige hueco y gama, y sale una pieza al azar de ese grupo. La gama se desbloquea
// subiendo la Torre, así que las de más arriba salen bloqueadas con su piso.
public class ForgeUI : MonoBehaviour
{
    [Tooltip("Canvas donde se monta el panel; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Taller que cobra los materiales y entrega la pieza.")]
    [SerializeField] private CraftingManager crafting;

    [Tooltip("Tamaño del modal; el mismo rango que el resto de paneles del juego.")]
    [SerializeField] private Vector2 size = new Vector2(1100f, 620f);

    [Tooltip("Gamas que se enseñan en la rejilla, incluidas las aún bloqueadas.")]
    [Min(1)]
    [SerializeField] private int tiersShown = 8;

    // Gamas por fila: la segunda fila (5-8) cae justo debajo, así el resto del panel no se
    // recoloca según cuántas gamas haya abiertas.
    private const int TiersPerRow = 4;
    private const float TierButtonHeight = 56f;
    private const float TierRowGap = 12f;

    private static readonly EquipmentSlot[] Slots =
        { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Shield, EquipmentSlot.Accessory };

    private GameObject panel;
    private TMP_Text titulo;
    private TMP_Text rotuloHueco;
    private TMP_Text rotuloGama;
    private TMP_Text coste;
    private TMP_Text resultado;
    private TMP_Text aviso;

    private readonly List<Button> botonesHueco = new List<Button>();
    private readonly List<TMP_Text> etiquetasHueco = new List<TMP_Text>();
    private readonly List<Button> botonesGama = new List<Button>();
    private readonly List<TMP_Text> etiquetasGama = new List<TMP_Text>();

    private Button botonForjar;
    private TMP_Text etiquetaForjar;

    private EquipmentSlot huecoElegido = EquipmentSlot.Weapon;
    private int gamaElegida = 1;

    // El minijuego es opcional: sin marcar, la pieza sale tal cual y no se pierde nada.
    private bool conMinijuego = true;
    private Button botonMinijuego;
    private TMP_Text etiquetaMinijuego;
    private HammerMinigameUI minijuego;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();

        Build();
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    void OnEnable()
    {
        LocalizationManager.LanguageChanged += OnLanguageChanged;
        if (crafting != null) crafting.CraftResolved += OnCraftResolved;
    }

    void OnDisable()
    {
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
        if (crafting != null) crafting.CraftResolved -= OnCraftResolved;
    }

    private void OnLanguageChanged()
    {
        if (IsOpen) Refresh();
    }

    private void OnCraftResolved(bool success, string message)
    {
        if (!IsOpen) return;

        aviso.text = message;
        aviso.color = success ? UITheme.Text : UITheme.DangerLight;
        Refresh();
    }

    public void Open()
    {
        if (panel == null) return;

        aviso.text = string.Empty;
        Refresh();
        UIManager.OpenExclusive(panel);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void OnSlotPressed(EquipmentSlot slot)
    {
        huecoElegido = slot;
        Refresh();
    }

    private void OnTierPressed(int tier)
    {
        gamaElegida = tier;
        Refresh();
    }

    private void OnMinigameToggled()
    {
        conMinijuego = !conMinijuego;
        Refresh();
    }

    private void OnForgePressed()
    {
        if (crafting == null) return;

        var pieza = crafting.TryCraftEquipment(huecoElegido, gamaElegida);
        if (pieza == null || !conMinijuego) return;

        // El minijuego es exclusivo y cierra esta ficha; al salir la vuelve a abrir.
        if (minijuego == null) minijuego = UnityEngine.Object.FindFirstObjectByType<HammerMinigameUI>();
        if (minijuego == null) return;

        minijuego.Show(pieza, Open);
    }

    private void Refresh()
    {
        if (crafting == null) return;

        titulo.text = LocalizationManager.Get("UI_FORGE_TITLE");
        rotuloHueco.text = LocalizationManager.Get("UI_FORGE_WHAT");
        rotuloGama.text = LocalizationManager.Get("UI_FORGE_TIER");

        for (int i = 0; i < Slots.Length; i++)
        {
            bool activo = Slots[i] == huecoElegido;
            etiquetasHueco[i].text = SlotName(Slots[i]);
            etiquetasHueco[i].fontStyle = activo ? FontStyles.Bold : FontStyles.Normal;
            botonesHueco[i].targetGraphic.color = activo ? UITheme.AccentPick : UITheme.Neutral;
        }

        // Solo se enseñan las gamas que existen de verdad más la siguiente, para que se vea a
        // qué piso se abre la próxima sin llenar la fila de gamas vacías.
        int ultimaVisible = Mathf.Min(botonesGama.Count, crafting.HighestTierInCatalog + 1);
        gamaElegida = Mathf.Clamp(gamaElegida, 1, Mathf.Max(1, ultimaVisible));

        for (int i = 0; i < botonesGama.Count; i++)
        {
            int gama = i + 1;

            bool visible = gama <= ultimaVisible;
            if (botonesGama[i].gameObject.activeSelf != visible)
                botonesGama[i].gameObject.SetActive(visible);
            if (!visible) continue;

            bool abierta = crafting.IsTierUnlocked(gama);
            bool activa = gama == gamaElegida;

            etiquetasGama[i].text = abierta
                ? string.Format(LocalizationManager.Get("UI_FORGE_TIER_N"), gama)
                : string.Format(LocalizationManager.Get("UI_TIER_LOCKED"), crafting.FloorForTier(gama));

            etiquetasGama[i].fontStyle = activa ? FontStyles.Bold : FontStyles.Normal;
            etiquetasGama[i].color = abierta ? UITheme.Text : UITheme.TextFaint;
            botonesGama[i].interactable = abierta;
            botonesGama[i].targetGraphic.color = activa && abierta ? UITheme.AccentPick : UITheme.Neutral;
        }

        var pool = crafting.EquipmentPool(huecoElegido, gamaElegida);

        coste.text = string.Format(LocalizationManager.Get("UI_FORGE_COST"),
            crafting.EquipmentWoodCost(gamaElegida),
            crafting.EquipmentIronCost(gamaElegida),
            crafting.EquipmentFoodCost(gamaElegida));

        // Se enseña de qué piezas puede salir: elegir a ciegas no da información ninguna.
        if (pool.Count == 0)
        {
            resultado.text = LocalizationManager.Get("UI_FORGE_EMPTY");
            resultado.color = UITheme.TextFaint;
        }
        else
        {
            var nombres = new List<string>();
            foreach (var pieza in pool) nombres.Add(pieza.LocalizedName());

            resultado.text = string.Format(LocalizationManager.Get("UI_FORGE_OUTCOME"),
                string.Join(", ", nombres));
            resultado.color = UITheme.TextMuted;
        }

        etiquetaMinijuego.text = (conMinijuego ? "[X]  " : "[  ]  ")
            + LocalizationManager.Get("UI_FORGE_MINIGAME");
        etiquetaMinijuego.color = conMinijuego ? UITheme.Text : UITheme.TextMuted;
        botonMinijuego.targetGraphic.color = conMinijuego ? UITheme.AccentPick : UITheme.Neutral;

        bool sePuede = crafting.CanCraftEquipment(huecoElegido, gamaElegida);
        botonForjar.interactable = sePuede;
        botonForjar.targetGraphic.color = sePuede ? UITheme.AccentPick : UITheme.Neutral;
        etiquetaForjar.text = LocalizationManager.Get("UI_FORGE_DO");
        etiquetaForjar.color = sePuede ? UITheme.Text : UITheme.TextFaint;
    }

    private static string SlotName(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: return LocalizationManager.Get("UI_SLOT_WEAPON");
            case EquipmentSlot.Armor: return LocalizationManager.Get("UI_SLOT_ARMOR");
            case EquipmentSlot.Shield: return LocalizationManager.Get("UI_SLOT_SHIELD");
        }
        return LocalizationManager.Get("UI_SLOT_ACCESSORY");
    }

    private void Build()
    {
        if (canvas == null) return;

        // Alto extra por cada fila de gamas más allá de la primera; lo que va debajo baja igual.
        int filasGama = Mathf.CeilToInt(Mathf.Max(1, tiersShown) / (float)TiersPerRow);
        float extra = (filasGama - 1) * (TierButtonHeight + TierRowGap);

        panel = UIBuild.Panel(canvas.transform, "ForgePanel", new Vector2(size.x, size.y + extra),
            UITheme.Bg);

        titulo = UIBuild.TopLabel(panel.transform, "Title", UITheme.SizeTitle, 34f, -20f,
            TextAlignmentOptions.Left);

        rotuloHueco = UIBuild.TopLabel(panel.transform, "SlotLabel", UITheme.SizeBody, 26f, -76f,
            TextAlignmentOptions.Left);
        rotuloHueco.color = UITheme.TextMuted;

        float ancho = (size.x - 40f - 3f * 12f) / 4f;
        float inicio = -(size.x - 40f) * 0.5f + ancho * 0.5f;

        for (int i = 0; i < Slots.Length; i++)
        {
            var slot = Slots[i];
            var boton = UIBuild.Button(panel.transform, "Btn_Slot_" + slot, string.Empty,
                UITheme.Neutral, new Vector2(ancho, 56f),
                new Vector2(inicio + i * (ancho + 12f), -108f), () => OnSlotPressed(slot));

            botonesHueco.Add(boton);
            etiquetasHueco.Add(boton.GetComponentInChildren<TMP_Text>());
        }

        rotuloGama = UIBuild.TopLabel(panel.transform, "TierLabel", UITheme.SizeBody, 26f, -180f,
            TextAlignmentOptions.Left);
        rotuloGama.color = UITheme.TextMuted;

        int gamas = Mathf.Max(1, tiersShown);
        float anchoGama = (size.x - 40f - (TiersPerRow - 1) * 12f) / TiersPerRow;
        float inicioGama = -(size.x - 40f) * 0.5f + anchoGama * 0.5f;

        for (int i = 0; i < gamas; i++)
        {
            int gama = i + 1;
            int fila = i / TiersPerRow;
            int columna = i % TiersPerRow;

            var boton = UIBuild.Button(panel.transform, "Btn_Tier_" + gama, string.Empty,
                UITheme.Neutral, new Vector2(anchoGama, TierButtonHeight),
                new Vector2(inicioGama + columna * (anchoGama + 12f),
                            -212f - fila * (TierButtonHeight + TierRowGap)),
                () => OnTierPressed(gama));

            botonesGama.Add(boton);
            etiquetasGama.Add(boton.GetComponentInChildren<TMP_Text>());
        }

        coste = UIBuild.TopLabel(panel.transform, "Cost", UITheme.SizeName, 30f, -292f - extra,
            TextAlignmentOptions.Center);

        resultado = UIBuild.TopLabel(panel.transform, "Outcome", UITheme.SizeBody, 60f, -328f - extra,
            TextAlignmentOptions.Center);
        resultado.enableWordWrapping = true;

        aviso = UIBuild.TopLabel(panel.transform, "Notice", UITheme.SizeBody, 28f, -396f - extra,
            TextAlignmentOptions.Center);

        botonMinijuego = UIBuild.Button(panel.transform, "Btn_Minigame", string.Empty,
            UITheme.AccentPick, new Vector2(460f, 48f), new Vector2(0f, -430f - extra), OnMinigameToggled);
        etiquetaMinijuego = botonMinijuego.GetComponentInChildren<TMP_Text>();

        botonForjar = UIBuild.Button(panel.transform, "Btn_Forge", string.Empty,
            UITheme.AccentPick, new Vector2(360f, 64f), new Vector2(0f, -490f - extra), OnForgePressed);
        etiquetaForjar = botonForjar.GetComponentInChildren<TMP_Text>();

        UIBuild.CloseButtonTopRight(panel.transform, Close);
    }
}
