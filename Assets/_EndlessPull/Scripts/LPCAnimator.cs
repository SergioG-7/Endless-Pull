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
}
