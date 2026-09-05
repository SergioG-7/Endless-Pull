using UnityEngine;

// Fragmento de memoria que se desbloquea al alcanzar una rareza concreta.
[System.Serializable]
public class HeroMemory
{
    [Tooltip("Estrellas que hacen falta para recordar esto.")]
    [Range(1, 7)]
    public int starRank = 2;

    [Tooltip("Texto del recuerdo (español).")]
    [TextArea(2, 4)]
    public string text = string.Empty;

    [Tooltip("Texto del recuerdo (inglés).")]
    [TextArea(2, 4)]
    public string textEn = string.Empty;

    [Tooltip("Texto del recuerdo (japonés).")]
    [TextArea(2, 4)]
    public string textJa = string.Empty;

    // Mismo criterio que la bio: si falta la traducción, se cae al español.
    public string GetLocalized()
    {
        string localizado = LocalizationManager.Current switch
        {
            GameLanguage.English => textEn,
            GameLanguage.Japanese => textJa,
            _ => text
        };

        return string.IsNullOrEmpty(localizado) ? text : localizado;
    }

    public bool IsValid => !string.IsNullOrEmpty(text) || !string.IsNullOrEmpty(textEn)
                           || !string.IsNullOrEmpty(textJa);
}

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

    [Tooltip("Recuerdos que va recuperando al ascender; vacío = este héroe aún no tiene escritos.")]
    public HeroMemory[] memories = new HeroMemory[0];

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
