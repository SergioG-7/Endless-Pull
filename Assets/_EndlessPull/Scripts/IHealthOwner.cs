using System;

// Contrato mínimo para que la barra de vida sirva igual a héroes y a enemigos.
public interface IHealthOwner
{
    int CurrentHealth { get; }
    int MaxHealth { get; }

    // Se dispara con (vidaActual, vidaMaxima) cada vez que cambia la salud.
    event Action<int, int> HealthChanged;
}
