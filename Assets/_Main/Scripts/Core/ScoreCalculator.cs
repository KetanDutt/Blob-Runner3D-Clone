using System;

namespace BlobRunner.Core
{
    /// <summary>Breakdown of the score awarded for a completed level.</summary>
    public struct ScoreResult
    {
        public int Score;
        public int Stars;

        /// <summary>Share (0..1) of body parts that were still attached at the finish line.</summary>
        public float Integrity;

        public int BasePoints;
        public int IntegrityPoints;
        public int LootPoints;
        public int TimePoints;
    }

    /// <summary>Pure score maths so it can be balanced and unit tested without Unity.</summary>
    public static class ScoreCalculator
    {
        public const int PointsPerLoot = 100;
        public const int MaxIntegrityPoints = 1000;
        public const int MaxTimePoints = 1000;
        public const float TimePointsPerSecond = 20f;

        /// <summary>Integrity needed for three / two stars. Finishing always awards at least one.</summary>
        public const float ThreeStarIntegrity = 0.70f;
        public const float TwoStarIntegrity = 0.35f;

        public static int StarsFor(float integrity)
        {
            if (integrity >= ThreeStarIntegrity)
                return 3;
            if (integrity >= TwoStarIntegrity)
                return 2;
            return 1;
        }

        /// <param name="level">Level that was played (1-based).</param>
        /// <param name="partsAlive">Body parts attached at the finish line.</param>
        /// <param name="partsTotal">Total number of body parts.</param>
        /// <param name="lootCollected">Loot items picked up during the run.</param>
        /// <param name="runSeconds">Duration of the run.</param>
        /// <param name="parSeconds">Time in which a clean run finishes; faster runs earn time points.</param>
        public static ScoreResult Compute(int level, int partsAlive, int partsTotal, int lootCollected, float runSeconds, float parSeconds)
        {
            level = Math.Max(1, level);
            partsTotal = Math.Max(0, partsTotal);
            partsAlive = Math.Max(0, Math.Min(partsAlive, partsTotal));
            lootCollected = Math.Max(0, lootCollected);
            if (float.IsNaN(runSeconds) || runSeconds < 0f) runSeconds = 0f;
            if (float.IsNaN(parSeconds) || parSeconds < 0f) parSeconds = 0f;

            var result = new ScoreResult();
            result.Integrity = partsTotal > 0 ? (float)partsAlive / partsTotal : 0f;
            result.Stars = StarsFor(result.Integrity);
            result.BasePoints = 500 + 100 * level;
            result.IntegrityPoints = (int)Math.Round(result.Integrity * MaxIntegrityPoints);
            result.LootPoints = lootCollected * PointsPerLoot;

            float timeBonus = (parSeconds - runSeconds) * TimePointsPerSecond;
            result.TimePoints = (int)Math.Max(0f, Math.Min(MaxTimePoints, Math.Round(timeBonus)));

            result.Score = result.BasePoints + result.IntegrityPoints + result.LootPoints + result.TimePoints;
            return result;
        }
    }
}
