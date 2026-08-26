using UnityEngine;

[CreateAssetMenu(fileName = "Hero_New", menuName = "Endless Pull/Hero Data")]
public class HeroData : ScriptableObject
{
    [Tooltip("Nombre visible del héroe.")]
    public string heroName = "Loki";

    [Tooltip("Título con el que se le conoce.")]
    public string title = "Novato de la Vanguardia";

    [Tooltip("Lugar del que viene.")]
    public string origin = "Reino Fronterizo";

    [Tooltip("Trasfondo breve del héroe (español).")]
    [TextArea(2, 4)]
    public string bio = string.Empty;

    [Tooltip("Trasfondo breve del héroe (inglés).")]
    [TextArea(2, 4)]
    public string bioEn = string.Empty;

    [Tooltip("Trasfondo breve del héroe (japonés).")]
    [TextArea(2, 4)]
    public string bioJa = string.Empty;

    [Tooltip("Sprite del cuerpo, recortado del spritesheet LPC de este héroe.")]
    public Sprite bodySprite;

    [Tooltip("Los 36 recortes de la hoja LPC, por filas de 9; los rellena el editor.")]
    public Sprite[] walkFrames;

    [Tooltip("Rareza del héroe, de 1 a 5 estrellas.")]
    [Range(1, 5)]
    public int starRank = 1;

    [Tooltip("Vida máxima con la que arranca el héroe.")]
    public int maxHealth = 100;

    [Tooltip("Maná máximo; alimenta las habilidades activas.")]
    public int maxMP = 50;

    [Tooltip("Daño base antes de aplicar la defensa rival.")]
    public int baseAttack = 15;

    [Tooltip("Defensa que se resta al daño recibido.")]
    public int baseDefense = 5;

    [Tooltip("Velocidad de movimiento en unidades por segundo.")]
    public float moveSpeed = 2.5f;

    // Bio en el idioma activo, con fallback a español si la traducción está vacía.
    public string GetLocalizedBio()
    {
        string localized = LocalizationManager.Current switch
        {
            GameLanguage.English => bioEn,
            GameLanguage.Japanese => bioJa,
            _ => bio
        };

        return string.IsNullOrEmpty(localized) ? bio : localized;
    }
}
