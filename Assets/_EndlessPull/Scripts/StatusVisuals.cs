using TMPro;
using UnityEngine;

// Feedback visual de los estados alterados; se añade solo junto al StatusEffectManager.
public class StatusVisuals : MonoBehaviour
{
    [Tooltip("Tinte del veneno.")]
    [SerializeField] private Color poisonTint = new Color(0.45f, 0.95f, 0.40f);

    [Tooltip("Tinte del sangrado.")]
    [SerializeField] private Color bleedTint = new Color(0.75f, 0.30f, 0.85f);

    [Tooltip("Parpadeos por segundo del tinte.")]
    [SerializeField] private float blinkSpeed = 4f;

    [Tooltip("Grados de balanceo al estar aturdido.")]
    [SerializeField] private float stunSwayAngle = 14f;

    [Tooltip("Velocidad del balanceo de aturdimiento.")]
    [SerializeField] private float stunSwaySpeed = 9f;

    [Tooltip("Color del aura del escudo.")]
    [SerializeField] private Color shieldTint = new Color(0.35f, 0.85f, 1f, 0.35f);

    private StatusEffectManager status;
    private SpriteRenderer body;
    private Color baseTint = Color.white;

    private SpriteRenderer aura;
    private TextMeshPro stunLabel;
    private static Sprite sharedRing;

    void Awake()
    {
        status = GetComponent<StatusEffectManager>();
        body = GetComponent<SpriteRenderer>();
        if (body != null) baseTint = body.color;
    }

    void LateUpdate()
    {
        if (status == null) return;

        TickTint();
        TickStun();
        TickShield();
    }

    // Veneno y sangrado parpadean sobre el color propio, sin pisarlo del todo.
    private void TickTint()
    {
        if (body == null) return;

        bool veneno = status.Has(StatusEffect.Poison);
        bool sangre = status.Has(StatusEffect.Bleed);

        if (!veneno && !sangre)
        {
            body.color = baseTint;
            return;
        }

        float pulso = (Mathf.Sin(Time.time * blinkSpeed * Mathf.PI) + 1f) * 0.5f;
        body.color = Color.Lerp(baseTint, veneno ? poisonTint : bleedTint, pulso * 0.75f);
    }

    private void TickStun()
    {
        bool aturdido = status.IsStunned;

        if (!aturdido)
        {
            if (stunLabel != null && stunLabel.gameObject.activeSelf) stunLabel.gameObject.SetActive(false);
            transform.rotation = Quaternion.identity;
            return;
        }

        // Balanceo: el bicho se tambalea mientras no puede actuar.
        float angulo = Mathf.Sin(Time.time * stunSwaySpeed) * stunSwayAngle;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo);

        if (stunLabel == null) BuildStunLabel();
        if (!stunLabel.gameObject.activeSelf) stunLabel.gameObject.SetActive(true);

        // El rótulo no se balancea con el cuerpo, o sería ilegible.
        stunLabel.transform.rotation = Quaternion.identity;
    }

    private void TickShield()
    {
        bool escudo = status.ShieldPoints > 0;

        if (!escudo)
        {
            if (aura != null && aura.gameObject.activeSelf) aura.gameObject.SetActive(false);
            return;
        }

        if (aura == null) BuildAura();
        if (!aura.gameObject.activeSelf) aura.gameObject.SetActive(true);

        // Late un poco para que se distinga de una marca fija del suelo.
        float pulso = 0.85f + Mathf.Sin(Time.time * 3f) * 0.1f;
        aura.transform.localScale = Vector3.one * (1.9f * pulso);
    }

    private void BuildStunLabel()
    {
        var go = new GameObject("StunLabel", typeof(TextMeshPro));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 1.1f, 0f);

        stunLabel = go.GetComponent<TextMeshPro>();
        stunLabel.text = "¡ATURDIDO!";
        stunLabel.fontSize = 3.4f;
        stunLabel.alignment = TextAlignmentOptions.Center;
        stunLabel.color = new Color(1f, 0.85f, 0.30f);
        stunLabel.sortingOrder = 40;
        stunLabel.rectTransform.sizeDelta = new Vector2(6f, 1.2f);
    }

    private void BuildAura()
    {
        var go = new GameObject("ShieldAura", typeof(SpriteRenderer));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;

        aura = go.GetComponent<SpriteRenderer>();
        aura.sprite = Ring();
        aura.color = shieldTint;

        // Por detrás de la unidad: rodea sin taparla.
        aura.sortingOrder = -10;
    }

    // Anillo generado una vez y compartido; el proyecto no trae sprite circular.
    private static Sprite Ring()
    {
        if (sharedRing != null) return sharedRing;

        const int size = 96;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radio = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radio, radio));
                float alpha = d > radio ? 0f : d > radio - 5f ? 1f : 0.30f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        sharedRing = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return sharedRing;
    }
}
