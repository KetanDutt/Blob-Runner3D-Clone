using BlobRunner.Core;
using NUnit.Framework;

namespace BlobRunner.Tests
{
    public class DifficultyModelTests
    {
        private static LevelPlan PlanWith(params ObstacleSpec[] bars)
        {
            var plan = new LevelPlan { Level = 20, StartZ = 0f, FinishZ = 260f };
            plan.Obstacles.AddRange(bars);
            return plan;
        }

        private static ObstacleSpec Wall(float z, float y)
        {
            return new ObstacleSpec { Z = z, X = 0f, Y = y, Width = LevelGenerator.BarLength, Kind = ObstacleKind.Static, BlocksLane = true };
        }

        [TestCase(0.40f, 2)]
        [TestCase(0.74f, 4)]
        [TestCase(0.99f, 5)]
        [TestCase(1.30f, 6)]
        [TestCase(1.64f, 1)]
        public void BarHeightsCutTheDocumentedNumberOfParts(float height, int parts)
        {
            Assert.AreEqual(parts, DifficultyModel.CountBits(DifficultyModel.PartsCutMask(height)));
        }

        [Test]
        public void HipsAndChestTogetherRemoveEveryPart()
        {
            int mask = DifficultyModel.PartsCutMask(0.99f) | DifficultyModel.PartsCutMask(1.30f);
            Assert.AreEqual(DifficultyModel.AllParts, mask);
            Assert.AreEqual(DifficultyModel.PartCount, DifficultyModel.CountBits(mask));
        }

        [Test]
        public void TargetStartsAtZeroRisesAndSaturates()
        {
            Assert.AreEqual(0f, DifficultyModel.TargetDeathChance(1));
            Assert.AreEqual(0f, DifficultyModel.TargetDeathChance(2));

            float previous = 0f;
            for (int level = 1; level <= 60; level++)
            {
                float target = DifficultyModel.TargetDeathChance(level);
                Assert.GreaterOrEqual(target, previous, "level " + level);
                Assert.LessOrEqual(target, 0.38f + 0.0001f);
                previous = target;
            }

            Assert.AreEqual(0.38f, DifficultyModel.TargetDeathChance(24), 0.0001);
            Assert.AreEqual(0.38f, DifficultyModel.TargetDeathChance(500), 0.0001);
        }

        [Test]
        public void AnEmptyTrackIsHarmless()
        {
            Assert.AreEqual(0f, DifficultyModel.EstimateDeathChance(PlanWith(), 0.5f, 200, 1));
            Assert.AreEqual(0f, DifficultyModel.EstimateDeathChance(null, 0.5f, 200, 1));
            Assert.AreEqual(0f, DifficultyModel.EstimateDeathChance(PlanWith(), 0.5f, 0, 1));
        }

        [Test]
        public void UndodgeableHipAndChestWallsWithoutLootAlwaysKill()
        {
            var plan = PlanWith(Wall(30f, 0.99f), Wall(60f, 1.30f));
            Assert.AreEqual(1f, DifficultyModel.EstimateDeathChance(plan, 1f, 200, 1), 0.0001);
        }

        [Test]
        public void LootBetweenTheWallsMakesTheLevelSurvivable()
        {
            var plan = PlanWith(Wall(30f, 0.99f), Wall(60f, 1.30f));
            plan.Loot.Add(new LootSpec { Z = 45f, X = 0f, Y = 1.25f });

            // the loot is collected 92% of the time, so only the misses are fatal
            float chance = DifficultyModel.EstimateDeathChance(plan, 1f, 4000, 7);
            Assert.Less(chance, 0.15f);
            Assert.Greater(chance, 0.02f);
        }

        [Test]
        public void BetterPlayersDieLessOften()
        {
            var plan = LevelGenerator.Generate(24);
            float poor = DifficultyModel.EstimateDeathChance(plan, 0.45f, 2000, 5);
            float average = DifficultyModel.EstimateDeathChance(plan, 0.75f, 2000, 5);
            float expert = DifficultyModel.EstimateDeathChance(plan, 0.97f, 2000, 5);

            Assert.Greater(poor, average);
            Assert.Greater(average, expert);
        }

        [Test]
        public void EstimatesAreDeterministic()
        {
            var plan = LevelGenerator.Generate(12);
            Assert.AreEqual(DifficultyModel.EstimateDeathChance(plan, 0.75f, 500, 99), DifficultyModel.EstimateDeathChance(plan, 0.75f, 500, 99));
        }

        [Test]
        public void GeneratedLevelsFollowTheDifficultyTarget()
        {
            float totalError = 0f;
            const int levels = 40;
            for (int level = 1; level <= levels; level++)
            {
                var plan = LevelGenerator.Generate(level);
                // measured with a different seed / sample count than the generator used
                float measured = DifficultyModel.EstimateDeathChance(plan, DifficultyModel.ReferenceSkill, 3000, 424242 + level);
                float error = System.Math.Abs(measured - DifficultyModel.TargetDeathChance(level));
                totalError += error;
                Assert.LessOrEqual(error, 0.22f, "level " + level + " is far from its target (measured " + measured + ")");
            }

            Assert.LessOrEqual(totalError / levels, 0.06f, "levels do not follow the difficulty curve on average");
        }

        [Test]
        public void DifficultyRisesFromTheFirstToTheLaterLevels()
        {
            float Average(int from, int to)
            {
                float sum = 0f;
                for (int level = from; level <= to; level++)
                    sum += DifficultyModel.EstimateDeathChance(LevelGenerator.Generate(level), DifficultyModel.ReferenceSkill, 1500, 31337 + level);
                return sum / (to - from + 1);
            }

            float early = Average(1, 5);
            float middle = Average(8, 14);
            float late = Average(22, 30);

            Assert.Less(early, middle);
            Assert.Less(middle, late);
            Assert.Less(early, 0.06f, "the first levels must be easy");
        }
    }
}
