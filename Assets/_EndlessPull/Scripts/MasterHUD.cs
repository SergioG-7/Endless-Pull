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

    void OnEnable()
    {
        if (economy != null)
        {
            economy.GemsChanged += OnGemsChanged;
            economy.MaterialsChanged += OnMaterialsChanged;
        }
        if (waves != null) waves.ExpeditionChanged += OnExpeditionChanged;
    }

    void OnDisable()
    {
        if (economy != null)
        {
            economy.GemsChanged -= OnGemsChanged;
            economy.MaterialsChanged -= OnMaterialsChanged;
        }
        if (waves != null) waves.ExpeditionChanged -= OnExpeditionChanged;
    }

    void Start()
    {
        if (statusLabel != null) statusLabel.text = string.Empty;

        if (economy != null)
        {
            OnGemsChanged(economy.Gems);
            OnMaterialsChanged(economy.Wood, economy.Iron);
        }

        RefreshFloor();
    }

    private void OnMaterialsChanged(int wood, int iron)
    {
        if (materialsLabel != null) materialsLabel.text = $"Madera: {wood} | Hierro: {iron}";
    }

    private void OnGemsChanged(int gems)
    {
        if (gemsLabel != null) gemsLabel.text = $"Gemas: {gems}";

        // El botón de tirada se apaga solo cuando no llega el saldo.
        if (pullButton != null && gacha != null)
            pullButton.interactable = gems >= gacha.PullCost;
    }

    private void OnExpeditionChanged(ExpeditionState state, string message)
    {
        if (statusLabel != null) statusLabel.text = message;
        RefreshFloor();
    }

    private void RefreshFloor()
    {
        if (floorLabel != null && waves != null)
            floorLabel.text = $"Piso: {waves.CurrentFloor}";
    }
}
