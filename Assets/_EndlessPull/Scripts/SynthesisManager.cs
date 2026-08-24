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

        if (target == null)
        {
            target = hero;
            Report($"Objetivo: {Describe(hero)}. Pulsa en otro héroe para sacrificarlo.");
            return;
        }

        if (target == hero)
        {
            target = null;
            Report("Síntesis cancelada.");
            return;
        }

        Synthesize(target, hero);
    }

    public void ClearTarget()
    {
        if (target == null) return;

        target = null;
        Report("Síntesis cancelada.");
    }

    // Destruye al sacrificado y le pasa la EXP al objetivo.
    public bool Synthesize(HeroController targetHero, HeroController fodder)
    {
        if (targetHero == null || fodder == null || targetHero == fodder)
        {
            Report("Síntesis inválida: hacen falta dos héroes distintos.");
            return false;
        }

        var progress = targetHero.GetComponent<HeroProgress>();
        if (progress == null)
        {
            Report($"{Describe(targetHero)} no tiene HeroProgress; no puede absorber EXP.");
            return false;
        }

        int exp = ExpFrom(fodder);
        string fodderName = Describe(fodder);
        string targetName = Describe(targetHero);

        // Se desactiva antes de destruir para que FindObjectsByType deje de verlo ya en este frame.
        fodder.gameObject.SetActive(false);
        Destroy(fodder.gameObject);

        progress.AddEXP(exp);
        target = null;

        Report($"{fodderName} sacrificado: +{exp} EXP para {targetName} (Nv. {progress.Level}).");
        SaveManager.RequestSave();
        return true;
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
