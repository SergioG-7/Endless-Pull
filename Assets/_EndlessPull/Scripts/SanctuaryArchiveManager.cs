using System.Collections.Generic;
using UnityEngine;

// Registro del Archivo del Santuario: hitos de ascensión de héroes y lore ya
// desbloqueada por piso de torre. No inventa lore nueva, solo la resume desde design/lore/.
public class SanctuaryArchiveManager : MonoBehaviour
{
    [System.Serializable]
    private struct LoreEntry
    {
        public string titleKey;
        public string summaryKey;
        public int requiredFloor;
    }

    // Resúmenes de design/lore/*.md (Fase 27, world-builder), gateados por el mismo piso
    // que ya desbloquea los cuadrantes East/South/West (ver QuadrantController).
    private static readonly LoreEntry[] loreEntries =
    {
        new LoreEntry { titleKey = "ARCHIVE_LORE_TOWER", summaryKey = "ARCHIVE_LORE_TOWER_BODY", requiredFloor = 1 },
        new LoreEntry { titleKey = "ARCHIVE_LORE_DECREES", summaryKey = "ARCHIVE_LORE_DECREES_BODY", requiredFloor = 5 },
        new LoreEntry { titleKey = "ARCHIVE_LORE_HIERARCHY", summaryKey = "ARCHIVE_LORE_HIERARCHY_BODY", requiredFloor = 10 },
        new LoreEntry { titleKey = "ARCHIVE_LORE_FACTIONS", summaryKey = "ARCHIVE_LORE_FACTIONS_BODY", requiredFloor = 15 },
    };

    // Hito estructurado: se renderiza con la plantilla localizada en el momento de mostrarlo,
    // no al ascender, para que un cambio de idioma después no deje el historial congelado.
    public struct AscensionMilestone
    {
        public string heroName;
        public int starRank;
        public int floor;
    }

    // Registro en memoria de esta sesión; persistirlo en el save queda para el paso 2.
    private readonly List<AscensionMilestone> milestones = new List<AscensionMilestone>();
    public IReadOnlyList<AscensionMilestone> Milestones => milestones;

    public static string Format(AscensionMilestone m) =>
        string.Format(LocalizationManager.Get("ARCHIVE_MILESTONE"), m.heroName, m.starRank, m.floor);

    void OnEnable() => HeroProgress.HeroAscended += OnHeroAscended;
    void OnDisable() => HeroProgress.HeroAscended -= OnHeroAscended;

    private void OnHeroAscended(HeroController hero, int starRank)
    {
        if (hero == null || hero.Data == null) return;

        milestones.Insert(0, new AscensionMilestone
        {
            heroName = hero.Data.heroName,
            starRank = starRank,
            floor = BaseBuilding.TowerFloor
        });
    }

    // Entradas de lore que ya tocan según el piso actual de la torre.
    public List<(string titleKey, string summaryKey)> UnlockedLore()
    {
        var result = new List<(string, string)>();
        foreach (var entry in loreEntries)
            if (BaseBuilding.TowerFloor >= entry.requiredFloor)
                result.Add((entry.titleKey, entry.summaryKey));

        return result;
    }
}
