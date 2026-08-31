using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pantalla compartida de escuadras: reparte héroes entre el asalto a la torre y la
// recolección, y guarda o recupera los dos presets de equipo.
public class SquadManagementUI : MonoBehaviour
{
    [Tooltip("Canvas sobre el que se monta el modal.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Gestor de las dos escuadras y de los presets.")]
    [SerializeField] private PartyManager party;

    [Tooltip("Recolección en curso; mientras corra, su escuadra queda bloqueada.")]
    [SerializeField] private ResourceExpeditionManager expeditions;

    [Tooltip("Tamaño del modal.")]
    [SerializeField] private Vector2 size = new Vector2(1180f, 780f);

    [Tooltip("Alto de cada fila de héroe.")]
    [SerializeField] private float rowHeight = 72f;

    [Tooltip("Segundos entre refrescos mientras el panel está abierto.")]
    [SerializeField] private float refreshInterval = 0.5f;

    private GameObject panel;
    private TMP_Text titulo;
    private TMP_Text contadores;
    private TMP_Text aviso;
    private TMP_Text vacio;
    private TMP_Text etiquetaCerrar;
    private RectTransform lista;

    private readonly List<Button> presetApply = new List<Button>();
    private readonly List<Button> presetSave = new List<Button>();
    private readonly List<TMP_Text> presetApplyLabel = new List<TMP_Text>();
    private readonly List<TMP_Text> presetSaveLabel = new List<TMP_Text>();

    private float refreshTimer;

    // Modo confirmación: se abre desde Torre/Expediciones para revisar la escuadra antes de salir.
    // En este modo aparece un botón extra que dispara la acción real (empezar piso o expedición).
    private bool confirmMode;
    private bool confirmForTower;
    private System.Action pendingConfirmAction;
    private Button confirmButton;
    private TMP_Text confirmLabel;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (canvas == null) canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (party == null) party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();
        if (expeditions == null)
            expeditions = UnityEngine.Object.FindFirstObjectByType<ResourceExpeditionManager>();

        Build();
    }

    void OnEnable()
    {
        LocalizationManager.LanguageChanged += Rebuild;
        if (party != null) party.PartyChanged += Rebuild;
    }

    void OnDisable()
    {
        LocalizationManager.LanguageChanged -= Rebuild;
        if (party != null) party.PartyChanged -= Rebuild;
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    // Solo refresca con expedición corriendo (la cuenta atrás); si no, rehacía filas cada
    // 0.5s sin necesidad y parpadeaba.
    void Update()
    {
        if (!IsOpen) return;
        if (expeditions == null || !expeditions.IsRunning) return;

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

        confirmMode = false;
        pendingConfirmAction = null;

        refreshTimer = refreshInterval;
        Rebuild();
        UIManager.OpenExclusive(panel);
    }

    // Lo llaman Torre/Expediciones antes de arrancar: el jugador revisa y confirma la escuadra
    // correspondiente, o cierra el modal sin más si solo quería ajustar algo.
    public void OpenForConfirm(bool forTower, System.Action onConfirm)
    {
        if (panel == null) return;

        confirmMode = true;
        confirmForTower = forTower;
        pendingConfirmAction = onConfirm;

        refreshTimer = refreshInterval;
        Rebuild();
        UIManager.OpenExclusive(panel);
    }

    private void OnConfirmPressed()
    {
        if (party == null) return;

        int count = confirmForTower ? party.Party.Count : party.ExpeditionSquad.Count;
        if (count <= 0)
        {
            aviso.text = LocalizationManager.Get("UI_CONFIRM_NEED_HEROES");
            return;
        }

        var accion = pendingConfirmAction;
        confirmMode = false;
        pendingConfirmAction = null;
        Close();
        accion?.Invoke();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void OnTowerPressed(HeroController hero)
    {
        if (party == null) return;

        bool dentro = party.IsInParty(hero);
        party.Toggle(hero);

        // Si no ha cambiado nada es que el héroe ya tenía otro puesto: se explica en el aviso.
        if (!dentro && !party.IsInParty(hero)) aviso.text = HeroAssignment.BusyWarning(hero);
        else aviso.text = string.Empty;

        Rebuild();
    }

    private void OnGatherPressed(HeroController hero)
    {
        if (party == null) return;

        bool dentro = party.IsInExpedition(hero);
        party.ToggleExpedition(hero);

        if (!dentro && !party.IsInExpedition(hero)) aviso.text = HeroAssignment.BusyWarning(hero);
        else aviso.text = string.Empty;

        Rebuild();
    }

    private void OnApplyPreset(int index)
    {
        if (party == null) return;

        party.ApplyPreset(index);
        aviso.text = string.Empty;
        Rebuild();
    }

    private void OnSavePreset(int index)
    {
        if (party == null) return;

        party.SavePreset(index);
        Rebuild();
    }

    private void Rebuild()
    {
        if (lista == null) return;

        for (int i = lista.childCount - 1; i >= 0; i--) Destroy(lista.GetChild(i).gameObject);

        titulo.text = LocalizationManager.Get("UI_SQUADS");
        etiquetaCerrar.text = LocalizationManager.Get("UI_CLOSE");

        confirmButton.gameObject.SetActive(confirmMode);
        if (confirmMode)
        {
            confirmLabel.text = LocalizationManager.Get(
                confirmForTower ? "UI_CONFIRM_ENTER_TOWER" : "UI_CONFIRM_SEND_GATHER");
            int count = party == null ? 0 : (confirmForTower ? party.Party.Count : party.ExpeditionSquad.Count);
            confirmButton.interactable = count > 0;
        }

        bool recolectando = expeditions != null && expeditions.IsRunning;

        contadores.text = party == null ? string.Empty
            : $"<color={UITheme.Tag(UITheme.Amber)}>{LocalizationManager.Get("UI_TOWER_SQUAD")}</color> " +
              $"<b>{party.Party.Count}/{party.MaxPartySize}</b>    " +
              $"<color={UITheme.Tag(UITheme.Cyan)}>{LocalizationManager.Get("UI_GATHER_SQUAD")}</color> " +
              $"<b>{party.ExpeditionSquad.Count}/{party.MaxExpeditionSize}</b>";

        if (recolectando && string.IsNullOrEmpty(aviso.text))
            aviso.text = string.Format(LocalizationManager.Get("UI_GATHERING_NOW"),
                expeditions.Remaining.ToString("0"));

        for (int i = 0; i < presetApply.Count; i++)
        {
            presetApplyLabel[i].text = string.Format(LocalizationManager.Get("UI_PRESET"), i + 1);
            presetSaveLabel[i].text = LocalizationManager.Get("UI_SAVE_PRESET");

            bool guardado = party != null && party.HasPreset(i);
            presetApply[i].interactable = guardado;
            presetApply[i].targetGraphic.color = guardado ? UITheme.AccentPick : UITheme.Neutral;
            presetSave[i].interactable = party != null;
        }

        var heroes = new List<HeroController>(
            UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None));

        // FindObjectsByType no garantiza orden y las filas llevan botón: sin ordenar, bailan.
        heroes.Sort(Compare);

        vacio.gameObject.SetActive(heroes.Count == 0);
        vacio.text = LocalizationManager.Get("UI_NO_HEROES");

        foreach (var hero in heroes)
            if (hero != null && hero.Data != null) CreateRow(hero, recolectando);
    }

    private static int Compare(HeroController a, HeroController b)
    {
        if (a == null || a.Data == null) return 1;
        if (b == null || b.Data == null) return -1;

        int porRareza = b.StarRank.CompareTo(a.StarRank);
        if (porRareza != 0) return porRareza;

        return string.Compare(a.Data.heroName, b.Data.heroName, System.StringComparison.Ordinal);
    }

    // Una fila por héroe: identidad, puesto actual y los dos botones de escuadra.
    private void CreateRow(HeroController hero, bool recolectando)
    {
        var go = new GameObject($"Row_{hero.name}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(lista, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, rowHeight);
        UITheme.Surface(go, UITheme.Card, UITheme.BorderSoft, UITheme.RadiusItem);

        var duty = HeroAssignment.DutyOf(hero);
        bool bloqueado = recolectando && duty == HeroDuty.Expedition;

        var progress = hero.GetComponent<HeroProgress>();
        int nivel = progress != null ? progress.Level : 1;

        var estrellas = new System.Text.StringBuilder();
        for (int i = 0; i < hero.StarRank; i++) estrellas.Append('★');

        var rareza = UITheme.Rarity(hero.StarRank);

        // El humor puede venir vacio: sin esto quedaba un punto suelto tras el nivel.
        string humor = string.IsNullOrEmpty(hero.MoodName) ? string.Empty : " · " + hero.MoodName;

        var nombre = RowLabel(go.transform, "Name", 16f, 320f, UITheme.SizeName,
            TextAlignmentOptions.Left);
        nombre.text = $"<size={UITheme.SizeCaption}><color={UITheme.Tag(rareza)}>{estrellas}</color></size>  " +
                      $"<b>{hero.Data.heroName}</b>\n" +
                      $"<size={UITheme.SizeCaption}><color={UITheme.Tag(UITheme.TextMuted)}>" +
                      $"Nv.{nivel}{humor}</color></size>";

        // El puesto que ocupa ahora: nombre del edificio si trabaja, o el rótulo del deber.
        string puesto = duty == HeroDuty.Building
            ? HeroAssignment.WorkplaceName(hero)
            : HeroAssignment.DutyName(duty);

        var estado = RowLabel(go.transform, "Duty", 350f, 300f, UITheme.SizeBody,
            TextAlignmentOptions.Left);
        estado.color = duty == HeroDuty.Free ? UITheme.TextMuted : UITheme.Text;
        estado.text = bloqueado
            ? $"<color={UITheme.Tag(UITheme.Cyan)}>{puesto} · {LocalizationManager.Get("UI_LOCKED")}</color>"
            : puesto;

        bool enTorre = party != null && party.IsInParty(hero);
        bool enRecoleccion = party != null && party.IsInExpedition(hero);

        // Se puede quitar siempre; para meter, el héroe tiene que estar libre.
        bool puedeTorre = !bloqueado && (enTorre || duty == HeroDuty.Free);
        bool puedeRecoger = !bloqueado && (enRecoleccion || duty == HeroDuty.Free);

        CreateToggle(go.transform, "Btn_Tower", -172f, LocalizationManager.Get("UI_TOWER_SQUAD"),
            enTorre, puedeTorre, UITheme.Amber, () => OnTowerPressed(hero));

        CreateToggle(go.transform, "Btn_Gather", -16f, LocalizationManager.Get("UI_GATHER_SQUAD"),
            enRecoleccion, puedeRecoger, UITheme.Teal, () => OnGatherPressed(hero));
    }

    private void CreateToggle(Transform row, string name, float x, string text, bool activo,
                              bool interactuable, Color color,
                              UnityEngine.Events.UnityAction onClick)
    {
        var boton = UIBuild.Button(row, name, text, activo ? color : UITheme.Neutral,
            new Vector2(148f, 44f), Vector2.zero, onClick);

        var rt = boton.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);

        boton.interactable = interactuable;

        var label = boton.GetComponentInChildren<TMP_Text>();
        label.fontSize = UITheme.SizeBody;
        label.fontStyle = activo ? FontStyles.Bold : FontStyles.Normal;
        label.color = interactuable ? UITheme.Text : UITheme.TextFaint;
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

        panel = UIBuild.Panel(canvas.transform, "SquadManagementPanel", size, UITheme.Bg);

        titulo = UIBuild.TopLabel(panel.transform, "Title", UITheme.SizeTitle, 34f, -20f,
            TextAlignmentOptions.Left);

        contadores = UIBuild.TopLabel(panel.transform, "Counters", UITheme.SizeBody, 30f, -22f,
            TextAlignmentOptions.Right);

        // Barra de presets: aplicar y guardar, uno al lado del otro.
        float x = -(size.x * 0.5f) + 24f;
        for (int i = 0; i < PartyManager.PresetCount; i++)
        {
            int indice = i;

            var aplicar = UIBuild.Button(panel.transform, $"Btn_Preset{i + 1}", string.Empty,
                UITheme.AccentPick, new Vector2(180f, 42f),
                new Vector2(x + 90f, -64f), () => OnApplyPreset(indice));
            presetApply.Add(aplicar);
            presetApplyLabel.Add(aplicar.GetComponentInChildren<TMP_Text>());
            x += 188f;

            var guardar = UIBuild.Button(panel.transform, $"Btn_SavePreset{i + 1}", string.Empty,
                UITheme.Neutral, new Vector2(150f, 42f),
                new Vector2(x + 75f, -64f), () => OnSavePreset(indice));
            presetSave.Add(guardar);
            presetSaveLabel.Add(guardar.GetComponentInChildren<TMP_Text>());
            x += 166f;
        }

        aviso = UIBuild.TopLabel(panel.transform, "Notice", UITheme.SizeBody, 26f, -66f,
            TextAlignmentOptions.Right);
        aviso.color = UITheme.DangerLight;

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
        vrt.sizeDelta = new Vector2(-40f, size.y - 118f - 84f);
        vrt.anchoredPosition = new Vector2(0f, -118f);

        var contenido = new GameObject("Content", typeof(RectTransform));
        contenido.transform.SetParent(viewGo.transform, false);

        lista = contenido.GetComponent<RectTransform>();
        lista.anchorMin = new Vector2(0f, 1f);
        lista.anchorMax = new Vector2(1f, 1f);
        lista.pivot = new Vector2(0.5f, 1f);
        lista.sizeDelta = Vector2.zero;
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

        var cerrar = UIBuild.Button(panel.transform, "Btn_CloseSquads", string.Empty,
            Color.clear, new Vector2(260f, 46f), new Vector2(-150f, -(size.y - 62f)), Close);
        etiquetaCerrar = cerrar.GetComponentInChildren<TMP_Text>();

        // Solo se ve en modo confirmación (abierto desde Torre/Expediciones antes de salir).
        confirmButton = UIBuild.Button(panel.transform, "Btn_ConfirmSquad", string.Empty,
            UITheme.AccentPick, new Vector2(340f, 46f), new Vector2(220f, -(size.y - 62f)), OnConfirmPressed);
        confirmLabel = confirmButton.GetComponentInChildren<TMP_Text>();
        confirmButton.gameObject.SetActive(false);
    }
}
