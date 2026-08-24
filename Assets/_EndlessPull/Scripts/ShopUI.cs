using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panel modal de la tienda de equipo: comprar piezas y ver el almacén.
public class ShopUI : MonoBehaviour
{
    [Tooltip("Raíz del panel; se activa y desactiva al abrir y cerrar.")]
    [SerializeField] private GameObject panel;

    [Tooltip("Tienda que cobra y suelta las piezas.")]
    [SerializeField] private ShopManager shop;

    [Tooltip("Economía de la que salen las gemas.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Texto con el precio de la tirada de equipo.")]
    [SerializeField] private TMP_Text priceLabel;

    [Tooltip("Texto con lo que hay en el almacén.")]
    [SerializeField] private TMP_Text inventoryLabel;

    [Tooltip("Botón que compra una pieza al azar.")]
    [SerializeField] private Button buyButton;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (shop == null) shop = UnityEngine.Object.FindFirstObjectByType<ShopManager>();
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
    }

    void OnEnable()
    {
        if (shop != null) shop.InventoryChanged += Refresh;
    }

    void OnDisable()
    {
        if (shop != null) shop.InventoryChanged -= Refresh;
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
        Refresh();
    }

    // Las gemas cambian solas, así que el botón se repasa mientras esté abierto.
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

        UIManager.OpenExclusive(panel);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    // Enganchado al onClick del botón de compra.
    public void OnBuyPressed()
    {
        if (shop != null) shop.PullEquipment();
        Refresh();
    }

    private void Refresh()
    {
        if (shop == null) return;

        if (priceLabel != null)
            priceLabel.text = $"Pieza al azar: {shop.EquipmentPullCost} gemas" +
                              (economy != null ? $"     Tienes {economy.Gems}" : string.Empty);

        if (inventoryLabel != null) inventoryLabel.text = DescribeInventory();

        if (buyButton != null)
            buyButton.interactable = economy != null && economy.CanAfford(shop.EquipmentPullCost);
    }

    // Agrupa las piezas repetidas para no listar veinte líneas iguales.
    private string DescribeInventory()
    {
        if (shop.Inventory.Count == 0) return "Almacén vacío";

        var conteo = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var item in shop.Inventory)
        {
            string clave = item.ShortLabel();
            conteo[clave] = conteo.TryGetValue(clave, out int n) ? n + 1 : 1;
        }

        var lineas = new System.Collections.Generic.List<string>();
        foreach (var par in conteo) lineas.Add($"{par.Key}  x{par.Value}");

        return $"Almacén ({shop.Inventory.Count}):\n" + string.Join("\n", lineas);
    }
}
