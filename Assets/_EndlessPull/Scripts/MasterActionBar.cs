using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Barra de decretos del Maestro: pensada para tocar en móvil, sin depender del teclado.
public class MasterActionBar : MonoBehaviour
{
    [Tooltip("Comandante que ejecuta los decretos.")]
    [SerializeField] private MasterCommander commander;

    [Tooltip("Botón de Curar Escuadra.")]
    [SerializeField] private Button healButton;

    [Tooltip("Botón de Enfocar Objetivo.")]
    [SerializeField] private Button focusButton;

    [Tooltip("Botón de Reagruparse.")]
    [SerializeField] private Button regroupButton;

    [Tooltip("Botón de Retirada de emergencia.")]
    [SerializeField] private Button retreatButton;

    [Tooltip("Escuadra que se vigila para avisar de salud crítica.")]
    [SerializeField] private PartyManager party;

    [Tooltip("Fracción de vida por debajo de la cual se alerta al Maestro.")]
    [Range(0f, 1f)]
    [SerializeField] private float criticalHealthRatio = 0.25f;

    [Tooltip("Diámetro de cada decreto, según el mockup.")]
    [SerializeField] private float discSize = 96f;

    [Tooltip("Separación entre decretos.")]
    [SerializeField] private float discGap = 16f;

    // Cada decreto guarda las piezas que hay que repintar cada frame.
    private class Disco
    {
        public Image ring;
        public Image sweep;
        public Image alert;
        public TMP_Text name;
        public TMP_Text time;
    }

    private Disco heal;
    private Disco focus;
    private Disco regroup;
    private Disco retreat;

    [Tooltip("Margen mínimo contra el borde de la zona segura del dispositivo.")]
    [SerializeField] private float safeMargin = 16f;

    private TMP_Text synergyTag;
    private WaveManager waves;

    private RectTransform barRoot;
    private Vector2 barBasePosition;
    private bool barBaseCaptured;

    void Awake()
    {
        if (commander == null) commander = UnityEngine.Object.FindFirstObjectByType<MasterCommander>();
        if (party == null) party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();
        if (waves == null) waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();

        BuildChrome();
        ApplySafeArea();
    }

    // Monta los discos; se puede llamar desde el editor para que la escena coincida con el juego.
    public void BuildChrome()
    {
        heal = BuildDisc(healButton, 0, "✚");
        focus = BuildDisc(focusButton, 1, "◎");
        regroup = BuildDisc(regroupButton, 2, "◆");
        retreat = BuildDisc(retreatButton, 3, "←");

        BuildSynergyTag();
    }

    // Rótulo de sinergia sobre los discos; solo sale si la escuadra la tiene activa.
    private void BuildSynergyTag()
    {
        if (healButton == null) return;

        var barra = healButton.transform.parent;
        var previo = barra.Find("SynergyTag");
        var go = previo != null ? previo.gameObject
                                : new GameObject("SynergyTag", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(barra, false);

        synergyTag = go.GetComponent<TMP_Text>();

        var rt = synergyTag.rectTransform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(430f, 26f);
        rt.anchoredPosition = new Vector2(0f, discSize + 10f);

        synergyTag.fontSize = UITheme.SizeCaption;
        synergyTag.fontStyle = FontStyles.Bold;
        synergyTag.alignment = TextAlignmentOptions.Right;
        synergyTag.color = UITheme.Cyan;
        synergyTag.raycastTarget = false;
        go.SetActive(false);
    }

    // Convierte el botón rectangular de la escena en el disco del mockup.
    private Disco BuildDisc(Button button, int index, string icon)
    {
        if (button == null) return null;

        var rt = button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(discSize, discSize);
        rt.anchoredPosition = new Vector2(index * (discSize + discGap), 0f);

        var fondo = UITheme.Disk(button.gameObject, UITheme.Disc);
        button.targetGraphic = fondo;

        var disco = new Disco();

        // El barrido tapa el disco de arriba hacia la derecha mientras dura el enfriamiento.
        var previo = button.transform.Find("Sweep");
        var sweepGo = previo != null ? previo.gameObject
                                     : new GameObject("Sweep", typeof(RectTransform), typeof(Image));
        sweepGo.transform.SetParent(button.transform, false);
        UIBuild.Stretch(sweepGo.GetComponent<RectTransform>());

        disco.sweep = sweepGo.GetComponent<Image>();
        disco.sweep.sprite = UITheme.Circle;
        disco.sweep.type = Image.Type.Filled;
        disco.sweep.fillMethod = Image.FillMethod.Radial360;
        disco.sweep.fillOrigin = (int)Image.Origin360.Top;
        disco.sweep.color = UITheme.Hex("05060A", 0.82f);
        disco.sweep.raycastTarget = false;

        disco.ring = UITheme.DiskOutline(button.transform, "Ring", UITheme.Border);
        disco.alert = UITheme.DiskOutline(button.transform, "Alert", UITheme.Danger);
        disco.alert.gameObject.SetActive(false);

        // La etiqueta que venía de la escena pasa a ser el nombre; icono y cuenta atrás son nuevas.
        disco.name = LabelOf(button);
        Place(disco.name, 0f, 30f, UITheme.SizeMicro, FontStyles.Bold, UITheme.Text);

        var iconLabel = NewLabel(button.transform, "Icon");
        Place(iconLabel, 26f, 28f, 22f, FontStyles.Normal, UITheme.Text);
        iconLabel.text = icon;

        disco.time = NewLabel(button.transform, "Time");
        Place(disco.time, -30f, 16f, UITheme.SizeMicro, FontStyles.Bold, UITheme.TextSoft);

        return disco;
    }

    // La barra vive anclada a un borde (esquina inferior izquierda por defecto en el mockup),
    // así que UIManager.CenterInSafeArea la ignora: se recoloca aquí según a qué borde esté anclada.
    private void ApplySafeArea()
    {
        if (healButton == null) return;

        barRoot = healButton.transform.parent as RectTransform;
        if (barRoot == null) return;

        if (!barBaseCaptured)
        {
            barBasePosition = barRoot.anchoredPosition;
            barBaseCaptured = true;
        }

        var canvas = barRoot.GetComponentInParent<Canvas>();
        float escala = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        var segura = Screen.safeArea;

        float izquierda = segura.x / escala + safeMargin;
        float derecha = (Screen.width - segura.xMax) / escala + safeMargin;
        float abajo = segura.y / escala + safeMargin;
        float arriba = (Screen.height - segura.yMax) / escala + safeMargin;

        float dx = 0f, dy = 0f;
        if (barRoot.anchorMin.x <= 0.01f && barRoot.anchorMax.x <= 0.01f) dx = izquierda;
        else if (barRoot.anchorMin.x >= 0.99f && barRoot.anchorMax.x >= 0.99f) dx = -derecha;

        if (barRoot.anchorMin.y <= 0.01f && barRoot.anchorMax.y <= 0.01f) dy = abajo;
        else if (barRoot.anchorMin.y >= 0.99f && barRoot.anchorMax.y >= 0.99f) dy = -arriba;

        barRoot.anchoredPosition = barBasePosition + new Vector2(dx, dy);
    }

    // Se reutiliza la que ya hubiera: así montar dos veces no duplica etiquetas.
    private static TMP_Text NewLabel(Transform parent, string name)
    {
        var previo = parent.Find(name);
        var go = previo != null ? previo.gameObject
                                : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        return go.GetComponent<TMP_Text>();
    }

    // Centrada horizontalmente y colocada por su altura dentro del disco.
    private void Place(TMP_Text label, float y, float height, float size,
                       FontStyles style, Color color)
    {
        if (label == null) return;

        var rt = label.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(discSize - 12f, height);
        rt.anchoredPosition = new Vector2(0f, y);

        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
    }

    // En la base los decretos no pintan nada: solo salen con la escuadra en la arena.
    public bool ShouldShow => AnyDeployed();

    void Update()
    {
        if (commander == null) return;

        bool critico = AnyPartyMemberCritical();

        Refresh(healButton, heal, LocalizationManager.Get(critico ? "DEC_HEAL_ALERT" : "DEC_HEAL"),
            commander.HealReady, commander.HealCooldownLeft, commander.HealCooldown,
            critico ? UITheme.Danger : UITheme.DecreeHeal, critico);
        Refresh(focusButton, focus, LocalizationManager.Get("DEC_FOCUS"),
            commander.FocusFireReady, commander.FocusFireCooldownLeft, commander.FocusFireCooldown,
            UITheme.DecreeFocus, false);
        Refresh(regroupButton, regroup, LocalizationManager.Get("DEC_REGROUP"),
            commander.RegroupReady, commander.RegroupCooldownLeft, commander.RegroupCooldown,
            UITheme.DecreeRegroup, false);

        // La retirada no tiene enfriamiento: solo vale si hay alguien fuera peleando.
        Refresh(retreatButton, retreat, LocalizationManager.Get("DEC_RETREAT"), AnyDeployed(), 0f, 0f,
            UITheme.DecreeRetreat, false);

        RefreshSynergy();
    }

    // Sinergia de origen: se enseña solo cuando hay dos o más paisanos desplegados.
    private void RefreshSynergy()
    {
        if (synergyTag == null || waves == null) return;

        bool activa = waves.ActiveSynergyCount >= 2 && !string.IsNullOrEmpty(waves.ActiveSynergyOrigin);
        if (synergyTag.gameObject.activeSelf != activa) synergyTag.gameObject.SetActive(activa);
        if (!activa) return;

        synergyTag.text = $"{LocalizationManager.Get("UI_SYNERGY")}: {waves.ActiveSynergyOrigin} " +
                          $"· +{waves.OriginSynergyBonus:P0} ATK/DEF";
    }

    // Alguien de la escuadra por debajo del umbral: hay que avisar al Maestro.
    public bool AnyPartyMemberCritical()
    {
        if (party == null) return false;

        foreach (var hero in party.Party)
        {
            if (hero == null || hero.MaxHealth <= 0) continue;
            if (hero.CurrentHealth < hero.MaxHealth * criticalHealthRatio) return true;
        }

        return false;
    }

    private bool AnyDeployed()
    {
        if (party == null) return false;

        foreach (var hero in party.Party)
            if (hero != null && hero.IsDeployed) return true;

        return false;
    }

    // Enganchados desde el onClick de cada botón.
    public void OnHealPressed()
    {
        if (commander != null) commander.HealParty();
    }

    public void OnFocusPressed()
    {
        if (commander != null) commander.FocusFire();
    }

    public void OnRegroupPressed()
    {
        if (commander != null) commander.Regroup();
    }

    public void OnRetreatPressed()
    {
        if (commander != null) commander.Retreat();
    }

    // El aro lleva el color del decreto, el barrido la cuenta atrás y el aro rojo la alerta.
    private void Refresh(Button button, Disco disco, string text, bool ready,
                         float cooldownLeft, float cooldownTotal, Color activeColor, bool alerta)
    {
        if (button == null || disco == null) return;

        button.interactable = ready;

        bool enfriando = !ready && cooldownLeft > 0f && cooldownTotal > 0f;

        disco.ring.color = ready ? activeColor : UITheme.Dim;
        disco.sweep.fillAmount = enfriando ? Mathf.Clamp01(cooldownLeft / cooldownTotal) : 0f;

        if (disco.alert.gameObject.activeSelf != (alerta && ready))
            disco.alert.gameObject.SetActive(alerta && ready);

        if (disco.name != null) disco.name.text = text;
        if (disco.time != null) disco.time.text = enfriando ? $"{cooldownLeft:0.0}s" : string.Empty;
    }

    private static TMP_Text LabelOf(Button button)
        => button != null ? button.GetComponentInChildren<TMP_Text>() : null;
}
