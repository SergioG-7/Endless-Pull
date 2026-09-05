using TMPro;
using System.Collections.Generic;
using UnityEngine;

// Bocadillo flotante de los héroes en la base; lo que dicen depende de cómo estén.
public class SpeechBubble : MonoBehaviour
{
    [Tooltip("Segundos mínimos y máximos entre comentarios.")]
    [SerializeField] private Vector2 intervalRange = new Vector2(8f, 20f);

    [Tooltip("Segundos que se queda el bocadillo en pantalla.")]
    [SerializeField] private float showSeconds = 3f;

    [Tooltip("Altura del bocadillo sobre la cabeza del héroe.")]
    [SerializeField] private float height = 1.5f;

    [Tooltip("Tamaño de letra del bocadillo.")]
    [SerializeField] private float fontSize = 1.1f;

    [Tooltip("Ancho y alto de la píldora del bocadillo, en unidades de mundo.")]
    [SerializeField] private Vector2 bubbleSize = new Vector2(4.4f, 0.85f);

    [Tooltip("Comida por debajo de la cual los héroes empiezan a quejarse.")]
    [SerializeField] private int lowFoodThreshold = 120;

    private HeroController hero;
    private EconomyManager economy;
    private TextMeshPro label;
    private SpriteRenderer backdrop;
    private float nextTimer;
    private float hideTimer;

    void Awake()
    {
        hero = GetComponent<HeroController>();
        economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        nextTimer = Random.Range(intervalRange.x, intervalRange.y);
    }

    void Update()
    {
        if (hero == null || hero.Data == null) return;

        if (hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f && label != null) label.gameObject.SetActive(false);
            return;
        }

        // En combate nadie está para charlas.
        if (hero.IsDeployed) return;

        nextTimer -= Time.deltaTime;
        if (nextTimer > 0f) return;

        nextTimer = Random.Range(intervalRange.x, intervalRange.y);
        Say(PickLine());
    }

    public void Say(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (label == null) Build();

        label.text = text;
        label.gameObject.SetActive(true);
        hideTimer = showSeconds;
    }

    // Lo que dice sale de un sorteo entre TODO lo que le viene a cuento, no de la primera
    // condición que se cumpla: la cascada rígida de antes hacía que un héroe hambriento dijera
    // siempre lo mismo y no llegara a contar nunca dónde está ni quién es.
    private readonly List<string> candidatas = new List<string>();

    private string PickLine()
    {
        candidatas.Clear();

        // Urgencias: pesan más porque se añaden varias veces al sorteo.
        if (economy != null && economy.Food < lowFoodThreshold)
            Añadir(3, "SAY_HUNGRY_1", "SAY_HUNGRY_2", "SAY_HUNGRY_3", "SAY_HUNGRY_4", "SAY_HUNGRY_5");

        if (hero.IsExhausted)
            Añadir(3, "SAY_TIRED_1", "SAY_TIRED_2", "SAY_TIRED_3", "SAY_TIRED_4", "SAY_TIRED_5");

        if (hero.IsDemoralized || hero.MoralePercent < 30)
            Añadir(3, "SAY_LOWMORALE_1", "SAY_LOWMORALE_2", "SAY_LOWMORALE_3",
                      "SAY_LOWMORALE_4", "SAY_LOWMORALE_5");

        if (hero.MaxHealth > 0 && hero.CurrentHealth < hero.MaxHealth / 2)
            Añadir(3, "SAY_HURT_1", "SAY_HURT_2", "SAY_HURT_3", "SAY_HURT_4", "SAY_HURT_5");

        if (hero.HasBrokenGear)
            Añadir(3, "SAY_BROKEN_GEAR_1", "SAY_BROKEN_GEAR_2", "SAY_BROKEN_GEAR_3", "SAY_BROKEN_GEAR_4");

        // En el claro de recolección solo habla de eso: no está en la base.
        if (hero.GlobalState == HeroGlobalState.OnExpedition)
        {
            Añadir(4, "SAY_GATHERING_1", "SAY_GATHERING_2", "SAY_GATHERING_3",
                      "SAY_GATHERING_4", "SAY_GATHERING_5");
            return Sortear();
        }

        // Dónde está currando: es lo que más ancla al héroe en el mundo.
        AñadirPorEdificio();

        // Quién es: rareza, pericia y lo que te tiene cogido.
        if (hero.StarRank >= 5)
            Añadir(2, "SAY_PROUD_1", "SAY_PROUD_2", "SAY_PROUD_3");
        else if (hero.StarRank <= 2)
            Añadir(2, "SAY_ROOKIE_1", "SAY_ROOKIE_2", "SAY_ROOKIE_3");

        if (hero.EquippedWeaponType != WeaponType.None
            && hero.Mastery.RankOf(hero.EquippedWeaponType) >= MasteryRank.A)
            Añadir(2, "SAY_MASTERY_1", "SAY_MASTERY_2");

        if (hero.AffinityAtkBonus > 0f)
            Añadir(2, "SAY_AFFINITY_1", "SAY_AFFINITY_2", "SAY_AFFINITY_3");

        // La Galería pesa sobre los vivos.
        if (MemorialManager.LostCount > 0)
            Añadir(1, "SAY_MOURNING_1", "SAY_MOURNING_2", "SAY_MOURNING_3");

        if (hero.MoralePercent > 80)
            Añadir(2, "SAY_HAPPY_1", "SAY_HAPPY_2", "SAY_HAPPY_3", "SAY_HAPPY_4", "SAY_HAPPY_5");

        switch (hero.Trait)
        {
            case HeroTrait.Glutton: Añadir(2, "SAY_GLUTTON", "SAY_GLUTTON_2", "SAY_GLUTTON_3"); break;
            case HeroTrait.Slacker: Añadir(2, "SAY_SLACKER", "SAY_SLACKER_2", "SAY_SLACKER_3"); break;
            case HeroTrait.Fierce:  Añadir(2, "SAY_FIERCE", "SAY_FIERCE_2", "SAY_FIERCE_3"); break;
            default: Añadir(2, "SAY_DILIGENT_1", "SAY_DILIGENT_2", "SAY_DILIGENT_3"); break;
        }

        // Relleno: siempre hay algo que decir aunque no pase nada.
        Añadir(1, "SAY_IDLE_1", "SAY_IDLE_2", "SAY_IDLE_3", "SAY_IDLE_4", "SAY_IDLE_5", "SAY_IDLE_6");

        return Sortear();
    }

    private void AñadirPorEdificio()
    {
        var edificio = hero.CurrentBuilding != null ? hero.CurrentBuilding : hero.AssignedBuilding;
        if (edificio == null) return;

        switch (edificio.Type)
        {
            case BuildingType.TrainingDummy: Añadir(3, "SAY_AT_TRAINING_1", "SAY_AT_TRAINING_2"); break;
            case BuildingType.Canteen: Añadir(3, "SAY_AT_CANTEEN_1", "SAY_AT_CANTEEN_2"); break;
            case BuildingType.Forge: Añadir(3, "SAY_AT_FORGE_1", "SAY_AT_FORGE_2"); break;
            case BuildingType.Farm: Añadir(3, "SAY_AT_FARM_1", "SAY_AT_FARM_2"); break;
            case BuildingType.Lodging: Añadir(3, "SAY_AT_LODGING_1", "SAY_AT_LODGING_2"); break;
            case BuildingType.ManaWell: Añadir(3, "SAY_AT_MANAWELL_1", "SAY_AT_MANAWELL_2"); break;
            case BuildingType.Workshop: Añadir(3, "SAY_AT_WORKSHOP_1", "SAY_AT_WORKSHOP_2"); break;
            case BuildingType.Archive: Añadir(3, "SAY_AT_ARCHIVE_1", "SAY_AT_ARCHIVE_2"); break;
            case BuildingType.WarRoom: Añadir(3, "SAY_AT_WARROOM_1", "SAY_AT_WARROOM_2"); break;
            case BuildingType.WoodworkingShop: Añadir(3, "SAY_AT_WOODWORKING_1", "SAY_AT_WOODWORKING_2"); break;
            case BuildingType.MetalProcessing: Añadir(3, "SAY_AT_METALWORKS_1", "SAY_AT_METALWORKS_2"); break;
        }
    }

    // Repetir la clave es lo que le da peso: cuanto más veces entra, más probable sale.
    private void Añadir(int peso, params string[] keys)
    {
        for (int p = 0; p < peso; p++)
            for (int i = 0; i < keys.Length; i++) candidatas.Add(keys[i]);
    }

    private string Sortear()
    {
        if (candidatas.Count == 0) return LocalizationManager.Get("SAY_IDLE_1");

        // No repetir la última: con el mismo héroe hablando cada 8-20 s, oírla dos veces
        // seguidas se nota mucho más que la falta de variedad.
        for (int intento = 0; intento < 4; intento++)
        {
            string key = candidatas[Random.Range(0, candidatas.Count)];
            if (key != ultimaClave || candidatas.Count == 1)
            {
                ultimaClave = key;
                return LocalizationManager.Get(key);
            }
        }

        return LocalizationManager.Get(ultimaClave);
    }

    private string ultimaClave = string.Empty;

    private static string Pick(params string[] keys)
        => LocalizationManager.Get(keys[Random.Range(0, keys.Length)]);

    private void Build()
    {
        var go = new GameObject("SpeechBubble", typeof(TextMeshPro));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, height, 0f);

        // Fondo del bocadillo: sin él el texto se pierde sobre el suelo oscuro.
        var fondoGo = new GameObject("Backdrop", typeof(SpriteRenderer));
        fondoGo.transform.SetParent(go.transform, false);
        fondoGo.transform.localPosition = Vector3.zero;

        backdrop = fondoGo.GetComponent<SpriteRenderer>();
        backdrop.sprite = Resources.Load<Sprite>("UI/UI_Rounded");
        backdrop.drawMode = SpriteDrawMode.Sliced;
        backdrop.size = new Vector2(bubbleSize.x, bubbleSize.y);
        backdrop.color = UITheme.Hex("141A26", 0.92f);
        backdrop.sortingOrder = 19;

        var marcoGo = new GameObject("Border", typeof(SpriteRenderer));
        marcoGo.transform.SetParent(go.transform, false);
        marcoGo.transform.localPosition = Vector3.zero;

        var marco = marcoGo.GetComponent<SpriteRenderer>();
        marco.sprite = Resources.Load<Sprite>("UI/UI_RoundedRingThin");
        marco.drawMode = SpriteDrawMode.Sliced;
        marco.size = new Vector2(bubbleSize.x, bubbleSize.y);
        marco.color = UITheme.Border;
        marco.sortingOrder = 20;

        label = go.GetComponent<TextMeshPro>();
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = UITheme.Text;
        label.sortingOrder = 21;

        // Sin ancho fijo el texto largo se sale del sprite del héroe.
        label.rectTransform.sizeDelta = new Vector2(bubbleSize.x - 0.3f, bubbleSize.y);
        go.SetActive(false);
    }
}
