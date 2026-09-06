using System.Collections.Generic;
using UnityEngine;

// Rango visible de pericia, de F (novato) a S (maestro); overlay sobre el nivel numérico 1-10
// que ya existía, sin tocar su fórmula de daño.
public enum MasteryRank { F, E, D, C, B, A, S }

public static class MasteryRanks
{
    public static string DisplayName(MasteryRank rank) => rank switch
    {
        MasteryRank.F => LocalizationManager.Get("MASTERY_RANK_F"),
        MasteryRank.E => LocalizationManager.Get("MASTERY_RANK_E"),
        MasteryRank.D => LocalizationManager.Get("MASTERY_RANK_D"),
        MasteryRank.C => LocalizationManager.Get("MASTERY_RANK_C"),
        MasteryRank.B => LocalizationManager.Get("MASTERY_RANK_B"),
        MasteryRank.A => LocalizationManager.Get("MASTERY_RANK_A"),
        _ => LocalizationManager.Get("MASTERY_RANK_S")
    };
}

// Maestría por tipo de arma: se gana pegando y entrenando, y sube el daño con ese tipo.
[System.Serializable]
public class WeaponMastery
{
    [Tooltip("Puntos que hacen falta para subir un nivel de maestría.")]
    public int pointsPerLevel = 10;

    [Tooltip("Nivel máximo de maestría por tipo de arma.")]
    public int maxLevel = 10;

    [Tooltip("Daño extra en tanto por uno por cada nivel de maestría.")]
    public float bonusPerLevel = 0.08f;

    [Tooltip("Bonus de evasión (probabilidad de esquiva) al llegar al rango de pericia S.")]
    public float maxEvasionBonusAtRankS = 0.08f;

    [Tooltip("Reducción del tiempo de recuperación tras atacar al llegar al rango de pericia S.")]
    public float maxRecoveryReductionAtRankS = 0.18f;

    // Runtime: el diccionario no se serializa, lo guarda y restaura el SaveManager.
    private readonly Dictionary<WeaponType, int> points = new Dictionary<WeaponType, int>();

    public IReadOnlyDictionary<WeaponType, int> AllPoints => points;

    public int PointsOf(WeaponType type)
        => points.TryGetValue(type, out int value) ? value : 0;

    // Sin arma no hay maestría que valga; el nivel arranca en 1 en cuanto se empuña algo.
    public int LevelOf(WeaponType type)
    {
        if (type == WeaponType.None) return 0;
        return Mathf.Clamp(1 + PointsOf(type) / Mathf.Max(1, pointsPerLevel), 1, maxLevel);
    }

    public float DamageMultiplier(WeaponType type)
        => type == WeaponType.None ? 1f : 1f + LevelOf(type) * bonusPerLevel;

    // F=1-2, E=3-4, D=5-6, C=7, B=8, A=9, S=10 — overlay sobre el nivel numérico, sin tocar
    // la progresión de daño de arriba.
    public MasteryRank RankOf(WeaponType type)
    {
        int level = LevelOf(type);
        if (level <= 2) return MasteryRank.F;
        if (level <= 4) return MasteryRank.E;
        if (level <= 6) return MasteryRank.D;
        if (level == 7) return MasteryRank.C;
        if (level == 8) return MasteryRank.B;
        if (level == 9) return MasteryRank.A;
        return MasteryRank.S;
    }

    // Escala en línea recta con el rango (F=0/6 ... S=6/6) hasta el tope configurado.
    public float EvasionBonus(WeaponType type)
        => type == WeaponType.None ? 0f : (int)RankOf(type) / 6f * maxEvasionBonusAtRankS;

    public float RecoveryReduction(WeaponType type)
        => type == WeaponType.None ? 0f : (int)RankOf(type) / 6f * maxRecoveryReductionAtRankS;

    // Devuelve true si el punto ganado ha hecho subir de nivel.
    public bool AddPoints(WeaponType type, int amount)
    {
        if (type == WeaponType.None || amount <= 0) return false;

        int before = LevelOf(type);
        points[type] = PointsOf(type) + amount;
        return LevelOf(type) > before;
    }

    // La usa el SaveManager al restaurar una partida.
    public void LoadPoints(WeaponType type, int amount)
    {
        if (type == WeaponType.None) return;
        points[type] = Mathf.Max(0, amount);
    }

    // Resumen corto para el roster; solo lo que ya se ha entrenado.
    public string Describe()
    {
        var parts = new List<string>();
        foreach (var pair in points)
        {
            if (pair.Value <= 0) continue;
            parts.Add($"{WeaponTypes.DisplayName(pair.Key)} Nv.{LevelOf(pair.Key)} " +
                      $"({MasteryRanks.DisplayName(RankOf(pair.Key))})");
        }

        // Vacio en vez de un texto: el rotulo del roster ya pone "Maestria" delante.
        return parts.Count == 0 ? "-" : string.Join(", ", parts);
    }

    // Igual pero una por linea y con el formato "Arma: Nv.N (rango)" del resto del modal.
    // equipada: el tipo que lleva puesto ahora mismo. Sale siempre, aunque esté a cero, para
    // que al cambiar de arma se vea en el acto que empieza una pericia nueva desde Nv.1.
    public string DescribeLines(WeaponType equipada = WeaponType.None)
    {
        var sb = new System.Text.StringBuilder();

        var tipos = new List<WeaponType>();
        foreach (var pair in points)
            if (pair.Value > 0) tipos.Add(pair.Key);

        if (equipada != WeaponType.None && !tipos.Contains(equipada)) tipos.Add(equipada);

        foreach (var tipo in tipos)
        {
            var pair = new KeyValuePair<WeaponType, int>(tipo, PointsOf(tipo));
            if (sb.Length > 0) sb.Append('\n');

            int nivel = LevelOf(pair.Key);
            sb.Append($"<b>{WeaponTypes.DisplayName(pair.Key)}</b>  " +
                      $"{LocalizationManager.Get("UI_LEVEL_ABBR")}{nivel}/{maxLevel}  " +
                      $"({MasteryRanks.DisplayName(RankOf(pair.Key))})  " +
                      $"+{nivel * bonusPerLevel * 100f:0}% dmg");
        }

        return sb.ToString();
    }
}
