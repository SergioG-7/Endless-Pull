using UnityEngine;

// Catálogo de sprites de edificio por tipo. Un único asset compartido por las 16 instancias: al
// llegar arte nuevo se rellena aquí y aparece en la base sin tocar la escena.
[CreateAssetMenu(fileName = "BuildingArt", menuName = "Endless Pull/Building Art")]
public class BuildingArt : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("Tipo de edificio al que pertenece el sprite.")]
        public BuildingType type;

        [Tooltip("Ilustración del edificio, ya recortada con transparencia.")]
        public Sprite sprite;

        [Tooltip("Ancho en unidades de mundo; 0 usa el ancho por defecto del catálogo.")]
        public float width;
    }

    [Tooltip("Ancho por defecto de un edificio, en unidades de mundo.")]
    public float defaultWidth = 6.8f;

    [Tooltip("Niebla densa que cubre un edificio bloqueado; sustituye al recuadro tintado.")]
    public Sprite lockedFog;

    [Tooltip("Cuánto desborda la niebla la huella del edificio, en tanto por uno.")]
    public float lockedFogOverflow = 1.25f;

    [Tooltip("Un sprite por tipo; los tipos que falten se quedan con el bloque tintado de siempre.")]
    public Entry[] entries = new Entry[0];

    public Entry For(BuildingType type)
    {
        if (entries == null) return null;

        foreach (var entry in entries)
            if (entry != null && entry.type == type && entry.sprite != null) return entry;

        return null;
    }

    public float WidthFor(Entry entry)
        => entry != null && entry.width > 0f ? entry.width : defaultWidth;
}
