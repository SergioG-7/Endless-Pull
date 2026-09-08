using UnityEngine;

// Única fuente de verdad de las franjas de bioma de la Torre (ver design/levels/torre-biomas.md
// y design/audio/audio-torre-dinamica.md, gap 6). Antes solo vivían en el documento de diseño.
public static class TowerBiome
{
    // Pisos que dura cada bioma antes de pasar al siguiente.
    public const int FloorsPerBiome = 15;

    // 0 = Goblin, 1 = Minas, 2 = Cripta, 3 = Templo, y vuelta a empezar. Con solo cuatro fondos,
    // franjas de 5 pisos dejaban el Templo fijo de por vida a partir del 16.
    public static int IndexForFloor(int floor)
        => ((Mathf.Max(1, floor) - 1) / FloorsPerBiome) % Tint.Length;

    // Placeholder visual sin arte final: tinte de fondo de cámara por bioma, mismo índice que IndexForFloor.
    public static readonly Color[] Tint =
    {
        new Color(0.09f, 0.12f, 0.08f), // Goblin: verde apagado
        new Color(0.13f, 0.11f, 0.08f), // Minas: ocre apagado
        new Color(0.08f, 0.08f, 0.13f), // Cripta: violeta apagado
        new Color(0.14f, 0.09f, 0.07f), // Templo: rojo apagado
    };
}
