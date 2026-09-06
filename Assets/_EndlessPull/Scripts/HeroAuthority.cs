using UnityEngine;

// Autoridad del héroe de rango alto: los 7★ llevan parte del poder del Sistema, así que hacen
// por su cuenta tareas que si no tendría que dar el Maestro a mano. Vive en el mismo objeto que
// el HeroController y solo despierta cuando su portador llega al rango.
public class HeroAuthority : MonoBehaviour
{
    [Tooltip("Estrellas que hacen falta para tener autoridad propia.")]
    [Range(1, 7)]
    [SerializeField] private int minStarRank = 7;

    [Tooltip("Segundos entre dos decisiones propias.")]
    [SerializeField] private float interval = 20f;

    [Tooltip("Fatiga a partir de la cual manda a descansar a un compañero.")]
    [Range(0f, 100f)]
    [SerializeField] private float fatigueThreshold = 70f;

    [Tooltip("Por debajo de esta reserva de madera o hierro se considera que falta material.")]
    [SerializeField] private int materialThreshold = 200;

    [Tooltip("Por debajo de esta reserva de comida se considera que falta comida.")]
    [SerializeField] private int foodThreshold = 250;

    [Tooltip("Segundos que deja pasar tras una recolección antes de mandar otra.")]
    [SerializeField] private float expeditionCooldown = 180f;

    [Tooltip("Héroes que manda a recolectar como mucho.")]
    [Min(1)]
    [SerializeField] private int expeditionParty = 3;

    private HeroController hero;
    private float timer;
    private float expeditionTimer;

    private ResourceExpeditionManager expeditions;
    private PartyManager party;
    private EconomyManager economy;

    // Se dispara con (quien manda, texto ya localizado) cada vez que actúa por su cuenta.
    public static event System.Action<HeroController, string> Acted;

    public bool HasAuthority => hero != null
                                && hero.StarRank >= minStarRank
                                && !hero.Discarded
                                && hero.GlobalState == HeroGlobalState.InBase;

    void Awake()
    {
        hero = GetComponent<HeroController>();
        expeditions = UnityEngine.Object.FindFirstObjectByType<ResourceExpeditionManager>();
        party = UnityEngine.Object.FindFirstObjectByType<PartyManager>();
        economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
    }

    void Update()
    {
        if (!HasAuthority) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = interval;
        if (expeditionTimer > 0f) expeditionTimer -= interval;

        // Una sola decisión por turno, de más urgente a menos: recoger lo que ya está listo,
        // reponer material que falta, mandar a descansar y, por último, repartir trabajo.
        if (ClaimFinishedExpedition()) return;
        if (SendGatheringParty()) return;
        if (SendSomeoneToRest()) return;
        AssignSomeoneToWork();
    }

    // Una recolección terminada no se cobra sola: si el Maestro no está, la cobra el 7★.
    private bool ClaimFinishedExpedition()
    {
        if (expeditions == null || !expeditions.ReadyToClaim) return false;
        if (!expeditions.ClaimReward()) return false;

        expeditionTimer = expeditionCooldown;
        Announce("UI_AUTHORITY_CLAIM");
        return true;
    }

    // Manda una cuadrilla a por lo que más escasee, si hace rato que no sale ninguna.
    private bool SendGatheringParty()
    {
        if (expeditions == null || party == null || economy == null) return false;
        if (expeditions.IsRunning || expeditionTimer > 0f) return false;

        if (!FaltaAlgo(out ResourceExpeditionType destino)) return false;

        // La escuadra de recolección se arma con los ociosos: nadie sale del combate ni del
        // descanso para ir a talar.
        int enviados = 0;
        foreach (var otro in Roster())
        {
            if (enviados >= Mathf.Min(expeditionParty, party.MaxExpeditionSize)) break;
            if (otro == hero || otro.RestOrderPending || otro.Fatigue >= fatigueThreshold) continue;
            if (party.IsInExpedition(otro)) { enviados++; continue; }

            if (party.ToggleExpedition(otro)) enviados++;
        }

        if (enviados == 0 || !expeditions.StartExpedition(destino)) return false;

        expeditionTimer = expeditionCooldown;
        Announce("UI_AUTHORITY_GATHER", enviados,
                 ResourceExpeditionManager.DisplayName(destino));
        return true;
    }

    // Qué recurso está más bajo; sin ninguno por debajo del umbral no se manda a nadie.
    private bool FaltaAlgo(out ResourceExpeditionType destino)
    {
        destino = ResourceExpeditionType.Forest;

        int madera = economy.Wood, hierro = economy.Iron, comida = economy.Food;
        bool faltaMadera = madera < materialThreshold;
        bool faltaHierro = hierro < materialThreshold;
        bool faltaComida = comida < foodThreshold;

        if (!faltaMadera && !faltaHierro && !faltaComida) return false;

        // El más escaso en proporción a su propio umbral manda sobre el resto.
        float ratioMadera = faltaMadera ? madera / (float)materialThreshold : 99f;
        float ratioHierro = faltaHierro ? hierro / (float)materialThreshold : 99f;
        float ratioComida = faltaComida ? comida / (float)foodThreshold : 99f;

        if (ratioComida <= ratioMadera && ratioComida <= ratioHierro)
            destino = ResourceExpeditionType.Hunt;
        else if (ratioHierro <= ratioMadera)
            destino = ResourceExpeditionType.Mine;
        else
            destino = ResourceExpeditionType.Forest;

        return true;
    }

    // Manda a descansar al compañero más agotado que siga en pie sin orden previa.
    private bool SendSomeoneToRest()
    {
        HeroController peor = null;

        foreach (var otro in Roster())
        {
            if (otro == hero || otro.Fatigue < fatigueThreshold || otro.RestOrderPending) continue;
            if (peor == null || otro.Fatigue > peor.Fatigue) peor = otro;
        }

        if (peor == null || !peor.SendToRest()) return false;

        Announce("UI_AUTHORITY_REST", peor.Data.heroName);
        return true;
    }

    // Pone a currar a un héroe ocioso en el edificio productor con hueco más cercano a él.
    private bool AssignSomeoneToWork()
    {
        foreach (var otro in Roster())
        {
            if (otro == hero || otro.AssignedBuilding != null || otro.RestOrderPending) continue;
            if (otro.Fatigue >= fatigueThreshold) continue;

            var destino = NearestOpenBuilding(otro);
            if (destino == null || !destino.ToggleWorker(otro)) continue;

            Announce("UI_AUTHORITY_WORK", otro.Data.heroName,
                     destino.DisplayName);
            return true;
        }

        return false;
    }

    private BaseBuilding NearestOpenBuilding(HeroController quien)
    {
        BaseBuilding mejor = null;
        float mejorDistancia = float.MaxValue;

        foreach (var b in BaseBuilding.All)
        {
            if (b == null || !b.IsUnlocked || !b.IsProducer) continue;
            if (b.Workers.Count >= b.Capacity) continue;

            float distancia = Vector2.Distance(quien.transform.position, b.transform.position);
            if (distancia >= mejorDistancia) continue;

            mejorDistancia = distancia;
            mejor = b;
        }

        return mejor;
    }

    // Compañeros en la base sobre los que puede mandar; los desplegados y los de expedición no.
    private static System.Collections.Generic.IEnumerable<HeroController> Roster()
    {
        foreach (var otro in UnityEngine.Object.FindObjectsByType<HeroController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (otro == null || otro.Data == null || otro.Discarded) continue;
            if (otro.IsDeployed || otro.GlobalState != HeroGlobalState.InBase) continue;

            yield return otro;
        }
    }

    private void Announce(string clave, params object[] argumentos)
    {
        var todos = new object[argumentos.Length + 1];
        todos[0] = hero.Data.heroName;
        System.Array.Copy(argumentos, 0, todos, 1, argumentos.Length);

        string texto = string.Format(LocalizationManager.Get(clave), todos);
        hero.Bark(1f, "AUTHORITY_ORDER_1", "AUTHORITY_ORDER_2", "AUTHORITY_ORDER_3");
        ScreenBanner.ShowCompact(texto, 2.5f, UITheme.Accent);
        Acted?.Invoke(hero, texto);
        Debug.Log("[Autoridad] " + texto, this);
    }
}
