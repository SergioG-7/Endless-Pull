using UnityEngine;

// Personalidad del héroe; cambia a qué edificios va y cómo pelea.
public enum HeroTrait
{
    Diligent,
    Glutton,
    Slacker,
    Fierce
}

// Tabla central de modificadores por rasgo, para no repartir ifs por los controllers.
public static class HeroTraits
{
    public static readonly HeroTrait[] All =
    {
        HeroTrait.Diligent,
        HeroTrait.Glutton,
        HeroTrait.Slacker,
        HeroTrait.Fierce
    };

    public static HeroTrait Random()
        => All[UnityEngine.Random.Range(0, All.Length)];

    public static string DisplayName(HeroTrait trait)
    {
        switch (trait)
        {
            case HeroTrait.Diligent: return "Trabajador";
            case HeroTrait.Glutton: return "Glotón";
            case HeroTrait.Slacker: return "Perezoso";
            case HeroTrait.Fierce: return "Feroz";
        }
        return trait.ToString();
    }

    // Peso relativo al sortear a qué edificio ir.
    public static float BuildingWeight(HeroTrait trait, BuildingType building)
    {
        switch (trait)
        {
            case HeroTrait.Diligent:
                return building == BuildingType.TrainingDummy ? 4f : 1f;

            case HeroTrait.Glutton:
                if (building == BuildingType.Canteen) return 4f;
                return building == BuildingType.RestArea ? 2f : 1f;

            case HeroTrait.Slacker:
                if (building == BuildingType.RestArea) return 3f;
                return building == BuildingType.TrainingDummy ? 0.5f : 1.5f;

            case HeroTrait.Fierce:
                return building == BuildingType.TrainingDummy ? 2.5f : 1f;
        }
        return 1f;
    }

    // Cuánto se alarga el descanso entre paseos.
    public static float IdleMultiplier(HeroTrait trait)
        => trait == HeroTrait.Slacker ? 2.5f : 1f;

    // Con qué frecuencia decide ir a un edificio en vez de vagar.
    public static float VisitChanceMultiplier(HeroTrait trait)
    {
        if (trait == HeroTrait.Slacker) return 0.5f;
        if (trait == HeroTrait.Diligent) return 1.25f;
        return 1f;
    }

    public static int AttackBonus(HeroTrait trait)
        => trait == HeroTrait.Fierce ? 2 : 0;

    public static float DetectionMultiplier(HeroTrait trait)
        => trait == HeroTrait.Fierce ? 1.5f : 1f;

    // El glotón aprovecha mejor la comida.
    public static float HealMultiplier(HeroTrait trait, BuildingType building)
        => trait == HeroTrait.Glutton && building == BuildingType.Canteen ? 1.5f : 1f;
}
