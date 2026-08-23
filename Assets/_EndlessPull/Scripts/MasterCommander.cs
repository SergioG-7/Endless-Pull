using UnityEngine;
using UnityEngine.InputSystem;

// El Maestro: intervenciones puntuales del jugador sobre los héroes en escena.
public class MasterCommander : MonoBehaviour
{
    [Tooltip("Vida que restaura la curación rápida de Espacio.")]
    [SerializeField] private int quickHealAmount = 25;

    void Update()
    {
        // El proyecto usa solo el nuevo Input System, de ahí Keyboard.current.
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            HealAllHeroes(quickHealAmount);
        }
    }

    // Cura a todos los héroes vivos de la escena.
    public void HealAllHeroes(int amount)
    {
        var heroes = Object.FindObjectsByType<HeroController>(FindObjectsSortMode.None);
        int totalHealed = 0;

        foreach (var hero in heroes)
        {
            totalHealed += HealHero(hero, amount);
        }

        Debug.Log($"[Maestro] Curación en masa: +{totalHealed} PV repartidos entre {heroes.Length} héroe(s).", this);
    }

    // Curación dirigida a un héroe concreto; devuelve la vida realmente restaurada.
    public int HealHero(HeroController hero, int amount)
    {
        if (hero == null) return 0;
        return hero.Heal(amount);
    }
}
