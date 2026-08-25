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

    [Tooltip("Texto del contador de gemas.")]
    [SerializeField] private TextMeshProUGUI gemsLabel;

    [Tooltip("Texto de madera y hierro.")]
    [SerializeField] private TextMeshProUGUI materialsLabel;

    [Tooltip("Texto del piso actual.")]
    [SerializeField] private TextMeshProUGUI floorLabel;

    [Tooltip("Texto de feedback de la expedición.")]
    [SerializeField] private TextMeshProUGUI statusLabel;

    [Tooltip("Botón de invocación, se deshabilita si no hay gemas.")]
    [SerializeField] private Button pullButton;

    [Tooltip("Escuadra de la que se leen los intentos de torre.")]
    [SerializeField] private PartyManager party;

    [Tooltip("Texto con los intentos de torre restantes.")]
    [SerializeField] private TextMeshProUGUI energyLabel;

    [Tooltip("Botón táctil que recarga los intentos pagando gemas.")]
    [SerializeField] private Button energyRefillButton;

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
        if (party != null) party.PartyChanged += RefreshEnergy;
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
        if (party != null) party.PartyChanged -= RefreshEnergy;
    }

    void Start()
    {
        if (statusLabel != null) statusLabel.text = string.Empty;

        if (economy != null)
        {
            OnGemsChanged(economy.Gems);
            RefreshMaterials();
        }

        RefreshFloor();
        RefreshEnergy();
    }

    // Enganchado al botón "+" de la barra de intentos.
    public void OnRefillEnergyPressed()
    {
        if (party != null) party.TryRefillEnergyWithGems();
        RefreshEnergy();
    }

    private void RefreshEnergy()
    {
        if (party == null) return;

        if (energyLabel != null) energyLabel.text = Chip("INTENTOS", $"{party.Energy}/{party.MaxEnergy}");

        // El "+" solo se enciende si falta algún intento y hay gemas para pagarlo.
        if (energyRefillButton != null)
            energyRefillButton.interactable = party.Energy < party.MaxEnergy
                && economy != null && economy.CanAfford(party.EnergyRefillCost);
    }

    // Madera, hierro y comida comparten etiqueta; se repinta con lo que haya en economía.
    private void OnMaterialsChanged(int wood, int iron) => RefreshMaterials();
    private void OnFoodChanged(int food) => RefreshMaterials();

    private void RefreshMaterials()
    {
        if (materialsLabel == null || economy == null) return;

        materialsLabel.text = Inline("MADERA", economy.Wood.ToString(), UITheme.Hex("A8895C")) + "   " +
                              Inline("HIERRO", economy.Iron.ToString(), UITheme.Hex("9397AB")) + "   " +
                              Inline("COMIDA", economy.Food.ToString(), UITheme.Hex("8FBF5A"));
    }

    // Rótulo pequeño en mayúsculas sobre la cifra, como los bloques del mockup.
    private static string Chip(string caption, string value)
        => $"<size={UITheme.SizeCaption}><color={UITheme.Tag(UITheme.TextMuted)}>{caption}</color></size>\n<b>{value}</b>";

    // Chip de recurso en una línea: punto de color, rótulo apagado y cifra grande.
    private static string Inline(string caption, string value, Color dot)
        => $"<size={UITheme.SizeCaption}><color={UITheme.Tag(dot)}>■</color> " +
           $"<color={UITheme.Tag(UITheme.TextFaint)}>{caption}</color></size> <b>{value}</b>";

    private void OnGemsChanged(int gems)
    {
        if (gemsLabel != null)
            gemsLabel.text = $"<color={UITheme.Tag(UITheme.Cyan)}>◆</color> <b>{gems}</b>";

        // Con las gemas cambia si se puede pagar la recarga.
        RefreshEnergy();

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
        if (floorLabel != null && waves != null)
            floorLabel.text = Chip("TORRE", $"Piso {waves.CurrentFloor}");
    }
}
