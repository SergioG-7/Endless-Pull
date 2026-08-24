using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panel modal del Taller de Alquimia: coste, probabilidad, botón y resultado del último intento.
public class CraftingUI : MonoBehaviour
{
    [Tooltip("Raíz del panel; se activa y desactiva al abrir y cerrar.")]
    [SerializeField] private GameObject panel;

    [Tooltip("Taller que forja las piedras.")]
    [SerializeField] private CraftingManager crafting;

    [Tooltip("Economía de la que salen los materiales.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Texto con el coste y la probabilidad.")]
    [SerializeField] private TMP_Text recipeLabel;

    [Tooltip("Texto con las piedras que hay en el zurrón.")]
    [SerializeField] private TMP_Text stonesLabel;

    [Tooltip("Texto con el resultado del último intento.")]
    [SerializeField] private TMP_Text feedbackLabel;

    [Tooltip("Botón que lanza el intento de crafteo.")]
    [SerializeField] private Button craftButton;

    [Tooltip("Color del texto cuando el intento sale bien.")]
    [SerializeField] private Color successColor = new Color(0.45f, 0.95f, 0.55f);

    [Tooltip("Color del texto cuando el intento sale mal.")]
    [SerializeField] private Color failureColor = new Color(1f, 0.45f, 0.40f);

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
    }

    void OnEnable()
    {
        if (crafting == null) return;

        crafting.CraftResolved += OnCraftResolved;
        crafting.StonesChanged += OnStonesChanged;
    }

    void OnDisable()
    {
        if (crafting == null) return;

        crafting.CraftResolved -= OnCraftResolved;
        crafting.StonesChanged -= OnStonesChanged;
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (feedbackLabel != null) feedbackLabel.text = string.Empty;

        Refresh();
    }

    // El botón depende de los materiales, que cambian solos: se repasa cada frame abierto.
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

        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    // Enganchado al onClick del botón "Craftear Piedra".
    public void OnCraftPressed()
    {
        if (crafting != null) crafting.TryCraftAscensionStone();
        Refresh();
    }

    private void Refresh()
    {
        if (crafting == null) return;

        if (recipeLabel != null)
            recipeLabel.text = $"Coste: {crafting.WoodCost} Madera + {crafting.IronCost} Hierro\n" +
                               $"Probabilidad de éxito: {crafting.SuccessChance * 100f:0}%";

        if (stonesLabel != null)
            stonesLabel.text = $"Piedras de Ascensión: {crafting.AscensionStones}" +
                               (economy != null ? $"     Madera {economy.Wood} | Hierro {economy.Iron}" : string.Empty);

        if (craftButton != null) craftButton.interactable = crafting.CanAttemptCraft;
    }

    private void OnCraftResolved(bool success, string message)
    {
        if (feedbackLabel == null) return;

        feedbackLabel.text = message;
        feedbackLabel.color = success ? successColor : failureColor;
    }

    private void OnStonesChanged(int stones) => Refresh();
}
