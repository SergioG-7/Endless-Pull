using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Estados del héroe: ciclo tranquilo en la base, visita a un edificio, o persecución y ataque.
public enum HeroState
{
    BaseIdle,
    BaseWander,
    Training,
    CombatApproach,
    CombatAttack
}

// Estado global de alto nivel (Fase 37): en qué "modo" está el héroe, más allá de su
// sub-estado interno de FSM. InCombat es el único que corta el vector hacia la base.
public enum HeroGlobalState
{
    InBase,
    HeadingToPortal,
    InCombat,
    OnExpedition
}

// Sub-estado de combate; solo tiene sentido mientras GlobalState == InCombat.
public enum CombatState
{
    IdleSearching,
    MovingToTarget,
    InRangeAttacking,
    Kiting,
    Stunned
}

// Cómo está de ánimo el héroe; sale de la moral y modifica ataque, velocidad y cadencia.
public enum MoraleState
{
    Demoralized,
    Steady,
    Inspired
}

public class HeroController : MonoBehaviour, IHealthOwner
{
    [Tooltip("Datos del héroe: stats, rareza y velocidad.")]
    [SerializeField] private HeroData data;

    [Tooltip("Personalidad: decide preferencias de edificio y bonus de combate.")]
    [SerializeField] private HeroTrait trait = HeroTrait.Diligent;

    [Tooltip("Alcance de ataque de arcos y báculos, que pegan sin acercarse.")]
    [SerializeField] private float rangedAttackRange = 4.5f;

    [Tooltip("Distancia de alcance cuerpo a cuerpo bajo la que un héroe a distancia se reposiciona.")]
    [SerializeField] private float meleeThreatRange = 2.5f;

    [Tooltip("Distancia del micro-paso de retirada tras encajar daño o esquivar (los tanques no lo dan).")]
    [SerializeField] private float microStepDistance = 0.4f;

    [Tooltip("Segundos que tarda el paseo entre el Portal de la Torre y el puesto final, al salir o volver.")]
    [SerializeField] private float gatewayTravelSeconds = 0.4f;

    [Tooltip("Segundos que la escuadra se queda quieta reunida en el Portal antes de partir o de volver al Idle.")]
    [SerializeField] private float gatewayHoldSeconds = 1.2f;

    [Tooltip("Segundos de inactividad tras los que un héroe menor cae en apatía.")]
    [SerializeField] private float apathyAfterSeconds = 90f;

    [Tooltip("Segundos entre cada bajón de moral por apatía.")]
    [SerializeField] private float apathyInterval = 15f;

    [Tooltip("Moral que se pierde en cada bajón de apatía.")]
    [SerializeField] private float apathyMoraleLoss = 5f;

    [Tooltip("Centro del área de la base por la que pasea.")]
    [SerializeField] private Vector2 baseAreaCenter = Vector2.zero;

    [Tooltip("Ancho y alto del área de paseo, en unidades.")]
    [SerializeField] private Vector2 baseAreaSize = new Vector2(8f, 5f);

    [Tooltip("Distancia a la que se considera que ya llegó al destino.")]
    [SerializeField] private float arriveThreshold = 0.05f;

    [Tooltip("Segundos mínimos y máximos de descanso al llegar.")]
    [SerializeField] private Vector2 restTimeRange = new Vector2(1f, 3f);

    [Tooltip("Probabilidad de ir a un edificio en vez de a un punto al azar.")]
    [Range(0f, 1f)]
    [SerializeField] private float buildingVisitChance = 0.6f;

    [Tooltip("Radio en el que el héroe detecta enemigos y entra en combate.")]
    [SerializeField] private float detectionRange = 45f;

    [Tooltip("Distancia a la que deja de acercarse y empieza a golpear.")]
    [SerializeField] private float attackRange = 1.2f;

    [Tooltip("Segundos entre golpe y golpe.")]
    [SerializeField] private float attackCooldown = 1f;

    [Tooltip("Cada cuántos segundos vuelve a buscar enemigos cercanos.")]
    [SerializeField] private float scanInterval = 0.25f;

    [Tooltip("Maná que se regenera por segundo fuera de combate.")]
    [SerializeField] private float mpRegenOutOfCombat = 2f;

    [Tooltip("Maná que se regenera por segundo en combate.")]
    [SerializeField] private float mpRegenInCombat = 1f;

    [Tooltip("Habilidad activa que gasta maná y entra en enfriamiento.")]
    [SerializeField] private HeroSkill skill = new HeroSkill();

    [Tooltip("Fatiga que se acumula por segundo moviéndose en combate.")]
    [SerializeField] private float fatiguePerSecondMoving = 2f;

    [Tooltip("Fatiga que suma cada golpe recibido.")]
    [SerializeField] private float fatiguePerHitTaken = 5f;

    [Tooltip("Fatiga que se quita por segundo descansando en la base.")]
    [SerializeField] private float fatigueRecoveryIdle = 5f;

    [Tooltip("Fatiga que se quita por segundo en la cantina o la zona de descanso.")]
    [SerializeField] private float fatigueRecoveryResting = 8f;

    [Tooltip("Fatiga a partir de la cual el héroe se considera agotado.")]
    [SerializeField] private float exhaustionThreshold = 80f;

    [Tooltip("Recorte de velocidad mientras está agotado.")]
    [Range(0f, 1f)]
    [SerializeField] private float exhaustionSpeedPenalty = 0.3f;

    [Tooltip("Segundos extra de enfriamiento de ataque mientras está agotado.")]
    [SerializeField] private float exhaustionAttackDelay = 0.3f;

    [Tooltip("Moral con la que arranca el héroe, de 0 a 100.")]
    [SerializeField] private float startingMorale = 80f;

    [Tooltip("Fracción de vida por debajo de la cual el héroe entra en estado crítico.")]
    [Range(0f, 1f)]
    [SerializeField] private float criticalHealthRatio = 0.25f;

    [Tooltip("Moral que se pierde al caer en estado crítico.")]
    [SerializeField] private float moraleLossOnCritical = 15f;

    [Tooltip("Moral que se pierde al ver morir a un aliado cercano.")]
    [SerializeField] private float moraleLossOnAllyDeath = 20f;

    [Tooltip("Radio en el que un héroe se entera de la muerte de un aliado.")]
    [SerializeField] private float allyDeathRadius = 5f;

    [Tooltip("Moral por encima de la cual el héroe está inspirado.")]
    [SerializeField] private float inspiredThreshold = 80f;

    [Tooltip("Moral por debajo de la cual el héroe está desmoralizado.")]
    [SerializeField] private float demoralizedThreshold = 30f;

    [Tooltip("Ataque extra en tanto por uno mientras está inspirado.")]
    [Range(0f, 1f)]
    [SerializeField] private float inspiredAttackBonus = 0.10f;

    [Tooltip("Recorte de velocidad mientras está desmoralizado.")]
    [Range(0f, 1f)]
    [SerializeField] private float demoralizedSpeedPenalty = 0.20f;

    [Tooltip("Segundos extra de enfriamiento de ataque mientras está desmoralizado.")]
    [SerializeField] private float demoralizedAttackDelay = 0.5f;

    [Tooltip("Maestría acumulada por tipo de arma.")]
    [SerializeField] private WeaponMastery mastery = new WeaponMastery();

    [Tooltip("Puntos de maestría por cada golpe conectado en combate.")]
    [SerializeField] private int masteryPerHit = 1;

    [Tooltip("Probabilidad de esquiva total con la pasiva de Evasión.")]
    [Range(0f, 1f)]
    [SerializeField] private float evasionChance = 0.15f;

    [Tooltip("Bonus de crítico al llegar al refinamiento de habilidad máximo (entrenamiento en el muñeco).")]
    [Range(0f, 1f)]
    [SerializeField] private float maxCritBonusFromRefinement = 0.10f;

    [Tooltip("Reducción del enfriamiento de habilidad al llegar al refinamiento máximo.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxSkillCooldownReductionFromRefinement = 0.20f;

    [Tooltip("Probabilidad base de golpe crítico, de 0 a 1.")]
    [Range(0f, 1f)]
    [SerializeField] private float baseCritChance = 0.12f;

    [Tooltip("Multiplicador de daño de un crítico antes de sumar el afijo de la pieza.")]
    [SerializeField] private float baseCritMultiplier = 1.5f;

    [Tooltip("Fatiga que se conserva por golpe con la pasiva de Aguante.")]
    [Range(0f, 1f)]
    [SerializeField] private float painToleranceFactor = 0.5f;

    [Tooltip("Rango de detección extra con la pasiva de Ojo de Águila.")]
    [SerializeField] private float eagleEyeBonusRange = 3f;

    [Tooltip("Fracción de vida por debajo de la cual se activa Berserker.")]
    [SerializeField, Range(0f, 1f)] private float berserkHealthThreshold = 0.30f;

    [Tooltip("Ataque extra de Berserker mientras está por debajo del umbral.")]
    [SerializeField] private float berserkAttackBonus = 0.40f;

    [Tooltip("Altura a la que sale el rótulo con el nombre de la habilidad lanzada.")]
    [SerializeField] private float skillLabelHeight = 0.9f;

    [Tooltip("Altura del rótulo de intención táctica; por debajo del de habilidad para no solaparse.")]
    [SerializeField] private float intentLabelHeight = 0.6f;

    [Tooltip("Vida restante del objetivo por debajo de la cual no se le suelta aunque la escuadra mande otro.")]
    [SerializeField, Range(0f, 1f)] private float intentSwitchKeepRatio = 0.35f;

    [Tooltip("Defensa extra mientras dura el decreto de posición defensiva.")]
    [SerializeField] private int defensiveStanceBonus = 5;

    private HeroState state = HeroState.BaseIdle;
    private int currentHealth;
    private Vector2 wanderTarget;
    private float restTimer;
    private float attackTimer;
    private float scanTimer;
    private EnemyController target;
    private HeroProgress progress;

    // Maná, fatiga y moral van en float para que los cambios por segundo no se pierdan entre frames.
    private float currentMP;
    private float fatigue;
    private float morale;

    // Recuerda si ya estaba en crítico: la moral cae al cruzar el umbral, no en cada golpe.
    private bool wasCritical;

    // Pasivas y equipo son por instancia; el HeroData compartido no se toca nunca.
    private readonly List<PassiveSkill> passives = new List<PassiveSkill>();
    // Instancias, no el asset: el desgaste, el nivel de mejora y el afijo forjado son de
    // ESTA copia de la pieza, y el EquipmentData lo comparten todas las copias del juego.
    private EquipmentInstance weapon;
    private EquipmentInstance shield;
    private EquipmentInstance armor;
    private EquipmentInstance accessory;

    // Identidad de esta unidad concreta; sobrevive al guardado y no depende del orden del array.
    private string heroInstanceId;

    // Solo los héroes desplegados con la escuadra buscan pelea; el resto sigue en la base.
    private bool deployed;

    // Puesto de formación al desplegar en la arena; limita cuánto se puede alejar al kitear
    // para que un arquero/mago no acabe caminando fuera del combate persiguiendo distancia.
    private Vector2 combatAnchor;

    [Tooltip("Radio máximo de retirada (kite) respecto al puesto de formación en la arena.")]
    [SerializeField] private float maxKiteRadius = 3.5f;

    // Estado global (Fase 37): capa de alto nivel sobre la FSM interna, para que el resto del
    // juego (animaciones, IA, HUD) pueda preguntar "en qué modo está" sin conocer HeroState.
    private HeroGlobalState globalState = HeroGlobalState.InBase;
    public HeroGlobalState GlobalState => globalState;

    // Sub-estado de combate; solo se actualiza mientras GlobalState == InCombat.
    private CombatState combatState = CombatState.IdleSearching;
    private CombatIntent intent = CombatIntent.None;
    public CombatState CombatSubState => combatState;

    // Cuando está armado, IdleSearching aguanta el sitio en vez de avanzar a ciegas hacia +X.
    private bool holdPosition;
    public void SetHoldPosition(bool value) => holdPosition = value;

    // Candado del roster: un héroe bloqueado no se puede sacrificar por accidente.
    private bool isLocked;

    // En el gimnasio manda el agente: la FSM propia se aparta y la muerte no destruye la unidad.
    private bool externalControl;

    // Congelado durante la cuenta atrás de combate: ni FSM ni maná ni estados corren.
    private bool frozen;

    // En marcha por el paseo del Portal (entrada o salida): bloquea la FSM sin tocar "frozen",
    // que es de la cuenta atrás y no debe levantarse antes de tiempo.
    private bool traveling;

    // Subclase elegida al ascender; manda sobre la habilidad activa.
    private HeroSubclass subclass = HeroSubclass.None;

    // Durabilidad por instancia: el EquipmentData es compartido y no se puede tocar.
    // El desgaste y el nivel de mejora viajan con la pieza (EquipmentInstance), no con el
    // hueco: así una pieza guardada en el almacén conserva lo suyo al volver a equiparse.

    // Bonus de sinergia por compartir origen con la escuadra, en tanto por uno.
    private float originSynergy;

    private StatusEffectManager status;

    private LPCAnimator animator;
    private SpriteRenderer body;
    private Coroutine flashRoutine;

    [Tooltip("Fracción de la vida máxima en un solo golpe a partir de la cual se ve el flash blanco.")]
    [SerializeField] private float hitFlashThreshold = 0.12f;

    [Tooltip("Segundos que dura el flash blanco al recibir un golpe fuerte.")]
    [SerializeField] private float hitFlashDuration = 0.08f;

    // Puesto de trabajo fijo; el héroe vuelve solo a él en vez de vagar.
    private BaseBuilding assignedBuilding;

    // Segundos sin desplegarse, entrenar ni trabajar; alimenta la apatía.
    private float idleSeconds;
    private float apathyTimer;

    // Objetivo impuesto por el decreto de Enfocar Objetivo; manda sobre el más cercano.
    private EnemyController forcedTarget;

    // Posición defensiva: segundos que quedan de bonus de defensa.
    private float defensiveTimer;

    // Ascensión: estrellas ganadas y factor que escala las bases del asset.
    private int bonusStarRank;
    private float ascensionMultiplier = 1f;

    // Afecto acumulado por regalos; 0-100, persiste entre sesiones.
    private float affinity;

    [Tooltip("Afecto a partir del cual el héroe gana el bonus de ataque por afecto.")]
    [SerializeField] private float affinityAtkThreshold = 50f;

    [Tooltip("Bonus de ataque, en tanto por uno, al superar el umbral de afecto.")]
    [SerializeField] private float affinityAtkBonus = 0.02f;

    [Tooltip("Bonus de EXP de entrenamiento, en tanto por uno, con el afecto al máximo (100).")]
    [SerializeField] private float affinityExpBonus = 0.05f;

    // Bonus por instancia que aporta el nivel; el HeroData compartido no se toca nunca.
    // Van en float y se redondean solo al leer la cifra: redondear en cada nivel obligaba a
    // un mínimo de +1, que en un héroe de base baja es más porcentaje del que se pide.
    private float bonusMaxHealth;
    private float bonusAttack;

    // Mejora de equipo básico del Taller: plano, independiente del nivel y de la ascensión.
    private int gearUpgradeAttack;
    private int gearUpgradeDefense;

    private BaseBuilding destinationBuilding;
    private BaseBuilding currentBuilding;
    private float visitTimer;
    private float buildingTickTimer;

    public HeroData Data => data;
    public bool IsDeployed => deployed;
    public HeroState State => state;
    public HeroTrait Trait => trait;
    public BaseBuilding CurrentBuilding => currentBuilding;
    public HeroSkill Skill => skill;

    public WeaponMastery Mastery => mastery;
    public IReadOnlyList<PassiveSkill> Passives => passives;
    public EquipmentInstance Weapon => weapon;
    public EquipmentInstance Shield => shield;
    public EquipmentInstance Armor => armor;
    public EquipmentInstance Accessory => accessory;

    public string HeroInstanceId => heroInstanceId;
    public bool IsLocked => isLocked;

    public bool ExternalControl { get => externalControl; set => externalControl = value; }
    public bool IsFrozen => frozen;
    public void SetFrozen(bool value) => frozen = value;

    // Intervención del Maestro: durante esta ventana la esquiva no se tira, se da por buena.
    private float guaranteedDodgeUntil;

    public void SetGuaranteedDodge(float seconds)
        => guaranteedDodgeUntil = Mathf.Max(guaranteedDodgeUntil, Time.time + Mathf.Max(0f, seconds));

    public bool HasGuaranteedDodge => Time.time < guaranteedDodgeUntil;
    public bool IsDead => currentHealth <= 0;
    public bool AttackReady => attackTimer <= 0f;
    public bool CanCastSkill => skill != null && skill.IsReady && CurrentMP >= SkillManaCost;
    // Arcos y báculos pegan de lejos; el resto tiene que plantarse delante.
    public bool IsRanged
        => EquippedWeaponType == WeaponType.Bow || EquippedWeaponType == WeaponType.Staff
           || HeroSubclasses.ArchetypeOf(subclass) == WeaponType.Bow
           || HeroSubclasses.ArchetypeOf(subclass) == WeaponType.Staff;

    public float EffectiveAttackRange => IsRanged ? rangedAttackRange : attackRange;

    // Personalidad de combate; sin HeroProgress (agentes de prueba) se queda en los valores neutros.
    public float Aggression => progress != null ? progress.Aggression : 0.5f;
    public float SafeDistance => progress != null ? progress.SafeDistance : 0.5f;
    public float SkillThreshold => progress != null ? progress.SkillThreshold : 0.15f;

    public float AttackReach => EffectiveAttackRange;
    public float DetectionReach => EffectiveDetectionRange;
    public bool IsInDefensiveStance => defensiveTimer > 0f;

    public int StarRank => data != null ? Mathf.Min(7, data.starRank + bonusStarRank) : 0;

    // Multiplica el crecimiento por nivel según la rareza: un 1★ crece a la mitad del ritmo de
    // un 3★ y un 7★ al doble. Ascender sube el ritmo, no solo el tope de nivel.
    public float GrowthRateByRarity => 0.5f + 0.25f * (Mathf.Max(1, StarRank) - 1);
    public float AscensionMultiplier => ascensionMultiplier;

    // Recuerdos del héroe, en orden de rareza. Los que aún no toca salen sellados: el jugador
    // ve que existen y a qué estrella se abren, que es lo que empuja a ascender.
    public string MemoriesReport()
    {
        if (data == null || data.memories == null || data.memories.Length == 0)
            return LocalizationManager.Get("UI_MEMORY_NONE");

        var sb = new System.Text.StringBuilder();
        int estrellas = StarRank;

        foreach (var memoria in System.Linq.Enumerable.OrderBy(data.memories, m => m.starRank))
        {
            if (memoria == null || !memoria.IsValid) continue;

            if (sb.Length > 0) sb.Append('\n');

            sb.Append(estrellas >= memoria.starRank
                ? memoria.GetLocalized()
                : $"<color={UITheme.Tag(UITheme.TextFaint)}>" +
                  string.Format(LocalizationManager.Get("UI_MEMORY_LOCKED"), memoria.starRank) +
                  "</color>");
        }

        return sb.Length > 0 ? sb.ToString() : LocalizationManager.Get("UI_MEMORY_NONE");
    }
    public int BonusStarRank => bonusStarRank;

    public float Affinity => affinity;
    public float AffinityAtkBonus => affinity >= affinityAtkThreshold ? affinityAtkBonus : 0f;
    public float AffinityExpBonus => affinity >= 100f ? affinityExpBonus : 0f;

    public WeaponType EquippedWeaponType => weapon != null ? weapon.WeaponType : WeaponType.None;

    public HeroSubclass Subclass => subclass;
    public string SubclassName => HeroSubclasses.DisplayName(subclass);
    public bool IsSupport => HeroSubclasses.IsSupport(subclass);
    public bool IsTank => shield != null || HeroSubclasses.IsTank(subclass);
    public float OriginSynergy => originSynergy;
    public BaseBuilding AssignedBuilding => assignedBuilding;
    public float IdleSeconds => idleSeconds;

    // Apatía: 1★ y 2★ que llevan demasiado tiempo sin servir para nada.
    public bool IsApathetic => StarRank <= 2 && idleSeconds >= apathyAfterSeconds;

    // La insignia del roster: el juego sugiere que sobra, no lo decide por ti.
    public bool IsSynthesisCandidate => IsApathetic && !isLocked;

    public void SetAssignedBuilding(BaseBuilding building) => assignedBuilding = building;

    // Se crea a demanda: la mayoría de los héroes nunca llegan a tener un estado encima.
    public StatusEffectManager Status
    {
        get
        {
            if (status == null) status = StatusEffectManager.For(gameObject);
            return status;
        }
    }

    // Cuántos enemigos le están apuntando ahora mismo; la usa el aggro de los tanques.
    public int Threat
    {
        get
        {
            int count = 0;
            foreach (var e in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                if (e.CurrentTarget == this) count++;

            return count;
        }
    }

    // Una pieza rota sigue puesta pero no aporta nada hasta repararla en el Taller.
    public int EquipBonusATK => BonusOf(EquipmentSlot.Weapon, 0) + BonusOf(EquipmentSlot.Shield, 0)
                              + BonusOf(EquipmentSlot.Armor, 0) + BonusOf(EquipmentSlot.Accessory, 0);
    public int EquipBonusDEF => BonusOf(EquipmentSlot.Weapon, 1) + BonusOf(EquipmentSlot.Shield, 1)
                              + BonusOf(EquipmentSlot.Armor, 1) + BonusOf(EquipmentSlot.Accessory, 1);
    public int EquipBonusHP => BonusOf(EquipmentSlot.Weapon, 2) + BonusOf(EquipmentSlot.Shield, 2)
                             + BonusOf(EquipmentSlot.Armor, 2) + BonusOf(EquipmentSlot.Accessory, 2);

    // 0 = ataque, 1 = defensa, 2 = vida. La instancia ya aplica el nivel de mejora y descuenta
    // la pieza rota, así que aquí solo queda elegir la cifra.
    private int BonusOf(EquipmentSlot slot, int kind)
    {
        var item = GetEquipped(slot);
        if (item == null) return 0;

        return kind == 0 ? item.BonusATK : kind == 1 ? item.BonusDEF : item.BonusHP;
    }

    // Suma el afijo entre las piezas sanas; una rota no aporta nada, como sus cifras.
    public float AffixTotal(EquipmentAffix affix)
    {
        if (affix == EquipmentAffix.None) return 0f;

        float total = 0f;
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            var item = GetEquipped(slot);
            if (item == null) continue;

            total += item.AffixValue(affix);
        }

        return total;
    }

    // Cada fuente aporta su parte y se suman; quien no tiene ninguna se queda en cero. El grueso
    // es la pasiva innata: el afijo y la pericia de arma (rango F=0 ... S=tope) suman encima.
    // Los afijos van en porcentaje, así que aquí se pasan a tanto por uno una sola vez.
    public float EffectiveEvasionChance
        => Mathf.Clamp01(PassiveSkills.EvasionBonus(passives)
                          + AffixTotal(EquipmentAffix.EvasionBoost) * 0.01f
                          + mastery.EvasionBonus(EquippedWeaponType));

    public float ArmorPierce
        => Mathf.Clamp01(AffixTotal(EquipmentAffix.ArmorPierce) * 0.01f
                          + PassiveSkills.ArmorPierceBonus(passives));

    public float LifeStealRatio
        => Mathf.Clamp01(AffixTotal(EquipmentAffix.LifeSteal) * 0.01f
                          + PassiveSkills.LifeStealBonus(passives));

    // El refinamiento del muñeco de entrenamiento afina la puntería con la habilidad, aplicado
    // aquí como bonus general de crítico (no hay un stat de precisión separado en este combate).
    public float CritChance => Mathf.Clamp01(baseCritChance
        + (progress != null ? progress.SkillRefinement * maxCritBonusFromRefinement : 0f)
        + PassiveSkills.CritChanceBonus(passives));

    // Cuánto se acorta el enfriamiento de la habilidad activa por refinamiento de entrenamiento.
    public float SkillCooldownReduction
        => Mathf.Clamp01((progress != null ? progress.SkillRefinement * maxSkillCooldownReductionFromRefinement : 0f)
                         + PassiveSkills.SkillCooldownBonus(passives));

    public float CritMultiplier
        => baseCritMultiplier + AffixTotal(EquipmentAffix.CritDamage) * 0.01f
           + PassiveSkills.CritDamageBonus(passives);

    // Última Voluntad ya gastada en este combate; se rearma al desplegar de nuevo.
    private bool lastStandSpent;

    // Maná que cuesta de verdad lanzar la habilidad, ya con las pasivas que lo abaratan.
    public int SkillManaCost
        => skill == null ? 0
           : Mathf.Max(1, Mathf.RoundToInt(skill.mpCost * PassiveSkills.MpCostMultiplier(passives)));

    // Berserker: pega más fuerte cuanto peor está. Fuera del umbral no multiplica nada.
    private float BerserkMultiplier
        => PassiveSkills.HasBerserk(passives)
           && MaxHealth > 0 && currentHealth <= MaxHealth * berserkHealthThreshold
            ? 1f + berserkAttackBonus
            : 1f;

    // Tira el crítico sobre un daño ya calculado; el robo de vida se cobra al impactar.
    public int RollStrike(int raw, out bool critico)
    {
        critico = CritMultiplier > 1f && UnityEngine.Random.value < CritChance;
        return critico ? Mathf.Max(1, Mathf.RoundToInt(raw * CritMultiplier)) : raw;
    }

    // Golpe completo contra un enemigo: crítico, perforación de armadura y robo de vida.
    public void StrikeEnemy(EnemyController enemy, int raw, bool ignoresDefense = false)
    {
        if (enemy == null) return;

        int damage = RollStrike(raw, out bool critico);
        int antes = enemy.CurrentHealth;

        if (animator != null) animator.PlayAttackLunge(enemy.transform.position);

        AudioManager.PlayAt(SfxId.MeleeHit, enemy.transform.position);
        enemy.TakeDamage(damage, ignoresDefense, ArmorPierce);

        if (critico) DamageTextManager.Show(enemy.transform.position, LocalizationManager.Get("FX_CRITICAL"), UITheme.BarMorale);
        if (critico) AudioManager.Play(SfxId.Critical);
        if (critico) CombatFeelManager.OnCriticalHit();

        StealLife(antes - enemy.CurrentHealth);
    }

    // Cura al héroe con una parte del daño que acaba de meter; solo con afijo de robo.
    public void StealLife(int damageDealt)
    {
        float ratio = LifeStealRatio;
        if (ratio <= 0f || damageDealt <= 0) return;

        int curado = Heal(Mathf.Max(1, Mathf.RoundToInt(damageDealt * ratio)));
        if (curado > 0) Debug.Log($"[Afijo] {data.heroName} roba {curado} de vida.", this);
    }

    public int DurabilityOf(EquipmentSlot slot)
    {
        var item = GetEquipped(slot);
        return item != null ? item.durability : 0;
    }

    public bool IsBroken(EquipmentSlot slot)
    {
        var item = GetEquipped(slot);
        return item != null && item.IsBroken;
    }

    public void SetDurability(EquipmentSlot slot, int value)
    {
        var item = GetEquipped(slot);
        if (item != null) item.durability = Mathf.Max(0, value);
    }

    public int GearLevelOf(EquipmentSlot slot)
    {
        var item = GetEquipped(slot);
        return item != null ? item.gearLevel : 0;
    }

    // La usa el Taller al mejorar una pieza concreta.
    public void SetGearLevel(EquipmentSlot slot, int value)
    {
        var item = GetEquipped(slot);
        if (item != null) item.gearLevel = Mathf.Max(0, value);
    }

    // Cada expedición pasa factura a todo lo que lleve puesto.
    public void WearEquipment(int amount)
    {
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            var item = GetEquipped(slot);
            if (item == null) continue;

            int antes = DurabilityOf(slot);
            if (antes <= 0) continue;

            SetDurability(slot, antes - amount);
            if (DurabilityOf(slot) <= 0)
                Debug.LogWarning($"[Desgaste] {data.heroName}: {item.LocalizedName()} se ha roto.", this);
        }

        ClampHealthToMax();
    }

    // Deja la pieza como nueva; el coste lo cobra quien llama.
    public bool RepairSlot(EquipmentSlot slot)
    {
        var item = GetEquipped(slot);
        if (item == null || item.durability >= item.MaxDurability) return false;

        item.durability = item.MaxDurability;
        ClampHealthToMax();
        return true;
    }

    // Primera pieza rota que lleve encima; el Taller repara de una en una.
    public EquipmentSlot? FirstBrokenSlot()
    {
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            if (IsBroken(slot)) return slot;

        return null;
    }

    public int CurrentHealth => currentHealth;
    public int MaxHealth => data != null
        ? Mathf.RoundToInt((Mathf.RoundToInt(data.maxHealth * ascensionMultiplier + bonusMaxHealth)
          + EquipBonusHP) * PassiveSkills.MaxHealthMultiplier(passives))
        : 0;

    public int Defense => data != null
        ? Mathf.RoundToInt((Mathf.RoundToInt(data.baseDefense * ascensionMultiplier) + EquipBonusDEF
          + gearUpgradeDefense + (IsInDefensiveStance ? defensiveStanceBonus : 0)) * (1f + originSynergy)
          * PassiveSkills.DefenseMultiplier(passives))
        : 0;

    // Ascensión, nivel, rasgo y equipo suman; moral y maestría multiplican.
    public int Attack
    {
        get
        {
            if (data == null) return 0;

            int raw = Mathf.RoundToInt(data.baseAttack * ascensionMultiplier + bonusAttack)
                      + gearUpgradeAttack + HeroTraits.AttackBonus(trait) + EquipBonusATK;

            float multiplier = (IsInspired ? 1f + inspiredAttackBonus : 1f)
                               * mastery.DamageMultiplier(EquippedWeaponType)
                               * (1f + originSynergy)
                               * (1f + AffinityAtkBonus)
                               * PassiveSkills.AttackMultiplier(passives)
                               * BerserkMultiplier;

            return Mathf.RoundToInt(raw * multiplier);
        }
    }

    public float EffectiveDetectionRange
        => detectionRange * HeroTraits.DetectionMultiplier(trait)
           + PassiveSkills.DetectionRangeBonus(passives);

    // Se redondea hacia abajo: lo que se ve es lo que se puede gastar.
    public int CurrentMP => Mathf.FloorToInt(currentMP);
    public int MaxMP => data != null ? data.maxMP : 0;

    public float Fatigue => fatigue;
    public int FatiguePercent => Mathf.RoundToInt(fatigue);
    public bool IsExhausted => fatigue > exhaustionThreshold;

    public float Morale => morale;
    public int MoralePercent => Mathf.RoundToInt(morale);
    public bool IsInspired => morale > inspiredThreshold;
    public bool IsDemoralized => morale < demoralizedThreshold;

    public MoraleState Mood
        => IsInspired ? MoraleState.Inspired
         : IsDemoralized ? MoraleState.Demoralized
         : MoraleState.Steady;

    // El estado normal no se nombra: en la UI solo interesan los extremos.
    public string MoodName
        => Mood == MoraleState.Inspired ? LocalizationManager.Get("UI_MOOD_INSPIRED")
         : Mood == MoraleState.Demoralized ? LocalizationManager.Get("UI_MOOD_DEMORALIZED")
         : string.Empty;

    [Tooltip("Fracción de vida por debajo de la cual la vida residual cuenta para la Insubordinación.")]
    [Range(0f, 1f)]
    [SerializeField] private float insubordinationHealthRatio = 0.30f;

    // Cualquier pieza equipada y rota cuenta como equipo precario.
    public bool HasBrokenGear
    {
        get
        {
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
                if (GetEquipped(slot) != null && IsBroken(slot)) return true;
            return false;
        }
    }

    // Se niega a entrar a la Torre con la moral por los suelos, vida residual o equipo roto,
    // hasta que se resuelva en la Cantina o el Taller.
    public bool IsInsubordinate
        => IsDemoralized
           || (MaxHealth > 0 && (float)CurrentHealth / MaxHealth < insubordinationHealthRatio)
           || HasBrokenGear;

    // El primer motivo que aplica, para no listarlos todos a la vez en el aviso.
    public string InsubordinationReasonKey()
    {
        if (IsDemoralized) return "UI_INSUBORDINATE_MORALE";
        if (MaxHealth > 0 && (float)CurrentHealth / MaxHealth < insubordinationHealthRatio) return "UI_INSUBORDINATE_HEALTH";
        if (HasBrokenGear) return "UI_INSUBORDINATE_GEAR";
        return string.Empty;
    }

    // Agotamiento y desmoralización pesan a la vez sobre llegar y golpear.
    public float EffectiveMoveSpeed
    {
        get
        {
            if (data == null) return 0f;

            float speed = data.moveSpeed * PassiveSkills.MoveSpeedMultiplier(passives);
            if (IsExhausted) speed *= 1f - exhaustionSpeedPenalty;
            if (IsDemoralized) speed *= 1f - demoralizedSpeedPenalty;
            return speed * Status.SpeedMultiplier;
        }
    }

    public float EffectiveAttackCooldown
    {
        get
        {
            // La pericia de arma acorta la recuperación base; la fatiga/desmoralización se
            // suman encima tal cual, no se ven reducidas por la pericia.
            float cooldown = attackCooldown * (1f - mastery.RecoveryReduction(EquippedWeaponType))
                             * PassiveSkills.AttackCooldownMultiplier(passives);
            if (IsExhausted) cooldown += exhaustionAttackDelay;
            if (IsDemoralized) cooldown += demoralizedAttackDelay;
            return cooldown;
        }
    }

    public event Action<int, int> HealthChanged;

    // Vida y maná se fijan en Awake para que la barra ya los lea válidos en su Start.
    void Awake()
    {
        // Un héroe de escena arranca con id propio; el SaveManager lo pisa si viene de un guardado.
        if (string.IsNullOrEmpty(heroInstanceId)) heroInstanceId = System.Guid.NewGuid().ToString();

        animator = GetComponent<LPCAnimator>();
        body = GetComponent<SpriteRenderer>();
        progress = GetComponent<HeroProgress>();
        morale = startingMorale;

        if (data != null)
        {
            currentHealth = MaxHealth;
            currentMP = MaxMP;
        }
    }

    // La usa el gacha: asigna los datos justo tras instanciar, antes del primer Start.
    public void Initialize(HeroData heroData, HeroTrait heroTrait, Vector2 wanderCenter, Vector2 wanderSize)
    {
        data = heroData;
        trait = heroTrait;
        baseAreaCenter = wanderCenter;
        baseAreaSize = wanderSize;

        currentHealth = MaxHealth;
        currentMP = MaxMP;
        fatigue = 0f;
        morale = startingMorale;
        wasCritical = false;

        ApplyBodySprite();
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    // Pone el sprite LPC del héroe en su SpriteRenderer; sin sprite se deja el del prefab.
    public void ApplyBodySprite()
    {
        if (data == null || data.bodySprite == null) return;

        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        sr.sprite = data.bodySprite;

        // El cuadro cuadrado del prefab venía teñido; el sprite real va sin tinte.
        sr.color = Color.white;

        // Y el animador recibe los 36 recortes de esa misma hoja.
        if (animator != null) animator.SetFrames(data.walkFrames);
    }

    // La llama el WaveManager al mandar o retirar la escuadra de la torre.
    // viaGateway: aterriza en el Portal de Torre en vez de dispersarse directo por la base
    // (el WaveManager lo limita a un puñado de la escuadra para no amontonar la plaza).
public void SetDeployed(bool value, bool viaGateway = false)
    {
        deployed = value;

        // Cada despliegue es un combate nuevo: Última Voluntad vuelve a estar disponible y la
        // intención se limpia para que la escuadra la vuelva a anunciar.
        if (value) lastStandSpent = false;
        intent = CombatIntent.None;

        if (!deployed)
        {
            forcedTarget = null;
            target = null;
            defensiveTimer = 0f;
            globalState = HeroGlobalState.HeadingToPortal;

            if (viaGateway) EnterViaGatewayAnimated();
            else
            {
                TeleportToBaseArea();
                EnterBaseWander();
            }
        }
    }

    // La llama el WaveManager al desplegar: sale por el Portal de la Torre y camina hasta su
    // puesto en formación, en vez de aparecer ya puesto en la arena de golpe.
public void DeployViaGateway(Vector2 destination)
    {
        deployed = true;
        forcedTarget = null;
        target = null;
        combatAnchor = destination;
        globalState = HeroGlobalState.HeadingToPortal;
        combatState = CombatState.IdleSearching;
        TeleportToGateway();
        StartCoroutine(DeployRoutine(destination));
    }

    // Se queda quieto reunido en el Portal un instante antes de partir hacia su puesto en la
    // arena, en vez de saltar directo (lo pide la secuencia de despliegue visible).
    private IEnumerator DeployRoutine(Vector2 destination)
    {
        traveling = true;
        yield return new WaitForSeconds(gatewayHoldSeconds);
        yield return TravelRoutine(destination, gatewayTravelSeconds);
    }

    // La usa la carga de partida: sin esto los héroes reaparecían todos encima del altar.
    public void ScatterInBaseArea() => TeleportToBaseArea();

    // La arena está a decenas de unidades: volver andando serían medio minuto de paseo.
    private void TeleportToBaseArea()
    {
        Vector2 half = baseAreaSize * 0.5f;
        transform.position = baseAreaCenter + new Vector2(
            UnityEngine.Random.Range(-half.x, half.x),
            UnityEngine.Random.Range(-half.y, half.y));
    }

    // Aterriza en el punto de encuentro del Portal, con un scatter pequeño para no apilar sprites.
    private void TeleportToGateway()
    {
        transform.position = TowerGateway.Position + UnityEngine.Random.insideUnitCircle * 0.6f;
    }

    // Aparece justo en el centro del Portal y se aparta caminando hasta el punto de dispersión:
    // se ve el paso de "salir por el Portal" en vez de aparecer ya disperso de golpe.
// Aparece justo en el centro del Portal, se queda un instante reunido y luego camina hasta
    // el punto de dispersión: se ve el paso de "volver por el Portal" antes de caer en Idle.
    private void EnterViaGatewayAnimated()
    {
        transform.position = TowerGateway.Position;
        Vector2 scatterPoint = TowerGateway.Position + UnityEngine.Random.insideUnitCircle * 0.6f;
        StartCoroutine(ReturnRoutine(scatterPoint));
    }

// Sale por el Portal caminando (visible) y luego se retira del mapa: se va de expedición de
    // recursos. Nada de desaparecer de golpe desde donde estuviera vagando.
    public void SendOnExpedition()
    {
        StartCoroutine(ExpeditionDepartRoutine());
    }

    private IEnumerator ExpeditionDepartRoutine()
    {
        traveling = true;
        globalState = HeroGlobalState.HeadingToPortal;
        BaseBuilding.ReleaseSlotEverywhere(this);
        currentBuilding = null;

        Vector2 gatewayPoint = TowerGateway.Position + UnityEngine.Random.insideUnitCircle * 0.5f;
        yield return TravelRoutine(gatewayPoint, gatewayTravelSeconds);
        yield return new WaitForSeconds(gatewayHoldSeconds);

        traveling = false;
        gameObject.SetActive(false);
    }

    // Vuelve de la expedición: reaparece en el Portal, se queda un instante y camina a su Idle.
    public void ReturnFromExpedition()
    {
        gameObject.SetActive(true);
        EnterViaGatewayAnimated();
    }


    private IEnumerator ReturnRoutine(Vector2 scatterPoint)
    {
        traveling = true;
        yield return new WaitForSeconds(gatewayHoldSeconds);
        yield return TravelRoutine(scatterPoint, gatewayTravelSeconds);
        EnterBaseWander();
    }

    // Paseo lineal corto e independiente de la FSM (usa "traveling", no "frozen"): así no se pisa
    // con TickBaseWander/TickCombatApproach mientras dura, y no altera la cuenta atrás externa.
    private IEnumerator TravelRoutine(Vector2 destination, float duration)
    {
        traveling = true;
        Vector2 start = transform.position;
        float t = 0f;

        while (duration > 0f && t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector2.Lerp(start, destination, t / duration);
            yield return null;
        }

        transform.position = destination;
        traveling = false;

        // Fin del paso por el Portal: desplegado va a InCombat (a buscar objetivo en la arena),
        // de vuelta va a InBase (EnterBaseWander, llamado por el propio SetDeployed, lo confirma).
        globalState = deployed ? HeroGlobalState.InCombat : HeroGlobalState.InBase;
    }

    // Asignar subclase da la habilidad de rol, que es la que enseña el modal de elección. A
    // partir de ahí el héroe puede aprender otras del arquetipo despertando.
    public void SetSubclass(HeroSubclass value)
    {
        subclass = value;

        LearnAbility(HeroSubclasses.DefaultAbility(value));
    }

    // Le cambia la habilidad activa. La usan la subclase, el guardado y el despertar.
    public void LearnAbility(ActiveSkill ability)
    {
        var hecha = ActiveSkills.Make(ability);
        if (hecha == null) return;

        hecha.subclass = subclass;
        skill = hecha;
    }

    public ActiveSkill Ability => skill != null ? skill.ability : ActiveSkill.None;

    // Compartir origen con al menos un compañero de escuadra da un bonus pasivo en combate.
    public void SetOriginSynergy(float value) => originSynergy = Mathf.Max(0f, value);

    public void SetLocked(bool value) => isLocked = value;
    public bool ToggleLock() { isLocked = !isLocked; return isLocked; }

    public bool HasForcedTarget => forcedTarget != null;

    // Lo que este héroe ha decidido hacer; lo reparte SquadTactics y lo pinta el rótulo.
    public CombatIntent Intent => intent;

    // La escuadra le dice a qué va. El objetivo entra como sugerencia, no como decreto: la FSM
    // sigue mandando en el cuerpo a cuerpo y el héroe no suelta a quien ya tiene medio muerto.
    public void SetIntent(CombatIntent nuevo, EnemyController sugerido)
    {
        // Solo se anuncia el cambio, no cada relectura del campo: si no, el rótulo parpadea.
        if (nuevo != intent)
        {
            intent = nuevo;
            AnnounceIntent();
        }

        if (sugerido == null || sugerido.CurrentHealth <= 0) return;
        if (!deployed || frozen) return;

        // Cambiar de objetivo teniendo uno a punto de caer es tirar el daño ya metido.
        if (target != null && target.CurrentHealth > 0 && target != sugerido)
        {
            float restante = target.MaxHealth > 0 ? (float)target.CurrentHealth / target.MaxHealth : 1f;
            if (restante <= intentSwitchKeepRatio) return;
        }

        target = sugerido;
        if (!IsInCombat())
        {
            globalState = HeroGlobalState.InCombat;
            state = HeroState.CombatApproach;
        }
        if (combatState == CombatState.IdleSearching) combatState = CombatState.MovingToTarget;
    }

    // Rótulo con lo que acaba de decidir; es lo que hace legible la capa táctica en combate.
    private void AnnounceIntent()
    {
        if (intent == CombatIntent.None || data == null) return;

        DamageTextManager.Show(transform.position + Vector3.up * intentLabelHeight,
            $"{CombatIntents.Glyph(intent)} {CombatIntents.DisplayName(intent)}",
            CombatIntents.Tint(intent));
    }

    // Decreto de Enfocar Objetivo: este enemigo pasa por delante del más cercano.
    public void SetForcedTarget(EnemyController enemy)
    {
        forcedTarget = enemy;
        if (enemy == null) return;

        target = enemy;
        if (state != HeroState.CombatAttack) state = HeroState.CombatApproach;
    }

    // Decreto de Reagruparse: retrocede y aguanta mejor unos segundos.
    public void ApplyDefensiveStance(float duration, float retreatDistance)
    {
        defensiveTimer = Mathf.Max(defensiveTimer, duration);
        transform.position += new Vector3(-retreatDistance, 0f, 0f);

        // Retroceder rompe el contacto: vuelve a acercarse desde donde ha quedado.
        if (state == HeroState.CombatAttack) state = HeroState.CombatApproach;
    }

    public bool HasPassive(PassiveSkill passive) => passives.Contains(passive);

    // Las asigna el gacha al invocar y el SaveManager al cargar; no cambian en toda la vida del héroe.
    public void SetPassives(IList<PassiveSkill> newPassives)
    {
        passives.Clear();
        if (newPassives == null) return;

        foreach (var p in newPassives)
            if (!passives.Contains(p)) passives.Add(p);
    }

    // La marca el SaveManager en los héroes que va a destruir al recargar la partida. Destroy
    // es diferido: hasta el final del frame las búsquedas con FindObjectsInactive.Include los
    // siguen devolviendo, y sin esta marca contarían como vivos y bloquearían invocarlos.
    public bool Discarded { get; private set; }

    public void MarkDiscarded() => Discarded = true;

    public EquipmentInstance GetEquipped(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: return weapon;
            case EquipmentSlot.Shield: return shield;
            case EquipmentSlot.Armor: return armor;
            case EquipmentSlot.Accessory: return accessory;
        }
        return null;
    }

    // Coloca la pieza en su hueco y devuelve la que estuviera puesta. El desgaste y el nivel
    // viajan dentro de la propia pieza, así que aquí ya no hay nada que reponer ni resetear.
    public EquipmentInstance Equip(EquipmentInstance item)
    {
        if (item == null || !item.IsValid) return null;

        var replaced = GetEquipped(item.SlotType);
        SetSlot(item.SlotType, item);

        ClampHealthToMax();
        return replaced;
    }

    // Atajo para quien todavía trabaja con el asset suelto (gacha, arma inicial): envuelve la
    // pieza en una instancia nueva y entera.
    public EquipmentInstance Equip(EquipmentData item)
        => item == null ? null : Equip(new EquipmentInstance(item));

    public EquipmentInstance Unequip(EquipmentSlot slot)
    {
        var removed = GetEquipped(slot);
        if (removed == null) return null;

        SetSlot(slot, null);
        ClampHealthToMax();
        return removed;
    }

    private void SetSlot(EquipmentSlot slot, EquipmentInstance item)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: weapon = item; break;
            case EquipmentSlot.Shield: shield = item; break;
            case EquipmentSlot.Armor: armor = item; break;
            case EquipmentSlot.Accessory: accessory = item; break;
        }
    }

    // Quitarse una armadura baja la vida máxima; la actual no puede quedar por encima.
    private void ClampHealthToMax()
    {
        currentHealth = Mathf.Clamp(currentHealth, 1, Mathf.Max(1, MaxHealth));
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    // La llaman el combate y el campo de entrenamiento; sin arma no suma nada.
    public void AddMasteryPoints(int amount)
    {
        if (mastery.AddPoints(EquippedWeaponType, amount))
            Debug.Log($"[Maestría] {data.heroName} sube a {EquippedWeaponType} Nv." +
                      $"{mastery.LevelOf(EquippedWeaponType)}.", this);
    }

    // La llama HeroProgress al ascender: sube una estrella y escala las bases del asset.
    public void ApplyAscension(float multiplier)
    {
        bonusStarRank++;
        ascensionMultiplier *= multiplier;

        // El nivel vuelve a 1, así que los bonus acumulados por nivel se van con él.
        bonusMaxHealth = 0f;
        bonusAttack = 0f;

        currentHealth = MaxHealth;
        currentMP = MaxMP;
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    // La usa el SaveManager para devolver estrellas y escalado como estaban.
    public void LoadAscension(int savedBonusStarRank, float savedMultiplier)
    {
        bonusStarRank = Mathf.Max(0, savedBonusStarRank);
        ascensionMultiplier = savedMultiplier > 0f ? savedMultiplier : 1f;
    }

    // La llama HeroQuickCardUI al regalar comida; positiva siempre, con tope en 100.
    public void AddAffinity(float amount) => affinity = Mathf.Clamp(affinity + amount, 0f, 100f);

    // La usa el SaveManager para devolver el afecto tal cual estaba.
    public void LoadAffinity(float savedAffinity) => affinity = Mathf.Clamp(savedAffinity, 0f, 100f);

    public int GearUpgradeAttack => gearUpgradeAttack;
    public int GearUpgradeDefense => gearUpgradeDefense;

    // La llama el Taller: mejora de equipo básico, plana y acumulable.
    public void ApplyGearUpgrade(int attackFlat, int defenseFlat)
    {
        gearUpgradeAttack += Mathf.Max(0, attackFlat);
        gearUpgradeDefense += Mathf.Max(0, defenseFlat);
    }

    // La usa el SaveManager al cargar.
    public void LoadGearUpgrade(int savedAttack, int savedDefense)
    {
        gearUpgradeAttack = Mathf.Max(0, savedAttack);
        gearUpgradeDefense = Mathf.Max(0, savedDefense);
    }

    // La usa el SaveManager para devolverle su identidad original al cargar la partida.
    public void LoadInstanceId(string savedId)
    {
        if (!string.IsNullOrEmpty(savedId)) heroInstanceId = savedId;
    }

    // La usa el SaveManager para dejar vida, maná, fatiga y moral como estaban al guardar.
    public void LoadVitals(int savedHealth, int savedMP, float savedFatigue, float savedMorale)
    {
        currentHealth = Mathf.Clamp(savedHealth, 1, MaxHealth);
        currentMP = Mathf.Clamp(savedMP, 0, MaxMP);
        fatigue = Mathf.Clamp(savedFatigue, 0f, 100f);
        morale = Mathf.Clamp(savedMorale, 0f, 100f);

        // Se recalcula para no volver a cobrar la bajada de moral por un crítico ya sufrido.
        wasCritical = MaxHealth > 0 && currentHealth < MaxHealth * criticalHealthRatio;

        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    // La llama HeroProgress al subir de nivel; devuelve la vida máxima ganada.
    // Los porcentajes van sobre las bases del asset ya ascendidas, no sobre el máximo actual:
    // sobre el actual el nivel componía (x106 a Nv.50) y se quedaba con parte del equipo puesto.
    public int ApplyLevelUpBonus(float healthPercent, float attackPercent)
    {
        if (data == null) return 0;

        int antes = MaxHealth;
        float ritmo = GrowthRateByRarity;

        bonusMaxHealth += data.maxHealth * ascensionMultiplier * healthPercent * ritmo;
        bonusAttack += data.baseAttack * ascensionMultiplier * attackPercent * ritmo;

        int gain = MaxHealth - antes;

        // Al subir de nivel se restaura la salud.
        currentHealth = MaxHealth;
        HealthChanged?.Invoke(currentHealth, MaxHealth);

        return gain;
    }

    void Start()
    {
        if (data == null)
        {
            Debug.LogError($"[HeroController] {name} no tiene HeroData asignado.", this);
            enabled = false;
            return;
        }

        // Los héroes puestos a mano en la escena no pasan por Initialize.
        ApplyBodySprite();
        EnterBaseWander();
    }

    void Update()
    {
        // La cuenta atrás previa al combate lo congela todo, igual que a los enemigos; el paseo de
        // entrada/salida del Portal también frena la FSM para no pelearse por la posición.
        if (frozen || traveling) return;

        if (defensiveTimer > 0f) defensiveTimer -= Time.deltaTime;

        RegenerateMana();
        skill?.Tick(Time.deltaTime);
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;
        TickApathy();

        // Con control externo el agente decide: nada de buscar objetivo ni de correr la FSM.
        if (externalControl) return;

        // Aturdido no piensa ni se mueve, pero el maná y los estados siguen corriendo.
        if (Status.IsStunned)
        {
            if (IsInCombat()) combatState = CombatState.Stunned;
            return;
        }

        ScanForEnemies();

        // El soporte cuida de la escuadra desde donde esté, sin esperar a entrar en rango.
        if (deployed && IsSupport) TickSupport();

        switch (state)
        {
            case HeroState.BaseIdle: TickBaseIdle(); break;
            case HeroState.BaseWander: TickBaseWander(); break;
            case HeroState.Training: TickBuildingVisit(); break;
            case HeroState.CombatApproach: TickCombatApproach(); break;
            case HeroState.CombatAttack: TickCombatAttack(); break;
        }

        // Muro físico: en la arena ninguna unidad puede salir de sus límites.
        if (deployed)
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, WaveManager.ArenaWallMin.x, WaveManager.ArenaWallMax.x);
            pos.y = Mathf.Clamp(pos.y, WaveManager.ArenaWallMin.y, WaveManager.ArenaWallMax.y);
            transform.position = pos;
        }
    }

    // En combate el maná entra a la mitad de ritmo: no se pueden encadenar habilidades.
    private void RegenerateMana()
    {
        if (data == null) return;

        float rate = (IsInCombat() ? mpRegenInCombat : mpRegenOutOfCombat)
                     * PassiveSkills.ManaRegenMultiplier(passives);
        currentMP = Mathf.Min(MaxMP, currentMP + rate * Time.deltaTime);
    }

    public void AddFatigue(float amount)
    {
        if (amount <= 0f) return;
        fatigue = Mathf.Min(100f, fatigue + amount);
    }

    public void RecoverFatigue(float amount)
    {
        if (amount <= 0f) return;

        bool agotadoAntes = IsExhausted;
        fatigue = Mathf.Max(0f, fatigue - amount);

        // Dejar de estar agotado cuenta como contrato de la cantina cumplido.
        if (agotadoAntes && !IsExhausted) QuestManager.Report(QuestKind.CureFatigue);
    }

    public void AddMorale(float amount)
    {
        if (amount <= 0f) return;
        morale = Mathf.Min(100f, morale + amount);
    }

    public void LoseMorale(float amount)
    {
        if (amount <= 0f) return;
        morale = Mathf.Max(0f, morale - amount);
    }

    // Busca el enemigo más cercano cada scanInterval y decide entrar o salir de combate.
    private void ScanForEnemies()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f) return;
        scanTimer = scanInterval;

        // Sin desplegar no se entra en combate: los de la base siguen a lo suyo.
        if (!deployed)
        {
            if (IsInCombat()) EnterBaseWander();
            return;
        }

        // El decreto manda mientras el objetivo siga vivo.
        if (forcedTarget != null)
        {
            target = forcedTarget;
            if (!IsInCombat()) state = HeroState.CombatApproach;
            return;
        }

        // Ya en combate con un objetivo vivo: manda hasta que muera. Que el enemigo quede fuera
        // del rango de detección al kitear no debe arrancar al héroe de vuelta a la base.
        if (IsInCombat() && target != null) return;

        EnemyController nearest = FindNearestEnemy();
        if (nearest == null) return;

        // El combate interrumpe cualquier actividad de base, entrenamiento incluido. Si ya
        // estaba IsInCombat() con el objetivo muerto (IdleSearching), esto solo lo reengancha
        // en el sitio donde estaba de guardia, sin pasar por la base.
        currentBuilding = null;
        target = nearest;
        globalState = HeroGlobalState.InCombat;
        combatState = CombatState.MovingToTarget;
        state = HeroState.CombatApproach;
    }

    // La llama el WaveManager al soltar la oleada: engancha objetivo en el mismo frame en que
    // arranca el combate, sin esperar al primer tick de scanInterval.
    public void AcquireNearestTargetNow()
    {
        if (!deployed || frozen) return;

        EnemyController nearest = FindNearestEnemy();
        if (nearest == null) return;

        currentBuilding = null;
        target = nearest;
        globalState = HeroGlobalState.InCombat;
        combatState = CombatState.MovingToTarget;
        state = HeroState.CombatApproach;
        scanTimer = scanInterval;
    }

    private EnemyController FindNearestEnemy()
    {
        var enemies = UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        EnemyController nearest = null;
        float range = EffectiveDetectionRange;
        float bestSqr = range * range;

        foreach (var enemy in enemies)
        {
            float sqr = ((Vector2)(enemy.transform.position - transform.position)).sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private bool IsInCombat()
        => state == HeroState.CombatApproach || state == HeroState.CombatAttack;

    private void TickBaseIdle()
    {
        // Parado en la base es donde se le pasa el cansancio.
        RecoverFatigue(fatigueRecoveryIdle * Time.deltaTime);

        restTimer -= Time.deltaTime;
        if (restTimer <= 0f) EnterBaseWander();
    }

    private void TickBaseWander()
    {
        MoveTowards(wanderTarget);

        if (Vector2.Distance(transform.position, wanderTarget) > arriveThreshold) return;

        // Si el destino era un edificio y ya está dentro, se pone a usarlo.
        // Sin sitio libre no se entra: el edificio decide si desplaza a alguien o no.
        if (destinationBuilding != null && !destinationBuilding.TryAdmit(this)) destinationBuilding = null;

        if (destinationBuilding != null && destinationBuilding.IsInside(transform.position))
        {
            EnterBuildingVisit(destinationBuilding);
            return;
        }

        // El perezoso se queda parado bastante más rato.
        restTimer = UnityEngine.Random.Range(restTimeRange.x, restTimeRange.y)
                    * HeroTraits.IdleMultiplier(trait);
        state = HeroState.BaseIdle;
    }

    private void EnterBuildingVisit(BaseBuilding building)
    {
        currentBuilding = building;
        destinationBuilding = null;
        state = HeroState.Training;

        visitTimer = building.RandomVisitDuration();
        buildingTickTimer = building.TickInterval;

        Debug.Log($"[Base] {data.heroName} empieza a usar {building.BuildingName}.", this);
    }

    private void TickBuildingVisit()
    {
        if (currentBuilding == null) { EnterBaseWander(); return; }

        // La cantina y la zona de descanso quitan fatiga mientras dura la visita.
        if (currentBuilding.Type != BuildingType.TrainingDummy)
            RecoverFatigue(fatigueRecoveryResting * Time.deltaTime);

        visitTimer -= Time.deltaTime;
        buildingTickTimer -= Time.deltaTime;

        if (buildingTickTimer <= 0f)
        {
            buildingTickTimer = currentBuilding.TickInterval;
            currentBuilding.ApplyTick(this);
        }

        if (visitTimer <= 0f)
        {
            currentBuilding = null;
            EnterBaseWander();
        }
    }

    private void TickCombatApproach()
    {
        if (target == null)
        {
            // IdleSearching: sin objetivo visible, avanza hacia el lado enemigo (+X). PROHIBIDO
            // calcular ruta a la base — ScanForEnemies reengancha en cuanto detecte uno nuevo.
            // holdPosition (armado por WaveManager en misiones con huecos entre oleadas) frena
            // ese avance a ciegas para no sacar a la escuadra de la línea.
            combatState = CombatState.IdleSearching;
            if (!holdPosition) MoveTowards(transform.position + Vector3.right * detectionRange);
            return;
        }

        combatState = CombatState.MovingToTarget;
        MoveTowards(target.transform.position);

        // Correr detrás del enemigo cansa; pararse a golpear, no.
        AddFatigue(fatiguePerSecondMoving * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.transform.position) <= EffectiveAttackRange)
        {
            state = HeroState.CombatAttack;
            attackTimer = 0f;   // el primer golpe sale sin esperar
        }
    }

    private void TickCombatAttack()
    {
        if (target == null)
        {
            // Mismo criterio que TickCombatApproach: guardia en sitio, sin ruta a base.
            state = HeroState.CombatApproach;
            combatState = CombatState.IdleSearching;
            return;
        }

        float distancia = Vector2.Distance(transform.position, target.transform.position);

        // Si se aleja, vuelve a perseguirlo.
        if (distancia > EffectiveAttackRange)
        {
            state = HeroState.CombatApproach;
            return;
        }

        combatState = CombatState.InRangeAttacking;

        // Golpe en área del jefe cargando: los tanques aguantan la línea a propósito (mismo
        // criterio que TriggerMicroStep); el resto esquiva solo si es más prudente que agresivo.
        if (!IsTank && target.IsWindingUp && distancia < target.SlamRadius && SafeDistance > Aggression)
        {
            combatState = CombatState.Kiting;
            MoveAwayFrom(target.transform.position);
            return;
        }

        // El rango no se deja alcanzar cuerpo a cuerpo: solo se reposiciona si el objetivo entra
        // dentro del alcance de melee. Fuera de eso se queda plantado disparando.
        if (IsRanged && distancia < meleeThreatRange)
        {
            combatState = CombatState.Kiting;
            MoveAwayFrom(target.transform.position);
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        attackTimer = EffectiveAttackCooldown;

        // Si llega el maná y la habilidad está lista, el golpe especial sustituye al básico, salvo
        // que el enemigo esté ya por debajo del umbral del héroe: rematar con la habilidad es tirarla.
        bool targetAlmostDead = target.MaxHealth > 0
            && (float)target.CurrentHealth / target.MaxHealth < SkillThreshold;
        if (!IsSupport && !targetAlmostDead && CanCastSkill)
        {
            CastCombatSkill(target);
            return;
        }

        // De lejos el golpe viaja: se ve salir la flecha o el proyectil mágico.
        if (IsRanged) Projectile.Fire(transform.position, target, RollStrike(Attack, out _),
                                      ProjectileColor, false, this, magic: IsStaffRanged);
        else StrikeEnemy(target, Attack);

        AddMasteryPoints(masteryPerHit);
    }

    // Flecha clara para el arco, violeta para la magia.
    private Color ProjectileColor
        => IsStaffRanged
            ? new Color(0.70f, 0.45f, 1f)
            : new Color(1f, 0.92f, 0.60f);

    // Mismo criterio que el color: el báculo (equipado o de archetipo) es lo que se oye como magia.
    private bool IsStaffRanged
        => EquippedWeaponType == WeaponType.Staff
           || HeroSubclasses.ArchetypeOf(subclass) == WeaponType.Staff;

    // Desplegarse, entrenar o trabajar cuenta como servir; lo demás es estar de brazos cruzados.
    private void TickApathy()
    {
        bool ocupado = deployed || currentBuilding != null || assignedBuilding != null;

        if (ocupado)
        {
            idleSeconds = 0f;
            apathyTimer = 0f;
            return;
        }

        idleSeconds += Time.deltaTime;
        if (!IsApathetic) return;

        apathyTimer -= Time.deltaTime;
        if (apathyTimer > 0f) return;

        apathyTimer = apathyInterval;
        LoseMorale(apathyMoraleLoss);
    }

    // La usa el edificio cuando alguien de más rango le quita el sitio.
    public void EvictFromBuilding()
    {
        currentBuilding = null;
        EnterBaseWander();
    }

    // Habilidad exclusiva de la subclase; sin subclase sale el golpe potente de siempre.
    private void CastCombatSkill(EnemyController victim)
    {
        currentMP -= SkillManaCost;
        skill.PutOnCooldown(SkillCooldownReduction);

        AnnounceSkill();

        // Los golpes a distancia ya tienen su propio proyectil; el empujón es solo cuerpo a cuerpo.
        if (!IsRanged && animator != null) animator.PlayAttackLunge(victim.transform.position);

        // El crítico se tira una vez para toda la habilidad; las 18 ramas usan este daño.
        int damage = RollStrike(skill.DamageFrom(Attack), out bool critico);
        if (critico) DamageTextManager.Show(transform.position, LocalizationManager.Get("FX_CRITICAL"), UITheme.BarMorale);
        if (critico) AudioManager.Play(SfxId.Critical);
        if (critico) CombatFeelManager.OnCriticalHit();

        int vidaVictima = victim != null ? victim.CurrentHealth : 0;
        int veneno = Mathf.Max(1, Mathf.RoundToInt(Attack * 0.15f));

        switch (skill.ability)
        {
            case ActiveSkill.PoisonCut:
                victim.TakeDamage(damage);
                StatusEffectManager.Apply(victim.gameObject, StatusEffect.Poison, 6f, veneno);
                break;

            case ActiveSkill.IronGuard:
                victim.TakeDamage(damage);
                Status.Add(StatusEffect.Shield, 8f, Attack * 1.5f);
                break;

            case ActiveSkill.BladeDance:
                for (int i = 0; i < 3; i++) victim.TakeDamage(Mathf.Max(1, damage / 2));
                StatusEffectManager.Apply(victim.gameObject, StatusEffect.Bleed, 5f, veneno);
                break;

            case ActiveSkill.DragonThrust:
                // En hilera: alcanza a lo que esté alineado detrás del objetivo.
                foreach (var e in EnemiesInLine(victim, 3f)) e.TakeDamage(damage);
                break;

            case ActiveSkill.PikePush:
                victim.TakeDamage(damage);
                victim.PushBack(transform.position, 1.5f);
                StatusEffectManager.Apply(victim.gameObject, StatusEffect.Slow, 4f, 0f);
                break;

            case ActiveSkill.StormPierce:
                // Antiarmadura: el daño entra sin restar la defensa del enemigo.
                victim.TakeDamage(damage, true);
                StatusEffectManager.Apply(victim.gameObject, StatusEffect.Stun, 1.5f, 0f);
                break;

            case ActiveSkill.LightCall:
                victim.TakeDamage(damage);
                foreach (var e in EnemiesAround(transform.position, 4f)) e.Taunt(this, 6f);
                foreach (var a in AlliesAround(5f)) a.Status.Add(StatusEffect.Shield, 6f, Attack * 0.8f);
                break;

            case ActiveSkill.UnstoppableCharge:
                victim.TakeDamage(damage);
                StatusEffectManager.Apply(victim.gameObject, StatusEffect.Stun, 1.5f, 0f);
                RecoverFatigue(40f);
                break;

            case ActiveSkill.ImmortalWall:
                victim.TakeDamage(damage);
                Status.Add(StatusEffect.Shield, 10f, Attack * 4f);
                break;

            case ActiveSkill.ChargedShot:
                Projectile.Fire(transform.position, victim, Mathf.RoundToInt(damage * 1.5f), ProjectileColor);
                break;

            case ActiveSkill.ArrowRain:
                // La flecha que se ve es la del centro; el área la resuelve la habilidad.
                Projectile.Fire(transform.position, victim, 0, ProjectileColor);
                foreach (var e in EnemiesAround(victim.transform.position, 3f))
                {
                    e.TakeDamage(damage);
                    StatusEffectManager.Apply(e.gameObject, StatusEffect.Slow, 4f, 0f);
                }
                break;

            case ActiveSkill.ShadowBolt:
                Projectile.Fire(transform.position, victim, damage, ProjectileColor);
                victim.PushBack(transform.position, 2f);
                StatusEffectManager.Apply(victim.gameObject, StatusEffect.Poison, 8f, veneno);
                break;

            case ActiveSkill.FireBurst:
                Projectile.Fire(transform.position, victim, 0, new Color(1f, 0.55f, 0.15f), magic: true);
                foreach (var e in EnemiesAround(victim.transform.position, 3.5f)) e.TakeDamage(damage, true);
                break;

            case ActiveSkill.TimeFracture:
                victim.TakeDamage(damage);
                foreach (var e in EnemiesAround(transform.position, 12f))
                    StatusEffectManager.Apply(e.gameObject, StatusEffect.Slow, 6f, 0f);
                break;

            case ActiveSkill.ArcaneRay:
                // Rayo perforante: gasta todo el maná que quede y pega en proporción.
                int extra = CurrentMP;
                currentMP = 0f;
                Projectile.Fire(transform.position, victim, damage + extra * 2, ProjectileColor, true, magic: true);
                break;

            case ActiveSkill.BloodHarvest:
                // Sangrado el doble de largo y curación aparte del robo de vida normal.
                victim.TakeDamage(damage);
                StatusEffectManager.Apply(victim.gameObject, StatusEffect.Bleed, 10f, veneno);
                Heal(Mathf.Max(1, Mathf.RoundToInt(damage * 0.25f)));
                break;

            case ActiveSkill.Riposte:
                // Dos tiempos: el primero tantea y el segundo entra por el hueco de la guardia.
                victim.TakeDamage(damage);
                if (victim != null) victim.TakeDamage(Mathf.Max(1, damage / 2), true);
                break;

            case ActiveSkill.HalberdSweep:
                foreach (var e in EnemiesAround(victim.transform.position, 2.5f))
                {
                    e.TakeDamage(damage);
                    e.PushBack(transform.position, 1.2f);
                }
                break;

            case ActiveSkill.Skewer:
                foreach (var e in EnemiesInLine(victim, 3f))
                {
                    e.TakeDamage(damage);
                    StatusEffectManager.Apply(e.gameObject, StatusEffect.Bleed, 6f, veneno);
                }
                break;

            case ActiveSkill.SentinelWatch:
                victim.TakeDamage(damage);
                foreach (var a in AlliesAround(6f)) a.Status.Add(StatusEffect.Shield, 8f, Attack * 1.1f);
                foreach (var e in EnemiesAround(transform.position, 3.5f)) e.Taunt(this, 5f);
                break;

            case ActiveSkill.Retribution:
                // Represalia: cuanta menos vida le queda, más fuerte devuelve el golpe.
                float carencia = MaxHealth > 0 ? 1f - (float)currentHealth / MaxHealth : 0f;
                victim.TakeDamage(Mathf.RoundToInt(damage * (1f + carencia)));
                Status.Add(StatusEffect.Shield, 6f, Attack * 1.5f);
                break;

            case ActiveSkill.HuntingSnare:
                Projectile.Fire(transform.position, victim, damage, ProjectileColor);
                StatusEffectManager.Apply(victim.gameObject, StatusEffect.Stun, 1.2f, 0f);
                StatusEffectManager.Apply(victim.gameObject, StatusEffect.Slow, 6f, 0f);
                break;

            case ActiveSkill.WindVolley:
                // Una flecha por enemigo distinto, empezando por el objetivo.
                int flechas = 0;
                Projectile.Fire(transform.position, victim, damage, ProjectileColor);
                flechas++;
                foreach (var e in EnemiesAround(transform.position, EffectiveAttackRange + 2f))
                {
                    if (flechas >= 3) break;
                    if (e == victim) continue;

                    Projectile.Fire(transform.position, e, damage, ProjectileColor);
                    flechas++;
                }
                break;

            case ActiveSkill.FrostShroud:
                Projectile.Fire(transform.position, victim, 0, new Color(0.55f, 0.85f, 1f), magic: true);
                foreach (var e in EnemiesAround(victim.transform.position, 3.5f))
                {
                    e.TakeDamage(damage, true);
                    StatusEffectManager.Apply(e.gameObject, StatusEffect.Slow, 5f, 0f);
                    StatusEffectManager.Apply(e.gameObject, StatusEffect.Stun, 0.8f, 0f);
                }
                break;

            case ActiveSkill.WitheringTouch:
                Projectile.Fire(transform.position, victim, damage, new Color(0.45f, 0.75f, 0.4f),
                                magic: true);
                StatusEffectManager.Apply(victim.gameObject, StatusEffect.Poison, 10f, veneno * 2);
                Heal(Mathf.Max(1, Mathf.RoundToInt(damage * 0.30f)));
                break;

            default:
                victim.TakeDamage(damage);
                break;
        }

        // El robo de vida se cobra sobre lo que ha perdido de verdad la victima.
        if (victim != null) StealLife(vidaVictima - victim.CurrentHealth);

        AddMasteryPoints(masteryPerHit);
        Debug.Log($"[Habilidad] {data.heroName} ({SubclassName}) lanza {skill.GetDisplayName()}: " +
                  $"{damage} base, {skill.GetDescription()} " +
                  $"(-{skill.mpCost} MP, quedan {CurrentMP}/{MaxMP}).", this);
    }

    // Rótulo flotante con el nombre de la habilidad. Sin esto las 30 habilidades se notaban
    // solo en los números de daño, que es lo mismo que no verlas.
    private void AnnounceSkill()
    {
        if (skill == null) return;

        DamageTextManager.Show(transform.position + Vector3.up * skillLabelHeight,
                               skill.GetDisplayName(), SkillLabelColor);
        AudioManager.PlayAt(SfxId.Critical, transform.position);
    }

    // Un color por arquetipo: acero para espada y lanza, oro para escudo, verde para arco,
    // violeta para báculo y turquesa para las mazas de los clérigos.
    private Color SkillLabelColor
    {
        get
        {
            switch (ActiveSkills.ArchetypeOf(skill != null ? skill.ability : ActiveSkill.None))
            {
                case WeaponType.Sword:
                case WeaponType.Spear: return new Color(0.85f, 0.90f, 1f);
                case WeaponType.Shield: return UITheme.Amber;
                case WeaponType.Bow: return new Color(0.55f, 0.95f, 0.55f);
                case WeaponType.Staff: return new Color(0.75f, 0.55f, 1f);
                case WeaponType.Mace: return new Color(0.45f, 0.95f, 0.85f);
            }
            return UITheme.Text;
        }
    }

    // Los clérigos miran a la escuadra, no al enemigo: actúan sobre el que peor está.
    private void TickSupport()
    {
        if (!CanCastSkill) return;

        var herido = MostWoundedAlly();
        if (herido == null) return;

        // Curar a alguien intacto es tirar el maná; los bufos sí salen sin esperar.
        bool urgente = herido.MaxHealth > 0 && (float)herido.CurrentHealth / herido.MaxHealth < 0.85f;
        if (skill.ability == ActiveSkill.GreaterBlessing && !urgente) return;

        currentMP -= SkillManaCost;
        skill.PutOnCooldown(SkillCooldownReduction);
        AnnounceSkill();

        switch (skill.ability)
        {
            case ActiveSkill.GreaterBlessing:
                int curado = herido.Heal(Mathf.RoundToInt(Attack * 2f));
                DamageTextManager.Show(herido.transform.position, $"+{curado}", new Color(0.4f, 1f, 0.5f));
                Debug.Log($"[Soporte] {data.heroName} cura a {herido.Data.heroName}: +{curado} PV " +
                          $"({herido.CurrentHealth}/{herido.MaxHealth}).", this);
                break;

            case ActiveSkill.OracleAegis:
                foreach (var a in AlliesAround(6f)) a.Status.Add(StatusEffect.Shield, 8f, Attack * 1.2f);
                Debug.Log($"[Soporte] {data.heroName} escuda a la escuadra.", this);
                break;

            case ActiveSkill.WarHymn:
                foreach (var a in AlliesAround(6f)) a.AddMorale(15f);
                Debug.Log($"[Soporte] {data.heroName} entona el himno: +moral a la escuadra.", this);
                break;

            case ActiveSkill.PurgingRite:
                // Cura menos que el Sumo Sacerdote, pero limpia veneno, sangrado y ralentización.
                int purgado = herido.Heal(Mathf.RoundToInt(Attack * 1.4f));
                herido.Status.Remove(StatusEffect.Poison);
                herido.Status.Remove(StatusEffect.Bleed);
                herido.Status.Remove(StatusEffect.Slow);
                DamageTextManager.Show(herido.transform.position, $"+{purgado}", new Color(0.9f, 0.9f, 0.5f));
                Debug.Log($"[Soporte] {data.heroName} purga a {herido.Data.heroName}: +{purgado} PV.", this);
                break;

            case ActiveSkill.IronPalm:
                // Reparte una curación menor entre los de al lado en vez de volcarla en uno solo.
                foreach (var a in AlliesAround(4f)) a.Heal(Mathf.RoundToInt(Attack * 0.7f));
                RecoverFatigue(25f);
                Debug.Log($"[Soporte] {data.heroName} cura a los de alrededor y recupera aliento.", this);
                break;
        }
    }

    // El aliado desplegado con menos porcentaje de vida; se incluye a sí mismo.
    public HeroController MostWoundedAlly()
    {
        HeroController peor = null;
        float mejor = 2f;

        foreach (var other in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
        {
            if (other == null || !other.IsDeployed || other.MaxHealth <= 0) continue;

            float ratio = (float)other.CurrentHealth / other.MaxHealth;
            if (ratio >= mejor) continue;

            mejor = ratio;
            peor = other;
        }

        return peor;
    }

    private List<HeroController> AlliesAround(float radius)
    {
        var lista = new List<HeroController>();
        float sqr = radius * radius;

        foreach (var other in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
        {
            if (other == null || !other.IsDeployed) continue;
            if (((Vector2)(other.transform.position - transform.position)).sqrMagnitude > sqr) continue;

            lista.Add(other);
        }

        return lista;
    }

    private List<EnemyController> EnemiesAround(Vector2 center, float radius)
    {
        var lista = new List<EnemyController>();
        float sqr = radius * radius;

        foreach (var e in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            if (((Vector2)e.transform.position - center).sqrMagnitude <= sqr) lista.Add(e);

        return lista;
    }

    // Los que caen dentro de la banda que va del héroe al objetivo y sigue más allá.
    private List<EnemyController> EnemiesInLine(EnemyController victim, float width)
    {
        var lista = new List<EnemyController>();
        Vector2 origen = transform.position;
        Vector2 direccion = ((Vector2)victim.transform.position - origen).normalized;

        foreach (var e in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            Vector2 delta = (Vector2)e.transform.position - origen;
            float alcance = Vector2.Dot(delta, direccion);
            if (alcance < 0f) continue;

            float desvio = (delta - direccion * alcance).magnitude;
            if (desvio <= width * 0.5f) lista.Add(e);
        }

        return lista;
    }

    private void EnterBaseWander()
    {
        target = null;
        globalState = HeroGlobalState.InBase;

        // Al soltar el edificio hay que devolver el punto de llegada que tenía reservado.
        BaseBuilding.ReleaseSlotEverywhere(this);
        currentBuilding = null;
        state = HeroState.BaseWander;
        PickNewWanderTarget();
    }

    private void PickNewWanderTarget()
    {
        destinationBuilding = null;

        // Con puesto asignado no se vaga: se vuelve al trabajo.
        if (assignedBuilding != null && assignedBuilding.IsUnlocked)
        {
            destinationBuilding = assignedBuilding;
            wanderTarget = assignedBuilding.ClaimSlot(this);
            return;
        }

        // A veces el héroe decide ir a un edificio en vez de vagar sin rumbo.
        float chance = buildingVisitChance * HeroTraits.VisitChanceMultiplier(trait);
        if (UnityEngine.Random.value < chance)
        {
            var building = PickRandomBuilding();

            // Cada héroe reserva su propio punto de llegada alrededor del edificio; si no
            // quedaba hueco de verdad se pasea sin rumbo, en vez de sumarse al corrillo.
            if (building != null && building.TryClaimSlot(this, out Vector2 hueco))
            {
                destinationBuilding = building;
                wanderTarget = hueco;
                return;
            }
        }

        // Reintenta unas cuantas veces para no caer dentro de un cuadrante todavía bloqueado;
        // si no encuentra hueco libre, se queda en el centro de la base (fuera de cualquier veil).
        Vector2 half = baseAreaSize * 0.5f;
        Vector2 candidate = baseAreaCenter;
        bool found = false;
        for (int i = 0; i < 8; i++)
        {
            candidate = baseAreaCenter + new Vector2(
                UnityEngine.Random.Range(-half.x, half.x),
                UnityEngine.Random.Range(-half.y, half.y));

            if (!IsInsideLockedQuadrant(candidate) && !IsNearGateway(candidate)) { found = true; break; }
        }

        wanderTarget = found ? candidate : baseAreaCenter;
    }

    // Un cuadrante bloqueado (veil con candado activo) nunca es destino de vagabundeo válido.
    private static bool IsInsideLockedQuadrant(Vector2 point)
    {
        foreach (var quadrant in QuadrantController.All)
            if (quadrant != null && quadrant.ContainsPoint(point)) return true;

        return false;
    }

    // El Portal es solo para entrar/salir de la Torre en formación: el paseo común lo evita.
    private static bool IsNearGateway(Vector2 point)
        => Vector2.Distance(TowerGateway.Position, point) <= 1.3f;

    // Sorteo ponderado: cada rasgo tira más hacia unos edificios que hacia otros.
    private BaseBuilding PickRandomBuilding()
    {
        var all = BaseBuilding.All;
        if (all == null || all.Count == 0) return null;

        // El sorteo tiene que recorrer el MISMO subconjunto con el que se sumaron los pesos:
        // acumulando sobre la lista entera se elegían edificios llenos o aún bloqueados, y por
        // eso se amontonaba media base encima del campo de entrenamiento.
        float total = 0f;
        foreach (var b in all)
            if (b != null && b.IsUnlocked && b.HasRoom) total += HeroTraits.BuildingWeight(trait, b.Type);
        if (total <= 0f) return null;

        float roll = UnityEngine.Random.Range(0f, total);
        float acc = 0f;

        BaseBuilding ultimo = null;
        foreach (var b in all)
        {
            if (b == null || !b.IsUnlocked || !b.HasRoom) continue;

            ultimo = b;
            acc += HeroTraits.BuildingWeight(trait, b.Type);
            if (roll < acc) return b;
        }

        return ultimo;
    }

    private void MoveTowards(Vector2 destination)
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            destination,
            EffectiveMoveSpeed * Time.deltaTime);
    }

    // Igual que MoveTowards pero en la dirección contraria: mantener las distancias de rango.
    private void MoveAwayFrom(Vector2 origin)
    {
        Vector2 aqui = transform.position;

        // Ya está en el borde de su radio de kite: no sigue huyendo, se queda a tiro para golpear.
        if (Vector2.Distance(aqui, combatAnchor) >= maxKiteRadius) return;

        Vector2 direccion = (aqui - origin).normalized;
        if (direccion == Vector2.zero) direccion = UnityEngine.Random.insideUnitCircle.normalized;

        Vector2 destino = aqui + direccion * (EffectiveMoveSpeed * Time.deltaTime);

        // Kiting: en el eje X, nunca más atrás que la línea trasera de la formación aliada
        // (el puesto de despliegue menos el radio de kite) — un arquero/mago no cruza detrás
        // de su propia escuadra por perseguir distancia.
        destino.x = Mathf.Clamp(destino.x, combatAnchor.x - maxKiteRadius, combatAnchor.x + maxKiteRadius);

        transform.position = destino;
    }

    // Paso corto e instantáneo lejos del objetivo tras encajar un golpe o esquivar uno: da la
    // sensación de que la unidad reacciona, sin convertirlo en una huida continua. Los tanques
    // aguantan la línea a propósito y no lo dan.
    private void TriggerMicroStep()
    {
        if (IsTank || target == null) return;

        Vector2 aqui = transform.position;
        Vector2 direccion = (aqui - (Vector2)target.transform.position).normalized;
        if (direccion == Vector2.zero) direccion = UnityEngine.Random.insideUnitCircle.normalized;

        transform.position = aqui + direccion * microStepDistance;
    }

    // Flash blanco breve; restaura el tinte que hubiera justo antes.
    private void HitFlash()
    {
        if (body == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        Color before = body.color;
        body.color = Color.white;
        yield return new WaitForSeconds(hitFlashDuration);
        body.color = before;
        flashRoutine = null;
    }

    public void TakeDamage(int amount) => TakeDamage(amount, false);

    // El daño mágico se salta la defensa: contra un chamán la armadura no protege.
    public void TakeDamage(int amount, bool ignoresDefense)
    {
        // Congelado en la cuenta atrás: invulnerabilidad estricta, sin excepciones por origen del golpe.
        if (frozen) return;

        // Evasión: el golpe no llega, así que no hay daño, ni fatiga, ni moral perdida. La
        // intervención del Maestro la da por acertada sin tirar.
        if (HasGuaranteedDodge || UnityEngine.Random.value < EffectiveEvasionChance)
        {
            DamageTextManager.ShowDodge(transform.position);
            Debug.Log($"[Pasiva] {data.heroName} esquiva el golpe.", this);
            TriggerMicroStep();
            return;
        }

        int finalDamage = ignoresDefense
            ? Mathf.Max(1, amount)
            : Mathf.Max(Mathf.RoundToInt(amount * CombatTuning.MinDamageFraction), amount - Defense, 1);

        // El escudo temporal absorbe primero; lo que sobra es lo que llega a la vida.
        finalDamage = Status.AbsorbDamage(finalDamage);
        if (finalDamage <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - finalDamage);

        // Última Voluntad: el primer golpe mortal de cada combate deja al héroe a 1 de vida.
        if (currentHealth <= 0 && !lastStandSpent && PassiveSkills.HasLastStand(passives))
        {
            lastStandSpent = true;
            currentHealth = 1;
            DamageTextManager.Show(transform.position,
                PassiveSkills.DisplayName(PassiveSkill.LastStand), UITheme.Amber);
            Debug.Log($"[Pasiva] {data.heroName} aguanta en pie con Última Voluntad.", this);
        }

        HealthChanged?.Invoke(currentHealth, MaxHealth);

        DamageTextManager.ShowDamage(transform.position, finalDamage);
        AudioManager.PlayAt(SfxId.HeroHurt, transform.position);
        TriggerMicroStep();

        // Golpe fuerte o ráfaga (p.ej. el Pisotón del jefe): flash blanco breve.
        if (MaxHealth > 0 && finalDamage >= MaxHealth * hitFlashThreshold) HitFlash();

        // Encajar golpes cansa; con Aguante, la mitad.
        AddFatigue(fatiguePerHitTaken * PassiveSkills.FatigueMultiplier(passives));
        CheckCriticalMorale();

        Debug.Log($"[Hero] {data.heroName} recibe {finalDamage} ({currentHealth}/{MaxHealth})", this);

        if (currentHealth <= 0)
        {
            // En el gimnasio la unidad se reaprovecha entre episodios, así que no se destruye.
            if (externalControl) return;

            // Permadeath: el héroe no vuelve. La ficha se toma con el héroe todavía en pie,
            // que es cuando aún se pueden leer equipo, nivel y rareza; sin ella su nombre queda
            // libre y el gacha lo vuelve a ofrecer como si no hubiera pasado nada.
            MemorialManager.Record(this, MemorialCause.FallenInTower, BaseBuilding.TowerFloor);

            AudioManager.PlayAt(SfxId.Defeat, transform.position);
            NotifyAlliesOfDeath();
            Debug.Log($"[Hero] {data.heroName} ha muerto.", this);
            Destroy(gameObject);

            // La Galería tiene que sobrevivir al cierre: sin esto la muerte se pierde al salir
            // y el héroe reaparece en el catálogo del gacha.
            SaveManager.RequestSave();
        }
    }

    // Golpe básico a un enemigo concreto; devuelve true solo si llegó a pegar.
    public bool TryBasicAttack(EnemyController enemy)
    {
        if (enemy == null || !AttackReady) return false;
        if (Vector2.Distance(transform.position, enemy.transform.position) > attackRange) return false;

        attackTimer = EffectiveAttackCooldown;
        StrikeEnemy(enemy, Attack);
        AddMasteryPoints(masteryPerHit);
        return true;
    }

    // Habilidad activa a un enemigo concreto; cobra el maná y arranca el enfriamiento.
    public bool TrySkillAttack(EnemyController enemy)
    {
        if (enemy == null || !AttackReady || !CanCastSkill) return false;
        if (Vector2.Distance(transform.position, enemy.transform.position) > attackRange) return false;

        attackTimer = EffectiveAttackCooldown;
        currentMP -= SkillManaCost;
        skill.PutOnCooldown(SkillCooldownReduction);

        StrikeEnemy(enemy, skill.DamageFrom(Attack));
        AddMasteryPoints(masteryPerHit);
        return true;
    }

    // Devuelve la unidad al estado de arranque de un episodio del gimnasio.
    public void ResetForEpisode()
    {
        currentHealth = MaxHealth;
        currentMP = MaxMP;
        fatigue = 0f;
        morale = startingMorale;
        wasCritical = false;
        attackTimer = 0f;
        target = null;
        forcedTarget = null;
        defensiveTimer = 0f;
        Status.Clear();
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    // La moral cae al cruzar el umbral crítico, no en cada golpe estando ya por debajo.
    private void CheckCriticalMorale()
    {
        bool critical = MaxHealth > 0 && currentHealth > 0
                        && currentHealth < MaxHealth * criticalHealthRatio;

        if (critical && !wasCritical)
        {
            wasCritical = true;
            LoseMorale(moraleLossOnCritical);
            Debug.Log($"[Moral] {data.heroName} en estado crítico: -{moraleLossOnCritical} moral " +
                      $"(queda {MoralePercent}).", this);
            return;
        }

        // Al recuperarse por encima del umbral vuelve a poder sufrirlo.
        if (!critical) wasCritical = false;
    }

    // Ver caer a un compañero cercano hunde la moral del resto.
    private void NotifyAlliesOfDeath()
    {
        var heroes = UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None);
        float radiusSqr = allyDeathRadius * allyDeathRadius;

        foreach (var other in heroes)
        {
            if (other == this) continue;
            if (((Vector2)(other.transform.position - transform.position)).sqrMagnitude > radiusSqr) continue;

            other.LoseMorale(moraleLossOnAllyDeath);
            Debug.Log($"[Moral] {other.Data.heroName} ve caer a {data.heroName}: " +
                      $"-{moraleLossOnAllyDeath} moral (queda {other.MoralePercent}).", other);
        }
    }

    // Cura sin pasarse de la vida máxima; devuelve lo que realmente se curó.
    public int Heal(int amount)
    {
        if (amount <= 0 || data == null) return 0;

        amount = Mathf.RoundToInt(amount * PassiveSkills.HealingMultiplier(passives));

        int before = currentHealth;
        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);

        int healed = currentHealth - before;
        if (healed > 0)
        {
            HealthChanged?.Invoke(currentHealth, MaxHealth);
            DamageTextManager.ShowHeal(transform.position, healed);
            CheckCriticalMorale();
        }

        return healed;
    }

    // Restaura maná sin pasarse del máximo; admite fracciones para la regeneración pasiva del Pozo de Maná.
    public void RestoreMP(float amount)
    {
        if (amount <= 0f || data == null) return;

        currentMP = Mathf.Min(MaxMP, currentMP + amount);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(baseAreaCenter, baseAreaSize);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
