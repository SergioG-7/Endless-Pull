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
        public string summary;
        public int requiredFloor;
    }

    // Resúmenes de design/lore/*.md (Fase 27, world-builder), gateados por el mismo piso
    // que ya desbloquea los cuadrantes East/South/West (ver QuadrantController).
    private static readonly LoreEntry[] loreEntries =
    {
        new LoreEntry { titleKey = "ARCHIVE_LORE_TOWER", requiredFloor = 1,
            summary = "La Torre es una institución de expedición reconocida en el mundo: el Maestro " +
                      "dirige escuadras piso a piso desde el Portal, sin entrar nunca en persona." },
        new LoreEntry { titleKey = "ARCHIVE_LORE_DECREES", requiredFloor = 5,
            summary = "Los Decretos son la única voz de mando directa del Maestro en combate, " +
                      "un protocolo codificado por los eruditos de la Torre de Marfil." },
        new LoreEntry { titleKey = "ARCHIVE_LORE_HIERARCHY", requiredFloor = 10,
            summary = "La Ley del Rango reparte los cupos de cada instalación de base por mérito " +
                      "de expedición, no por antigüedad ni favor personal." },
        new LoreEntry { titleKey = "ARCHIVE_LORE_FACTIONS", requiredFloor = 15,
            summary = "Cada origen de héroe representa una facción real del mundo, con su propia " +
                      "razón para luchar mejor codo a codo con los suyos." },
    };

    // Registro en memoria de esta sesión; persistirlo en el save queda para el paso 2.
    private readonly List<string> milestones = new List<string>();
    public IReadOnlyList<string> Milestones => milestones;

    void OnEnable() => HeroProgress.HeroAscended += OnHeroAscended;
    void OnDisable() => HeroProgress.HeroAscended -= OnHeroAscended;

    private void OnHeroAscended(HeroController hero, int starRank)
    {
        if (hero == null || hero.Data == null) return;

        milestones.Insert(0,
            $"{hero.Data.heroName} asciende a {starRank}★ (Piso {BaseBuilding.TowerFloor}).");
    }

    // Entradas de lore que ya tocan según el piso actual de la torre.
    public List<(string titleKey, string summary)> UnlockedLore()
    {
        var result = new List<(string, string)>();
        foreach (var entry in loreEntries)
            if (BaseBuilding.TowerFloor >= entry.requiredFloor)
                result.Add((entry.titleKey, entry.summary));

        return result;
    }
}
