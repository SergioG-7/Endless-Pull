using System;
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
    [SerializeField] private float detectionRange = 6f;

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

    [Tooltip("Fatiga que se conserva por golpe con la pasiva de Aguante.")]
    [Range(0f, 1f)]
    [SerializeField] private float painToleranceFactor = 0.5f;

    [Tooltip("Rango de detección extra con la pasiva de Ojo de Águila.")]
    [SerializeField] private float eagleEyeBonusRange = 3f;

    [Tooltip("Defensa extra mientras dura el decreto de posición defensiva.")]
    [SerializeField] private int defensiveStanceBonus = 5;

    private HeroState state = HeroState.BaseIdle;
    private int currentHealth;
    private Vector2 wanderTarget;
    private float restTimer;
    private float attackTimer;
    private float scanTimer;
    private EnemyController target;

    // Maná, fatiga y moral van en float para que los cambios por segundo no se pierdan entre frames.
    private float currentMP;
    private float fatigue;
    private float morale;

    // Recuerda si ya estaba en crítico: la moral cae al cruzar el umbral, no en cada golpe.
    private bool wasCritical;

    // Pasivas y equipo son por instancia; el HeroData compartido no se toca nunca.
    private readonly List<PassiveSkill> passives = new List<PassiveSkill>();
    private EquipmentData weapon;
    private EquipmentData shield;
    private EquipmentData armor;
    private EquipmentData accessory;

    // Identidad de esta unidad concreta; sobrevive al guardado y no depende del orden del array.
    private string heroInstanceId;

    // Solo los héroes desplegados con la escuadra buscan pelea; el resto sigue en la base.
    private bool deployed;

    // Objetivo impuesto por el decreto de Enfocar Objetivo; manda sobre el más cercano.
    private EnemyController forcedTarget;

    // Posición defensiva: segundos que quedan de bonus de defensa.
    private float defensiveTimer;

    // Ascensión: estrellas ganadas y factor que escala las bases del asset.
    private int bonusStarRank;
    private float ascensionMultiplier = 1f;

    // Bonus por instancia que aporta el nivel; el HeroData compartido no se toca nunca.
    private int bonusMaxHealth;
    private int bonusAttack;

    private BaseBuilding destinationBuilding;
    private BaseBuilding currentBuilding;
    private float visitTimer;
    private float buildingTickTimer;

    public HeroData Data => data;
    public HeroState State => state;
    public HeroTrait Trait => trait;
    public BaseBuilding CurrentBuilding => currentBuilding;
    public HeroSkill Skill => skill;

    public WeaponMastery Mastery => mastery;
    public IReadOnlyList<PassiveSkill> Passives => passives;
    public EquipmentData Weapon => weapon;
    public EquipmentData Shield => shield;
    public EquipmentData Armor => armor;
    public EquipmentData Accessory => accessory;

    public string HeroInstanceId => heroInstanceId;
    public bool IsDeployed => deployed;
    public bool IsInDefensiveStance => defensiveTimer > 0f;

    public int StarRank => data != null ? Mathf.Min(5, data.starRank + bonusStarRank) : 0;
    public float AscensionMultiplier => ascensionMultiplier;
    public int BonusStarRank => bonusStarRank;

    public WeaponType EquippedWeaponType => weapon != null ? weapon.weaponType : WeaponType.None;

    public int EquipBonusATK => (weapon != null ? weapon.bonusATK : 0)
                              + (shield != null ? shield.bonusATK : 0)
                              + (armor != null ? armor.bonusATK : 0)
                              + (accessory != null ? accessory.bonusATK : 0);
    public int EquipBonusDEF => (weapon != null ? weapon.bonusDEF : 0)
                              + (shield != null ? shield.bonusDEF : 0)
                              + (armor != null ? armor.bonusDEF : 0)
                              + (accessory != null ? accessory.bonusDEF : 0);
    public int EquipBonusHP => (weapon != null ? weapon.bonusHP : 0)
                             + (shield != null ? shield.bonusHP : 0)
                             + (armor != null ? armor.bonusHP : 0)
                             + (accessory != null ? accessory.bonusHP : 0);

    public int CurrentHealth => currentHealth;
    public int MaxHealth => data != null
        ? Mathf.RoundToInt(data.maxHealth * ascensionMultiplier) + bonusMaxHealth + EquipBonusHP
        : 0;

    public int Defense => data != null
        ? Mathf.RoundToInt(data.baseDefense * ascensionMultiplier) + EquipBonusDEF
          + (IsInDefensiveStance ? defensiveStanceBonus : 0)
        : 0;

    // Ascensión, nivel, rasgo y equipo suman; moral y maestría multiplican.
    public int Attack
    {
        get
        {
            if (data == null) return 0;

            int raw = Mathf.RoundToInt(data.baseAttack * ascensionMultiplier)
                      + bonusAttack + HeroTraits.AttackBonus(trait) + EquipBonusATK;

            float multiplier = (IsInspired ? 1f + inspiredAttackBonus : 1f)
                               * mastery.DamageMultiplier(EquippedWeaponType);

            return Mathf.RoundToInt(raw * multiplier);
        }
    }

    public float EffectiveDetectionRange
        => detectionRange * HeroTraits.DetectionMultiplier(trait)
           + (HasPassive(PassiveSkill.EagleEye) ? eagleEyeBonusRange : 0f);

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
        => Mood == MoraleState.Inspired ? "Inspirado"
         : Mood == MoraleState.Demoralized ? "Desmoralizado"
         : string.Empty;

    // Agotamiento y desmoralización pesan a la vez sobre llegar y golpear.
    public float EffectiveMoveSpeed
    {
        get
        {
            if (data == null) return 0f;

            float speed = data.moveSpeed;
            if (IsExhausted) speed *= 1f - exhaustionSpeedPenalty;
            if (IsDemoralized) speed *= 1f - demoralizedSpeedPenalty;
            return speed;
        }
    }

    public float EffectiveAttackCooldown
    {
        get
        {
            float cooldown = attackCooldown;
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
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    // La llama el WaveManager al mandar o retirar la escuadra de la torre.
    public void SetDeployed(bool value)
    {
        deployed = value;

        if (!deployed)
        {
            forcedTarget = null;
            target = null;
            defensiveTimer = 0f;
            if (IsInCombat()) EnterBaseWander();
        }
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

    public EquipmentData GetEquipped(EquipmentSlot slot)
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

    // Coloca la pieza en su hueco y devuelve la que estuviera puesta.
    public EquipmentData Equip(EquipmentData item)
    {
        if (item == null) return null;

        var replaced = GetEquipped(item.slotType);
        SetSlot(item.slotType, item);
        ClampHealthToMax();
        return replaced;
    }

    public EquipmentData Unequip(EquipmentSlot slot)
    {
        var removed = GetEquipped(slot);
        if (removed == null) return null;

        SetSlot(slot, null);
        ClampHealthToMax();
        return removed;
    }

    private void SetSlot(EquipmentSlot slot, EquipmentData item)
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
        bonusMaxHealth = 0;
        bonusAttack = 0;

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
    public int ApplyLevelUpBonus(float healthPercent, int attackFlat)
    {
        int gain = Mathf.Max(1, Mathf.RoundToInt(MaxHealth * healthPercent));

        bonusMaxHealth += gain;
        bonusAttack += attackFlat;

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

        EnterBaseWander();
    }

    void Update()
    {
        if (defensiveTimer > 0f) defensiveTimer -= Time.deltaTime;

        ScanForEnemies();
        RegenerateMana();
        skill?.Tick(Time.deltaTime);

        switch (state)
        {
            case HeroState.BaseIdle: TickBaseIdle(); break;
            case HeroState.BaseWander: TickBaseWander(); break;
            case HeroState.Training: TickBuildingVisit(); break;
            case HeroState.CombatApproach: TickCombatApproach(); break;
            case HeroState.CombatAttack: TickCombatAttack(); break;
        }
    }

    // En combate el maná entra a la mitad de ritmo: no se pueden encadenar habilidades.
    private void RegenerateMana()
    {
        if (data == null) return;

        float rate = IsInCombat() ? mpRegenInCombat : mpRegenOutOfCombat;
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
        fatigue = Mathf.Max(0f, fatigue - amount);
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

        EnemyController nearest = FindNearestEnemy();

        if (nearest != null && target == null)
        {
            // El combate interrumpe cualquier actividad de base, entrenamiento incluido.
            currentBuilding = null;
            target = nearest;
            state = HeroState.CombatApproach;
        }
        else if (nearest == null && IsInCombat())
        {
            EnterBaseWander();
        }
        else if (nearest != null)
        {
            target = nearest;
        }
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
        if (target == null) { EnterBaseWander(); return; }

        MoveTowards(target.transform.position);

        // Correr detrás del enemigo cansa; pararse a golpear, no.
        AddFatigue(fatiguePerSecondMoving * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.transform.position) <= attackRange)
        {
            state = HeroState.CombatAttack;
            attackTimer = 0f;   // el primer golpe sale sin esperar
        }
    }

    private void TickCombatAttack()
    {
        if (target == null) { EnterBaseWander(); return; }

        // Si se aleja, vuelve a perseguirlo.
        if (Vector2.Distance(transform.position, target.transform.position) > attackRange)
        {
            state = HeroState.CombatApproach;
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        attackTimer = EffectiveAttackCooldown;

        // Si llega el maná y la habilidad está lista, el golpe especial sustituye al básico.
        if (skill != null && skill.CanCast(CurrentMP))
        {
            currentMP -= skill.mpCost;
            skill.PutOnCooldown();

            int damage = skill.DamageFrom(Attack);
            target.TakeDamage(damage);
            AddMasteryPoints(masteryPerHit);

            Debug.Log($"[Habilidad] {data.heroName} lanza {skill.skillName}: {damage} de daño " +
                      $"(-{skill.mpCost} MP, quedan {CurrentMP}/{MaxMP}).", this);
            return;
        }

        target.TakeDamage(Attack);
        AddMasteryPoints(masteryPerHit);
    }

    private void EnterBaseWander()
    {
        target = null;
        currentBuilding = null;
        state = HeroState.BaseWander;
        PickNewWanderTarget();
    }

    private void PickNewWanderTarget()
    {
        destinationBuilding = null;

        // A veces el héroe decide ir a un edificio en vez de vagar sin rumbo.
        float chance = buildingVisitChance * HeroTraits.VisitChanceMultiplier(trait);
        if (UnityEngine.Random.value < chance)
        {
            var building = PickRandomBuilding();
            if (building != null)
            {
                destinationBuilding = building;

                // Se planta en un punto al azar dentro del radio, no todos en el mismo pixel.
                Vector2 offset = UnityEngine.Random.insideUnitCircle * (building.InteractionRadius * 0.6f);
                wanderTarget = (Vector2)building.transform.position + offset;
                return;
            }
        }

        Vector2 half = baseAreaSize * 0.5f;
        wanderTarget = baseAreaCenter + new Vector2(
            UnityEngine.Random.Range(-half.x, half.x),
            UnityEngine.Random.Range(-half.y, half.y));
    }

    // Sorteo ponderado: cada rasgo tira más hacia unos edificios que hacia otros.
    private BaseBuilding PickRandomBuilding()
    {
        var all = BaseBuilding.All;
        if (all == null || all.Count == 0) return null;

        float total = 0f;
        foreach (var b in all) total += HeroTraits.BuildingWeight(trait, b.Type);
        if (total <= 0f) return null;

        float roll = UnityEngine.Random.Range(0f, total);
        float acc = 0f;

        foreach (var b in all)
        {
            acc += HeroTraits.BuildingWeight(trait, b.Type);
            if (roll < acc) return b;
        }

        return all[all.Count - 1];
    }

    private void MoveTowards(Vector2 destination)
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            destination,
            EffectiveMoveSpeed * Time.deltaTime);
    }

    public void TakeDamage(int amount)
    {
        // Evasión: el golpe no llega, así que no hay daño, ni fatiga, ni moral perdida.
        if (HasPassive(PassiveSkill.Evasion) && UnityEngine.Random.value < evasionChance)
        {
            DamageTextManager.ShowDodge(transform.position);
            Debug.Log($"[Pasiva] {data.heroName} esquiva el golpe.", this);
            return;
        }

        int finalDamage = Mathf.Max(1, amount - Defense);
        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        HealthChanged?.Invoke(currentHealth, MaxHealth);

        DamageTextManager.ShowDamage(transform.position, finalDamage);

        // Encajar golpes cansa; con Aguante, la mitad.
        AddFatigue(fatiguePerHitTaken * (HasPassive(PassiveSkill.PainTolerance) ? painToleranceFactor : 1f));
        CheckCriticalMorale();

        Debug.Log($"[Hero] {data.heroName} recibe {finalDamage} ({currentHealth}/{MaxHealth})", this);

        if (currentHealth <= 0)
        {
            // Permadeath: el héroe no vuelve.
            NotifyAlliesOfDeath();
            Debug.Log($"[Hero] {data.heroName} ha muerto.", this);
            Destroy(gameObject);
        }
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
