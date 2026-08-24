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

    [Tooltip("Color de alerta del botón de curar cuando alguien está crítico.")]
    [SerializeField] private Color alertColor = new Color(0.85f, 0.20f, 0.20f);

    [Tooltip("Color del botón cuando el decreto está listo.")]
    [SerializeField] private Color readyColor = new Color(0.25f, 0.45f, 0.65f);

    [Tooltip("Color del botón mientras el decreto se enfría.")]
    [SerializeField] private Color cooldownColor = new Color(0.30f, 0.30f, 0.34f);

    private TMP_Text healLabel;
    private TMP_Text focusLabel;
    private TMP_Text regroupLabel;
    private TMP_Text retreatLabel;

    void Awake()
    {
        if (commander == null) commander = UnityEngine.Object.FindFirstObjectByType<MasterCommander>();
        if (party == null) party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();

        healLabel = LabelOf(healButton);
        focusLabel = LabelOf(focusButton);
        regroupLabel = LabelOf(regroupButton);
        retreatLabel = LabelOf(retreatButton);
    }

    void Update()
    {
        if (commander == null) return;

        bool critico = AnyPartyMemberCritical();

        Refresh(healButton, healLabel, critico ? "¡CURAR ESCUADRA!" : "Curar Escuadra",
            commander.HealReady, commander.HealCooldownLeft, critico ? alertColor : readyColor);
        Refresh(focusButton, focusLabel, "Enfocar Objetivo",
            commander.FocusFireReady, commander.FocusFireCooldownLeft, readyColor);
        Refresh(regroupButton, regroupLabel, "Reagruparse",
            commander.RegroupReady, commander.RegroupCooldownLeft, readyColor);

        // La retirada no tiene enfriamiento: solo vale si hay alguien fuera peleando.
        Refresh(retreatButton, retreatLabel, "Retirada", AnyDeployed(), 0f, readyColor);
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

    // El botón se apaga y enseña los segundos que faltan; en móvil no hay otra pista.
    private void Refresh(Button button, TMP_Text label, string text,
                         bool ready, float cooldownLeft, Color activeColor)
    {
        if (button == null) return;

        button.interactable = ready;

        var image = button.GetComponent<Image>();
        if (image != null) image.color = ready ? activeColor : cooldownColor;

        if (label != null) label.text = ready || cooldownLeft <= 0f ? text : $"{text}  {cooldownLeft:0.0}s";
    }

    private static TMP_Text LabelOf(Button button)
        => button != null ? button.GetComponentInChildren<TMP_Text>() : null;
}
