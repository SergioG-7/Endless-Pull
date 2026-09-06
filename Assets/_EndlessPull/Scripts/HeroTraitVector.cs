using UnityEngine;

// Vector de rasgos de combate de un héroe, en tres canales normalizados 0-1. Se deriva de lo
// que ya existe (personalidad, pasivas, moral, fatiga y refinamiento) en vez de guardar campos
// nuevos, así que una partida vieja lo calcula igual sin migración.
//
// Es la entrada de personalidad que consume HeroAgent junto a las observaciones espaciales.
public struct HeroTraitVector
{
    [Tooltip("0 = se aparta al primer golpe, 1 = entra de cabeza aunque le cueste la vida.")]
    public float Bravery;

    [Tooltip("0 = se descompone bajo fuego, 1 = mantiene la puntería en plena ráfaga.")]
    public float Composure;

    [Tooltip("0 = lobo solitario, 1 = aguanta la línea y sigue al portaestandarte.")]
    public float Cooperation;

    // Etiqueta visible: se decide por el canal que más se separa del centro.
    public string Label
    {
        get
        {
            float valor = Bravery - 0.5f;
            float calculo = Composure - 0.5f;

            if (calculo > 0.22f && calculo >= Mathf.Abs(valor)) return LocalizationManager.Get("TRAIT_CALCULATING");
            if (valor >= 0.18f) return LocalizationManager.Get("TRAIT_BRAVE");
            if (valor <= -0.18f) return LocalizationManager.Get("TRAIT_COWARD");

            return LocalizationManager.Get("TRAIT_STEADY");
        }
    }
}

public static class HeroTraitVectors
{
    // Cuánto pesa cada fuente en su canal; sacados a constantes para poder tunearlos de golpe.
    private const float MoraleWeight = 0.25f;
    private const float FatigueWeight = 0.30f;
    private const float RefinementWeight = 0.20f;
    private const float PassiveFlagWeight = 0.18f;

    public static HeroTraitVector Of(HeroController hero)
    {
        var vector = new HeroTraitVector { Bravery = 0.5f, Composure = 0.5f, Cooperation = 0.5f };
        if (hero == null) return vector;

        var pasivas = hero.Passives;
        var progress = hero.GetComponent<HeroProgress>();

        float moral = hero.MoralePercent / 100f;
        float fatiga = hero.Fatigue / 100f;
        float pericia = progress != null ? progress.SkillRefinement : 0f;

        // Valor: lo que el jugador ha puesto en el perfil táctico, más lo que empujan las
        // pasivas de sed de sangre, más la moral. Un héroe hundido no carga contra nada.
        float agresion = progress != null ? progress.Aggression : 0.5f;
        vector.Bravery = Mathf.Clamp01(
            agresion
            + PassiveSkills.AggressionBonus(pasivas)
            + (moral - 0.5f) * MoraleWeight
            + (hero.Trait == HeroTrait.Fierce ? 0.15f : 0f)
            + (hero.Trait == HeroTrait.Slacker ? -0.10f : 0f));

        // Templanza: las horas de muñeco y las pasivas de sangre fría la suben; la fatiga la
        // hunde. Es el canal que degrada la puntería bajo ráfaga.
        vector.Composure = Mathf.Clamp01(
            0.5f
            + pericia * RefinementWeight
            + (PassiveSkills.KeepsCool(pasivas) ? PassiveFlagWeight : 0f)
            + (PassiveSkills.ReadsField(pasivas) ? PassiveFlagWeight * 0.5f : 0f)
            - fatiga * FatigueWeight
            + (moral - 0.5f) * MoraleWeight
            + (hero.Trait == HeroTrait.Diligent ? 0.08f : 0f));

        // Cooperación: aguantar la línea y liderar suman; el fiero y el vago tiran del grupo.
        vector.Cooperation = Mathf.Clamp01(
            0.5f
            + (PassiveSkills.HoldsLine(pasivas) ? PassiveFlagWeight : 0f)
            + (PassiveSkills.Leads(pasivas) ? PassiveFlagWeight : 0f)
            + (moral - 0.5f) * MoraleWeight
            + (hero.Trait == HeroTrait.Fierce ? -0.12f : 0f)
            + (hero.Trait == HeroTrait.Diligent ? 0.10f : 0f));

        return vector;
    }
}
