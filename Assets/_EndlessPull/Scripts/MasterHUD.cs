using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ata la UI del Maestro a los eventos de economía y expedición; no hace polling.
public class MasterHUD : MonoBehaviour
{
    [Tooltip("Economía de la que se lee el saldo de gemas.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Gacha, para conocer el coste de la tirada.")]
    [SerializeField] private GachaManager gacha;

    [Tooltip("Gestor de oleadas, para el piso y el feedback.")]
    [SerializeField] private WaveManager waves;

    [Tooltip("Chip compacto con Gemas, Madera, Hierro y Comida agrupados en la esquina.")]
    [SerializeField] private TextMeshProUGUI resourcesLabel;

    [Tooltip("Texto del piso actual.")]
    [SerializeField] private TextMeshProUGUI floorLabel;

    [Tooltip("Texto de feedback de la expedición.")]
    [SerializeField] private TextMeshProUGUI statusLabel;

    [Tooltip("Botón de invocación, se deshabilita si no hay gemas.")]
    [SerializeField] private Button pullButton;

    // Contador de héroes por estrellas; hueco vacío a la izquierda de la TopBar (construido por
    // código, no viene de la escena como el resto de la barra).
    private TMP_Text starCountLabel;


    void OnEnable()
    {
        if (economy != null)
        {
            economy.GemsChanged += OnGemsChanged;
            economy.MaterialsChanged += OnMaterialsChanged;
            economy.FoodChanged += OnFoodChanged;
        }
        if (waves != null)
        {
            waves.ExpeditionChanged += OnExpeditionChanged;
            waves.FloorChanged += OnFloorChanged;
            waves.FloorCleared += OnFloorCleared;
        }
        HeroProgress.HeroAscended += OnHeroAscended;
        SaveManager.RosterLoaded += RefreshStarCounts;
        LocalizationManager.LanguageChanged += OnLanguageChanged;
    }

    void OnDisable()
    {
        if (economy != null)
        {
            economy.GemsChanged -= OnGemsChanged;
            economy.MaterialsChanged -= OnMaterialsChanged;
            economy.FoodChanged -= OnFoodChanged;
        }
        if (waves != null)
        {
            waves.ExpeditionChanged -= OnExpeditionChanged;
            waves.FloorChanged -= OnFloorChanged;
            waves.FloorCleared -= OnFloorCleared;
        }
        HeroProgress.HeroAscended -= OnHeroAscended;
        SaveManager.RosterLoaded -= RefreshStarCounts;
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
    }

    // Los rótulos de la TopBar solo se recalculaban con eventos de economía/piso: "TORRE Piso N"
    // y "COMIDA" se quedaban en el idioma anterior si el jugador cambiaba de idioma sin gastar
    // recursos ni cambiar de piso mientras tanto.
    private void OnLanguageChanged()
    {
        if (economy != null) RefreshResources();
        RefreshFloor();
        if (showingWonStatus) RefreshWonStatus();
    }

    private void OnHeroAscended(HeroController hero, int newStarRank) => RefreshStarCounts();

void Start()
    {
        if (statusLabel != null) statusLabel.text = string.Empty;

        // Que ninguno de los tres textos de la TopBar se corte en 16:9 estrecho o en móvil.
        ConfigureNoClip(resourcesLabel);
        ConfigureNoClip(floorLabel);
        ConfigureNoClip(statusLabel);

        BuildStarCountLabel();

        if (economy != null) RefreshResources();

        RefreshFloor();
        RefreshStarCounts();
    }


    // El hueco vacío a la izquierda de la TopBar (todo lo demás cuelga anclado a la derecha).
    private void BuildStarCountLabel()
    {
        if (floorLabel == null) return;

        var topBar = floorLabel.transform.parent;
        if (topBar == null) return;

        starCountLabel = UIBuild.Label(topBar, "Txt_StarCounts", UITheme.SizeCaption, TextAlignmentOptions.Left);
        var rt = starCountLabel.rectTransform;
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(320f, 34f);
        rt.anchoredPosition = new Vector2(20f, 0f);

        ConfigureNoClip((TextMeshProUGUI)starCountLabel);
    }

    // Total de héroes por rareza; se refresca con las gemas (toda tirada/ascenso gasta gemas)
    // y con SaveManager.RosterLoaded/HeroProgress.HeroAscended para los casos sin gasto de gemas.
    private void RefreshStarCounts()
    {
        if (starCountLabel == null) return;

        var counts = new int[6];
        foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
            if (hero != null && hero.StarRank >= 1 && hero.StarRank <= 5) counts[hero.StarRank]++;

        var sb = new System.Text.StringBuilder();
        for (int estrellas = 1; estrellas <= 5; estrellas++)
        {
            if (counts[estrellas] == 0) continue;
            if (sb.Length > 0) sb.Append("  ");
            sb.Append($"<color={UITheme.Tag(HeroProgress.RarityColor(estrellas))}>{estrellas}★</color> <b>{counts[estrellas]}</b>");
        }

        starCountLabel.text = sb.ToString();
    }


    // Encoge el texto en vez de cortarlo cuando la TopBar no tiene ancho suficiente.
    private static void ConfigureNoClip(TextMeshProUGUI label)
    {
        if (label == null) return;

        label.enableAutoSizing = true;
        label.fontSizeMin = label.fontSize * 0.6f;
        label.fontSizeMax = label.fontSize;
        label.overflowMode = TextOverflowModes.Ellipsis;
    }

    // Gemas, madera, hierro y comida comparten un único chip compacto en la esquina.
    private void OnMaterialsChanged(int wood, int iron) => RefreshResources();
    private void OnFoodChanged(int food) => RefreshResources();

    private void RefreshResources()
    {
        if (resourcesLabel == null || economy == null) return;

        resourcesLabel.text =
            Inline("◆", LocalizationManager.Get("UI_GEMS").ToUpperInvariant(), economy.Gems.ToString(), UITheme.Cyan) + "  " +
            Inline("■", LocalizationManager.Get("UI_WOOD").ToUpperInvariant(), economy.Wood.ToString(), UITheme.Hex("A8895C")) + "  " +
            Inline("■", LocalizationManager.Get("UI_IRON").ToUpperInvariant(), economy.Iron.ToString(), UITheme.Hex("9397AB")) + "  " +
            Inline("■", LocalizationManager.Get("UI_FOOD").ToUpperInvariant(), economy.Food.ToString(), UITheme.Hex("8FBF5A"));
    }

    // Rótulo pequeño en mayúsculas sobre la cifra, como los bloques del mockup.
    private static string Chip(string caption, string value)
        => $"<size={UITheme.SizeCaption}><color={UITheme.Tag(UITheme.TextMuted)}>{caption}</color></size>\n<b>{value}</b>";

    // Chip de recurso en una línea: icono de color, rótulo apagado y cifra grande.
    private static string Inline(string icon, string caption, string value, Color dot)
        => $"<size={UITheme.SizeCaption}><color={UITheme.Tag(dot)}>{icon}</color> " +
           $"<color={UITheme.Tag(UITheme.TextFaint)}>{caption}</color></size> <b>{value}</b>";

private void OnGemsChanged(int gems)
    {
        RefreshResources();
        RefreshStarCounts();

        // El botón de tirada se apaga solo cuando no llega el saldo.
        if (pullButton != null && gacha != null)
            pullButton.interactable = gems >= gacha.PullCost;
    }

    private void OnFloorChanged(int floor) => RefreshFloor();

    // Datos puros del último piso ganado; el mensaje se regenera con la plantilla del idioma
    // activo en vez de guardar el string ya formateado, que se quedaba congelado en el idioma
    // de cuando se ganó si el jugador cambiaba de idioma después (banner bajo la TopBar).
    private void OnFloorCleared(FloorRewardInfo info) => lastWonReward = info;

    private FloorRewardInfo lastWonReward;
    private bool showingWonStatus;

    private void OnExpeditionChanged(ExpeditionState state, string message)
    {
        showingWonStatus = state == ExpeditionState.Won;
        if (statusLabel != null) statusLabel.text = showingWonStatus ? BuildWonMessage() : message;
        RefreshFloor();
    }

    private void RefreshWonStatus()
    {
        if (statusLabel != null) statusLabel.text = BuildWonMessage();
    }

    private string BuildWonMessage()
    {
        string modo = lastWonReward.firstClear
            ? LocalizationManager.Get("UI_FIRST_CLEAR")
            : LocalizationManager.Get("UI_REPEAT");

        return string.Format(LocalizationManager.Get("UI_STATUS_WON"),
            lastWonReward.floor, modo, lastWonReward.gems, lastWonReward.wood, lastWonReward.iron);
    }

private void RefreshFloor()
    {
        if (floorLabel == null || waves == null) return;

        // Piso mostrado: el que se está peleando ahora mismo, o si no hay combate en curso
        // (recién ganado, aún sin empezar el siguiente, o piso inferior repetido) el techo ya
        // superado — nunca el `currentFloor` que ya avanzó de fondo al ganar (Piso 7 tras ganar
        // el 6, antes de siquiera pisarlo).
        int pisoMostrado = waves.State == ExpeditionState.InProgress
            ? waves.CurrentFloor
            : Mathf.Max(1, waves.HighestClearedFloor);
        string piso = string.Format(LocalizationManager.Get("UI_QUADRANT_FLOOR_LABEL"), pisoMostrado);
        floorLabel.text = Chip(LocalizationManager.Get("UI_TOWER").ToUpperInvariant(), piso);
    }
}
