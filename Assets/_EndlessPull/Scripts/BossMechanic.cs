// Mecánica propia de un jefe: lo que hay que resolver además de bajarle la vida. Valores nuevos
// SIEMPRE al final: el save y el YAML de escena guardan el número, no el nombre.
public enum BossMechanic
{
    None,
    Summon,
    Barrier,
    GroundZone,
    Frenzy
}

// Reparto de mecánicas por la Torre. No cuelga del bioma a propósito: el Templo es todo el
// piso 16 hacia arriba, así que atarla al bioma dejaba a todos los jefes del 20 en adelante con
// la misma. Se rota por número de jefe, que no tiene techo.
public static class BossMechanics
{
    private static readonly BossMechanic[] Rotation =
    {
        BossMechanic.Summon, BossMechanic.Barrier, BossMechanic.GroundZone, BossMechanic.Frenzy
    };

    // A partir de este jefe se le encima una segunda mecánica; antes de eso van sueltas.
    public const int DoubleFromBoss = 9;

    // Número de jefe a partir del piso: con jefe cada 5 pisos, el piso 5 es el jefe 1.
    public static int IndexForFloor(int floor, int bossEveryFloors)
        => bossEveryFloors <= 0 ? 0 : floor / bossEveryFloors;

    public static BossMechanic Primary(int bossIndex)
        => bossIndex < 1 ? BossMechanic.None : Rotation[(bossIndex - 1) % Rotation.Length];

    // La segunda va desplazada respecto a la primera, y el desplazamiento cambia cada vuelta:
    // así salen las seis parejas posibles por turnos y nunca coincide con la primera.
    public static BossMechanic Secondary(int bossIndex)
    {
        if (bossIndex < DoubleFromBoss) return BossMechanic.None;

        int vuelta = (bossIndex - DoubleFromBoss) / Rotation.Length;
        int salto = 1 + vuelta % (Rotation.Length - 1);
        return Rotation[(bossIndex - 1 + salto) % Rotation.Length];
    }

    public static string DisplayName(BossMechanic mechanic)
        => mechanic == BossMechanic.None
            ? string.Empty
            : LocalizationManager.Get("BOSS_" + mechanic.ToString().ToUpperInvariant());

    public static string Description(BossMechanic mechanic)
        => mechanic == BossMechanic.None
            ? string.Empty
            : LocalizationManager.Get("BOSS_" + mechanic.ToString().ToUpperInvariant() + "_DESC");
}
