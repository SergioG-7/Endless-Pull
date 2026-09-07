using System.Collections.Generic;
using UnityEngine;

// Lo que un héroe ha decidido hacer ahora mismo; se enseña en pantalla para que se entienda por
// qué pelea como pelea.
public enum CombatIntent
{
    None,
    Focusing,     // va a por el objetivo prioritario de la escuadra
    Holding,      // sujeta la línea sobre el que tiene encima
    SavingSkill,  // tiene habilidad lista pero se la guarda para algo que la merezca
    Protecting,   // soporte: mira a los suyos, no al enemigo
    Kiting        // a distancia con alguien encima: se separa antes de disparar
}

public static class CombatIntents
{
    public static string DisplayName(CombatIntent intent)
        => intent == CombatIntent.None
           ? string.Empty
           : LocalizationManager.Get("INTENT_" + intent.ToString().ToUpperInvariant());

    // Símbolo corto que acompaña al rótulo; se lee de un vistazo sin tener que ir al texto.
    public static string Glyph(CombatIntent intent)
    {
        switch (intent)
        {
            case CombatIntent.Focusing: return "»";
            case CombatIntent.Holding: return "#";
            case CombatIntent.SavingSkill: return "~";
            case CombatIntent.Protecting: return "+";
            case CombatIntent.Kiting: return "<";
        }
        return string.Empty;
    }

    public static Color Tint(CombatIntent intent)
    {
        switch (intent)
        {
            case CombatIntent.Focusing: return new Color(1f, 0.55f, 0.35f);
            case CombatIntent.Holding: return UITheme.Amber;
            case CombatIntent.SavingSkill: return new Color(0.70f, 0.60f, 1f);
            case CombatIntent.Protecting: return new Color(0.45f, 0.95f, 0.85f);
            case CombatIntent.Kiting: return new Color(0.55f, 0.85f, 1f);
        }
        return UITheme.Text;
    }
}

// Capa táctica de la escuadra: cada pocos segundos lee el campo, elige a quién conviene centrar
// y reparte una intención por héroe. No pausa el combate ni sustituye a la FSM de HeroController:
// solo decide a qué objetivo va cada uno y deja el porqué a la vista.
public class SquadTactics : MonoBehaviour
{
    [Tooltip("Segundos entre lecturas del campo de batalla.")]
    [SerializeField] private float evaluationInterval = 2.5f;

    [Tooltip("Peso del peligro del enemigo al elegir objetivo prioritario (tiradores y jefes).")]
    [SerializeField] private float threatWeight = 40f;

    [Tooltip("Peso de rematar a un enemigo ya tocado en vez de repartir daño.")]
    [SerializeField] private float finishWeight = 35f;

    [Tooltip("Penalización por unidad de distancia hasta el objetivo prioritario.")]
    [SerializeField] private float distancePenalty = 6f;

    [Tooltip("Penalización por cada unidad que haya que andar de más para saltarse la primera línea.")]
    [SerializeField] private float lineSkipPenalty = 25f;

    [Tooltip("Vida del enemigo por debajo de la cual no merece la pena gastarle una habilidad.")]
    [SerializeField, Range(0f, 1f)] private float almostDeadRatio = 0.20f;

    private float timer;

    // El enemigo que la escuadra ha decidido centrar; lo leen el HUD y los propios héroes.
    public EnemyController FocusTarget { get; private set; }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = evaluationInterval;
        Evaluate();
    }

    private void Evaluate()
    {
        var heroes = new List<HeroController>();
        foreach (var hero in Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
            if (hero != null && hero.IsDeployed && hero.CurrentHealth > 0) heroes.Add(hero);

        if (heroes.Count == 0) { FocusTarget = null; return; }

        var enemies = new List<EnemyController>();
        foreach (var enemy in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            if (enemy != null && enemy.CurrentHealth > 0 && !enemy.IsFrozen) enemies.Add(enemy);

        if (enemies.Count == 0) { FocusTarget = null; return; }

        FocusTarget = PickFocus(heroes, enemies);
        foreach (var hero in heroes) AssignIntent(hero, enemies);
    }

    // A quién conviene centrar: primero lo que más daño hace desde lejos, luego lo que ya está
    // tocado, y siempre penalizando lo que queda a tomar por saco.
    private EnemyController PickFocus(List<HeroController> heroes, List<EnemyController> enemies)
    {
        Vector2 centro = Vector2.zero;
        foreach (var hero in heroes) centro += (Vector2)hero.transform.position;
        centro /= heroes.Count;

        EnemyController mejor = null;
        float mejorPuntos = float.MinValue;

        // Lo que hay en primera línea, para medir contra eso. Sin esta referencia el bono de
        // tirador (que son justo los que aparecen en retaguardia) ganaba siempre a los tres o
        // cuatro pasos de más que cuesta llegar hasta ellos, y la escuadra cruzaba entera por
        // delante de la primera línea enemiga sin tocarla.
        float distanciaMinima = float.MaxValue;
        foreach (var enemy in enemies)
            distanciaMinima = Mathf.Min(distanciaMinima,
                                        Vector2.Distance(centro, enemy.transform.position));

        foreach (var enemy in enemies)
        {
            float puntos = 0f;

            // Tiradores y chamanes pegan sin exponerse: son los que hay que quitar de en medio.
            if (enemy.IsRanged) puntos += threatWeight;

            // Rematar al que ya está tocado saca un enemigo del campo antes.
            float vidaRestante = enemy.MaxHealth > 0 ? (float)enemy.CurrentHealth / enemy.MaxHealth : 1f;
            puntos += (1f - vidaRestante) * finishWeight;

            float distancia = Vector2.Distance(centro, enemy.transform.position);
            puntos -= distancia * distancePenalty;
            puntos -= (distancia - distanciaMinima) * lineSkipPenalty;

            if (puntos <= mejorPuntos) continue;

            mejorPuntos = puntos;
            mejor = enemy;
        }

        return mejor;
    }

    private void AssignIntent(HeroController hero, List<EnemyController> enemies)
    {
        // El decreto del Maestro manda por encima de la escuadra: si hay uno puesto, no se toca.
        if (hero.HasForcedTarget) return;

        // Los clérigos no eligen enemigo; su trabajo es la escuadra.
        if (hero.IsSupport)
        {
            hero.SetIntent(CombatIntent.Protecting, null);
            return;
        }

        var encima = NearestEnemy(hero, enemies);

        // Un tanque sujeta lo que tenga delante en vez de irse a por el prioritario: si se va,
        // deja pasar a los de rango hasta la retaguardia.
        if (hero.IsTank && encima != null)
        {
            hero.SetIntent(CombatIntent.Holding, encima);
            return;
        }

        // A distancia con alguien pegado: separarse primero, disparar después.
        if (hero.IsRanged && encima != null
            && Vector2.Distance(hero.transform.position, encima.transform.position) < hero.AttackReach * 0.6f)
        {
            hero.SetIntent(CombatIntent.Kiting, encima);
            return;
        }

        var objetivo = FocusTarget != null ? FocusTarget : encima;
        if (objetivo == null) return;

        // Guardarse la habilidad para algo que la merezca es una decisión, y se enseña como tal:
        // el héroe la tiene lista pero el objetivo se cae con un golpe normal.
        float vidaObjetivo = objetivo.MaxHealth > 0
            ? (float)objetivo.CurrentHealth / objetivo.MaxHealth
            : 1f;

        if (hero.CanCastSkill && vidaObjetivo <= almostDeadRatio)
        {
            hero.SetIntent(CombatIntent.SavingSkill, objetivo);
            return;
        }

        hero.SetIntent(CombatIntent.Focusing, objetivo);
    }

    private static EnemyController NearestEnemy(HeroController hero, List<EnemyController> enemies)
    {
        EnemyController cerca = null;
        float mejor = float.MaxValue;

        foreach (var enemy in enemies)
        {
            float d = Vector2.SqrMagnitude(enemy.transform.position - hero.transform.position);
            if (d >= mejor) continue;

            mejor = d;
            cerca = enemy;
        }

        return cerca;
    }
}
