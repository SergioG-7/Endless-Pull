using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

// Agente del micro-combate: decide si acercarse, retroceder, pegar o gastar la habilidad.
public class HeroAgent : Agent
{
    [Tooltip("Arena que reposiciona las unidades entre episodios.")]
    [SerializeField] private GymManager gym;

    [Tooltip("Distancia con la que se normaliza la observación de separación.")]
    [SerializeField] private float observableDistance = 20f;

    [Tooltip("Recompensa por dejar caer un golpe propio sobre el enemigo.")]
    [SerializeField] private float hitReward = 0.1f;

    [Tooltip("Recompensa por salir del radio del golpe circular mientras el jefe lo carga.")]
    [SerializeField] private float dodgeReward = 0.4f;

    [Tooltip("Penalización por gastar la habilidad en un enemigo casi muerto.")]
    [SerializeField] private float wastedSkillPenalty = 0.2f;

    [Tooltip("Vida del enemigo por debajo de la cual gastar maná se considera desperdicio.")]
    [SerializeField] private float residualHealthRatio = 0.1f;

    [Tooltip("Coste por paso, para que aprenda a resolver rápido.")]
    [SerializeField] private float stepPenalty = 0.001f;

    [Tooltip("Escribe por consola el resultado de cada episodio.")]
    [SerializeField] private bool logEpisodes = true;

    [Tooltip("Peso de entrenamiento. Multiplica hitReward y wastedSkillPenalty: cuánto se premia/castiga la agresividad de la política.")]
    [SerializeField] private float aggressionWeight = 1f;

    [Tooltip("Multiplica dodgeReward: cuánto pesa mantener la distancia de seguridad ante el golpe en área.")]
    [SerializeField] private float safeDistanceWeight = 1f;

    [Tooltip("Penalización por atacar mientras GymManager tiene activo el pulso simulado de Reagruparse.")]
    [SerializeField] private float decreeDisobeyPenalty = 0.05f;

    [Tooltip("Penalización por paso mientras sigue dentro de la marca del suelo ya encendida.")]
    [SerializeField] private float groundZonePenalty = 0.02f;

    [Tooltip("Recompensa por salir de la marca del suelo del jefe.")]
    [SerializeField] private float groundZoneEscapeReward = 0.3f;

    private HeroController hero;
    private EnemyController enemy;

    // Qué canales del consejo neuronal se obedecen en la Torre. Medido en el piso 30: con Full la
    // red nunca aconseja la habilidad y el piso se alarga un 13 %, así que arranca en Off.
    public enum AdviceMode { Off, MovementOnly, Full }

    public static AdviceMode Advice = AdviceMode.Off;

    // Sin arena el agente no manda: solo mira y deja su consejo para que lo lea la FSM.
    private bool advisor;

    // Paso aconsejado (0 quieto, 1 acercarse, 2 retroceder) y golpe aconsejado (0 nada, 1 basico,
    // 2 habilidad). Solo valen si HasAdvice.
    public int AdvisedMove { get; private set; }
    public int AdvisedCombat { get; private set; }

    // Falso hasta que llega la primera decision: antes de eso no hay nada que aconsejar.
    public bool HasAdvice { get; private set; }

    // El esquivar solo se premia una vez por cada aviso de golpe.
    private bool dodgeRewarded;

    // Salir de la marca solo se premia si antes estaba dentro.
    private bool insideZone;

    public override void Initialize()
    {
        hero = GetComponent<HeroController>();

        // Por el padre, no el primero de la escena: con varias arenas a la vez, coger cualquiera
        // dejaba al agente reapareciendo en la arena del vecino.
        if (gym == null) gym = GetComponentInParent<GymManager>();

        // Sin gimnasio (la Torre) el agente pasa a consejero: la FSM sigue decidiendo objetivo,
        // formación, decretos, retirada y soporte, y aquí solo se recogen observaciones.
        advisor = gym == null;

        // El agente manda: la máquina de estados del héroe se aparta y morir no lo destruye.
        if (hero != null && !advisor) hero.ExternalControl = true;
    }

    public override void OnEpisodeBegin()
    {
        if (advisor) return;

        dodgeRewarded = false;
        insideZone = false;
        if (gym != null) enemy = gym.ResetArena(hero);
    }

    // 8 de situación + 3 de mecánica de jefe + 2 de arma + 1 de enfriamiento + 1 de rival a
    // distancia + 2 de rol + 3 del vector de rasgos. Si se añade una, hay que subir también
    // Behavior Parameters > Space Size en la escena Y volver a entrenar: cambia la entrada.
    public const int ObservationSize = 20;

    public override void CollectObservations(VectorSensor sensor)
    {
        if (hero == null)
        {
            sensor.AddObservation(new float[ObservationSize]);
            return;
        }

        // De consejero el rival lo elige la FSM; en el gimnasio lo reparte la arena.
        if (advisor) enemy = hero.CurrentTarget;

        sensor.AddObservation(hero.MaxHealth > 0 ? (float)hero.CurrentHealth / hero.MaxHealth : 0f);
        sensor.AddObservation(hero.MaxMP > 0 ? (float)hero.CurrentMP / hero.MaxMP : 0f);
        sensor.AddObservation(hero.Fatigue / 100f);
        sensor.AddObservation(Mathf.Clamp01(DistanceToEnemy() / observableDistance));

        // Estado del enemigo en tres casillas: quieto, pegando y cargando el golpe en área.
        bool vivo = enemy != null;
        bool cargando = vivo && enemy.IsWindingUp;
        bool pegando = vivo && !cargando && enemy.State == EnemyState.Attack;
        sensor.AddObservation(vivo && !cargando && !pegando ? 1f : 0f);
        sensor.AddObservation(pegando ? 1f : 0f);
        sensor.AddObservation(cargando ? 1f : 0f);

        sensor.AddObservation(hero.CanCastSkill ? 1f : 0f);

        // Mecánicas del jefe: la marca del suelo (dentro / ya quemando) y la barrera del objetivo.
        var zona = BossGroundZone.ThreatAt(transform.position);
        sensor.AddObservation(zona != null ? 1f : 0f);
        sensor.AddObservation(zona != null && zona.IsBurning ? 1f : 0f);
        sensor.AddObservation(vivo && enemy.HasBarrier ? 1f : 0f);

        // Con qué pelea: sin esto la red daba consejos de espadachín a un arquero, porque no veía
        // ni que fuera de rango ni a qué distancia alcanza.
        sensor.AddObservation(hero.IsRanged ? 1f : 0f);
        sensor.AddObservation(Mathf.Clamp01(hero.AttackReach / observableDistance));

        // Su propio enfriamiento: sin esto se le pedía temporizar los golpes a ciegas y la orden
        // de atacar caía en un ataque que aún no estaba listo la mayor parte del tiempo.
        sensor.AddObservation(hero.AttackReady ? 1f : 0f);

        // Si el rival también pega de lejos, retroceder no salva de nada.
        sensor.AddObservation(vivo && enemy.IsRanged ? 1f : 0f);

        // Rol: el clérigo cura en vez de pegar y el tanque aguanta el golpe en área a propósito.
        sensor.AddObservation(hero.IsSupport ? 1f : 0f);
        sensor.AddObservation(hero.IsTank ? 1f : 0f);

        // Vector de rasgos: la red no solo ve la situación, también a quién la está viviendo.
        var rasgos = hero.TraitVector;
        sensor.AddObservation(rasgos.Bravery);
        sensor.AddObservation(rasgos.Composure);
        sensor.AddObservation(rasgos.Cooperation);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (hero == null) return;

        int movimiento = actions.DiscreteActions[0];
        int combate = actions.DiscreteActions[1];

        // De consejero no se premia ni se mueve nada: la decisión se guarda y la aplica la FSM.
        if (advisor)
        {
            AdvisedMove = movimiento;
            AdvisedCombat = combate;
            HasAdvice = true;
            return;
        }

        AddReward(-stepPenalty);

        ApplyMovement(movimiento);
        ApplyCombat(combate);

        TickGroundZoneReward();
        ResolveEpisodeEnd();
    }

    // Sin política entrenada el héroe se acerca y pega, con el mismo perfil táctico (agresividad,
    // distancia de seguridad, umbral de habilidad) que usa en combate real: sirve para probar la
    // arena a mano y para ver de un vistazo cómo se comportaría cada héroe según su personalidad.
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var acciones = actionsOut.DiscreteActions;
        float distancia = DistanceToEnemy();

        // Salir de la marca del suelo va antes que nada: se elige el sentido que aleja del centro.
        var zona = BossGroundZone.ThreatAt(transform.position);
        if (zona != null && enemy != null)
        {
            Vector2 haciaEnemigo = ((Vector2)(enemy.transform.position - transform.position)).normalized;
            acciones[0] = Vector2.Dot(haciaEnemigo, (Vector2)transform.position - zona.Center) > 0f ? 1 : 2;
            acciones[1] = 0;
            return;
        }

        // Mismo criterio que HeroController.TickCombatAttack: el tanque aguanta y el resto esquiva
        // solo si es más prudente que agresivo.
        bool peligro = enemy != null && !hero.IsTank && enemy.IsWindingUp
                       && distancia < enemy.SlamRadius && hero.SafeDistance > hero.Aggression;
        acciones[0] = peligro ? 2 : (distancia > hero.AttackReach ? 1 : 0);

        // El clérigo se cura cuando le hace falta y pega el resto del tiempo.
        if (hero.IsSupport && hero.CanCastSkill && hero.MaxHealth > 0
            && (float)hero.CurrentHealth / hero.MaxHealth < 0.85f)
        {
            acciones[1] = 2;
            return;
        }

        if (distancia > hero.AttackReach) acciones[1] = 0;
        else if (hero.CanCastSkill && !EnemyIsAlmostDead()) acciones[1] = 2;
        else acciones[1] = 1;
    }

    private void ApplyMovement(int movimiento)
    {
        if (movimiento == 0 || enemy == null) return;

        Vector2 haciaEnemigo = ((Vector2)(enemy.transform.position - transform.position)).normalized;
        Vector2 direccion = movimiento == 1 ? haciaEnemigo : -haciaEnemigo;

        Vector2 destino = (Vector2)transform.position
                        + direccion * hero.EffectiveMoveSpeed * Time.deltaTime;

        transform.position = gym != null ? gym.Clamp(destino) : destino;

        // Salir del radio mientras el jefe carga es justo lo que se quiere enseñar; al tanque no,
        // que en la Torre aguanta la línea a propósito (TickCombatAttack filtra por IsTank).
        if (movimiento == 2 && !dodgeRewarded && !hero.IsTank && enemy.IsWindingUp
            && DistanceToEnemy() > enemy.SlamRadius)
        {
            dodgeRewarded = true;
            AddReward(dodgeReward * safeDistanceWeight);
        }
    }

    private void ApplyCombat(int combate)
    {
        if (combate == 0 || enemy == null) return;

        if (combate == 1)
        {
            if (!hero.TryBasicAttack(enemy)) return;

            AddReward(hitReward * aggressionWeight);
            CobrarDesobediencia();
            return;
        }

        // El clérigo no lanza la habilidad contra nadie: cura, y en el gimnasio la escuadra es él
        // solo. Curar a pleno es tirar el maná igual que rematar con la habilidad a un moribundo.
        if (hero.IsSupport)
        {
            bool sano = hero.MaxHealth > 0 && (float)hero.CurrentHealth / hero.MaxHealth >= 0.85f;
            if (!hero.CastSupportSkill(hero)) return;

            AddReward(sano ? -wastedSkillPenalty * aggressionWeight : hitReward * aggressionWeight);
            return;
        }

        // Rematar con la habilidad a un enemigo casi muerto es tirar el maná.
        bool desperdicio = EnemyIsAlmostDead();
        if (!hero.TrySkillAttack(enemy)) return;

        AddReward(desperdicio ? -wastedSkillPenalty * aggressionWeight : hitReward * aggressionWeight);
        CobrarDesobediencia();
    }

    // GymManager simula el decreto Reagruparse; desobedecerlo es soltar un golpe mientras dura, y
    // se cobra SOLO por golpe soltado. Cobrándolo por decisión (aunque el ataque no llegara a
    // salir, por estar fuera de alcance o en enfriamiento) el castigo se llevaba entre 3 y 6
    // puntos por episodio: había victorias con recompensa negativa y la señal no dependía de lo
    // que el agente controla.
    private void CobrarDesobediencia()
    {
        if (hero.IsInDefensiveStance) AddReward(-decreeDisobeyPenalty);
    }

    // Quedarse dentro de la marca encendida cuesta cada paso; salir de ella se paga una vez.
    private void TickGroundZoneReward()
    {
        var zona = BossGroundZone.ThreatAt(transform.position);

        if (zona != null)
        {
            insideZone = true;
            if (zona.IsBurning) AddReward(-groundZonePenalty * safeDistanceWeight);
            return;
        }

        if (!insideZone) return;

        insideZone = false;
        AddReward(groundZoneEscapeReward * safeDistanceWeight);
    }

    private void ResolveEpisodeEnd()
    {
        // El aviso del jefe se rearma en cuanto termina, para poder premiar la siguiente esquiva.
        if (enemy != null && !enemy.IsWindingUp) dodgeRewarded = false;

        if (hero.IsDead)
        {
            AddReward(-1f);
            if (logEpisodes)
                Debug.Log($"[Gym] Episodio {(gym != null ? gym.Episodes : 0)}: DERROTA  " +
                          $"recompensa={GetCumulativeReward():0.000}", this);
            EndEpisode();
            return;
        }

        // El enemigo se destruye al morir, así que su hueco vacío es la señal de victoria.
        if (enemy == null)
        {
            AddReward(1f);
            if (logEpisodes)
                Debug.Log($"[Gym] Episodio {(gym != null ? gym.Episodes : 0)}: VICTORIA  " +
                          $"recompensa={GetCumulativeReward():0.000}  " +
                          $"PV={hero.CurrentHealth}/{hero.MaxHealth}", this);
            EndEpisode();
        }
    }

    // Umbral por héroe si hay HeroProgress (el mismo que en combate real); si no, el global del inspector.
    private bool EnemyIsAlmostDead()
    {
        // Con barrera la vida no baja: no hay nada que rematar, y la habilidad sirve para romperla.
        if (enemy == null || enemy.MaxHealth <= 0 || enemy.HasBarrier) return false;

        float ratio = hero != null ? hero.SkillThreshold : residualHealthRatio;
        return (float)enemy.CurrentHealth / enemy.MaxHealth < ratio;
    }

    private float DistanceToEnemy()
        => enemy != null ? Vector2.Distance(transform.position, enemy.transform.position) : observableDistance;
}
