using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Toque o clic en el escenario: decide si has tocado un héroe, un edificio o el suelo.
public class WorldInteractionManager : MonoBehaviour
{
    [Tooltip("Cámara con la que se convierte el toque a coordenadas de mundo.")]
    [SerializeField] private Camera worldCamera;

    [Tooltip("Ficha rápida que se abre al tocar un héroe.")]
    [SerializeField] private HeroQuickCardUI heroCard;

    [Tooltip("Ficha del edificio que se abre al tocarlo.")]
    [SerializeField] private BuildingInspectUI buildingCard;

    [Tooltip("Radio de gracia alrededor del héroe, para que el dedo no tenga que ser preciso.")]
    [SerializeField] private float heroTouchRadius = 0.7f;

    [Tooltip("Modal de invocación que abre el Altar.")]
    [SerializeField] private SummonAltarUI summonCard;

    [Tooltip("Radio de toque del Altar de Invocación.")]
    [SerializeField] private float altarTouchRadius = 2f;

    [Tooltip("Panel de Torre que abre el Portal de la base.")]
    [SerializeField] private TowerPanelUI towerCard;

    [Tooltip("Radio de toque del Portal de la Torre.")]
    [SerializeField] private float gatewayTouchRadius = 1.1f;

    void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        if (heroCard == null) heroCard = UnityEngine.Object.FindFirstObjectByType<HeroQuickCardUI>();
        if (buildingCard == null) buildingCard = UnityEngine.Object.FindFirstObjectByType<BuildingInspectUI>();
        if (summonCard == null) summonCard = UnityEngine.Object.FindFirstObjectByType<SummonAltarUI>();
        if (towerCard == null) towerCard = UnityEngine.Object.FindFirstObjectByType<TowerPanelUI>();
    }

void Update()
    {
        if (!TryGetTouchPoint(out Vector2 punto)) return;

        // El héroe manda sobre el edificio: suelen estar uno encima del otro.
        var hero = HeroAt(punto);
        if (hero != null)
        {
            if (heroCard != null) heroCard.Show(hero);
            return;
        }

        // El Altar no es un BaseBuilding: abre su propio modal de invocación.
        if (summonCard != null && SummonAltar.Exists
            && Vector2.Distance(SummonAltar.AltarPosition, punto) <= altarTouchRadius)
        {
            summonCard.Open();
            return;
        }

        // Portal de la Torre: abre la selección de piso/expedición, igual que el botón del sidebar.
        if (towerCard != null && Vector2.Distance(TowerGateway.Position, punto) <= gatewayTouchRadius)
        {
            towerCard.Open();
            return;
        }

        var building = BuildingAt(punto);
        if (building != null)
        {
            if (buildingCard != null) buildingCard.Show(building);
            return;
        }

        // Edificio bloqueado: mismo toast que un cuadrante bloqueado, sin abrir su ficha.
        var lockedBuilding = LockedBuildingAt(punto);
        if (lockedBuilding != null)
        {
            string textoEdificio = string.Format(LocalizationManager.Get("UI_QUADRANT_LOCKED_TAP"), lockedBuilding.RequiredFloor);
            DamageTextManager.Show(punto, textoEdificio, UITheme.TextSoft);
            AudioManager.Play(SfxId.Error);
            return;
        }

        // Zona bloqueada: feedback informativo, sin abrir ningún panel.
        var quadrant = QuadrantAt(punto);
        if (quadrant != null)
        {
            string texto = string.Format(LocalizationManager.Get("UI_QUADRANT_LOCKED_TAP"), quadrant.RequiredFloor);
            DamageTextManager.Show(punto, texto, UITheme.TextSoft);
            AudioManager.Play(SfxId.Error);
        }
    }

    // Un solo punto para ratón y dedo; el proyecto usa solo el Input System nuevo.
    private bool TryGetTouchPoint(out Vector2 world)
    {
        world = Vector2.zero;
        if (worldCamera == null) return false;

        Vector2 pantalla;

        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            pantalla = touch.primaryTouch.position.ReadValue();
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            pantalla = Mouse.current.position.ReadValue();
        else
            return false;

        // Si el toque cae sobre un panel de UI, no es un toque al mundo.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;

        world = worldCamera.ScreenToWorldPoint(pantalla);
        return true;
    }

    private HeroController HeroAt(Vector2 point)
    {
        HeroController mejor = null;
        float mejorDist = heroTouchRadius;

        foreach (var hero in UnityEngine.Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None))
        {
            if (hero == null) continue;

            float d = Vector2.Distance(hero.transform.position, point);
            if (d > mejorDist) continue;

            mejorDist = d;
            mejor = hero;
        }

        return mejor;
    }

    private BaseBuilding BuildingAt(Vector2 point)
    {
        foreach (var building in BaseBuilding.All)
        {
            if (building == null || !building.IsUnlocked) continue;
            if (building.IsInside(point)) return building;
        }

        return null;
    }


    private BaseBuilding LockedBuildingAt(Vector2 point)
    {
        foreach (var building in BaseBuilding.All)
        {
            if (building == null || building.IsUnlocked) continue;
            if (building.IsInside(point)) return building;
        }

        return null;
    }


    private QuadrantController QuadrantAt(Vector2 point)
    {
        foreach (var quadrant in QuadrantController.All)
        {
            if (quadrant == null) continue;
            if (quadrant.ContainsPoint(point)) return quadrant;
        }

        return null;
    }
}
