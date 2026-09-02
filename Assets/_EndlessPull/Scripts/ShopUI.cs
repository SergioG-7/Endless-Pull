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

    // Cuadrícula de tarjetas del almacén (Fase 44): sustituye el texto plano por tarjetas
    // simétricas con icono, igual que el Taller de Alquimia, con paginación real.
    private const int GridColumns = 4;
    private const int GridRows = 3;
    private const int PageSize = GridColumns * GridRows;
    private const float CardWidth = 232f;
    private const float CardHeight = 150f;
    private const float CardGap = 20f;

    private RectTransform gridContent;
    private GameObject paginationRow;
    private TMP_Text pageIndicator;
    private Button prevPageButton;
    private Button nextPageButton;
    private int currentPage;

    // Una entrada por tipo de objeto agrupado; el icono es un color plano, ya que el proyecto
    // no tiene arte de icono real para materiales/piedras/pociones/equipo (mismo criterio que
    // el resto de la base, sin sprites propios: acento de color por categoría).
    private struct StorageEntry
    {
        public string title;
        public int count;
        public Color swatch;
    }

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
        if (shop != null) shop.InventoryChanged += MarkDirty;
    }

    void OnDisable()
    {
        if (shop != null) shop.InventoryChanged -= MarkDirty;
    }

void Start()
    {
        if (panel != null) panel.SetActive(false);

        // El texto plano de siempre se sustituye por la cuadrícula de tarjetas (Fase 44);
        // se desactiva en vez de borrarse para no tocar la escena a mano.
        if (inventoryLabel != null) inventoryLabel.gameObject.SetActive(false);
        if (inventoryScroll != null) inventoryScroll.gameObject.SetActive(false);

        // El título venía fijo en la escena ("TIENDA DE EQUIPO"), sin traducir ni actualizar
        // tras el cambio de rol a Almacén.
        if (titleLabel != null) titleLabel.text = LocalizationManager.Get("UI_STORAGE");

        // El botón [X] venía con "Cerrar" fijo en la escena, sin traducir (Fase 39).
        if (panel != null)
        {
            var cerrarLabel = panel.transform.Find("Btn_CloseShop")?.GetComponentInChildren<TMP_Text>();
            if (cerrarLabel != null) cerrarLabel.text = LocalizationManager.Get("UI_CLOSE");
        }

        BuildGrid();

        // El equipo ya no se compra con gemas: solo se obtiene por crafteo y recompensas.
        if (priceLabel != null) priceLabel.gameObject.SetActive(false);
        if (buyButton != null) buyButton.gameObject.SetActive(false);

        MarkDirty();
    }

    [Tooltip("Segundos entre refrescos de la cuadrícula mientras el panel está abierto.")]
    [SerializeField] private float refreshInterval = 0.5f;

    private float refreshTimer;
    private bool gridDirty;

    // Los materiales cambian solos (granja, expediciones), sin evento propio: se repasa por
    // temporizador. `InventoryChanged`/`Open` solo marcan sucio en vez de reconstruir al momento
    // — Destroy() difiere hasta fin de frame, así que reconstruir la cuadrícula entera más de una
    // vez en el mismo frame (p. ej. varias piezas añadidas seguidas) apilaba tarjetas viejas y
    // nuevas a la vez. Como mucho una reconstrucción real por frame, aquí.
    void Update()
    {
        if (!IsOpen) return;

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = refreshInterval;
            gridDirty = true;
        }

        if (gridDirty)
        {
            gridDirty = false;
            RebuildGrid();
        }
    }

    private void MarkDirty() => gridDirty = true;

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (panel == null) return;

        UIManager.OpenExclusive(panel);
        refreshTimer = refreshInterval;
        MarkDirty();
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

    // Almacén completo: materiales, piedras, pociones y equipo, cada uno como tarjeta propia.
    private System.Collections.Generic.List<StorageEntry> BuildEntries()
    {
        var entries = new System.Collections.Generic.List<StorageEntry>();

        if (economy != null)
        {
            entries.Add(new StorageEntry { title = LocalizationManager.Get("UI_GEMS"), count = economy.Gems, swatch = UITheme.Amber });
            entries.Add(new StorageEntry { title = LocalizationManager.Get("UI_WOOD"), count = economy.Wood, swatch = UITheme.Cyan });
            entries.Add(new StorageEntry { title = LocalizationManager.Get("UI_IRON"), count = economy.Iron, swatch = UITheme.Cyan });
            entries.Add(new StorageEntry { title = LocalizationManager.Get("UI_FOOD"), count = economy.Food, swatch = UITheme.Cyan });
        }

        if (crafting != null)
        {
            foreach (AscensionStoneTier tier in System.Enum.GetValues(typeof(AscensionStoneTier)))
                entries.Add(new StorageEntry { title = AscensionStoneTiers.DisplayName(tier),
                    count = crafting.StoneCount(tier), swatch = UITheme.Accent2 });

            entries.Add(new StorageEntry { title = LocalizationManager.Get("UI_POTIONS_HELD"),
                count = crafting.HealingPotions, swatch = UITheme.BarHP });
            entries.Add(new StorageEntry { title = LocalizationManager.Get("UI_MANA_POTIONS_HELD"),
                count = crafting.ManaPotions, swatch = UITheme.BarMP });
        }

        if (shop != null)
        {
            var conteo = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var item in shop.Inventory)
            {
                string clave = item.ShortLabel();
                conteo[clave] = conteo.TryGetValue(clave, out int n) ? n + 1 : 1;
            }

            foreach (var par in conteo)
                entries.Add(new StorageEntry { title = par.Key, count = par.Value, swatch = UITheme.Teal });
        }

        return entries;
    }

    // Reconstruye solo las tarjetas de la página activa; el resto del inventario ni se instancia.
    private void RebuildGrid()
    {
        if (gridContent == null) return;

        var entries = BuildEntries();

        int totalPages = Mathf.Max(1, Mathf.CeilToInt(entries.Count / (float)PageSize));
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        // DestroyImmediate a propósito: Destroy() difiere la baja real hasta fin de frame, y dos
        // reconstrucciones en el mismo frame (p. ej. Anterior/Siguiente pulsados muy seguidos, o
        // varias altas de inventario a la vez) apilaban tarjetas viejas sin borrar con las nuevas.
        for (int i = gridContent.childCount - 1; i >= 0; i--)
            DestroyImmediate(gridContent.GetChild(i).gameObject);

        if (entries.Count == 0)
        {
            CreateEmptyLabel();
        }
        else
        {
            int start = currentPage * PageSize;
            int end = Mathf.Min(start + PageSize, entries.Count);
            for (int i = start; i < end; i++)
                CreateStorageCard(entries[i]);
        }

        // La paginación solo aparece si el inventario supera una página entera.
        bool needsPagination = totalPages > 1;
        if (paginationRow != null) paginationRow.SetActive(needsPagination);
        if (needsPagination)
        {
            pageIndicator.text = string.Format(LocalizationManager.Get("UI_PAGE_INDICATOR"), currentPage + 1, totalPages);
            prevPageButton.interactable = currentPage > 0;
            nextPageButton.interactable = currentPage < totalPages - 1;
        }
    }

    private void CreateEmptyLabel()
    {
        var tmp = UIBuild.Label(gridContent, "Empty", UIBuild.BodySize, TextAlignmentOptions.Center);
        tmp.text = LocalizationManager.Get("UI_EMPTY_STORAGE");
        tmp.color = UITheme.TextFaint;
        UIBuild.Stretch(tmp.rectTransform);
    }

    private void CreateStorageCard(StorageEntry entry)
    {
        var go = new GameObject("Card_" + entry.title, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(gridContent, false);
        UITheme.Surface(go, UITheme.Card, UITheme.BorderCard, UITheme.RadiusCard);

        var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(go.transform, false);
        var irt = icon.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0f, 1f);
        irt.anchorMax = new Vector2(0f, 1f);
        irt.pivot = new Vector2(0f, 1f);
        irt.sizeDelta = new Vector2(36f, 36f);
        irt.anchoredPosition = new Vector2(14f, -14f);
        icon.GetComponent<Image>().color = entry.swatch;

        // Ancla top-stretch: offsetMin/offsetMax fijan el rect entero (alto incluido).
        // Nunca pisar después con sizeDelta/anchoredPosition en el mismo eje (lección ya
        // conocida de esta base: la última asignación gana y descuadra el texto).
        var title = UIBuild.Label(go.transform, "Title", UITheme.SizeBody, TextAlignmentOptions.TopLeft);
        title.text = entry.title;
        title.enableWordWrapping = true;
        var trt = title.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.offsetMin = new Vector2(14f, -124f);
        trt.offsetMax = new Vector2(-14f, -58f);

        // Ancla bottom-stretch, mismo criterio.
        var count = UIBuild.Label(go.transform, "Count", UITheme.SizeValue, TextAlignmentOptions.BottomRight);
        count.text = $"x{entry.count}";
        count.fontStyle = FontStyles.Bold;
        count.color = UITheme.Cyan;
        var crt = count.rectTransform;
        crt.anchorMin = new Vector2(0f, 0f);
        crt.anchorMax = new Vector2(1f, 0f);
        crt.pivot = new Vector2(0.5f, 0f);
        crt.offsetMin = new Vector2(14f, 8f);
        crt.offsetMax = new Vector2(-14f, 34f);
    }

    // Un clic es un único evento por frame: no arriesga el apilado de Destroy() diferido que
    // sí podía darse con varias altas de inventario seguidas, así que aquí se reconstruye al
    // momento (sin el paso extra por Update, para que la página cambie sin demora perceptible).
    private void OnPrevPagePressed()
    {
        currentPage--;
        RebuildGrid();
    }

    private void OnNextPagePressed()
    {
        currentPage++;
        RebuildGrid();
    }

    // Cuadrícula (GridLayoutGroup) + barra de paginación, montadas bajo el panel de escena.
    private void BuildGrid()
    {
        if (panel == null) return;

        var viewport = new GameObject("StorageGrid", typeof(RectTransform));
        viewport.transform.SetParent(panel.transform, false);
        // Solo offsetMin/offsetMax: mezclarlos con sizeDelta/anchoredPosition en un ancla
        // top-stretch descuadra el rect (lección ya conocida de otros modales de esta base).
        var vrt = viewport.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0f, 1f);
        vrt.anchorMax = new Vector2(1f, 1f);
        vrt.pivot = new Vector2(0.5f, 1f);
        vrt.offsetMin = new Vector2(40f, -(96f + 500f));
        vrt.offsetMax = new Vector2(-40f, -96f);

        gridContent = vrt;
        var grid = viewport.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(CardWidth, CardHeight);
        grid.spacing = new Vector2(CardGap, CardGap);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = GridColumns;

        paginationRow = new GameObject("Pagination", typeof(RectTransform));
        paginationRow.transform.SetParent(panel.transform, false);
        var prt = paginationRow.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0f);
        prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.sizeDelta = new Vector2(360f, 40f);
        prt.anchoredPosition = new Vector2(0f, 74f);

        prevPageButton = UIBuild.Button(paginationRow.transform, "Btn_PagePrev", "<", UITheme.Neutral,
            new Vector2(48f, 40f), new Vector2(-156f, 0f), OnPrevPagePressed);

        nextPageButton = UIBuild.Button(paginationRow.transform, "Btn_PageNext", ">", UITheme.Neutral,
            new Vector2(48f, 40f), new Vector2(156f, 0f), OnNextPagePressed);

        pageIndicator = UIBuild.Label(paginationRow.transform, "Label", UITheme.SizeBody, TextAlignmentOptions.Center);
        var lrt = pageIndicator.rectTransform;
        lrt.anchorMin = new Vector2(0.5f, 0.5f);
        lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(220f, 40f);
        lrt.anchoredPosition = Vector2.zero;

        paginationRow.SetActive(false);
    }
}
