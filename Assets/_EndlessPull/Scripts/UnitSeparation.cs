using System.Collections.Generic;
using UnityEngine;

// Empuje suave entre unidades del mismo bando para que no se apilen en un punto.
public class UnitSeparation : MonoBehaviour
{
    [Tooltip("Distancia a la que dos unidades empiezan a empujarse.")]
    [SerializeField] private float radius = 1.2f;

    [Tooltip("Unidades por segundo del empuje cuando están totalmente encima.")]
    [SerializeField] private float strength = 1.6f;

    [Tooltip("Vecinos que se tienen en cuenta como mucho; evita corros enormes.")]
    [SerializeField] private int maxNeighbours = 6;

    // Héroes y enemigos se separan por separado: si se repelieran entre bandos no llegarían a pegarse.
    private static readonly List<UnitSeparation> heroes = new List<UnitSeparation>();
    private static readonly List<UnitSeparation> enemies = new List<UnitSeparation>();

    private List<UnitSeparation> group;

    void OnEnable()
    {
        group = GetComponent<HeroController>() != null ? heroes : enemies;
        if (!group.Contains(this)) group.Add(this);
    }

    void OnDisable()
    {
        if (group != null) group.Remove(this);
    }

    // En LateUpdate: primero la FSM decide adónde va y luego se corrige el amontonamiento.
    void LateUpdate()
    {
        if (group == null || group.Count < 2) return;

        Vector2 propia = transform.position;
        Vector2 empuje = Vector2.zero;
        int contados = 0;

        foreach (var otra in group)
        {
            if (otra == null || otra == this) continue;

            Vector2 delta = propia - (Vector2)otra.transform.position;
            float distancia = delta.magnitude;
            if (distancia > radius) continue;

            // Dos unidades exactamente encima no tienen dirección: se les da una al azar.
            if (distancia < 0.0001f)
            {
                empuje += Random.insideUnitCircle.normalized;
                contados++;
                continue;
            }

            // Cuanto más cerca, más fuerte: a radio completo el empuje es cero.
            empuje += delta / distancia * (1f - distancia / radius);
            contados++;

            if (contados >= maxNeighbours) break;
        }

        if (contados == 0) return;

        Vector2 paso = empuje / contados * strength * Time.deltaTime;
        transform.position = propia + paso;
    }
}
