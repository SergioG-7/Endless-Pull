using UnityEngine;

// Única fuente de verdad de las franjas de bioma de la Torre (ver design/levels/torre-biomas.md
// y design/audio/audio-torre-dinamica.md, gap 6). Antes solo vivían en el documento de diseño.
public static class TowerBiome
{
    // Último piso de cada franja (Goblin, Minas, Cripta); Templo es todo lo que quede por encima, sin techo.
    private static readonly int[] FloorCap = { 5, 10, 15 };

    // 0 = Goblin, 1 = Minas, 2 = Cripta, 3 = Templo (16+, se repite sin variar de índice).
    public static int IndexForFloor(int floor)
    {
        for (int i = 0; i < FloorCap.Length; i++)
            if (floor <= FloorCap[i]) return i;

        return FloorCap.Length;
    }

    // Placeholder visual sin arte final: tinte de fondo de cámara por bioma, mismo índice que IndexForFloor.
    public static readonly Color[] Tint =
    {
        new Color(0.09f, 0.12f, 0.08f), // Goblin: verde apagado
        new Color(0.13f, 0.11f, 0.08f), // Minas: ocre apagado
        new Color(0.08f, 0.08f, 0.13f), // Cripta: violeta apagado
        new Color(0.14f, 0.09f, 0.07f), // Templo: rojo apagado
    };
}
