using System.Collections;
using UnityEngine;

// Entrada con rebote suave para paneles modales; se dispara solo con OnEnable, sin tocar cada Open().
// Usa tiempo real: el hitstop de combate no debe congelar la interfaz mientras se abre un modal.
[RequireComponent(typeof(RectTransform))]
public class UIModalAnimator : MonoBehaviour
{
    [Tooltip("Segundos que dura la entrada del panel.")]
    [SerializeField] private float duration = 0.15f;

    [Tooltip("Escala de partida antes de asentarse en 1.")]
    [SerializeField] private float startScale = 0.85f;

    private RectTransform rt;
    private CanvasGroup group;
    private Coroutine routine;

    void Awake()
    {
        rt = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PunchIn());
    }

    private IEnumerator PunchIn()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));

            rt.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, p);
            group.alpha = p;

            yield return null;
        }

        rt.localScale = Vector3.one;
        group.alpha = 1f;
        routine = null;
    }
}
