using System.Collections.Generic;

namespace BlobRunner.Core
{
    public enum ObstacleKind
    {
        /// <summary>A bar that stays where it is placed.</summary>
        Static = 0,

        /// <summary>A bar that sweeps sideways across the lane.</summary>
        Sliding = 1
    }

    /// <summary>Describes one horizontal bar the blob has to dodge (or lose parts to).</summary>
    public sealed class ObstacleSpec
    {
        /// <summary>Position along the track.</summary>
        public float Z;

        /// <summary>Lateral position of the bar centre (centre of the sweep for sliding bars).</summary>
        public float X;

        /// <summary>Height of the bar centre above the floor.</summary>
        public float Y;

        /// <summary>Length of the bar along X.</summary>
        public float Width;

        public ObstacleKind Kind;

        /// <summary>Half distance of the sweep (sliding bars only).</summary>
        public float SlideRange;

        /// <summary>Angular speed of the sweep in radians / second (sliding bars only).</summary>
        public float SlideSpeed;

        /// <summary>Initial phase of the sweep in radians (sliding bars only).</summary>
        public float SlidePhase;

        /// <summary>True when the bar spans the full lane and cannot be dodged.</summary>
        public bool BlocksLane;

        /// <summary>Left / right edge of the (static) bar.</summary>
        public float MinX { get { return X - Width * 0.5f; } }

        public float MaxX { get { return X + Width * 0.5f; } }
    }

    /// <summary>A pickup that regrows every cut off body part.</summary>
    public sealed class LootSpec
    {
        public float Z;
        public float X;
        public float Y;

        /// <summary>Index into the colour palette of the game layer.</summary>
        public int ColorIndex;
    }

    /// <summary>Complete, engine independent description of a level.</summary>
    public sealed class LevelPlan
    {
        public int Level;
        public int Seed;
        public int ThemeIndex;
        public float StartZ;
        public float FinishZ;
        public float PlayerSpeed;

        public readonly List<ObstacleSpec> Obstacles = new List<ObstacleSpec>();
        public readonly List<LootSpec> Loot = new List<LootSpec>();

        public float Length { get { return FinishZ - StartZ; } }

        /// <summary>0..1 progress of a Z coordinate along the track.</summary>
        public float ProgressAt(float z)
        {
            float length = Length;
            if (length <= 0.0001f)
                return 1f;

            float p = (z - StartZ) / length;
            return p < 0f ? 0f : (p > 1f ? 1f : p);
        }
    }
}
