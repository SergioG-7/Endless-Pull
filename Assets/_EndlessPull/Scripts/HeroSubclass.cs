using System.Collections.Generic;
using UnityEngine;

// Subclases del héroe: tres por arquetipo de arma. None es "aún sin especializar".
public enum HeroSubclass
{
    None = 0,

    ShadowBlade = 1,
    IronBlade = 2,
    ZephyrBlade = 3,

    DragonLancer = 4,
    PikeGuard = 5,
    StormPiercer = 6,

    LightPaladin = 7,
    Juggernaut = 8,
    ImmortalBastion = 9,

    Sniper = 10,
    VolleyShooter = 11,
    ShadowHunter = 12,

    Pyromancer = 13,
    Chronomage = 14,
    ArcaneMage = 15,

    HighPriest = 16,
    ProtectiveOracle = 17,
    WarCleric = 18
}

// Tabla central de subclases: nombre, arquetipo, habilidad exclusiva y su efecto.
public static class HeroSubclasses
{
    // Estrellas a partir de las cuales se puede elegir subclase.
    public const int MinStarRank = 3;

    public static string DisplayName(HeroSubclass subclass)
    {
        switch (subclass)
        {
            case HeroSubclass.ShadowBlade: return "Espadachín Sombrío";
            case HeroSubclass.IronBlade: return "Espadachín Férreo";
            case HeroSubclass.ZephyrBlade: return "Espadachín Céfiro";
            case HeroSubclass.DragonLancer: return "Lancero Dragón";
            case HeroSubclass.PikeGuard: return "Guardia de Pica";
            case HeroSubclass.StormPiercer: return "Perforador de Tormenta";
            case HeroSubclass.LightPaladin: return "Paladín de la Luz";
            case HeroSubclass.Juggernaut: return "Juggernaut";
            case HeroSubclass.ImmortalBastion: return "Bastión Inmortal";
            case HeroSubclass.Sniper: return "Francotirador";
            case HeroSubclass.VolleyShooter: return "Tirador de Ráfaga";
            case HeroSubclass.ShadowHunter: return "Cazador de Sombras";
            case HeroSubclass.Pyromancer: return "Piromante";
            case HeroSubclass.Chronomage: return "Cronomago";
            case HeroSubclass.ArcaneMage: return "Mago Arcano";
            case HeroSubclass.HighPriest: return "Sumo Sacerdote";
            case HeroSubclass.ProtectiveOracle: return "Oráculo Protector";
            case HeroSubclass.WarCleric: return "Clérigo de Guerra";
        }
        return "sin subclase";
    }

    public static WeaponType ArchetypeOf(HeroSubclass subclass)
    {
        switch (subclass)
        {
            case HeroSubclass.ShadowBlade:
            case HeroSubclass.IronBlade:
            case HeroSubclass.ZephyrBlade: return WeaponType.Sword;

            case HeroSubclass.DragonLancer:
            case HeroSubclass.PikeGuard:
            case HeroSubclass.StormPiercer: return WeaponType.Spear;

            case HeroSubclass.LightPaladin:
            case HeroSubclass.Juggernaut:
            case HeroSubclass.ImmortalBastion: return WeaponType.Shield;

            case HeroSubclass.Sniper:
            case HeroSubclass.VolleyShooter:
            case HeroSubclass.ShadowHunter: return WeaponType.Bow;

            case HeroSubclass.Pyromancer:
            case HeroSubclass.Chronomage:
            case HeroSubclass.ArcaneMage: return WeaponType.Staff;

            case HeroSubclass.HighPriest:
            case HeroSubclass.ProtectiveOracle:
            case HeroSubclass.WarCleric: return WeaponType.Mace;
        }
        return WeaponType.None;
    }

    // Las tres opciones del arquetipo; el arma que empuña el héroe decide cuáles ve.
    public static HeroSubclass[] OptionsFor(WeaponType archetype)
    {
        switch (archetype)
        {
            case WeaponType.Sword:
                return new[] { HeroSubclass.ShadowBlade, HeroSubclass.IronBlade, HeroSubclass.ZephyrBlade };
            case WeaponType.Spear:
                return new[] { HeroSubclass.DragonLancer, HeroSubclass.PikeGuard, HeroSubclass.StormPiercer };
            case WeaponType.Shield:
                return new[] { HeroSubclass.LightPaladin, HeroSubclass.Juggernaut, HeroSubclass.ImmortalBastion };
            case WeaponType.Bow:
                return new[] { HeroSubclass.Sniper, HeroSubclass.VolleyShooter, HeroSubclass.ShadowHunter };
            case WeaponType.Staff:
                return new[] { HeroSubclass.Pyromancer, HeroSubclass.Chronomage, HeroSubclass.ArcaneMage };
            case WeaponType.Mace:
                return new[] { HeroSubclass.HighPriest, HeroSubclass.ProtectiveOracle, HeroSubclass.WarCleric };
        }
        return new HeroSubclass[0];
    }

    // Las tres subclases de clérigo curan y protegen en vez de pegar.
    public static bool IsSupport(HeroSubclass subclass)
        => ArchetypeOf(subclass) == WeaponType.Mace;

    // Un tanque aguanta la línea: escudo equipado o subclase de escudo.
    public static bool IsTank(HeroSubclass subclass)
        => ArchetypeOf(subclass) == WeaponType.Shield;

    // Habilidad exclusiva de la subclase; sustituye a la genérica del prefab.
    public static HeroSkill MakeSkill(HeroSubclass subclass)
    {
        switch (subclass)
        {
            case HeroSubclass.ShadowBlade: return Skill("Corte Ponzoñoso", 18, 5f, 1.6f);
            case HeroSubclass.IronBlade: return Skill("Guardia de Hierro", 15, 8f, 1.2f);
            case HeroSubclass.ZephyrBlade: return Skill("Danza de Cortes", 22, 6f, 1.1f);

            case HeroSubclass.DragonLancer: return Skill("Lanza del Dragón", 25, 7f, 1.8f);
            case HeroSubclass.PikeGuard: return Skill("Empuje de Pica", 16, 5f, 1.3f);
            case HeroSubclass.StormPiercer: return Skill("Perforación Tormentosa", 24, 8f, 1.5f);

            case HeroSubclass.LightPaladin: return Skill("Llamada de la Luz", 20, 9f, 0.9f);
            case HeroSubclass.Juggernaut: return Skill("Embestida Imparable", 22, 7f, 1.4f);
            case HeroSubclass.ImmortalBastion: return Skill("Muro Inmortal", 18, 10f, 0.8f);

            case HeroSubclass.Sniper: return Skill("Disparo Cargado", 26, 8f, 2.6f);
            case HeroSubclass.VolleyShooter: return Skill("Lluvia de Flechas", 28, 9f, 1.0f);
            case HeroSubclass.ShadowHunter: return Skill("Saeta Sombría", 20, 6f, 1.4f);

            case HeroSubclass.Pyromancer: return Skill("Estallido Ígneo", 30, 8f, 1.5f);
            case HeroSubclass.Chronomage: return Skill("Fractura Temporal", 26, 10f, 0.7f);
            case HeroSubclass.ArcaneMage: return Skill("Rayo Arcano", 32, 7f, 1.2f);

            case HeroSubclass.HighPriest: return Skill("Bendición Mayor", 22, 6f, 0f);
            case HeroSubclass.ProtectiveOracle: return Skill("Égida del Oráculo", 26, 9f, 0f);
            case HeroSubclass.WarCleric: return Skill("Himno de Guerra", 24, 10f, 0f);
        }
        return null;
    }

    // Lo que hace la habilidad más allá del daño; el daño base lo aplica quien la lanza.
    public static string DescribeSkill(HeroSubclass subclass)
    {
        switch (subclass)
        {
            case HeroSubclass.ShadowBlade: return "envenena 6s";
            case HeroSubclass.IronBlade: return "escudo propio";
            case HeroSubclass.ZephyrBlade: return "tres cortes y sangrado";
            case HeroSubclass.DragonLancer: return "daño en hilera";
            case HeroSubclass.PikeGuard: return "empuja y ralentiza";
            case HeroSubclass.StormPiercer: return "ignora armadura y aturde";
            case HeroSubclass.LightPaladin: return "provoca y escuda a la escuadra";
            case HeroSubclass.Juggernaut: return "aturde y limpia fatiga";
            case HeroSubclass.ImmortalBastion: return "escudo enorme propio";
            case HeroSubclass.Sniper: return "crítico a distancia";
            case HeroSubclass.VolleyShooter: return "área y ralentiza";
            case HeroSubclass.ShadowHunter: return "veneno y retroceso";
            case HeroSubclass.Pyromancer: return "área ígnea";
            case HeroSubclass.Chronomage: return "ralentiza a todos";
            case HeroSubclass.ArcaneMage: return "gasta todo el maná";
            case HeroSubclass.HighPriest: return "cura al más herido";
            case HeroSubclass.ProtectiveOracle: return "escudos a la escuadra";
            case HeroSubclass.WarCleric: return "+ATK en área";
        }
        return string.Empty;
    }

    private static HeroSkill Skill(string name, int mpCost, float cooldown, float multiplier)
        => new HeroSkill { skillName = name, mpCost = mpCost, cooldown = cooldown, damageMultiplier = multiplier };

    // Sortea una subclase del arquetipo; la usa la ascensión cuando el jugador no elige.
    public static HeroSubclass RandomFor(WeaponType archetype)
    {
        var options = OptionsFor(archetype);
        if (options.Length == 0) return HeroSubclass.None;

        return options[Random.Range(0, options.Length)];
    }

    public static List<HeroSubclass> AllReal()
    {
        var list = new List<HeroSubclass>();
        for (int i = 1; i <= 18; i++) list.Add((HeroSubclass)i);
        return list;
    }
}
