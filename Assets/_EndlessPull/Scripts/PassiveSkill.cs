using System.Collections.Generic;
using UnityEngine;

// Aptitudes del héroe: unas salen sorteadas al invocarlo y el resto se despiertan peleando en la
// Torre o entrenando en el muñeco. Un héroe nunca repite pasiva.
public enum PassiveSkill
{
    // Ofensivas
    PainTolerance = 0,
    Evasion = 1,
    EagleEye = 2,
    Bloodlust = 3,
    Precision = 4,
    Executioner = 5,
    ArmorBreaker = 6,
    Vampiric = 7,
    Swiftblade = 8,
    Berserker = 9,
    Initiative = 10,

    // Defensivas
    IronSkin = 11,
    Vitality = 12,
    LastStand = 13,
    Regeneration = 14,
    Counterstrike = 15,
    Bulwark = 16,
    Unbreakable = 17,

    // Tácticas y utilidad
    Tactician = 18,
    ManaFlow = 19,
    ArcaneEconomy = 20,
    Fleetfoot = 21,
    Scout = 22,
    Opportunist = 23,
    Duelist = 24,
    Ambusher = 25,

    // Carácter, al estilo de las aptitudes del manhwa
    MentalFortitude = 26,
    Adaptability = 27,
    MonstrousGrowth = 28,
    Leadership = 29,
    Judgement = 30,
    Observation = 31,
    Indomitable = 32,
    Strategist = 33,

    // Sinergias de escuadra: miran a los compañeros, no al propio héroe.
    Vanguard = 34,
    Bannerman = 35,
    Lonewolf = 36
}

// Lo que aporta una pasiva, por canales. Los que multiplican arrancan en 1 y los que suman en 0,
// así que una pasiva solo declara lo que de verdad toca.
public class PassiveMods
{
    public float Attack = 1f;
    public float Defense = 1f;
    public float MaxHealth = 1f;
    public float AttackCooldown = 1f;
    public float MoveSpeed = 1f;
    public float Fatigue = 1f;
    public float MoraleLoss = 1f;
    public float MpCost = 1f;
    public float ManaRegen = 1f;
    public float Healing = 1f;
    public float Exp = 1f;

    public float Evasion;
    public float CritChance;
    public float CritDamage;
    public float LifeSteal;
    public float ArmorPierce;
    public float SkillCooldown;
    public float DetectionRange;

    // Aguanta a 1 de vida el primer golpe mortal de cada combate.
    public bool LastStand;

    // Pega más fuerte cuanto peor está; el umbral y el bonus los pone HeroController.
    public bool Berserk;

    // --- Canales de disposición: cambian CÓMO decide el héroe, no cuánto pega ---

    // Empuja el perfil táctico (agresividad / distancia segura / umbral de habilidad) del héroe.
    // Suman sobre el valor guardado en HeroProgress, que sigue siendo la base.
    public float Aggression;
    public float SafeDistance;
    public float SkillThreshold;

    // Aguanta la línea aunque su velocidad le permitiera flanquear: disciplina por encima de
    // oportunidad. Es lo contrario de lanzarse a rodear en cuanto se puede.
    public bool HoldsLine;

    // Lee el campo y va a por el objetivo que más conviene (el más tocado) en vez del más
    // cercano, que es lo que hace todo el mundo por defecto.
    public bool ReadsField;

    // Se crece con los compañeros al lado y se encoge solo; el número lo pone HeroController.
    public bool Leads;

    // No pierde la cabeza: la moral y la fatiga le afectan menos a la hora de decidir.
    public bool KeepsCool;

    // --- Sinergias de escuadra: por cada compañero cerca, no por sí mismo ---
    public float SquadAttack;
    public float SquadDefense;

    // Al revés: rinde más cuanto MENOS compañeros tenga al lado.
    public float SoloBonus;
}

// Tabla central de pasivas, al estilo de HeroTraits.
public static class PassiveSkills
{
    public static readonly PassiveSkill[] All =
    {
        PassiveSkill.PainTolerance, PassiveSkill.Evasion, PassiveSkill.EagleEye,
        PassiveSkill.Bloodlust, PassiveSkill.Precision, PassiveSkill.Executioner,
        PassiveSkill.ArmorBreaker, PassiveSkill.Vampiric, PassiveSkill.Swiftblade,
        PassiveSkill.Berserker, PassiveSkill.Initiative,

        PassiveSkill.IronSkin, PassiveSkill.Vitality, PassiveSkill.LastStand,
        PassiveSkill.Regeneration, PassiveSkill.Counterstrike, PassiveSkill.Bulwark,
        PassiveSkill.Unbreakable,

        PassiveSkill.Tactician, PassiveSkill.ManaFlow, PassiveSkill.ArcaneEconomy,
        PassiveSkill.Fleetfoot, PassiveSkill.Scout, PassiveSkill.Opportunist,
        PassiveSkill.Duelist, PassiveSkill.Ambusher,

        PassiveSkill.MentalFortitude, PassiveSkill.Adaptability, PassiveSkill.MonstrousGrowth,
        PassiveSkill.Leadership, PassiveSkill.Judgement, PassiveSkill.Observation,
        PassiveSkill.Indomitable, PassiveSkill.Strategist,

        PassiveSkill.Vanguard, PassiveSkill.Bannerman, PassiveSkill.Lonewolf
    };

    // Neutro compartido: lo devuelve Mods() para una pasiva sin entrada, y así nadie tiene que
    // comprobar null antes de leer un canal.
    private static readonly PassiveMods Neutral = new PassiveMods();

    // Qué hace cada pasiva. Los números viven aquí y en ningún otro sitio.
    private static readonly Dictionary<PassiveSkill, PassiveMods> Table =
        new Dictionary<PassiveSkill, PassiveMods>
    {
        // --- Ofensivas ---
        { PassiveSkill.PainTolerance, new PassiveMods { Fatigue = 0.5f } },
        { PassiveSkill.Evasion,       new PassiveMods { Evasion = 0.15f } },
        { PassiveSkill.EagleEye,      new PassiveMods { DetectionRange = 3f } },
        { PassiveSkill.Bloodlust,     new PassiveMods { Attack = 1.12f, Aggression = 0.25f,
                                                SafeDistance = -0.15f } },
        { PassiveSkill.Precision,     new PassiveMods { CritChance = 0.10f } },
        { PassiveSkill.Executioner,   new PassiveMods { CritDamage = 0.35f, SkillThreshold = 0.25f,
                                                ReadsField = true } },
        { PassiveSkill.ArmorBreaker,  new PassiveMods { ArmorPierce = 0.12f } },
        { PassiveSkill.Vampiric,      new PassiveMods { LifeSteal = 0.08f } },
        { PassiveSkill.Swiftblade,    new PassiveMods { AttackCooldown = 0.85f } },
        { PassiveSkill.Berserker,     new PassiveMods { Berserk = true } },
        { PassiveSkill.Initiative,    new PassiveMods { CritChance = 0.06f, CritDamage = 0.15f } },

        // --- Defensivas ---
        { PassiveSkill.IronSkin,      new PassiveMods { Defense = 1.20f } },
        { PassiveSkill.Vitality,      new PassiveMods { MaxHealth = 1.15f } },
        { PassiveSkill.LastStand,     new PassiveMods { LastStand = true } },
        { PassiveSkill.Regeneration,  new PassiveMods { Healing = 1.60f } },
        { PassiveSkill.Counterstrike, new PassiveMods { Evasion = 0.10f, Attack = 1.08f } },
        { PassiveSkill.Bulwark,       new PassiveMods { Defense = 1.14f, MoveSpeed = 0.90f,
                                                HoldsLine = true, Aggression = -0.15f } },
        { PassiveSkill.Unbreakable,   new PassiveMods { MaxHealth = 1.10f, Defense = 1.08f } },

        // --- Tácticas y utilidad ---
        { PassiveSkill.Tactician,     new PassiveMods { SkillCooldown = 0.15f, HoldsLine = true } },
        { PassiveSkill.ManaFlow,      new PassiveMods { ManaRegen = 1.50f } },
        { PassiveSkill.ArcaneEconomy, new PassiveMods { MpCost = 0.75f } },
        { PassiveSkill.Fleetfoot,     new PassiveMods { MoveSpeed = 1.20f } },
        { PassiveSkill.Scout,         new PassiveMods { MoveSpeed = 1.10f, DetectionRange = 2f,
                                                SafeDistance = 0.15f } },
        { PassiveSkill.Opportunist,   new PassiveMods { CritChance = 0.08f, ArmorPierce = 0.10f } },
        { PassiveSkill.Duelist,       new PassiveMods { Attack = 1.10f, Evasion = 0.08f,
                                                Aggression = 0.15f, ReadsField = true } },
        { PassiveSkill.Ambusher,      new PassiveMods { Attack = 1.08f, AttackCooldown = 0.92f,
                                                Aggression = 0.20f } },

        // --- Carácter ---
        { PassiveSkill.MentalFortitude, new PassiveMods { MoraleLoss = 0.5f, KeepsCool = true } },
        { PassiveSkill.Adaptability,    new PassiveMods { Attack = 1.05f, Defense = 1.05f,
                                                  ReadsField = true } },
        { PassiveSkill.MonstrousGrowth, new PassiveMods { Exp = 1.40f } },
        { PassiveSkill.Leadership,      new PassiveMods { Attack = 1.06f, MoraleLoss = 0.85f,
                                                  Leads = true } },
        { PassiveSkill.Judgement,       new PassiveMods { CritChance = 0.05f, CritDamage = 0.15f,
                                                  SkillThreshold = 0.20f } },
        { PassiveSkill.Observation,     new PassiveMods { DetectionRange = 2.5f, CritDamage = 0.10f,
                                                  ReadsField = true } },
        { PassiveSkill.Indomitable,     new PassiveMods { MaxHealth = 1.12f, Fatigue = 0.70f,
                                                  KeepsCool = true, Aggression = 0.15f } },
        { PassiveSkill.Strategist,      new PassiveMods { SkillCooldown = 0.12f, Attack = 1.04f,
                                                  HoldsLine = true, ReadsField = true } },

        // --- Sinergias de escuadra. El número lo pone HeroController contando compañeros. ---
        { PassiveSkill.Vanguard,        new PassiveMods { SquadAttack = 0.05f, HoldsLine = true } },
        { PassiveSkill.Bannerman,       new PassiveMods { SquadDefense = 0.05f, Leads = true } },
        { PassiveSkill.Lonewolf,        new PassiveMods { SoloBonus = 0.30f, Aggression = 0.20f } }
    };

    public static PassiveMods Mods(PassiveSkill passive)
        => Table.TryGetValue(passive, out var mods) ? mods : Neutral;

    // --- Canales agregados. Los llama HeroController; multiplican o suman según el canal. ---

    public static float AttackMultiplier(IReadOnlyList<PassiveSkill> p) => Product(p, m => m.Attack);
    public static float DefenseMultiplier(IReadOnlyList<PassiveSkill> p) => Product(p, m => m.Defense);
    public static float MaxHealthMultiplier(IReadOnlyList<PassiveSkill> p) => Product(p, m => m.MaxHealth);
    public static float AttackCooldownMultiplier(IReadOnlyList<PassiveSkill> p) => Product(p, m => m.AttackCooldown);
    public static float MoveSpeedMultiplier(IReadOnlyList<PassiveSkill> p) => Product(p, m => m.MoveSpeed);
    public static float FatigueMultiplier(IReadOnlyList<PassiveSkill> p) => Product(p, m => m.Fatigue);
    public static float MoraleLossMultiplier(IReadOnlyList<PassiveSkill> p) => Product(p, m => m.MoraleLoss);
    public static float MpCostMultiplier(IReadOnlyList<PassiveSkill> p) => Product(p, m => m.MpCost);
    public static float ManaRegenMultiplier(IReadOnlyList<PassiveSkill> p) => Product(p, m => m.ManaRegen);
    public static float HealingMultiplier(IReadOnlyList<PassiveSkill> p) => Product(p, m => m.Healing);
    public static float ExpMultiplier(IReadOnlyList<PassiveSkill> p) => Product(p, m => m.Exp);

    public static float EvasionBonus(IReadOnlyList<PassiveSkill> p) => Sum(p, m => m.Evasion);
    public static float CritChanceBonus(IReadOnlyList<PassiveSkill> p) => Sum(p, m => m.CritChance);
    public static float CritDamageBonus(IReadOnlyList<PassiveSkill> p) => Sum(p, m => m.CritDamage);
    public static float LifeStealBonus(IReadOnlyList<PassiveSkill> p) => Sum(p, m => m.LifeSteal);
    public static float ArmorPierceBonus(IReadOnlyList<PassiveSkill> p) => Sum(p, m => m.ArmorPierce);
    public static float SkillCooldownBonus(IReadOnlyList<PassiveSkill> p) => Sum(p, m => m.SkillCooldown);
    public static float DetectionRangeBonus(IReadOnlyList<PassiveSkill> p) => Sum(p, m => m.DetectionRange);

    public static bool HasLastStand(IReadOnlyList<PassiveSkill> p) => Any(p, m => m.LastStand);
    public static bool HasBerserk(IReadOnlyList<PassiveSkill> p) => Any(p, m => m.Berserk);

    // --- Disposición ---
    public static float AggressionBonus(IReadOnlyList<PassiveSkill> p) => Sum(p, m => m.Aggression);
    public static float SafeDistanceBonus(IReadOnlyList<PassiveSkill> p) => Sum(p, m => m.SafeDistance);
    public static float SkillThresholdBonus(IReadOnlyList<PassiveSkill> p) => Sum(p, m => m.SkillThreshold);

    public static bool HoldsLine(IReadOnlyList<PassiveSkill> p) => Any(p, m => m.HoldsLine);
    public static bool ReadsField(IReadOnlyList<PassiveSkill> p) => Any(p, m => m.ReadsField);
    public static bool Leads(IReadOnlyList<PassiveSkill> p) => Any(p, m => m.Leads);
    public static bool KeepsCool(IReadOnlyList<PassiveSkill> p) => Any(p, m => m.KeepsCool);

    public static float SquadAttackPerAlly(IReadOnlyList<PassiveSkill> p) => Sum(p, m => m.SquadAttack);
    public static float SquadDefensePerAlly(IReadOnlyList<PassiveSkill> p) => Sum(p, m => m.SquadDefense);
    public static float SoloBonus(IReadOnlyList<PassiveSkill> p) => Sum(p, m => m.SoloBonus);

    private static float Product(IReadOnlyList<PassiveSkill> passives, System.Func<PassiveMods, float> canal)
    {
        float total = 1f;
        if (passives == null) return total;

        for (int i = 0; i < passives.Count; i++) total *= canal(Mods(passives[i]));
        return total;
    }

    private static float Sum(IReadOnlyList<PassiveSkill> passives, System.Func<PassiveMods, float> canal)
    {
        float total = 0f;
        if (passives == null) return total;

        for (int i = 0; i < passives.Count; i++) total += canal(Mods(passives[i]));
        return total;
    }

    private static bool Any(IReadOnlyList<PassiveSkill> passives, System.Func<PassiveMods, bool> canal)
    {
        if (passives == null) return false;

        for (int i = 0; i < passives.Count; i++) if (canal(Mods(passives[i]))) return true;
        return false;
    }

    // El nombre y la descripción salen del diccionario; la clave es el propio valor del enum.
    public static string DisplayName(PassiveSkill passive)
        => LocalizationManager.Get("PASSIVE_NAME_" + passive.ToString().ToUpperInvariant());

    public static string Description(PassiveSkill passive)
        => LocalizationManager.Get("PASSIVE_DESC_" + passive.ToString().ToUpperInvariant());

    // Despierta una pasiva nueva del pool que el héroe aún no tenga; devuelve false si ya las
    // tiene todas. Lo llaman la Torre (al superar un piso) y el muñeco de entrenamiento.
    public static bool TryAwaken(HeroController hero)
    {
        if (hero == null || hero.Data == null) return false;

        var pool = new List<PassiveSkill>(All);
        pool.RemoveAll(hero.HasPassive);
        if (pool.Count == 0) return false;

        var chosen = pool[Random.Range(0, pool.Count)];
        var updated = new List<PassiveSkill>(hero.Passives) { chosen };
        hero.SetPassives(updated);

        string msg = string.Format(LocalizationManager.Get("UI_SKILL_AWAKENING"),
            hero.Data.heroName, DisplayName(chosen));
        ScreenBanner.ShowCompact(msg, 3f, UITheme.AccentPick);
        Debug.Log($"[Despertar] {msg}", hero);
        return true;
    }

    // Estrellas a partir de las cuales un héroe puede nacer ya con alguna pasiva. Un 1★ no trae
    // ninguna: si quiere una, se la gana peleando o entrenando.
    public const int MinStarRankForInnate = 2;

    // Probabilidad de que un 2★ nazca con una; por encima de esa rareza siempre traen alguna.
    private const float TwoStarChance = 0.6f;

    // Sortea las pasivas de salida de un héroe recién invocado: cuantas más estrellas, más
    // arranca sabiendo. El resto las tiene que despertar peleando o entrenando.
    public static List<PassiveSkill> RandomSet(int starRank = 1)
    {
        var pool = new List<PassiveSkill>(All);
        var picked = new List<PassiveSkill>();

        if (starRank < MinStarRankForInnate) return picked;

        // 2★ una o ninguna, 3★-4★ una o dos, 5★+ dos o tres.
        int count = starRank >= 5 ? Random.Range(2, 4)
                  : starRank >= 3 ? Random.Range(1, 3)
                  : (Random.value < TwoStarChance ? 1 : 0);

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            picked.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return picked;
    }

    public static string Describe(IReadOnlyList<PassiveSkill> passives)
    {
        if (passives == null || passives.Count == 0) return LocalizationManager.Get("UI_NO_PASSIVES");

        var names = new List<string>();
        foreach (var p in passives) names.Add(DisplayName(p));
        return string.Join(", ", names);
    }
}
