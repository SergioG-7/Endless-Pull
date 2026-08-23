using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Genera un botón "Mejorar" por cada edificio de la base y lo pinta según si hay materiales.
public class BuildingUpgradeUI : MonoBehaviour
{
    [Tooltip("Contenedor donde se apilan los botones de mejora.")]
    [SerializeField] private RectTransform container;

    [Tooltip("Economía de la que salen madera y hierro.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Color del botón cuando sí hay materiales.")]
    [SerializeField] private Color affordableColor = new Color(0.20f, 0.50f, 0.30f);

    [Tooltip("Color del botón cuando faltan materiales.")]
    [SerializeField] private Color blockedColor = new Color(0.55f, 0.18f, 0.18f);

    private readonly Dictionary<BaseBuilding, Button> buttons = new Dictionary<BaseBuilding, Button>();
    private readonly Dictionary<BaseBuilding, TMP_Text> labels = new Dictionary<BaseBuilding, TMP_Text>();

    void OnEnable()
    {
        if (economy != null) economy.MaterialsChanged += OnMaterialsChanged;
    }

    void OnDisable()
    {
        if (economy != null) economy.MaterialsChanged -= OnMaterialsChanged;
    }

    void Start()
    {
        BuildButtons();
        RefreshAll();
    }

    private void BuildButtons()
    {
        if (container == null) return;

        // BaseBuilding.All ya está poblado: los OnEnable de los edificios corren antes que este Start.
        foreach (var building in BaseBuilding.All)
            CreateButton(building);
    }

    private void CreateButton(BaseBuilding building)
    {
        var go = new GameObject($"Btn_Upgrade_{building.name}",
            typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(container, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(460f, 76f);

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 26f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        var button = go.GetComponent<Button>();
        var captured = building;
        button.onClick.AddListener(() => OnUpgradeClicked(captured));

        buttons[building] = button;
        labels[building] = tmp;

        building.LevelChanged += _ => RefreshAll();
    }

    private void OnUpgradeClicked(BaseBuilding building)
    {
        building.TryUpgrade(economy);
        RefreshAll();
    }

    private void OnMaterialsChanged(int wood, int iron) => RefreshAll();

    private void RefreshAll()
    {
        foreach (var pair in buttons)
        {
            var building = pair.Key;
            if (building == null) continue;

            int wood = building.NextWoodCost;
            int iron = building.NextIronCost;
            bool canAfford = economy != null && economy.CanAffordMaterials(wood, iron);

            pair.Value.interactable = canAfford;
            pair.Value.GetComponent<Image>().color = canAfford ? affordableColor : blockedColor;

            if (labels.TryGetValue(building, out var label))
                label.text = $"Mejorar {building.BuildingName} Nv.{building.Level}  ({wood}M / {iron}H)";
        }
    }
}
