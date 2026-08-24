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

    // Los enums van como int: es lo único que JsonUtility garantiza dentro de una lista.
    public List<int> passives = new List<int>();
    public List<MasterySaveData> mastery = new List<MasterySaveData>();

    public int bonusStarRank;
    public float ascensionMultiplier = 1f;

    public string weaponAssetName;
    public string shieldAssetName;
    public string armorAssetName;
    public string accessoryAssetName;

    // Durabilidad por hueco: el EquipmentData es compartido y no puede guardarla.
    public int weaponDurability;
    public int shieldDurability;
    public int armorDurability;
    public int accessoryDurability;
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
    public int gems;
    public int wood;
    public int iron;
    public int food;
    public int currentFloor = 1;
    public int highestClearedFloor;
    public int ascensionStones;
    public int expeditionEnergy = 5;

    // Identidad de cada héroe de la escuadra; no depende del orden del array.
    public List<string> party = new List<string>();
    public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
    public List<HeroSaveData> heroes = new List<HeroSaveData>();

    // Piezas que no lleva nadie puesto, por nombre de asset.
    public List<string> inventory = new List<string>();
}

// Guarda y restaura la partida en JSON dentro de Application.persistentDataPath.
public class SaveManager : MonoBehaviour
{
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

    [Tooltip("Escribe el JSON indentado para poder leerlo a mano.")]
    [SerializeField] private bool prettyPrint = true;

    [Tooltip("Carga sola al arrancar; se apaga cuando el menú principal decide qué partida abrir.")]
    [SerializeField] private bool loadOnStart = true;

    private static SaveManager instance;

    public string SavePath => Path.Combine(Application.persistentDataPath, fileName);
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
        if (MainMenuUI.IsShowing)
        {
            Debug.Log("[Guardado] Ignorado: el menu principal sigue abierto.", this);
            return;
        }

        var save = new GameSaveData();

        if (economy != null)
        {
            save.gems = economy.Gems;
            save.wood = economy.Wood;
            save.iron = economy.Iron;
            save.food = economy.Food;
        }

        if (crafting != null) save.ascensionStones = crafting.AscensionStones;
        if (party != null) save.expeditionEnergy = party.Energy;

        if (waves != null)
        {
            save.currentFloor = waves.CurrentFloor;
            save.highestClearedFloor = waves.HighestClearedFloor;
        }

        foreach (var building in BaseBuilding.All)
        {
            if (building == null) continue;
            save.buildings.Add(new BuildingSaveData
            {
                buildingId = building.SaveId,
                level = building.Level
            });
        }

        var heroes = UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None);
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
                assignedBuilding = hero.AssignedBuilding != null ? hero.AssignedBuilding.SaveId : string.Empty,
                shieldAssetName = AssetNameOf(hero.Shield),
                weaponDurability = hero.DurabilityOf(EquipmentSlot.Weapon),
                shieldDurability = hero.DurabilityOf(EquipmentSlot.Shield),
                armorDurability = hero.DurabilityOf(EquipmentSlot.Armor),
                accessoryDurability = hero.DurabilityOf(EquipmentSlot.Accessory),
                bonusStarRank = hero.BonusStarRank,
                ascensionMultiplier = hero.AscensionMultiplier,
                weaponAssetName = AssetNameOf(hero.Weapon),
                armorAssetName = AssetNameOf(hero.Armor),
                accessoryAssetName = AssetNameOf(hero.Accessory)
            };

            foreach (var passive in hero.Passives) entry.passives.Add((int)passive);

            foreach (var pair in hero.Mastery.AllPoints)
                entry.mastery.Add(new MasterySaveData { weaponType = (int)pair.Key, points = pair.Value });

            save.heroes.Add(entry);
        }

        if (shop != null)
            foreach (var item in shop.Inventory) save.inventory.Add(item.name);

        if (party != null) save.party.AddRange(party.PartyInstanceIds());

        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(save, prettyPrint));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Guardado] No se pudo escribir {SavePath}: {e.Message}", this);
            return;
        }

        Debug.Log($"[Guardado] {save.heroes.Count} héroe(s), piso {save.currentFloor}, " +
                  $"{save.gems} gemas, {save.wood}M/{save.iron}H/{save.food}C, " +
                  $"{save.ascensionStones} piedra(s) -> {SavePath}", this);
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

        if (waves != null) waves.LoadProgress(save.currentFloor, save.highestClearedFloor);
        if (crafting != null) crafting.LoadStones(save.ascensionStones);
        if (party != null) party.LoadEnergy(save.expeditionEnergy);

        RestoreBuildings(save);
        RestoreInventory(save);
        RestoreRoster(save);

        Debug.Log($"[Guardado] Partida cargada: {save.heroes.Count} héroe(s), piso {save.currentFloor}, " +
                  $"{save.gems} gemas, {save.wood}M/{save.iron}H.", this);
    }

    // Borra el fichero; útil para empezar de cero sin tocar la escena.
    public void DeleteSave()
    {
        if (!HasSave) return;

        File.Delete(SavePath);
        Debug.Log($"[Guardado] Partida borrada: {SavePath}", this);
    }

    private static string AssetNameOf(EquipmentData item) => item != null ? item.name : string.Empty;

    // El inventario se rehace antes que el roster: equipar saca piezas de él.
    private void RestoreInventory(GameSaveData save)
    {
        if (shop == null) return;

        shop.ClearInventory();
        foreach (var assetName in save.inventory)
        {
            var item = shop.FindByAssetName(assetName);
            if (item != null) shop.AddToInventory(item);
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
    private void RestoreEquipment(HeroController hero, HeroSaveData entry)
    {
        if (shop == null) return;

        EquipOne(hero, entry.weaponAssetName);
        EquipOne(hero, entry.shieldAssetName);
        EquipOne(hero, entry.armorAssetName);
        EquipOne(hero, entry.accessoryAssetName);

        // La durabilidad se escribe después de equipar: Equip deja la pieza entera por defecto.
        Restore(hero, EquipmentSlot.Weapon, entry.weaponDurability);
        Restore(hero, EquipmentSlot.Shield, entry.shieldDurability);
        Restore(hero, EquipmentSlot.Armor, entry.armorDurability);
        Restore(hero, EquipmentSlot.Accessory, entry.accessoryDurability);
    }

    // Las partidas anteriores al desgaste traen 0; sin esto todo saldría roto de golpe.
    private static void Restore(HeroController hero, EquipmentSlot slot, int saved)
    {
        var item = hero.GetEquipped(slot);
        if (item == null) return;

        hero.SetDurability(slot, saved > 0 ? saved : item.maxDurability);
    }

    private void EquipOne(HeroController hero, string assetName)
    {
        var item = shop.FindByAssetName(assetName);
        if (item == null) return;

        hero.Equip(item);
    }

    private void RestoreRoster(GameSaveData save)
    {
        if (gacha == null)
        {
            Debug.LogWarning("[Guardado] Sin GachaManager no se puede reconstruir el roster.", this);
            return;
        }

        // Los héroes que trae la escena sobran: manda el roster guardado.
        var existing = UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None);
        foreach (var hero in existing)
        {
            // Se desactiva antes de destruir para que FindObjectsByType deje de verlo ya en este frame.
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

            var passives = new List<PassiveSkill>();
            foreach (int value in entry.passives) passives.Add((PassiveSkill)value);
            hero.SetPassives(passives);

            foreach (var m in entry.mastery)
                hero.Mastery.LoadPoints((WeaponType)m.weaponType, m.points);

            RestoreEquipment(hero, entry);

            // El nivel después: aplica los bonus y deja la vida al máximo, que luego se pisa.
            var progress = hero.GetComponent<HeroProgress>();
            if (progress != null) progress.LoadState(entry.level, entry.currentExp);

            hero.LoadVitals(entry.currentHealth, entry.currentMP, entry.fatigue, entry.morale);
            hero.SetLocked(entry.isLocked);
            hero.SetSubclass((HeroSubclass)entry.subclass);
            RestoreWorkplace(hero, entry.assignedBuilding);
            spawned.Add(hero);
        }

        RestoreParty(save, spawned);
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

    // La escuadra se rehace por identidad, así que da igual en qué orden se hayan creado.
    private void RestoreParty(GameSaveData save, List<HeroController> spawned)
    {
        if (party == null) return;

        party.LoadParty(save.party, spawned);
    }
}
