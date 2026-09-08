using System.Collections;
using UnityEngine;

// Congelación breve del tiempo (hitstop) en los golpes con más peso: críticos y golpes de jefe.
// Presupuesto: como mucho un hitstop activo a la vez; uno nuevo reinicia al que estuviera corriendo.
public class CombatFeelManager : MonoBehaviour
{
    [Tooltip("Escala de tiempo durante el hitstop; 0 sería pausa total.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float hitstopTimeScale = 0.05f;

    [Tooltip("Duración del hitstop en un golpe crítico del héroe.")]
    [SerializeField] private float critHitStopDuration = 0.04f;

    [Tooltip("Magnitud del temblor de cámara en un golpe crítico.")]
    [SerializeField] private float critShakeMagnitude = 0.08f;

    [Tooltip("Duración del temblor de cámara en un golpe crítico.")]
    [SerializeField] private float critShakeDuration = 0.12f;

    [Tooltip("Duración del hitstop cuando un golpe de jefe conecta contra un héroe.")]
    [SerializeField] private float bossHitStopDuration = 0.08f;

    [Tooltip("Magnitud del temblor de cámara en un golpe de jefe.")]
    [SerializeField] private float bossShakeMagnitude = 0.22f;

    [Tooltip("Duración del temblor de cámara en un golpe de jefe.")]
    [SerializeField] private float bossShakeDuration = 0.22f;

    [Tooltip("Duración del hitstop cuando cae un héroe; es la pausa más larga del juego.")]
    [SerializeField] private float heroFallHitStopDuration = 0.16f;

    [Tooltip("Magnitud y duración del temblor al caer un héroe.")]
    [SerializeField] private float heroFallShakeMagnitude = 0.3f;
    [SerializeField] private float heroFallShakeDuration = 0.35f;

    [Tooltip("Cuánto se asoma la cámara hacia el héroe caído, en unidades de mundo.")]
    [SerializeField] private float heroFallNudgeDistance = 0.7f;

    [Tooltip("Segundos que tarda la cámara en asomarse y volver.")]
    [SerializeField] private float heroFallNudgeDuration = 0.8f;

    [Tooltip("Hitstop y temblor al morir un enemigo; muy cortos, pasa muchas veces por piso.")]
    [SerializeField] private float enemyDeathHitStopDuration = 0.03f;
    [SerializeField] private float enemyDeathShakeMagnitude = 0.05f;
    [SerializeField] private float enemyDeathShakeDuration = 0.08f;

    private static CombatFeelManager instance;
    private Coroutine routine;
    private float defaultFixedDelta;

    // Velocidad a la que va el juego cuando no hay hitstop: la fija el botón de x1/x2. Antes se
    // leía el reloj al entrar en el hitstop, y dos muertes seguidas dejaban el juego a cámara
    // lenta para siempre, porque la segunda guardaba como "normal" la velocidad de la primera.
    private static float normalTimeScale = 1f;

    // La llaman el HUD de combate y el gimnasio al cambiar de velocidad.
    public static void SetNormalTimeScale(float value)
    {
        normalTimeScale = Mathf.Max(0.01f, value);

        // Sin hitstop en curso el cambio se aplica ya; con uno corriendo lo recoge al acabar.
        if (instance == null || instance.routine == null)
        {
            Time.timeScale = normalTimeScale;
            Time.fixedDeltaTime = 0.02f * normalTimeScale;
        }
    }

    void Awake()
    {
        instance = this;
        defaultFixedDelta = Time.fixedDeltaTime;
    }

    void OnDestroy()
    {
        if (instance != this) return;

        instance = null;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDelta;
    }

    // Golpe crítico del héroe: hitstop corto y un temblor apenas perceptible.
    public static void OnCriticalHit()
    {
        if (instance == null) return;

        instance.TriggerHitStop(instance.critHitStopDuration);
        CameraDirector.Shake(instance.critShakeDuration, instance.critShakeMagnitude);
    }

    // Golpe de jefe conectando contra un héroe: hitstop y temblor más marcados.
    public static void OnBossImpact()
    {
        if (instance == null) return;

        instance.TriggerHitStop(instance.bossHitStopDuration);
        CameraDirector.Shake(instance.bossShakeDuration, instance.bossShakeMagnitude);
    }

    // Cae un héroe: es permadeath, así que se para el juego un momento, tiembla y la cámara se
    // asoma sin cambiar el encuadre. Lo más marcado que hace el combate.
    public static void OnHeroFallen(Vector3 position)
    {
        if (instance == null) return;

        instance.TriggerHitStop(instance.heroFallHitStopDuration);
        CameraDirector.Shake(instance.heroFallShakeDuration, instance.heroFallShakeMagnitude);
        CameraDirector.Nudge(position, instance.heroFallNudgeDuration, instance.heroFallNudgeDistance);
    }

    // Muere un enemigo: el mismo gesto, pero apenas perceptible.
    public static void OnEnemyDeath()
    {
        if (instance == null) return;

        instance.TriggerHitStop(instance.enemyDeathHitStopDuration);
        CameraDirector.Shake(instance.enemyDeathShakeDuration, instance.enemyDeathShakeMagnitude);
    }

    private void TriggerHitStop(float duration)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(HitStopRoutine(duration));
    }

    // Tiempo real, no de juego: si no, el propio Time.timeScale congelado no dejaría avanzar la espera.
    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = hitstopTimeScale;
        Time.fixedDeltaTime = defaultFixedDelta * hitstopTimeScale;

        yield return new WaitForSecondsRealtime(duration);

        routine = null;

        // Si alguien pisó el reloj mientras durábamos (el menú pausando a 0, por ejemplo), manda
        // lo suyo: restaurar aquí es lo que despausaba el juego con el menú abierto.
        if (!Mathf.Approximately(Time.timeScale, hitstopTimeScale)) yield break;

        Time.timeScale = normalTimeScale;
        Time.fixedDeltaTime = defaultFixedDelta * normalTimeScale;
    }
}
