using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ficha de detalle de un héroe: la abre un clic sobre su tarjeta en el Roster. Aquí viven
// seis de las ocho acciones que antes iban pegadas a la tarjeta (Lock, Party, Equip/Unequip,
// Subclass, Repair); Ascend y Synth se mudaron al Santuario (ver SanctuaryUI).
public class HeroDetailModal : MonoBehaviour
{
    [Tooltip("Canvas sobre el que se monta el modal.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Tienda de la que salen inventario y equipo.")]
    [SerializeField] private ShopManager shop;

    [Tooltip("Taller que repara el equipo desgastado.")]
    [SerializeField] private CraftingManager crafting;

    [Tooltip("Escuadra a la que se apunta o se saca al héroe.")]
    [SerializeField] private PartyManager party;

    [Tooltip("Modal de equipamiento manual que abre el botón Equipar.")]
    [SerializeField] private EquipmentSelectModalUI equipModal;

    [Tooltip("Tamaño del modal.")]
    [SerializeField] private Vector2 size = new Vector2(820f, 520f);

    [Tooltip("Segundos entre refrescos mientras el modal está abierto (vida, moral y estados cambian solos).")]
    [SerializeField] private float refreshInterval = 0.5f;

    private const float PortraitSize = 96f;
    private const float ColLeft = 420f;
    private const float BtnWidth = 168f;
    private const float BtnHeight = 46f;
    private const float BtnGap = 12f;

    // Deja hueco bajo el botón de cierre (ocupa y -20 a -64) para que la grilla de acciones no lo solape.
    private const float ActionGridTop = -84f;

    private GameObject panel;
    private TMP_Text identityLabel;
    private TMP_Text bioLabel;
    private Image hpFill;
    private TMP_Text hpLabel;
    private Image mpFill;
    private TMP_Text mpLabel;
    private Image moraleFill;
    private TMP_Text moraleLabel;
    private TMP_Text gearLabelLeft;
    private TMP_Text gearLabelRight;
    private Image portraitArt;
    private Image portraitFrame;

    private Button btnLock, btnParty, btnEquip, btnUnequip, btnSubclass, btnRepair;

    private HeroController hero;
    private float refreshTimer;

    public bool IsOpen => panel != null && panel.activeSelf;
    public HeroController CurrentHero => hero;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (shop == null) shop = UnityEngine.Object.FindFirstObjectByType<ShopManager>();
        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();
        if (party == null) party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();
        if (equipModal == null) equipModal = UnityEngine.Object.FindFirstObjectByType<EquipmentSelectModalUI>();

        Build();
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        if (!IsOpen) return;

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer > 0f) return;

        refreshTimer = refreshInterval;
        Rebuild();
    }

    // La abre un clic en la tarjeta del roster.
    public void Open(HeroController target)
    {
        if (panel == null || target == null) return;

        hero = target;
        refreshTimer = refreshInterval;
        Rebuild();
        UIManager.OpenExclusive(panel);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        hero = null;
    }

    private void Rebuild()
    {
        if (hero == null || hero.Data == null) { Close(); return; }

        var progress = hero.GetComponent<HeroProgress>();
        int level = progress != null ? progress.Level : 1;
        int maxLevel = progress != null ? progress.MaxLevel : 0;
        string tope = progress != null && progress.IsMaxLevel ? "  TOPE" : string.Empty;

        var stars = new StringBuilder();
        for (int i = 0; i < hero.StarRank; i++) stars.Append('★');

        string oficio = hero.Subclass != HeroSubclass.None
            ? hero.SubclassName
            : HeroTraits.DisplayName(hero.Trait);

        if (hero.IsSynthesisCandidate)
            oficio += "  <color=#C08040>· candidato a síntesis ·</color>";

        var rareza = HeroProgress.RarityColor(hero.StarRank);
        portraitFrame.color = UITheme.Hex("262838");
        var borde = portraitFrame.transform.Find("Border");
        if (borde != null) borde.GetComponent<Image>().color = rareza;
        UIBuild.HeroArt(portraitFrame.transform, hero.Data.bodySprite, PortraitSize - 12f);

        identityLabel.text = $"<size={UITheme.SizeName}><b><color={UITheme.Tag(rareza)}>{stars}</color></b></size>\n" +
                             $"<size={UITheme.SizeTitle}><b>{hero.Data.heroName}</b></size>\n" +
                             $"<color={UITheme.Tag(UITheme.TextMuted)}>{oficio} · Nv.{level}/{maxLevel}{tope}</color>";

        bioLabel.text = hero.Data.GetLocalizedBio();

        float ratioHp = hero.MaxHealth > 0 ? (float)hero.CurrentHealth / hero.MaxHealth : 0f;
        float ratioMp = hero.MaxMP > 0 ? (float)hero.CurrentMP / hero.MaxMP : 0f;
        UIBuild.SetBar(hpFill, ratioHp);
        hpLabel.text = $"HP {hero.CurrentHealth}/{hero.MaxHealth}";
        UIBuild.SetBar(mpFill, ratioMp);
        mpLabel.text = $"MP {hero.CurrentMP}/{hero.MaxMP}";
        UIBuild.SetBar(moraleFill, hero.MoralePercent / 100f);
        moraleLabel.text = $"Moral {hero.MoralePercent} {hero.MoodName}".TrimEnd();

        string estados = hero.Status.Describe();
        string lineaEstado = string.IsNullOrEmpty(estados)
            ? $"{Key("Maestría")} {hero.Mastery.Describe()}"
            : $"{Key("Estados")} {estados}";

        // Dos columnas para llenar el espacio inferior izquierdo en vez de una sola tira vertical.
        gearLabelLeft.text = $"{Key("Pasivas")} {PassiveSkills.Describe(hero.Passives)}\n" +
                             $"{Key("Arma")} {GearLabel(hero, EquipmentSlot.Weapon)}\n" +
                             $"{Key("Escudo")} {GearLabel(hero, EquipmentSlot.Shield)}";

        gearLabelRight.text = $"{Key("Armadura")} {GearLabel(hero, EquipmentSlot.Armor)}\n" +
                              $"{Key("Accesorio")} {GearLabel(hero, EquipmentSlot.Accessory)}\n" +
                              $"{lineaEstado}";

        RefreshButtons();
    }

    private static string Key(string text) => $"<color={UITheme.Tag(UITheme.TextFaint)}>{text}</color>";

    private static string GearLabel(HeroController hero, EquipmentSlot slot)
    {
        var item = hero.GetEquipped(slot);
        if (item == null) return "-";

        string etiqueta = item.ShortLabel();
        return hero.IsBroken(slot)
            ? $"{etiqueta} [{LocalizationManager.Get("UI_BROKEN")}]"
            : $"{etiqueta} ({hero.DurabilityOf(slot)}/{item.maxDurability})";
    }

    private void RefreshButtons()
    {
        bool inParty = party != null && party.IsInParty(hero);
        bool wearsGear = hero.Weapon != null || hero.Shield != null
                         || hero.Armor != null || hero.Accessory != null;
        bool puedeSubclase = hero.StarRank >= HeroSubclasses.MinStarRank;
        bool roto = hero.FirstBrokenSlot() != null;

        SetButton(btnLock, LocalizationManager.Get(hero.IsLocked ? "UI_LOCK" : "UI_UNLOCK"),
            hero.IsLocked ? UITheme.DangerSoft : UITheme.Neutral, true);
        SetButton(btnParty, LocalizationManager.Get(inParty ? "UI_IN_PARTY" : "UI_PARTY"),
            inParty ? UITheme.Amber : UITheme.Neutral, party != null);
        SetButton(btnEquip, LocalizationManager.Get("UI_EQUIP"), UITheme.Teal, shop != null);
        SetButton(btnUnequip, LocalizationManager.Get("UI_UNEQUIP"), UITheme.Teal, wearsGear);
        SetButton(btnSubclass, LocalizationManager.Get("UI_SUBCLASS"), UITheme.AccentSoft, puedeSubclase);
        SetButton(btnRepair, LocalizationManager.Get("UI_REPAIR"), UITheme.DangerSoft, roto && crafting != null);
    }

    private static void SetButton(Button button, string text, Color color, bool interactable)
    {
        if (button == null) return;

        var label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = text;
            label.color = interactable ? UITheme.Text : UITheme.TextFaint;
        }

        button.targetGraphic.color = interactable ? color : UITheme.Neutral;
        button.interactable = interactable;
    }

    private void OnLockClicked()
    {
        if (hero == null) return;

        hero.ToggleLock();

        SaveManager.RequestSave();
        Rebuild();
    }

    private void OnPartyClicked()
    {
        if (hero == null || party == null) return;

        party.Toggle(hero);
        Rebuild();
    }

    private void OnEquipClicked()
    {
        if (hero == null) return;

        if (equipModal != null) { equipModal.Open(hero); return; }
        if (shop == null) return;

        shop.EquipFromInventory(hero, shop.FirstEquippableFor(hero));
        Rebuild();
    }

    private void OnUnequipClicked()
    {
        if (hero == null || shop == null) return;

        if (hero.Weapon != null) shop.UnequipToInventory(hero, EquipmentSlot.Weapon);
        else if (hero.Shield != null) shop.UnequipToInventory(hero, EquipmentSlot.Shield);
        else if (hero.Armor != null) shop.UnequipToInventory(hero, EquipmentSlot.Armor);
        else if (hero.Accessory != null) shop.UnequipToInventory(hero, EquipmentSlot.Accessory);

        Rebuild();
    }

    // Rota entre las tres subclases del arquetipo del héroe.
    private void OnSubclassClicked()
    {
        if (hero == null) return;

        var progress = hero.GetComponent<HeroProgress>();
        if (progress != null) progress.CycleSubclass();
        Rebuild();
    }

    private void OnRepairClicked()
    {
        if (hero == null) return;

        if (crafting != null) crafting.TryRepair(hero);
        Rebuild();
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "HeroDetailPanel", size, UITheme.Bg);

        var closeBtn = UIBuild.Button(panel.transform, "Btn_CloseDetail", "×", Color.clear,
            new Vector2(44f, 44f), new Vector2(-24f, -20f), Close);
        var crt = closeBtn.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(1f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(1f, 1f);

        // Columna izquierda: retrato, identidad, barras y equipo.
        portraitFrame = BuildPortraitFrame(panel.transform, 24f, -24f);

        identityLabel = UIBuild.Label(panel.transform, "Identity", UITheme.SizeName, TextAlignmentOptions.Left);
        AnchorTopLeft(identityLabel.rectTransform, 24f + PortraitSize + 16f, -24f,
            ColLeft - PortraitSize - 16f, 96f);

        bioLabel = UIBuild.Label(panel.transform, "Bio", UITheme.SizeCaption, TextAlignmentOptions.TopLeft);
        bioLabel.color = UITheme.TextMuted;
        AnchorTopLeft(bioLabel.rectTransform, 24f, -24f - PortraitSize - 12f, ColLeft, 70f);

        float barsY = -24f - PortraitSize - 90f;
        hpFill = UIBuild.Bar(panel.transform, "Bar_HP", new Vector2(ColLeft, 16f),
            new Vector2(24f, barsY), UITheme.BarHP, out hpLabel);
        AnchorBarLeft(hpFill, 24f, barsY);

        mpFill = UIBuild.Bar(panel.transform, "Bar_MP", new Vector2(ColLeft, 16f),
            new Vector2(24f, barsY - 24f), UITheme.BarMP, out mpLabel);
        AnchorBarLeft(mpFill, 24f, barsY - 24f);

        moraleFill = UIBuild.Bar(panel.transform, "Bar_Morale", new Vector2(ColLeft, 16f),
            new Vector2(24f, barsY - 48f), UITheme.BarMorale, out moraleLabel);
        AnchorBarLeft(moraleFill, 24f, barsY - 48f);

        // El relleno dorado deja el texto claro casi ilegible: un contorno oscuro lo salva.
        var moraleOutline = moraleLabel.gameObject.AddComponent<Outline>();
        moraleOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        moraleOutline.effectDistance = new Vector2(1f, -1f);

        // Misma franja horizontal que antes (24..24+ColLeft) partida en dos columnas con hueco entre medio.
        const float gearGap = 16f;
        float gearColWidth = (ColLeft - gearGap) / 2f;
        float gearY = barsY - 78f;
        const float gearHeight = 190f;

        gearLabelLeft = UIBuild.Label(panel.transform, "GearLeft", UITheme.SizeSmall, TextAlignmentOptions.TopLeft);
        gearLabelLeft.color = UITheme.TextSoft;
        gearLabelLeft.lineSpacing = 14f;
        AnchorTopLeft(gearLabelLeft.rectTransform, 24f, gearY, gearColWidth, gearHeight);

        gearLabelRight = UIBuild.Label(panel.transform, "GearRight", UITheme.SizeSmall, TextAlignmentOptions.TopLeft);
        gearLabelRight.color = UITheme.TextSoft;
        gearLabelRight.lineSpacing = 14f;
        AnchorTopLeft(gearLabelRight.rectTransform, 24f + gearColWidth + gearGap, gearY, gearColWidth, gearHeight);

        // Columna derecha: las seis acciones, en dos columnas de tres.
        float rightX = ColLeft + 48f;
        btnLock = BuildActionButton(panel.transform, "Btn_Lock", rightX, 0, OnLockClicked);
        btnParty = BuildActionButton(panel.transform, "Btn_Party", rightX, 1, OnPartyClicked);
        btnEquip = BuildActionButton(panel.transform, "Btn_Equip", rightX, 2, OnEquipClicked);
        btnUnequip = BuildActionButton(panel.transform, "Btn_Unequip", rightX, 3, OnUnequipClicked);
        btnSubclass = BuildActionButton(panel.transform, "Btn_Subclass", rightX, 4, OnSubclassClicked);
        btnRepair = BuildActionButton(panel.transform, "Btn_Repair", rightX, 5, OnRepairClicked);

        // Las acciones se crean después: sin esto quedaban por encima y tapaban el cierre.
        closeBtn.transform.SetAsLastSibling();

        panel.SetActive(false);
    }

    // Un botón por acción; dos por fila para no estirar el panel de más.
    private Button BuildActionButton(Transform parent, string name, float baseX, int slotIndex,
                                     UnityEngine.Events.UnityAction onClick)
    {
        int columna = slotIndex % 2;
        int fila = slotIndex / 2;

        float x = baseX + columna * (BtnWidth + BtnGap);
        float y = ActionGridTop - fila * (BtnHeight + BtnGap);

        var button = UIBuild.Button(parent, name, string.Empty, UITheme.Neutral,
            new Vector2(BtnWidth, BtnHeight), new Vector2(x, y), onClick);

        var rt = button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);

        return button;
    }

    private Image BuildPortraitFrame(Transform parent, float x, float y)
    {
        var go = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(PortraitSize, PortraitSize);
        rt.anchoredPosition = new Vector2(x, y);

        var image = UITheme.Surface(go, UITheme.Hex("262838"), UITheme.BorderSoft, UITheme.RadiusCard);
        image.raycastTarget = false;
        return image;
    }

    private static void AnchorTopLeft(RectTransform rt, float x, float y, float width, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(x, y);
    }

    private static void AnchorBarLeft(Image fill, float x, float y)
    {
        var rt = fill.transform.parent.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
    }
}
