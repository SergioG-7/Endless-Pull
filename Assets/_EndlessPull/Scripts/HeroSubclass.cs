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
    WarCleric = 18,

    // Segunda hornada: dos más por arquetipo, para que dos héroes con la misma arma no acaben
    // siempre con la misma habilidad.
    BloodReaver = 19,
    Riposteur = 20,

    Halberdier = 21,
    Skewerer = 22,

    Sentinel = 23,
    Retributor = 24,

    Trapper = 25,
    WindArcher = 26,

    FrostWeaver = 27,
    Necromancer = 28,

    Exorcist = 29,
    BattleMonk = 30
}

// Tabla central de subclases: nombre, arquetipo, habilidad exclusiva y su efecto.
public static class HeroSubclasses
{
    // Estrellas a partir de las cuales se puede elegir subclase.
    public const int MinStarRank = 3;

    // El nombre sale del diccionario; la clave es el propio valor del enum.
    public static string DisplayName(HeroSubclass subclass)
    {
        if (subclass != HeroSubclass.None)
            return LocalizationManager.Get("SUB_" + subclass.ToString().ToUpperInvariant());

        return DisplayNameFallback(subclass);
    }

    private static string DisplayNameFallback(HeroSubclass subclass)
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
            case HeroSubclass.BloodReaver: return "Segador de Sangre";
            case HeroSubclass.Riposteur: return "Espadachín de Réplica";
            case HeroSubclass.Halberdier: return "Alabardero";
            case HeroSubclass.Skewerer: return "Empalador";
            case HeroSubclass.Sentinel: return "Centinela";
            case HeroSubclass.Retributor: return "Vengador";
            case HeroSubclass.Trapper: return "Trampero";
            case HeroSubclass.WindArcher: return "Arquero del Viento";
            case HeroSubclass.FrostWeaver: return "Tejedor de Escarcha";
            case HeroSubclass.Necromancer: return "Nigromante";
            case HeroSubclass.Exorcist: return "Exorcista";
            case HeroSubclass.BattleMonk: return "Monje Guerrero";
        }
        return "sin subclase";
    }

    public static WeaponType ArchetypeOf(HeroSubclass subclass)
    {
        switch (subclass)
        {
            case HeroSubclass.ShadowBlade:
            case HeroSubclass.IronBlade:
            case HeroSubclass.ZephyrBlade:
            case HeroSubclass.BloodReaver:
            case HeroSubclass.Riposteur: return WeaponType.Sword;

            case HeroSubclass.DragonLancer:
            case HeroSubclass.PikeGuard:
            case HeroSubclass.StormPiercer:
            case HeroSubclass.Halberdier:
            case HeroSubclass.Skewerer: return WeaponType.Spear;

            case HeroSubclass.LightPaladin:
            case HeroSubclass.Juggernaut:
            case HeroSubclass.ImmortalBastion:
            case HeroSubclass.Sentinel:
            case HeroSubclass.Retributor: return WeaponType.Shield;

            case HeroSubclass.Sniper:
            case HeroSubclass.VolleyShooter:
            case HeroSubclass.ShadowHunter:
            case HeroSubclass.Trapper:
            case HeroSubclass.WindArcher: return WeaponType.Bow;

            case HeroSubclass.Pyromancer:
            case HeroSubclass.Chronomage:
            case HeroSubclass.ArcaneMage:
            case HeroSubclass.FrostWeaver:
            case HeroSubclass.Necromancer: return WeaponType.Staff;

            case HeroSubclass.HighPriest:
            case HeroSubclass.ProtectiveOracle:
            case HeroSubclass.WarCleric:
            case HeroSubclass.Exorcist:
            case HeroSubclass.BattleMonk: return WeaponType.Mace;
        }
        return WeaponType.None;
    }

    // Las cinco subclases del arquetipo; el arma que empuña el héroe decide cuáles le tocan.
    public static HeroSubclass[] OptionsFor(WeaponType archetype)
    {
        switch (archetype)
        {
            case WeaponType.Sword:
                return new[] { HeroSubclass.ShadowBlade, HeroSubclass.IronBlade, HeroSubclass.ZephyrBlade,
                               HeroSubclass.BloodReaver, HeroSubclass.Riposteur };
            case WeaponType.Spear:
                return new[] { HeroSubclass.DragonLancer, HeroSubclass.PikeGuard, HeroSubclass.StormPiercer,
                               HeroSubclass.Halberdier, HeroSubclass.Skewerer };
            case WeaponType.Shield:
                return new[] { HeroSubclass.LightPaladin, HeroSubclass.Juggernaut, HeroSubclass.ImmortalBastion,
                               HeroSubclass.Sentinel, HeroSubclass.Retributor };
            case WeaponType.Bow:
                return new[] { HeroSubclass.Sniper, HeroSubclass.VolleyShooter, HeroSubclass.ShadowHunter,
                               HeroSubclass.Trapper, HeroSubclass.WindArcher };
            case WeaponType.Staff:
                return new[] { HeroSubclass.Pyromancer, HeroSubclass.Chronomage, HeroSubclass.ArcaneMage,
                               HeroSubclass.FrostWeaver, HeroSubclass.Necromancer };
            case WeaponType.Mace:
                return new[] { HeroSubclass.HighPriest, HeroSubclass.ProtectiveOracle, HeroSubclass.WarCleric,
                               HeroSubclass.Exorcist, HeroSubclass.BattleMonk };
        }
        return new HeroSubclass[0];
    }

    // Cartas que se ofrecen de golpe en el modal de elección; el pool es mayor que la oferta,
    // así que dos héroes con la misma arma no ven la misma mano.
    public const int OfferedOptions = 3;

    // Rareza de nacimiento (la del gacha, sin contar ascensiones) a partir de la cual se puede
    // optar a Mago Arcano; por debajo, esa carta no se ofrece nunca, aunque el héroe ascienda
    // después a 3★+ y empuñe báculo.
    public const int MageMinBirthStarRank = 3;

    // Igual que OptionsFor(archetype), pero le quita Mago Arcano a un héroe nacido plebeyo
    // (1★/2★) — el resto de arquetipos no incluyen esa subclase, así que no les afecta.
    public static HeroSubclass[] OptionsFor(WeaponType archetype, int birthStarRank)
    {
        var elegibles = new List<HeroSubclass>(OptionsFor(archetype));
        if (birthStarRank < MageMinBirthStarRank) elegibles.Remove(HeroSubclass.ArcaneMage);

        // Baraja y corta: la mano sale del pool completo, no siempre las mismas tres.
        for (int i = elegibles.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (elegibles[i], elegibles[j]) = (elegibles[j], elegibles[i]);
        }

        if (elegibles.Count > OfferedOptions) elegibles.RemoveRange(OfferedOptions, elegibles.Count - OfferedOptions);
        return elegibles.ToArray();
    }

    // Las tres subclases de clérigo curan y protegen en vez de pegar.
    public static bool IsSupport(HeroSubclass subclass)
        => ArchetypeOf(subclass) == WeaponType.Mace;

    // Un tanque aguanta la línea: escudo equipado o subclase de escudo.
    public static bool IsTank(HeroSubclass subclass)
        => ArchetypeOf(subclass) == WeaponType.Shield;

    // Habilidad exclusiva de la subclase; sustituye a la genérica del prefab.
    // El nombre en español es solo el respaldo: el visible sale de GetDisplayName().
    public static HeroSkill MakeSkill(HeroSubclass subclass)
    {
        var hecha = Build(subclass);
        if (hecha != null) hecha.subclass = subclass;

        return hecha;
    }

    // Habilidad de rol con la que arranca cada subclase. La habilidad ya es una entidad
    // aparte: esto solo dice con cuál empieza, no a cuál queda atada para siempre.
    public static ActiveSkill DefaultAbility(HeroSubclass subclass)
    {
        switch (subclass)
        {
            case HeroSubclass.ShadowBlade: return ActiveSkill.PoisonCut;
            case HeroSubclass.IronBlade: return ActiveSkill.IronGuard;
            case HeroSubclass.ZephyrBlade: return ActiveSkill.BladeDance;
            case HeroSubclass.BloodReaver: return ActiveSkill.BloodHarvest;
            case HeroSubclass.Riposteur: return ActiveSkill.Riposte;
            case HeroSubclass.DragonLancer: return ActiveSkill.DragonThrust;
            case HeroSubclass.PikeGuard: return ActiveSkill.PikePush;
            case HeroSubclass.StormPiercer: return ActiveSkill.StormPierce;
            case HeroSubclass.Halberdier: return ActiveSkill.HalberdSweep;
            case HeroSubclass.Skewerer: return ActiveSkill.Skewer;
            case HeroSubclass.LightPaladin: return ActiveSkill.LightCall;
            case HeroSubclass.Juggernaut: return ActiveSkill.UnstoppableCharge;
            case HeroSubclass.ImmortalBastion: return ActiveSkill.ImmortalWall;
            case HeroSubclass.Sentinel: return ActiveSkill.SentinelWatch;
            case HeroSubclass.Retributor: return ActiveSkill.Retribution;
            case HeroSubclass.Sniper: return ActiveSkill.ChargedShot;
            case HeroSubclass.VolleyShooter: return ActiveSkill.ArrowRain;
            case HeroSubclass.ShadowHunter: return ActiveSkill.ShadowBolt;
            case HeroSubclass.Trapper: return ActiveSkill.HuntingSnare;
            case HeroSubclass.WindArcher: return ActiveSkill.WindVolley;
            case HeroSubclass.Pyromancer: return ActiveSkill.FireBurst;
            case HeroSubclass.Chronomage: return ActiveSkill.TimeFracture;
            case HeroSubclass.ArcaneMage: return ActiveSkill.ArcaneRay;
            case HeroSubclass.FrostWeaver: return ActiveSkill.FrostShroud;
            case HeroSubclass.Necromancer: return ActiveSkill.WitheringTouch;
            case HeroSubclass.HighPriest: return ActiveSkill.GreaterBlessing;
            case HeroSubclass.ProtectiveOracle: return ActiveSkill.OracleAegis;
            case HeroSubclass.WarCleric: return ActiveSkill.WarHymn;
            case HeroSubclass.Exorcist: return ActiveSkill.PurgingRite;
            case HeroSubclass.BattleMonk: return ActiveSkill.IronPalm;
        }
        return ActiveSkill.BasicStrike;
    }

    private static HeroSkill Build(HeroSubclass subclass)
        => ActiveSkills.Make(DefaultAbility(subclass));

    // Lo que hace la habilidad más allá del daño; el daño base lo aplica quien la lanza.
    public static string DescribeSkill(HeroSubclass subclass)
    {
        if (subclass != HeroSubclass.None)
            return LocalizationManager.Get("SKILLDESC_" + subclass.ToString().ToUpperInvariant());

        return DescribeSkillFallback(subclass);
    }

    private static string DescribeSkillFallback(HeroSubclass subclass)
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
            case HeroSubclass.BloodReaver: return "sangrado fuerte y roba vida";
            case HeroSubclass.Riposteur: return "golpe doble, el segundo ignora armadura";
            case HeroSubclass.Halberdier: return "barrido en área y empuja";
            case HeroSubclass.Skewerer: return "hilera con sangrado";
            case HeroSubclass.Sentinel: return "escuda a la escuadra y provoca";
            case HeroSubclass.Retributor: return "más daño cuanto peor está";
            case HeroSubclass.Trapper: return "inmoviliza y ralentiza";
            case HeroSubclass.WindArcher: return "tres flechas a objetivos distintos";
            case HeroSubclass.FrostWeaver: return "congela el área";
            case HeroSubclass.Necromancer: return "veneno mágico y roba vida";
            case HeroSubclass.Exorcist: return "cura y limpia estados";
            case HeroSubclass.BattleMonk: return "cura en área y se cura al pegar";
        }
        return string.Empty;
    }

    private static HeroSkill Skill(string name, int mpCost, float cooldown, float multiplier)
        => new HeroSkill { skillName = name, mpCost = mpCost, cooldown = cooldown, damageMultiplier = multiplier };

    // Nombre y efecto salen del diccionario; la clave es el propio valor del enum.
    public static string SkillName(HeroSubclass subclass)
        => subclass == HeroSubclass.None
           ? string.Empty
           : LocalizationManager.Get("SKILL_" + subclass.ToString().ToUpperInvariant());

    public static string RoleDescription(HeroSubclass subclass)
        => subclass == HeroSubclass.None
           ? string.Empty
           : LocalizationManager.Get("ROLE_" + subclass.ToString().ToUpperInvariant());

    // Peso relativo de Mago Arcano frente al resto (peso 1) cuando sí puede salir — su ratio de
    // aparición es mucho más bajo incluso entre los héroes elegibles (3★+ de nacimiento).
    private const float ArcaneMageWeight = 0.3f;

    // Sortea una subclase del arquetipo; la usa la ascensión cuando el jugador no elige.
    public static HeroSubclass RandomFor(WeaponType archetype, int birthStarRank)
    {
        var options = OptionsFor(archetype, birthStarRank);
        if (options.Length == 0) return HeroSubclass.None;

        float total = 0f;
        foreach (var opt in options) total += opt == HeroSubclass.ArcaneMage ? ArcaneMageWeight : 1f;

        float roll = Random.value * total;
        float acumulado = 0f;
        foreach (var opt in options)
        {
            acumulado += opt == HeroSubclass.ArcaneMage ? ArcaneMageWeight : 1f;
            if (roll <= acumulado) return opt;
        }

        return options[options.Length - 1];
    }

    public static List<HeroSubclass> AllReal()
    {
        var list = new List<HeroSubclass>();
        for (int i = 1; i <= (int)HeroSubclass.BattleMonk; i++) list.Add((HeroSubclass)i);
        return list;
    }
}
