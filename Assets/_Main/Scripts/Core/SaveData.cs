using System;

namespace BlobRunner.Core
{
    /// <summary>
    /// Everything that is persisted between sessions. Plain public fields so that Unity's
    /// <c>JsonUtility</c> can (de)serialize it; all logic is engine independent and unit tested.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;
        public const int MaxLevel = 9999;

        public int version = CurrentVersion;

        /// <summary>Level that will be played next (1-based).</summary>
        public int level = 1;

        public int highestLevel = 1;
        public int bestScore;
        public int totalScore;
        public int totalStars;
        public int lootCollected;
        public int runs;
        public int wins;

        public bool sfxOn = true;
        public bool musicOn = true;
        public bool hapticsOn = true;
        public bool tutorialDone;

        /// <summary>Repairs out-of-range values (hand edited / corrupted / older saves).</summary>
        public void Sanitize()
        {
            if (version <= 0 || version > CurrentVersion)
                version = CurrentVersion;

            level = Clamp(level, 1, MaxLevel);
            highestLevel = Clamp(Math.Max(highestLevel, level), 1, MaxLevel);
            bestScore = Math.Max(0, bestScore);
            totalScore = Math.Max(0, totalScore);
            totalStars = Math.Max(0, totalStars);
            lootCollected = Math.Max(0, lootCollected);
            runs = Math.Max(0, runs);
            wins = Math.Max(0, Math.Min(wins, runs));
        }

        public void RegisterRunStarted()
        {
            runs = Math.Min(runs + 1, int.MaxValue - 1);
        }

        /// <summary>Records a finished level and advances to the next one.</summary>
        public void RegisterWin(int levelPlayed, int score, int stars, int loot)
        {
            wins++;
            score = Math.Max(0, score);
            bestScore = Math.Max(bestScore, score);
            totalScore = (int)Math.Min((long)totalScore + score, int.MaxValue);
            totalStars += Math.Max(0, Math.Min(3, stars));
            lootCollected += Math.Max(0, loot);

            level = Clamp(Math.Max(level, levelPlayed + 1), 1, MaxLevel);
            highestLevel = Math.Max(highestLevel, level);
            Sanitize();
        }

        private static int Clamp(int v, int min, int max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }
}
