using TMPro;
using UnityEngine;

// Textos flotantes de combate: se crean, suben, se desvanecen y se destruyen.
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
        => Show(worldPosition, "¡ESQUIVA!", new Color(0.6f, 0.85f, 1f));

    private void Spawn(Vector3 worldPosition, string text, Color color)
    {
        var go = new GameObject("DamageText", typeof(TextMeshPro));
        go.transform.SetParent(transform, false);
        go.transform.position = worldPosition
            + new Vector3(Random.Range(-horizontalJitter, horizontalJitter), verticalOffset, 0f);

        var tmp = go.GetComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 100;

        var floater = go.AddComponent<FloatingText>();
        floater.Initialize(lifetime, riseDistance);
    }
}

// Movimiento y desvanecido de un solo texto; se destruye al terminar.
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

        if (t >= 1f) Destroy(gameObject);
    }
}
