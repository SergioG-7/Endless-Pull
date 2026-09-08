using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Textos flotantes de combate: se reutilizan mediante una reserva (mismo patrón que AudioManager).
public class DamageTextManager : MonoBehaviour
{
    [Tooltip("Segundos que dura cada texto en pantalla.")]
    [SerializeField] private float lifetime = 0.8f;

    [Tooltip("Unidades que sube el texto durante su vida.")]
    [SerializeField] private float riseDistance = 1.2f;

    [Tooltip("Tamaño de fuente del texto flotante.")]
    [SerializeField] private float fontSize = 4f;

    [Tooltip("Dispersión horizontal para que dos golpes seguidos no se solapen.")]
    [SerializeField] private float horizontalJitter = 0.35f;

    [Tooltip("Altura sobre el objetivo a la que aparece el texto.")]
    [SerializeField] private float verticalOffset = 0.9f;

    private static DamageTextManager instance;

    // Reserva de textos flotantes; crece bajo demanda y nunca se destruye (evita GC churn).
    private readonly List<FloatingText> pool = new List<FloatingText>();
    private int nextPoolIndex;

    void Awake() => instance = this;

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Punto de entrada estático: los sistemas de combate no necesitan referencia.
    public static void Show(Vector3 worldPosition, string text, Color color)
    {
        if (instance != null) instance.Spawn(worldPosition, text, color);
    }

    public static void ShowDamage(Vector3 worldPosition, int amount)
        => Show(worldPosition, amount.ToString(), new Color(1f, 0.85f, 0.3f));

    public static void ShowHeal(Vector3 worldPosition, int amount)
        => Show(worldPosition, "+" + amount, new Color(0.4f, 0.95f, 0.5f));

    public static void ShowDodge(Vector3 worldPosition)
        => Show(worldPosition, LocalizationManager.Get("FX_DODGE"), new Color(0.6f, 0.85f, 1f));

    // Por encima de héroes y enemigos, que se ordenan por Y con valores de unos pocos miles.
    private const int OverlayOrder = 20000;

    private void Spawn(Vector3 worldPosition, string text, Color color)
    {
        var floater = GetFromPool();
        var go = floater.gameObject;
        go.transform.position = worldPosition
            + new Vector3(Random.Range(-horizontalJitter, horizontalJitter), verticalOffset, 0f);
        go.SetActive(true);

        var tmp = go.GetComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;

        // La capa manda sobre el orden: en "Default" el texto caía por DEBAJO de los fondos, que
        // van en "Environment", y no se veía ni el daño ni el nombre de la habilidad.
        tmp.sortingLayerID = SortingLayer.NameToID("Characters");
        tmp.sortingOrder = OverlayOrder;

        floater.Initialize(lifetime, riseDistance);
    }

    // Busca un texto libre en la reserva; si todos están en uso crea uno nuevo y lo añade.
    private FloatingText GetFromPool()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            int idx = (nextPoolIndex + i) % pool.Count;
            var candidato = pool[idx];
            if (candidato != null && !candidato.gameObject.activeSelf)
            {
                nextPoolIndex = (idx + 1) % pool.Count;
                return candidato;
            }
        }

        var go = new GameObject("DamageText", typeof(TextMeshPro));
        go.transform.SetParent(transform, false);

        var floater = go.AddComponent<FloatingText>();
        pool.Add(floater);
        return floater;
    }
}

// Movimiento y desvanecido de un solo texto; al terminar se desactiva y vuelve a la reserva.
public class FloatingText : MonoBehaviour
{
    private TextMeshPro label;
    private Vector3 origin;
    private float duration = 0.8f;
    private float rise = 1.2f;
    private float elapsed;

    public void Initialize(float lifetime, float riseDistance)
    {
        label = GetComponent<TextMeshPro>();
        origin = transform.position;
        duration = Mathf.Max(0.05f, lifetime);
        rise = riseDistance;
        elapsed = 0f; // reinicio necesario: la instancia se reutiliza desde la reserva.
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        transform.position = origin + new Vector3(0f, rise * t, 0f);

        if (label != null)
        {
            var c = label.color;
            c.a = 1f - t;
            label.color = c;
        }

        if (t >= 1f) gameObject.SetActive(false);
    }
}
