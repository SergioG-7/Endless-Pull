using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Sacude y parpadea en rojo un botón cuando el jugador lo toca sin fondos suficientes, en vez
// de no dar ninguna respuesta. Solo actúa si el Button ya está deshabilitado (coste no cubierto).
public class UnaffordableFeedback : MonoBehaviour, IPointerClickHandler
{
    private const float ShakeDuration = 0.28f;
    private const float ShakeMagnitude = 6f;

    private Button button;
    private RectTransform rt;
    private Vector2 basePos;
    private Coroutine running;

    // Añade el componente a un botón ya construido; no hace nada si ya lo tenía.
    public static void Attach(GameObject target)
    {
        if (target == null || target.GetComponent<UnaffordableFeedback>() != null) return;
        target.AddComponent<UnaffordableFeedback>();
    }

    void Awake()
    {
        button = GetComponent<Button>();
        rt = transform as RectTransform;
        if (rt != null) basePos = rt.anchoredPosition;
    }

    // Un Button no interactuable sigue recibiendo el raycast, solo ignora su propio onClick;
    // este manejador aparte sí se dispara y detecta justo ese caso.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (button == null || button.interactable || rt == null) return;

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Shake());
    }

    // Vaivén horizontal con caída lineal y el color del botón virando a rojo y volviendo;
    // usa tiempo real por si el juego está en pausa.
    private IEnumerator Shake()
    {
        Color baseColor = button.targetGraphic != null ? button.targetGraphic.color : Color.white;
        float t = 0f;

        while (t < ShakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / ShakeDuration);
            float falloff = 1f - progress;

            rt.anchoredPosition = basePos + new Vector2(Random.Range(-1f, 1f) * ShakeMagnitude * falloff, 0f);
            if (button.targetGraphic != null)
                button.targetGraphic.color = Color.Lerp(UITheme.DangerLight, baseColor, progress);

            yield return null;
        }

        rt.anchoredPosition = basePos;
        if (button.targetGraphic != null) button.targetGraphic.color = baseColor;
        running = null;
    }
}
