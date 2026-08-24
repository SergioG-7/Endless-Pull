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

    [Tooltip("Color del botón cuando el decreto está listo.")]
    [SerializeField] private Color readyColor = new Color(0.25f, 0.45f, 0.65f);

    [Tooltip("Color del botón mientras el decreto se enfría.")]
    [SerializeField] private Color cooldownColor = new Color(0.30f, 0.30f, 0.34f);

    private TMP_Text healLabel;
    private TMP_Text focusLabel;
    private TMP_Text regroupLabel;

    void Awake()
    {
        if (commander == null) commander = UnityEngine.Object.FindFirstObjectByType<MasterCommander>();

        healLabel = LabelOf(healButton);
        focusLabel = LabelOf(focusButton);
        regroupLabel = LabelOf(regroupButton);
    }

    void Update()
    {
        if (commander == null) return;

        Refresh(healButton, healLabel, "Curar Escuadra",
            commander.HealReady, commander.HealCooldownLeft);
        Refresh(focusButton, focusLabel, "Enfocar Objetivo",
            commander.FocusFireReady, commander.FocusFireCooldownLeft);
        Refresh(regroupButton, regroupLabel, "Reagruparse",
            commander.RegroupReady, commander.RegroupCooldownLeft);
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

    // El botón se apaga y enseña los segundos que faltan; en móvil no hay otra pista.
    private void Refresh(Button button, TMP_Text label, string text, bool ready, float cooldownLeft)
    {
        if (button == null) return;

        button.interactable = ready;

        var image = button.GetComponent<Image>();
        if (image != null) image.color = ready ? readyColor : cooldownColor;

        if (label != null) label.text = ready ? text : $"{text}  {cooldownLeft:0.0}s";
    }

    private static TMP_Text LabelOf(Button button)
        => button != null ? button.GetComponentInChildren<TMP_Text>() : null;
}
