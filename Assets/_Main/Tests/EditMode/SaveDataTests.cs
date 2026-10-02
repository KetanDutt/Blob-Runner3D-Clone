using BlobRunner.Core;
using NUnit.Framework;

namespace BlobRunner.Tests
{
    public class SaveDataTests
    {
        [Test]
        public void DefaultsAreSensible()
        {
            var data = new SaveData();
            Assert.AreEqual(1, data.level);
            Assert.True(data.sfxOn && data.musicOn && data.hapticsOn);
            Assert.False(data.tutorialDone);
            Assert.AreEqual(SaveData.CurrentVersion, data.version);
        }

        [Test]
        public void SanitizeRepairsCorruptedValues()
        {
            var data = new SaveData { version = 99, level = -4, highestLevel = 0, bestScore = -1, totalScore = -9, totalStars = -2, lootCollected = -5, runs = 2, wins = 17 };
            data.Sanitize();

            Assert.AreEqual(SaveData.CurrentVersion, data.version);
            Assert.AreEqual(1, data.level);
            Assert.GreaterOrEqual(data.highestLevel, data.level);
            Assert.AreEqual(0, data.bestScore);
            Assert.AreEqual(0, data.totalScore);
            Assert.AreEqual(0, data.totalStars);
            Assert.AreEqual(0, data.lootCollected);
            Assert.LessOrEqual(data.wins, data.runs);
        }

        [Test]
        public void SanitizeClampsAbsurdLevels()
        {
            var data = new SaveData { level = int.MaxValue, highestLevel = 3 };
            data.Sanitize();
            Assert.AreEqual(SaveData.MaxLevel, data.level);
            Assert.AreEqual(SaveData.MaxLevel, data.highestLevel);
        }

        [Test]
        public void WinAdvancesToTheNextLevelAndKeepsTheBestScore()
        {
            var data = new SaveData();
            data.RegisterRunStarted();
            data.RegisterWin(1, 1500, 3, 2);

            Assert.AreEqual(2, data.level);
            Assert.AreEqual(2, data.highestLevel);
            Assert.AreEqual(1500, data.bestScore);
            Assert.AreEqual(3, data.totalStars);
            Assert.AreEqual(2, data.lootCollected);
            Assert.AreEqual(1, data.wins);

            data.RegisterRunStarted();
            data.RegisterWin(2, 900, 1, 0);
            Assert.AreEqual(1500, data.bestScore, "a worse score must not replace the best score");
            Assert.AreEqual(3, data.level);
            Assert.AreEqual(2400, data.totalScore);
        }

        [Test]
        public void ReplayingAnEarlierLevelNeverMovesProgressBackwards()
        {
            var data = new SaveData { level = 7, highestLevel = 7 };
            data.RegisterRunStarted();
            data.RegisterWin(2, 1000, 2, 1);
            Assert.AreEqual(7, data.level);
            Assert.AreEqual(7, data.highestLevel);
        }

        [Test]
        public void StarsAreClampedPerWin()
        {
            var data = new SaveData();
            data.RegisterRunStarted();
            data.RegisterWin(1, 10, 99, 0);
            Assert.AreEqual(3, data.totalStars);
        }
    }
}
