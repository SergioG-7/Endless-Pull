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

    private static CombatFeelManager instance;
    private Coroutine routine;
    private float defaultFixedDelta;

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

        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDelta;
        routine = null;
    }
}
