using UnityEngine;

// Punto de la base donde aterrizan los héroes recién invocados.
public class SummonAltar : MonoBehaviour
{
    [Tooltip("Desplazamiento sobre el altar al que sale el héroe.")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, -1.2f);

    [Tooltip("Dispersión para que dos invocaciones seguidas no se solapen.")]
    [SerializeField] private Vector2 spawnJitter = new Vector2(0.8f, 0.4f);

    [Tooltip("Nombre del hijo que lleva la ilustración del altar.")]
    [SerializeField] private string artChildName = "Altar_Art";

    [Tooltip("Hueco entre el pie de la ilustración y el rótulo del nombre.")]
    [SerializeField] private float nameLabelGap = 0.35f;

    [Tooltip("Alto de la caja del rótulo del nombre.")]
    [SerializeField] private float nameLabelHeight = 2.2f;

    private static SummonAltar instance;

    public static bool Exists => instance != null;

    void OnEnable()
    {
        instance = this;
        PlaceNameLabel();
    }

    // El altar no es un BaseBuilding, así que no pasa por su PlaceNameLabel: su rótulo se quedó
    // encima de la ilustración mientras el resto de la base lo lleva debajo. Mismo criterio aquí.
    private void PlaceNameLabel()
    {
        var arte = transform.Find(artChildName);
        if (arte == null) return;

        var sr = arte.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        float alto = sr.sprite.bounds.size.y * arte.localScale.y;
        float ancho = sr.sprite.bounds.size.x * arte.localScale.x;

        foreach (var texto in GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            var rt = texto.GetComponent<RectTransform>();
            if (rt == null) continue;

            rt.sizeDelta = new Vector2(ancho, nameLabelHeight);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -alto * 0.5f - nameLabelGap);
            texto.fontStyle |= TMPro.FontStyles.Bold;
        }
    }

    void OnDisable()
    {
        if (instance == this) instance = null;
    }

    public Vector2 SpawnPoint
        => (Vector2)transform.position + spawnOffset + new Vector2(
            Random.Range(-spawnJitter.x, spawnJitter.x),
            Random.Range(-spawnJitter.y, spawnJitter.y));

    // La usa el gacha; si no hay altar en la escena devuelve false y se usa el punto de siempre.
    public static bool TryGetSpawnPoint(out Vector2 point)
    {
        if (instance == null)
        {
            point = Vector2.zero;
            return false;
        }

        point = instance.SpawnPoint;
        return true;
    }

    public static Vector3 AltarPosition
        => instance != null ? instance.transform.position : Vector3.zero;
}
