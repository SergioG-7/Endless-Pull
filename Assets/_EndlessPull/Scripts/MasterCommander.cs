using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// El Maestro: intervenciones puntuales del jugador sobre los héroes en escena.
public class MasterCommander : MonoBehaviour
{
    [Tooltip("Vida que restaura la curación rápida de Espacio.")]
    [SerializeField] private int quickHealAmount = 25;

    [Tooltip("Escuadra sobre la que actúan los decretos.")]
    [SerializeField] private PartyManager party;

    [Tooltip("Segundos de espera entre usos de Curar Escuadra.")]
    [SerializeField] private float healCooldown = 10f;

    [Tooltip("Segundos de espera entre usos de Enfocar Objetivo.")]
    [SerializeField] private float focusFireCooldown = 8f;

    [Tooltip("Segundos de espera entre usos de Reagruparse.")]
    [SerializeField] private float regroupCooldown = 12f;

    [Tooltip("Gestor de oleadas al que se le pide la retirada.")]
    [SerializeField] private WaveManager waves;

    [Tooltip("Segundos que dura la posición defensiva.")]
    [SerializeField] private float regroupDuration = 4f;

    [Tooltip("Unidades que retrocede la escuadra al reagruparse.")]
    [SerializeField] private float regroupRetreat = 2f;

    private float healTimer;
    private float focusFireTimer;
    private float regroupTimer;

    public bool HealReady => healTimer <= 0f;
    public bool FocusFireReady => focusFireTimer <= 0f;
    public bool RegroupReady => regroupTimer <= 0f;
    public float HealCooldown => healCooldown;
    public float FocusFireCooldown => focusFireCooldown;
    public float RegroupCooldown => regroupCooldown;
    public float HealCooldownLeft => Mathf.Max(0f, healTimer);
    public float FocusFireCooldownLeft => Mathf.Max(0f, focusFireTimer);
    public float RegroupCooldownLeft => Mathf.Max(0f, regroupTimer);

    void Awake()
    {
        if (party == null) party = Object.FindFirstObjectByType<PartyManager>();
        if (waves == null) waves = Object.FindFirstObjectByType<WaveManager>();
    }

    // Decreto de Retirada: se abandona el piso, pero nadie se queda atrás.
    public bool Retreat()
    {
        if (waves == null) return false;

        // Se pinta antes de retirar: al volver ya no están desplegados y la lista saldría vacía.
        var salen = DeployedParty();
        if (salen.Count == 0) return false;

        foreach (var hero in salen)
            DamageTextManager.Show(hero.transform.position,
                LocalizationManager.Get("UI_RETREAT"), new Color(0.9f, 0.8f, 0.4f));

        return waves.RetreatExpedition();
    }

    void Update()
    {
        if (healTimer > 0f) healTimer -= Time.deltaTime;
        if (focusFireTimer > 0f) focusFireTimer -= Time.deltaTime;
        if (regroupTimer > 0f) regroupTimer -= Time.deltaTime;

        // El proyecto usa solo el nuevo Input System, de ahí Keyboard.current.
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.spaceKey.wasPressedThisFrame) HealParty();
        if (keyboard.digit1Key.wasPressedThisFrame) FocusFire();
        if (keyboard.digit2Key.wasPressedThisFrame) Regroup();
        if (keyboard.digit3Key.wasPressedThisFrame) Retreat();
    }

    // Decreto: toda la escuadra se centra en el enemigo más gordo, o en el jefe si lo hay.
    public bool FocusFire()
    {
        if (!FocusFireReady)
        {
            Debug.LogWarning($"[Decreto] Enfocar Objetivo aún tarda {FocusFireCooldownLeft:0.0}s.", this);
            return false;
        }

        var prey = PickPriorityEnemy();
        if (prey == null)
        {
            Debug.LogWarning("[Decreto] No hay enemigos a los que enfocar.", this);
            return false;
        }

        int count = 0;
        foreach (var hero in DeployedParty())
        {
            hero.SetForcedTarget(prey);
            count++;
        }

        if (count == 0) return false;

        focusFireTimer = focusFireCooldown;
        DamageTextManager.Show(prey.transform.position, "¡ENFOCAR!", new Color(1f, 0.55f, 0.2f));
        Debug.Log($"[Decreto] Enfocar Objetivo: {count} héroe(s) sobre {prey.Data.enemyName} " +
                  $"({prey.CurrentHealth} PV).", this);
        return true;
    }

    // Decreto: la escuadra retrocede y aguanta mejor unos segundos.
    public bool Regroup()
    {
        if (!RegroupReady)
        {
            Debug.LogWarning($"[Decreto] Reagruparse aún tarda {RegroupCooldownLeft:0.0}s.", this);
            return false;
        }

        int count = 0;
        foreach (var hero in DeployedParty())
        {
            hero.SetForcedTarget(null);
            hero.ApplyDefensiveStance(regroupDuration, regroupRetreat);
            DamageTextManager.Show(hero.transform.position, "¡DEFENSA!", new Color(0.6f, 0.8f, 1f));
            count++;
        }

        if (count == 0)
        {
            Debug.LogWarning("[Decreto] No hay escuadra desplegada que reagrupar.", this);
            return false;
        }

        regroupTimer = regroupCooldown;
        Debug.Log($"[Decreto] Reagruparse: {count} héroe(s) retroceden {regroupRetreat} unidades " +
                  $"durante {regroupDuration}s.", this);
        return true;
    }

    // Prioriza al jefe; si no lo hay, al enemigo con más vida en pie.
    private EnemyController PickPriorityEnemy()
    {
        EnemyController best = null;

        foreach (var enemy in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            if (best == null) { best = enemy; continue; }

            if (enemy.IsBoss && !best.IsBoss) { best = enemy; continue; }
            if (best.IsBoss && !enemy.IsBoss) continue;

            if (enemy.CurrentHealth > best.CurrentHealth) best = enemy;
        }

        return best;
    }

    private List<HeroController> DeployedParty()
    {
        var list = new List<HeroController>();
        if (party == null) return list;

        foreach (var hero in party.Party)
            if (hero != null && hero.IsDeployed) list.Add(hero);

        return list;
    }

    // Decreto de curación: la escuadra primero; si no hay ninguna, todo el roster.
    public bool HealParty()
    {
        if (!HealReady)
        {
            Debug.LogWarning($"[Decreto] Curar Escuadra aún tarda {HealCooldownLeft:0.0}s.", this);
            return false;
        }

        var objetivos = DeployedOrParty();
        if (objetivos.Count == 0)
        {
            HealAllHeroes(quickHealAmount);
            healTimer = healCooldown;
            return true;
        }

        int total = 0;
        foreach (var hero in objetivos) total += HealHero(hero, quickHealAmount);

        healTimer = healCooldown;
        Debug.Log($"[Decreto] Curar Escuadra: +{total} PV entre {objetivos.Count} héroe(s).", this);
        return true;
    }

    // La escuadra desplegada si la hay; si no, la escuadra apuntada.
    private List<HeroController> DeployedOrParty()
    {
        var desplegados = DeployedParty();
        if (desplegados.Count > 0) return desplegados;

        var lista = new List<HeroController>();
        if (party == null) return lista;

        foreach (var hero in party.Party)
            if (hero != null) lista.Add(hero);

        return lista;
    }

    // Cura a todos los héroes vivos de la escena.
    public void HealAllHeroes(int amount)
    {
        var heroes = Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None);
        int totalHealed = 0;

        foreach (var hero in heroes)
        {
            totalHealed += HealHero(hero, amount);
        }

        Debug.Log($"[Maestro] Curación en masa: +{totalHealed} PV repartidos entre {heroes.Length} héroe(s).", this);
    }

    // Curación dirigida a un héroe concreto; devuelve la vida realmente restaurada.
    public int HealHero(HeroController hero, int amount)
    {
        if (hero == null) return 0;
        return hero.Heal(amount);
    }
}
