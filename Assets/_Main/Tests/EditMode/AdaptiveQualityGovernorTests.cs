using BlobRunner.Core;
using NUnit.Framework;

namespace BlobRunner.Tests
{
    public class AdaptiveQualityGovernorTests
    {
        private static void Feed(AdaptiveQualityGovernor governor, float fps, float seconds)
        {
            float dt = 1f / fps;
            for (float t = 0; t < seconds; t += dt)
                governor.Tick(dt);
        }

        [Test]
        public void StartsAtFullResolution()
        {
            Assert.AreEqual(1f, new AdaptiveQualityGovernor().Scale, 0.0001);
        }

        [Test]
        public void StableSixtyFpsNeverReducesQuality()
        {
            var governor = new AdaptiveQualityGovernor(60f);
            Feed(governor, 60f, 120f);
            Assert.AreEqual(1f, governor.Scale, 0.0001);
            Assert.AreEqual(0, governor.Downgrades);
        }

        [Test]
        public void SlowDevicesAreSteppedDownUntilTheMinimum()
        {
            var governor = new AdaptiveQualityGovernor(60f, 0.6f, 1f, 0.1f);
            Feed(governor, 30f, 90f);
            Assert.AreEqual(0.6f, governor.Scale, 0.0001);
        }

        [Test]
        public void SingleSlowWindowDoesNotTriggerAStep()
        {
            var governor = new AdaptiveQualityGovernor(60f);
            Feed(governor, 30f, 2.5f);
            Assert.AreEqual(1f, governor.Scale, 0.0001);
        }

        [Test]
        public void HitchesAreIgnored()
        {
            var governor = new AdaptiveQualityGovernor(60f);
            for (int i = 0; i < 100; i++)
            {
                governor.Tick(1f);     // scene load / app switch
                governor.Tick(0.5f);
            }

            Assert.AreEqual(1f, governor.Scale, 0.0001);
        }

        [Test]
        public void InvalidFrameTimesAreIgnored()
        {
            var governor = new AdaptiveQualityGovernor(60f);
            Assert.False(governor.Tick(0f));
            Assert.False(governor.Tick(-1f));
            Assert.False(governor.Tick(float.NaN));
        }

        [Test]
        public void RecoversOnceAfterALongStablePeriod()
        {
            var governor = new AdaptiveQualityGovernor(60f, 0.6f, 1f, 0.1f);
            Feed(governor, 30f, 6f);              // one downgrade
            float reduced = governor.Scale;
            Assert.Less(reduced, 1f);

            Feed(governor, 60f, 40f);
            Assert.Greater(governor.Scale, reduced);
        }

        [Test]
        public void StopsRaisingAfterTwoDowngrades()
        {
            var governor = new AdaptiveQualityGovernor(60f, 0.5f, 1f, 0.1f);
            Feed(governor, 30f, 12f);             // several downgrades
            Assert.GreaterOrEqual(governor.Downgrades, 2);

            Feed(governor, 60f, 5f);              // let the measuring window settle (a mixed window may still count as slow)
            float scale = governor.Scale;

            Feed(governor, 60f, 120f);            // minutes of perfect frames must not raise the resolution again
            Assert.AreEqual(scale, governor.Scale, 0.0001);
        }

        [Test]
        public void ScaleStaysWithinBounds()
        {
            var governor = new AdaptiveQualityGovernor(60f, 0.7f, 0.95f, 0.1f);
            Assert.AreEqual(0.95f, governor.Scale, 0.0001);
            Feed(governor, 20f, 200f);
            Assert.GreaterOrEqual(governor.Scale, 0.7f - 0.0001f);
            Assert.LessOrEqual(governor.Scale, 0.95f + 0.0001f);
        }

        [Test]
        public void ResetRestoresFullResolution()
        {
            var governor = new AdaptiveQualityGovernor(60f);
            Feed(governor, 25f, 30f);
            Assert.Less(governor.Scale, 1f);
            governor.Reset();
            Assert.AreEqual(1f, governor.Scale, 0.0001);
            Assert.AreEqual(0, governor.Downgrades);
        }
    }
}
