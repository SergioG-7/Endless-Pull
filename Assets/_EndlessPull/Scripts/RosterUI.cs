using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panel modal que lista los héroes vivos de la base.
public class RosterUI : MonoBehaviour
{
    [Tooltip("Raíz del panel; se activa y desactiva al abrir y cerrar.")]
    [SerializeField] private GameObject panel;

    [Tooltip("Contenedor donde se generan las filas de héroes.")]
    [SerializeField] private RectTransform content;

    [Tooltip("Texto que se muestra cuando no queda ningún héroe.")]
    [SerializeField] private TMP_Text emptyLabel;

    [Tooltip("Segundos entre refrescos mientras el panel está abierto.")]
    [SerializeField] private float refreshInterval = 0.5f;

    [Tooltip("Alto de cada fila de héroe, en píxeles de UI.")]
    [SerializeField] private float rowHeight = 78f;

    [Tooltip("Tienda de la que salen inventario y equipo.")]
    [SerializeField] private ShopManager shop;

    [Tooltip("Economía que paga las ascensiones.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Taller del que salen las Piedras de Ascensión.")]
    [SerializeField] private CraftingManager crafting;

    [Tooltip("Escuadra a la que se apunta o se saca a los héroes.")]
    [SerializeField] private PartyManager party;

    [Tooltip("Gestor de síntesis al que apuntan los botones de cada fila.")]
    [SerializeField] private SynthesisManager synthesis;

    [Tooltip("Ancho de cada botón de la fila.")]
    [SerializeField] private float rowButtonWidth = 150f;

    [Tooltip("Color del botón de síntesis en reposo.")]
    [SerializeField] private Color synthColor = new Color(0.35f, 0.30f, 0.60f);

    [Tooltip("Color del botón del héroe elegido como objetivo.")]
    [SerializeField] private Color targetColor = new Color(0.70f, 0.45f, 0.15f);

    [Tooltip("Color del botón de ascender cuando se puede pagar.")]
    [SerializeField] private Color ascendColor = new Color(0.65f, 0.55f, 0.15f);

    [Tooltip("Color de los botones de equipo.")]
    [SerializeField] private Color gearColor = new Color(0.25f, 0.45f, 0.45f);

    [Tooltip("Color de un botón que ahora mismo no se puede pulsar.")]
    [SerializeField] private Color disabledColor = new Color(0.28f, 0.28f, 0.32f);

    private float refreshTimer;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (synthesis == null) synthesis = UnityEngine.Object.FindFirstObjectByType<SynthesisManager>();
        if (shop == null) shop = UnityEngine.Object.FindFirstObjectByType<ShopManager>();
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();
        if (party == null) party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();
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

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (panel == null) return;

        // Abrir uno cierra los demas: nunca se solapan dos modales.
        UIManager.OpenExclusive(panel);
        refreshTimer = refreshInterval;
        Rebuild();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    // Regenera la lista entera; con una decena de héroes sale más barato que diferenciar.
    private void Rebuild()
    {
        if (content == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        var heroes = UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None);

        // FindObjectsByType no garantiza orden y las filas llevan botón: sin ordenar, bailan al refrescar.
        System.Array.Sort(heroes, CompareHeroes);

        if (emptyLabel != null) emptyLabel.gameObject.SetActive(heroes.Length == 0);

        foreach (var hero in heroes)
            CreateRow(hero);
    }

    // Primero los de más estrellas, y a igualdad por nombre: el orden no cambia entre refrescos.
    private static int CompareHeroes(HeroController a, HeroController b)
    {
        if (a == null || a.Data == null) return 1;
        if (b == null || b.Data == null) return -1;

        int byRank = b.StarRank.CompareTo(a.StarRank);
        if (byRank != 0) return byRank;

        return string.Compare(a.Data.heroName, b.Data.heroName, System.StringComparison.Ordinal);
    }

    private void CreateRow(HeroController hero)
    {
        var go = new GameObject($"Row_{hero.name}", typeof(RectTransform));
        go.transform.SetParent(content, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, rowHeight);

        // La etiqueta ocupa la fila menos el hueco reservado a los cuatro botones.
        float buttonsWidth = rowButtonWidth * 5f + 30f;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);

        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = new Vector2(-(buttonsWidth + 16f), 0f);

        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 18f;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.white;
        tmp.text = BuildRowText(hero) + "\n" + BuildDetailText(hero);

        CreateRowButtons(go.transform, hero);
    }

    // Cuatro botones alineados a la derecha, de fuera hacia dentro.
    private void CreateRowButtons(Transform row, HeroController hero)
    {
        var progress = hero.GetComponent<HeroProgress>();
        bool isTarget = synthesis != null && synthesis.Target == hero;
        bool canAscend = progress != null && progress.CanAscend(economy, crafting);
        bool inParty = party != null && party.IsInParty(hero);
        bool hasGear = shop != null && shop.FirstEquippableFor(hero) != null;
        bool wearsGear = hero.Weapon != null || hero.Shield != null
                         || hero.Armor != null || hero.Accessory != null;

        CreateButton(row, "Btn_Synth", 0, isTarget ? "OBJETIVO" : "Sintetizar",
            isTarget ? targetColor : synthColor, synthesis != null, () => OnSynthClicked(hero));

        CreateButton(row, "Btn_Ascend", 1, "Ascender",
            canAscend ? ascendColor : disabledColor, canAscend, () => OnAscendClicked(hero));

        CreateButton(row, "Btn_Equip", 2, "Equipar",
            hasGear ? gearColor : disabledColor, hasGear, () => OnEquipClicked(hero));

        CreateButton(row, "Btn_Unequip", 3, "Quitar",
            wearsGear ? gearColor : disabledColor, wearsGear, () => OnUnequipClicked(hero));

        CreateButton(row, "Btn_Party", 4, inParty ? "EN ESCUADRA" : "Escuadra",
            inParty ? targetColor : synthColor, party != null, () => OnPartyClicked(hero));
    }

    private void OnPartyClicked(HeroController hero)
    {
        if (party == null) return;

        party.Toggle(hero);
        Rebuild();
    }

    private void CreateButton(Transform row, string name, int slotIndex, string text,
                              Color color, bool interactable, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(row, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-4f - slotIndex * (rowButtonWidth + 6f), 0f);
        rt.sizeDelta = new Vector2(rowButtonWidth, rowHeight - 16f);

        var image = go.GetComponent<Image>();
        image.color = color;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);

        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 18f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = text;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.interactable = interactable;
        button.onClick.AddListener(onClick);
    }

    private void OnAscendClicked(HeroController hero)
    {
        var progress = hero.GetComponent<HeroProgress>();
        if (progress != null) progress.AscendHero(economy, crafting);
        Rebuild();
    }

    private void OnEquipClicked(HeroController hero)
    {
        if (shop == null) return;

        shop.EquipFromInventory(hero, shop.FirstEquippableFor(hero));
        Rebuild();
    }

    // Quita la primera pieza que lleve puesta, de arma a accesorio.
    private void OnUnequipClicked(HeroController hero)
    {
        if (shop == null) return;

        if (hero.Weapon != null) shop.UnequipToInventory(hero, EquipmentSlot.Weapon);
        else if (hero.Shield != null) shop.UnequipToInventory(hero, EquipmentSlot.Shield);
        else if (hero.Armor != null) shop.UnequipToInventory(hero, EquipmentSlot.Armor);
        else if (hero.Accessory != null) shop.UnequipToInventory(hero, EquipmentSlot.Accessory);

        Rebuild();
    }

    // Primer clic elige objetivo; el segundo, sobre otro héroe, lo sacrifica.
    private void OnSynthClicked(HeroController hero)
    {
        if (synthesis == null) return;

        synthesis.SelectHero(hero);
        Rebuild();
    }

    private string BuildRowText(HeroController hero)
    {
        var progress = hero.GetComponent<HeroProgress>();
        int level = progress != null ? progress.Level : 1;

        // La fuente por defecto no tiene ★, asi que las estrellas van en ASCII.
        var stars = new StringBuilder();
        for (int i = 0; i < hero.StarRank; i++) stars.Append('*');

        // Agotamiento y ánimo se marcan en la fila: explican por qué va lento o pega más.
        string tired = hero.IsExhausted ? " AGOTADO" : string.Empty;
        string mood = string.IsNullOrEmpty(hero.MoodName) ? string.Empty : " " + hero.MoodName;

        int maxLevel = progress != null ? progress.MaxLevel : 0;
        string tope = progress != null && progress.IsMaxLevel ? " TOPE" : string.Empty;

        return $"{hero.Data.heroName}  {stars}   Nv. {level}/{maxLevel}{tope}   " +
               $"{HeroTraits.DisplayName(hero.Trait)}   " +
               $"HP {hero.CurrentHealth}/{hero.MaxHealth}   " +
               $"MP {hero.CurrentMP}/{hero.MaxMP}   " +
               $"Fat {hero.FatiguePercent}{tired}   " +
               $"Mor {hero.MoralePercent}{mood}   " +
               $"ATK {hero.Attack}  DEF {hero.Defense}   [{hero.State}]";
    }

    // Segunda línea: pasivas, maestrías entrenadas y las tres piezas de equipo.
    private string BuildDetailText(HeroController hero)
    {
        return $"  Pasivas: {PassiveSkills.Describe(hero.Passives)}   " +
               $"Maestría: {hero.Mastery.Describe()}   " +
               $"Arma: {SlotLabel(hero.Weapon)}   " +
               $"Escudo: {SlotLabel(hero.Shield)}   " +
               $"Armadura: {SlotLabel(hero.Armor)}   " +
               $"Accesorio: {SlotLabel(hero.Accessory)}";
    }

    private static string SlotLabel(EquipmentData item) => item != null ? item.ShortLabel() : "-";
}
