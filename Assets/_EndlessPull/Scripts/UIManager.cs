using System.Collections.Generic;
using UnityEngine;

// Guardián de los paneles modales: solo puede haber uno abierto a la vez.
public class UIManager : MonoBehaviour
{
    [Tooltip("Paneles modales que se excluyen entre sí.")]
    [SerializeField] private List<GameObject> panels = new List<GameObject>();

    [Tooltip("Fondo oscuro a pantalla completa que bloquea los clics de detrás.")]
    [SerializeField] private GameObject backdrop;

    [Tooltip("Barra de decretos, que se esconde mientras haya un modal abierto.")]
    [SerializeField] private GameObject actionBar;

    [Tooltip("Lógica de la barra: decide además si toca enseñarla o estamos en la base.")]
    [SerializeField] private MasterActionBar actionBarLogic;

    [Tooltip("Margen que se deja libre entre el modal y el borde de la zona segura.")]
    [SerializeField] private float safeMargin = 12f;

    private static UIManager instance;

    void Awake()
    {
        instance = this;
        if (actionBarLogic == null) actionBarLogic = UnityEngine.Object.FindFirstObjectByType<MasterActionBar>();
    }

    // La barra sale solo si no hay modal delante y además hay escuadra desplegada.
    private bool ShouldShowActionBar(bool modalAbierto)
        => !modalAbierto && (actionBarLogic == null || actionBarLogic.ShouldShow);

    // Los paneles se cierran solos, así que la barra y el fondo tienen que seguirlos.
    // Recuerda si el frame anterior habia algun modal, para saber cuando se cierra el ultimo.
    private bool habiaModal;

    void Update()
    {
        bool abierto = false;
        foreach (var p in panels)
            if (p != null && p.activeSelf) { abierto = true; break; }

        // Los modales se cierran solos desde su propio Close: el sonido se detecta aqui.
        if (abierto != habiaModal)
        {
            AudioManager.Play(abierto ? SfxId.UiOpen : SfxId.UiClose);
            habiaModal = abierto;
        }

        if (backdrop != null && backdrop.activeSelf != abierto) backdrop.SetActive(abierto);

        bool mostrarBarra = ShouldShowActionBar(abierto);
        if (actionBar != null && actionBar.activeSelf != mostrarBarra) actionBar.SetActive(mostrarBarra);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Start()
    {
        CloseAll();
    }

    // Abre uno y cierra el resto; los paneles la llaman desde su propio Open.
    public static void OpenExclusive(GameObject panel)
    {
        if (instance != null) instance.ShowOnly(panel);
        else if (panel != null) panel.SetActive(true);
    }

    public static void CloseEverything()
    {
        if (instance != null) instance.CloseAll();
    }

    public static bool IsAnyOpen()
    {
        if (instance == null) return false;

        foreach (var p in instance.panels)
            if (p != null && p.activeSelf) return true;

        return false;
    }

    // Registra un panel montado en tiempo de ejecución.
    public void Register(GameObject panel)
    {
        if (panel != null && !panels.Contains(panel)) panels.Add(panel);
    }

    private void ShowOnly(GameObject panel)
    {
        Register(panel);

        foreach (var p in panels)
        {
            if (p == null) continue;
            p.SetActive(p == panel);
        }

        // El fondo primero y el panel después: así el panel queda por encima del oscurecido.
        if (backdrop != null)
        {
            backdrop.SetActive(true);
            backdrop.transform.SetAsLastSibling();
        }

        if (panel != null)
        {
            panel.transform.SetAsLastSibling();
            CenterInSafeArea(panel);
        }

        if (actionBar != null) actionBar.SetActive(false);
    }

    // Con notch o barra de gestos el centro de la pantalla no es el centro visible: el modal
    // se recoloca en el centro de la zona segura para que no le recorten los botones.
    private void CenterInSafeArea(GameObject panel)
    {
        var rt = panel.transform as RectTransform;
        var canvas = panel.GetComponentInParent<Canvas>();
        if (rt == null || canvas == null) return;

        // Solo tiene sentido en paneles centrados; los anclados a un borde se dejan como están.
        if (rt.anchorMin != rt.anchorMax) return;

        float escala = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        var segura = Screen.safeArea;

        float dx = (segura.center.x - Screen.width * 0.5f) / escala;
        float dy = (segura.center.y - Screen.height * 0.5f) / escala;

        // Si aun así el panel se sale, se arrima al borde seguro en vez de quedarse cortado.
        float mitadAncho = rt.sizeDelta.x * 0.5f;
        float mitadAlto = rt.sizeDelta.y * 0.5f;
        float libreX = Mathf.Max(0f, segura.width / escala * 0.5f - mitadAncho - safeMargin);
        float libreY = Mathf.Max(0f, segura.height / escala * 0.5f - mitadAlto - safeMargin);

        rt.anchoredPosition = new Vector2(
            Mathf.Clamp(dx, -libreX, libreX),
            Mathf.Clamp(dy, -libreY, libreY));
    }

    public void CloseAll()
    {
        foreach (var p in panels)
            if (p != null) p.SetActive(false);

        if (backdrop != null) backdrop.SetActive(false);
        if (actionBar != null) actionBar.SetActive(ShouldShowActionBar(false));
    }
}
