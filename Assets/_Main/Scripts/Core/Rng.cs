namespace BlobRunner.Core
{
    /// <summary>
    /// Small deterministic pseudo random generator (xorshift32).
    /// Unlike <see cref="System.Random"/> the sequence is guaranteed to be identical on every
    /// runtime (Mono, IL2CPP, .NET), which keeps generated levels and synthesized audio reproducible.
    /// </summary>
    public sealed class Rng
    {
        private uint _state;

        public Rng(int seed)
        {
            // Mix the seed (splitmix-like) so that small consecutive seeds do not produce similar streams.
            uint z = unchecked((uint)seed + 0x9E3779B9u);
            z = unchecked((z ^ (z >> 16)) * 0x85EBCA6Bu);
            z = unchecked((z ^ (z >> 13)) * 0xC2B2AE35u);
            z ^= z >> 16;
            _state = z == 0 ? 0x1234ABCDu : z;
        }

        /// <summary>Next raw 32 bit value.</summary>
        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>Uniform float in [0, 1).</summary>
        public float NextFloat()
        {
            // 24 random bits -> exactly representable float mantissa.
            return (NextUInt() >> 8) * (1.0f / 16777216.0f);
        }

        /// <summary>Uniform float in [min, max).</summary>
        public float Range(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                return minInclusive;

            uint span = unchecked((uint)(maxExclusive - minInclusive));
            return unchecked((int)(NextUInt() % span)) + minInclusive;
        }

        /// <summary>True with the given probability (0..1).</summary>
        public bool Chance(float probability)
        {
            return NextFloat() < probability;
        }

        /// <summary>-1 or +1 with equal probability.</summary>
        public int Sign()
        {
            return (NextUInt() & 0x80000000u) == 0 ? 1 : -1;
        }

        /// <summary>Uniform float in [-1, 1).</summary>
        public float Signed()
        {
            return NextFloat() * 2f - 1f;
        }
    }
}
