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

    [Tooltip("Economía de la que salen las gemas y materiales.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Taller del que salen piedras y pociones guardadas.")]
    [SerializeField] private CraftingManager crafting;

    [Tooltip("Título del panel; antes venía fijo en la escena sin traducir.")]
    [SerializeField] private TMP_Text titleLabel;

    [Tooltip("Texto con el precio de la tirada de equipo.")]
    [SerializeField] private TMP_Text priceLabel;

    [Tooltip("Texto con lo que hay en el almacén.")]
    [SerializeField] private TMP_Text inventoryLabel;

    [Tooltip("Scroll que envuelve el texto del almacén; opcional, se resetea arriba en cada refresh.")]
    [SerializeField] private ScrollRect inventoryScroll;

    [Tooltip("Botón que compra una pieza al azar.")]
    [SerializeField] private Button buyButton;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (shop == null) shop = UnityEngine.Object.FindFirstObjectByType<ShopManager>();
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();
        if (inventoryScroll == null && inventoryLabel != null)
            inventoryScroll = inventoryLabel.GetComponentInParent<ScrollRect>();
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

        // Los rótulos vienen de la escena con el tamaño viejo: se suben al mínimo legible.
        Style(inventoryLabel, UITheme.SizeBody);

        // El título venía fijo en la escena ("TIENDA DE EQUIPO"), sin traducir ni actualizar
        // tras el cambio de rol a Almacén.
        if (titleLabel != null) titleLabel.text = LocalizationManager.Get("UI_STORAGE");

        // El botón [X] venía con "Cerrar" fijo en la escena, sin traducir (Fase 39).
        if (panel != null)
        {
            var cerrarLabel = panel.transform.Find("Btn_CloseShop")?.GetComponentInChildren<TMP_Text>();
            if (cerrarLabel != null) cerrarLabel.text = LocalizationManager.Get("UI_CLOSE");
        }

        // El equipo ya no se compra con gemas: solo se obtiene por crafteo y recompensas.
        if (priceLabel != null) priceLabel.gameObject.SetActive(false);
        if (buyButton != null) buyButton.gameObject.SetActive(false);

        Refresh();
    }

    // Tamaño fijo y alto contraste; el autoajuste de TMP encoge el texto al entrar en Play.
    private static void Style(TMP_Text label, float size)
    {
        if (label == null) return;

        label.enableAutoSizing = false;
        label.fontSize = size;
        label.color = UITheme.Text;
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

    // Enganchado al onClick del botón de compra en la escena; se deja vacío (el botón está
    // oculto) para no dejar una referencia colgante en el binding de Base.unity.
    public void OnBuyPressed()
    {
    }

    private void Refresh()
    {
        if (shop == null) return;

        if (inventoryLabel != null)
        {
            inventoryLabel.text = DescribeInventory();
            // El ContentSizeFitter del propio label ajusta la altura; el ScrollRect/RectMask2D
            // del padre recorta lo que no cabe en vez de desbordar fuera del panel.
            if (inventoryScroll != null) inventoryScroll.verticalNormalizedPosition = 1f;
        }
    }

    // Almacén completo: materiales, piedras, pociones y equipo, cada uno en su bloque.
    private string DescribeInventory()
    {
        var bloques = new System.Collections.Generic.List<string>();

        if (economy != null)
        {
            bloques.Add($"<b>{LocalizationManager.Get("UI_SECTION_MATERIALS")}</b>\n" +
                $"{LocalizationManager.Get("UI_GEMS")}  <color={UITheme.Tag(UITheme.Cyan)}>{economy.Gems}</color>\n" +
                $"{LocalizationManager.Get("UI_WOOD")}  <color={UITheme.Tag(UITheme.Cyan)}>{economy.Wood}</color>\n" +
                $"{LocalizationManager.Get("UI_IRON")}  <color={UITheme.Tag(UITheme.Cyan)}>{economy.Iron}</color>\n" +
                $"{LocalizationManager.Get("UI_FOOD")}  <color={UITheme.Tag(UITheme.Cyan)}>{economy.Food}</color>");
        }

        if (crafting != null)
        {
            var piedras = new System.Collections.Generic.List<string>();
            foreach (AscensionStoneTier tier in System.Enum.GetValues(typeof(AscensionStoneTier)))
                piedras.Add($"{AscensionStoneTiers.DisplayName(tier)}  " +
                            $"<color={UITheme.Tag(UITheme.Cyan)}>{crafting.StoneCount(tier)}</color>");

            bloques.Add($"<b>{LocalizationManager.Get("UI_SECTION_STONES")}</b>\n" + string.Join("\n", piedras));

            bloques.Add($"<b>{LocalizationManager.Get("UI_SECTION_POTIONS")}</b>\n" +
                $"{LocalizationManager.Get("UI_POTIONS_HELD")}  " +
                $"<color={UITheme.Tag(UITheme.Cyan)}>{crafting.HealingPotions}</color>");
        }

        bloques.Add(DescribeEquipment());

        return string.Join("\n\n", bloques);
    }

    // Agrupa las piezas repetidas para no listar veinte líneas iguales.
    private string DescribeEquipment()
    {
        string titulo = $"<b>{LocalizationManager.Get("UI_SECTION_EQUIPMENT")}</b>";

        if (shop == null || shop.Inventory.Count == 0)
            return $"{titulo}\n<color={UITheme.Tag(UITheme.TextFaint)}>{LocalizationManager.Get("UI_EMPTY_STORAGE")}</color>";

        var conteo = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var item in shop.Inventory)
        {
            string clave = item.ShortLabel();
            conteo[clave] = conteo.TryGetValue(clave, out int n) ? n + 1 : 1;
        }

        var lineas = new System.Collections.Generic.List<string>();
        foreach (var par in conteo)
            lineas.Add($"{par.Key}  <color={UITheme.Tag(UITheme.Cyan)}>x{par.Value}</color>");

        return $"{titulo}\n" + string.Join("\n", lineas);
    }
}
