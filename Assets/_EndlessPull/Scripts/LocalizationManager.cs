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

        // Menú de pausa in-game (InGameMenuUI).
        { "UI_IN_GAME_MENU", new[] { "Menú", "Menu", "メニュー" } },
        { "UI_RESUME",       new[] { "Reanudar", "Resume", "再開" } },
        { "UI_UI_VOLUME",    new[] { "Volumen de interfaz", "UI Volume", "UI音量" } },
        { "UI_COMBAT_VOLUME",new[] { "Volumen de combate", "Combat Volume", "戦闘音量" } },
        { "UI_QUIT_TO_MENU", new[] { "Guardar y salir", "Save and Quit", "セーブして終了" } },
        { "UI_CLOSE",        new[] { "Cerrar", "Close", "とじる" } },
        { "UI_NO_SAVE",      new[] { "Sin partida guardada", "No saved game", "セーブデータなし" } },

        { "UI_VIEW_HEROES",  new[] { "Ver Héroes", "View Heroes", "英雄一覧" } },
        { "UI_HEAL_ALL",     new[] { "Curar Todos", "Heal All", "全員回復" } },
        { "UI_ALL_HEALED",   new[] { "Todos los héroes ya están al máximo de salud",
                                       "All heroes are already at full health", "全員すでに満タンです" } },
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
        { "UI_ASCEND_PREVIEW",    new[] { "{0}★ → {1}★  (×{2:0.00} estadísticas)",
                                            "{0}★ → {1}★  (×{2:0.00} stats)",
                                            "{0}★ → {1}★（ステータス×{2:0.00}）" } },
        { "UI_ASCEND_COST",       new[] { "{0} gemas + 1 {1}", "{0} gems + 1 {1}", "ジェム{0} + {1}×1" } },
        { "UI_ASCEND_MAX_RARITY", new[] { "Rareza máxima", "Max rarity", "最大レア度" } },
        { "UI_ASCEND_NEED_LEVEL", new[] { "Necesita llegar a Nv. {0}", "Needs to reach Lv. {0}", "Lv.{0}が必要" } },

        { "UI_READY",        new[] { "¡PREPARAOS!", "READY...", "構えろ！" } },
        { "UI_ENGAGE",       new[] { "¡AL ATAQUE!", "ENGAGE!", "突撃！" } },
        { "UI_RETREAT",      new[] { "¡RETIRADA!", "RETREAT!", "撤退！" } },

        { "UI_FLOOR",        new[] { "Piso", "Floor", "階" } },
        { "UI_FIRST_CLEAR",  new[] { "Primera vez", "First clear", "初回" } },
        { "UI_REPEAT",       new[] { "Repetición", "Repeat", "周回" } },
        // Cuadrantes direccionales bloqueados de la base (QuadrantController).
        { "UI_QUADRANT_FLOOR_LABEL", new[] { "Piso {0}", "Floor {0}", "{0}階" } },
        { "UI_QUADRANT_LOCKED_TAP",  new[] { "Desbloquea en Piso {0}", "Unlocks at Floor {0}", "{0}階で解放" } },

        // Estados de combate/expedición mostrados en el HUD (WaveManager.Report).
        { "UI_STATUS_ASSIGN_HEROES", new[] { "Asigna héroes a la escuadra", "Assign heroes to the party", "部隊に英雄を配置してください" } },
        { "UI_STATUS_FLOOR_START",   new[] { "Piso {0}: {1} enemigos{2} (x{3:0.00}){4}  Escuadra {5}/{6}",
                                               "Floor {0}: {1} enemies{2} (x{3:0.00}){4}  Party {5}/{6}",
                                               "{0}階：敵{1}体{2}（x{3:0.00}）{4}　部隊{5}/{6}" } },
        { "UI_BOSS_TAG",             new[] { " + JEFE", " + BOSS", " + ボス" } },
        { "UI_BOSS_ARRIVAL",         new[] { "¡JEFE!", "BOSS!", "ボス出現！" } },
        { "UI_STATUS_WON",   new[] { "Piso {0} superado ({1})  +{2} gemas, +{3} madera, +{4} hierro",
                                       "Floor {0} cleared ({1})  +{2} gems, +{3} wood, +{4} iron",
                                       "{0}階クリア（{1}）　+{2}ジェム、+{3}木材、+{4}鉄" } },
        { "UI_STATUS_LOST",  new[] { "Expedición fallida en el piso {0}", "Expedition failed on floor {0}",
                                       "{0}階で遠征失敗" } },
        { "UI_STATUS_RETREAT", new[] { "Retirada del piso {0}: {1} héroe(s) a salvo, sin recompensa",
                                         "Retreat from floor {0}: {1} hero(es) safe, no reward",
                                         "{0}階から撤退：{1}人の英雄が無事、報酬なし" } },

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
        { "UI_SYNTH_NO_PROGRESS",   new[] { "{0} no tiene HeroProgress; no puede absorber EXP.",
                                              "{0} has no HeroProgress; it cannot absorb EXP.",
                                              "{0}にHeroProgressがなく、経験値を吸収できません。" } },
        { "UI_SYNTH_DONE",          new[] { "{0} sacrificado: +{1} EXP para {2} (Nv. {3}).",
                                              "{0} sacrificed: +{1} EXP for {2} (Lv. {3}).",
                                              "{0}を生贄に：{2}に経験値+{1}（Lv.{3}）。" } },

        { "UI_FOREST",       new[] { "Bosque", "Forest", "森" } },
        { "UI_MINE",         new[] { "Mina", "Mine", "鉱山" } },
        { "UI_HUNT",         new[] { "Tierras de Caza", "Hunting Grounds", "狩場" } },
        { "UI_WOOD",         new[] { "Madera", "Wood", "木材" } },
        { "UI_IRON",         new[] { "Hierro", "Iron", "鉄" } },
        { "UI_FOOD",         new[] { "Comida", "Food", "食料" } },
        { "UI_GEMS",         new[] { "Gemas", "Gems", "ジェム" } },

        { "UI_MENU",         new[] { "Menú", "Menu", "メニュー" } },
        { "UI_SECTION_MANAGEMENT", new[] { "Gestión", "Management", "運営" } },
        { "UI_SECTION_STAFF",      new[] { "Personal", "Staff", "人事" } },
        { "UI_SECTION_FACILITIES", new[] { "Instalaciones", "Facilities", "施設" } },

        { "UI_SUBCLASS",     new[] { "Subclase", "Subclass", "職種" } },
        { "UI_REPAIR",       new[] { "Reparar", "Repair", "修理" } },
        { "UI_BROKEN",       new[] { "ROTO", "BROKEN", "破損" } },

        // Panel de edificio (BuildingInspectUI).
        { "UI_BUILDING_TITLE",  new[] { "{0}   ·   Nv. {1}", "{0}   ·   Lv. {1}", "{0}　・　Lv.{1}" } },
        { "UI_OCCUPANCY",       new[] { "Ocupación: {0}/{1} ({2})   ·   Torre: piso {3}",
                                         "Occupancy: {0}/{1} ({2})   ·   Tower: floor {3}",
                                         "在籍：{0}/{1}（{2}）　・　塔：{3}階" } },
        { "UI_PER_TICK",        new[] { "Por tick: {0}", "Per tick: {0}", "ティックごと：{0}" } },
        { "UI_UPGRADE_BUILDING",new[] { "Mejorar Edificio   ({0}M / {1}H)", "Upgrade Building   ({0}W / {1}I)",
                                         "建物を強化   （木{0} / 鉄{1}）" } },

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
        { "UI_EXHAUSTED",        new[] { "   AGOTADO", "   EXHAUSTED", "　消耗" } },
        { "UI_QUICKCARD_GEAR",   new[] { "ATK {0}   DEF {1}\nArma: {2}\nEscudo: {3}\nArmadura: {4}\nAccesorio: {5}",
                                          "ATK {0}   DEF {1}\nWeapon: {2}\nShield: {3}\nArmor: {4}\nAccessory: {5}",
                                          "ATK {0}   DEF {1}\n武器：{2}\n盾：{3}\n鎧：{4}\nアクセサリー：{5}" } },
        { "UI_GEAR_PIECE",       new[] { "{0} ({1}/{2})", "{0} ({1}/{2})", "{0} ({1}/{2})" } },
        { "UI_GEAR_PIECE_BROKEN",new[] { "{0} [ROTO]", "{0} [BROKEN]", "{0}［破損］" } },
        { "UI_GEAR_EMPTY",       new[] { "-", "-", "-" } },
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
        { "UI_CLAIM_REWARD",       new[] { "Reclamar", "Claim", "受け取る" } },

        // Confirmación de escuadra previa a combate/expedición (SquadManagementUI en modo confirmar).
        { "UI_CONFIRM_ENTER_TOWER", new[] { "Confirmar y Entrar a la Torre", "Confirm and Enter the Tower",
                                              "確認して塔へ入る" } },
        { "UI_CONFIRM_SEND_GATHER", new[] { "Confirmar y Salir a Recolectar", "Confirm and Depart to Gather",
                                              "確認して採集へ出発" } },
        { "UI_CONFIRM_NEED_HEROES", new[] { "Asigna al menos un héroe antes de confirmar",
                                              "Assign at least one hero before confirming",
                                              "確認する前に英雄を1人以上配置してください" } },

        { "BLD_TRAINING",    new[] { "Campo de Entrenamiento", "Training Field", "訓練場" } },
        { "BLD_CANTEEN",     new[] { "Cantina", "Tavern & Canteen", "食堂" } },
        { "BLD_FARM",        new[] { "Granja", "Farm", "農場" } },
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

        // Altar de invocación.
        { "UI_REVEAL_ALL",   new[] { "Revelar Todo", "Reveal All", "すべて公開" } },
        { "UI_ACCEPT",       new[] { "Aceptar", "Accept", "決定" } },

        // Taller de Alquimia.
        { "UI_WORKSHOP",     new[] { "Taller de Alquimia", "Alchemy Workshop", "錬金工房" } },
        { "UI_ARTISANS",     new[] { "Artesanos", "Artisans", "職人" } },
        { "UI_COST",         new[] { "Coste", "Cost", "費用" } },
        { "UI_FORGE_STONES", new[] { "Forja de Piedras", "Stone Forging", "石の錬成" } },
        { "UI_TAB_EQUIPMENT", new[] { "Forja y Reparación", "Forge & Repair", "製作と修理" } },
        { "UI_TAB_ALCHEMY",  new[] { "Alquimia", "Alchemy", "錬金術" } },
        { "UI_STONES_HELD",  new[] { "Piedras de Ascensión", "Ascension Stones", "覚醒石" } },
        { "UI_SUCCESS_CHANCE", new[] { "Probabilidad de éxito", "Success chance", "成功率" } },
        { "UI_FORGE",        new[] { "Forjar Piedra", "Forge Stone", "石を錬成" } },
        { "UI_FORGE_WEAPONS", new[] { "Fabricación de Armas", "Weapon Crafting", "武器の製作" } },
        { "UI_FORGE_WEAPONS_HELP", new[] { "Sale un arma al azar del recetario y va al almacén.",
                                           "A random weapon from the recipe book goes to storage.",
                                           "レシピからランダムな武器が倉庫に入る。" } },
        { "UI_CRAFT_WEAPON", new[] { "Fabricar Arma", "Craft Weapon", "武器を作る" } },
        { "UI_REPAIR_GEAR",  new[] { "Reparación de Equipo", "Gear Repair", "装備の修理" } },
        { "UI_TOTAL_WEAR",   new[] { "Desgaste acumulado", "Total wear", "累積損耗" } },
        { "UI_NO_MATERIALS", new[] { "Faltan materiales", "Not enough materials", "素材が足りない" } },
        { "UI_NO_RECIPES",   new[] { "Sin recetas de arma", "No weapon recipes", "武器レシピなし" } },
        { "UI_STONE_FORGED", new[] { "¡Piedra forjada!", "Stone forged!", "石を錬成した！" } },
        { "UI_CRAFT_FAILED", new[] { "¡FALLO DE FORJA!", "FORGING FAILED!", "錬成失敗！" } },
        { "UI_FORGE_POTIONS", new[] { "Pociones de Curación", "Healing Potions", "回復薬の調合" } },
        { "UI_POTIONS_HELD",  new[] { "Pociones", "Potions", "回復薬" } },
        { "UI_CRAFT_POTION",  new[] { "Fabricar Poción", "Craft Potion", "回復薬を作る" } },
        { "UI_POTION_CRAFTED", new[] { "¡Poción lista!", "Potion ready!", "回復薬が完成した！" } },
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
        { "UI_RANDOM_PIECE", new[] { "Pieza al azar", "Random piece", "ランダム装備" } },
        { "UI_DURABILITY",   new[] { "Durabilidad", "Durability", "耐久" } },
        { "UI_SLOT_TAKEN",   new[] { "sustituye a la actual", "replaces current", "現在の装備と交換" } },
        { "UI_SLOT_WEAPON",  new[] { "Arma", "Weapon", "武器" } },
        { "UI_SLOT_SHIELD",  new[] { "Escudo", "Shield", "盾" } },
        { "UI_SLOT_ARMOR",   new[] { "Armadura", "Armor", "鎧" } },
        { "UI_SLOT_ACCESSORY", new[] { "Accesorio", "Accessory", "装飾品" } },

        // Escuadras y presets.
        { "UI_SQUADS",       new[] { "Escuadras", "Squads", "部隊編成" } },
        { "UI_TOWER_SQUAD",  new[] { "Torre", "Tower", "塔" } },
        { "UI_GATHER_SQUAD", new[] { "Recolectar", "Gather", "採集" } },
        { "UI_PRESET",       new[] { "Escuadra {0}", "Squad {0}", "部隊{0}" } },
        { "UI_SAVE_PRESET",  new[] { "Guardar", "Save", "保存" } },
        { "UI_NO_HEROES",    new[] { "No hay héroes en la base", "No heroes at the base", "拠点にヒーローがいない" } },
        { "UI_GATHERING_NOW", new[] { "Recolección en curso: {0}s", "Gathering in progress: {0}s", "採集中：{0}秒" } },

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
