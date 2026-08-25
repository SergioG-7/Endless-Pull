using TMPro;
using UnityEngine;

// Rótulo que se repinta solo al cambiar de idioma; vale para UI y para texto en el mundo.
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Clave del diccionario de LocalizationManager.")]
    [SerializeField] private string key;

    [Tooltip("Pasa el texto a mayúsculas; lo usan las cabeceras de las tarjetas.")]
    [SerializeField] private bool upperCase;

    private TMP_Text label;

    public string Key
    {
        get => key;
        set { key = value; Refresh(); }
    }

    void Awake() => label = GetComponent<TMP_Text>();

    void OnEnable()
    {
        LocalizationManager.LanguageChanged += Refresh;
        Refresh();
    }

    void OnDisable() => LocalizationManager.LanguageChanged -= Refresh;

    public void Refresh()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        if (label == null || string.IsNullOrEmpty(key)) return;

        string texto = LocalizationManager.Get(key);
        label.text = upperCase ? texto.ToUpperInvariant() : texto;
    }
}
