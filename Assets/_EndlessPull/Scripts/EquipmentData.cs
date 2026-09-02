using UnityEngine;

// Tipo de arma; manda en la maestría y en qué slot entra la pieza.
public enum WeaponType
{
    None,
    Sword,
    Spear,
    Bow,
    Shield,
    Armor,
    Accessory,
    Staff,
    Mace
}

// Afijo pasivo de una pieza; cada uno se lee en un punto distinto del combate.
public enum EquipmentAffix
{
    None,
    LifeSteal,
    EvasionBoost,
    CritDamage,
    ArmorPierce
}

// Nombres y unidades de los afijos; todos se expresan en porcentaje.
public static class EquipmentAffixes
{
    public static string DisplayName(EquipmentAffix affix)
    {
        if (affix == EquipmentAffix.None) return string.Empty;

        return LocalizationManager.Get("AFFIX_" + affix.ToString().ToUpperInvariant());
    }

    public static string Describe(EquipmentAffix affix, float value)
    {
        if (affix == EquipmentAffix.None) return string.Empty;

        return $"{DisplayName(affix)} +{value:0.#}%";
    }

    // Un color por afijo, para distinguirlos de un vistazo en el modal de equipo.
    public static Color Color(EquipmentAffix affix)
    {
        switch (affix)
        {
            case EquipmentAffix.LifeSteal: return UITheme.Hex("EF4444");
            case EquipmentAffix.EvasionBoost: return UITheme.Hex("22C55E");
            case EquipmentAffix.CritDamage: return UITheme.Hex("FFD700");
            case EquipmentAffix.ArmorPierce: return UITheme.Hex("A855F7");
        }
        return UITheme.TextMuted;
    }
}

// Hueco del héroe que ocupa la pieza. Los valores van fijos: insertar Shield en medio
// habría reinterpretado los assets ya guardados.
public enum EquipmentSlot
{
    Weapon = 0,
    Armor = 1,
    Accessory = 2,
    Shield = 3
}

// Nombres visibles de los tipos de arma.
public static class WeaponTypes
{
    public static string DisplayName(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Sword: return LocalizationManager.Get("WEAPON_SWORD");
            case WeaponType.Spear: return LocalizationManager.Get("WEAPON_SPEAR");
            case WeaponType.Bow: return LocalizationManager.Get("WEAPON_BOW");
            case WeaponType.Shield: return LocalizationManager.Get("WEAPON_SHIELD");
            case WeaponType.Armor: return LocalizationManager.Get("WEAPON_ARMOR");
            case WeaponType.Accessory: return LocalizationManager.Get("WEAPON_ACCESSORY");
            case WeaponType.Staff: return LocalizationManager.Get("WEAPON_STAFF");
            case WeaponType.Mace: return LocalizationManager.Get("WEAPON_MACE");
        }
        return LocalizationManager.Get("WEAPON_NONE");
    }
}

[CreateAssetMenu(fileName = "Equip_New", menuName = "Endless Pull/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    [Tooltip("Nombre visible de la pieza.")]
    public string equipName = "Espada de Hierro";

    [Tooltip("Clave de localización del nombre; vacío usa equipName tal cual (compatibilidad con piezas antiguas).")]
    public string nameKey = string.Empty;

    public string LocalizedName() => string.IsNullOrEmpty(nameKey) ? equipName : LocalizationManager.Get(nameKey);


    [Tooltip("Hueco del héroe en el que se coloca.")]
    public EquipmentSlot slotType = EquipmentSlot.Weapon;

    [Tooltip("Tipo de arma; solo cuenta para la maestría si va en el hueco de arma.")]
    public WeaponType weaponType = WeaponType.Sword;

    [Tooltip("Ataque que suma la pieza.")]
    public int bonusATK;

    [Tooltip("Defensa que suma la pieza.")]
    public int bonusDEF;

    [Tooltip("Vida máxima que suma la pieza.")]
    public int bonusHP;

    [Tooltip("Combates que aguanta la pieza antes de romperse y dejar de dar bonus.")]
    public int maxDurability = 10;

    [Tooltip("Afijo pasivo de la pieza; None deja la pieza con solo sus cifras.")]
    public EquipmentAffix passiveTrait = EquipmentAffix.None;

    [Tooltip("Valor del afijo, siempre en porcentaje (15 = 15%).")]
    public float passiveValue;

    public bool HasAffix => passiveTrait != EquipmentAffix.None && passiveValue > 0f;

    public string AffixLabel() => EquipmentAffixes.Describe(passiveTrait, passiveValue);

    // Texto corto para el roster: nombre y lo que aporta.
public string ShortLabel()
    {
        string bonus = string.Empty;
        if (bonusATK != 0) bonus += $" +{bonusATK}ATK";
        if (bonusDEF != 0) bonus += $" +{bonusDEF}DEF";
        if (bonusHP != 0) bonus += $" +{bonusHP}HP";
        if (HasAffix) bonus += $" [{AffixLabel()}]";

        return LocalizedName() + bonus;
    }
}
