using System;
using System.Collections.Generic;

namespace BlobRunner.Core
{
    /// <summary>
    /// Small statistical model of the cut / regrow rules used to balance generated levels.
    ///
    /// A simulated player dodges every dodgeable row with a given probability ("skill"); whatever it does not dodge
    /// cuts off body parts according to the bar height, loot regrows everything, and the run is lost when all 11 parts
    /// are gone. Averaging a few hundred such runs gives the chance that a player of that skill loses the level.
    /// <see cref="LevelGenerator"/> uses it to pick, among several candidates, the layout that is closest to
    /// <see cref="TargetDeathChance"/> - so the difficulty rises smoothly instead of depending on the luck of one draw.
    /// </summary>
    public static class DifficultyModel
    {
        public const int PartCount = 11;

        // One bit per body part.
        private const int Head = 1 << 0;
        private const int TorsoUpper = 1 << 1;
        private const int TorsoLower = 1 << 2;
        private const int LeftArmUpper = 1 << 3;
        private const int LeftArmLower = 1 << 4;
        private const int RightArmUpper = 1 << 5;
        private const int RightArmLower = 1 << 6;
        private const int LeftLegUpper = 1 << 7;
        private const int LeftLegLower = 1 << 8;
        private const int RightLegUpper = 1 << 9;
        private const int RightLegLower = 1 << 10;

        private const int Arms = LeftArmUpper | LeftArmLower | RightArmUpper | RightArmLower;
        private const int Legs = LeftLegUpper | LeftLegLower | RightLegUpper | RightLegLower;

        public const int AllParts = (1 << PartCount) - 1;

        /// <summary>Skill of the reference player the levels are balanced for (dodges 75% of the bars it could dodge).</summary>
        public const float ReferenceSkill = 0.75f;

        private const float LootSkill = 0.92f;

        /// <summary>
        /// Parts that come off when a bar at <paramref name="barHeight"/> hits an upright blob:
        /// shins (both lower legs), thighs (legs), hips (torso + legs), chest (torso + arms + head), head.
        /// Mirrors the cascades of <c>Player.prefab</c> (documented in docs/GAMEPLAY.md).
        /// </summary>
        public static int PartsCutMask(float barHeight)
        {
            if (barHeight < 0.55f) return LeftLegLower | RightLegLower;
            if (barHeight < 0.87f) return Legs;
            if (barHeight < 1.15f) return TorsoLower | Legs;
            if (barHeight < 1.50f) return TorsoUpper | Arms | Head;
            return Head;
        }

        /// <summary>Chance (0..1) that the reference player loses a level of the given number. 0 on the first levels, ~38% from level 24.</summary>
        public static float TargetDeathChance(int level)
        {
            float t = (Math.Max(1, level) - 2) / 22f;
            if (t <= 0f) return 0f;
            if (t > 1f) t = 1f;
            return 0.38f * (float)Math.Pow(t, 0.8);
        }

        public static int CountBits(int mask)
        {
            int n = 0;
            while (mask != 0)
            {
                mask &= mask - 1;
                n++;
            }

            return n;
        }

        /// <summary>Estimated chance (0..1) that a player with the given dodge skill loses the level.</summary>
        public static float EstimateDeathChance(LevelPlan plan, float dodgeSkill, int samples, int seed)
        {
            if (plan == null || samples <= 0)
                return 0f;

            // Merge obstacles into rows (same Z) and interleave them with the loot, ordered along the track.
            var events = new List<Evt>();
            var rowStart = 0;
            while (rowStart < plan.Obstacles.Count)
            {
                float z = plan.Obstacles[rowStart].Z;
                var evt = new Evt { Z = z, IsLoot = false, Bars = new List<ObstacleSpec>() };
                int i = rowStart;
                while (i < plan.Obstacles.Count && Math.Abs(plan.Obstacles[i].Z - z) < 0.01f)
                {
                    evt.Bars.Add(plan.Obstacles[i]);
                    i++;
                }

                events.Add(evt);
                rowStart = i;
            }

            for (int i = 0; i < plan.Loot.Count; i++)
                events.Add(new Evt { Z = plan.Loot[i].Z, IsLoot = true });

            events.Sort((a, b) => a.Z.CompareTo(b.Z));

            var rng = new Rng(seed);
            int deaths = 0;

            for (int run = 0; run < samples; run++)
            {
                int alive = AllParts;
                for (int e = 0; e < events.Count && alive != 0; e++)
                {
                    var evt = events[e];
                    if (evt.IsLoot)
                    {
                        if (rng.NextFloat() < LootSkill)
                            alive = AllParts;
                        continue;
                    }

                    alive &= ~CutByRow(evt.Bars, dodgeSkill, rng);
                }

                if (alive == 0)
                    deaths++;
            }

            return (float)deaths / samples;
        }

        private static int CutByRow(List<ObstacleSpec> bars, float skill, Rng rng)
        {
            bool wall = false;
            bool sliding = false;
            for (int i = 0; i < bars.Count; i++)
            {
                wall |= bars[i].BlocksLane;
                sliding |= bars[i].Kind == ObstacleKind.Sliding;
            }

            if (!wall)
            {
                float dodge = skill;
                if (sliding) dodge *= 0.88f;
                if (bars.Count >= 2) dodge *= 0.9f; // needle: precise steering needed

                if (rng.NextFloat() < dodge)
                    return 0;

                // a needle that was not threaded cuts one of its two bars
                if (bars.Count >= 2)
                    return PartsCutMask(bars[rng.Range(0, bars.Count)].Y);
            }

            int mask = 0;
            for (int i = 0; i < bars.Count; i++)
                mask |= PartsCutMask(bars[i].Y);
            return mask;
        }

        private struct Evt
        {
            public float Z;
            public bool IsLoot;
            public List<ObstacleSpec> Bars;
        }
    }
}
