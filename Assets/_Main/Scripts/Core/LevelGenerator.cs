using System;
using System.Collections.Generic;

namespace BlobRunner.Core
{
    /// <summary>
    /// Deterministic, engine independent level generator.
    ///
    /// A level is a list of "rows" (bars the blob has to dodge or lose parts to) separated by a
    /// distance that shrinks as the level number grows, plus loot that regrows the cut off parts.
    ///
    /// Fairness rules (verified by unit tests):
    ///  * every row that is not a wall / a sliding bar leaves a gap of at least <see cref="MinGapWidth"/>;
    ///  * walls (undodgeable rows) are limited per level and are always "paid for": loot is available
    ///    shortly before and right after them;
    ///  * loot never overlaps a bar (<see cref="LootClearance"/>), stays inside the lane and is spaced out.
    /// </summary>
    public static class LevelGenerator
    {
        // ---- Track geometry (world units, matches the authored Scene_001) ----------------------

        public const float LaneHalfWidth = 1.5f;
        public const float LaneWidth = 3f;

        /// <summary>Length (local X scale) of a full size bar.</summary>
        public const float BarLength = 3.5f;

        /// <summary>Distance between the start and the first row.</summary>
        public const float RunUp = 14f;

        /// <summary>Clear distance between the last row and the finish line.</summary>
        public const float RunOut = 22f;

        public const float LootHeight = 1.25f;

        /// <summary>Every dodgeable row leaves at least this much room (the blob is ~0.9 wide with arms).</summary>
        public const float MinGapWidth = 1.0f;

        /// <summary>Minimum Z distance between a loot item and any bar.</summary>
        public const float LootClearance = 3f;

        /// <summary>Maximum distance between two loot items that a wall depends on.</summary>
        public const float MaxSupplyDistance = 56f;

        public const int ThemeCount = 6;
        public const int LootColorCount = 8;

        /// <summary>Bar heights: shins, thighs, hips (cuts both legs), chest (cuts arms + head), head.</summary>
        public static readonly float[] BarHeights = { 0.40f, 0.74f, 0.99f, 1.30f, 1.64f };

        private enum RowKind
        {
            None,
            SideBar,
            HighBar,
            LowBar,
            Needle,
            Slider,
            Wall
        }

        // ---- Difficulty curves ----------------------------------------------------------------

        public static int SeedFor(int level)
        {
            return unchecked(level * 7919 + 17);
        }

        /// <summary>Level 1 matches the authored scene (finish at Z = 120).</summary>
        public static float FinishDistance(int level)
        {
            return Clamp(100f + 20f * Math.Max(1, level), 120f, 260f);
        }

        public static float PlayerSpeedFor(int level)
        {
            return Math.Min(4f + 0.15f * (Math.Max(1, level) - 1), 5.5f);
        }

        /// <summary>0 (level 1) .. 1 (level 15 and above).</summary>
        public static float Difficulty(int level)
        {
            return Clamp((Math.Max(1, level) - 1) / 14f, 0f, 1f);
        }

        public static float RowSpacing(int level)
        {
            float t = Clamp((Math.Max(1, level) - 1) / 12f, 0f, 1f);
            return Lerp(24f, 12f, t);
        }

        public static int MaxWalls(int level)
        {
            return level < 2 ? 0 : Math.Min(1 + level / 3, 6);
        }

        public static int MinLoot(int level)
        {
            return Math.Min(2 + Math.Max(1, level) / 2, 8);
        }

        // ---- Generation -----------------------------------------------------------------------

        /// <summary>Number of layouts that are generated and compared per level.</summary>
        public const int Candidates = 24;

        /// <summary>
        /// Generates the level: several deterministic candidates are produced and the one whose estimated difficulty
        /// (see <see cref="DifficultyModel"/>) is closest to the target of that level is kept. The best few candidates
        /// are re-measured with more samples so that estimation noise does not decide the outcome.
        /// </summary>
        public static LevelPlan Generate(int level)
        {
            if (level < 1)
                level = 1;

            float target = DifficultyModel.TargetDeathChance(level);

            var plans = new List<LevelPlan>(Candidates);
            var errors = new List<float>(Candidates);

            for (int attempt = 0; attempt < Candidates; attempt++)
            {
                var candidate = GenerateCandidate(level, unchecked(SeedFor(level) + attempt * 104729));
                float estimate = DifficultyModel.EstimateDeathChance(candidate, DifficultyModel.ReferenceSkill, 300, candidate.Seed ^ 0x2545F491);
                plans.Add(candidate);
                errors.Add(Math.Abs(estimate - target));
            }

            // refine: re-measure the three most promising candidates with 5x the samples
            LevelPlan best = null;
            float bestError = float.MaxValue;
            for (int pick = 0; pick < 3; pick++)
            {
                int index = -1;
                float lowest = float.MaxValue;
                for (int i = 0; i < errors.Count; i++)
                {
                    if (errors[i] < lowest)
                    {
                        lowest = errors[i];
                        index = i;
                    }
                }

                if (index < 0)
                    break;

                var plan = plans[index];
                errors[index] = float.MaxValue; // taken

                float refined = DifficultyModel.EstimateDeathChance(plan, DifficultyModel.ReferenceSkill, 1500, plan.Seed ^ 0x1B873593);
                float error = Math.Abs(refined - target);
                if (error < bestError)
                {
                    best = plan;
                    bestError = error;
                }
            }

            return best;
        }

        private static LevelPlan GenerateCandidate(int level, int seed)
        {
            var plan = new LevelPlan
            {
                Level = level,
                Seed = seed,
                ThemeIndex = (level - 1) % ThemeCount,
                StartZ = 0f,
                FinishZ = FinishDistance(level),
                PlayerSpeed = PlayerSpeedFor(level)
            };

            var rng = new Rng(plan.Seed);
            float difficulty = Difficulty(level);
            float spacing = RowSpacing(level);
            int wallsLeft = MaxWalls(level);

            float z = RunUp + rng.Range(0f, 4f);
            float lastLootZ = 0f;           // the player starts "supplied"
            int sameSideCount = 0;
            int lastSide = 0;
            RowKind prev = RowKind.None;
            RowKind prev2 = RowKind.None;

            while (z <= plan.FinishZ - RunOut)
            {
                bool supplied = z - lastLootZ <= MaxSupplyDistance;
                RowKind kind = PickKind(level, difficulty, rng, wallsLeft, supplied, prev, prev2);

                int side = rng.Sign();
                if (side == lastSide && sameSideCount >= 2)
                    side = -side;

                BuildRow(plan, kind, z, side, rng, level, difficulty);

                if (kind == RowKind.Wall)
                    wallsLeft--;

                if (side == lastSide)
                    sameSideCount++;
                else
                    sameSideCount = 1;
                lastSide = side;

                prev2 = prev;
                prev = kind;

                float nextZ = z + spacing * rng.Range(0.85f, 1.2f);

                // Loot lives in the free zone between this row and the next one.
                float zoneMin = z + LootClearance + 0.25f;
                float zoneMax = nextZ - LootClearance - 0.25f;
                // Loot gets sparser as the levels get harder: that is what makes losing every body part possible.
                bool hard = kind == RowKind.Wall;
                float supplyGap = Lerp(32f, 52f, difficulty);
                float lootChance = Lerp(0.45f, 0.08f, difficulty);
                bool wantLoot = hard || (nextZ - lastLootZ > supplyGap) || rng.Chance(lootChance);

                if (wantLoot && zoneMax > zoneMin)
                {
                    float desired = z + rng.Range(8f, 13f);
                    float lz = Math.Max(zoneMin, Math.Min(desired, zoneMax));
                    AddLoot(plan, rng, lz);
                    lastLootZ = lz;
                }

                z = nextZ;
            }

            // A little treat close to the finish line (score + visual reward).
            float finishLootZ = plan.FinishZ - RunOut * 0.5f;
            if (finishLootZ - lastLootZ > 10f && FarFromBars(plan, finishLootZ))
                AddLoot(plan, rng, finishLootZ);

            // Guarantee the minimum amount of loot by filling the widest free zones.
            int guard = 0;
            while (plan.Loot.Count < MinLoot(level) && guard++ < 32)
            {
                if (!AddLootInWidestGap(plan, rng))
                    break;
            }

            SortByZ(plan);
            return plan;
        }

        // ---- Row construction -----------------------------------------------------------------

        private static RowKind PickKind(int level, float difficulty, Rng rng, int wallsLeft, bool supplied, RowKind prev, RowKind prev2)
        {
            var kinds = new[] { RowKind.SideBar, RowKind.HighBar, RowKind.LowBar, RowKind.Needle, RowKind.Slider, RowKind.Wall };
            var weights = new float[kinds.Length];

            weights[0] = 6f;
            weights[1] = 3f;
            weights[2] = 3f;
            weights[3] = level >= 3 ? 1.5f + 3f * difficulty : 0f;
            weights[4] = level >= 4 ? 1f + 3f * difficulty : 0f;
            weights[5] = (wallsLeft > 0 && supplied) ? 1f + 2f * difficulty : 0f;

            float total = 0f;
            for (int i = 0; i < kinds.Length; i++)
            {
                // never the same kind three times in a row, never two walls in a row
                if (kinds[i] == prev && kinds[i] == prev2)
                    weights[i] = 0f;
                if (kinds[i] == RowKind.Wall && prev == RowKind.Wall)
                    weights[i] = 0f;
                if (kinds[i] == prev && (kinds[i] == RowKind.Needle || kinds[i] == RowKind.Slider))
                    weights[i] *= 0.3f;

                total += weights[i];
            }

            if (total <= 0f)
                return RowKind.SideBar;

            float roll = rng.NextFloat() * total;
            for (int i = 0; i < kinds.Length; i++)
            {
                roll -= weights[i];
                if (roll < 0f && weights[i] > 0f)
                    return kinds[i];
            }

            return RowKind.SideBar;
        }

        private static float PickHeight(Rng rng, float difficulty, bool allowSevere)
        {
            // shins, thighs, hips, chest, head
            float hips = allowSevere ? 0.4f + 5f * difficulty : 0f;
            float chest = allowSevere ? 0.2f + 4f * difficulty : 0f;
            float[] weights = { 3f, 3f, hips, chest, 3f };
            return BarHeights[PickIndex(rng, weights)];
        }

        private static float PickFrom(Rng rng, params float[] heights)
        {
            return heights[rng.Range(0, heights.Length)];
        }

        private static int PickIndex(Rng rng, float[] weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Length; i++)
                total += weights[i];

            float roll = rng.NextFloat() * total;
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= weights[i];
                if (roll < 0f && weights[i] > 0f)
                    return i;
            }

            return 0;
        }

        private static void BuildRow(LevelPlan plan, RowKind kind, float z, int side, Rng rng, int level, float difficulty)
        {
            switch (kind)
            {
                case RowKind.Wall:
                {
                    float y = PickWallHeight(rng, difficulty);
                    plan.Obstacles.Add(new ObstacleSpec
                    {
                        Z = z, X = 0f, Y = y, Width = BarLength, Kind = ObstacleKind.Static, BlocksLane = true
                    });
                    break;
                }

                case RowKind.Needle:
                {
                    float gap = Lerp(1.5f, MinGapWidth, difficulty);
                    float cover = LaneWidth - gap;
                    float left = cover * rng.Range(0.25f, 0.75f);
                    float right = cover - left;
                    plan.Obstacles.Add(SideBarSpec(z, -1, left, PickHeight(rng, difficulty, true)));
                    plan.Obstacles.Add(SideBarSpec(z, +1, right, PickHeight(rng, difficulty, true)));
                    break;
                }

                case RowKind.Slider:
                {
                    float width = Lerp(2.0f, 1.4f, difficulty);
                    plan.Obstacles.Add(new ObstacleSpec
                    {
                        Z = z,
                        X = 0f,
                        Y = PickFrom(rng, 0.40f, 0.74f, 1.64f),
                        Width = width,
                        Kind = ObstacleKind.Sliding,
                        SlideRange = 1.0f,
                        SlideSpeed = Math.Min(1.6f + 0.12f * level, 3.2f),
                        SlidePhase = rng.Range(0f, 6.2831853f),
                        BlocksLane = false
                    });
                    break;
                }

                case RowKind.HighBar:
                {
                    float y = rng.Chance(0.25f + 0.5f * difficulty) ? 1.30f : 1.64f;
                    plan.Obstacles.Add(SideBarSpec(z, side, rng.Range(1.2f, 2.0f), y));
                    break;
                }

                case RowKind.LowBar:
                {
                    float y = rng.Chance(0.5f) ? 0.40f : 0.74f;
                    plan.Obstacles.Add(SideBarSpec(z, side, rng.Range(1.2f, 2.0f), y));
                    break;
                }

                default: // SideBar
                {
                    float coverage = rng.Range(1.0f, 1.0f + 0.9f * (0.4f + 0.6f * difficulty));
                    plan.Obstacles.Add(SideBarSpec(z, side, coverage, PickHeight(rng, difficulty, true)));
                    break;
                }
            }
        }

        private static float PickWallHeight(Rng rng, float difficulty)
        {
            // Early walls only take legs or the head; chest / hips walls appear on later levels.
            if (difficulty > 0.6f)
                return PickFrom(rng, 0.40f, 0.74f, 0.99f, 1.30f, 1.64f);
            if (difficulty > 0.3f)
                return PickFrom(rng, 0.40f, 0.74f, 0.99f, 1.64f);
            return PickFrom(rng, 0.40f, 0.74f, 1.64f);
        }

        /// <summary>
        /// A full length bar entering the lane from one side and covering <paramref name="coverage"/> units of it.
        /// </summary>
        private static ObstacleSpec SideBarSpec(float z, int side, float coverage, float y)
        {
            float innerEdge = side * (LaneHalfWidth - coverage);
            return new ObstacleSpec
            {
                Z = z,
                X = innerEdge + side * BarLength * 0.5f,
                Y = y,
                Width = BarLength,
                Kind = ObstacleKind.Static,
                BlocksLane = false
            };
        }

        // ---- Loot placement -------------------------------------------------------------------

        private static void AddLoot(LevelPlan plan, Rng rng, float z)
        {
            plan.Loot.Add(new LootSpec
            {
                Z = z,
                X = rng.Range(-1.1f, 1.1f),
                Y = LootHeight + rng.Range(-0.1f, 0.1f),
                ColorIndex = (plan.Loot.Count + plan.Level) % LootColorCount
            });
        }

        private static bool FarFromBars(LevelPlan plan, float z)
        {
            for (int i = 0; i < plan.Obstacles.Count; i++)
            {
                if (Math.Abs(plan.Obstacles[i].Z - z) < LootClearance)
                    return false;
            }

            return true;
        }

        private static bool FarFromLoot(LevelPlan plan, float z, float distance)
        {
            for (int i = 0; i < plan.Loot.Count; i++)
            {
                if (Math.Abs(plan.Loot[i].Z - z) < distance)
                    return false;
            }

            return true;
        }

        /// <summary>Puts loot in the middle of the widest bar free stretch that is not yet supplied.</summary>
        private static bool AddLootInWidestGap(LevelPlan plan, Rng rng)
        {
            // Collect obstacle rows (distinct Z) plus the start / finish as limits.
            var rows = new List<float> { plan.StartZ + 4f };
            for (int i = 0; i < plan.Obstacles.Count; i++)
            {
                float oz = plan.Obstacles[i].Z;
                if (Math.Abs(oz - rows[rows.Count - 1]) > 0.01f)
                    rows.Add(oz);
            }
            rows.Add(plan.FinishZ - 6f);

            float bestScore = 0f;
            float bestZ = 0f;
            bool found = false;

            for (int i = 0; i + 1 < rows.Count; i++)
            {
                float a = rows[i] + LootClearance + 0.25f;
                float b = rows[i + 1] - LootClearance - 0.25f;
                if (b <= a)
                    continue;

                float mid = (a + b) * 0.5f;
                if (!FarFromLoot(plan, mid, 4f))
                    continue;

                // prefer the candidate that is furthest from existing loot
                float nearest = 1000f;
                for (int k = 0; k < plan.Loot.Count; k++)
                    nearest = Math.Min(nearest, Math.Abs(plan.Loot[k].Z - mid));

                if (!found || nearest > bestScore)
                {
                    found = true;
                    bestScore = nearest;
                    bestZ = mid;
                }
            }

            if (!found)
                return false;

            AddLoot(plan, rng, bestZ);
            return true;
        }

        private static void SortByZ(LevelPlan plan)
        {
            // Insertion sort: stable and the lists are tiny.
            for (int i = 1; i < plan.Obstacles.Count; i++)
            {
                var item = plan.Obstacles[i];
                int j = i - 1;
                while (j >= 0 && plan.Obstacles[j].Z > item.Z)
                {
                    plan.Obstacles[j + 1] = plan.Obstacles[j];
                    j--;
                }
                plan.Obstacles[j + 1] = item;
            }

            for (int i = 1; i < plan.Loot.Count; i++)
            {
                var item = plan.Loot[i];
                int j = i - 1;
                while (j >= 0 && plan.Loot[j].Z > item.Z)
                {
                    plan.Loot[j + 1] = plan.Loot[j];
                    j--;
                }
                plan.Loot[j + 1] = item;
            }
        }

        // ---- Analysis helpers (used by tests and by the runtime sanity check) -----------------

        /// <summary>
        /// Largest free lateral interval inside the lane for the static bars whose Z is within
        /// <paramref name="tolerance"/> of <paramref name="z"/>.
        /// </summary>
        public static float LargestFreeGap(LevelPlan plan, float z, float tolerance = 0.5f)
        {
            var covered = new List<float[]>();
            for (int i = 0; i < plan.Obstacles.Count; i++)
            {
                var o = plan.Obstacles[i];
                if (o.Kind != ObstacleKind.Static || Math.Abs(o.Z - z) > tolerance)
                    continue;

                float min = Math.Max(-LaneHalfWidth, o.MinX);
                float max = Math.Min(LaneHalfWidth, o.MaxX);
                if (max > min)
                    covered.Add(new[] { min, max });
            }

            covered.Sort((a, b) => a[0].CompareTo(b[0]));

            float cursor = -LaneHalfWidth;
            float best = 0f;
            for (int i = 0; i < covered.Count; i++)
            {
                best = Math.Max(best, covered[i][0] - cursor);
                cursor = Math.Max(cursor, covered[i][1]);
            }

            best = Math.Max(best, LaneHalfWidth - cursor);
            return best;
        }

        /// <summary>Returns a human readable list of rule violations (empty when the plan is fair and well formed).</summary>
        public static List<string> Validate(LevelPlan plan)
        {
            var problems = new List<string>();
            if (plan == null)
            {
                problems.Add("plan is null");
                return problems;
            }

            if (plan.FinishZ <= plan.StartZ)
                problems.Add("finish is not after the start");

            float lastZ = float.MinValue;
            int walls = 0;
            for (int i = 0; i < plan.Obstacles.Count; i++)
            {
                var o = plan.Obstacles[i];
                if (o.Z < lastZ)
                    problems.Add("obstacles are not sorted by Z at index " + i);
                lastZ = o.Z;

                if (o.Z < plan.StartZ + 5f || o.Z > plan.FinishZ - RunOut + 0.01f)
                    problems.Add("obstacle " + i + " is outside the playable stretch (z=" + o.Z + ")");

                if (o.Width <= 0.1f)
                    problems.Add("obstacle " + i + " has no width");

                if (o.BlocksLane)
                    walls++;
                else if (o.Kind == ObstacleKind.Static && LargestFreeGap(plan, o.Z) < MinGapWidth - 0.001f)
                    problems.Add("obstacle row at z=" + o.Z + " leaves less than the minimum gap");
            }

            if (walls > MaxWalls(plan.Level))
                problems.Add("too many undodgeable walls: " + walls);

            for (int i = 0; i < plan.Loot.Count; i++)
            {
                var l = plan.Loot[i];
                if (Math.Abs(l.X) > LaneHalfWidth - 0.2f)
                    problems.Add("loot " + i + " is not inside the lane");

                if (!FarFromBars(plan, l.Z))
                    problems.Add("loot " + i + " overlaps a bar (z=" + l.Z + ")");

                if (l.Z < plan.StartZ || l.Z > plan.FinishZ)
                    problems.Add("loot " + i + " is outside the track");

                if (l.ColorIndex < 0 || l.ColorIndex >= LootColorCount)
                    problems.Add("loot " + i + " has an invalid colour index");
            }

            // Every wall must have loot within MaxSupplyDistance before it (start counts as supplied) and right after it.
            for (int i = 0; i < plan.Obstacles.Count; i++)
            {
                var o = plan.Obstacles[i];
                if (!o.BlocksLane)
                    continue;

                float before = plan.StartZ;
                float after = float.MaxValue;
                for (int k = 0; k < plan.Loot.Count; k++)
                {
                    float lz = plan.Loot[k].Z;
                    if (lz < o.Z)
                        before = Math.Max(before, lz);
                    else
                        after = Math.Min(after, lz);
                }

                if (o.Z - before > MaxSupplyDistance + 0.01f)
                    problems.Add("wall at z=" + o.Z + " is not supplied with loot before it");

                if (after - o.Z > 20f)
                    problems.Add("wall at z=" + o.Z + " has no recovery loot after it");
            }

            if (plan.Loot.Count < MinLoot(plan.Level))
                problems.Add("not enough loot: " + plan.Loot.Count);

            return problems;
        }

        // ---- Math helpers ---------------------------------------------------------------------

        private static float Clamp(float v, float min, float max)
        {
            return v < min ? min : (v > max ? max : v);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
