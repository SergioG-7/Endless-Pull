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
    [SerializeField] private float fontSize = 1.1f;

    [Tooltip("Ancho y alto de la píldora del bocadillo, en unidades de mundo.")]
    [SerializeField] private Vector2 bubbleSize = new Vector2(4.4f, 0.85f);

    [Tooltip("Comida por debajo de la cual los héroes empiezan a quejarse.")]
    [SerializeField] private int lowFoodThreshold = 120;

    private HeroController hero;
    private EconomyManager economy;
    private TextMeshPro label;
    private SpriteRenderer backdrop;
    private float nextTimer;
    private float hideTimer;

    void Awake()
    {
        hero = GetComponent<HeroController>();
        economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
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

    // Lo que más le pesa al héroe manda sobre lo que dice; todo sale del diccionario.
    private string PickLine()
    {
        // La despensa vacía preocupa a toda la base, no solo al que come mucho.
        if (economy != null && economy.Food < lowFoodThreshold)
            return Pick("SAY_HUNGRY_1", "SAY_HUNGRY_2");

        if (hero.IsExhausted)
            return Pick("SAY_TIRED_1", "SAY_TIRED_2");

        if (hero.MoralePercent < 30)
            return Pick("SAY_LOWMORALE_1", "SAY_LOWMORALE_2");

        if (hero.CurrentHealth < hero.MaxHealth / 2)
            return Pick("SAY_HURT_1", "SAY_HURT_2");

        if (hero.HasBrokenGear)
            return Pick("SAY_BROKEN_GEAR_1", "SAY_BROKEN_GEAR_2");

        if (hero.MoralePercent > 80)
            return Pick("SAY_HAPPY_1", "SAY_HAPPY_2");

        switch (hero.Trait)
        {
            case HeroTrait.Glutton: return Pick("SAY_GLUTTON");
            case HeroTrait.Slacker: return Pick("SAY_SLACKER");
            case HeroTrait.Fierce: return Pick("SAY_FIERCE");
        }

        return Pick("SAY_IDLE_1", "SAY_IDLE_2");
    }

    private static string Pick(params string[] keys)
        => LocalizationManager.Get(keys[Random.Range(0, keys.Length)]);

    private void Build()
    {
        var go = new GameObject("SpeechBubble", typeof(TextMeshPro));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, height, 0f);

        // Fondo del bocadillo: sin él el texto se pierde sobre el suelo oscuro.
        var fondoGo = new GameObject("Backdrop", typeof(SpriteRenderer));
        fondoGo.transform.SetParent(go.transform, false);
        fondoGo.transform.localPosition = Vector3.zero;

        backdrop = fondoGo.GetComponent<SpriteRenderer>();
        backdrop.sprite = Resources.Load<Sprite>("UI/UI_Rounded");
        backdrop.drawMode = SpriteDrawMode.Sliced;
        backdrop.size = new Vector2(bubbleSize.x, bubbleSize.y);
        backdrop.color = UITheme.Hex("141A26", 0.92f);
        backdrop.sortingOrder = 19;

        var marcoGo = new GameObject("Border", typeof(SpriteRenderer));
        marcoGo.transform.SetParent(go.transform, false);
        marcoGo.transform.localPosition = Vector3.zero;

        var marco = marcoGo.GetComponent<SpriteRenderer>();
        marco.sprite = Resources.Load<Sprite>("UI/UI_RoundedRingThin");
        marco.drawMode = SpriteDrawMode.Sliced;
        marco.size = new Vector2(bubbleSize.x, bubbleSize.y);
        marco.color = UITheme.Border;
        marco.sortingOrder = 20;

        label = go.GetComponent<TextMeshPro>();
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = UITheme.Text;
        label.sortingOrder = 21;

        // Sin ancho fijo el texto largo se sale del sprite del héroe.
        label.rectTransform.sizeDelta = new Vector2(bubbleSize.x - 0.3f, bubbleSize.y);
        go.SetActive(false);
    }
}
