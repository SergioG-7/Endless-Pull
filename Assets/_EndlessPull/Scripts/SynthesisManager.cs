using UnityEngine;

// Síntesis: un héroe se sacrifica de forma definitiva para dar EXP a otro.
public class SynthesisManager : MonoBehaviour
{
    [Tooltip("EXP base que aporta cada estrella del héroe sacrificado.")]
    [SerializeField] private int expPerStarRank = 30;

    [Tooltip("EXP extra en tanto por uno por cada nivel del sacrificado por encima del 1.")]
    [SerializeField] private float expPerExtraLevel = 1f;

    private HeroController target;

    public HeroController Target => target;
    public bool HasTarget => target != null;

    // Se dispara con el mensaje de la última operación, para que la UI lo muestre.
    public event System.Action<string> SynthesisChanged;

    // Con un solo botón por héroe: el primer clic elige objetivo, el segundo sacrifica.
    public void SelectHero(HeroController hero)
    {
        if (hero == null) return;

        // Red de seguridad: el candado manda aunque la UI dejara pulsar el botón.
        if (hero.IsLocked)
        {
            Report(string.Format(LocalizationManager.Get("UI_SYNTH_LOCKED"), Describe(hero)));
            return;
        }

        if (target == null)
        {
            target = hero;
            Report(string.Format(LocalizationManager.Get("UI_SYNTH_TARGET_SET"), Describe(hero)));
            return;
        }

        if (target == hero)
        {
            target = null;
            Report(LocalizationManager.Get("UI_SYNTH_CANCELLED"));
            return;
        }

        Synthesize(target, hero);
    }

    public void ClearTarget()
    {
        if (target == null) return;

        target = null;
        Report(LocalizationManager.Get("UI_SYNTH_CANCELLED"));
    }

    // Destruye al sacrificado y le pasa la EXP al objetivo.
    public bool Synthesize(HeroController targetHero, HeroController fodder)
    {
        if (targetHero == null || fodder == null || targetHero == fodder)
        {
            Report(LocalizationManager.Get("UI_SYNTH_INVALID"));
            return false;
        }

        if (fodder.IsLocked)
        {
            Report(string.Format(LocalizationManager.Get("UI_SYNTH_FODDER_LOCKED"), Describe(fodder)));
            return false;
        }

        // Un héroe desplegado en combate no se puede sacrificar a medio piso (roadmap: saneamiento
        // de punteros antes de destruir, mejor evitar el hueco en la escuadra que limpiarlo después).
        if (fodder.IsDeployed)
        {
            Report(string.Format(LocalizationManager.Get("UI_SYNTH_FODDER_DEPLOYED"), Describe(fodder)));
            return false;
        }

        var progress = targetHero.GetComponent<HeroProgress>();
        if (progress == null)
        {
            Report(string.Format(LocalizationManager.Get("UI_SYNTH_NO_PROGRESS"), Describe(targetHero)));
            return false;
        }

        int exp = ExpFrom(fodder);
        string fodderName = Describe(fodder);
        string targetName = Describe(targetHero);

        // La ficha de la Galería se toma con el héroe entero todavía en pie: equipo, nivel y
        // rareza dejan de poder leerse en cuanto se destruye.
        MemorialManager.Record(fodder, MemorialCause.Synthesis);

        // Desvincular de escuadra/expedición/edificio ANTES de destruir: no depender del prune
        // periódico de cada manager, que solo corre en su propio Update() del frame siguiente.
        UnassignFromEverywhere(fodder);

        // Se desactiva antes de destruir para que FindObjectsByType deje de verlo ya en este frame.
        fodder.gameObject.SetActive(false);
        Destroy(fodder.gameObject);

        progress.AddEXP(exp);
        target = null;

        Report(string.Format(LocalizationManager.Get("UI_SYNTH_DONE"),
            fodderName, exp, targetName, progress.Level));
        SaveManager.RequestSave();
        return true;
    }

    // Saca al sacrificado de escuadra de torre, expedición y puesto de trabajo (si tenía alguno).
    private static void UnassignFromEverywhere(HeroController hero)
    {
        var party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();
        if (party != null)
        {
            if (party.IsInParty(hero)) party.Toggle(hero);
            if (party.IsInExpedition(hero)) party.ToggleExpedition(hero);
        }

        foreach (var building in BaseBuilding.All)
            if (building != null && building.IsWorker(hero)) building.ToggleWorker(hero);
    }

    // EXP proporcional a la rareza y al nivel del sacrificado.
    public int ExpFrom(HeroController fodder)
    {
        if (fodder == null || fodder.Data == null) return 0;

        var progress = fodder.GetComponent<HeroProgress>();
        int level = progress != null ? progress.Level : 1;

        float amount = fodder.Data.starRank * expPerStarRank * (1f + expPerExtraLevel * (level - 1));
        return Mathf.Max(1, Mathf.RoundToInt(amount));
    }

    private string Describe(HeroController hero)
        => hero != null && hero.Data != null ? hero.Data.heroName : "un héroe";

    private void Report(string message)
    {
        Debug.Log($"[Síntesis] {message}", this);
        SynthesisChanged?.Invoke(message);
    }
}
