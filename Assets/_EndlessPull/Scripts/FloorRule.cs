using UnityEngine;

// Regla propia de un piso: un modificador que cambia cómo se pelea ese piso concreto, además de
// su misión y su bioma. Valores nuevos SIEMPRE al final: el save serializa el número.
public enum FloorRule
{
    None,
    Fog,
    NoHealing,
    DrainingGround,
    EliteWave
}

// Única fuente de verdad de la regla del piso en curso. Es estática a propósito: la leen el
// héroe, el enemigo y la oleada desde sitios muy distintos, y pasarla por parámetro obligaría a
// tocar media docena de firmas ya operativas.
public static class FloorRules
{
    [Tooltip("Regla activa del piso en curso.")]
    public static FloorRule Current { get; private set; } = FloorRule.None;

    // Reglas que se pueden sortear; None no entra en el sorteo, es el resultado de no sortear.
    private static readonly FloorRule[] Sorteables =
    {
        FloorRule.Fog, FloorRule.NoHealing, FloorRule.DrainingGround, FloorRule.EliteWave
    };

    // Por debajo de este piso no hay reglas: los primeros pisos son el tutorial de hecho.
    public const int FirstRuledFloor = 8;

    // Probabilidad de que un piso corriente traiga regla.
    public const float Chance = 0.22f;

    // Multiplicador de alcance de detección con niebla.
    public const float FogRangeMultiplier = 0.55f;

    // Fatiga por segundo que impone el suelo que drena, mientras dure el combate.
    public const float DrainPerSecond = 3.5f;

    // Vida y daño extra de los enemigos de una oleada de élites.
    public const float EliteHealthMultiplier = 1.55f;
    public const float EliteDamageMultiplier = 1.25f;

    // Cuántos enemigos menos salen a la vez en una oleada de élites; son menos pero más duros.
    public const int EliteSimultaneousPenalty = 1;

    // La sortea el WaveManager al empezar el piso. Un piso de jefe nunca lleva regla: ya tiene
    // su propia mecánica y encadenar las dos convierte el piso en un muro.
    public static void RollForFloor(int floor, bool bossFloor)
    {
        if (bossFloor || floor < FirstRuledFloor) { Current = FloorRule.None; return; }

        Current = Random.value < Chance
            ? Sorteables[Random.Range(0, Sorteables.Length)]
            : FloorRule.None;
    }

    // Al volver a la base no queda regla colgando; si no, la base heredaba la niebla del piso.
    public static void Clear() => Current = FloorRule.None;

    public static bool IsFog => Current == FloorRule.Fog;
    public static bool BlocksHealing => Current == FloorRule.NoHealing;
    public static bool DrainsFatigue => Current == FloorRule.DrainingGround;
    public static bool IsEliteWave => Current == FloorRule.EliteWave;

    public static string DisplayName(FloorRule rule)
    {
        switch (rule)
        {
            case FloorRule.Fog: return LocalizationManager.Get("RULE_FOG");
            case FloorRule.NoHealing: return LocalizationManager.Get("RULE_NO_HEALING");
            case FloorRule.DrainingGround: return LocalizationManager.Get("RULE_DRAINING");
            case FloorRule.EliteWave: return LocalizationManager.Get("RULE_ELITE");
        }
        return string.Empty;
    }

    public static string Description(FloorRule rule)
    {
        switch (rule)
        {
            case FloorRule.Fog: return LocalizationManager.Get("RULE_FOG_DESC");
            case FloorRule.NoHealing: return LocalizationManager.Get("RULE_NO_HEALING_DESC");
            case FloorRule.DrainingGround: return LocalizationManager.Get("RULE_DRAINING_DESC");
            case FloorRule.EliteWave: return LocalizationManager.Get("RULE_ELITE_DESC");
        }
        return string.Empty;
    }
}
