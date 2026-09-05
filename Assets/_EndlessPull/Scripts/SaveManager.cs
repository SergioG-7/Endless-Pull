using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Puntos de maestría de un tipo de arma. JsonUtility no serializa diccionarios, de ahí la lista.
[System.Serializable]
public class MasterySaveData
{
    public int weaponType;
    public int points;
}

// Una fila del roster tal y como se escribe en disco.
[System.Serializable]
public class HeroSaveData
{
    public string heroInstanceId;
    public string heroDataAssetName;
    public int level = 1;
    public int currentExp;
    public int currentHealth;
    public int currentMP;
    public float fatigue;
    public float morale = 80f;
    public HeroTrait trait;
    public bool isLocked;
    public int subclass;
    public string assignedBuilding = string.Empty;

    // Personalidad de combate: agresividad, distancia de seguridad, umbral de habilidad.
    public float aggression = 0.5f;
    public float safeDistance = 0.5f;
    public float skillThreshold = 0.15f;

    // Refinamiento de habilidad (0-1) ganado entrenando en el muñeco de la base; 0 en saves
    // anteriores a esta fase, arranca desde cero sin migración especial.
    public float skillRefinement;

    // Los enums van como int: es lo único que JsonUtility garantiza dentro de una lista.
    public List<int> passives = new List<int>();
    public List<MasterySaveData> mastery = new List<MasterySaveData>();

    public int bonusStarRank;
    public float ascensionMultiplier = 1f;

    // Afecto por regalos; 0 por defecto en saves antiguos, sin héroe regalado todavía.
    public float affinity;

    // Mejora de equipo básico del Taller: bonus plano, independiente del nivel/ascensión.
    public int gearUpgradeAttack;
    public int gearUpgradeDefense;

    public string weaponAssetName;
    public string shieldAssetName;
    public string armorAssetName;
    public string accessoryAssetName;

    // Formato nuevo: cada pieza equipada con lo suyo (afijo forjado, desgaste, nivel).
    public List<EquipmentInstanceSaveData> equipped = new List<EquipmentInstanceSaveData>();

    // --- Formato antiguo, anterior al equipo instanciado. Ya no se rellena al guardar, pero
    // se conserva para poder migrar partidas guardadas antes (ver MigrateLegacyEquipment). ---
    public int weaponGearLevel;
    public int shieldGearLevel;
    public int armorGearLevel;
    public int accessoryGearLevel;

    public int weaponDurability;
    public int shieldDurability;
    public int armorDurability;
    public int accessoryDurability;
}

// Un preset de escuadras: quién sube a la torre y quién sale a recolectar.
[System.Serializable]
public class SquadPresetSaveData
{
    public List<string> party = new List<string>();
    public List<string> expedition = new List<string>();
}

// Nivel de un edificio, identificado por el nombre de su GameObject en la escena.
[System.Serializable]
public class BuildingSaveData
{
    public string buildingId;
    public int level = 1;
}

// Todo lo que acaba dentro de savegame.json.
[System.Serializable]
public class GameSaveData
{
    // Version del esquema de guardado; permite migrar formatos antiguos en el futuro.
    public int saveVersion = 1;

    // Momento del guardado en UTC (ISO-8601). Vacío en partidas anteriores al progreso offline:
    // sin marca no se acredita nada, que es lo correcto para un save de antes.
    public string lastSaveUtc = string.Empty;

    public int gems;
    public int wood;
    public int iron;
    public int food;
    public int currentFloor = 1;
    public int highestClearedFloor;

    // Zurrón de piedras de saves anteriores a los tiers; se migra a Menor al cargar (ver Load).
    public int ascensionStones;

    // Piedras por tier: [0]=Menor [1]=Media [2]=Mayor [3]=Legendaria.
    public int[] ascensionStoneCounts;

    // Zurrón de pociones de saves anteriores a los tiers; se migra a Menor al cargar (ver Load).
    public int healingPotions;
    public int manaPotions;

    // Pociones por tier: [0]=Menor [1]=Media [2]=Mayor.
    public int[] healingPotionCounts;
    public int[] manaPotionCounts;

    // Pisos que ya pagaron su Sub-Misión Oculta alguna vez; no se vuelve a sortear ahí.
    public List<int> hiddenChallengeAwardedFloors = new List<int>();

    // Expedición de recursos en curso, si la había al guardar (WS3: cooldown real + reclamo manual).
    public int expeditionType;
    public float expeditionRemaining;
    public int expeditionHeroesSent;
    public bool expeditionReadyToClaim;

    // Animación de revelado de cuadrante ya reproducida; evita repetirla en cargas posteriores.
    public bool quadrantEastRevealed;
    public bool quadrantSouthRevealed;
    public bool quadrantWestRevealed;
    public bool quadrantNorthRevealed;

    // Identidad de cada héroe de la escuadra; no depende del orden del array.
    public List<string> party = new List<string>();
    public List<string> expeditionSquad = new List<string>();
    public List<SquadPresetSaveData> presets = new List<SquadPresetSaveData>();
    public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
    public List<HeroSaveData> heroes = new List<HeroSaveData>();

    // Piezas que no lleva nadie puesto, cada una con lo suyo.
    public List<EquipmentInstanceSaveData> inventoryItems = new List<EquipmentInstanceSaveData>();

    // Formato antiguo del almacén (solo nombres de asset); se migra al cargar y ya no se escribe.
    public List<string> inventory = new List<string>();

    // Piezas del Guardián ya conseguidas; sin esto la suelta segura volvería a caer cada vez.
    public List<string> wardenGranted = new List<string>();

    // Galería Memorial: los héroes perdidos y cómo se perdieron.
    public List<MemorialRecord> memorial = new List<MemorialRecord>();
}

// Guarda y restaura la partida en JSON dentro de Application.persistentDataPath.
public class SaveManager : MonoBehaviour
{
    // Version actual del esquema; se escribe en cada guardado nuevo.
    private const int CurrentSaveVersion = 1;

    [Tooltip("Nombre del fichero dentro de Application.persistentDataPath.")]
    [SerializeField] private string fileName = "savegame.json";

    [Tooltip("Economía que se guarda y se restaura.")]
    [SerializeField] private EconomyManager economy;

    [Tooltip("Gestor de oleadas: aporta el piso y avisa al terminar una expedición.")]
    [SerializeField] private WaveManager waves;

    [Tooltip("Gacha: de él salen el prefab y el catálogo para reconstruir el roster.")]
    [SerializeField] private GachaManager gacha;

    [Tooltip("Tienda: aporta el catálogo de equipo y el inventario común.")]
    [SerializeField] private ShopManager shop;

    [Tooltip("Taller: guarda las Piedras de Ascensión.")]
    [SerializeField] private CraftingManager crafting;

    [Tooltip("Escuadra: guarda quién está apuntado y los intentos de torre.")]
    [SerializeField] private PartyManager party;

    [Tooltip("Expediciones de recursos: guarda el temporizador y si hay recompensa pendiente.")]
    [SerializeField] private ResourceExpeditionManager expeditions;

    [Tooltip("Progreso offline: acredita al cargar lo que la base produjo con el juego cerrado.")]
    [SerializeField] private OfflineProgressManager offline;

    [Tooltip("Escribe el JSON indentado para poder leerlo a mano.")]
    [SerializeField] private bool prettyPrint = true;

    [Tooltip("Carga sola al arrancar; se apaga cuando el menú principal decide qué partida abrir.")]
    [SerializeField] private bool loadOnStart = true;

    private static SaveManager instance;

    // La pone en true/false quien muestre un menú previo a elegir partida (p.ej. MainMenuUI):
    // así SaveManager no necesita conocer ninguna clase de UI.
    public static bool SavingAllowed = true;

    // Se dispara al terminar de reconstruir el roster de héroes guardado; economy.LoadState()
    // (más arriba en Load()) ya lanza sus propios eventos antes de que existan los héroes, así
    // que la UI que cuenta héroes (p.ej. MasterHUD) necesita esta señal aparte, no la de gemas.
    public static event System.Action RosterLoaded;

    public string SavePath => Path.Combine(Application.persistentDataPath, fileName);
    public string TempSavePath => SavePath + ".tmp";
    public string BackupSavePath => SavePath + ".bak";
    public bool HasSave => File.Exists(SavePath);

    void Awake()
    {
        instance = this;

        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        if (waves == null) waves = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
        if (gacha == null) gacha = UnityEngine.Object.FindFirstObjectByType<GachaManager>();
        if (shop == null) shop = UnityEngine.Object.FindFirstObjectByType<ShopManager>();
        if (crafting == null) crafting = UnityEngine.Object.FindFirstObjectByType<CraftingManager>();
        if (party == null) party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();
        if (expeditions == null) expeditions = UnityEngine.Object.FindFirstObjectByType<ResourceExpeditionManager>();
    }

    void OnEnable()
    {
        if (waves != null) waves.ExpeditionChanged += OnExpeditionChanged;
    }

    void OnDisable()
    {
        if (waves != null) waves.ExpeditionChanged -= OnExpeditionChanged;
        if (instance == this) instance = null;
    }

    // La carga va en Start y no en Awake: así los edificios ya se han registrado en BaseBuilding.All.
    void Start()
    {
        if (loadOnStart) Load();
    }

    // Salir del Play Mode cuenta como cerrar el juego, y hay que conservar maná y fatiga.
    void OnApplicationQuit()
    {
        Save();
    }

    // Punto de entrada para los sistemas que no tienen referencia al manager.
    public static void RequestSave()
    {
        if (instance != null) instance.Save();
    }

    private void OnExpeditionChanged(ExpeditionState state, string message)
    {
        if (state == ExpeditionState.Won || state == ExpeditionState.Lost) Save();
    }

    public void Save()
    {
        // Con el menu principal delante aun no se ha elegido partida: guardar borraria la de disco.
        if (!SavingAllowed)
        {
            Debug.Log("[Guardado] Ignorado: aun no se ha elegido partida.", this);
            return;
        }

        var save = new GameSaveData
        {
            saveVersion = CurrentSaveVersion,
            lastSaveUtc = System.DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture)
        };

        if (economy != null)
        {
            save.gems = economy.Gems;
            save.wood = economy.Wood;
            save.iron = economy.Iron;
            save.food = economy.Food;
        }

        if (crafting != null) save.ascensionStoneCounts = (int[])crafting.StoneCounts.Clone();
        if (crafting != null) save.healingPotionCounts = (int[])crafting.HealingPotionCounts.Clone();
        if (crafting != null) save.manaPotionCounts = (int[])crafting.ManaPotionCounts.Clone();

        if (expeditions != null)
        {
            save.expeditionType = (int)expeditions.CurrentType;
            save.expeditionRemaining = expeditions.Remaining;
            save.expeditionHeroesSent = expeditions.HeroesSent;
            save.expeditionReadyToClaim = expeditions.ReadyToClaim;
        }

        if (waves != null)
        {
            save.currentFloor = waves.CurrentFloor;
            save.highestClearedFloor = waves.HighestClearedFloor;
            save.hiddenChallengeAwardedFloors = new List<int>(waves.HiddenChallengeAwardedFloors);
            save.wardenGranted = new List<string>(waves.WardenGranted);
        }

        var memorial = UnityEngine.Object.FindFirstObjectByType<MemorialManager>();
        if (memorial != null) save.memorial = memorial.Snapshot();

        save.quadrantEastRevealed = QuadrantController.Find(QuadrantId.East)?.Revealed ?? false;
        save.quadrantSouthRevealed = QuadrantController.Find(QuadrantId.South)?.Revealed ?? false;
        save.quadrantWestRevealed = QuadrantController.Find(QuadrantId.West)?.Revealed ?? false;
        save.quadrantNorthRevealed = QuadrantController.Find(QuadrantId.North)?.Revealed ?? false;

        foreach (var building in BaseBuilding.All)
        {
            if (building == null) continue;
            save.buildings.Add(new BuildingSaveData
            {
                buildingId = building.SaveId,
                level = building.Level
            });
        }

        var heroes = UnityEngine.Object.FindObjectsByType<HeroController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var hero in heroes)
        {
            if (hero.Data == null) continue;

            var progress = hero.GetComponent<HeroProgress>();
            var entry = new HeroSaveData
            {
                heroInstanceId = hero.HeroInstanceId,
                heroDataAssetName = hero.Data.name,
                level = progress != null ? progress.Level : 1,
                currentExp = progress != null ? progress.CurrentEXP : 0,
                currentHealth = hero.CurrentHealth,
                currentMP = hero.CurrentMP,
                fatigue = hero.Fatigue,
                morale = hero.Morale,
                trait = hero.Trait,
                isLocked = hero.IsLocked,
                subclass = (int)hero.Subclass,
                aggression = progress != null ? progress.Aggression : 0.5f,
                safeDistance = progress != null ? progress.SafeDistance : 0.5f,
                skillThreshold = progress != null ? progress.SkillThreshold : 0.15f,
                skillRefinement = progress != null ? progress.SkillRefinement : 0f,
                assignedBuilding = hero.AssignedBuilding != null ? hero.AssignedBuilding.SaveId : string.Empty,
                bonusStarRank = hero.BonusStarRank,
                ascensionMultiplier = hero.AscensionMultiplier,
                affinity = hero.Affinity,
                gearUpgradeAttack = hero.GearUpgradeAttack,
                gearUpgradeDefense = hero.GearUpgradeDefense
            };

            // Cada pieza equipada va entera: asset base, afijo forjado, desgaste y nivel.
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                var pieza = ToSaveData(hero.GetEquipped(slot));
                if (pieza != null) entry.equipped.Add(pieza);
            }

            foreach (var passive in hero.Passives) entry.passives.Add((int)passive);

            foreach (var pair in hero.Mastery.AllPoints)
                entry.mastery.Add(new MasterySaveData { weaponType = (int)pair.Key, points = pair.Value });

            save.heroes.Add(entry);
        }

        if (shop != null)
            foreach (var item in shop.Inventory)
            {
                var pieza = ToSaveData(item);
                if (pieza != null) save.inventoryItems.Add(pieza);
            }

        if (party != null)
        {
            save.party.AddRange(party.PartyInstanceIds());
            save.expeditionSquad.AddRange(party.ExpeditionInstanceIds());

            for (int i = 0; i < PartyManager.PresetCount; i++)
                save.presets.Add(new SquadPresetSaveData
                {
                    party = new List<string>(party.PresetPartyIds(i)),
                    expedition = new List<string>(party.PresetExpeditionIds(i))
                });
        }

        try
        {
            // Se escribe primero a un temporal: si el proceso muere a mitad, el .json de verdad
            // ni se toca. File.Replace intercambia ambos de un golpe y deja el anterior en .bak.
            File.WriteAllText(TempSavePath, JsonUtility.ToJson(save, prettyPrint));

            if (File.Exists(SavePath))
                File.Replace(TempSavePath, SavePath, BackupSavePath);
            else
                File.Move(TempSavePath, SavePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Guardado] No se pudo escribir {SavePath}: {e.Message}", this);
            return;
        }

        int totalPiedras = 0;
        if (save.ascensionStoneCounts != null)
            foreach (int n in save.ascensionStoneCounts) totalPiedras += n;

        Debug.Log($"[Guardado] {save.heroes.Count} héroe(s), piso {save.currentFloor}, " +
                  $"{save.gems} gemas, {save.wood}M/{save.iron}H/{save.food}C, " +
                  $"{totalPiedras} piedra(s) -> {SavePath}", this);
    }

    public void Load()
    {
        if (!HasSave)
        {
            Debug.Log($"[Guardado] Sin partida previa; se arranca con lo que trae la escena. ({SavePath})", this);
            return;
        }

        GameSaveData save;
        try
        {
            save = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(SavePath));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Guardado] No se pudo leer {SavePath}: {e.Message}", this);
            return;
        }

        if (save == null)
        {
            Debug.LogError($"[Guardado] {SavePath} no contiene una partida válida.", this);
            return;
        }

        // LoadState dispara los eventos, así que la UI se pone al día sin depender del orden de Start.
        if (economy != null) economy.LoadState(save.gems, save.wood, save.iron, save.food);
        // Las partidas anteriores al selector de torre no traen el piso maximo: se deduce del suyo.
        if (save.highestClearedFloor <= 0)
            save.highestClearedFloor = Mathf.Max(0, save.currentFloor - 1);

        if (waves != null)
        {
            waves.LoadProgress(save.currentFloor, save.highestClearedFloor);
            waves.LoadHiddenChallengeAwardedFloors(save.hiddenChallengeAwardedFloors);
            waves.LoadWardenGranted(save.wardenGranted);
        }

        UnityEngine.Object.FindFirstObjectByType<MemorialManager>()?.LoadRecords(save.memorial);

        // El piso ya está publicado en BaseBuilding.TowerFloor: los cuadrantes pueden calcular su IsUnlocked.
        QuadrantController.Find(QuadrantId.East)?.LoadRevealed(save.quadrantEastRevealed);
        QuadrantController.Find(QuadrantId.South)?.LoadRevealed(save.quadrantSouthRevealed);
        QuadrantController.Find(QuadrantId.West)?.LoadRevealed(save.quadrantWestRevealed);
        QuadrantController.Find(QuadrantId.North)?.LoadRevealed(save.quadrantNorthRevealed);

        if (crafting != null)
        {
            // Un save de antes de los tiers no trae ascensionStoneCounts: su zurrón genérico
            // se conserva entero como Piedra Menor, para no perder progreso ya guardado.
            var counts = save.ascensionStoneCounts;
            int menor = (counts != null && counts.Length > 0) ? counts[0] : save.ascensionStones;
            int media = (counts != null && counts.Length > 1) ? counts[1] : 0;
            int mayor = (counts != null && counts.Length > 2) ? counts[2] : 0;
            int legendaria = (counts != null && counts.Length > 3) ? counts[3] : 0;
            int trascendente = (counts != null && counts.Length > 4) ? counts[4] : 0;
            int celestial = (counts != null && counts.Length > 5) ? counts[5] : 0;
            crafting.LoadStones(menor, media, mayor, legendaria, trascendente, celestial);
        }

        if (crafting != null)
        {
            // Un save de antes de los tiers no trae healingPotionCounts: su zurrón genérico
            // se conserva entero como Poción Menor, para no perder progreso ya guardado.
            var hp = save.healingPotionCounts;
            int hpMenor = (hp != null && hp.Length > 0) ? hp[0] : save.healingPotions;
            int hpMedia = (hp != null && hp.Length > 1) ? hp[1] : 0;
            int hpMayor = (hp != null && hp.Length > 2) ? hp[2] : 0;
            crafting.LoadPotions(hpMenor, hpMedia, hpMayor);

            var mp = save.manaPotionCounts;
            int mpMenor = (mp != null && mp.Length > 0) ? mp[0] : save.manaPotions;
            int mpMedia = (mp != null && mp.Length > 1) ? mp[1] : 0;
            int mpMayor = (mp != null && mp.Length > 2) ? mp[2] : 0;
            crafting.LoadManaPotions(mpMenor, mpMedia, mpMayor);
        }

        if (expeditions != null)
            expeditions.LoadState(save.expeditionType, save.expeditionRemaining,
                save.expeditionHeroesSent, save.expeditionReadyToClaim);

        RestoreBuildings(save);
        RestoreInventory(save);
        RestoreRoster(save);

        Debug.Log($"[Guardado] Partida cargada: {save.heroes.Count} héroe(s), piso {save.currentFloor}, " +
                  $"{save.gems} gemas, {save.wood}M/{save.iron}H.", this);

        // Lo último: necesita el roster y los edificios ya restaurados para saber quién descansaba.
        if (offline == null) offline = UnityEngine.Object.FindFirstObjectByType<OfflineProgressManager>();
        if (offline != null) offline.ApplySince(save.lastSaveUtc);
    }

    // Borra el fichero; útil para empezar de cero sin tocar la escena.
    public void DeleteSave()
    {
        if (!HasSave) return;

        File.Delete(SavePath);
        Debug.Log($"[Guardado] Partida borrada: {SavePath}", this);
    }

    private static EquipmentInstanceSaveData ToSaveData(EquipmentInstance item)
    {
        if (item == null || !item.IsValid) return null;

        return new EquipmentInstanceSaveData
        {
            instanceId = item.instanceId,
            assetName = item.data.name,
            slot = (int)item.SlotType,
            forgedAffix = (int)item.forgedAffix,
            forgedValue = item.forgedValue,
            durability = item.durability,
            gearLevel = item.gearLevel
        };
    }

    // Vuelve a atar la pieza guardada con su asset del catálogo; null si ese asset ya no existe.
    private EquipmentInstance FromSaveData(EquipmentInstanceSaveData saved)
    {
        if (saved == null || shop == null) return null;

        var asset = shop.FindByAssetName(saved.assetName);
        if (asset == null)
        {
            Debug.LogWarning($"[Guardado] {saved.assetName} ya no está en el catálogo de equipo.", this);
            return null;
        }

        return new EquipmentInstance
        {
            instanceId = string.IsNullOrEmpty(saved.instanceId)
                ? System.Guid.NewGuid().ToString() : saved.instanceId,
            data = asset,
            forgedAffix = (EquipmentAffix)saved.forgedAffix,
            forgedValue = saved.forgedValue,
            // 0 de desgaste en un save viejo significaba "sin estrenar", no "rota".
            durability = saved.durability > 0 ? saved.durability : asset.maxDurability,
            gearLevel = Mathf.Max(0, saved.gearLevel)
        };
    }

    // El inventario se rehace antes que el roster: equipar saca piezas de él.
    private void RestoreInventory(GameSaveData save)
    {
        if (shop == null) return;

        shop.ClearInventory();

        foreach (var saved in save.inventoryItems)
        {
            var pieza = FromSaveData(saved);
            if (pieza != null) shop.AddToInventory(pieza);
        }

        // Migración: una partida anterior al equipo instanciado solo guardaba nombres de asset.
        // Sin esto el almacén se vaciaría en silencio al cargarla.
        if (save.inventoryItems.Count == 0 && save.inventory.Count > 0)
        {
            foreach (var assetName in save.inventory)
            {
                var asset = shop.FindByAssetName(assetName);
                if (asset != null) shop.AddToInventory(asset);
            }

            Debug.Log($"[Guardado] Almacén migrado al formato nuevo: {save.inventory.Count} pieza(s).", this);
        }
    }

    private void RestoreBuildings(GameSaveData save)
    {
        foreach (var entry in save.buildings)
        {
            foreach (var building in BaseBuilding.All)
            {
                if (building == null || building.SaveId != entry.buildingId) continue;

                building.LoadLevel(entry.level);
                break;
            }
        }
    }

    // Lo equipado se guarda aparte y no está en la lista de inventario, así que solo se coloca.
    // Cada pieza llega entera (afijo forjado, desgaste y nivel incluidos), sin retoques después.
    private void RestoreEquipment(HeroController hero, HeroSaveData entry)
    {
        if (shop == null) return;

        if (entry.equipped.Count > 0)
        {
            foreach (var saved in entry.equipped)
            {
                var pieza = FromSaveData(saved);
                if (pieza != null) hero.Equip(pieza);
            }
        }
        else
        {
            MigrateLegacyEquipment(hero, entry);
        }

        // Partidas guardadas antes del arma inicial: se les repone la espada de madera.
        if (gacha != null) gacha.GrantStarterWeapon(hero);
    }

    // Partidas anteriores al equipo instanciado: el equipo eran cuatro nombres de asset sueltos
    // más su desgaste y su nivel en campos aparte. Se reconstruye una instancia por hueco para
    // no perder ni las piezas ni lo que ya costaron.
    private void MigrateLegacyEquipment(HeroController hero, HeroSaveData entry)
    {
        EquipLegacy(hero, entry.weaponAssetName, entry.weaponDurability, entry.weaponGearLevel);
        EquipLegacy(hero, entry.shieldAssetName, entry.shieldDurability, entry.shieldGearLevel);
        EquipLegacy(hero, entry.armorAssetName, entry.armorDurability, entry.armorGearLevel);
        EquipLegacy(hero, entry.accessoryAssetName, entry.accessoryDurability, entry.accessoryGearLevel);
    }

    private void EquipLegacy(HeroController hero, string assetName, int durability, int gearLevel)
    {
        var asset = shop.FindByAssetName(assetName);
        if (asset == null) return;

        var pieza = new EquipmentInstance(asset)
        {
            // 0 de desgaste en el formato viejo significaba "sin estrenar", no "rota".
            durability = durability > 0 ? durability : asset.maxDurability,
            gearLevel = Mathf.Max(0, gearLevel)
        };

        hero.Equip(pieza);
    }

    private void RestoreRoster(GameSaveData save)
    {
        if (gacha == null)
        {
            Debug.LogWarning("[Guardado] Sin GachaManager no se puede reconstruir el roster.", this);
            return;
        }

        // Los héroes que trae la escena sobran: manda el roster guardado.
        var existing = UnityEngine.Object.FindObjectsByType<HeroController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var hero in existing)
        {
            // Desactivar ya no basta: las búsquedas del roster incluyen inactivos a propósito
            // (los héroes de expedición lo están). Se marcan como descartados para que nadie
            // los cuente durante el frame que tardan en desaparecer de verdad.
            hero.MarkDiscarded();
            hero.gameObject.SetActive(false);
            Destroy(hero.gameObject);
        }

        // Se guarda el orden de creación para poder rehacer la escuadra por índice.
        var spawned = new List<HeroController>();

        foreach (var entry in save.heroes)
        {
            var data = gacha.FindByAssetName(entry.heroDataAssetName);
            if (data == null)
            {
                Debug.LogWarning($"[Guardado] {entry.heroDataAssetName} no está en el catálogo; " +
                                 "ese héroe no se puede restaurar.", this);
                continue;
            }

            var hero = gacha.SpawnHero(data, entry.trait, gacha.RandomSpawnPosition());
            if (hero == null) continue;

            hero.LoadInstanceId(entry.heroInstanceId);

            // La ascensión va antes que el nivel: escala las bases sobre las que se calculan los bonus.
            hero.LoadAscension(entry.bonusStarRank, entry.ascensionMultiplier);
            hero.LoadAffinity(entry.affinity);
            hero.LoadGearUpgrade(entry.gearUpgradeAttack, entry.gearUpgradeDefense);

            var passives = new List<PassiveSkill>();
            foreach (int value in entry.passives) passives.Add((PassiveSkill)value);
            hero.SetPassives(passives);

            foreach (var m in entry.mastery)
                hero.Mastery.LoadPoints((WeaponType)m.weaponType, m.points);

            RestoreEquipment(hero, entry);

            // El nivel después: aplica los bonus y deja la vida al máximo, que luego se pisa.
            var progress = hero.GetComponent<HeroProgress>();
            if (progress != null)
            {
                progress.LoadState(entry.level, entry.currentExp);
                progress.LoadTactics(entry.aggression, entry.safeDistance, entry.skillThreshold);
                progress.LoadSkillRefinement(entry.skillRefinement);
            }

            hero.LoadVitals(entry.currentHealth, entry.currentMP, entry.fatigue, entry.morale);
            hero.SetLocked(entry.isLocked);
            hero.SetSubclass((HeroSubclass)entry.subclass);
            RestoreWorkplace(hero, entry.assignedBuilding);
            PlaceOnLoad(hero, entry.assignedBuilding);
            spawned.Add(hero);
        }

        RestoreParty(save, spawned);
        RosterLoaded?.Invoke();
    }

    // Al cargar, todos nacían en el punto del altar y quedaban amontonados. Cada uno vuelve a
    // donde estaba: al hueco de su edificio si trabajaba, o repartido por su zona de deambular.
    private static void PlaceOnLoad(HeroController hero, string buildingId)
    {
        if (hero == null) return;

        if (!string.IsNullOrEmpty(buildingId))
        {
            foreach (var building in BaseBuilding.All)
            {
                if (building == null || building.SaveId != buildingId) continue;

                hero.transform.position = building.ClaimSlot(hero);
                return;
            }
        }

        hero.ScatterInBaseArea();
    }

    // El puesto de trabajo se cotejaba por nombre de GameObject, igual que el nivel del edificio.
    private static void RestoreWorkplace(HeroController hero, string buildingId)
    {
        if (string.IsNullOrEmpty(buildingId)) return;

        foreach (var building in BaseBuilding.All)
        {
            if (building == null || building.SaveId != buildingId) continue;

            building.LoadWorker(hero);
            return;
        }
    }

    // Las escuadras se rehacen por identidad, así que da igual en qué orden se hayan creado.
    private void RestoreParty(GameSaveData save, List<HeroController> spawned)
    {
        if (party == null) return;

        party.LoadParty(save.party, spawned);
        party.LoadExpedition(save.expeditionSquad, spawned);

        for (int i = 0; i < save.presets.Count && i < PartyManager.PresetCount; i++)
            party.LoadPreset(i, save.presets[i].party, save.presets[i].expedition);
    }
}
