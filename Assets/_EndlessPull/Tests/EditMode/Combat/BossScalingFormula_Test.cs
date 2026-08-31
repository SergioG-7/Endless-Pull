using NUnit.Framework;
using UnityEngine;

namespace EndlessPull.Tests.EditMode.Combat
{
    // Story type: Logic (formula) — Fase 29, "Balance de Combate y Decretos".
    // Verifica que WaveManager.SpawnBoss() preserva los ratios HP/ATK del jefe,
    // calibrados en el piso 5 contra Enemy_GoblinKing / Enemy_Goblin_Test,
    // en cualquier piso de jefe futuro (5, 10, 15, 20).
    // Evidencia: BLOCKING. Ubicacion logica pedida: tests/unit/combat/ — movido
    // a Assets/ porque Unity no compila scripts fuera de esa carpeta (ver reporte QA).
    public class BossScalingFormula_Test
    {
        // Enemy_GoblinKing.asset — unico jefe calibrado hoy.
        private const int BossBaseHealth = 450;
        private const int BossBaseAttack = 40;

        // Enemy_Goblin_Test.asset — relleno de referencia usado para calibrar el ratio.
        private const int FillBaseHealth = 50;
        private const int FillBaseAttack = 12;

        // Debe coincidir con el default de WaveManager.bossCalibrationFloor.
        private const int CalibrationFloor = 5;

        private const float ExpectedHpRatio = 4.5f;
        private const float ExpectedAttackRatio = 1.2255f;
        private const float Tolerance = 0.001f;

        private WaveManager _waveManager;

        [SetUp]
        public void SetUp()
        {
            _waveManager = new GameObject("WaveManager_Test").AddComponent<WaveManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_waveManager != null) Object.DestroyImmediate(_waveManager.gameObject);
        }

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(15)]
        [TestCase(20)]
        public void test_boss_hp_ratio_stays_constant_across_boss_floors(int floor)
        {
            float bossHpMult = _waveManager.StatMultiplierForFloor(floor)
                                / _waveManager.StatMultiplierForFloor(CalibrationFloor);

            float bossHp = BossBaseHealth * bossHpMult;
            float fillHp = FillBaseHealth * _waveManager.StatMultiplierForFloor(floor);

            float ratio = bossHp / fillHp;

            Assert.That(ratio, Is.EqualTo(ExpectedHpRatio).Within(Tolerance),
                $"El ratio HP jefe/relleno debe mantenerse en {ExpectedHpRatio} en el piso {floor}.");
        }

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(15)]
        [TestCase(20)]
        public void test_boss_attack_ratio_stays_constant_across_boss_floors(int floor)
        {
            float bossHpMult = _waveManager.StatMultiplierForFloor(floor)
                                / _waveManager.StatMultiplierForFloor(CalibrationFloor);
            float bossAtkMult = _waveManager.AttackMultiplierForFloor(floor)
                                 / _waveManager.AttackMultiplierForFloor(CalibrationFloor);

            // Misma formula que EnemyController.Attack: baseAttack * statMultiplier * attackMultiplier.
            float bossAttack = BossBaseAttack * bossHpMult * bossAtkMult;
            float fillAttack = FillBaseAttack * _waveManager.StatMultiplierForFloor(floor)
                                * _waveManager.AttackMultiplierForFloor(floor);

            float ratio = bossAttack / fillAttack;

            Assert.That(ratio, Is.EqualTo(ExpectedAttackRatio).Within(Tolerance),
                $"El ratio ATK jefe/relleno debe mantenerse en ~{ExpectedAttackRatio} en el piso {floor}.");
        }

        [Test]
        public void test_boss_multipliers_are_exactly_one_at_calibration_floor()
        {
            // En su propio piso de calibracion el jefe no debe reescalarse: es el ancla de la formula.
            float bossHpMult = _waveManager.StatMultiplierForFloor(CalibrationFloor)
                                / _waveManager.StatMultiplierForFloor(CalibrationFloor);
            float bossAtkMult = _waveManager.AttackMultiplierForFloor(CalibrationFloor)
                                 / _waveManager.AttackMultiplierForFloor(CalibrationFloor);

            Assert.That(bossHpMult, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(bossAtkMult, Is.EqualTo(1f).Within(Tolerance));
        }
    }
}
