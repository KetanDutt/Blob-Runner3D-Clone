using BlobRunner.Core;
using NUnit.Framework;

namespace BlobRunner.Tests
{
    public class RngTests
    {
        [Test]
        public void SameSeedProducesSameSequence()
        {
            var a = new Rng(1234);
            var b = new Rng(1234);
            for (int i = 0; i < 200; i++)
                Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }

        [Test]
        public void DifferentSeedsProduceDifferentSequences()
        {
            var a = new Rng(1);
            var b = new Rng(2);
            int same = 0;
            for (int i = 0; i < 100; i++)
                if (a.NextUInt() == b.NextUInt()) same++;
            Assert.Less(same, 3);
        }

        [Test]
        public void ZeroSeedIsUsable()
        {
            var rng = new Rng(0);
            Assert.AreNotEqual(0u, rng.NextUInt());
        }

        [Test]
        public void NextFloatStaysInUnitRange()
        {
            var rng = new Rng(7);
            for (int i = 0; i < 10000; i++)
            {
                float v = rng.NextFloat();
                Assert.True(v >= 0f && v < 1f, "value " + v);
            }
        }

        [Test]
        public void RangesAreRespected()
        {
            var rng = new Rng(99);
            for (int i = 0; i < 5000; i++)
            {
                float f = rng.Range(-2.5f, 4f);
                Assert.True(f >= -2.5f && f < 4f);
                int n = rng.Range(3, 9);
                Assert.True(n >= 3 && n < 9);
            }

            Assert.AreEqual(5, rng.Range(5, 5));
        }

        [Test]
        public void SignProducesBothValues()
        {
            var rng = new Rng(5);
            int plus = 0;
            int minus = 0;
            for (int i = 0; i < 400; i++)
            {
                if (rng.Sign() > 0) plus++; else minus++;
            }

            Assert.Greater(plus, 120);
            Assert.Greater(minus, 120);
        }

        [Test]
        public void DistributionIsRoughlyUniform()
        {
            var rng = new Rng(2024);
            var buckets = new int[10];
            const int samples = 20000;
            for (int i = 0; i < samples; i++)
                buckets[(int)(rng.NextFloat() * 10f)]++;

            foreach (int b in buckets)
            {
                Assert.Greater(b, samples / 10 * 0.85);
                Assert.Less(b, samples / 10 * 1.15);
            }
        }
    }
}
