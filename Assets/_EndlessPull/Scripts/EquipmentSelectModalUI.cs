using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Modal de equipamiento manual: lista el almacén con sus cifras y deja elegir la pieza.
// El auto-equipar sigue disponible como atajo, ya en segundo plano.
public class EquipmentSelectModalUI : MonoBehaviour
{
    [Tooltip("Canvas sobre el que se monta el modal.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Tienda de la que salen el almacén y el equipo.")]
    [SerializeField] private ShopManager shop;

    [Tooltip("Tamaño del modal.")]
    [SerializeField] private Vector2 size = new Vector2(920f, 660f);

    [Tooltip("Alto de cada fila del almacén; caben dos líneas por si la pieza trae afijo.")]
    [SerializeField] private float rowHeight = 74f;

    private GameObject panel;
    private TMP_Text titulo;
    private TMP_Text equipado;
    private TMP_Text vacio;
    private TMP_Text etiquetaAuto;
    private TMP_Text etiquetaCerrar;
    private Button botonAuto;
    private RectTransform lista;

    private HeroController hero;

    public bool IsOpen => panel != null && panel.activeSelf;
    public HeroController CurrentHero => hero;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (shop == null) shop = UnityEngine.Object.FindFirstObjectByType<ShopManager>();

        Build();
    }

    void OnEnable()
    {
        LocalizationManager.LanguageChanged += Rebuild;
        if (shop != null) shop.InventoryChanged += Rebuild;
    }

    void OnDisable()
    {
        LocalizationManager.LanguageChanged -= Rebuild;
        if (shop != null) shop.InventoryChanged -= Rebuild;
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    // La abre el botón [Equipar] de la tarjeta del roster.
    public void Open(HeroController target)
    {
        if (panel == null || target == null) return;

        hero = target;
        Rebuild();
        UIManager.OpenExclusive(panel);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        hero = null;
    }

    // Atajo de siempre: coge la primera pieza que le sirva, empezando por el arma.
    public void OnAutoEquipPressed()
    {
        if (shop == null || hero == null) return;

        shop.EquipFromInventory(hero, shop.FirstEquippableFor(hero));
        Rebuild();
    }

    private void OnPiecePressed(EquipmentData item)
    {
        if (shop == null || hero == null || item == null) return;

        shop.EquipFromInventory(hero, item);
        Rebuild();
    }

    private void Rebuild()
    {
        if (lista == null) return;

        for (int i = lista.childCount - 1; i >= 0; i--) Destroy(lista.GetChild(i).gameObject);

        titulo.text = hero != null && hero.Data != null
            ? $"{LocalizationManager.Get("UI_EQUIP")}  ·  <b>{hero.Data.heroName}</b>"
            : LocalizationManager.Get("UI_EQUIP");

        equipado.text = hero != null ? DescribeEquipped() + DescribeAffixes() : string.Empty;
        etiquetaAuto.text = LocalizationManager.Get("UI_AUTO_EQUIP");
        etiquetaCerrar.text = LocalizationManager.Get("UI_CLOSE");

        bool hayAlgo = shop != null && shop.Inventory.Count > 0;
        vacio.gameObject.SetActive(!hayAlgo);
        vacio.text = LocalizationManager.Get("UI_EMPTY_STORAGE");

        botonAuto.interactable = hayAlgo && hero != null;
        botonAuto.targetGraphic.color = botonAuto.interactable ? UITheme.Neutral : UITheme.Neutral;

        if (!hayAlgo) return;

        foreach (var item in shop.Inventory)
            if (item != null) CreateRow(item);
    }

    // Lo que lleva puesto ahora, hueco a hueco, con su desgaste.
    private string DescribeEquipped()
    {
        return $"<color={UITheme.Tag(UITheme.TextFaint)}>" +
               $"{LocalizationManager.Get("UI_SLOT_WEAPON")}</color> {Worn(EquipmentSlot.Weapon)}   " +
               $"<color={UITheme.Tag(UITheme.TextFaint)}>" +
               $"{LocalizationManager.Get("UI_SLOT_SHIELD")}</color> {Worn(EquipmentSlot.Shield)}   " +
               $"<color={UITheme.Tag(UITheme.TextFaint)}>" +
               $"{LocalizationManager.Get("UI_SLOT_ARMOR")}</color> {Worn(EquipmentSlot.Armor)}   " +
               $"<color={UITheme.Tag(UITheme.TextFaint)}>" +
               $"{LocalizationManager.Get("UI_SLOT_ACCESSORY")}</color> {Worn(EquipmentSlot.Accessory)}";
    }

    // Resumen de los afijos activos del héroe, cada uno con su color.
    private string DescribeAffixes()
    {
        var partes = new System.Collections.Generic.List<string>();

        foreach (EquipmentAffix affix in System.Enum.GetValues(typeof(EquipmentAffix)))
        {
            if (affix == EquipmentAffix.None) continue;

            float total = hero.AffixTotal(affix);
            if (total <= 0f) continue;

            partes.Add($"<color={UITheme.Tag(EquipmentAffixes.Color(affix))}>" +
                       $"{EquipmentAffixes.Describe(affix, total)}</color>");
        }

        if (partes.Count == 0) return string.Empty;

        return "\n<size=" + UITheme.SizeCaption + ">" + string.Join("   ", partes) + "</size>";
    }

    private string Worn(EquipmentSlot slot)
    {
        var item = hero.GetEquipped(slot);
        if (item == null) return "-";

        return hero.IsBroken(slot)
            ? $"{item.equipName} [{LocalizationManager.Get("UI_BROKEN")}]"
            : $"{item.equipName} ({hero.DurabilityOf(slot)}/{item.maxDurability})";
    }

    // Una fila por pieza: nombre y tipo a la izquierda, cifras en medio, botón a la derecha.
    private void CreateRow(EquipmentData item)
    {
        var go = new GameObject($"Row_{item.name}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(lista, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, rowHeight);
        UITheme.Surface(go, UITheme.Card, UITheme.BorderSoft, UITheme.RadiusItem);

        bool ocupado = hero != null && hero.GetEquipped(item.slotType) != null;

        var nombre = RowLabel(go.transform, "Name", 16f, 300f, UITheme.SizeName,
            TextAlignmentOptions.Left);
        nombre.text = $"<b>{item.equipName}</b>\n" +
                      $"<size={UITheme.SizeCaption}><color={UITheme.Tag(UITheme.TextMuted)}>" +
                      $"{WeaponTypes.DisplayName(item.weaponType)}" +
                      (ocupado ? $" · {LocalizationManager.Get("UI_SLOT_TAKEN")}" : string.Empty) +
                      $"</color></size>";

        var cifras = RowLabel(go.transform, "Stats", 330f, 380f, UITheme.SizeBody,
            TextAlignmentOptions.Left);
        cifras.color = UITheme.TextSoft;

        // El afijo va en su propio color, en la segunda línea; sin afijo no ocupa nada.
        string afijo = item.HasAffix
            ? $"\n<color={UITheme.Tag(EquipmentAffixes.Color(item.passiveTrait))}>" +
              $"◆ {item.AffixLabel()}</color>"
            : string.Empty;

        cifras.text = $"<color={UITheme.Tag(UITheme.BarHP)}>ATK</color> {item.bonusATK}   " +
                      $"<color={UITheme.Tag(UITheme.BarMP)}>DEF</color> {item.bonusDEF}   " +
                      $"<color={UITheme.Tag(UITheme.BarMorale)}>HP</color> {item.bonusHP}   " +
                      $"<color={UITheme.Tag(UITheme.TextFaint)}>" +
                      $"{LocalizationManager.Get("UI_DURABILITY")}</color> {item.maxDurability}" +
                      afijo;

        var boton = UIBuild.Button(go.transform, "Btn_Pick", LocalizationManager.Get("UI_EQUIP"),
            UITheme.Teal, new Vector2(140f, 42f), Vector2.zero, () => OnPiecePressed(item));

        var brt = boton.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(1f, 0.5f);
        brt.anchorMax = new Vector2(1f, 0.5f);
        brt.pivot = new Vector2(1f, 0.5f);
        brt.anchoredPosition = new Vector2(-16f, 0f);
        boton.interactable = hero != null;
    }

    private static TMP_Text RowLabel(Transform parent, string name, float x, float width,
                                     float size, TextAlignmentOptions align)
    {
        var tmp = UIBuild.Label(parent, name, size, align);

        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta = new Vector2(width, -12f);
        return tmp;
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "EquipmentSelectPanel", size, UITheme.Bg);

        titulo = UIBuild.TopLabel(panel.transform, "Title", UITheme.SizeTitle, 34f, -20f,
            TextAlignmentOptions.Left);

        equipado = UIBuild.TopLabel(panel.transform, "Equipped", UITheme.SizeBody, 50f, -56f,
            TextAlignmentOptions.TopLeft);
        equipado.color = UITheme.TextSoft;

        // Viewport con scroll: el almacén puede pasar de diez piezas sin problema.
        var viewGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image),
                                    typeof(Mask), typeof(ScrollRect));
        viewGo.transform.SetParent(panel.transform, false);
        UITheme.Surface(viewGo, UITheme.BgPanel, UITheme.BorderSoft, UITheme.RadiusCard);
        viewGo.GetComponent<Mask>().showMaskGraphic = true;

        var vrt = viewGo.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0f, 1f);
        vrt.anchorMax = new Vector2(1f, 1f);
        vrt.pivot = new Vector2(0.5f, 1f);
        vrt.offsetMin = new Vector2(20f, 0f);
        vrt.offsetMax = new Vector2(-20f, 0f);
        vrt.sizeDelta = new Vector2(-40f, size.y - 112f - 90f);
        vrt.anchoredPosition = new Vector2(0f, -112f);

        var contenido = new GameObject("Content", typeof(RectTransform));
        contenido.transform.SetParent(viewGo.transform, false);

        lista = contenido.GetComponent<RectTransform>();
        lista.anchorMin = new Vector2(0f, 1f);
        lista.anchorMax = new Vector2(1f, 1f);
        lista.pivot = new Vector2(0.5f, 1f);
        lista.sizeDelta = new Vector2(0f, 0f);
        lista.anchoredPosition = Vector2.zero;

        var layout = contenido.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        contenido.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewGo.GetComponent<ScrollRect>();
        scroll.viewport = vrt;
        scroll.content = lista;
        scroll.horizontal = false;
        scroll.scrollSensitivity = 34f;

        vacio = UIBuild.Label(panel.transform, "Empty", UITheme.SizeBody, TextAlignmentOptions.Center);
        UIBuild.Stretch(vacio.rectTransform);
        vacio.color = UITheme.TextMuted;

        botonAuto = UIBuild.Button(panel.transform, "Btn_AutoEquip", string.Empty,
            UITheme.Neutral, new Vector2(280f, 48f), new Vector2(-150f, -(size.y - 66f)),
            OnAutoEquipPressed);
        etiquetaAuto = botonAuto.GetComponentInChildren<TMP_Text>();

        var cerrar = UIBuild.Button(panel.transform, "Btn_CloseEquip", string.Empty,
            Color.clear, new Vector2(280f, 48f), new Vector2(150f, -(size.y - 66f)), Close);
        etiquetaCerrar = cerrar.GetComponentInChildren<TMP_Text>();
    }
}
