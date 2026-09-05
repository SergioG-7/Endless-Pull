using UnityEngine;

// Acciones de intervención del Maestro; cada una gasta carga de la barra.
public enum InterventionAction
{
    ForcedDodge,
    BreakBarrier,
    HealingPulse
}

// Barra de intervención divina: la cargan los héroes de 6★ y 7★ desplegados, y se gasta en
// interferir directamente en el combate. Sin héroes de rango alto en la escuadra no hay barra.
public class MasterIntervention : MonoBehaviour
{
    [Tooltip("Carga máxima de la barra.")]
    [SerializeField] private float maxCharge = 100f;

    [Tooltip("Carga por segundo que aporta cada héroe de 6★ desplegado.")]
    [SerializeField] private float chargePerSixStar = 1.2f;

    [Tooltip("Carga por segundo que aporta cada héroe de 7★ desplegado.")]
    [SerializeField] private float chargePerSevenStar = 2.5f;

    [Tooltip("Coste de cada acción: esquiva forzada, ruptura de barrera y pulso curativo.")]
    [SerializeField] private float[] actionCost = { 30f, 40f, 50f };

    [Tooltip("Segundos que dura la esquiva forzada sobre la escuadra.")]
    [SerializeField] private float dodgeDuration = 4f;

    [Tooltip("Segundos que la armadura enemiga queda rota.")]
    [SerializeField] private float breakDuration = 6f;

    [Tooltip("Fracción de vida máxima que devuelve el pulso curativo.")]
    [Range(0f, 1f)]
    [SerializeField] private float healPercent = 0.35f;

    [SerializeField] private WaveManager waves;

    private float charge;

    public float Charge => charge;
    public float MaxCharge => maxCharge;
    public float Ratio => maxCharge > 0f ? Mathf.Clamp01(charge / maxCharge) : 0f;

    // Salta al cargar y al gastar; lo escucha la barra para repintarse sin sondear cada frame.
    public event System.Action ChargeChanged;

    public float CostOf(InterventionAction action)
    {
        int i = (int)action;
        return actionCost != null && i < actionCost.Length ? actionCost[i] : 0f;
    }

    public bool CanUse(InterventionAction action) => IsActive && charge >= CostOf(action);

    // Solo hay barra en combate y con héroes de rango alto fuera: es lo que la carga.
    public bool IsActive => waves != null && waves.State == ExpeditionState.InProgress && ChargeRate > 0f;

    // Carga por segundo según quién esté desplegado ahora mismo.
    public float ChargeRate
    {
        get
        {
            if (waves == null) return 0f;

            float rate = 0f;
            foreach (var hero in waves.Deployed)
            {
                if (hero == null) continue;
                if (hero.StarRank >= 7) rate += chargePerSevenStar;
                else if (hero.StarRank == 6) rate += chargePerSixStar;
            }

            return rate;
        }
    }

    void Awake()
    {
        if (waves == null) waves = Object.FindFirstObjectByType<WaveManager>();
    }

    void Update()
    {
        if (waves == null || waves.State != ExpeditionState.InProgress)
        {
            // Fuera de combate la barra se vacía: no se acumula entre pisos.
            if (charge != 0f) { charge = 0f; ChargeChanged?.Invoke(); }
            return;
        }

        float rate = ChargeRate;
        if (rate <= 0f || charge >= maxCharge) return;

        charge = Mathf.Min(maxCharge, charge + rate * Time.deltaTime);
        ChargeChanged?.Invoke();
    }

    public bool Use(InterventionAction action)
    {
        if (!CanUse(action)) return false;

        charge -= CostOf(action);
        ChargeChanged?.Invoke();

        switch (action)
        {
            case InterventionAction.ForcedDodge: ApplyForcedDodge(); break;
            case InterventionAction.BreakBarrier: ApplyBreakBarrier(); break;
            case InterventionAction.HealingPulse: ApplyHealingPulse(); break;
        }

        AudioManager.Play(SfxId.Ascension);
        return true;
    }

    private void ApplyForcedDodge()
    {
        foreach (var hero in waves.Deployed)
            if (hero != null) hero.SetGuaranteedDodge(dodgeDuration);

        Debug.Log($"[Maestro] Esquiva forzada {dodgeDuration}s sobre la escuadra.", this);
    }

    private void ApplyBreakBarrier()
    {
        foreach (var enemy in waves.Wave)
        {
            if (enemy == null) continue;

            enemy.SetArmorBroken(breakDuration);
            enemy.Status.Remove(StatusEffect.Shield);
        }

        Debug.Log($"[Maestro] Armadura enemiga rota {breakDuration}s.", this);
    }

    private void ApplyHealingPulse()
    {
        foreach (var hero in waves.Deployed)
        {
            if (hero == null) continue;
            hero.Heal(Mathf.Max(1, Mathf.RoundToInt(hero.MaxHealth * healPercent)));
        }

        Debug.Log($"[Maestro] Pulso curativo: {healPercent:P0} de vida a la escuadra.", this);
    }
}
