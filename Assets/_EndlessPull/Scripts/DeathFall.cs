using System.Collections.Generic;
using UnityEngine;

// Cadáver visual del que acaba de caer: copia su sprite, se desploma de lado y se apaga.
// Va suelto y sin lógica de unidad, para que nada del combate lo confunda con alguien vivo.
// El del enemigo se desvanece del todo; el del héroe se queda tumbado hasta que acabe el piso.
public class DeathFall : MonoBehaviour
{
    // Unidades que se hunde el cuerpo mientras cae.
    private const float SinkSpeed = 0.35f;

    // Cadáveres que siguen en el suelo; los limpia el WaveManager al cerrar el piso.
    private static readonly List<DeathFall> lingering = new List<DeathFall>();

    // Cuerpo que se desvanece del todo: el del enemigo.
    public static void Spawn(SpriteRenderer source, float duration, float tilt)
        => Create(source, duration, tilt, 0f);

    // Cuerpo que se queda en el sitio, apagado, hasta que termine el piso: el del héroe.
    public static void SpawnLingering(SpriteRenderer source, float duration, float tilt, float restAlpha)
    {
        var fall = Create(source, duration, tilt, Mathf.Clamp01(restAlpha));
        if (fall != null) lingering.Add(fall);
    }

    // Se acabó el piso, gane, pierda o se retire: el campo se queda limpio para el siguiente.
    public static void ClearLingering()
    {
        foreach (var fall in lingering)
            if (fall != null) Destroy(fall.gameObject);

        lingering.Clear();
    }

    private static DeathFall Create(SpriteRenderer source, float duration, float tilt, float restAlpha)
    {
        if (source == null || source.sprite == null) return null;

        var go = new GameObject("DeathFall");
        go.transform.position = source.transform.position;
        go.transform.localScale = source.transform.lossyScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = source.sprite;
        sr.color = source.color;
        sr.flipX = source.flipX;

        // Sin copiar la capa se iría a Default, que queda detrás del fondo.
        sr.sortingLayerID = source.sortingLayerID;

        // Por debajo de los vivos: un cuerpo tirado no puede tapar a quien sigue peleando.
        sr.sortingOrder = source.sortingOrder - 1;

        var fall = go.AddComponent<DeathFall>();
        fall.body = sr;
        fall.duration = Mathf.Max(0.05f, duration);
        fall.tilt = tilt;
        fall.restAlpha = restAlpha;
        fall.baseColor = sr.color;
        return fall;
    }

    private SpriteRenderer body;
    private Color baseColor;
    private float duration;
    private float tilt;
    private float restAlpha;
    private float timer;
    private bool settled;

    void OnDestroy() => lingering.Remove(this);

    void Update()
    {
        if (settled) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        transform.rotation = Quaternion.Euler(0f, 0f, tilt * Mathf.SmoothStep(0f, 1f, t));
        transform.position += Vector3.down * (SinkSpeed * Time.deltaTime);

        // Siempre desde el color de partida: leer el del frame anterior lo iba oscureciendo
        // sin parar hasta dejarlo negro.
        float gris = restAlpha > 0f ? Mathf.Lerp(1f, 0.45f, t) : 1f;
        body.color = new Color(
            baseColor.r * gris, baseColor.g * gris, baseColor.b * gris,
            Mathf.Lerp(baseColor.a, restAlpha, t * t));

        if (t < 1f) return;

        // Con restAlpha a cero desaparece; si no, se queda tumbado donde cayó.
        if (restAlpha <= 0f) Destroy(gameObject);
        else settled = true;
    }
}
