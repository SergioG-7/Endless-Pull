using System.Collections.Generic;
using UnityEngine;

// Catálogo de habilidades activas, independiente de la subclase. La subclase decide con cuál
// arranca el héroe (su habilidad de rol), pero una habilidad puede acabar en cualquier héroe:
// aprenderla no exige cambiar de subclase.
public enum ActiveSkill
{
    None = 0,
    BasicStrike = 1,

    // Espada
    PoisonCut = 2,
    IronGuard = 3,
    BladeDance = 4,
    BloodHarvest = 5,
    Riposte = 6,

    // Lanza
    DragonThrust = 7,
    PikePush = 8,
    StormPierce = 9,
    HalberdSweep = 10,
    Skewer = 11,

    // Escudo
    LightCall = 12,
    UnstoppableCharge = 13,
    ImmortalWall = 14,
    SentinelWatch = 15,
    Retribution = 16,

    // Arco
    ChargedShot = 17,
    ArrowRain = 18,
    ShadowBolt = 19,
    HuntingSnare = 20,
    WindVolley = 21,

    // Báculo
    FireBurst = 22,
    TimeFracture = 23,
    ArcaneRay = 24,
    FrostShroud = 25,
    WitheringTouch = 26,

    // Maza (soporte)
    GreaterBlessing = 27,
    OracleAegis = 28,
    WarHymn = 29,
    PurgingRite = 30,
    IronPalm = 31
}

// Para qué sirve una habilidad. Sin esto la única forma de elegir era "la que más pega", que
// tira la curación y el área a la basura.
public enum SkillRole
{
    SingleTarget,   // daño a uno
    Area,           // pega a varios: vale la pena con enemigos agrupados
    Execute,        // remata: se guarda para el que está a punto de caer
    Defensive,      // aguante propio: se usa estando tocado
    Heal            // cura a la escuadra
}

// Tabla central de habilidades: coste, enfriamiento, multiplicador y a qué arma pertenecen.
public static class ActiveSkills
{
    // Coste de maná, enfriamiento en segundos y multiplicador de daño sobre el ataque efectivo.
    private class Stats
    {
        public int Mp;
        public float Cooldown;
        public float Multiplier;
        public WeaponType Archetype;

        // Por defecto daño a uno; solo se declara cuando es otra cosa.
        public SkillRole Role;
    }

    private static readonly Dictionary<ActiveSkill, Stats> Table = new Dictionary<ActiveSkill, Stats>
    {
        { ActiveSkill.BasicStrike,       new Stats { Mp = 20, Cooldown = 5f,  Multiplier = 2.0f, Archetype = WeaponType.None } },

        { ActiveSkill.PoisonCut,         new Stats { Mp = 18, Cooldown = 5f,  Multiplier = 1.6f, Archetype = WeaponType.Sword } },
        { ActiveSkill.IronGuard,         new Stats { Mp = 15, Cooldown = 8f,  Multiplier = 1.2f, Archetype = WeaponType.Sword , Role = SkillRole.Defensive } },
        { ActiveSkill.BladeDance,        new Stats { Mp = 22, Cooldown = 6f,  Multiplier = 1.1f, Archetype = WeaponType.Sword , Role = SkillRole.Area } },
        { ActiveSkill.BloodHarvest,      new Stats { Mp = 20, Cooldown = 6f,  Multiplier = 1.5f, Archetype = WeaponType.Sword , Role = SkillRole.Execute } },
        { ActiveSkill.Riposte,           new Stats { Mp = 17, Cooldown = 5f,  Multiplier = 1.3f, Archetype = WeaponType.Sword , Role = SkillRole.Defensive } },

        { ActiveSkill.DragonThrust,      new Stats { Mp = 25, Cooldown = 7f,  Multiplier = 1.8f, Archetype = WeaponType.Spear } },
        { ActiveSkill.PikePush,          new Stats { Mp = 16, Cooldown = 5f,  Multiplier = 1.3f, Archetype = WeaponType.Spear } },
        { ActiveSkill.StormPierce,       new Stats { Mp = 24, Cooldown = 8f,  Multiplier = 1.5f, Archetype = WeaponType.Spear , Role = SkillRole.Area } },
        { ActiveSkill.HalberdSweep,      new Stats { Mp = 23, Cooldown = 7f,  Multiplier = 1.4f, Archetype = WeaponType.Spear , Role = SkillRole.Area } },
        { ActiveSkill.Skewer,            new Stats { Mp = 21, Cooldown = 6f,  Multiplier = 1.5f, Archetype = WeaponType.Spear } },

        { ActiveSkill.LightCall,         new Stats { Mp = 20, Cooldown = 9f,  Multiplier = 0.9f, Archetype = WeaponType.Shield , Role = SkillRole.Heal } },
        { ActiveSkill.UnstoppableCharge, new Stats { Mp = 22, Cooldown = 7f,  Multiplier = 1.4f, Archetype = WeaponType.Shield } },
        { ActiveSkill.ImmortalWall,      new Stats { Mp = 18, Cooldown = 10f, Multiplier = 0.8f, Archetype = WeaponType.Shield , Role = SkillRole.Defensive } },
        { ActiveSkill.SentinelWatch,     new Stats { Mp = 19, Cooldown = 9f,  Multiplier = 0.8f, Archetype = WeaponType.Shield , Role = SkillRole.Defensive } },
        { ActiveSkill.Retribution,       new Stats { Mp = 21, Cooldown = 8f,  Multiplier = 1.2f, Archetype = WeaponType.Shield } },

        { ActiveSkill.ChargedShot,       new Stats { Mp = 26, Cooldown = 8f,  Multiplier = 2.6f, Archetype = WeaponType.Bow } },
        { ActiveSkill.ArrowRain,         new Stats { Mp = 28, Cooldown = 9f,  Multiplier = 1.0f, Archetype = WeaponType.Bow , Role = SkillRole.Area } },
        { ActiveSkill.ShadowBolt,        new Stats { Mp = 20, Cooldown = 6f,  Multiplier = 1.4f, Archetype = WeaponType.Bow , Role = SkillRole.Execute } },
        { ActiveSkill.HuntingSnare,      new Stats { Mp = 18, Cooldown = 7f,  Multiplier = 1.3f, Archetype = WeaponType.Bow } },
        { ActiveSkill.WindVolley,        new Stats { Mp = 25, Cooldown = 8f,  Multiplier = 0.8f, Archetype = WeaponType.Bow , Role = SkillRole.Area } },

        { ActiveSkill.FireBurst,         new Stats { Mp = 30, Cooldown = 8f,  Multiplier = 1.5f, Archetype = WeaponType.Staff , Role = SkillRole.Area } },
        { ActiveSkill.TimeFracture,      new Stats { Mp = 26, Cooldown = 10f, Multiplier = 0.7f, Archetype = WeaponType.Staff } },
        { ActiveSkill.ArcaneRay,         new Stats { Mp = 32, Cooldown = 7f,  Multiplier = 1.2f, Archetype = WeaponType.Staff } },
        { ActiveSkill.FrostShroud,       new Stats { Mp = 27, Cooldown = 9f,  Multiplier = 1.1f, Archetype = WeaponType.Staff , Role = SkillRole.Defensive } },
        { ActiveSkill.WitheringTouch,    new Stats { Mp = 29, Cooldown = 8f,  Multiplier = 1.3f, Archetype = WeaponType.Staff , Role = SkillRole.Execute } },

        { ActiveSkill.GreaterBlessing,   new Stats { Mp = 22, Cooldown = 6f,  Multiplier = 0f,   Archetype = WeaponType.Mace , Role = SkillRole.Heal } },
        { ActiveSkill.OracleAegis,       new Stats { Mp = 26, Cooldown = 9f,  Multiplier = 0f,   Archetype = WeaponType.Mace , Role = SkillRole.Defensive } },
        { ActiveSkill.WarHymn,           new Stats { Mp = 24, Cooldown = 10f, Multiplier = 0f,   Archetype = WeaponType.Mace , Role = SkillRole.Heal } },
        { ActiveSkill.PurgingRite,       new Stats { Mp = 23, Cooldown = 8f,  Multiplier = 0f,   Archetype = WeaponType.Mace , Role = SkillRole.Area } },
        { ActiveSkill.IronPalm,          new Stats { Mp = 20, Cooldown = 7f,  Multiplier = 0f,   Archetype = WeaponType.Mace , Role = SkillRole.Heal } }
    };

    public static readonly ActiveSkill[] All = BuildAll();

    private static ActiveSkill[] BuildAll()
    {
        var lista = new List<ActiveSkill>();
        foreach (ActiveSkill s in System.Enum.GetValues(typeof(ActiveSkill)))
            if (s != ActiveSkill.None) lista.Add(s);

        return lista.ToArray();
    }

    // Arma a la que pertenece; None en el golpe genérico, que lo puede llevar cualquiera.
    public static WeaponType ArchetypeOf(ActiveSkill ability)
        => Table.TryGetValue(ability, out var s) ? s.Archetype : WeaponType.None;

    // Para qué sirve, en una palabra: es lo que explica por qué el héroe la lanza cuando la
    // lanza, en vez de parecer que elige al azar.
    public static string RoleName(SkillRole role)
    {
        switch (role)
        {
            case SkillRole.Area: return LocalizationManager.Get("ROLE_AREA");
            case SkillRole.Execute: return LocalizationManager.Get("ROLE_EXECUTE");
            case SkillRole.Defensive: return LocalizationManager.Get("ROLE_DEFENSIVE");
            case SkillRole.Heal: return LocalizationManager.Get("ROLE_HEAL");
        }
        return LocalizationManager.Get("ROLE_SINGLE");
    }

    public static SkillRole RoleOf(ActiveSkill ability)
        => Table.TryGetValue(ability, out var s) ? s.Role : SkillRole.SingleTarget;

    // Las de maza actúan sobre la escuadra, no sobre el enemigo.
    public static bool IsSupport(ActiveSkill ability) => ArchetypeOf(ability) == WeaponType.Mace;

    // Nombre y efecto salen del diccionario; la clave es el propio valor del enum.
    public static string DisplayName(ActiveSkill ability)
        => ability == ActiveSkill.None
           ? string.Empty
           : LocalizationManager.Get("ABILITY_" + ability.ToString().ToUpperInvariant());

    public static string Description(ActiveSkill ability)
        => ability == ActiveSkill.None
           ? string.Empty
           : LocalizationManager.Get("ABILITYDESC_" + ability.ToString().ToUpperInvariant());

    // Fabrica la habilidad lista para usar, con sus números de la tabla.
    public static HeroSkill Make(ActiveSkill ability)
    {
        if (ability == ActiveSkill.None || !Table.TryGetValue(ability, out var s)) return null;

        return new HeroSkill
        {
            ability = ability,
            skillName = ability.ToString(),
            mpCost = s.Mp,
            cooldown = s.Cooldown,
            damageMultiplier = s.Multiplier
        };
    }

    // Peso de la habilidad de rol frente al resto del arquetipo: sale la mitad de las veces,
    // y la otra mitad se reparte entre las demás.
    private const float RoleWeight = 5f;

    // Le enseña una habilidad más de su misma arma, de las que aún no lleva. Es lo que hace que
    // dos héroes que empezaron igual dejen de pelear igual.
    public static bool TryAwaken(HeroController hero)
    {
        if (hero == null || hero.Data == null) return false;

        // El arquetipo sale del arma que empuña; si no lleva, del que tenga aprendido.
        var arma = hero.EquippedWeaponType != WeaponType.None
            ? hero.EquippedWeaponType
            : ArchetypeOf(hero.Ability);

        var opciones = ForArchetype(arma);
        opciones.Remove(ActiveSkill.BasicStrike);

        foreach (var aprendida in hero.Skills)
            if (aprendida != null) opciones.Remove(aprendida.ability);

        if (opciones.Count == 0) return false;

        var elegida = opciones[Random.Range(0, opciones.Count)];
        hero.LearnAbility(elegida);

        string msg = string.Format(LocalizationManager.Get("UI_ABILITY_AWAKENING"),
            hero.Data.heroName, DisplayName(elegida));
        ScreenBanner.ShowCompact(msg, 3f, UITheme.AccentPick);
        VfxManager.Play(VfxId.Awaken, hero.transform.position);
        QuestManager.Report(QuestKind.AwakenSkill);
        Debug.Log($"[Despertar] {msg}", hero);
        return true;
    }

    // Las que puede llevar un héroe con esa arma: las de su arquetipo más el golpe genérico.
    public static List<ActiveSkill> ForArchetype(WeaponType archetype)
    {
        var lista = new List<ActiveSkill> { ActiveSkill.BasicStrike };

        foreach (var ability in All)
            if (ability != ActiveSkill.BasicStrike && ArchetypeOf(ability) == archetype) lista.Add(ability);

        return lista;
    }
}
