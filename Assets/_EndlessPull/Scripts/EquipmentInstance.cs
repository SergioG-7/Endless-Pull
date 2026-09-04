using UnityEngine;

// Una pieza concreta del mundo. El EquipmentData es un ScriptableObject COMPARTIDO por todas
// las copias de esa pieza, así que nada propio de una copia (desgaste, nivel de mejora, el
// afijo que salió al forjarla) puede vivir ahí: vive aquí.
[System.Serializable]
public class EquipmentInstance
{
    // Cada nivel de mejora suma este tanto por uno sobre las cifras propias de la pieza.
    public const float BonusPerGearLevel = 0.12f;

    [Tooltip("Identidad de esta copia; la usa el guardado para no confundir dos piezas iguales.")]
    public string instanceId = string.Empty;

    [Tooltip("Asset base del que sale la pieza; nunca se modifica.")]
    public EquipmentData data;

    [Tooltip("Afijo extra ganado en la forja; None si salió sin bonus.")]
    public EquipmentAffix forgedAffix = EquipmentAffix.None;

    [Tooltip("Valor del afijo forjado, en porcentaje igual que el del asset.")]
    public float forgedValue;

    [Tooltip("Combates que le quedan antes de romperse.")]
    public int durability;

    [Tooltip("Nivel de mejora de esta pieza (+0 a +N).")]
    public int gearLevel;

    public EquipmentInstance() { }

    public EquipmentInstance(EquipmentData source)
    {
        data = source;
        instanceId = System.Guid.NewGuid().ToString();
        durability = source != null ? source.maxDurability : 0;
    }

    public bool IsValid => data != null;

    // Espejo de lo que antes se leía del asset: así el resto del código apenas cambia.
    public EquipmentSlot SlotType => data != null ? data.slotType : EquipmentSlot.Weapon;
    public WeaponType WeaponType => data != null ? data.weaponType : WeaponType.None;
    public int MaxDurability => data != null ? data.maxDurability : 0;
    public int Tier => data != null ? data.tier : 1;
    public bool IsBroken => durability <= 0;

    public string LocalizedName()
    {
        if (data == null) return string.Empty;

        // El nivel se enseña pegado al nombre, como en cualquier juego con mejoras: "Espada +3".
        return gearLevel > 0 ? $"{data.LocalizedName()} +{gearLevel}" : data.LocalizedName();
    }

    // Una pieza rota no aporta nada; el nivel de mejora escala lo que ya daba la pieza.
    private int Scaled(int baseValue)
    {
        if (baseValue == 0 || IsBroken) return 0;

        return Mathf.RoundToInt(baseValue * (1f + BonusPerGearLevel * Mathf.Max(0, gearLevel)));
    }

    public int BonusATK => data != null ? Scaled(data.bonusATK) : 0;
    public int BonusDEF => data != null ? Scaled(data.bonusDEF) : 0;
    public int BonusHP => data != null ? Scaled(data.bonusHP) : 0;

    // Cuánto aporta esta pieza a un afijo concreto: el del asset más el forjado, que pueden
    // ser el mismo y entonces se suman.
    public float AffixValue(EquipmentAffix affix)
    {
        if (affix == EquipmentAffix.None || data == null || IsBroken) return 0f;

        float total = 0f;
        if (data.passiveTrait == affix) total += data.passiveValue;
        if (forgedAffix == affix) total += forgedValue;
        return total;
    }

    public bool HasForgedAffix => forgedAffix != EquipmentAffix.None && forgedValue > 0f;

    public string ForgedAffixLabel()
        => HasForgedAffix ? EquipmentAffixes.Describe(forgedAffix, forgedValue) : string.Empty;

    // Texto corto para almacén y listados: nombre y lo que aporta ya escalado.
    public string ShortLabel()
    {
        if (data == null) return string.Empty;

        string bonus = string.Empty;
        if (BonusATK != 0) bonus += $" +{BonusATK}ATK";
        if (BonusDEF != 0) bonus += $" +{BonusDEF}DEF";
        if (BonusHP != 0) bonus += $" +{BonusHP}HP";
        if (data.HasAffix) bonus += $" [{data.AffixLabel()}]";
        if (HasForgedAffix) bonus += $" [{ForgedAffixLabel()}]";

        return LocalizedName() + bonus;
    }
}

// Una pieza tal y como se escribe en disco. Guarda el nombre del asset base más lo propio de
// esta copia; el asset se vuelve a resolver al cargar.
[System.Serializable]
public class EquipmentInstanceSaveData
{
    public string instanceId = string.Empty;
    public string assetName = string.Empty;
    public int slot;
    public int forgedAffix;
    public float forgedValue;
    public int durability;
    public int gearLevel;
}
