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
    [SerializeField] private float decreeDisobeyPenalty = 0.15f;

    private HeroController hero;
    private EnemyController enemy;

    // El esquivar solo se premia una vez por cada aviso de golpe.
    private bool dodgeRewarded;

    public override void Initialize()
    {
        hero = GetComponent<HeroController>();
        if (gym == null) gym = UnityEngine.Object.FindFirstObjectByType<GymManager>();

        // El agente manda: la máquina de estados del héroe se aparta y morir no lo destruye.
        if (hero != null) hero.ExternalControl = true;
    }

    public override void OnEpisodeBegin()
    {
        dodgeRewarded = false;
        if (gym != null) enemy = gym.ResetArena(hero);
    }

    // 8 observaciones de situación + 3 canales del vector de rasgos. Si se añade una, hay que
    // subir también Behavior Parameters > Vector Observation > Space Size en el prefab del agente.
    public const int ObservationSize = 11;

    public override void CollectObservations(VectorSensor sensor)
    {
        if (hero == null)
        {
            sensor.AddObservation(new float[ObservationSize]);
            return;
        }

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

        // Vector de rasgos: la red no solo ve la situación, también a quién la está viviendo.
        var rasgos = hero.TraitVector;
        sensor.AddObservation(rasgos.Bravery);
        sensor.AddObservation(rasgos.Composure);
        sensor.AddObservation(rasgos.Cooperation);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (hero == null) return;

        AddReward(-stepPenalty);

        int movimiento = actions.DiscreteActions[0];
        int combate = actions.DiscreteActions[1];

        ApplyMovement(movimiento);
        ApplyCombat(combate);

        ResolveEpisodeEnd();
    }

    // Sin política entrenada el héroe se acerca y pega, con el mismo perfil táctico (agresividad,
    // distancia de seguridad, umbral de habilidad) que usa en combate real: sirve para probar la
    // arena a mano y para ver de un vistazo cómo se comportaría cada héroe según su personalidad.
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var acciones = actionsOut.DiscreteActions;
        float distancia = DistanceToEnemy();

        // Mismo criterio que HeroController.TickCombatAttack: esquiva solo si es más prudente que agresivo.
        bool peligro = enemy != null && enemy.IsWindingUp && distancia < enemy.SlamRadius
                       && hero.SafeDistance > hero.Aggression;
        acciones[0] = peligro ? 2 : (distancia > hero.AttackReach ? 1 : 0);

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

        // Salir del radio mientras el jefe carga es justo lo que se quiere enseñar.
        if (movimiento == 2 && !dodgeRewarded && enemy.IsWindingUp
            && DistanceToEnemy() > enemy.SlamRadius)
        {
            dodgeRewarded = true;
            AddReward(dodgeReward * safeDistanceWeight);
        }
    }

    private void ApplyCombat(int combate)
    {
        if (combate == 0 || enemy == null) return;

        // GymManager simula el decreto Reagruparse; atacar mientras está activo es desobedecerlo.
        if (hero.IsInDefensiveStance) AddReward(-decreeDisobeyPenalty);

        if (combate == 1)
        {
            if (hero.TryBasicAttack(enemy)) AddReward(hitReward * aggressionWeight);
            return;
        }

        // Rematar con la habilidad a un enemigo casi muerto es tirar el maná.
        bool desperdicio = EnemyIsAlmostDead();
        if (!hero.TrySkillAttack(enemy)) return;

        AddReward(desperdicio ? -wastedSkillPenalty * aggressionWeight : hitReward * aggressionWeight);
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
        if (enemy == null || enemy.MaxHealth <= 0) return false;

        float ratio = hero != null ? hero.SkillThreshold : residualHealthRatio;
        return (float)enemy.CurrentHealth / enemy.MaxHealth < ratio;
    }

    private float DistanceToEnemy()
        => enemy != null ? Vector2.Distance(transform.position, enemy.transform.position) : observableDistance;
}
