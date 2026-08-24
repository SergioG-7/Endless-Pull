using TMPro;
using UnityEngine;

// Bocadillo flotante de los héroes en la base; lo que dicen depende de cómo estén.
public class SpeechBubble : MonoBehaviour
{
    [Tooltip("Segundos mínimos y máximos entre comentarios.")]
    [SerializeField] private Vector2 intervalRange = new Vector2(8f, 20f);

    [Tooltip("Segundos que se queda el bocadillo en pantalla.")]
    [SerializeField] private float showSeconds = 3f;

    [Tooltip("Altura del bocadillo sobre la cabeza del héroe.")]
    [SerializeField] private float height = 1.5f;

    [Tooltip("Tamaño de letra del bocadillo.")]
    [SerializeField] private float fontSize = 3.2f;

    private HeroController hero;
    private TextMeshPro label;
    private float nextTimer;
    private float hideTimer;

    void Awake()
    {
        hero = GetComponent<HeroController>();
        nextTimer = Random.Range(intervalRange.x, intervalRange.y);
    }

    void Update()
    {
        if (hero == null || hero.Data == null) return;

        if (hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f && label != null) label.gameObject.SetActive(false);
            return;
        }

        // En combate nadie está para charlas.
        if (hero.IsDeployed) return;

        nextTimer -= Time.deltaTime;
        if (nextTimer > 0f) return;

        nextTimer = Random.Range(intervalRange.x, intervalRange.y);
        Say(PickLine());
    }

    public void Say(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (label == null) Build();

        label.text = text;
        label.gameObject.SetActive(true);
        hideTimer = showSeconds;
    }

    // Lo que más le pesa al héroe manda sobre lo que dice.
    private string PickLine()
    {
        if (hero.IsExhausted)
            return Pick("Necesito sentarme...", "No puedo con mi alma.", "Un descanso, por favor.");

        if (hero.MoralePercent < 30)
            return Pick("¿De verdad vamos a volver ahí?", "Esto acabará mal.", "Ya no sé para qué peleo.");

        if (hero.CurrentHealth < hero.MaxHealth / 2)
            return Pick("Estas heridas escuecen.", "Aún puedo aguantar.", "Necesito vendas.");

        if (hero.MoralePercent > 80)
            return Pick("¡Hoy es un buen día!", "Que venga el siguiente piso.", "Nadie nos para.");

        switch (hero.Trait)
        {
            case HeroTrait.Glutton:
                return Pick("¿Queda algo en la cantina?", "Huele bien por ahí...", "Yo peleo mejor comido.");
            case HeroTrait.Slacker:
                return Pick("Cinco minutos más.", "¿Ya toca entrenar?", "Qué bien se está aquí.");
            case HeroTrait.Fierce:
                return Pick("Quiero pelea.", "¿A quién hay que partir?", "Estoy afilando el arma.");
        }

        return Pick("Un día tranquilo en la base.", "Todo en orden, Maestro.", "¿Alguna orden?");
    }

    private static string Pick(params string[] options) => options[Random.Range(0, options.Length)];

    private void Build()
    {
        var go = new GameObject("SpeechBubble", typeof(TextMeshPro));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, height, 0f);

        label = go.GetComponent<TextMeshPro>();
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 1f, 0.90f);
        label.sortingOrder = 20;

        // Sin ancho fijo el texto largo se sale del sprite del héroe.
        label.rectTransform.sizeDelta = new Vector2(6f, 1.6f);
        label.enableWordWrapping = true;
        go.SetActive(false);
    }
}
