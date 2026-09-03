using System.Collections.Generic;
using UnityEngine;

// Rasgos innatos del héroe; salen sorteados al invocarlo y no cambian nunca.
public enum PassiveSkill
{
    PainTolerance,
    Evasion,
    EagleEye
}

// Tabla central de pasivas, al estilo de HeroTraits.
public static class PassiveSkills
{
    public static readonly PassiveSkill[] All =
    {
        PassiveSkill.PainTolerance,
        PassiveSkill.Evasion,
        PassiveSkill.EagleEye
    };

    public static string DisplayName(PassiveSkill passive)
    {
        switch (passive)
        {
            case PassiveSkill.PainTolerance: return LocalizationManager.Get("PASSIVE_NAME_PAINTOLERANCE");
            case PassiveSkill.Evasion: return LocalizationManager.Get("PASSIVE_NAME_EVASION");
            case PassiveSkill.EagleEye: return LocalizationManager.Get("PASSIVE_NAME_EAGLEEYE");
        }
        return passive.ToString();
    }

    // Explicación corta de qué hace de verdad; la usa el modal de Habilidades de la ficha.
    public static string Description(PassiveSkill passive)
    {
        switch (passive)
        {
            case PassiveSkill.PainTolerance: return LocalizationManager.Get("PASSIVE_DESC_PAINTOLERANCE");
            case PassiveSkill.Evasion: return LocalizationManager.Get("PASSIVE_DESC_EVASION");
            case PassiveSkill.EagleEye: return LocalizationManager.Get("PASSIVE_DESC_EAGLEEYE");
        }
        return string.Empty;
    }

    // Sortea una o dos pasivas distintas para un héroe recién invocado.
    public static List<PassiveSkill> RandomSet()
    {
        var pool = new List<PassiveSkill>(All);
        var picked = new List<PassiveSkill>();

        int count = Random.value < 0.5f ? 1 : 2;
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            picked.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return picked;
    }

    public static string Describe(IReadOnlyList<PassiveSkill> passives)
    {
        if (passives == null || passives.Count == 0) return LocalizationManager.Get("UI_NO_PASSIVES");

        var names = new List<string>();
        foreach (var p in passives) names.Add(DisplayName(p));
        return string.Join(", ", names);
    }
}
