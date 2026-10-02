using BlobRunner.Core;
using NUnit.Framework;

namespace BlobRunner.Tests
{
    public class ScoreCalculatorTests
    {
        [TestCase(1.0f, 3)]
        [TestCase(0.70f, 3)]
        [TestCase(0.69f, 2)]
        [TestCase(0.35f, 2)]
        [TestCase(0.34f, 1)]
        [TestCase(0.0f, 1)]
        public void StarsFollowTheIntegrityThresholds(float integrity, int stars)
        {
            Assert.AreEqual(stars, ScoreCalculator.StarsFor(integrity));
        }

        [Test]
        public void CleanFastRunScoresTheMost()
        {
            var clean = ScoreCalculator.Compute(3, 11, 11, 4, 20f, 30f);
            var damaged = ScoreCalculator.Compute(3, 4, 11, 4, 20f, 30f);
            Assert.Greater(clean.Score, damaged.Score);
            Assert.AreEqual(3, clean.Stars);
            Assert.AreEqual(2, damaged.Stars);
            Assert.AreEqual(1f, clean.Integrity, 0.0001);
        }

        [Test]
        public void LootAddsPoints()
        {
            var none = ScoreCalculator.Compute(1, 8, 11, 0, 40f, 30f);
            var some = ScoreCalculator.Compute(1, 8, 11, 3, 40f, 30f);
            Assert.AreEqual(3 * ScoreCalculator.PointsPerLoot, some.Score - none.Score);
        }

        [Test]
        public void TimeBonusIsCappedAndNeverNegative()
        {
            Assert.AreEqual(0, ScoreCalculator.Compute(1, 11, 11, 0, 500f, 30f).TimePoints);
            Assert.AreEqual(ScoreCalculator.MaxTimePoints, ScoreCalculator.Compute(1, 11, 11, 0, 0f, 3000f).TimePoints);
        }

        [Test]
        public void HigherLevelsAreWorthMore()
        {
            Assert.Greater(ScoreCalculator.Compute(9, 11, 11, 0, 30f, 30f).Score, ScoreCalculator.Compute(1, 11, 11, 0, 30f, 30f).Score);
        }

        [Test]
        public void NeverNegativeAndSafeAgainstBadInput()
        {
            var r = ScoreCalculator.Compute(-5, -3, -1, -2, -1f, float.NaN);
            Assert.GreaterOrEqual(r.Score, 0);
            Assert.GreaterOrEqual(r.Integrity, 0f);
            Assert.AreEqual(1, r.Stars);

            var over = ScoreCalculator.Compute(1, 99, 11, 0, 10f, 10f);
            Assert.AreEqual(1f, over.Integrity, 0.0001);
        }

        [Test]
        public void ScoreIsTheSumOfItsParts()
        {
            var r = ScoreCalculator.Compute(4, 7, 11, 2, 25f, 31f);
            Assert.AreEqual(r.BasePoints + r.IntegrityPoints + r.LootPoints + r.TimePoints, r.Score);
        }
    }
}
