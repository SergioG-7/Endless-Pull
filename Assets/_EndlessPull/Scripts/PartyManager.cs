using System.Collections.Generic;
using UnityEngine;

// Escuadra de asalto: quién sube a la torre y cuántos intentos quedan.
public class PartyManager : MonoBehaviour
{
    [Tooltip("Héroes que caben en la escuadra.")]
    [SerializeField] private int maxPartySize = 4;

    [Tooltip("Intentos de torre disponibles al empezar.")]
    [SerializeField] private int maxEnergy = 5;

    [Tooltip("Segundos que tarda en recargarse un intento.")]
    [SerializeField] private float energyRechargeSeconds = 300f;

    [Tooltip("Gemas que cuesta rellenar los intentos de torre al máximo.")]
    [SerializeField] private int energyRefillCost = 100;

    [Tooltip("Puestos de la formación, del slot 1 al 4; evitan que la escuadra se apile.")]
    [SerializeField] private Vector2[] formationSlots =
    {
        new Vector2(3.5f, 0f),
        new Vector2(2.5f, 0.8f),
        new Vector2(1.8f, -0.8f),
        new Vector2(1.0f, 0f)
    };

    [Tooltip("Economía de la que sale el pago de las recargas.")]
    [SerializeField] private EconomyManager economy;

    private readonly List<HeroController> party = new List<HeroController>();
    private int energy;
    private float rechargeTimer;

    public IReadOnlyList<HeroController> Party => party;
    public int MaxPartySize => maxPartySize;
    public int Energy => energy;
    public int MaxEnergy => maxEnergy;
    public int EnergyRefillCost => energyRefillCost;
    public float SecondsToNextEnergy => energy >= maxEnergy ? 0f : rechargeTimer;

    // Se dispara cuando cambia la escuadra o la energía.
    public event System.Action PartyChanged;

    void Awake()
    {
        if (economy == null) economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        energy = maxEnergy;
        rechargeTimer = energyRechargeSeconds;
    }

    void Update()
    {
        PruneParty();

        if (energy >= maxEnergy) return;

        rechargeTimer -= Time.deltaTime;
        if (rechargeTimer > 0f) return;

        rechargeTimer = energyRechargeSeconds;
        energy++;
        PartyChanged?.Invoke();
    }

    // Puesto que le toca al héroe según su orden en la escuadra.
    public Vector2 FormationSlot(int slotIndex)
    {
        if (formationSlots == null || formationSlots.Length == 0) return Vector2.zero;

        return formationSlots[Mathf.Clamp(slotIndex, 0, formationSlots.Length - 1)];
    }

    public bool IsInParty(HeroController hero) => hero != null && party.Contains(hero);
    public bool IsFull => party.Count >= maxPartySize;

    // Mete o saca al héroe de la escuadra; devuelve true si se quedó dentro.
    public bool Toggle(HeroController hero)
    {
        if (hero == null) return false;

        if (party.Remove(hero))
        {
            PartyChanged?.Invoke();
            return false;
        }

        if (IsFull)
        {
            Debug.LogWarning($"[Escuadra] Ya hay {maxPartySize} héroes asignados.", this);
            return false;
        }

        party.Add(hero);
        PartyChanged?.Invoke();
        return true;
    }

    public void Clear()
    {
        party.Clear();
        PartyChanged?.Invoke();
    }

    // Gasta un intento de torre; el WaveManager la llama antes de montar la oleada.
    public bool TryConsumeEnergy()
    {
        if (energy <= 0)
        {
            Debug.LogWarning("[Escuadra] Sin intentos de torre. Espera la recarga o paga gemas.", this);
            return false;
        }

        // El contador de recarga arranca al gastar el primer intento del tope.
        if (energy == maxEnergy) rechargeTimer = energyRechargeSeconds;

        energy--;
        PartyChanged?.Invoke();
        return true;
    }

    // Rellena los intentos hasta el tope de una vez; no compensa pagar por uno solo.
    public bool TryRefillEnergyWithGems()
    {
        if (energy >= maxEnergy)
        {
            Debug.LogWarning("[Escuadra] Los intentos de torre ya están al máximo.", this);
            return false;
        }

        if (economy == null || !economy.TrySpend(energyRefillCost))
        {
            int saldo = economy != null ? economy.Gems : 0;
            Debug.LogWarning($"[Escuadra] Recarga bloqueada: cuesta {energyRefillCost} y hay {saldo} gemas.", this);
            return false;
        }

        energy = maxEnergy;
        rechargeTimer = energyRechargeSeconds;
        PartyChanged?.Invoke();

        Debug.Log($"[Escuadra] Intentos recargados a {energy}/{maxEnergy} por {energyRefillCost} gemas.", this);
        SaveManager.RequestSave();
        return true;
    }

    // Ids de la escuadra, para guardarla sin depender del orden del array.
    public List<string> PartyInstanceIds()
    {
        var ids = new List<string>();
        foreach (var hero in party)
            if (hero != null) ids.Add(hero.HeroInstanceId);

        return ids;
    }

    // Rehace la escuadra buscando por identidad entre los héroes que haya en escena.
    public void LoadParty(IList<string> ids, IList<HeroController> pool)
    {
        party.Clear();
        if (ids == null || pool == null) return;

        foreach (string id in ids)
        {
            foreach (var hero in pool)
            {
                if (hero == null || hero.HeroInstanceId != id) continue;

                if (!party.Contains(hero) && !IsFull) party.Add(hero);
                break;
            }
        }

        PartyChanged?.Invoke();
    }

    // La usa el SaveManager al cargar.
    public void LoadEnergy(int savedEnergy)
    {
        energy = Mathf.Clamp(savedEnergy, 0, maxEnergy);
        rechargeTimer = energyRechargeSeconds;
        PartyChanged?.Invoke();
    }

    // Los muertos y los sacrificados no pueden seguir apuntados.
    private void PruneParty()
    {
        bool changed = false;
        for (int i = party.Count - 1; i >= 0; i--)
        {
            if (party[i] != null) continue;

            party.RemoveAt(i);
            changed = true;
        }

        if (changed) PartyChanged?.Invoke();
    }
}
