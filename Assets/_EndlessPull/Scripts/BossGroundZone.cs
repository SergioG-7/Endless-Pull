using System.Collections.Generic;
using UnityEngine;

// Marca de suelo del jefe. Es un componente y no un GameObject suelto para que los héroes puedan
// consultarla: el registro estático evita recorrer la escena entera en cada tick de combate.
public class BossGroundZone : MonoBehaviour
{
    private static readonly List<BossGroundZone> active = new List<BossGroundZone>();

    public Vector2 Center { get; private set; }
    public float Radius { get; private set; }

    // Durante el aviso la marca todavía no quema: esa es la ventana para salir.
    public bool IsBurning { get; private set; }

    // Vida propia: si el jefe muere a mitad, su corrutina no llega a borrar la marca y se quedaría
    // en la arena espantando a la escuadra para siempre.
    private float lifetime = 12f;

    public void Setup(Vector2 center, float radius, float maxLifetime)
    {
        Center = center;
        Radius = Mathf.Max(0.1f, radius);
        lifetime = Mathf.Max(0.1f, maxLifetime);
    }

    public void Arm() => IsBurning = true;

    void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) Destroy(gameObject);
    }

    void OnEnable() => active.Add(this);

    void OnDisable() => active.Remove(this);

    public bool Contains(Vector2 position)
        => (position - Center).sqrMagnitude <= Radius * Radius;

    // Zona que pilla a ese punto, si hay alguna; con varias encima manda la que ya está quemando.
    public static BossGroundZone ThreatAt(Vector2 position)
    {
        BossGroundZone peor = null;

        foreach (var zone in active)
        {
            if (zone == null || !zone.Contains(position)) continue;
            if (peor == null || (zone.IsBurning && !peor.IsBurning)) peor = zone;
        }

        return peor;
    }

    // Salida por el vector más corto, con margen para no quedarse pegado al borde.
    public Vector2 EscapePoint(Vector2 from, float margin)
    {
        Vector2 salida = from - Center;
        if (salida.sqrMagnitude < 0.0001f) salida = Vector2.right;

        return Center + salida.normalized * (Radius + margin);
    }
}
