using System.Collections.Generic;
using UnityEngine;

// Guardián de los paneles modales: solo puede haber uno abierto a la vez.
public class UIManager : MonoBehaviour
{
    [Tooltip("Paneles modales que se excluyen entre sí.")]
    [SerializeField] private List<GameObject> panels = new List<GameObject>();

    private static UIManager instance;

    void Awake() => instance = this;

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
    }

    public void CloseAll()
    {
        foreach (var p in panels)
            if (p != null) p.SetActive(false);
    }
}
