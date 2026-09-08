using System.Collections.Generic;
using UnityEngine;

// Anima los 36 recortes de una hoja LPC de marcha (9 columnas x 4 direcciones) sin
// Animator ni AnimationClip: lee el movimiento del transform y cambia el sprite.
// Fila 0 arriba, 1 izquierda, 2 abajo, 3 derecha; la columna 0 es el reposo.
public class LPCAnimator : MonoBehaviour
{
    [Tooltip("Renderer al que se le cambia el sprite; vacío coge el del propio objeto.")]
    [SerializeField] private SpriteRenderer target;

    [Tooltip("Velocidad a partir de la cual se considera que el héroe camina.")]
    [SerializeField] private float walkThreshold = 0.05f;

    [Tooltip("Fotogramas por segundo del ciclo de marcha.")]
    [Range(4f, 20f)]
    [SerializeField] private float framesPerSecond = 9f;

    [Tooltip("Margen antes de dar por parado al héroe; evita el tirón de la separación.")]
    [SerializeField] private float idleGrace = 0.1f;

    [Tooltip("Distancia que avanza la arremetida visual al golpear cuerpo a cuerpo.")]
    [SerializeField] private float lungeDistance = 0.15f;

    [Tooltip("Segundos que dura la arremetida completa: retroceso, golpe y vuelta.")]
    [SerializeField] private float lungeDuration = 0.15f;

    [Tooltip("Retroceso previo al golpe, como fracción de la distancia de arremetida.")]
    [Range(0f, 1f)]
    [SerializeField] private float windUpFraction = 0.35f;

    private Coroutine lungeRoutine;

    public const int Columnas = 9;
    public const int Filas = 4;

    public const int Arriba = 0;
    public const int Izquierda = 1;
    public const int Abajo = 2;
    public const int Derecha = 3;

    // Los 36 recortes en orden de fila y columna; los pone el HeroData del héroe.
    private Sprite[] frames;

    private Vector3 lastPosition;
    private float frameTimer;
    private float idleTimer;
    private int column;
    private int row = Abajo;

    public bool IsWalking { get; private set; }
    public int Row => row;
    public int Column => column;
    public bool HasFrames => frames != null && frames.Length == Columnas * Filas;

    void Awake()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
        lastPosition = transform.position;
    }

    // La llama el HeroController al aplicar el sprite del héroe. Con una hoja
    // incompleta se queda sin animar en vez de enseñar huecos.
    public void SetFrames(Sprite[] hoja)
    {
        frames = hoja != null && hoja.Length == Columnas * Filas ? hoja : null;

        if (!HasFrames) return;

        row = Abajo;
        column = 0;
        Apply();
    }

    void LateUpdate()
    {
        if (!HasFrames) return;

        // Con el juego pausado (el menú principal pone timeScale a 0) deltaTime es 0:
        // dividir por él daba NaN y dejaba al héroe congelado a medio paso.
        if (Time.deltaTime <= 0f) return;

        Vector3 ahora = transform.position;
        Vector2 delta = ahora - lastPosition;
        lastPosition = ahora;

        float velocidad = delta.magnitude / Time.deltaTime;

        if (velocidad > walkThreshold)
        {
            idleTimer = idleGrace;
            IsWalking = true;
            row = DirectionOf(delta);

            // Ciclo de marcha: columnas 1 a 8; la 0 se reserva para el reposo.
            frameTimer += Time.deltaTime * framesPerSecond;
            while (frameTimer >= 1f)
            {
                frameTimer -= 1f;
                column = column < 1 ? 1 : (column % (Columnas - 1)) + 1;
            }
        }
        else
        {
            // Un respiro antes de parar: si no, el empuje de separación corta el paso.
            idleTimer -= Time.deltaTime;
            if (idleTimer > 0f) return;

            IsWalking = false;
            column = 0;
            frameTimer = 0f;
        }

        Apply();
    }

    private static readonly Dictionary<Texture2D, Sprite[]> sliceCache = new Dictionary<Texture2D, Sprite[]>();

    // Recorta una hoja LPC de 9x4 en 36 sprites. Los héroes traen sus recortes horneados a
    // mano, pero los enemigos comparten hoja por tipo: se recorta una vez por textura y el
    // resto de instancias del mismo tipo reusan el resultado.
    public static Sprite[] SliceWalkSheet(Texture2D sheet)
    {
        if (sheet == null) return null;

        // Un recorte cacheado puede tener los Sprite ya destruidos aunque la textura clave
        // siga viva; comprobar el primer elemento antes de fiarse del acierto de caché.
        if (sliceCache.TryGetValue(sheet, out var cached) && cached != null && cached.Length > 0 && cached[0] != null)
            return cached;

        int frameWidth = sheet.width / Columnas;
        int frameHeight = sheet.height / Filas;
        var frames = new Sprite[Columnas * Filas];

        for (int fila = 0; fila < Filas; fila++)
        {
            // Fila 0 (arriba) es la primera del PNG, que en coordenadas UV queda arriba del todo.
            float y = (Filas - 1 - fila) * frameHeight;

            for (int columna = 0; columna < Columnas; columna++)
            {
                var rect = new Rect(columna * frameWidth, y, frameWidth, frameHeight);
                frames[fila * Columnas + columna] = Sprite.Create(sheet, rect, new Vector2(0.5f, 0.22f), 64f);
            }
        }

        sliceCache[sheet] = frames;
        return frames;
    }

    // La componente mayor manda: así una diagonal elige el lateral, que se lee mejor.
    private static int DirectionOf(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            return delta.x >= 0f ? Derecha : Izquierda;

        return delta.y >= 0f ? Arriba : Abajo;
    }

    private void Apply()
    {
        if (target == null) return;

        var sprite = frames[row * Columnas + Mathf.Clamp(column, 0, Columnas - 1)];
        if (sprite != null) target.sprite = sprite;
    }

    // Golpe cuerpo a cuerpo: un empujón corto hacia el objetivo y vuelta, sin tocar la colisión real.
    public void PlayAttackLunge(Vector2 hacia)
    {
        if (lungeRoutine != null) StopCoroutine(lungeRoutine);
        lungeRoutine = StartCoroutine(LungeRoutine(hacia));
    }

    private System.Collections.IEnumerator LungeRoutine(Vector2 hacia)
    {
        Vector3 origen = transform.position;
        Vector2 direccion = hacia - (Vector2)origen;
        if (direccion.sqrMagnitude > 0.0001f) direccion.Normalize();

        Vector3 destino = origen + (Vector3)(direccion * lungeDistance);
        Vector3 retroceso = origen - (Vector3)(direccion * (lungeDistance * windUpFraction));

        // El reparto suma 1: la duración total del golpe no cambia, solo cómo se gasta. El
        // retroceso se toma con calma, el golpe sale seco y la vuelta se relaja.
        float total = Mathf.Max(0.03f, lungeDuration);

        yield return Move(origen, retroceso, total * 0.35f);
        yield return Move(retroceso, destino, total * 0.20f);
        yield return Move(destino, origen, total * 0.45f);

        transform.position = origen;
        lungeRoutine = null;
    }

    private System.Collections.IEnumerator Move(Vector3 desde, Vector3 hasta, float duracion)
    {
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(desde, hasta, t / duracion);
            yield return null;
        }

        transform.position = hasta;
    }
}
