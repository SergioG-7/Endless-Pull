using UnityEngine;

// Puesto que ocupa un héroe ahora mismo; solo puede tener uno a la vez.
public enum HeroDuty
{
    Free,
    Building,
    TowerSquad,
    Expedition
}

// Árbitro de exclusividad: edificio, escuadra de torre y escuadra de expedición se excluyen.
public static class HeroAssignment
{
    private static PartyManager party;
    private static ResourceExpeditionManager expeditions;

    // Los managers viven toda la partida; solo se rebusca si el que había murió.
    private static PartyManager Party
        => party != null ? party : (party = Object.FindFirstObjectByType<PartyManager>());

    private static ResourceExpeditionManager Expeditions
        => expeditions != null
           ? expeditions
           : (expeditions = Object.FindFirstObjectByType<ResourceExpeditionManager>());

    public static HeroDuty DutyOf(HeroController hero)
    {
        if (hero == null) return HeroDuty.Free;

        foreach (var building in BaseBuilding.All)
            if (building != null && building.IsWorker(hero)) return HeroDuty.Building;

        var p = Party;
        if (p == null) return HeroDuty.Free;

        if (p.IsInExpedition(hero)) return HeroDuty.Expedition;
        if (p.IsInParty(hero)) return HeroDuty.TowerSquad;

        return HeroDuty.Free;
    }

    // true si el héroe ya ocupa otro puesto distinto del que se le quiere dar.
    public static bool IsBusyElsewhere(HeroController hero, HeroDuty wanted)
    {
        var duty = DutyOf(hero);
        return duty != HeroDuty.Free && duty != wanted;
    }

    // Un héroe de una expedición en curso queda bloqueado hasta que vuelva.
    public static bool IsLockedByExpedition(HeroController hero)
    {
        var e = Expeditions;
        if (e == null || !e.IsRunning) return false;

        var p = Party;
        return p != null && p.IsInExpedition(hero);
    }

    // Nombre del edificio si trabaja en uno; vacío en cualquier otro caso.
    public static string WorkplaceName(HeroController hero)
    {
        if (hero == null) return string.Empty;

        foreach (var building in BaseBuilding.All)
            if (building != null && building.IsWorker(hero)) return building.BuildingName;

        return string.Empty;
    }

    public static string DutyName(HeroDuty duty)
    {
        switch (duty)
        {
            case HeroDuty.Building: return LocalizationManager.Get("UI_DUTY_BUILDING");
            case HeroDuty.TowerSquad: return LocalizationManager.Get("UI_DUTY_TOWER");
            case HeroDuty.Expedition: return LocalizationManager.Get("UI_DUTY_EXPEDITION");
        }
        return LocalizationManager.Get("UI_DUTY_FREE");
    }

    // Aviso listo para la UI cuando se intenta asignar a alguien que ya tiene puesto.
    public static string BusyWarning(HeroController hero)
    {
        var duty = DutyOf(hero);
        string donde = duty == HeroDuty.Building ? WorkplaceName(hero) : DutyName(duty);

        return string.Format(LocalizationManager.Get("UI_ALREADY_ASSIGNED"),
            hero != null && hero.Data != null ? hero.Data.heroName : "?", donde);
    }
}
