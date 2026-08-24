using System.Collections.Generic;
using UnityEngine;

// Maestría por tipo de arma: se gana pegando y entrenando, y sube el daño con ese tipo.
[System.Serializable]
public class WeaponMastery
{
    [Tooltip("Puntos que hacen falta para subir un nivel de maestría.")]
    public int pointsPerLevel = 10;

    [Tooltip("Nivel máximo de maestría por tipo de arma.")]
    public int maxLevel = 10;

    [Tooltip("Daño extra en tanto por uno por cada nivel de maestría.")]
    public float bonusPerLevel = 0.05f;

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
            parts.Add($"{WeaponTypes.DisplayName(pair.Key)} Nv.{LevelOf(pair.Key)}");
        }

        return parts.Count == 0 ? "sin maestría" : string.Join(", ", parts);
    }
}
