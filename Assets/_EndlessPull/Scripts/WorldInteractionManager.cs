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

    void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        if (heroCard == null) heroCard = UnityEngine.Object.FindFirstObjectByType<HeroQuickCardUI>();
        if (buildingCard == null) buildingCard = UnityEngine.Object.FindFirstObjectByType<BuildingInspectUI>();
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

        var building = BuildingAt(punto);
        if (building != null && buildingCard != null) buildingCard.Show(building);
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
}
