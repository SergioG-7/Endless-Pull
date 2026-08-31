using NUnit.Framework;
using UnityEngine;

namespace EndlessPull.Tests.EditMode.Commander
{
    // Story type: Logic (formula) — Fase 29, "Balance de Combate y Decretos".
    // Verifica MasterCommander.HealHero()/HealAllHeroes() tras el paso de +25 PV
    // plano a 8% de vida maxima por heroe (healPercent).
    // Evidencia: BLOCKING. Ubicacion logica pedida: tests/unit/commander/ — movido
    // a Assets/ porque Unity no compila scripts fuera de esa carpeta (ver reporte QA).
    public class MasterCommanderHeal_Test
    {
        // Debe coincidir con el default de MasterCommander.healPercent.
        private const float HealPercent = 0.08f;

        private MasterCommander _commander;
        private HeroController _lowHealthHero;
        private HeroController _highHealthHero;
        private HeroData _lowHealthData;
        private HeroData _highHealthData;

        [SetUp]
        public void SetUp()
        {
            _commander = new GameObject("MasterCommander_Test").AddComponent<MasterCommander>();

            _lowHealthData = ScriptableObject.CreateInstance<HeroData>();
            _lowHealthData.maxHealth = 100; // heroe 1 estrella, sin ascender (ejemplo del GDD)

            _highHealthData = ScriptableObject.CreateInstance<HeroData>();
            _highHealthData.maxHealth = 1200; // heroe 5 estrellas ascendido (ejemplo del GDD)

            _lowHealthHero = new GameObject("Hero_1Star_Test").AddComponent<HeroController>();
            _lowHealthHero.Initialize(_lowHealthData, HeroTrait.Diligent, Vector2.zero, Vector2.one);

            _highHealthHero = new GameObject("Hero_5Star_Test").AddComponent<HeroController>();
            _highHealthHero.Initialize(_highHealthData, HeroTrait.Diligent, Vector2.zero, Vector2.one);

            // Deja margen de sobra (mitad de la vida) para que la curacion nunca choque
            // con el techo de MaxHealth; ignora defensa para que el dano sea exacto y determinista.
            _lowHealthHero.TakeDamage(Mathf.RoundToInt(_lowHealthHero.MaxHealth * 0.5f), true);
            _highHealthHero.TakeDamage(Mathf.RoundToInt(_highHealthHero.MaxHealth * 0.5f), true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_commander != null) Object.DestroyImmediate(_commander.gameObject);
            if (_lowHealthHero != null) Object.DestroyImmediate(_lowHealthHero.gameObject);
            if (_highHealthHero != null) Object.DestroyImmediate(_highHealthHero.gameObject);
            if (_lowHealthData != null) Object.DestroyImmediate(_lowHealthData);
            if (_highHealthData != null) Object.DestroyImmediate(_highHealthData);
        }

        [Test]
        public void test_heal_hero_low_max_health_heals_proportionally_less()
        {
            int healed = _commander.HealHero(_lowHealthHero);

            Assert.That(healed, Is.EqualTo(Mathf.RoundToInt(HealPercent * _lowHealthHero.MaxHealth)));
            Assert.That(healed, Is.EqualTo(8), "round(8% * 100) debe dar 8 PV, igual que el ejemplo del GDD.");
        }

        [Test]
        public void test_heal_hero_high_max_health_heals_proportionally_more()
        {
            int healed = _commander.HealHero(_highHealthHero);

            Assert.That(healed, Is.EqualTo(Mathf.RoundToInt(HealPercent * _highHealthHero.MaxHealth)));
            Assert.That(healed, Is.EqualTo(96), "round(8% * 1200) debe dar 96 PV, igual que el ejemplo del GDD.");
        }

        [Test]
        public void test_heal_all_heroes_ignores_legacy_argument()
        {
            // Ruta nueva: sin argumento.
            int beforeLowNoArg = _lowHealthHero.CurrentHealth;
            int beforeHighNoArg = _highHealthHero.CurrentHealth;

            _commander.HealAllHeroes();

            int healedLowNoArg = _lowHealthHero.CurrentHealth - beforeLowNoArg;
            int healedHighNoArg = _highHealthHero.CurrentHealth - beforeHighNoArg;

            // Ruta legacy: mismo argumento (25) que el OnClick serializado en escena.
            // Sigue habiendo margen de sobra (solo se gasto un 8%/96 de un colchon del 50%).
            int beforeLowLegacy = _lowHealthHero.CurrentHealth;
            int beforeHighLegacy = _highHealthHero.CurrentHealth;

            _commander.HealAllHeroes(25);

            int healedLowLegacy = _lowHealthHero.CurrentHealth - beforeLowLegacy;
            int healedHighLegacy = _highHealthHero.CurrentHealth - beforeHighLegacy;

            Assert.That(healedLowLegacy, Is.EqualTo(healedLowNoArg),
                "El argumento legacy no debe cambiar cuanto se cura un heroe de baja vida maxima.");
            Assert.That(healedHighLegacy, Is.EqualTo(healedHighNoArg),
                "El argumento legacy no debe cambiar cuanto se cura un heroe de alta vida maxima.");
        }
    }
}
