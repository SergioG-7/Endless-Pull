// Constantes de combate compartidas entre héroes y enemigos, para no duplicar el número
// en los dos TakeDamage y que se desincronicen.
public static class CombatTuning
{
    // Fracción del golpe que atraviesa siempre la defensa. Sin esto la resta plana convierte
    // la defensa en invulnerabilidad total hasta que el ataque enemigo la cruza de golpe.
    public const float MinDamageFraction = 0.20f;
}
