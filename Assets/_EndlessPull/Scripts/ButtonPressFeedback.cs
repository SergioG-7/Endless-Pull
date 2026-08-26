using UnityEngine;
using UnityEngine.EventSystems;

// Micro-escala al pulsar (0.95x) para que cualquier botón se sienta táctil, en PC y en móvil.
// El sonido de clic ya lo dispara UIBuild.Button; este componente solo añade la deformación visual.
public class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private const float PressedScale = 0.95f;

    private RectTransform rt;
    private Vector3 baseScale;
    private bool pressed;

    // Añade el componente a un botón ya construido; no hace nada si ya lo tenía.
    public static void Attach(GameObject target)
    {
        if (target == null || target.GetComponent<ButtonPressFeedback>() != null) return;
        target.AddComponent<ButtonPressFeedback>();
    }

    void Awake()
    {
        rt = transform as RectTransform;
        baseScale = rt != null ? rt.localScale : Vector3.one;
    }

    public void OnPointerDown(PointerEventData eventData) => SetPressed(true);
    public void OnPointerUp(PointerEventData eventData) => SetPressed(false);
    public void OnPointerExit(PointerEventData eventData) => SetPressed(false);

    private void SetPressed(bool value)
    {
        if (pressed == value || rt == null) return;

        pressed = value;
        rt.localScale = value ? baseScale * PressedScale : baseScale;
    }
}
