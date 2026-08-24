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
    Accessory
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

// Nombres visibles de los tipos de arma; la UI va en español.
public static class WeaponTypes
{
    public static string DisplayName(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Sword: return "Espada";
            case WeaponType.Spear: return "Lanza";
            case WeaponType.Bow: return "Arco";
            case WeaponType.Shield: return "Escudo";
            case WeaponType.Armor: return "Armadura";
            case WeaponType.Accessory: return "Accesorio";
        }
        return "Sin arma";
    }
}

[CreateAssetMenu(fileName = "Equip_New", menuName = "Endless Pull/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    [Tooltip("Nombre visible de la pieza.")]
    public string equipName = "Espada de Hierro";

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

    // Texto corto para el roster: nombre y lo que aporta.
    public string ShortLabel()
    {
        string bonus = string.Empty;
        if (bonusATK != 0) bonus += $" +{bonusATK}ATK";
        if (bonusDEF != 0) bonus += $" +{bonusDEF}DEF";
        if (bonusHP != 0) bonus += $" +{bonusHP}HP";

        return equipName + bonus;
    }
}
