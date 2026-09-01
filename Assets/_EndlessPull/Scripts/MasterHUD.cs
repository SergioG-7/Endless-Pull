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
        }
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
        }
    }

    void Start()
    {
        if (statusLabel != null) statusLabel.text = string.Empty;

        // Que ninguno de los tres textos de la TopBar se corte en 16:9 estrecho o en móvil.
        ConfigureNoClip(resourcesLabel);
        ConfigureNoClip(floorLabel);
        ConfigureNoClip(statusLabel);

        if (economy != null) RefreshResources();

        RefreshFloor();
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

        // El botón de tirada se apaga solo cuando no llega el saldo.
        if (pullButton != null && gacha != null)
            pullButton.interactable = gems >= gacha.PullCost;
    }

    private void OnFloorChanged(int floor) => RefreshFloor();

    private void OnExpeditionChanged(ExpeditionState state, string message)
    {
        if (statusLabel != null) statusLabel.text = message;
        RefreshFloor();
    }

    private void RefreshFloor()
    {
        if (floorLabel == null || waves == null) return;

        // Repetir un piso inferior no debe hacer bajar el HUD: siempre el mayor entre el piso
        // actual y el techo ya superado (highestClearedFloor es inmutable a la baja).
        int pisoMostrado = Mathf.Max(waves.CurrentFloor, waves.HighestClearedFloor);
        string piso = string.Format(LocalizationManager.Get("UI_QUADRANT_FLOOR_LABEL"), pisoMostrado);
        floorLabel.text = Chip(LocalizationManager.Get("UI_TOWER").ToUpperInvariant(), piso);
    }
}
