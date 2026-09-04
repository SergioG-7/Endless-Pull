using System.Globalization;
using UnityEngine;

// La base sigue viva con el juego cerrado: al cargar se acredita de golpe lo que las granjas
// habrían cosechado, lo que habrían descansado los trabajadores y el avance de la expedición.
public class OfflineProgressManager : MonoBehaviour
{
    [Tooltip("Tope de horas de ausencia que se acreditan de una vez.")]
    [SerializeField] private float maxOfflineHours = 8f;

    [Tooltip("Rendimiento de la base sin nadie mirando, como fracción del ritmo en juego activo.")]
    [Range(0f, 1f)]
    [SerializeField] private float offlineEfficiency = 0.1f;

    [Tooltip("Tope duro de comida que puede traer una sola vuelta, pase el tiempo que pase.")]
    [SerializeField] private int maxOfflineFood = 800;

    [Tooltip("Ausencia mínima, en segundos, para acreditar nada; por debajo se ignora.")]
    [SerializeField] private float minOfflineSeconds = 60f;

    [Tooltip("Segundos que se enseña el informe de vuelta.")]
    [SerializeField] private float reportSeconds = 6f;

    [Tooltip("Economía a la que se abona la cosecha acumulada.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Recolección en curso, para adelantar su cuenta atrás.")]
    [SerializeField] private ResourceExpeditionManager expeditions;

    void Awake()
    {
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        if (expeditions == null)
            expeditions = UnityEngine.Object.FindFirstObjectByType<ResourceExpeditionManager>();
    }

    // La llama el SaveManager al terminar de cargar, con la marca de tiempo del guardado.
    // Una partida anterior al campo no trae marca: se trata como si no hubiera pasado tiempo.
    public void ApplySince(string lastSaveUtc)
    {
        float segundos = ElapsedSeconds(lastSaveUtc);
        if (segundos < minOfflineSeconds) return;

        int comida = 0;
        int descansados = 0;

        foreach (var building in BaseBuilding.All)
        {
            if (building == null) continue;

            comida += building.OfflineHarvest(segundos);
            descansados += building.OfflineRecover(segundos);
        }

        // La granja rinde ~34 de comida cada 10s, ritmo pensado para una sesión de minutos:
        // a pleno rendimiento una noche fuera daría decenas de miles y rompería la economía.
        comida = Mathf.Min(Mathf.RoundToInt(comida * offlineEfficiency), maxOfflineFood);

        if (comida > 0 && economy != null) economy.AddFood(comida);

        bool expedicionLista = expeditions != null && expeditions.AdvanceOffline(segundos);

        Report(segundos, comida, descansados, expedicionLista);
        SaveManager.RequestSave();
    }

    // Segundos fuera, ya recortados al tope. Un reloj movido hacia atrás da 0, no un negativo.
    private float ElapsedSeconds(string lastSaveUtc)
    {
        if (string.IsNullOrEmpty(lastSaveUtc)) return 0f;

        if (!System.DateTime.TryParse(lastSaveUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var guardado))
        {
            Debug.LogWarning($"[Offline] Marca de tiempo ilegible en el guardado: '{lastSaveUtc}'.", this);
            return 0f;
        }

        double transcurridos = (System.DateTime.UtcNow - guardado.ToUniversalTime()).TotalSeconds;
        if (transcurridos <= 0d) return 0f;

        return (float)System.Math.Min(transcurridos, maxOfflineHours * 3600d);
    }

    private void Report(float segundos, int comida, int descansados, bool expedicionLista)
    {
        int horas = Mathf.FloorToInt(segundos / 3600f);
        int minutos = Mathf.FloorToInt(segundos % 3600f / 60f);

        var texto = new System.Text.StringBuilder();
        texto.Append(string.Format(LocalizationManager.Get("UI_OFFLINE_AWAY"), horas, minutos));

        if (comida > 0)
            texto.Append("   ").Append(string.Format(LocalizationManager.Get("UI_OFFLINE_FOOD"), comida));

        if (descansados > 0)
            texto.Append("   ").Append(string.Format(LocalizationManager.Get("UI_OFFLINE_RESTED"), descansados));

        if (expedicionLista)
            texto.Append("   ").Append(LocalizationManager.Get("UI_OFFLINE_EXPEDITION"));

        Debug.Log($"[Offline] {texto}", this);
        ScreenBanner.ShowCompact(texto.ToString(), reportSeconds, new Color(0.72f, 0.86f, 0.66f));
    }
}
