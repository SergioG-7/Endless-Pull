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
        { "UI_BACK",         new[] { "Volver", "Back", "もどる" } },

        // Menú de pausa in-game (InGameMenuUI) y opciones del menú principal (MainMenuUI):
        // los 3 canales de audio unificados de AudioManager (Fase 46).
        { "UI_IN_GAME_MENU", new[] { "Menú", "Menu", "メニュー" } },
        { "UI_RESUME",       new[] { "Reanudar", "Resume", "再開" } },
        { "UI_PAUSE",        new[] { "Pausar", "Pause", "一時停止" } },
        { "UI_BGM_VOLUME",   new[] { "Música", "Music", "BGM" } },
        { "UI_UI_VOLUME",    new[] { "Interfaz", "UI", "UI音" } },
        { "UI_COMBAT_VOLUME",new[] { "Combate", "Combat", "戦闘音" } },
        { "UI_QUIT_TO_MENU", new[] { "Guardar y salir", "Save and Quit", "セーブして終了" } },
        { "UI_CLOSE",        new[] { "Cerrar", "Close", "閉じる" } },
        { "UI_NO_SAVE",      new[] { "Sin partida guardada", "No saved game", "セーブデータなし" } },

        { "UI_VIEW_HEROES",  new[] { "Ver Héroes", "View Heroes", "英雄一覧" } },
        { "UI_HEAL_ALL",     new[] { "Curar Todos", "Heal All", "全員回復" } },
        { "UI_ALL_HEALED",   new[] { "Todos los héroes ya están al máximo de salud",
                                       "All heroes are already at full health", "全員すでに満タンです" } },
        { "UI_TOWER",        new[] { "Torre", "Tower", "塔" } },
        { "UI_SHOP",         new[] { "Almacén", "Storage", "倉庫" } },
        { "UI_CRAFT",        new[] { "Taller", "Workshop", "工房" } },
        { "UI_EXPEDITIONS",  new[] { "Expediciones", "Expeditions", "遠征" } },

        // Kickers de categoría sobre los edificios de la base (LocalizedText en Base.unity).
        { "TAG_BARRACKS",    new[] { "BARRACONES", "BARRACKS", "兵舎" } },
        { "TAG_MORALE",      new[] { "MORAL", "MORALE", "士気" } },
        { "TAG_FOOD",        new[] { "COMIDA", "FOOD", "食料" } },
        { "TAG_RITUAL",      new[] { "RITUAL", "RITUAL", "儀式" } },
        { "TAG_WORKSHOP",    new[] { "TALLER", "WORKSHOP", "工房" } },

        { "UI_ALL",          new[] { "Todos", "All", "すべて" } },
        { "UI_PARTY",        new[] { "Escuadra", "Party", "部隊" } },
        { "UI_IN_PARTY",     new[] { "EN ESCUADRA", "IN PARTY", "部隊中" } },
        { "UI_SORT_LEVEL",   new[] { "Nivel", "Level", "レベル" } },
        { "UI_SORT_RARITY",  new[] { "Rareza", "Rarity", "レア度" } },
        { "UI_LOCK",         new[] { "BLOQ", "LOCK", "ロック" } },
        { "UI_UNLOCK",       new[] { "LIBRE", "FREE", "かいじょ" } },
        { "UI_SYNTH",        new[] { "Sintetizar", "Synthesize", "合成" } },
        { "UI_SYNTH_CONFIRM_TITLE", new[] { "¿Sacrificar héroe?", "Sacrifice hero?", "英雄を生贄にする？" } },
        { "UI_SYNTH_CONFIRM_MSG",   new[] { "{0} se sacrificará para dar {1} EXP a {2}. Esta acción no se puede deshacer.",
                                              "{0} will be sacrificed to give {1} EXP to {2}. This cannot be undone.",
                                              "{0}を生贄にして{2}に経験値{1}を与えます。元に戻せません。" } },
        { "UI_CANCEL",       new[] { "Cancelar", "Cancel", "キャンセル" } },
        { "UI_ASCEND",       new[] { "Ascender", "Ascend", "覚醒" } },
        { "UI_EQUIP",        new[] { "Equipar", "Equip", "装備" } },
        { "UI_UNEQUIP",      new[] { "Quitar", "Unequip", "はずす" } },

        // Santuario de Ascensión y Síntesis (SanctuaryUI).
        { "UI_SANCTUARY",         new[] { "Santuario", "Sanctuary", "聖域" } },
        { "UI_ASCENSION_TAB",     new[] { "Ascensión", "Ascension", "覚醒" } },
        { "UI_SYNTHESIS_TAB",     new[] { "Síntesis", "Synthesis", "合成" } },
        { "UI_RECEIVER",          new[] { "Receptor", "Receiver", "受け手" } },
        { "UI_SACRIFICE",         new[] { "Sacrificio", "Sacrifice", "生贄" } },
        { "UI_SELECT_HERO",       new[] { "Selecciona un héroe de la lista", "Select a hero from the list", "リストから英雄を選んでください" } },
        { "UI_STONE_MENOR",       new[] { "Piedra Menor", "Minor Stone", "小さな覚醒石" } },
        { "UI_STONE_MEDIA",       new[] { "Piedra Media", "Median Stone", "中位の覚醒石" } },
        { "UI_STONE_MAYOR",       new[] { "Piedra Mayor", "Greater Stone", "大きな覚醒石" } },
        { "UI_STONE_LEGENDARIA",  new[] { "Piedra Legendaria", "Legendary Stone", "伝説の覚醒石" } },
        { "UI_STONE_TRASCENDENTE", new[] { "Piedra Trascendente", "Transcendent Stone", "超越の覚醒石" } },
        { "UI_STONE_CELESTIAL",   new[] { "Piedra Celestial", "Celestial Stone", "天界の覚醒石" } },
        { "UI_ASCEND_PREVIEW",    new[] { "{0}★ → {1}★  (×{2:0.00} estadísticas)",
                                            "{0}★ → {1}★  (×{2:0.00} stats)",
                                            "{0}★ → {1}★（ステータス×{2:0.00}）" } },
        { "UI_ASCEND_COST",       new[] { "{0} gemas + 1 {1}", "{0} gems + 1 {1}", "ジェム{0} + {1}×1" } },
        { "UI_ASCEND_MAX_RARITY", new[] { "Rareza máxima", "Max rarity", "最大レア度" } },
        { "UI_ASCEND_NEED_LEVEL", new[] { "Necesita llegar a Nv. {0}", "Needs to reach Lv. {0}", "Lv.{0}が必要" } },
        { "UI_ASCEND_NEED_GEMS",  new[] { "Gemas: {0}/{1}", "Gems: {0}/{1}", "ジェム：{0}/{1}" } },
        { "UI_ASCEND_NEED_STONE", new[] { "{0}: {1}/1", "{0}: {1}/1", "{0}：{1}/1" } },

        { "UI_READY",        new[] { "¡PREPARAOS!", "READY...", "構えろ！" } },
        { "UI_ENGAGE",       new[] { "¡AL ATAQUE!", "ENGAGE!", "突撃！" } },
        { "UI_RETREAT",      new[] { "¡RETIRADA!", "RETREAT!", "撤退！" } },

        { "UI_FLOOR",        new[] { "Piso", "Floor", "階" } },
        { "UI_TOWN",         new[] { "Townia", "Townia", "タウニア" } },
        { "UI_MULTI_SQUAD",  new[] { "{0} escuadrones", "{0} squads", "{0}部隊" } },
        { "UI_MEMORIES",     new[] { "Recuerdos", "Memories", "記憶" } },

        // Intervención del Maestro: la barra la cargan los héroes de 6★ y 7★ desplegados.
        { "BLD_LODGING",     new[] { "Dormitorios", "Lodging", "宿舎" } },
        { "BLD_GALLERY",     new[] { "Galería Memorial", "Memorial Gallery", "追悼の間" } },
        { "UI_MEMORIAL_SYNTH",  new[] { "Sacrificado en síntesis", "Sacrificed in synthesis", "合成で捧げられた" } },
        { "UI_MEMORIAL_FALLEN", new[] { "Caído en el Piso {0}", "Fallen on Floor {0}", "{0}階で斃れた" } },
        { "UI_MEMORIAL_EMPTY",  new[] { "Aún no se ha perdido a nadie.", "No one has been lost yet.",
                                        "まだ誰も失っていない。" } },
        { "UI_PROD_LODGING", new[] { "-{0} de fatiga cada {1}s", "-{0} fatigue every {1}s",
                                     "{1}秒ごとに疲労-{0}" } },
        { "UI_INTERVENTION", new[] { "Intervención", "Intervention", "介入" } },
        { "UI_INTERVENTION_NEED", new[] { "Necesitas héroes de 6★ o 7★ desplegados",
                                          "Requires 6★ or 7★ heroes deployed",
                                          "6★か7★のヒーロー展開が必要" } },
        { "UI_ACT_DODGE",    new[] { "Esquiva", "Dodge", "回避" } },
        { "UI_ACT_BREAK",    new[] { "Ruptura", "Shatter", "破砕" } },
        { "UI_ACT_PULSE",    new[] { "Pulso", "Pulse", "鼓動" } },
        { "UI_MEMORY_LOCKED", new[] { "Recuerdo sellado — asciende a {0}★",
                                      "Sealed memory — ascend to {0}★",
                                      "封じられた記憶 — {0}★に昇華せよ" } },
        { "UI_MEMORY_NONE",  new[] { "No recuerda nada de su vida anterior.",
                                     "Remembers nothing of their earlier life.",
                                     "前世の記憶は何も残っていない。" } },
        { "UI_MULTI_SQUAD_HINT", new[] { "Piso {0}: hacen falta {1} escuadrones. Prepara los presets.",
                                         "Floor {0}: needs {1} squads. Get your presets ready.",
                                         "{0}階：{1}部隊が必要。プリセットを準備せよ。" } },
        { "UI_TOWN_LEVEL",   new[] { "Nv. {0}", "Lv. {0}", "Lv.{0}" } },
        { "UI_TOWN_NEXT",    new[] { "Townia sube en el Piso {0}", "Townia grows at Floor {0}", "タウニアは{0}階で成長" } },
        { "UI_TOWN_GREW",    new[] { "¡Townia crece a Nv. {0}!", "Townia grows to Lv. {0}!", "タウニアがLv.{0}に成長！" } },
        { "UI_FIRST_CLEAR",  new[] { "Primera vez", "First clear", "初回" } },
        { "UI_REPEAT",       new[] { "Repetición", "Repeat", "周回" } },
        // Cuadrantes direccionales bloqueados de la base (QuadrantController).
        { "UI_QUADRANT_FLOOR_LABEL", new[] { "Piso {0}", "Floor {0}", "{0}階" } },
        

        // Nombres de piezas de equipo (Fase 39): claves opcionales via EquipmentData.nameKey.
        { "EQUIP_WOODEN_SWORD",  new[] { "Espada de Madera", "Wooden Sword", "木の剣" } },
        { "EQUIP_SHORT_BOW", new[] { "Arco Corto", "Short Bow", "短弓" } },
        { "EQUIP_APPRENTICE_STAFF", new[] { "Bastón de Aprendiz", "Apprentice Staff", "見習いの杖" } },
        { "EQUIP_WOODEN_MACE", new[] { "Maza de Madera", "Wooden Mace", "木のメイス" } },
        { "EQUIP_TRAINING_SPEAR", new[] { "Lanza de Entreno", "Training Spear", "訓練用の槍" } },
        { "EQUIP_IRON_SWORD",    new[] { "Espada de Hierro", "Iron Sword", "鉄の剣" } },
        { "EQUIP_OAK_SHIELD",    new[] { "Escudo de Roble", "Oak Shield", "オークの盾" } },
        { "EQUIP_IRON_SHIELD",   new[] { "Escudo de Hierro", "Iron Shield", "鉄の盾" } },
        { "EQUIP_HUNTER_SPEAR",  new[] { "Lanza de Cazador", "Hunter's Spear", "狩人の槍" } },
        { "EQUIP_LEATHER_ARMOR", new[] { "Armadura de Cuero", "Leather Armor", "革の鎧" } },
        { "EQUIP_IRON_ARMOR",    new[] { "Armadura de Hierro", "Iron Armor", "鉄の鎧" } },
        

        // Filtro por tipo del modal de equipamiento (Fase 39).
        { "UI_FILTER_ALL",         new[] { "Todos", "All", "すべて" } },
        { "UI_FILTER_WEAPONS",     new[] { "Armas", "Weapons", "武器" } },
        { "UI_FILTER_SHIELDS",     new[] { "Escudos", "Shields", "盾" } },
        { "UI_FILTER_ARMORS",      new[] { "Armaduras", "Armor", "防具" } },
        { "UI_FILTER_ACCESSORIES", new[] { "Accesorios", "Accessories", "装飾品" } },
        { "UI_FILTER_CONSUMABLES", new[] { "Consumibles", "Consumables", "消耗品" } },
{ "EQUIP_HUNTER_RING",   new[] { "Anillo del Cazador", "Hunter's Ring", "狩人の指輪" } },

        // Equipo forjable de gama 2 a 6; una gama nueva cada 10 pisos de Torre superados.
        { "EQUIP_TRAVELER_BAND",  new[] { "Brazalete del Viajero", "Traveler's Band", "旅人の腕輪" } },
        { "EQUIP_STEEL_SWORD",    new[] { "Espada de Acero", "Steel Sword", "鋼の剣" } },
        { "EQUIP_STEEL_SHIELD",   new[] { "Escudo de Acero", "Steel Shield", "鋼の盾" } },
        { "EQUIP_STEEL_ARMOR",    new[] { "Armadura de Acero", "Steel Armor", "鋼の鎧" } },
        { "EQUIP_STEEL_TALISMAN", new[] { "Talismán de Acero", "Steel Talisman", "鋼の護符" } },
        { "EQUIP_SILVER_SWORD",   new[] { "Espada de Plata", "Silver Sword", "銀の剣" } },
        { "EQUIP_SILVER_SHIELD",  new[] { "Escudo de Plata", "Silver Shield", "銀の盾" } },
        { "EQUIP_SILVER_ARMOR",   new[] { "Armadura de Plata", "Silver Armor", "銀の鎧" } },
        { "EQUIP_SILVER_AMULET",  new[] { "Amuleto de Plata", "Silver Amulet", "銀の護符" } },
        { "EQUIP_MITHRIL_SWORD",  new[] { "Espada de Mitrilo", "Mithril Sword", "ミスリルの剣" } },
        { "EQUIP_MITHRIL_SHIELD", new[] { "Escudo de Mitrilo", "Mithril Shield", "ミスリルの盾" } },
        { "EQUIP_MITHRIL_ARMOR",  new[] { "Armadura de Mitrilo", "Mithril Armor", "ミスリルの鎧" } },
        { "EQUIP_MITHRIL_SIGIL",  new[] { "Sello de Mitrilo", "Mithril Sigil", "ミスリルの印" } },
        { "EQUIP_ADAMANT_SWORD",  new[] { "Espada de Adamantita", "Adamant Sword", "アダマンタイトの剣" } },
        { "EQUIP_ADAMANT_SHIELD", new[] { "Escudo de Adamantita", "Adamant Shield", "アダマンタイトの盾" } },
        { "EQUIP_ADAMANT_ARMOR",  new[] { "Armadura de Adamantita", "Adamant Armor", "アダマンタイトの鎧" } },
        { "EQUIP_ADAMANT_CROWN",  new[] { "Diadema de Adamantita", "Adamant Crown", "アダマンタイトの冠" } },

        // Conjunto del Guardián: solo cae de jefes y escoltas duras, nunca se puede forjar.
        { "EQUIP_WARDEN_BLADE",   new[] { "Filo del Guardián", "Warden's Blade", "守護者の刃" } },
        { "EQUIP_WARDEN_BULWARK", new[] { "Baluarte del Guardián", "Warden's Bulwark", "守護者の砦盾" } },
        { "EQUIP_WARDEN_PLATE",   new[] { "Coraza del Guardián", "Warden's Plate", "守護者の胸当て" } },
        { "EQUIP_WARDEN_SEAL",    new[] { "Sello del Guardián", "Warden's Seal", "守護者の封印" } },
{ "UI_QUADRANT_LOCKED_TAP",  new[] { "Desbloquea en Piso {0}", "Unlocks at Floor {0}", "フロア{0}で解放" } },

        // Estados de combate/expedición mostrados en el HUD (WaveManager.Report).
        { "UI_STATUS_ASSIGN_HEROES", new[] { "Asigna héroes a la escuadra", "Assign heroes to the party", "部隊に英雄を配置してください" } },
        { "UI_STATUS_FLOOR_START",   new[] { "Piso {0}: {1} enemigos{2} (x{3:0.00}){4}  Escuadra {5}/{6}",
                                               "Floor {0}: {1} enemies{2} (x{3:0.00}){4}  Party {5}/{6}",
                                               "{0}階：敵{1}体{2}（x{3:0.00}）{4}　部隊{5}/{6}" } },
        { "UI_BOSS_TAG",             new[] { " + JEFE", " + BOSS", " + ボス" } },
        { "UI_BOSS_ARRIVAL",         new[] { "¡JEFE!", "BOSS!", "ボス出現！" } },
        { "UI_STATUS_WON",   new[] { "Piso {0} superado ({1})  +{2} gemas, +{3} madera, +{4} hierro{5}",
                                       "Floor {0} cleared ({1})  +{2} gems, +{3} wood, +{4} iron{5}",
                                       "{0}階クリア（{1}）　+{2}ジェム、+{3}木材、+{4}鉄{5}" } },
        { "UI_STATUS_LOST",  new[] { "Derrota en el piso {0}", "Defeated on floor {0}",
                                       "フロア{0}で敗北" } },
        { "UI_STATUS_RETREAT", new[] { "Retirada del piso {0}: {1} héroe(s) a salvo, sin recompensa",
                                         "Retreat from floor {0}: {1} hero(es) safe, no reward",
                                         "フロア{0}から撤退: {1}名の英雄が無事帰還、報酬なし" } },
        { "UI_ESCORT_TAG",   new[] { " + ESCOLTA", " + ESCORT", " + 護衛" } },
        { "UI_STATUS_ESCORT_LOST", new[] { "{0} ha caído: misión de escolta fracasada en el piso {1}",
                                             "{0} has fallen: escort mission failed on floor {1}",
                                             "{0}が倒れた：{1}階の護衛任務は失敗" } },

        // Banner de misión al empezar el piso (MissionBannerUI): título y objetivo por FloorMissionType.
        { "MISSION_TITLE_SUBJUGATION", new[] { "Subyugación", "Subjugation", "討伐" } },
        { "MISSION_TITLE_SURVIVAL",    new[] { "Supervivencia", "Survival", "生存" } },
        { "MISSION_TITLE_ESCORT",      new[] { "Escolta", "Escort", "護衛" } },
        { "MISSION_TITLE_BOSSHUNT",    new[] { "Caza de Jefe", "Boss Hunt", "ボス討伐" } },
        { "MISSION_OBJ_SUBJUGATION",   new[] { "Elimina a toda la oleada enemiga.",
                                                 "Wipe out the entire enemy wave.",
                                                 "敵の大群を殲滅せよ。" } },
        { "MISSION_OBJ_SURVIVAL",      new[] { "Aguanta con vida durante {0} segundos.",
                                                 "Survive for {0} seconds.",
                                                 "{0}秒間生き延びろ。" } },
        { "MISSION_OBJ_ESCORT",        new[] { "Protege a Friacis hasta despejar el piso.",
                                                 "Protect Friacis until the floor is cleared.",
                                                 "フロアを制圧するまでフリアシスを守れ。" } },
        { "MISSION_OBJ_BOSSHUNT",      new[] { "Derrota al jefe del piso.",
                                                 "Defeat the floor boss.",
                                                 "フロアのボスを倒せ。" } },
        { "MISSION_HIDDEN_YES", new[] { "Reto oculto: Sí", "Hidden challenge: Yes", "隠しミッション：あり" } },
        { "MISSION_HIDDEN_NO",  new[] { "Reto oculto: No", "Hidden challenge: No", "隠しミッション：なし" } },

        // Sub-Misiones Ocultas: toast de recompensa al completar un reto oculto.
        { "CHALLENGE_SPEED_CLEAR_WON",     new[] { "¡Reto oculto superado! Despeje relámpago: +1 Piedra de Ascensión y +{0} gemas.",
                                                     "Hidden challenge cleared! Lightning clear: +1 Ascension Stone and +{0} gems.",
                                                     "隠しミッション達成！電光石火の制圧：覚醒石+1、ジェム+{0}。" } },
        { "CHALLENGE_ASSASSINATE_WON",     new[] { "¡Reto oculto superado! Cazador de exploradores: +1 Piedra de Ascensión y +{0} gemas.",
                                                     "Hidden challenge cleared! Scout hunter: +1 Ascension Stone and +{0} gems.",
                                                     "隠しミッション達成！斥候狩り：覚醒石+1、ジェム+{0}。" } },
        { "CHALLENGE_NO_HERO_DOWN_WON",    new[] { "¡Reto oculto superado! Nadie cayó: +1 Piedra de Ascensión y +{0} gemas.",
                                                     "Hidden challenge cleared! No one fell: +1 Ascension Stone and +{0} gems.",
                                                     "隠しミッション達成！誰も倒れず：覚醒石+1、ジェム+{0}。" } },
        { "CHALLENGE_ESCORT_UNHARMED_WON", new[] { "¡Reto oculto superado! Escolta impecable: +1 Piedra de Ascensión y +{0} gemas.",
                                                     "Hidden challenge cleared! Flawless escort: +1 Ascension Stone and +{0} gems.",
                                                     "隠しミッション達成！完璧な護衛：覚醒石+1、ジェム+{0}。" } },

        // Isel, hada guía de la base: pistas al tocarla y aviso de mediación de disputas.
        { "FAIRY_HINT_HUNGRY",      new[] { "La despensa anda escasa; tus héroes lo notarán si no comen pronto.",
                                              "The pantry's running low; your heroes will feel it if they don't eat soon.",
                                              "食料庫が心細いわ。早く食べさせないと英雄たちが困るわよ。" } },
        { "FAIRY_HINT_STONE_READY", new[] { "Tienes una Piedra de Ascensión lista: pásate por el Taller.",
                                              "You have an Ascension Stone ready: stop by the Workshop.",
                                              "覚醒石の準備ができてるわ。工房に寄ってみて。" } },
        { "FAIRY_HINT_FLOOR_ALERT", new[] { "El Piso {0} que te espera es de {1}: prepara bien la escuadra.",
                                              "Floor {0} ahead is a {1} floor: prepare your squad well.",
                                              "次のフロア{0}は{1}よ。編成をしっかり整えてね。" } },
        { "FAIRY_HINT_IDLE_1",      new[] { "¿Necesitas algo, Comandante? Estoy para ayudar.",
                                              "Need anything, Commander? I'm here to help.",
                                              "何か用かしら、司令官？いつでも力になるわ。" } },
        { "FAIRY_HINT_IDLE_2",      new[] { "La base va bien por ahora. Sigamos así.",
                                              "The base is doing fine for now. Let's keep it up.",
                                              "拠点は今のところ順調ね。この調子で行きましょう。" } },
        { "FAIRY_MEDIATION_TOAST", new[] { "Isel calma una disputa entre héroes desanimados.",
                                             "Isel calms a dispute between disheartened heroes.",
                                             "イセルが意気消沈した英雄たちの諍いを鎮めた。" } },

        // Rango de Pericia de Arma (F-S), overlay sobre el nivel numérico (WeaponMastery).
        { "MASTERY_RANK_F", new[] { "F", "F", "F" } },
        { "MASTERY_RANK_E", new[] { "E", "E", "E" } },
        { "MASTERY_RANK_D", new[] { "D", "D", "D" } },
        { "MASTERY_RANK_C", new[] { "C", "C", "C" } },
        { "MASTERY_RANK_B", new[] { "B", "B", "B" } },
        { "MASTERY_RANK_A", new[] { "A", "A", "A" } },
        { "MASTERY_RANK_S", new[] { "S", "S", "S" } },

        // Despertar de Habilidades: pasiva nueva al sobrevivir crítico o matar a un jefe.
        { "UI_SKILL_AWAKENING", new[] { "¡{0} despierta una nueva pasiva: {1}!",
                                          "{0} awakens a new passive: {1}!",
                                          "{0}が新しいパッシブ「{1}」に目覚めた！" } },
        { "UI_ABILITY_AWAKENING", new[] { "¡{0} aprende una habilidad nueva: {1}!",
                                          "{0} learns a new ability: {1}!",
                                          "{0}が新しいスキル「{1}」を覚えた！" } },

        // Acelerador de combate y retirada automática (CombatHUD).
        { "UI_AUTO_RETREAT",  new[] { "Auto-Retirada", "Auto-Retreat", "自動撤退" } },

        // Modal de recompensa de piso (TowerRewardUI).
        { "UI_CHEST_TITLE",      new[] { "¡Piso superado!", "Floor cleared!", "階クリア！" } },
        { "UI_BOSS_CHEST_TITLE", new[] { "¡Jefe derrotado!", "Boss defeated!", "ボス撃破！" } },
        { "UI_EXP_GAINED",       new[] { "EXP", "EXP", "経験値" } },
        { "UI_CHEST_CONTINUE",   new[] { "Continuar", "Continue", "つづける" } },

        // Alertas de Síntesis (SynthesisManager.Report).
        { "UI_SYNTH_LOCKED",        new[] { "{0} está bloqueado: quita el candado para poder sintetizarlo.",
                                              "{0} is locked: remove the lock to synthesize it.",
                                              "{0}はロック中：合成するにはロックを外してください。" } },
        { "UI_SYNTH_TARGET_SET",    new[] { "Objetivo: {0}. Pulsa en otro héroe para sacrificarlo.",
                                              "Target: {0}. Tap another hero to sacrifice it.",
                                              "対象：{0}。生贄にする英雄をタップしてください。" } },
        { "UI_SYNTH_CANCELLED",     new[] { "Síntesis cancelada.", "Synthesis cancelled.", "合成をキャンセルしました。" } },
        { "UI_SYNTH_INVALID",       new[] { "Síntesis inválida: hacen falta dos héroes distintos.",
                                              "Invalid synthesis: two different heroes are required.",
                                              "無効な合成：異なる英雄が2体必要です。" } },
        { "UI_SYNTH_FODDER_LOCKED", new[] { "{0} está bloqueado y no se puede sacrificar.",
                                              "{0} is locked and cannot be sacrificed.",
                                              "{0}はロック中で生贄にできません。" } },
        { "UI_SYNTH_FODDER_DEPLOYED", new[] { "{0} está desplegado en la Torre y no se puede sacrificar.",
                                              "{0} is deployed in the Tower and cannot be sacrificed.",
                                              "{0}は塔に出撃中で生贄にできません。" } },
        { "UI_SYNTH_NO_PROGRESS",   new[] { "{0} no tiene HeroProgress; no puede absorber EXP.",
                                              "{0} has no HeroProgress; it cannot absorb EXP.",
                                              "{0}にHeroProgressがなく、経験値を吸収できません。" } },
        { "UI_SYNTH_DONE",          new[] { "{0} sacrificado: +{1} EXP para {2} (Nv. {3}).",
                                              "{0} sacrificed: +{1} EXP for {2} (Lv. {3}).",
                                              "{0}を生贄に：{2}に経験値+{1}（Lv.{3}）。" } },

        { "UI_FOREST",       new[] { "Bosque Ancestral", "Ancient Forest", "古の森" } },
        { "UI_MINE",         new[] { "Minas Profundas", "Deep Mines", "深層鉱山" } },
        { "UI_HUNT",         new[] { "Tierras de Caza", "Hunting Grounds", "狩場" } },
        { "UI_RIFT",         new[] { "Grieta Dimensional", "Dimensional Rift", "次元の裂け目" } },
        { "UI_WOOD",         new[] { "Madera", "Wood", "木材" } },
        { "UI_IRON",         new[] { "Hierro", "Iron", "鉄" } },
        { "UI_FOOD",         new[] { "Comida", "Food", "食料" } },
        { "UI_GEMS",         new[] { "Gemas", "Gems", "ジェム" } },

        { "UI_MENU",         new[] { "Menú", "Menu", "メニュー" } },
        { "UI_SECTION_MANAGEMENT", new[] { "Gestión", "Management", "運営" } },
        { "UI_SECTION_STAFF",      new[] { "Personal", "Staff", "人事" } },
        { "UI_SECTION_FACILITIES", new[] { "Instalaciones", "Facilities", "施設" } },

        { "UI_REPAIR",       new[] { "Reparar", "Repair", "修理" } },
        { "UI_BROKEN",       new[] { "ROTO", "BROKEN", "破損" } },

        // Panel de edificio (BuildingInspectUI).
        { "UI_BUILDING_TITLE",  new[] { "{0}   ·   Nv. {1}", "{0}   ·   Lv. {1}", "{0}　・　Lv.{1}" } },
        { "UI_OCCUPANCY",       new[] { "Ocupación: {0}/{1} ({2})",
                                         "Occupancy: {0}/{1} ({2})",
                                         "在籍：{0}/{1}（{2}）" } },
        { "UI_PER_TICK",        new[] { "Por tick: {0}", "Per tick: {0}", "ティックごと：{0}" } },

        // Ratio de producción por tipo de edificio (BuildingInspectUI.BeneficioPorTick): antes
        // eran cadenas fijas en español que no reaccionaban al cambio de idioma.
        { "UI_PROD_TRAINING",  new[] { "{0} EXP cada {1}s", "{0} EXP every {1}s", "{1}秒ごとに{0}EXP" } },
        { "UI_PROD_REST",      new[] { "{0} PV y {1} moral cada {2}s", "{0} HP and {1} morale every {2}s",
                                         "{2}秒ごとにHP{0}・士気{1}" } },
        { "UI_PROD_FARM",      new[] { "{0} comida cada {1}s (+50% por trabajador)",
                                         "{0} food every {1}s (+50% per worker)",
                                         "{1}秒ごとに食料{0}（作業員ごとに+50%）" } },
        { "UI_PROD_WORKSHOP",  new[] { "+{0}% de éxito en la forja", "+{0}% forging success",
                                         "鍛造成功率+{0}%" } },
        { "UI_PROD_MANAWELL",  new[] { "{0} MP cada {1}s (+{2} MP/s a trabajadores)",
                                         "{0} MP every {1}s (+{2} MP/s to workers)",
                                         "{1}秒ごとにMP{0}（作業員に+{2}MP/秒）" } },
        { "UI_PROD_FORGE",     new[] { "-{0}% coste de mejora de equipo", "-{0}% gear upgrade cost",
                                         "装備強化コスト-{0}%" } },
        { "UI_PROD_WARROOM",   new[] { "-{0}% cooldown de decretos", "-{0}% decree cooldown",
                                         "指令クールダウン-{0}%" } },
        { "BTN_ASSIGN",         new[] { "ASIGNAR", "ASSIGN", "配属" } },
        { "BTN_UNASSIGN",       new[] { "DESASIGNAR", "UNASSIGN", "配属解除" } },

        // Panel de asignacion de trabajadores (WorkerAssignUI) y su boton en la ficha del edificio.
        { "UI_ASSIGN_STAFF",    new[] { "Asignar Personal   ({0}/{1})", "Assign Staff   ({0}/{1})",
                                        "配属   ({0}/{1})" } },
        { "UI_ASSIGN_TITLE",    new[] { "Asignar  ·  {0}", "Assign  ·  {0}", "配属  ·  {0}" } },
        { "UI_ASSIGN_SLOTS",    new[] { "Puestos {0}/{1}", "Slots {0}/{1}", "枠 {0}/{1}" } },
        { "UI_ASSIGN_FULL",     new[] { "{0} esta al completo", "{0} is full", "{0}は満員" } },

        // Forja por hueco y gama (Fase 61): la gama se desbloquea subiendo la Torre.
        { "UI_TIER_LOCKED",     new[] { "Se desbloquea al superar el Piso {0}",
                                        "Unlocks after clearing Floor {0}", "{0}階クリアで解放" } },
        { "UI_FORGE_TITLE",     new[] { "Forja", "Forge", "鍛冶" } },
        { "UI_FORGE_WHAT",      new[] { "Que forjar", "What to forge", "何を鍛える" } },
        { "UI_FORGE_TIER",      new[] { "Gama", "Grade", "等級" } },
        { "UI_FORGE_TIER_N",    new[] { "Gama {0}", "Grade {0}", "等級{0}" } },
        { "UI_FORGE_COST",      new[] { "Coste  {0}M / {1}H / {2} comida",
                                        "Cost  {0}W / {1}I / {2} food", "費用  木{0} / 鉄{1} / 食料{2}" } },
        { "UI_FORGE_OUTCOME",   new[] { "Sale una pieza de: {0}", "You get one of: {0}", "入手候補: {0}" } },
        { "UI_FORGE_EMPTY",     new[] { "Nada que forjar en esa gama", "Nothing to forge at that grade",
                                        "その等級で鍛えられる物がない" } },
        { "UI_FORGE_DO",        new[] { "FORJAR", "FORGE", "鍛える" } },
        { "UI_FORGE_MINIGAME",  new[] { "Minijuego de martillo (opcional)", "Hammer minigame (optional)",
                                        "ハンマーのミニゲーム (任意)" } },

        // Minijuego del martillo (HammerMinigameUI): acertar da un afijo extra a la pieza.
        { "UI_HAMMER_TITLE",    new[] { "Martillazo", "Hammer Strike", "槌撃ち" } },
        { "UI_HAMMER_HELP",     new[] { "Para el cursor dentro de la zona dorada para sacar un afijo extra. Fallar no rompe nada.",
                                        "Stop the cursor inside the golden zone for an extra affix. Missing costs nothing.",
                                        "金色の帯でカーソルを止めると追加効果。外しても損はない。" } },
        { "UI_HAMMER_STRIKE",   new[] { "¡GOLPEAR!", "STRIKE!", "打つ！" } },
        { "UI_HAMMER_HIT",      new[] { "¡Golpe perfecto!  {0}", "Perfect strike!  {0}", "会心の一撃！ {0}" } },
        { "UI_HAMMER_MISS",     new[] { "Golpe desviado; la pieza sale sin afijo extra.",
                                        "Off-target; the piece comes out with no extra affix.",
                                        "外れた。追加効果なしで仕上がった。" } },
        { "UI_UPGRADE_DONE",    new[] { "{0} mejorada a +{1}", "{0} upgraded to +{1}", "{0}を+{1}に強化" } },
        { "UI_UPGRADE_MAX",     new[] { "MAX", "MAX", "最大" } },
        { "UI_UPGRADE_MAXED",   new[] { "Ya esta al maximo (+{0})", "Already at max (+{0})", "すでに最大 (+{0})" } },
        { "UI_UPGRADE_NO_PIECE",new[] { "No lleva nada en ese hueco", "Nothing equipped in that slot",
                                        "その枠は空いている" } },

        // Informe de progreso offline (OfflineProgressManager) al volver a cargar la partida.
        { "UI_OFFLINE_AWAY",    new[] { "Mientras no estabas ({0}h {1}m):", "While you were away ({0}h {1}m):",
                                        "留守のあいだ ({0}時間{1}分):" } },
        { "UI_OFFLINE_FOOD",    new[] { "+{0} comida", "+{0} food", "食料 +{0}" } },
        { "UI_OFFLINE_RESTED",  new[] { "{0} heroe(s) descansados", "{0} hero(es) rested", "ヒーロー{0}人が休息" } },
        { "UI_OFFLINE_EXPEDITION", new[] { "recoleccion lista para reclamar", "gathering ready to claim",
                                           "採集の受取が可能" } },
        { "UI_WORKS_AT",        new[] { "   ·   Trabaja en {0}", "   ·   Works at {0}", "　・　{0}で勤務" } },
        { "UI_UPGRADE_BUILDING",new[] { "Mejorar Edificio   ({0}M / {1}H)", "Upgrade Building   ({0}W / {1}I)",
                                         "建物を強化   （木{0} / 鉄{1}）" } },
        { "UI_UPGRADE_BUILDING_CAP", new[] { "Tope de nivel   ·   supera el piso {0}",
                                             "Level cap   ·   clear floor {0}",
                                             "レベル上限　・　{0}階を突破せよ" } },

        // Panel de Taller (CraftingUI).
        { "UI_CRAFT_COST",      new[] { "Coste: {0} Madera + {1} Hierro\nProbabilidad de éxito: {2:0}%",
                                         "Cost: {0} Wood + {1} Iron\nSuccess chance: {2:0}%",
                                         "コスト：木材{0} + 鉄{1}\n成功率：{2:0}%" } },
        { "UI_CRAFT_STONES",    new[] { "Piedras de Ascensión: {0}", "Ascension Stones: {0}", "覚醒石：{0}" } },
        { "UI_CRAFT_STORAGE",   new[] { "     Madera {0} | Hierro {1}", "     Wood {0} | Iron {1}",
                                         "     木材{0} | 鉄{1}" } },

        // Ficha rápida de héroe (HeroQuickCardUI).
        { "UI_QUICKCARD_HP",     new[] { "HP  {0}/{1}", "HP  {0}/{1}", "HP  {0}/{1}" } },
        { "UI_QUICKCARD_MP",     new[] { "MP  {0}/{1}", "MP  {0}/{1}", "MP  {0}/{1}" } },
        { "UI_QUICKCARD_MORALE", new[] { "Moral  {0}   {1}", "Morale  {0}   {1}", "士気  {0}   {1}" } },
        { "UI_QUICKCARD_FATIGUE",new[] { "Fatiga  {0}", "Fatigue  {0}", "疲労  {0}" } },
        { "UI_QUICKCARD_AFFINITY", new[] { "Afecto  {0}/100", "Affinity  {0}/100", "好感度  {0}/100" } },
        { "UI_GIFT",               new[] { "Regalar ({0})", "Gift ({0})", "贈り物（{0}）" } },
        { "UI_EXHAUSTED",        new[] { "   AGOTADO", "   EXHAUSTED", "　消耗" } },
        { "UI_QUICKCARD_GEAR",   new[] { "ATK {0}   DEF {1}\nArma: {2}\nEscudo: {3}\nArmadura: {4}\nAccesorio: {5}",
                                          "ATK {0}   DEF {1}\nWeapon: {2}\nShield: {3}\nArmor: {4}\nAccessory: {5}",
                                          "ATK {0}   DEF {1}\n武器：{2}\n盾：{3}\n鎧：{4}\nアクセサリー：{5}" } },
        { "UI_GEAR_PIECE",       new[] { "{0} ({1}/{2})", "{0} ({1}/{2})", "{0} ({1}/{2})" } },
        { "UI_GEAR_PIECE_BROKEN",new[] { "{0} [ROTO]", "{0} [BROKEN]", "{0}［破損］" } },
        { "UI_GEAR_EMPTY",       new[] { "-", "-", "-" } },
        // Botón "Habilidades" de la ficha rápida y su modal aparte (activa + pasivas + pericia).
        { "UI_SKILLS_BUTTON",         new[] { "Habilidades", "Skills", "スキル" } },
        { "UI_SKILLS_MODAL_TITLE",    new[] { "Habilidades y Pericia", "Skills and Mastery", "スキルと熟練度" } },
        { "UI_SKILLS_SECTION_ACTIVE", new[] { "Habilidad Activa", "Active Skill", "アクティブスキル" } },
        { "UI_SKILLS_SECTION_PASSIVE",new[] { "Pasivas", "Passives", "パッシブ" } },
        { "UI_SKILLS_SECTION_MASTERY",new[] { "Pericia de Arma", "Weapon Mastery", "武器熟練度" } },
        { "UI_MASTERY_EXPLANATION",  new[] { "Sube golpeando en combate o entrenando en el muñeco. A más rango, menos recuperación tras atacar y más probabilidad de esquivar.",
                                               "Grows by fighting or training on the dummy. Higher rank means less recovery after attacking and a better chance to dodge.",
                                               "戦闘や訓練用の人形での鍛錬で上がる。ランクが高いほど攻撃後の隙が減り、回避率も上がる。" } },
        { "UI_MASTERY_NONE",         new[] { "Aún sin entrenar ningún arma.", "Hasn't trained with any weapon yet.", "まだどの武器も鍛えていない。" } },
        { "UI_NO_PASSIVES",          new[] { "sin pasivas", "no passives", "パッシブなし" } },
        { "UI_SKILL_BASIC_STRIKE",   new[] { "Golpe Potente", "Mighty Strike", "渾身の一撃" } },
        { "UI_SKILL_BASIC_STRIKE_DESC", new[] { "Ataque cuerpo a cuerpo reforzado; se sustituye por la habilidad de la subclase al llegar a 3★.",
                                                  "A reinforced melee strike; replaced by the subclass skill upon reaching 3★.",
                                                  "強化された近接攻撃。3★に到達すると転職スキルに置き換わる。" } },
        { "PASSIVE_NAME_PAINTOLERANCE", new[] { "Aguante", "Pain Tolerance", "忍耐" } },
        { "PASSIVE_NAME_EVASION",       new[] { "Evasión", "Evasion", "回避" } },
        { "PASSIVE_NAME_EAGLEEYE",      new[] { "Ojo de Águila", "Eagle Eye", "鷹の目" } },
        { "PASSIVE_DESC_PAINTOLERANCE", new[] { "Encajar golpes cansa la mitad de lo normal.",
                                                   "Taking hits builds fatigue at half the normal rate.",
                                                   "被弾時の疲労蓄積が通常の半分になる。" } },
        { "PASSIVE_DESC_EVASION",       new[] { "Probabilidad de esquivar un golpe por completo, sin sufrir daño.",
                                                   "A chance to dodge a hit entirely, taking no damage.",
                                                   "攻撃を完全に回避し、ダメージを受けない確率がある。" } },
        { "PASSIVE_DESC_EAGLEEYE",      new[] { "Alcance de ataque extra.", "Extra attack range.", "攻撃射程が伸びる。" } },
        { "PASSIVE_NAME_BLOODLUST", new[] { "Sed de Sangre", "Bloodlust", "血の渇き" } },
        { "PASSIVE_NAME_PRECISION", new[] { "Precisión", "Precision", "精密" } },
        { "PASSIVE_NAME_EXECUTIONER", new[] { "Verdugo", "Executioner", "処刑人" } },
        { "PASSIVE_NAME_ARMORBREAKER", new[] { "Rompearmaduras", "Armor Breaker", "鎧砕き" } },
        { "PASSIVE_NAME_VAMPIRIC", new[] { "Vampírico", "Vampiric", "吸血" } },
        { "PASSIVE_NAME_SWIFTBLADE", new[] { "Filo Veloz", "Swiftblade", "迅剣" } },
        { "PASSIVE_NAME_BERSERKER", new[] { "Berserker", "Berserker", "狂戦士" } },
        { "PASSIVE_NAME_INITIATIVE", new[] { "Iniciativa", "Initiative", "先手" } },
        { "PASSIVE_NAME_IRONSKIN", new[] { "Piel de Hierro", "Iron Skin", "鉄の肌" } },
        { "PASSIVE_NAME_VITALITY", new[] { "Vitalidad", "Vitality", "活力" } },
        { "PASSIVE_NAME_LASTSTAND", new[] { "Última Voluntad", "Last Stand", "不屈の意志" } },
        { "PASSIVE_NAME_REGENERATION", new[] { "Regeneración", "Regeneration", "再生" } },
        { "PASSIVE_NAME_COUNTERSTRIKE", new[] { "Contragolpe", "Counterstrike", "反撃" } },
        { "PASSIVE_NAME_BULWARK", new[] { "Baluarte", "Bulwark", "防壁" } },
        { "PASSIVE_NAME_UNBREAKABLE", new[] { "Inquebrantable", "Unbreakable", "不屈" } },
        { "PASSIVE_NAME_TACTICIAN", new[] { "Táctico", "Tactician", "戦術家" } },
        { "PASSIVE_NAME_MANAFLOW", new[] { "Flujo de Maná", "Mana Flow", "魔力循環" } },
        { "PASSIVE_NAME_ARCANEECONOMY", new[] { "Economía Arcana", "Arcane Economy", "魔力節約" } },
        { "PASSIVE_NAME_FLEETFOOT", new[] { "Pies Ligeros", "Fleetfoot", "韋駄天" } },
        { "PASSIVE_NAME_SCOUT", new[] { "Explorador", "Scout", "斥候" } },
        { "PASSIVE_NAME_OPPORTUNIST", new[] { "Oportunista", "Opportunist", "日和見" } },
        { "PASSIVE_NAME_DUELIST", new[] { "Duelista", "Duelist", "決闘者" } },
        { "PASSIVE_NAME_AMBUSHER", new[] { "Emboscador", "Ambusher", "伏兵" } },
        { "PASSIVE_NAME_MENTALFORTITUDE", new[] { "Fortaleza Mental", "Mental Fortitude", "精神力" } },
        { "PASSIVE_NAME_ADAPTABILITY", new[] { "Adaptabilidad", "Adaptability", "適応力" } },
        { "PASSIVE_NAME_MONSTROUSGROWTH", new[] { "Crecimiento Monstruoso", "Monstrous Growth", "規格外の成長" } },
        { "PASSIVE_NAME_LEADERSHIP", new[] { "Liderazgo", "Leadership", "統率" } },
        { "PASSIVE_NAME_JUDGEMENT", new[] { "Juicio", "Judgement", "判断力" } },
        { "PASSIVE_NAME_OBSERVATION", new[] { "Observación", "Observation", "観察眼" } },
        { "PASSIVE_NAME_INDOMITABLE", new[] { "Indomable", "Indomitable", "不撓" } },
        { "PASSIVE_NAME_STRATEGIST", new[] { "Estratega", "Strategist", "策士" } },
        { "PASSIVE_DESC_BLOODLUST", new[] { "Ataque un 12% mayor.",
                                                   "12% more attack.",
                                                   "攻撃力が12%上がる。" } },
        { "PASSIVE_DESC_PRECISION", new[] { "Un 10% más de probabilidad de golpe crítico.",
                                                   "10% more critical hit chance.",
                                                   "クリティカル率が10%上がる。" } },
        { "PASSIVE_DESC_EXECUTIONER", new[] { "Los críticos hacen un 35% más de daño.",
                                                   "Critical hits deal 35% more damage.",
                                                   "クリティカル時のダメージが35%上がる。" } },
        { "PASSIVE_DESC_ARMORBREAKER", new[] { "Ignora un 12% de la defensa enemiga.",
                                                   "Ignores 12% of enemy defense.",
                                                   "敵の防御力を12%無視する。" } },
        { "PASSIVE_DESC_VAMPIRIC", new[] { "Recupera un 8% del daño causado como vida.",
                                                   "Heals for 8% of the damage dealt.",
                                                   "与えたダメージの8%を回復する。" } },
        { "PASSIVE_DESC_SWIFTBLADE", new[] { "Recupera un 15% más rápido entre golpe y golpe.",
                                                   "Recovers 15% faster between hits.",
                                                   "攻撃間の硬直が15%短くなる。" } },
        { "PASSIVE_DESC_BERSERKER", new[] { "Por debajo del 30% de vida, ataca un 40% más fuerte.",
                                                   "Below 30% health, attacks 40% harder.",
                                                   "体力30%以下で攻撃力が40%上がる。" } },
        { "PASSIVE_DESC_INITIATIVE", new[] { "Un 6% más de crítico y un 15% más de daño crítico.",
                                                   "6% more crit chance and 15% more crit damage.",
                                                   "クリティカル率6%、クリティカルダメージ15%上昇。" } },
        { "PASSIVE_DESC_IRONSKIN", new[] { "Defensa un 20% mayor.",
                                                   "20% more defense.",
                                                   "防御力が20%上がる。" } },
        { "PASSIVE_DESC_VITALITY", new[] { "Vida máxima un 15% mayor.",
                                                   "15% more maximum health.",
                                                   "最大体力が15%上がる。" } },
        { "PASSIVE_DESC_LASTSTAND", new[] { "Aguanta a 1 de vida el primer golpe mortal de cada combate.",
                                                   "Survives the first lethal hit of each battle at 1 health.",
                                                   "戦闘ごとに最初の致命傷を体力1で耐える。" } },
        { "PASSIVE_DESC_REGENERATION", new[] { "Recibe un 60% más de curación.",
                                                   "Receives 60% more healing.",
                                                   "受ける回復量が60%増える。" } },
        { "PASSIVE_DESC_COUNTERSTRIKE", new[] { "Un 10% más de evasión y un 8% más de ataque.",
                                                   "10% more evasion and 8% more attack.",
                                                   "回避率10%、攻撃力8%上昇。" } },
        { "PASSIVE_DESC_BULWARK", new[] { "Un 14% más de defensa, a cambio de moverse un 10% más despacio.",
                                                   "14% more defense, but moves 10% slower.",
                                                   "防御力14%上昇、移動速度10%低下。" } },
        { "PASSIVE_DESC_UNBREAKABLE", new[] { "Un 10% más de vida y un 8% más de defensa.",
                                                   "10% more health and 8% more defense.",
                                                   "体力10%、防御力8%上昇。" } },
        { "PASSIVE_DESC_TACTICIAN", new[] { "La habilidad activa se recupera un 15% antes.",
                                                   "The active skill comes off cooldown 15% sooner.",
                                                   "スキルの再使用時間が15%短くなる。" } },
        { "PASSIVE_DESC_MANAFLOW", new[] { "Recupera maná un 50% más rápido.",
                                                   "Regenerates mana 50% faster.",
                                                   "マナ回復速度が50%上がる。" } },
        { "PASSIVE_DESC_ARCANEECONOMY", new[] { "La habilidad activa cuesta un 25% menos de maná.",
                                                   "The active skill costs 25% less mana.",
                                                   "スキルの消費マナが25%減る。" } },
        { "PASSIVE_DESC_FLEETFOOT", new[] { "Se mueve un 20% más rápido.",
                                                   "Moves 20% faster.",
                                                   "移動速度が20%上がる。" } },
        { "PASSIVE_DESC_SCOUT", new[] { "Más alcance de detección y un 10% más de velocidad.",
                                                   "More detection range and 10% more speed.",
                                                   "索敵範囲と移動速度が上がる。" } },
        { "PASSIVE_DESC_OPPORTUNIST", new[] { "Un 8% más de crítico y un 10% de perforación de armadura.",
                                                   "8% more crit chance and 10% armor penetration.",
                                                   "クリティカル率8%、防御貫通10%。" } },
        { "PASSIVE_DESC_DUELIST", new[] { "Un 10% más de ataque y un 8% más de evasión.",
                                                   "10% more attack and 8% more evasion.",
                                                   "攻撃力10%、回避率8%上昇。" } },
        { "PASSIVE_DESC_AMBUSHER", new[] { "Un 8% más de ataque y golpea un 8% más seguido.",
                                                   "8% more attack and strikes 8% more often.",
                                                   "攻撃力8%上昇、攻撃間隔8%短縮。" } },
        { "PASSIVE_DESC_MENTALFORTITUDE", new[] { "Pierde la mitad de moral en las malas rachas.",
                                                   "Loses half the usual morale when things go badly.",
                                                   "士気の低下が半分になる。" } },
        { "PASSIVE_DESC_ADAPTABILITY", new[] { "Un 8% más de ataque y de defensa.",
                                                   "8% more attack and defense.",
                                                   "攻撃力と防御力が8%上がる。" } },
        { "PASSIVE_DESC_MONSTROUSGROWTH", new[] { "Gana un 40% más de experiencia.",
                                                   "Gains 40% more experience.",
                                                   "獲得経験値が40%増える。" } },
        { "PASSIVE_DESC_LEADERSHIP", new[] { "Un 10% más de ataque y aguanta mejor la desmoralización.",
                                                   "10% more attack and holds morale better.",
                                                   "攻撃力10%上昇、士気が下がりにくい。" } },
        { "PASSIVE_DESC_JUDGEMENT", new[] { "Un 5% más de crítico y un 15% más de daño crítico.",
                                                   "5% more crit chance and 15% more crit damage.",
                                                   "クリティカル率5%、クリティカルダメージ15%上昇。" } },
        { "PASSIVE_DESC_OBSERVATION", new[] { "Más alcance de detección y un 10% más de daño crítico.",
                                                   "More detection range and 10% more crit damage.",
                                                   "索敵範囲とクリティカルダメージが上がる。" } },
        { "PASSIVE_DESC_INDOMITABLE", new[] { "Un 12% más de vida y se cansa un 30% menos.",
                                                   "12% more health and builds 30% less fatigue.",
                                                   "体力12%上昇、疲労の蓄積が30%減る。" } },
        { "PASSIVE_DESC_STRATEGIST", new[] { "La habilidad se recupera un 12% antes y pega un 6% más.",
                                                   "The skill recovers 12% sooner and hits 6% harder.",
                                                   "スキル再使用12%短縮、攻撃力6%上昇。" } },
        { "UI_USE_POTION",       new[] { "Usar Poción ({0})", "Use Potion ({0})", "ポーションを使う（{0}）" } },
        { "UI_USE_MANA_POTION",  new[] { "Usar Poción de Maná ({0})", "Use Mana Potion ({0})", "マナポーションを使う（{0}）" } },
        { "UI_POTION_HP",        new[] { "Curación", "Healing", "回復" } },
        { "UI_NO_SUBCLASS",      new[] { "Sin subclase", "No subclass", "サブクラスなし" } },
        { "UI_POTION_MP",        new[] { "Maná", "Mana", "マナ" } },
        { "UI_SEE_ROSTER",       new[] { "Ver en Roster", "View in Roster", "名簿で見る" } },
        { "UI_APATHETIC",        new[] { "   ·   APÁTICO (candidato a síntesis)",
                                          "   ·   APATHETIC (synthesis candidate)",
                                          "　・　無気力（合成候補）" } },
        { "UI_NO_STATUS",        new[] { "Sin estados alterados", "No active status effects", "状態異常なし" } },

        // Taller: reparación (CraftingManager.CraftResolved).
        { "UI_NO_REPAIR_MATERIALS", new[] { "Faltan materiales para reparar", "Not enough materials to repair",
                                              "修理する材料が足りません" } },
        { "UI_PIECE_REPAIRED",      new[] { "{0} reparada", "{0} repaired", "{0}を修理しました" } },
        { "UI_PIECES_REPAIRED",     new[] { "{0} pieza(s) reparada(s)", "{0} piece(s) repaired", "{0}個を修理しました" } },

        // Expediciones de recursos por cooldown (ResourceExpeditionManager).
        { "UI_EXPEDITION_BUSY",    new[] { "La escuadra ya está en {0}.", "The party is already at {0}.",
                                             "部隊はすでに{0}にいます。" } },
        { "UI_EXPEDITION_SENT",    new[] { "Escuadra de {0} enviada a {1} ({2:0}s).",
                                             "Party of {0} sent to {1} ({2:0}s).",
                                             "{0}人の部隊が{1}へ出発（{2:0}秒）。" } },
        { "UI_EXPEDITION_READY",   new[] { "{0}: la escuadra ha vuelto. Reclama la recompensa.",
                                             "{0}: the party is back. Claim the reward.",
                                             "{0}：部隊が帰還しました。報酬を受け取ってください。" } },
        { "UI_EXPEDITION_CLAIMED", new[] { "Vuelta de {0}: +{1} {2}.", "Back from {0}: +{1} {2}.",
                                             "{0}から帰還：+{1} {2}。" } },
        { "UI_EXPEDITION_SUMMARY_TOAST", new[] { "Expedición Completada: +{0} {1}",
                                             "Expedition Complete: +{0} {1}",
                                             "遠征完了：+{0} {1}" } },
        { "UI_CLAIM_REWARD",       new[] { "Reclamar", "Claim", "受け取る" } },

        // Rotación diaria de expediciones: qué destino da bono hoy, mostrado en el panel.
        { "UI_EXPEDITION_TODAY_BONUS", new[] { "Bono de hoy: {0}  (x{1:0.0})",
                                                 "Today's bonus: {0}  (x{1:0.0})",
                                                 "本日のボーナス：{0}（x{1:0.0}）" } },
        { "UI_EXPEDITION_BONUS_TAG",   new[] { "  ★ BONO", "  ★ BONUS", "  ★ボーナス" } },

        // Confirmación de escuadra previa a combate/expedición (SquadManagementUI en modo confirmar).
        { "UI_CONFIRM_ENTER_TOWER", new[] { "Confirmar y Entrar a la Torre", "Confirm and Enter the Tower",
                                              "確認して塔へ入る" } },
        { "UI_CONFIRM_SEND_GATHER", new[] { "Confirmar y Salir a Recolectar", "Confirm and Depart to Gather",
                                              "確認して採集へ出発" } },
        { "UI_CONFIRM_NEED_HEROES", new[] { "Asigna al menos un héroe antes de confirmar",
                                              "Assign at least one hero before confirming",
                                              "確認する前に英雄を1人以上配置してください" } },

        { "BLD_TRAINING",    new[] { "Campo de Entrenamiento", "Training Field", "訓練場" } },
        { "BLD_TRAINING_ADVANCED", new[] { "Campo de Entrenamiento Avanzado", "Advanced Training Field", "上級訓練場" } },
        { "BLD_CANTEEN",     new[] { "Cantina", "Tavern & Canteen", "食堂" } },
        { "BLD_FARM",        new[] { "Granja del Valle", "Valley Farm", "谷の農場" } },
        { "BLD_ALTAR",       new[] { "Altar de Invocación", "Summoning Gate", "召喚の祭壇" } },
        { "BLD_WORKSHOP",    new[] { "Taller de Alquimia", "Alchemy Workshop", "錬金工房" } },
        { "BLD_RESTAREA",    new[] { "Zona de Descanso", "Rest Area", "休憩所" } },
        { "BLD_MANAWELL",    new[] { "Pozo de Maná", "Mana Well", "魔力の泉" } },
        { "BLD_FORGE",       new[] { "Forja", "Forge", "鍛冶場" } },
        { "BLD_WARROOM",     new[] { "Sala de Guerra", "War Room", "作戦室" } },
        { "BLD_ARCHIVE",     new[] { "Archivo del Santuario", "Sanctuary Archive", "聖域の記録庫" } },
        { "BLD_GATEWAY",     new[] { "Portal de la Torre", "Tower Gateway", "塔の門" } },

        // Forja: mejora de equipo, gateada tras la Forja construida (CraftingManager.ForgeUnlocked).
        { "UI_FORGE_LOCKED", new[] { "Requiere la Forja construida", "Requires the Forge built",
                                      "鍛冶場の建設が必要" } },

        // Archivo del Santuario: hitos de ascensión y lore ya desbloqueado por piso.
        { "UI_ARCHIVE_TITLE",      new[] { "Archivo del Santuario", "Sanctuary Archive", "聖域の記録庫" } },
        { "UI_ARCHIVE_MILESTONES", new[] { "Hitos de Ascensión", "Ascension Milestones", "昇格の記録" } },
        { "UI_ARCHIVE_LORE",       new[] { "Crónicas Desbloqueadas", "Unlocked Lore", "解放された記録" } },
        { "UI_ARCHIVE_HINT",       new[] { "Registro de hitos y crónicas de la Torre", "Log of milestones and Tower lore",
                                              "塔の記録と昇格の歴史" } },
        { "UI_ARCHIVE_NO_MILESTONES", new[] { "Aún no hay ascensiones registradas",
                                              "No ascensions recorded yet", "まだ昇格の記録はありません" } },

        { "ARCHIVE_LORE_TOWER",     new[] { "La Torre", "The Tower", "塔" } },
        { "ARCHIVE_LORE_DECREES",   new[] { "Decretos del Maestro", "Decrees of the Master", "師の勅令" } },
        { "ARCHIVE_LORE_HIERARCHY", new[] { "La Ley del Rango", "The Law of Rank", "序列の掟" } },
        { "ARCHIVE_LORE_FACTIONS",  new[] { "Facciones y Orígenes", "Factions and Origins", "派閥と出自" } },
        { "ARCHIVE_LORE_TOWER_BODY", new[] {
            "La Torre es una institución de expedición reconocida en el mundo: el Maestro dirige escuadras piso a piso desde el Portal, sin entrar nunca en persona.",
            "The Tower is an expedition institution recognised across the world: the Master directs squads floor by floor from the Portal, never setting foot inside in person.",
            "塔は世界に認められた遠征機関である。師はポータルから階ごとに部隊を指揮し、自ら足を踏み入れることはない。" } },
        { "ARCHIVE_LORE_DECREES_BODY", new[] {
            "Los Decretos son la única voz de mando directa del Maestro en combate, un protocolo codificado por los eruditos de la Torre de Marfil.",
            "Decrees are the Master's only direct voice of command in battle, a protocol codified by the scholars of the Ivory Tower.",
            "勅令は戦闘における師の唯一の直接命令であり、象牙の塔の学者たちが体系化した手順である。" } },
        { "ARCHIVE_LORE_HIERARCHY_BODY", new[] {
            "La Ley del Rango reparte los cupos de cada instalación de base por mérito de expedición, no por antigüedad ni favor personal.",
            "The Law of Rank allocates every base facility's slots by expedition merit, not by seniority or personal favour.",
            "序列の掟は、年功や縁故ではなく遠征の功績によって拠点各施設の枠を割り振る。" } },
        { "ARCHIVE_MILESTONE", new[] { "{0} asciende a {1}★ (Piso {2}).",
                                        "{0} ascends to {1}★ (Floor {2}).",
                                        "{0}が{1}★に覚醒（{2}階）。" } },
        { "ARCHIVE_LORE_FACTIONS_BODY", new[] {
            "Cada origen de héroe representa una facción real del mundo, con su propia razón para luchar mejor codo a codo con los suyos.",
            "Every hero origin represents a real faction of the world, each with its own reason to fight better shoulder to shoulder with its own.",
            "英雄の出自はそれぞれ世界の実在する派閥を表し、同郷の者と肩を並べてこそ真価を発揮する理由を持つ。" } },

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
        { "SUB_BLOODREAVER", new[] { "Segador de Sangre", "Blood Reaver", "血の刈り手" } },
        { "SUB_RIPOSTEUR", new[] { "Espadachín de Réplica", "Riposteur", "返し剣士" } },
        { "SUB_HALBERDIER", new[] { "Alabardero", "Halberdier", "斧槍兵" } },
        { "SUB_SKEWERER", new[] { "Empalador", "Skewerer", "串刺し" } },
        { "SUB_SENTINEL", new[] { "Centinela", "Sentinel", "歩哨" } },
        { "SUB_RETRIBUTOR", new[] { "Vengador", "Retributor", "報復者" } },
        { "SUB_TRAPPER", new[] { "Trampero", "Trapper", "罠師" } },
        { "SUB_WINDARCHER", new[] { "Arquero del Viento", "Wind Archer", "風の射手" } },
        { "SUB_FROSTWEAVER", new[] { "Tejedor de Escarcha", "Frost Weaver", "氷の織り手" } },
        { "SUB_NECROMANCER", new[] { "Nigromante", "Necromancer", "死霊術師" } },
        { "SUB_EXORCIST", new[] { "Exorcista", "Exorcist", "祓魔師" } },
        { "SUB_BATTLEMONK", new[] { "Monje Guerrero", "Battle Monk", "武僧" } },

        // Bocadillos de la base, agrupados por lo que le pasa al héroe.
        { "SAY_HUNGRY_1",    new[] { "Las raciones escasean...", "Rations are running low...", "食料が足りない…" } },
        { "SAY_HUNGRY_2",    new[] { "¿Queda algo en la cantina?", "Anything left in the canteen?", "食堂に何か残ってるか？" } },
        { "SAY_TIRED_1",     new[] { "Necesito descansar...", "I need rest...", "休養が必要だ…" } },
        { "SAY_TIRED_2",     new[] { "No puedo con mi alma.", "I'm dead on my feet.", "もう限界だ。" } },
        { "SAY_LOWMORALE_1", new[] { "¿De verdad vamos a volver ahí?", "Are we really going back in?", "本当にまた戻るのか？" } },
        { "SAY_LOWMORALE_2", new[] { "Esto acabará mal.", "This will end badly.", "碌なことにならないぞ。" } },
        { "SAY_HURT_1",      new[] { "Estas heridas escuecen.", "These wounds sting.", "この傷が痛む。" } },
        { "SAY_HURT_2",      new[] { "Necesito vendas.", "I need bandages.", "包帯が要る。" } },
        { "SAY_BROKEN_GEAR_1", new[] { "Mi equipo está para el arrastre.", "My gear is falling apart.", "装備がボロボロだ。" } },
        { "SAY_BROKEN_GEAR_2", new[] { "Esta arma ya no aguanta más golpes.", "This weapon can't take another hit.", "この武器はもう限界だ。" } },

        // El héroe se niega a subir a la Torre hasta resolver lo que le pasa; el banner de
        // expedición y el aviso al meterlo en la escuadra comparten los mismos motivos.
        { "UI_INSUBORDINATE_MORALE", new[] { "la moral por los suelos", "morale in the gutter", "士気がどん底" } },
        { "UI_INSUBORDINATE_HEALTH", new[] { "las heridas sin curar", "unhealed wounds", "傷が癒えていない" } },
        { "UI_INSUBORDINATE_GEAR",   new[] { "el equipo roto", "broken gear", "装備の破損" } },
        { "UI_STATUS_INSUBORDINATE", new[] { "{0} se niega a entrar a la Torre: {1}.",
                                               "{0} refuses to enter the Tower: {1}.",
                                               "{0}は塔に入るのを拒否した：{1}。" } },
        { "UI_INSUBORDINATE_WARNING", new[] { "{0} se niega a unirse a la escuadra: {1}.",
                                                "{0} refuses to join the party: {1}.",
                                                "{0}は部隊への参加を拒否した：{1}。" } },
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
        { "SKILL_BLOODREAVER", new[] { "Siega Sangrienta", "Blood Harvest", "血の収穫" } },
        { "SKILL_RIPOSTEUR", new[] { "Réplica", "Riposte", "返し技" } },
        { "SKILL_HALBERDIER", new[] { "Barrido de Alabarda", "Halberd Sweep", "斧槍薙ぎ" } },
        { "SKILL_SKEWERER", new[] { "Ensarte", "Skewer", "串刺し突き" } },
        { "SKILL_SENTINEL", new[] { "Vigilia del Centinela", "Sentinel's Watch", "歩哨の守り" } },
        { "SKILL_RETRIBUTOR", new[] { "Represalia", "Retribution", "報復" } },
        { "SKILL_TRAPPER", new[] { "Cepo de Caza", "Hunting Snare", "狩りの罠" } },
        { "SKILL_WINDARCHER", new[] { "Salva del Viento", "Wind Volley", "風の斉射" } },
        { "SKILL_FROSTWEAVER", new[] { "Manto de Escarcha", "Frost Shroud", "氷の帳" } },
        { "SKILL_NECROMANCER", new[] { "Toque Marchito", "Withering Touch", "枯死の手" } },
        { "SKILL_EXORCIST", new[] { "Rito de Purga", "Rite of Purging", "祓いの儀" } },
        { "SKILL_BATTLEMONK", new[] { "Palma de Hierro", "Iron Palm", "鉄掌" } },

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
        { "SKILLDESC_BLOODREAVER", new[] { "sangrado fuerte y roba vida", "heavy bleed and life steal", "強い出血と吸血" } },
        { "SKILLDESC_RIPOSTEUR", new[] { "golpe doble, el segundo ignora armadura", "double strike, the second ignores armor", "二連撃、二撃目は防御貫通" } },
        { "SKILLDESC_HALBERDIER", new[] { "barrido en área y empuja", "area sweep that pushes back", "範囲を薙ぎ払い、押し戻す" } },
        { "SKILLDESC_SKEWERER", new[] { "hilera con sangrado", "pierces a line and bleeds", "直線を貫き出血させる" } },
        { "SKILLDESC_SENTINEL", new[] { "escuda a la escuadra y provoca", "shields the party and taunts", "部隊を守り、敵を引きつける" } },
        { "SKILLDESC_RETRIBUTOR", new[] { "más daño cuanto peor está", "hits harder the more wounded he is", "傷つくほど威力が上がる" } },
        { "SKILLDESC_TRAPPER", new[] { "inmoviliza y ralentiza", "stuns and slows", "拘束して鈍足にする" } },
        { "SKILLDESC_WINDARCHER", new[] { "tres flechas a objetivos distintos", "three arrows at separate targets", "三本の矢を別々の敵へ" } },
        { "SKILLDESC_FROSTWEAVER", new[] { "congela el área", "freezes the area", "範囲を凍らせる" } },
        { "SKILLDESC_NECROMANCER", new[] { "veneno mágico y roba vida", "magic poison and life steal", "魔法毒と吸命" } },
        { "SKILLDESC_EXORCIST", new[] { "cura y limpia estados", "heals and cleanses ailments", "回復し状態異常を解除する" } },
        { "SKILLDESC_BATTLEMONK", new[] { "cura en área y se cura al pegar", "heals nearby allies and recovers stamina", "周囲を癒し、自らも回復する" } },

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
        { "ROLE_BLOODREAVER", new[] { "Desangra al enemigo y se cura con ello", "Bleeds the enemy dry and heals from it", "敵を出血させ、その分回復する" } },
        { "ROLE_RIPOSTEUR", new[] { "Castiga la guardia abierta con un segundo corte", "Punishes an open guard with a second cut", "隙を突いて二撃目を入れる" } },
        { "ROLE_HALBERDIER", new[] { "Despeja el frente de un barrido", "Clears the front line with one sweep", "一薙ぎで前線を払う" } },
        { "ROLE_SKEWERER", new[] { "Atraviesa a todo lo que tenga delante", "Runs through everything in front", "前方の敵をまとめて貫く" } },
        { "ROLE_SENTINEL", new[] { "Sostiene la línea y protege a los suyos", "Holds the line and protects the party", "戦線を支え、味方を守る" } },
        { "ROLE_RETRIBUTOR", new[] { "Devuelve el castigo que ha recibido", "Returns every blow he has taken", "受けた痛みをそのまま返す" } },
        { "ROLE_TRAPPER", new[] { "Clava al enemigo en el sitio", "Pins the enemy in place", "敵をその場に縫い止める" } },
        { "ROLE_WINDARCHER", new[] { "Reparte flechas por todo el frente", "Spreads arrows across the front", "前線全体に矢を配る" } },
        { "ROLE_FROSTWEAVER", new[] { "Congela el avance enemigo", "Freezes the enemy advance", "敵の進撃を凍らせる" } },
        { "ROLE_NECROMANCER", new[] { "Marchita al enemigo y se alimenta de ello", "Withers the enemy and feeds on it", "敵を枯らし、その命を吸う" } },
        { "ROLE_EXORCIST", new[] { "Cura y limpia lo que envenena a los suyos", "Heals and cleanses what afflicts the party", "味方を癒し、穢れを祓う" } },
        { "ROLE_BATTLEMONK", new[] { "Sostiene a los de al lado sin dejar de pelear", "Sustains those beside him without leaving the fight", "戦いながら周囲を支える" } },

        // Modal de selección de subclase (SubclassSelectionUI).
        { "UI_SUBCLASS_OFFER_TITLE", new[] { "{0} alcanza {1}★   ·   elige su especialidad de {2}",
                                              "{0} reaches {1}★   ·   choose a {2} specialty",
                                              "{0}が{1}★到達　・　{2}の専門を選択" } },
        // Texto flotante de combate (DamageTextManager.Show).
        { "FX_SLAM_CHARGING", new[] { "¡CARGANDO PISOTÓN!", "CHARGING SLAM!", "踏みつけ準備中！" } },
        { "FX_SLAM",          new[] { "¡PISOTÓN!", "SLAM!", "踏みつけ！" } },
        { "FX_NEW_HERO",      new[] { "¡Nuevo Héroe Invocado!", "New Hero Summoned!", "新しい英雄召喚！" } },
        { "FX_CRITICAL",      new[] { "¡CRÍTICO!", "CRITICAL!", "会心の一撃！" } },
        { "FX_FOCUS_FIRE",    new[] { "¡ENFOCAR!", "FOCUS!", "集中攻撃！" } },
        { "FX_DEFENSE",       new[] { "¡DEFENSA!", "DEFENSE!", "防御！" } },
        { "FX_SHIELD_ABSORB", new[] { "-{0} escudo", "-{0} shield", "-{0} シールド" } },
        { "FX_DODGE",         new[] { "¡ESQUIVA!", "DODGE!", "回避！" } },
        { "UI_LEVEL_ABBR",    new[] { "Nv.", "Lv.", "Lv." } },

        { "ROLE_TAG_TANK",    new[] { "Tanque", "Tank", "タンク" } },
        { "ROLE_TAG_CONTROL", new[] { "Control", "Control", "コントロール" } },
        { "ROLE_TAG_SUPPORT", new[] { "Soporte", "Support", "サポート" } },
        { "ROLE_TAG_DPS",     new[] { "DPS", "DPS", "DPS" } },

        // Altar de invocación.
        { "UI_SUMMON",       new[] { "Invocar", "Summon", "召喚" } },
        { "UI_SUMMON_ALTAR", new[] { "Altar de Invocación", "Summoning Gate", "召喚の祭壇" } },
        { "UI_SUMMON_X1",    new[] { "Invocación Simple", "Single Summon", "単発召喚" } },
        { "UI_SUMMON_X10",   new[] { "Invocación Múltiple", "Ten Summon", "十連召喚" } },
        { "UI_REVEAL",       new[] { "Tocar para revelar", "Tap to reveal", "タップで公開" } },
        { "UI_NO_GEMS",      new[] { "Gemas insuficientes", "Not enough gems", "ジェムが足りない" } },
        { "UI_CATALOG_FULL", new[] { "Ya tienes a todos los héroes", "You own every hero", "全ヒーロー獲得済み" } },

        // Tablón de misiones.
        { "UI_QUESTS",       new[] { "Tablón de Misiones", "Quest Board", "依頼掲示板" } },
        { "UI_CLAIM",        new[] { "Reclamar", "Claim", "受け取る" } },
        { "UI_CLAIMED",      new[] { "Reclamada", "Claimed", "受取済" } },
        { "UI_REWARD",       new[] { "Recompensa", "Reward", "報酬" } },
        { "Q_FLOOR",         new[] { "Asalto a la Cima: que la Torre note tu paso hasta el piso {0}.", "Assault on the Peak: make the Tower notice you at floor {0}.", "頂への挑戦：{0}階まで塔に爪痕を残せ。" } },
        { "Q_LEVEL",         new[] { "Forja de Veteranos: entrena a un héroe hasta el nivel {0}.", "Forging Veterans: train a hero up to level {0}.", "戦士の鍛錬：ヒーローをレベル{0}まで鍛え上げろ。" } },
        { "Q_ASCEND",        new[] { "Rito de Ascensión: consagra a un héroe hasta {0}★.", "Rite of Ascension: consecrate a hero to {0}★.", "覚醒の儀：ヒーローを{0}★へ覚醒させろ。" } },
        { "Q_WORKERS",       new[] { "Manos a la Obra: pon a {0} trabajadores en sus puestos.", "All Hands on Deck: put {0} workers to their posts.", "人手を配せ：{0}人の作業員を持ち場につかせろ。" } },
        { "Q_REPAIR",        new[] { "Yunque y Fuelle: repara {0} piezas de equipo.", "Anvil and Bellows: repair {0} pieces of gear.", "金床と鞴：装備を{0}点修理せよ。" } },
        { "Q_RESTED",        new[] { "Descanso Merecido: cura la fatiga de {0} héroe(s).", "A Well-Earned Rest: cure the fatigue of {0} hero(es).", "束の間の休息：ヒーロー{0}人の疲労を癒せ。" } },

        // Puestos: un héroe solo puede ocupar uno a la vez.
        { "UI_DUTY_FREE",       new[] { "Libre", "Idle", "待機" } },
        { "UI_DUTY_BUILDING",   new[] { "En un edificio", "At a building", "施設勤務" } },
        { "UI_DUTY_TOWER",      new[] { "Escuadra de Torre", "Tower Squad", "塔の部隊" } },
        { "UI_DUTY_EXPEDITION", new[] { "Escuadra de Recolección", "Gathering Squad", "採集部隊" } },
        { "UI_ALREADY_ASSIGNED", new[] { "{0} ya está en: {1}", "{0} is already at: {1}", "{0}はすでに{1}に配属" } },
        { "UI_LOCKED",          new[] { "bloqueado", "locked", "ロック中" } },

        // Personalidades (HeroTraits.DisplayName) y estados de moral (HeroController.MoodName).
        { "HERO_TRAIT_DILIGENT", new[] { "Trabajador", "Diligent", "勤勉" } },
        { "HERO_TRAIT_GLUTTON",  new[] { "Glotón", "Glutton", "大食い" } },
        { "HERO_TRAIT_SLACKER",  new[] { "Perezoso", "Slacker", "怠け者" } },
        { "HERO_TRAIT_FIERCE",   new[] { "Feroz", "Fierce", "獰猛" } },

        // Orígenes de héroe (HeroData.origin, texto ES en el propio asset -- ver GetOrigin()).
        { "ORIGIN_FRONTIER_REALM", new[] { "Reino Fronterizo", "Frontier Realm", "辺境の王国" } },
        { "ORIGIN_SALT_ABBEY",     new[] { "Abadia de Sal", "Salt Abbey", "塩の修道院" } },
        { "ORIGIN_ALDER_FOREST",   new[] { "Bosque de Alder", "Alder Forest", "アルダーの森" } },
        { "ORIGIN_LOWTOWN",        new[] { "Ciudad Baja", "Lowtown", "下町" } },
        { "ORIGIN_NORTH_FJORDS",   new[] { "Fiordos del Norte", "Northern Fjords", "北の フィヨルド" } },
        { "ORIGIN_BURNT_MARCH",    new[] { "Marca Quemada", "Burnt March", "焼けた辺境" } },
        { "ORIGIN_IVORY_TOWER",    new[] { "Torre de Marfil", "Ivory Tower", "象牙の塔" } },

        // Títulos de héroe (HeroData.title, texto ES en el propio asset -- ver GetTitle()).
        { "TITLE_ROOKIE",   new[] { "Novato de la Vanguardia", "Vanguard Recruit", "先鋒の新兵" } },
        { "TITLE_ASTRAEA",  new[] { "Saeta del Alba", "Arrow of Dawn", "暁の矢" } },
        { "TITLE_BRUNO",    new[] { "Muro de la Puerta Vieja", "Wall of the Old Gate", "旧門の壁" } },
        { "TITLE_CERES",    new[] { "Hermana de la Vigilia", "Sister of the Vigil", "夜警の姉妹" } },
        { "TITLE_GARRICK",  new[] { "Sargento de Brecha", "Sergeant of the Breach", "突破の軍曹" } },
        { "TITLE_KRAVEN",   new[] { "Verdugo del Ocaso", "Executioner of Dusk", "黄昏の処刑人" } },
        { "TITLE_NEREZZA",  new[] { "Tejedora de Cenizas", "Weaver of Ashes", "灰の織り手" } },
        { "TITLE_PIP",      new[] { "Ratero de los Tejados", "Rooftop Rogue", "屋根の盗人" } },
        { "TITLE_SOLVEIG",  new[] { "Lectora de Escarcha", "Frost Reader", "霜読みの者" } },
        { "TITLE_ULRIC",    new[] { "Yunque de Invierno", "Anvil of Winter", "冬の金床" } },
        { "TITLE_TORVALD",  new[] { "Escudero del Risco", "Squire of the Crag", "断崖の従者" } },
        { "TITLE_WREN",     new[] { "Ojo del Sotobosque", "Eye of the Underbrush", "下生えの目" } },
        { "TITLE_OSRIC",    new[] { "Hachero del Bosque Viejo", "Woodcutter of the Old Forest", "古い森の木こり" } },
        { "TITLE_PETRA",    new[] { "Manos de Horno", "Hands of the Hearth", "かまどの手" } },
        { "TITLE_DAGNY",    new[] { "Guardiana de Rebaño", "Keeper of the Flock", "群れの番人" } },
        { "TITLE_COLM",     new[] { "Yunque Joven", "Young Anvil", "若き金床" } },
        { "TITLE_NESS",     new[] { "Sal de la Tierra", "Salt of the Earth", "大地の塩" } },
        { "TITLE_BRAM",     new[] { "Surco Firme", "Steady Furrow", "揺るがぬ畝" } },
        { "TITLE_OTTILIE",  new[] { "Dedos de Sombra", "Fingers of Shadow", "影の指" } },
        { "TITLE_FARIN",    new[] { "Piedra de Molino", "Millstone", "石臼" } },
        { "TITLE_YSOLDE",   new[] { "Cuenco de Barro", "Bowl of Clay", "土の器" } },
        { "TITLE_GARWEN",   new[] { "Redes del Río", "Nets of the River", "川の網" } },
        { "TITLE_MERET",    new[] { "Guiso de Guarnición", "Garrison Stew", "駐屯の煮込み" } },
        { "TITLE_TORBIN",   new[] { "Cuero y Correa", "Leather and Strap", "革と帯" } },
        { "TITLE_SABLE",    new[] { "Filo de Camino", "Roadside Blade", "街道の刃" } },
        { "TITLE_HOLLIS",   new[] { "Cesto de Leña", "Basket of Firewood", "薪の籠" } },
        { "TITLE_WENNA",    new[] { "Telar de Invierno", "Winter Loom", "冬の機織り" } },
        { "TITLE_CAEL",     new[] { "Pico y Pala", "Pick and Shovel", "つるはしと鋤" } },
        { "TITLE_ISOLDE",   new[] { "Puños de Taberna", "Tavern Fists", "酒場の拳" } },
        { "TITLE_RENN",     new[] { "Rueda y Eje", "Wheel and Axle", "車輪と軸" } },
        { "TITLE_ASLIN",    new[] { "Desertora con Suerte", "Lucky Deserter", "幸運な脱走兵" } },
        { "TITLE_DORAN",    new[] { "Tronco al Hombro", "Log on the Shoulder", "肩の丸太" } },

        // Nombres de enemigo (EnemyData.enemyName, texto ES en el propio asset -- ver GetEnemyName()).
        { "ENEMY_GOBLIN",        new[] { "Goblin", "Goblin", "ゴブリン" } },
        { "ENEMY_GOBLIN_KING",   new[] { "Rey Goblin", "Goblin King", "ゴブリン王" } },
        { "ENEMY_GOBLIN_ARCHER", new[] { "Goblin Tirador", "Goblin Archer", "ゴブリン弓兵" } },
        { "ENEMY_ORC_BRAWLER",   new[] { "Orco Bruto", "Orc Brute", "オークの荒くれ者" } },
        { "ENEMY_DARK_SHAMAN",   new[] { "Chaman Oscuro", "Dark Shaman", "闇のシャーマン" } },
        { "ENEMY_SKELETON_ROGUE",new[] { "Esqueleto Pillo", "Rogue Skeleton", "盗賊スケルトン" } },
        { "UI_MOOD_INSPIRED",    new[] { "Inspirado", "Inspired", "鼓舞" } },
        { "UI_MOOD_DEMORALIZED", new[] { "Desmoralizado", "Demoralized", "士気低下" } },

        // Altar de invocación.
        { "UI_REVEAL_ALL",   new[] { "Revelar Todo", "Reveal All", "すべて公開" } },
        { "UI_ACCEPT",       new[] { "Aceptar", "Accept", "決定" } },

        // Taller de Alquimia.
        { "UI_WORKSHOP",     new[] { "Taller de Alquimia", "Alchemy Workshop", "錬金工房" } },
        { "UI_ARTISANS",     new[] { "Artesanos", "Artisans", "職人" } },
        { "UI_COST",         new[] { "Coste", "Cost", "費用" } },
        { "UI_FORGE_STONES", new[] { "Forja de Piedras", "Stone Forging", "覚醒石錬成" } },
        { "UI_TAB_EQUIPMENT", new[] { "Forjar y Reparar", "Forge & Repair", "鍛造・修理" } },
        { "UI_TAB_ALCHEMY",  new[] { "Alquimia", "Alchemy", "錬金術" } },
        { "UI_STONES_HELD",  new[] { "Piedras de Ascensión", "Ascension Stones", "覚醒石" } },
        { "UI_SUCCESS_CHANCE", new[] { "Probabilidad de éxito", "Success chance", "成功率" } },
        { "UI_FORGE",        new[] { "Forjar Piedra", "Forge Stone", "石を錬成" } },
        { "UI_FORGE_WEAPONS", new[] { "Fabricación de Armas", "Weapon Crafting", "武器の製作" } },
        { "UI_FORGE_WEAPONS_HELP", new[] { "Sale un arma al azar del recetario y va al almacén.",
                                           "A random weapon from the recipe book goes to storage.",
                                           "レシピからランダムな武器が倉庫に入る。" } },
        { "UI_CRAFT_EQUIPMENT", new[] { "Fabricar Equipo", "Craft Equipment", "装備を作る" } },
        { "UI_REPAIR_GEAR",  new[] { "Reparación de Equipo", "Gear Repair", "装備の修理" } },
        { "UI_TOTAL_WEAR",   new[] { "Desgaste acumulado", "Total wear", "累積損耗" } },
        { "UI_NO_MATERIALS", new[] { "Faltan materiales", "Not enough materials", "素材が足りない" } },
        { "UI_NO_RECIPES",   new[] { "Sin recetas de arma", "No weapon recipes", "武器レシピなし" } },
        { "UI_STONE_FORGED", new[] { "¡Piedra forjada!", "Stone forged!", "石を錬成した！" } },
        { "UI_CRAFT_FAILED", new[] { "¡FALLO DE FORJA!", "FORGING FAILED!", "錬成失敗！" } },
        { "UI_FORGE_POTIONS", new[] { "Pociones de Curación", "Healing Potions", "回復薬の調合" } },
        { "UI_FORGE_MANA_POTIONS", new[] { "Pociones de Maná", "Mana Potions", "マナポーションの調合" } },
        { "UI_POTIONS_HELD",  new[] { "Pociones", "Potions", "回復薬" } },
        { "UI_MANA_POTIONS_HELD", new[] { "Pociones de Maná", "Mana Potions", "マナポーション" } },
        { "UI_POTION_MENOR",      new[] { "Poción Menor", "Minor Potion", "小さな回復薬" } },
        { "UI_POTION_MEDIA",      new[] { "Poción Media", "Median Potion", "中位の回復薬" } },
        { "UI_POTION_MAYOR",      new[] { "Poción Mayor", "Greater Potion", "大きな回復薬" } },
        { "UI_MANA_POTION_MENOR", new[] { "Poción de Maná Menor", "Minor Mana Potion", "小さなマナポーション" } },
        { "UI_MANA_POTION_MEDIA", new[] { "Poción de Maná Media", "Median Mana Potion", "中位のマナポーション" } },
        { "UI_MANA_POTION_MAYOR", new[] { "Poción de Maná Mayor", "Greater Mana Potion", "大きなマナポーション" } },
        { "UI_CRAFT_POTION",  new[] { "Fabricar Poción", "Craft Potion", "回復薬を作る" } },
        { "UI_CRAFT_MANA_POTION", new[] { "Fabricar Poción de Maná", "Craft Mana Potion", "マナポーションを作る" } },
        { "UI_POTION_CRAFTED", new[] { "¡Poción lista!", "Potion ready!", "回復薬が完成した！" } },
        { "UI_MANA_POTION_CRAFTED", new[] { "¡Poción de maná lista!", "Mana potion ready!", "マナポーションが完成した！" } },
        { "UI_UPGRADE_GEAR",  new[] { "Mejora de Equipo", "Gear Upgrade", "装備強化" } },
        { "UI_UPGRADE_GEAR_HELP", new[] { "Bonus plano de ataque y defensa para el héroe elegido.",
                                          "Flat attack and defense bonus for the chosen hero.",
                                          "選んだ英雄に攻撃力と防御力の固定ボーナスを与える。" } },
        { "UI_UPGRADE_BUTTON", new[] { "Mejorar Equipo", "Upgrade Gear", "装備を強化" } },
        { "UI_ALL_GEAR_UPGRADED", new[] { "¡Equipo de todo el roster mejorado!", "Whole roster's gear upgraded!", "全員の装備を強化した！" } },

        // Equipamiento manual y almacén.
        { "UI_AUTO_EQUIP",   new[] { "Auto-equipar", "Auto-equip", "自動装備" } },
        { "UI_STORAGE",      new[] { "Almacén", "Storage", "倉庫" } },
        { "UI_EMPTY_STORAGE", new[] { "Almacén vacío", "Storage is empty", "倉庫は空" } },
        { "UI_SECTION_MATERIALS", new[] { "Materiales", "Materials", "素材" } },
        { "UI_SECTION_STONES",    new[] { "Piedras de Ascensión", "Ascension Stones", "昇格石" } },
        { "UI_SECTION_POTIONS",   new[] { "Pociones", "Potions", "ポーション" } },
        { "UI_SECTION_EQUIPMENT", new[] { "Equipo", "Equipment", "装備" } },
        { "UI_PAGE_INDICATOR", new[] { "Página {0}/{1}", "Page {0}/{1}", "ページ {0}/{1}" } },
        { "UI_RANDOM_PIECE", new[] { "Pieza al azar", "Random piece", "ランダム装備" } },
        { "UI_DURABILITY",   new[] { "Durabilidad", "Durability", "耐久" } },
        { "UI_SLOT_TAKEN",   new[] { "sustituye a la actual", "replaces current", "現在の装備と交換" } },
        { "UI_SLOT_WEAPON",  new[] { "Arma", "Weapon", "武器" } },
        { "UI_SLOT_SHIELD",  new[] { "Escudo", "Shield", "盾" } },
        { "UI_SLOT_ARMOR",   new[] { "Armadura", "Armor", "鎧" } },
        { "UI_SLOT_ACCESSORY", new[] { "Accesorio", "Accessory", "装飾品" } },

        // Tipos de arma (WeaponTypes.DisplayName).
        { "WEAPON_SWORD",     new[] { "Espada", "Sword", "剣" } },
        { "WEAPON_SPEAR",     new[] { "Lanza", "Spear", "槍" } },
        { "WEAPON_BOW",       new[] { "Arco", "Bow", "弓" } },
        { "WEAPON_SHIELD",    new[] { "Escudo", "Shield", "盾" } },
        { "WEAPON_ARMOR",     new[] { "Armadura", "Armor", "鎧" } },
        { "WEAPON_ACCESSORY", new[] { "Accesorio", "Accessory", "装飾品" } },
        { "WEAPON_STAFF",     new[] { "Báculo", "Staff", "杖" } },
        { "WEAPON_MACE",      new[] { "Maza", "Mace", "メイス" } },
        { "WEAPON_NONE",      new[] { "Sin arma", "No weapon", "武器なし" } },

        // Escuadras y presets.
        { "UI_SQUADS",       new[] { "Escuadras", "Squads", "部隊編成" } },
        { "UI_TOWER_SQUAD",  new[] { "Torre", "Tower", "塔" } },
        { "UI_GATHER_SQUAD", new[] { "Recolectar", "Gather", "採集" } },
        { "UI_PRESET",       new[] { "Escuadra {0}", "Squad {0}", "部隊{0}" } },
        { "UI_SAVE_PRESET",  new[] { "Guardar", "Save", "保存" } },
        { "UI_NO_HEROES",    new[] { "No hay héroes en la base", "No heroes at the base", "拠点にヒーローがいない" } },
        { "UI_GATHERING_NOW", new[] { "Recolección en curso: {0}s", "Gathering in progress: {0}s", "採集中：{0}秒" } },
        { "UI_GATHER_READY", new[] { "Recolección lista", "Gathering ready", "採集完了" } },
        { "BATTLE_SIGHTING_MELEE", new[] { "{0} enfrente. Sin tiradores.", "{0} ahead. No shooters.", "前方に{0}体。射手なし。" } },
        { "BATTLE_SIGHTING_MIXED", new[] { "{0} enfrente y {1} tirando desde atrás.", "{0} ahead, {1} shooting from the back.", "前方に{0}体、後方に射手{1}体。" } },
        { "BATTLE_PLAN_HOLD", new[] { "Yo aguanto el frente. Vosotros, detrás.", "I hold the front. Stay behind me.", "前は俺が持つ。後ろにいろ。" } },
        { "ABILITY_POISONCUT", new[] { "Corte Ponzoñoso", "Venom Cut", "毒断ち" } },
        { "ABILITY_IRONGUARD", new[] { "Guardia de Hierro", "Iron Guard", "鉄の構え" } },
        { "ABILITY_BLADEDANCE", new[] { "Danza de Cortes", "Blade Dance", "斬撃乱舞" } },
        { "ABILITY_BLOODHARVEST", new[] { "Siega Sangrienta", "Blood Harvest", "血の収穫" } },
        { "ABILITY_RIPOSTE", new[] { "Réplica", "Riposte", "返し技" } },
        { "ABILITY_DRAGONTHRUST", new[] { "Lanza del Dragón", "Dragon Lance", "竜槍" } },
        { "ABILITY_PIKEPUSH", new[] { "Empuje de Pica", "Pike Thrust", "槍衾" } },
        { "ABILITY_STORMPIERCE", new[] { "Perforar Tormenta", "Storm Pierce", "嵐穿ち" } },
        { "ABILITY_HALBERDSWEEP", new[] { "Barrido de Alabarda", "Halberd Sweep", "斧槍薙ぎ" } },
        { "ABILITY_SKEWER", new[] { "Ensarte", "Skewer", "串刺し突き" } },
        { "ABILITY_LIGHTCALL", new[] { "Égida de Luz", "Aegis of Light", "光の盾" } },
        { "ABILITY_UNSTOPPABLECHARGE", new[] { "Embate Imparable", "Unstoppable Charge", "不動の突進" } },
        { "ABILITY_IMMORTALWALL", new[] { "Muralla Inmortal", "Immortal Wall", "不滅の壁" } },
        { "ABILITY_SENTINELWATCH", new[] { "Vigilia del Centinela", "Sentinel's Watch", "歩哨の守り" } },
        { "ABILITY_RETRIBUTION", new[] { "Represalia", "Retribution", "報復" } },
        { "ABILITY_CHARGEDSHOT", new[] { "Disparo Certero", "Perfect Shot", "必中の一矢" } },
        { "ABILITY_ARROWRAIN", new[] { "Lluvia de Flechas", "Arrow Volley", "矢の雨" } },
        { "ABILITY_SHADOWBOLT", new[] { "Saeta Sombría", "Shadow Bolt", "影の矢" } },
        { "ABILITY_HUNTINGSNARE", new[] { "Cepo de Caza", "Hunting Snare", "狩りの罠" } },
        { "ABILITY_WINDVOLLEY", new[] { "Salva del Viento", "Wind Volley", "風の斉射" } },
        { "ABILITY_FIREBURST", new[] { "Estallido Ígneo", "Fire Burst", "業火の炸裂" } },
        { "ABILITY_TIMEFRACTURE", new[] { "Freno Temporal", "Time Warp", "時の楔" } },
        { "ABILITY_ARCANERAY", new[] { "Descarga Arcana", "Arcane Surge", "秘術の奔流" } },
        { "ABILITY_FROSTSHROUD", new[] { "Manto de Escarcha", "Frost Shroud", "氷の帳" } },
        { "ABILITY_WITHERINGTOUCH", new[] { "Toque Marchito", "Withering Touch", "枯死の手" } },
        { "ABILITY_GREATERBLESSING", new[] { "Luz Sanadora", "Healing Light", "癒しの光" } },
        { "ABILITY_ORACLEAEGIS", new[] { "Manto Protector", "Warding Veil", "守りの帳" } },
        { "ABILITY_WARHYMN", new[] { "Cántico de Guerra", "War Chant", "戦の詠唱" } },
        { "ABILITY_PURGINGRITE", new[] { "Rito de Purga", "Rite of Purging", "祓いの儀" } },
        { "ABILITY_IRONPALM", new[] { "Palma de Hierro", "Iron Palm", "鉄掌" } },
        { "ABILITY_BASICSTRIKE", new[] { "Golpe Potente", "Mighty Strike", "渾身の一撃" } },
        { "ABILITYDESC_POISONCUT", new[] { "envenena 6s", "poisons for 6s", "6秒毒付与" } },
        { "ABILITYDESC_IRONGUARD", new[] { "escudo propio", "shields self", "自身にシールド" } },
        { "ABILITYDESC_BLADEDANCE", new[] { "tres cortes y sangrado", "three cuts and bleed", "三連斬と出血" } },
        { "ABILITYDESC_BLOODHARVEST", new[] { "sangrado fuerte y roba vida", "heavy bleed and life steal", "強い出血と吸血" } },
        { "ABILITYDESC_RIPOSTE", new[] { "golpe doble, el segundo ignora armadura", "double strike, the second ignores armor", "二連撃、二撃目は防御貫通" } },
        { "ABILITYDESC_DRAGONTHRUST", new[] { "daño en hilera", "pierces in a line", "直線貫通" } },
        { "ABILITYDESC_PIKEPUSH", new[] { "empuja y ralentiza", "knocks back and slows", "押し戻して鈍足" } },
        { "ABILITYDESC_STORMPIERCE", new[] { "ignora armadura y aturde", "ignores armour and stuns", "防御無視とスタン" } },
        { "ABILITYDESC_HALBERDSWEEP", new[] { "barrido en área y empuja", "area sweep that pushes back", "範囲を薙ぎ払い、押し戻す" } },
        { "ABILITYDESC_SKEWER", new[] { "hilera con sangrado", "pierces a line and bleeds", "直線を貫き出血させる" } },
        { "ABILITYDESC_LIGHTCALL", new[] { "provoca y escuda a la escuadra", "taunts and shields the party", "挑発と部隊シールド" } },
        { "ABILITYDESC_UNSTOPPABLECHARGE", new[] { "aturde y limpia fatiga", "stuns and clears fatigue", "スタンと疲労回復" } },
        { "ABILITYDESC_IMMORTALWALL", new[] { "escudo enorme propio", "huge shield on self", "自身に大シールド" } },
        { "ABILITYDESC_SENTINELWATCH", new[] { "escuda a la escuadra y provoca", "shields the party and taunts", "部隊を守り、敵を引きつける" } },
        { "ABILITYDESC_RETRIBUTION", new[] { "más daño cuanto peor está", "hits harder the more wounded he is", "傷つくほど威力が上がる" } },
        { "ABILITYDESC_CHARGEDSHOT", new[] { "crítico a distancia", "critical from range", "遠距離クリティカル" } },
        { "ABILITYDESC_ARROWRAIN", new[] { "área y ralentiza", "area hit and slow", "範囲攻撃と鈍足" } },
        { "ABILITYDESC_SHADOWBOLT", new[] { "veneno y retroceso", "poison and knockback", "毒と後退" } },
        { "ABILITYDESC_HUNTINGSNARE", new[] { "inmoviliza y ralentiza", "stuns and slows", "拘束して鈍足にする" } },
        { "ABILITYDESC_WINDVOLLEY", new[] { "tres flechas a objetivos distintos", "three arrows at separate targets", "三本の矢を別々の敵へ" } },
        { "ABILITYDESC_FIREBURST", new[] { "área ígnea", "fire area", "火炎範囲" } },
        { "ABILITYDESC_TIMEFRACTURE", new[] { "ralentiza a todos", "slows everyone", "全体を鈍足" } },
        { "ABILITYDESC_ARCANERAY", new[] { "gasta todo el maná", "spends all mana", "マナ全消費" } },
        { "ABILITYDESC_FROSTSHROUD", new[] { "congela el área", "freezes the area", "範囲を凍らせる" } },
        { "ABILITYDESC_WITHERINGTOUCH", new[] { "veneno mágico y roba vida", "magic poison and life steal", "魔法毒と吸命" } },
        { "ABILITYDESC_GREATERBLESSING", new[] { "cura al más herido", "heals the most hurt", "最も傷ついた者を回復" } },
        { "ABILITYDESC_ORACLEAEGIS", new[] { "escudos a la escuadra", "shields the party", "部隊にシールド" } },
        { "ABILITYDESC_WARHYMN", new[] { "+ATK en área", "+ATK in an area", "範囲攻撃力上昇" } },
        { "ABILITYDESC_PURGINGRITE", new[] { "cura y limpia estados", "heals and cleanses ailments", "回復し状態異常を解除する" } },
        { "ABILITYDESC_IRONPALM", new[] { "cura en área y se cura al pegar", "heals nearby allies and recovers stamina", "周囲を癒し、自らも回復する" } },
        { "ABILITYDESC_BASICSTRIKE", new[] { "Ataque cuerpo a cuerpo reforzado; se sustituye por la habilidad de la subclase al llegar a 3★.", "A reinforced melee strike; replaced by the subclass skill upon reaching 3★.", "強化された近接攻撃。3★に到達すると転職スキルに置き換わる。" } },
        { "INTENT_FOCUSING", new[] { "Centrando", "Focusing", "狙い撃ち" } },
        { "INTENT_HOLDING", new[] { "Sujetando", "Holding", "足止め" } },
        { "INTENT_SAVINGSKILL", new[] { "Reservando", "Saving skill", "温存" } },
        { "INTENT_PROTECTING", new[] { "Protegiendo", "Protecting", "援護" } },
        { "INTENT_KITING", new[] { "Separándose", "Kiting", "距離取り" } },

        // Afijos pasivos del equipo.
        { "AFFIX_LIFESTEAL",    new[] { "Robo de vida", "Life steal", "吸血" } },
        { "AFFIX_EVASIONBOOST", new[] { "Evasión", "Evasion", "回避" } },
        { "AFFIX_CRITDAMAGE",   new[] { "Daño crítico", "Crit damage", "会心ダメージ" } },
        { "AFFIX_ARMORPIERCE",  new[] { "Perfora armadura", "Armor pierce", "防御貫通" } }
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

    // HeroData.origin guarda el texto ES tal cual (dato de diseño, no una clave) -- se traduce
    // buscando esa cadena entre los 7 orígenes conocidos; si no coincide, se enseña sin traducir.
    private static readonly Dictionary<string, string> OriginKeys = new Dictionary<string, string>
    {
        { "Reino Fronterizo", "ORIGIN_FRONTIER_REALM" },
        { "Abadia de Sal", "ORIGIN_SALT_ABBEY" },
        { "Bosque de Alder", "ORIGIN_ALDER_FOREST" },
        { "Ciudad Baja", "ORIGIN_LOWTOWN" },
        { "Fiordos del Norte", "ORIGIN_NORTH_FJORDS" },
        { "Marca Quemada", "ORIGIN_BURNT_MARCH" },
        { "Torre de Marfil", "ORIGIN_IVORY_TOWER" },
    };

    public static string GetOrigin(string origin)
        => !string.IsNullOrEmpty(origin) && OriginKeys.TryGetValue(origin, out var key) ? Get(key) : origin;

    // HeroData.title guarda el texto ES tal cual (dato de diseño, no una clave) -- se traduce
    // buscando esa cadena entre los títulos conocidos; si no coincide (título nuevo sin
    // catalogar), se enseña sin traducir en vez de romper.
    private static readonly Dictionary<string, string> TitleKeys = new Dictionary<string, string>
    {
        { "Novato de la Vanguardia", "TITLE_ROOKIE" },
        { "Saeta del Alba", "TITLE_ASTRAEA" },
        { "Muro de la Puerta Vieja", "TITLE_BRUNO" },
        { "Hermana de la Vigilia", "TITLE_CERES" },
        { "Sargento de Brecha", "TITLE_GARRICK" },
        { "Verdugo del Ocaso", "TITLE_KRAVEN" },
        { "Tejedora de Cenizas", "TITLE_NEREZZA" },
        { "Ratero de los Tejados", "TITLE_PIP" },
        { "Lectora de Escarcha", "TITLE_SOLVEIG" },
        { "Yunque de Invierno", "TITLE_ULRIC" },
        { "Escudero del Risco", "TITLE_TORVALD" },
        { "Ojo del Sotobosque", "TITLE_WREN" },
        { "Hachero del Bosque Viejo", "TITLE_OSRIC" },
        { "Manos de Horno", "TITLE_PETRA" },
        { "Guardiana de Rebaño", "TITLE_DAGNY" },
        { "Yunque Joven", "TITLE_COLM" },
        { "Sal de la Tierra", "TITLE_NESS" },
        { "Surco Firme", "TITLE_BRAM" },
        { "Dedos de Sombra", "TITLE_OTTILIE" },
        { "Piedra de Molino", "TITLE_FARIN" },
        { "Cuenco de Barro", "TITLE_YSOLDE" },
        { "Redes del Río", "TITLE_GARWEN" },
        { "Guiso de Guarnición", "TITLE_MERET" },
        { "Cuero y Correa", "TITLE_TORBIN" },
        { "Filo de Camino", "TITLE_SABLE" },
        { "Cesto de Leña", "TITLE_HOLLIS" },
        { "Telar de Invierno", "TITLE_WENNA" },
        { "Pico y Pala", "TITLE_CAEL" },
        { "Puños de Taberna", "TITLE_ISOLDE" },
        { "Rueda y Eje", "TITLE_RENN" },
        { "Desertora con Suerte", "TITLE_ASLIN" },
        { "Tronco al Hombro", "TITLE_DORAN" },
    };

    public static string GetTitle(string title)
        => !string.IsNullOrEmpty(title) && TitleKeys.TryGetValue(title, out var key) ? Get(key) : title;

    // EnemyData.enemyName guarda el texto ES tal cual (dato de diseño, no una clave) -- mismo
    // patrón que TitleKeys/OriginKeys: si no coincide, se enseña sin traducir en vez de romper.
    private static readonly Dictionary<string, string> EnemyNameKeys = new Dictionary<string, string>
    {
        { "Goblin", "ENEMY_GOBLIN" },
        { "Rey Goblin", "ENEMY_GOBLIN_KING" },
        { "Goblin Tirador", "ENEMY_GOBLIN_ARCHER" },
        { "Orco Bruto", "ENEMY_ORC_BRAWLER" },
        { "Chaman Oscuro", "ENEMY_DARK_SHAMAN" },
        { "Esqueleto Pillo", "ENEMY_SKELETON_ROGUE" },
    };

    public static string GetEnemyName(string enemyName)
        => !string.IsNullOrEmpty(enemyName) && EnemyNameKeys.TryGetValue(enemyName, out var key) ? Get(key) : enemyName;

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
