using System.Collections.Generic;
using UnityEngine;

// Idiomas soportados; el orden es el del selector del menú y el del diccionario.
public enum GameLanguage
{
    Spanish,
    English,
    Japanese
}

// Diccionario de claves de texto y idioma activo. Estático: el menú lo necesita antes de que arranque nada.
public static class LocalizationManager
{
    private const string LanguageKey = "EndlessPull.Language";

    private static GameLanguage current = GameLanguage.Spanish;
    private static bool loaded;
    private static bool warnedMissingGlyphs;

    // Se dispara al cambiar de idioma; los rótulos se resuscriben solos.
    public static event System.Action LanguageChanged;

    public static GameLanguage Current
    {
        get { EnsureLoaded(); return current; }
    }

    // Cada entrada lleva las tres formas en el orden del enum: ES, EN, JA.
    private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
    {
        { "UI_TITLE",        new[] { "Endless Pull", "Endless Pull", "Endless Pull" } },
        { "UI_START",        new[] { "Nueva Partida", "New Game", "ニューゲーム" } },
        { "UI_CONTINUE",     new[] { "Continuar", "Continue", "つづきから" } },
        { "UI_OPTIONS",      new[] { "Opciones", "Options", "設定" } },
        { "UI_LANGUAGE",     new[] { "Idioma", "Language", "言語" } },
        { "UI_MASTER_VOLUME",new[] { "Volumen general", "Master Volume", "音量" } },
        { "UI_BACK",         new[] { "Volver", "Back", "もどる" } },
        { "UI_CLOSE",        new[] { "Cerrar", "Close", "とじる" } },
        { "UI_NO_SAVE",      new[] { "Sin partida guardada", "No saved game", "セーブデータなし" } },

        { "UI_ROSTER",       new[] { "Roster", "Roster", "編成" } },
        { "UI_TOWER",        new[] { "Torre", "Tower", "塔" } },
        { "UI_SHOP",         new[] { "Tienda", "Shop", "店" } },
        { "UI_CRAFT",        new[] { "Taller", "Workshop", "工房" } },
        { "UI_EXPEDITIONS",  new[] { "Expediciones", "Expeditions", "遠征" } },

        { "UI_ALL",          new[] { "Todos", "All", "すべて" } },
        { "UI_PARTY",        new[] { "Escuadra", "Party", "部隊" } },
        { "UI_IN_PARTY",     new[] { "EN ESCUADRA", "IN PARTY", "部隊中" } },
        { "UI_SORT_LEVEL",   new[] { "Nivel", "Level", "レベル" } },
        { "UI_SORT_RARITY",  new[] { "Rareza", "Rarity", "レア度" } },
        { "UI_LOCK",         new[] { "BLOQ", "LOCK", "ロック" } },
        { "UI_UNLOCK",       new[] { "LIBRE", "FREE", "かいじょ" } },
        { "UI_SYNTH",        new[] { "Sintetizar", "Synthesize", "合成" } },
        { "UI_SYNTH_TARGET", new[] { "OBJETIVO", "TARGET", "対象" } },
        { "UI_ASCEND",       new[] { "Ascender", "Ascend", "覚醒" } },
        { "UI_EQUIP",        new[] { "Equipar", "Equip", "装備" } },
        { "UI_UNEQUIP",      new[] { "Quitar", "Unequip", "はずす" } },

        { "UI_READY",        new[] { "¡PREPARAOS!", "READY...", "構えろ！" } },
        { "UI_ENGAGE",       new[] { "¡AL ATAQUE!", "ENGAGE!", "突撃！" } },
        { "UI_RETREAT",      new[] { "¡RETIRADA!", "RETREAT!", "撤退！" } },

        { "UI_FLOOR",        new[] { "Piso", "Floor", "階" } },
        { "UI_FIRST_CLEAR",  new[] { "Primera vez", "First clear", "初回" } },
        { "UI_REPEAT",       new[] { "Repetición", "Repeat", "周回" } },
        { "UI_LOCKED_FLOOR", new[] { "Bloqueado", "Locked", "未開放" } },

        { "UI_FOREST",       new[] { "Bosque", "Forest", "森" } },
        { "UI_MINE",         new[] { "Mina", "Mine", "鉱山" } },
        { "UI_HUNT",         new[] { "Tierras de Caza", "Hunting Grounds", "狩場" } },
        { "UI_WOOD",         new[] { "Madera", "Wood", "木材" } },
        { "UI_IRON",         new[] { "Hierro", "Iron", "鉄" } },
        { "UI_FOOD",         new[] { "Comida", "Food", "食料" } },
        { "UI_GEMS",         new[] { "Gemas", "Gems", "ジェム" } },
        { "UI_ATTEMPTS",     new[] { "Intentos", "Attempts", "挑戦回数" } }
    };

    // Nombre del idioma en su propio idioma: el selector se entiende sin saber el actual.
    public static string NameOf(GameLanguage language)
    {
        switch (language)
        {
            case GameLanguage.English: return "English";
            case GameLanguage.Japanese: return "日本語";
        }
        return "Español";
    }

    public static string Get(string key)
    {
        EnsureLoaded();

        if (!Table.TryGetValue(key, out var forms)) return key;

        int index = (int)current;
        if (index >= 0 && index < forms.Length && !string.IsNullOrEmpty(forms[index])) return forms[index];

        return forms[0];
    }

    public static void SetLanguage(GameLanguage language)
    {
        EnsureLoaded();
        if (current == language) return;

        current = language;
        PlayerPrefs.SetInt(LanguageKey, (int)language);
        PlayerPrefs.Save();

        WarnIfFontCannotDrawIt();
        LanguageChanged?.Invoke();
    }

    // Rota entre los tres idiomas; es lo que hace el botón del menú.
    public static GameLanguage NextLanguage()
    {
        var next = (GameLanguage)(((int)Current + 1) % 3);
        SetLanguage(next);
        return next;
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;

        loaded = true;
        current = (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, 2);
    }

    // Sin kana ni kanji el japonés sale como cuadrados; se mira también la lista de fallbacks.
    private static void WarnIfFontCannotDrawIt()
    {
        if (current != GameLanguage.Japanese || warnedMissingGlyphs) return;
        if (CanDrawJapanese()) return;

        warnedMissingGlyphs = true;
        Debug.LogWarning("[Idioma] Ni la fuente por defecto ni sus fallbacks tienen glifos japoneses: " +
                         "hay que añadir una fuente CJK a los Fallback Font Assets de TMP Settings.");
    }

    // Un fallback dinámico no trae el glifo hasta que se pide, así que se le pregunta si puede añadirlo.
    public static bool CanDrawJapanese()
    {
        if (Cubre(TMPro.TMP_Settings.defaultFontAsset)) return true;

        var fallbacks = TMPro.TMP_Settings.fallbackFontAssets;
        if (fallbacks == null) return false;

        foreach (var fuente in fallbacks)
            if (Cubre(fuente)) return true;

        return false;
    }

    private static bool Cubre(TMPro.TMP_FontAsset fuente)
    {
        if (fuente == null) return false;
        if (fuente.HasCharacter('日')) return true;

        // Los atlas dinámicos generan el glifo bajo demanda: se comprueba pidiéndoselo.
        return fuente.atlasPopulationMode == TMPro.AtlasPopulationMode.Dynamic
               && fuente.TryAddCharacters("日本語");
    }
}
