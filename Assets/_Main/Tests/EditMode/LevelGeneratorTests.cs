using System;
using System.Collections.Generic;
using BlobRunner.Core;
using NUnit.Framework;

namespace BlobRunner.Tests
{
    public class LevelGeneratorTests
    {
        private const int LevelsToCheck = 80;

        [Test]
        public void GenerationIsDeterministic()
        {
            for (int level = 1; level <= 25; level++)
            {
                var a = LevelGenerator.Generate(level);
                var b = LevelGenerator.Generate(level);

                Assert.AreEqual(a.Obstacles.Count, b.Obstacles.Count);
                Assert.AreEqual(a.Loot.Count, b.Loot.Count);
                for (int i = 0; i < a.Obstacles.Count; i++)
                {
                    Assert.AreEqual(a.Obstacles[i].Z, b.Obstacles[i].Z);
                    Assert.AreEqual(a.Obstacles[i].X, b.Obstacles[i].X);
                    Assert.AreEqual(a.Obstacles[i].Y, b.Obstacles[i].Y);
                    Assert.AreEqual(a.Obstacles[i].Width, b.Obstacles[i].Width);
                }
                for (int i = 0; i < a.Loot.Count; i++)
                {
                    Assert.AreEqual(a.Loot[i].Z, b.Loot[i].Z);
                    Assert.AreEqual(a.Loot[i].X, b.Loot[i].X);
                }
            }
        }

        [Test]
        public void DifferentLevelsProduceDifferentLayouts()
        {
            var a = LevelGenerator.Generate(3);
            var b = LevelGenerator.Generate(4);
            bool different = a.Obstacles.Count != b.Obstacles.Count || a.FinishZ != b.FinishZ;
            for (int i = 0; !different && i < Math.Min(a.Obstacles.Count, b.Obstacles.Count); i++)
                different = a.Obstacles[i].Z != b.Obstacles[i].Z || a.Obstacles[i].X != b.Obstacles[i].X;
            Assert.True(different);
        }

        [Test]
        public void EveryGeneratedLevelPassesTheFairnessRules()
        {
            for (int level = 1; level <= LevelsToCheck; level++)
            {
                var problems = LevelGenerator.Validate(LevelGenerator.Generate(level));
                Assert.IsEmpty(problems, "level " + level + ": " + string.Join("; ", problems.ToArray()));
            }
        }

        [Test]
        public void LevelsBelowOneAreClampedToLevelOne()
        {
            Assert.AreEqual(1, LevelGenerator.Generate(0).Level);
            Assert.AreEqual(1, LevelGenerator.Generate(-7).Level);
        }

        [Test]
        public void FirstLevelMatchesTheAuthoredTrackLength()
        {
            Assert.AreEqual(120f, LevelGenerator.FinishDistance(1));
            Assert.AreEqual(LevelGenerator.Generate(1).FinishZ, 120f);
        }

        [Test]
        public void TrackLengthGrowsAndIsCapped()
        {
            float previous = 0f;
            for (int level = 1; level <= 30; level++)
            {
                float d = LevelGenerator.FinishDistance(level);
                Assert.GreaterOrEqual(d, previous);
                Assert.LessOrEqual(d, 260f);
                previous = d;
            }

            Assert.AreEqual(260f, LevelGenerator.FinishDistance(30));
        }

        [Test]
        public void PlayerSpeedGrowsAndIsCapped()
        {
            Assert.AreEqual(4f, LevelGenerator.PlayerSpeedFor(1), 0.0001);
            Assert.Greater(LevelGenerator.PlayerSpeedFor(5), LevelGenerator.PlayerSpeedFor(1));
            Assert.AreEqual(5.5f, LevelGenerator.PlayerSpeedFor(500), 0.0001);
        }

        [Test]
        public void RowsGetCloserTogetherAsLevelsProgress()
        {
            Assert.Greater(LevelGenerator.RowSpacing(1), LevelGenerator.RowSpacing(8));
            Assert.Greater(LevelGenerator.RowSpacing(8), LevelGenerator.RowSpacing(13));
            Assert.AreEqual(12f, LevelGenerator.RowSpacing(100), 0.0001);
        }

        [Test]
        public void ObstacleRowsAreSortedAndNeverTooClose()
        {
            for (int level = 1; level <= LevelsToCheck; level++)
            {
                var plan = LevelGenerator.Generate(level);
                float lastRow = -1000f;
                for (int i = 0; i < plan.Obstacles.Count; i++)
                {
                    float z = plan.Obstacles[i].Z;
                    Assert.GreaterOrEqual(z, lastRow, "level " + level);
                    if (z - lastRow > 0.01f && lastRow > -999f)
                        Assert.GreaterOrEqual(z - lastRow, 9f, "level " + level + " rows too close");
                    lastRow = Math.Max(lastRow, z);
                }
            }
        }

        [Test]
        public void LevelOneHasNoUndodgeableWall()
        {
            foreach (var o in LevelGenerator.Generate(1).Obstacles)
                Assert.False(o.BlocksLane);
        }

        [Test]
        public void WallCountIsLimitedPerLevel()
        {
            for (int level = 1; level <= LevelsToCheck; level++)
            {
                int walls = 0;
                foreach (var o in LevelGenerator.Generate(level).Obstacles)
                    if (o.BlocksLane) walls++;
                Assert.LessOrEqual(walls, LevelGenerator.MaxWalls(level), "level " + level);
            }
        }

        [Test]
        public void NeedleRowsAndSlidingBarsAppearOnlyOnLaterLevels()
        {
            bool needleLate = false;
            bool sliderLate = false;
            for (int level = 1; level <= 40; level++)
            {
                var plan = LevelGenerator.Generate(level);
                bool sliders = false;
                bool needles = false;
                for (int i = 0; i < plan.Obstacles.Count; i++)
                {
                    if (plan.Obstacles[i].Kind == ObstacleKind.Sliding)
                        sliders = true;
                    if (i > 0 && plan.Obstacles[i].Kind == ObstacleKind.Static && plan.Obstacles[i - 1].Kind == ObstacleKind.Static
                        && Math.Abs(plan.Obstacles[i].Z - plan.Obstacles[i - 1].Z) < 0.01f)
                        needles = true;
                }

                if (level < 4)
                    Assert.False(sliders, "sliding bar on level " + level);
                if (level < 3)
                    Assert.False(needles, "needle row on level " + level);

                sliderLate |= sliders;
                needleLate |= needles;
            }

            Assert.True(sliderLate, "no sliding bar was generated on levels 1-40");
            Assert.True(needleLate, "no needle row was generated on levels 1-40");
        }

        [Test]
        public void SlidingBarsHaveSaneMotionParameters()
        {
            for (int level = 4; level <= 40; level++)
            {
                foreach (var o in LevelGenerator.Generate(level).Obstacles)
                {
                    if (o.Kind != ObstacleKind.Sliding)
                        continue;
                    Assert.Greater(o.SlideRange, 0.2f);
                    Assert.Greater(o.SlideSpeed, 0.5f);
                    Assert.LessOrEqual(o.SlideSpeed, 3.2f + 0.0001f);
                    Assert.Less(o.Width, LevelGenerator.BarLength);
                }
            }
        }

        [Test]
        public void ObstacleHeightsComeFromTheKnownSet()
        {
            var allowed = new List<float>(LevelGenerator.BarHeights);
            for (int level = 1; level <= LevelsToCheck; level++)
            {
                foreach (var o in LevelGenerator.Generate(level).Obstacles)
                    Assert.True(allowed.Contains(o.Y), "level " + level + " height " + o.Y);
            }
        }

        [Test]
        public void LootStaysInsideTheLaneAndIsSpacedOut()
        {
            for (int level = 1; level <= LevelsToCheck; level++)
            {
                var plan = LevelGenerator.Generate(level);
                for (int i = 0; i < plan.Loot.Count; i++)
                {
                    Assert.LessOrEqual(Math.Abs(plan.Loot[i].X), 1.11f, "level " + level);
                    if (i > 0)
                        Assert.GreaterOrEqual(plan.Loot[i].Z - plan.Loot[i - 1].Z, 4f, "level " + level + " loot too close together");
                }
            }
        }

        [Test]
        public void LootCountMeetsTheMinimumAndGrowsWithLevel()
        {
            for (int level = 1; level <= LevelsToCheck; level++)
                Assert.GreaterOrEqual(LevelGenerator.Generate(level).Loot.Count, LevelGenerator.MinLoot(level), "level " + level);

            Assert.GreaterOrEqual(LevelGenerator.MinLoot(10), LevelGenerator.MinLoot(1));
        }

        [Test]
        public void ThemesCycle()
        {
            Assert.AreEqual(0, LevelGenerator.Generate(1).ThemeIndex);
            Assert.AreEqual(1, LevelGenerator.Generate(2).ThemeIndex);
            Assert.AreEqual(0, LevelGenerator.Generate(1 + LevelGenerator.ThemeCount).ThemeIndex);
        }

        [Test]
        public void LargestFreeGapMeasuresTheOpenPartOfTheLane()
        {
            var plan = new LevelPlan();
            // a bar covering the left 1.8 units of the lane (centre -1.75 + ... = edge at +0.3)
            plan.Obstacles.Add(new ObstacleSpec { Z = 20, X = 0.3f - 1.75f, Y = 0.4f, Width = 3.5f, Kind = ObstacleKind.Static });
            Assert.AreEqual(1.2f, LevelGenerator.LargestFreeGap(plan, 20f), 0.001);

            // a second bar on the right leaves a needle in the middle
            plan.Obstacles.Add(new ObstacleSpec { Z = 20, X = 1.3f + 1.75f, Y = 1.64f, Width = 3.5f, Kind = ObstacleKind.Static });
            Assert.AreEqual(1.0f, LevelGenerator.LargestFreeGap(plan, 20f), 0.001);

            // a different row is ignored
            Assert.AreEqual(3.0f, LevelGenerator.LargestFreeGap(plan, 60f), 0.001);
        }

        [Test]
        public void ValidateFlagsBrokenPlans()
        {
            var plan = LevelGenerator.Generate(5);
            Assert.IsEmpty(LevelGenerator.Validate(plan));

            // a bar that fills the lane without being flagged as a wall
            plan.Obstacles.Add(new ObstacleSpec { Z = plan.FinishZ - LevelGenerator.RunOut, X = 0f, Y = 0.4f, Width = 3.5f, Kind = ObstacleKind.Static });
            Assert.Greater(LevelGenerator.Validate(plan).Count, 0);

            Assert.Greater(LevelGenerator.Validate(null).Count, 0);
        }

        [Test]
        public void ProgressIsClampedToZeroOne()
        {
            var plan = LevelGenerator.Generate(1);
            Assert.AreEqual(0f, plan.ProgressAt(-10f));
            Assert.AreEqual(1f, plan.ProgressAt(plan.FinishZ + 10f));
            Assert.AreEqual(0.5f, plan.ProgressAt(plan.FinishZ * 0.5f), 0.0001);
        }
    }
}
