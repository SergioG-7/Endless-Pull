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
        { "UI_ATTEMPTS",     new[] { "Intentos", "Attempts", "挑戦回数" } },

        { "UI_MENU",         new[] { "Menú", "Menu", "メニュー" } },
        { "UI_SECTION_MANAGEMENT", new[] { "Gestión", "Management", "運営" } },
        { "UI_SECTION_STAFF",      new[] { "Personal", "Staff", "人事" } },
        { "UI_SECTION_FACILITIES", new[] { "Instalaciones", "Facilities", "施設" } },

        { "UI_SUBCLASS",     new[] { "Subclase", "Subclass", "職種" } },
        { "UI_REPAIR",       new[] { "Reparar", "Repair", "修理" } },
        { "UI_BROKEN",       new[] { "ROTO", "BROKEN", "破損" } },

        // Cabeceras de las tarjetas de edificio en el mundo.
        { "TAG_BARRACKS",    new[] { "BARRACONES", "BARRACKS", "兵舎" } },
        { "TAG_MORALE",      new[] { "MORAL", "MORALE", "士気" } },
        { "TAG_FOOD",        new[] { "COMIDA", "FOOD", "食料" } },
        { "TAG_RITUAL",      new[] { "RITUAL", "RITUAL", "儀式" } },
        { "TAG_WORKSHOP",    new[] { "TALLER", "WORKSHOP", "工房" } },

        { "BLD_TRAINING",    new[] { "Campo de Entrenamiento", "Training Field", "訓練場" } },
        { "BLD_CANTEEN",     new[] { "Cantina", "Tavern & Canteen", "食堂" } },
        { "BLD_FARM",        new[] { "Granja", "Farm", "農場" } },
        { "BLD_ALTAR",       new[] { "Altar de Invocación", "Summoning Gate", "召喚の祭壇" } },
        { "BLD_WORKSHOP",    new[] { "Taller de Alquimia", "Alchemy Workshop", "錬金工房" } },

        // Decretos del Maestro y avisos de combate.
        { "DEC_HEAL",        new[] { "Curar Escuadra", "Heal Party", "部隊回復" } },
        { "DEC_HEAL_ALERT",  new[] { "¡CURAR!", "HEAL!", "回復！" } },
        { "DEC_FOCUS",       new[] { "Enfocar Objetivo", "Focus Fire", "集中攻撃" } },
        { "DEC_REGROUP",     new[] { "Reagruparse", "Regroup", "再集結" } },
        { "DEC_RETREAT",     new[] { "Retirada", "Retreat", "撤退" } },
        { "UI_STUNNED",      new[] { "¡ATURDIDO!", "STUNNED!", "スタン！" } },

        // Estados alterados y mantenimiento.
        { "ST_POISON",       new[] { "Veneno", "Poison", "毒" } },
        { "ST_BLEED",        new[] { "Sangrado", "Bleed", "出血" } },
        { "ST_SHIELD",       new[] { "Escudo", "Shield", "シールド" } },
        { "ST_STUN",         new[] { "Aturdimiento", "Stun", "スタン" } },
        { "ST_SLOW",         new[] { "Ralentización", "Slow", "鈍足" } },
        { "UI_REPAIR_ALL",   new[] { "Reparar Todo el Equipo", "Repair All Gear", "装備を全て修理" } },
        { "UI_MAINTENANCE",  new[] { "Mantenimiento de Equipo", "Equipment Maintenance", "装備整備" } },
        { "UI_NOTHING_BROKEN", new[] { "No hay nada que reparar", "Nothing needs repair", "修理不要" } },

        // Sinergia de escuadra.
        { "UI_SYNERGY",      new[] { "Sinergia de origen", "Origin synergy", "出身シナジー" } },
        { "UI_SYNERGY_NONE", new[] { "Sin sinergia", "No synergy", "シナジーなし" } },

        // Las 18 subclases.
        { "SUB_SHADOWBLADE",     new[] { "Espadachín Sombrío", "Shadow Blade", "影の剣士" } },
        { "SUB_IRONBLADE",       new[] { "Espadachín Férreo", "Iron Blade", "鉄の剣士" } },
        { "SUB_ZEPHYRBLADE",     new[] { "Espadachín Céfiro", "Zephyr Blade", "疾風の剣士" } },
        { "SUB_DRAGONLANCER",    new[] { "Lancero Dragón", "Dragon Lancer", "竜騎兵" } },
        { "SUB_PIKEGUARD",       new[] { "Guardia de Pica", "Pike Guard", "槍衛兵" } },
        { "SUB_STORMPIERCER",    new[] { "Perforador de Tormenta", "Storm Piercer", "嵐の穿ち手" } },
        { "SUB_LIGHTPALADIN",    new[] { "Paladín de la Luz", "Light Paladin", "光の聖騎士" } },
        { "SUB_JUGGERNAUT",      new[] { "Juggernaut", "Juggernaut", "重装兵" } },
        { "SUB_IMMORTALBASTION", new[] { "Bastión Inmortal", "Immortal Bastion", "不滅の砦" } },
        { "SUB_SNIPER",          new[] { "Francotirador", "Sniper", "狙撃手" } },
        { "SUB_VOLLEYSHOOTER",   new[] { "Tirador de Ráfaga", "Volley Shooter", "連射手" } },
        { "SUB_SHADOWHUNTER",    new[] { "Cazador de Sombras", "Shadow Hunter", "影の狩人" } },
        { "SUB_PYROMANCER",      new[] { "Piromante", "Pyromancer", "炎術師" } },
        { "SUB_CHRONOMAGE",      new[] { "Cronomago", "Chronomage", "時魔道士" } },
        { "SUB_ARCANEMAGE",      new[] { "Mago Arcano", "Arcane Mage", "秘術師" } },
        { "SUB_HIGHPRIEST",      new[] { "Sumo Sacerdote", "High Priest", "大司祭" } },
        { "SUB_PROTECTIVEORACLE",new[] { "Oráculo Protector", "Protective Oracle", "守護の巫女" } },
        { "SUB_WARCLERIC",       new[] { "Clérigo de Guerra", "War Cleric", "戦僧" } },

        // Bocadillos de la base, agrupados por lo que le pasa al héroe.
        { "SAY_HUNGRY_1",    new[] { "Las raciones escasean...", "Rations are running low...", "食料が足りない…" } },
        { "SAY_HUNGRY_2",    new[] { "¿Queda algo en la cantina?", "Anything left in the canteen?", "食堂に何か残ってるか？" } },
        { "SAY_TIRED_1",     new[] { "Necesito descansar...", "I need rest...", "休養が必要だ…" } },
        { "SAY_TIRED_2",     new[] { "No puedo con mi alma.", "I'm dead on my feet.", "もう限界だ。" } },
        { "SAY_LOWMORALE_1", new[] { "¿De verdad vamos a volver ahí?", "Are we really going back in?", "本当にまた戻るのか？" } },
        { "SAY_LOWMORALE_2", new[] { "Esto acabará mal.", "This will end badly.", "碌なことにならないぞ。" } },
        { "SAY_HURT_1",      new[] { "Estas heridas escuecen.", "These wounds sting.", "この傷が痛む。" } },
        { "SAY_HURT_2",      new[] { "Necesito vendas.", "I need bandages.", "包帯が要る。" } },
        { "SAY_HAPPY_1",     new[] { "¡Hoy es un buen día!", "Today's a good day!", "今日はいい日だ！" } },
        { "SAY_HAPPY_2",     new[] { "Que venga el siguiente piso.", "Bring on the next floor.", "次の階へ行こう。" } },
        { "SAY_GLUTTON",     new[] { "Yo peleo mejor comido.", "I fight better fed.", "腹が減っては戦はできぬ。" } },
        { "SAY_SLACKER",     new[] { "Cinco minutos más.", "Five more minutes.", "あと五分だけ。" } },
        { "SAY_FIERCE",      new[] { "Un día perfecto para entrenar", "Perfect day to train", "訓練に最適な日だ" } },
        { "SAY_IDLE_1",      new[] { "Un día tranquilo en la base.", "A quiet day at the base.", "基地は今日も平和だ。" } },
        { "SAY_IDLE_2",      new[] { "Todo en orden, Maestro.", "All in order, Master.", "異常なし、マスター。" } },

        // Nombre de la habilidad activa de cada subclase.
        { "SKILL_SHADOWBLADE",     new[] { "Corte Ponzoñoso", "Venom Cut", "毒断ち" } },
        { "SKILL_IRONBLADE",       new[] { "Guardia de Hierro", "Iron Guard", "鉄の構え" } },
        { "SKILL_ZEPHYRBLADE",     new[] { "Danza de Cortes", "Blade Dance", "斬撃乱舞" } },
        { "SKILL_DRAGONLANCER",    new[] { "Lanza del Dragón", "Dragon Lance", "竜槍" } },
        { "SKILL_PIKEGUARD",       new[] { "Empuje de Pica", "Pike Thrust", "槍衾" } },
        { "SKILL_STORMPIERCER",    new[] { "Perforar Tormenta", "Storm Pierce", "嵐穿ち" } },
        { "SKILL_LIGHTPALADIN",    new[] { "Égida de Luz", "Aegis of Light", "光の盾" } },
        { "SKILL_JUGGERNAUT",      new[] { "Embate Imparable", "Unstoppable Charge", "不動の突進" } },
        { "SKILL_IMMORTALBASTION", new[] { "Muralla Inmortal", "Immortal Wall", "不滅の壁" } },
        { "SKILL_SNIPER",          new[] { "Disparo Certero", "Perfect Shot", "必中の一矢" } },
        { "SKILL_VOLLEYSHOOTER",   new[] { "Lluvia de Flechas", "Arrow Volley", "矢の雨" } },
        { "SKILL_SHADOWHUNTER",    new[] { "Saeta Sombría", "Shadow Bolt", "影の矢" } },
        { "SKILL_PYROMANCER",      new[] { "Estallido Ígneo", "Fire Burst", "業火の炸裂" } },
        { "SKILL_CHRONOMAGE",      new[] { "Freno Temporal", "Time Warp", "時の楔" } },
        { "SKILL_ARCANEMAGE",      new[] { "Descarga Arcana", "Arcane Surge", "秘術の奔流" } },
        { "SKILL_HIGHPRIEST",      new[] { "Luz Sanadora", "Healing Light", "癒しの光" } },
        { "SKILL_PROTECTIVEORACLE",new[] { "Manto Protector", "Warding Veil", "守りの帳" } },
        { "SKILL_WARCLERIC",       new[] { "Cántico de Guerra", "War Chant", "戦の詠唱" } },

        // Qué hace la habilidad, en una línea.
        { "SKILLDESC_SHADOWBLADE",     new[] { "envenena 6s", "poisons for 6s", "6秒毒付与" } },
        { "SKILLDESC_IRONBLADE",       new[] { "escudo propio", "shields self", "自身にシールド" } },
        { "SKILLDESC_ZEPHYRBLADE",     new[] { "tres cortes y sangrado", "three cuts and bleed", "三連斬と出血" } },
        { "SKILLDESC_DRAGONLANCER",    new[] { "daño en hilera", "pierces in a line", "直線貫通" } },
        { "SKILLDESC_PIKEGUARD",       new[] { "empuja y ralentiza", "knocks back and slows", "押し戻して鈍足" } },
        { "SKILLDESC_STORMPIERCER",    new[] { "ignora armadura y aturde", "ignores armour and stuns", "防御無視とスタン" } },
        { "SKILLDESC_LIGHTPALADIN",    new[] { "provoca y escuda a la escuadra", "taunts and shields the party", "挑発と部隊シールド" } },
        { "SKILLDESC_JUGGERNAUT",      new[] { "aturde y limpia fatiga", "stuns and clears fatigue", "スタンと疲労回復" } },
        { "SKILLDESC_IMMORTALBASTION", new[] { "escudo enorme propio", "huge shield on self", "自身に大シールド" } },
        { "SKILLDESC_SNIPER",          new[] { "crítico a distancia", "critical from range", "遠距離クリティカル" } },
        { "SKILLDESC_VOLLEYSHOOTER",   new[] { "área y ralentiza", "area hit and slow", "範囲攻撃と鈍足" } },
        { "SKILLDESC_SHADOWHUNTER",    new[] { "veneno y retroceso", "poison and knockback", "毒と後退" } },
        { "SKILLDESC_PYROMANCER",      new[] { "área ígnea", "fire area", "火炎範囲" } },
        { "SKILLDESC_CHRONOMAGE",      new[] { "ralentiza a todos", "slows everyone", "全体を鈍足" } },
        { "SKILLDESC_ARCANEMAGE",      new[] { "gasta todo el maná", "spends all mana", "マナ全消費" } },
        { "SKILLDESC_HIGHPRIEST",      new[] { "cura al más herido", "heals the most hurt", "最も傷ついた者を回復" } },
        { "SKILLDESC_PROTECTIVEORACLE",new[] { "escudos a la escuadra", "shields the party", "部隊にシールド" } },
        { "SKILLDESC_WARCLERIC",       new[] { "+ATK en área", "+ATK in an area", "範囲攻撃力上昇" } },

        // Rol y modificadores que se enseñan en las cartas de elección de subclase.
        { "ROLE_SHADOWBLADE",     new[] { "Daño sostenido\nVeneno que ignora la defensa", "Sustained damage\nPoison that ignores defence", "持続ダメージ\n防御無視の毒" } },
        { "ROLE_IRONBLADE",       new[] { "Aguante alto\nEscudo propio al golpear", "High endurance\nShields itself on hit", "高耐久\n攻撃時に自身をシールド" } },
        { "ROLE_ZEPHYRBLADE",     new[] { "Tres cortes por uso\nSangrado acumulado", "Three cuts per use\nStacking bleed", "一度に三連斬\n出血の蓄積" } },
        { "ROLE_DRAGONLANCER",    new[] { "Daño en hilera\nCastiga formaciones cerradas", "Line damage\nPunishes tight formations", "直線ダメージ\n密集陣形に強い" } },
        { "ROLE_PIKEGUARD",       new[] { "Empuja y ralentiza\nRompe el avance rival", "Knocks back and slows\nBreaks the enemy advance", "押し戻して鈍足\n敵の前進を崩す" } },
        { "ROLE_STORMPIERCER",    new[] { "Ignora armadura\nAturde 1,5 s", "Ignores armour\nStuns for 1.5s", "防御無視\n1.5秒スタン" } },
        { "ROLE_LIGHTPALADIN",    new[] { "Provoca a los cercanos\nEscudo a toda la escuadra", "Taunts nearby foes\nShields the whole party", "周囲を挑発\n部隊全体にシールド" } },
        { "ROLE_JUGGERNAUT",      new[] { "Aturde y se quita la fatiga", "Stuns and sheds fatigue", "スタンし疲労を払う" } },
        { "ROLE_IMMORTALBASTION", new[] { "Escudo enorme\nAguanta el golpe del jefe", "Huge shield\nSoaks the boss hit", "巨大シールド\nボスの一撃を受け切る" } },
        { "ROLE_SNIPER",          new[] { "Golpe crítico a distancia\nEl mayor daño de un solo tiro", "Critical hit from range\nHighest single-shot damage", "遠距離クリティカル\n単発最大火力" } },
        { "ROLE_VOLLEYSHOOTER",   new[] { "Área a distancia\nRalentiza a los alcanzados", "Ranged area\nSlows those hit", "遠距離範囲\n命中者を鈍足" } },
        { "ROLE_SHADOWHUNTER",    new[] { "Veneno y retroceso\nMantiene la distancia", "Poison and knockback\nKeeps its distance", "毒と後退\n距離を保つ" } },
        { "ROLE_PYROMANCER",      new[] { "Área ígnea\nIgnora armadura", "Fire area\nIgnores armour", "火炎範囲\n防御無視" } },
        { "ROLE_CHRONOMAGE",      new[] { "Ralentiza a todo el campo", "Slows the whole field", "戦場全体を鈍足" } },
        { "ROLE_ARCANEMAGE",      new[] { "Gasta todo el maná\nDaño proporcional al restante", "Spends all mana\nDamage scales with what's left", "マナ全消費\n残量に応じた威力" } },
        { "ROLE_HIGHPRIEST",      new[] { "Cura al aliado más herido", "Heals the most wounded ally", "最も傷ついた味方を回復" } },
        { "ROLE_PROTECTIVEORACLE",new[] { "Escudos de absorción en área", "Absorption shields in an area", "範囲吸収シールド" } },
        { "ROLE_WARCLERIC",       new[] { "Sube la moral de la escuadra", "Raises party morale", "部隊の士気を上げる" } },

        // Altar de invocación.
        { "UI_SUMMON",       new[] { "Invocar", "Summon", "召喚" } },
        { "UI_SUMMON_ALTAR", new[] { "Altar de Invocación", "Summoning Gate", "召喚の祭壇" } },
        { "UI_SUMMON_X1",    new[] { "Invocación Simple", "Single Summon", "単発召喚" } },
        { "UI_SUMMON_X10",   new[] { "Invocación Múltiple", "Ten Summon", "十連召喚" } },
        { "UI_DISCOUNT",     new[] { "10 % de descuento", "10% off", "10%割引" } },
        { "UI_REVEAL",       new[] { "Tocar para revelar", "Tap to reveal", "タップで公開" } },
        { "UI_SUMMON_AGAIN", new[] { "Invocar otra vez", "Summon again", "もう一度召喚" } },
        { "UI_NO_GEMS",      new[] { "Gemas insuficientes", "Not enough gems", "ジェムが足りない" } },
        { "UI_CATALOG_FULL", new[] { "Ya tienes a todos los héroes", "You own every hero", "全ヒーロー獲得済み" } },

        // Tablón de misiones.
        { "UI_QUESTS",       new[] { "Tablón de Misiones", "Quest Board", "依頼掲示板" } },
        { "UI_CLAIM",        new[] { "Reclamar", "Claim", "受け取る" } },
        { "UI_CLAIMED",      new[] { "Reclamada", "Claimed", "受取済" } },
        { "UI_REWARD",       new[] { "Recompensa", "Reward", "報酬" } },
        { "Q_FLOOR",         new[] { "Supera el piso {0} de la Torre", "Clear floor {0} of the Tower", "塔の{0}階を突破" } },
        { "Q_LEVEL",         new[] { "Alcanza el nivel {0} con un héroe", "Reach level {0} with a hero", "ヒーローをレベル{0}に" } },
        { "Q_ASCEND",        new[] { "Asciende un héroe a {0}★", "Ascend a hero to {0}★", "ヒーローを{0}★に覚醒" } },
        { "Q_WORKERS",       new[] { "Asigna {0} trabajadores", "Assign {0} workers", "作業員を{0}人配置" } },
        { "Q_REPAIR",        new[] { "Repara {0} piezas de equipo", "Repair {0} pieces of gear", "装備を{0}点修理" } },
        { "Q_RESTED",        new[] { "Cura la fatiga de {0} héroe(s)", "Cure fatigue on {0} hero(es)", "ヒーロー{0}人の疲労を回復" } }
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
