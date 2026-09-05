using System.Collections.Generic;
using UnityEngine;

// Estados que pueden llevar héroes y enemigos a la vez.
public enum StatusEffect
{
    Poison,
    Bleed,
    Stun,
    Slow,
    Shield
}

// Un estado activo sobre una unidad; la magnitud significa una cosa distinta en cada tipo.
public class ActiveStatus
{
    public StatusEffect type;
    public float remaining;
    public float magnitude;
    public float tickTimer;
}

// Lleva los estados alterados de una unidad. Se añade solo la primera vez que le aplican uno.
public class StatusEffectManager : MonoBehaviour
{
    [Tooltip("Segundos entre golpes de daño de veneno y sangrado.")]
    [SerializeField] private float tickInterval = 1f;

    [Tooltip("Velocidad que queda al estar ralentizado, en tanto por uno.")]
    [SerializeField] private float slowFactor = 0.6f;

    private readonly List<ActiveStatus> active = new List<ActiveStatus>();

    private HeroController hero;
    private EnemyController enemy;

    public bool IsStunned => Has(StatusEffect.Stun);
    public float SpeedMultiplier => Has(StatusEffect.Slow) ? slowFactor : 1f;
    public int ShieldPoints => Mathf.RoundToInt(MagnitudeOf(StatusEffect.Shield));

    void Awake()
    {
        hero = GetComponent<HeroController>();
        enemy = GetComponent<EnemyController>();

        // El feedback visual viaja siempre con los estados; no hay que cablearlo aparte.
        if (GetComponent<StatusVisuals>() == null) gameObject.AddComponent<StatusVisuals>();
    }

    // Punto de entrada único: crea el componente si la unidad aún no lo tenía.
    public static StatusEffectManager For(GameObject target)
    {
        if (target == null) return null;

        var manager = target.GetComponent<StatusEffectManager>();
        return manager != null ? manager : target.AddComponent<StatusEffectManager>();
    }

    public static void Apply(GameObject target, StatusEffect type, float duration, float magnitude)
    {
        var manager = For(target);
        if (manager != null) manager.Add(type, duration, magnitude);
    }

    // Refrescar un estado no lo apila: se queda la duración más larga y la magnitud mayor.
    public void Add(StatusEffect type, float duration, float magnitude)
    {
        if (duration <= 0f) return;

        foreach (var status in active)
        {
            if (status.type != type) continue;

            status.remaining = Mathf.Max(status.remaining, duration);
            status.magnitude = type == StatusEffect.Shield
                ? status.magnitude + magnitude
                : Mathf.Max(status.magnitude, magnitude);
            return;
        }

        active.Add(new ActiveStatus { type = type, remaining = duration, magnitude = magnitude });
    }

    public bool Has(StatusEffect type)
    {
        foreach (var status in active)
            if (status.type == type && status.remaining > 0f) return true;

        return false;
    }

    public float MagnitudeOf(StatusEffect type)
    {
        foreach (var status in active)
            if (status.type == type && status.remaining > 0f) return status.magnitude;

        return 0f;
    }

    // El escudo se come el daño antes que la vida; devuelve lo que llega de verdad.
    public int AbsorbDamage(int amount)
    {
        if (amount <= 0) return amount;

        foreach (var status in active)
        {
            if (status.type != StatusEffect.Shield || status.remaining <= 0f) continue;

            int absorbido = Mathf.Min(amount, Mathf.RoundToInt(status.magnitude));
            status.magnitude -= absorbido;
            if (status.magnitude <= 0f) status.remaining = 0f;

            DamageTextManager.Show(transform.position, string.Format(LocalizationManager.Get("FX_SHIELD_ABSORB"), absorbido), new Color(0.5f, 0.8f, 1f));
            return amount - absorbido;
        }

        return amount;
    }

    public void Clear() => active.Clear();

    // Quita un estado concreto sin tocar el resto; la usa la ruptura de barrera del Maestro.
    public void Remove(StatusEffect type) => active.RemoveAll(s => s != null && s.type == type);

    void Update()
    {
        if (active.Count == 0) return;

        float dt = Time.deltaTime;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            var status = active[i];
            status.remaining -= dt;

            if (status.type == StatusEffect.Poison || status.type == StatusEffect.Bleed)
                TickDamage(status, dt);

            if (status.remaining <= 0f) active.RemoveAt(i);
        }
    }

    // El daño por tick se salta la defensa: es la gracia de envenenar a un tanque.
    private void TickDamage(ActiveStatus status, float dt)
    {
        status.tickTimer -= dt;
        if (status.tickTimer > 0f) return;

        status.tickTimer = tickInterval;
        int damage = Mathf.Max(1, Mathf.RoundToInt(status.magnitude));

        if (hero != null) hero.TakeDamage(damage, true);
        else if (enemy != null) enemy.TakeDamage(damage);
    }

    // Resumen corto para el roster.
    public string Describe()
    {
        if (active.Count == 0) return string.Empty;

        var parts = new List<string>();
        foreach (var status in active)
            parts.Add($"{DisplayName(status.type)} {status.remaining:0.0}s");

        return string.Join(", ", parts);
    }

    public static string DisplayName(StatusEffect type)
    {
        switch (type)
        {
            case StatusEffect.Poison: return LocalizationManager.Get("ST_POISON");
            case StatusEffect.Bleed: return LocalizationManager.Get("ST_BLEED");
            case StatusEffect.Stun: return LocalizationManager.Get("ST_STUN");
            case StatusEffect.Slow: return LocalizationManager.Get("ST_SLOW");
            case StatusEffect.Shield: return LocalizationManager.Get("ST_SHIELD");
        }
        return type.ToString();
    }
}
