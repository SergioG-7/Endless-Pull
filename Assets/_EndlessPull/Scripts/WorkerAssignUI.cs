using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panel único de asignación de trabajadores, compartido por todos los edificios. Solo existen
// los widgets de una página: el roster completo nunca se instancia, por muy grande que sea.
public class WorkerAssignUI : MonoBehaviour
{
    [Tooltip("Canvas donde se monta el panel; vacío coge el primero de la escena.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Tamaño del panel.")]
    [SerializeField] private Vector2 size = new Vector2(900f, 700f);

    [Tooltip("Héroes visibles por página; también es cuántas filas se crean en total.")]
    [SerializeField] private int rowsPerPage = 7;

    private const float RowHeight = 60f;
    private const float RowGap = 6f;
    private const float ListTop = 132f;

    // Una fila reutilizable: solo cambia su contenido al pasar de página.
    private class RowWidgets
    {
        public GameObject root;
        public TMP_Text identity;
        public TMP_Text duty;
        public Button toggle;
        public TMP_Text toggleLabel;
        public HeroController hero;
    }

    private GameObject panel;
    private TMP_Text titulo;
    private TMP_Text ocupacion;
    private TMP_Text aviso;
    private TMP_Text vacio;
    private UIPager pager;

    private readonly List<RowWidgets> rows = new List<RowWidgets>();
    private readonly List<HeroController> ordenados = new List<HeroController>();

    private BaseBuilding building;
    private System.Action onClosed;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        Build();
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    void OnEnable() => LocalizationManager.LanguageChanged += OnLanguageChanged;
    void OnDisable() => LocalizationManager.LanguageChanged -= OnLanguageChanged;

    private void OnLanguageChanged()
    {
        if (IsOpen) RefreshHeader();
        if (IsOpen) pager.RefreshLocalization();
    }

    // onClose devuelve el foco a quien abrió el panel (la ficha del edificio).
    public void Show(BaseBuilding target, System.Action onClose)
    {
        if (panel == null || target == null) return;

        building = target;
        onClosed = onClose;
        aviso.text = string.Empty;

        RebuildOrder();
        UIManager.OpenExclusive(panel);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);

        building = null;

        var volver = onClosed;
        onClosed = null;
        volver?.Invoke();
    }

    // El orden solo se recalcula al abrir: asignar a un edificio no cambia el rango de nadie,
    // así que las filas no bailan bajo el dedo mientras se reparte gente.
    private void RebuildOrder()
    {
        ordenados.Clear();
        foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (hero != null && hero.Data != null && !hero.Discarded) ordenados.Add(hero);

        // Se cachea el rango una vez por héroe: el comparador lo pediría O(n log n) veces.
        var party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();
        var rango = new Dictionary<HeroController, int>(ordenados.Count);
        foreach (var hero in ordenados) rango[hero] = BaseBuilding.Rank(hero, party);

        ordenados.Sort((a, b) =>
        {
            int porRango = rango[b].CompareTo(rango[a]);
            if (porRango != 0) return porRango;

            return string.Compare(a.Data.heroName, b.Data.heroName, System.StringComparison.Ordinal);
        });

        vacio.gameObject.SetActive(ordenados.Count == 0);
        vacio.text = LocalizationManager.Get("UI_NO_HEROES");

        RefreshHeader();
        pager.SetupVirtual(ordenados.Count, rows.Count, RenderPage);
    }

    private void RefreshHeader()
    {
        if (building == null) return;

        titulo.text = string.Format(LocalizationManager.Get("UI_ASSIGN_TITLE"),
            BuildingTypes.DisplayName(building.Type));
        ocupacion.text = string.Format(LocalizationManager.Get("UI_ASSIGN_SLOTS"),
            building.Workers.Count, building.Capacity);
    }

    // La llama el pager con el rango de la página activa; las filas sobrantes se apagan.
    private void RenderPage(int start, int count)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            int index = start + i;

            bool visible = i < count && index < ordenados.Count;
            if (row.root.activeSelf != visible) row.root.SetActive(visible);
            if (!visible) { row.hero = null; continue; }

            UpdateRow(row, ordenados[index]);
        }
    }

    private void UpdateRow(RowWidgets row, HeroController hero)
    {
        row.hero = hero;

        var estrellas = new System.Text.StringBuilder();
        for (int i = 0; i < hero.StarRank; i++) estrellas.Append('★');

        int nivel = hero.GetComponent<HeroProgress>()?.Level ?? 1;

        row.identity.text =
            $"<size={UITheme.SizeCaption}><color={UITheme.Tag(UITheme.Rarity(hero.StarRank))}>{estrellas}</color></size>  " +
            $"<b>{hero.Data.heroName}</b>  " +
            $"<size={UITheme.SizeCaption}><color={UITheme.Tag(UITheme.TextMuted)}>" +
            $"{LocalizationManager.Get("UI_LEVEL_ABBR")}{nivel}</color></size>";

        var duty = HeroAssignment.DutyOf(hero);
        string puesto = duty == HeroDuty.Building
            ? HeroAssignment.WorkplaceName(hero)
            : HeroAssignment.DutyName(duty);

        row.duty.text = puesto;
        row.duty.color = duty == HeroDuty.Free ? UITheme.TextMuted : UITheme.Text;

        bool dentro = building != null && building.IsWorker(hero);
        bool hayHueco = building != null && building.Workers.Count < building.Capacity;

        row.toggleLabel.text = LocalizationManager.Get(dentro ? "BTN_UNASSIGN" : "BTN_ASSIGN");
        row.toggleLabel.fontStyle = dentro ? FontStyles.Bold : FontStyles.Normal;
        row.toggle.interactable = dentro || hayHueco;
        row.toggleLabel.color = row.toggle.interactable ? UITheme.Text : UITheme.TextFaint;
        row.toggle.targetGraphic.color = dentro ? UITheme.Amber : UITheme.Neutral;
    }

    private void OnTogglePressed(RowWidgets row)
    {
        if (building == null || row.hero == null) return;

        bool dentro = building.IsWorker(row.hero);
        building.ToggleWorker(row.hero);

        // Si pedía entrar y sigue fuera es que el edificio estaba lleno: se dice, no se calla.
        aviso.text = !dentro && !building.IsWorker(row.hero)
            ? string.Format(LocalizationManager.Get("UI_ASSIGN_FULL"),
                BuildingTypes.DisplayName(building.Type))
            : string.Empty;

        RefreshHeader();
        pager.RefreshPage();
    }

    private void Build()
    {
        if (canvas == null) return;

        panel = UIBuild.Panel(canvas.transform, "WorkerAssignPanel", size, UITheme.Bg);

        titulo = UIBuild.TopLabel(panel.transform, "Title", UITheme.SizeTitle, 34f, -20f,
            TextAlignmentOptions.Left);

        // Centrado y bajo el título: alineado a la derecha se metía debajo del botón de cerrar.
        ocupacion = UIBuild.TopLabel(panel.transform, "Slots", UITheme.SizeName, 30f, -68f,
            TextAlignmentOptions.Center);

        aviso = UIBuild.TopLabel(panel.transform, "Notice", UITheme.SizeBody, 26f, -100f,
            TextAlignmentOptions.Center);
        aviso.color = UITheme.DangerLight;

        for (int i = 0; i < Mathf.Max(1, rowsPerPage); i++)
            rows.Add(CreateRow(-(ListTop + i * (RowHeight + RowGap))));

        vacio = UIBuild.Label(panel.transform, "Empty", UITheme.SizeBody, TextAlignmentOptions.Center);
        UIBuild.Stretch(vacio.rectTransform);
        vacio.color = UITheme.TextMuted;
        vacio.gameObject.SetActive(false);

        pager = new UIPager(panel.transform, new Vector2(0f, 22f));

        UIBuild.CloseButtonTopRight(panel.transform, Close);
    }

    private RowWidgets CreateRow(float y)
    {
        var go = new GameObject("Row", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(panel.transform, false);
        UITheme.Surface(go, UITheme.Card, UITheme.BorderSoft, UITheme.RadiusItem);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(20f, 0f);
        rt.offsetMax = new Vector2(-20f, 0f);
        rt.sizeDelta = new Vector2(-40f, RowHeight);
        rt.anchoredPosition = new Vector2(0f, y);

        var row = new RowWidgets { root = go };

        row.identity = RowLabel(go.transform, "Identity", 16f, 400f, UITheme.SizeName,
            TextAlignmentOptions.Left);
        row.duty = RowLabel(go.transform, "Duty", 430f, 250f, UITheme.SizeBody,
            TextAlignmentOptions.Left);

        row.toggle = UIBuild.Button(go.transform, "Btn_Toggle", string.Empty, UITheme.Neutral,
            new Vector2(170f, 44f), Vector2.zero, () => OnTogglePressed(row));

        var brt = row.toggle.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(1f, 0.5f);
        brt.anchorMax = new Vector2(1f, 0.5f);
        brt.pivot = new Vector2(1f, 0.5f);
        brt.anchoredPosition = new Vector2(-16f, 0f);

        row.toggleLabel = row.toggle.GetComponentInChildren<TMP_Text>();
        go.SetActive(false);
        return row;
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
}
