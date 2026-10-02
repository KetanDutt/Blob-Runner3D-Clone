using System;
using BlobRunner.Core;
using NUnit.Framework;

namespace BlobRunner.Tests
{
    public class ProceduralSynthTests
    {
        private static float Peak(float[] s)
        {
            float peak = 0f;
            for (int i = 0; i < s.Length; i++)
                peak = Math.Max(peak, Math.Abs(s[i]));
            return peak;
        }

        private static double Rms(float[] s)
        {
            double sum = 0;
            for (int i = 0; i < s.Length; i++)
                sum += s[i] * s[i];
            return Math.Sqrt(sum / Math.Max(1, s.Length));
        }

        /// <summary>Signal power at one frequency (Goertzel algorithm) over a window of the buffer.</summary>
        private static double PowerAt(float[] s, int from, int count, double hz, int sampleRate)
        {
            double w = 2.0 * Math.PI * hz / sampleRate;
            double coeff = 2.0 * Math.Cos(w);
            double s1 = 0;
            double s2 = 0;
            for (int i = from; i < from + count; i++)
            {
                double s0 = s[i] + coeff * s1 - s2;
                s2 = s1;
                s1 = s0;
            }

            return (s1 * s1 + s2 * s2 - coeff * s1 * s2) / (count * (double)count);
        }

        [Test]
        public void EveryEffectIsAudibleBoundedAndFinite()
        {
            foreach (SfxKind kind in Enum.GetValues(typeof(SfxKind)))
            {
                float[] samples = ProceduralSynth.Render(kind);
                double seconds = samples.Length / (double)ProceduralSynth.DefaultSampleRate;

                Assert.Greater(seconds, 0.05, kind + " is too short");
                Assert.Less(seconds, 3.0, kind + " is too long");
                Assert.LessOrEqual(Peak(samples), 1.0f, kind + " clips");
                Assert.Greater(Peak(samples), 0.3f, kind + " is too quiet");
                Assert.Greater(Rms(samples), 0.01, kind + " is practically silent");

                for (int i = 0; i < samples.Length; i++)
                    Assert.True(!float.IsNaN(samples[i]) && !float.IsInfinity(samples[i]), kind + " has a non finite sample");
            }
        }

        [Test]
        public void EffectsStartAndEndSilentlyToAvoidClicks()
        {
            foreach (SfxKind kind in Enum.GetValues(typeof(SfxKind)))
            {
                float[] samples = ProceduralSynth.Render(kind);
                Assert.Less(Math.Abs(samples[0]), 0.06f, kind + " starts with a click");
                Assert.Less(Math.Abs(samples[samples.Length - 1]), 0.02f, kind + " ends with a click");
            }
        }

        [Test]
        public void RenderingIsDeterministic()
        {
            foreach (SfxKind kind in Enum.GetValues(typeof(SfxKind)))
            {
                float[] a = ProceduralSynth.Render(kind);
                float[] b = ProceduralSynth.Render(kind);
                Assert.AreEqual(a.Length, b.Length);
                for (int i = 0; i < a.Length; i += 97)
                    Assert.AreEqual(a[i], b[i]);
            }
        }

        [Test]
        public void SampleRateChangesTheLengthButNotTheDuration()
        {
            float[] low = ProceduralSynth.Render(SfxKind.Collect, 22050);
            float[] high = ProceduralSynth.Render(SfxKind.Collect, 44100);
            Assert.AreEqual(low.Length * 2.0, high.Length, 4.0);
        }

        [Test]
        public void CollectJingleStartsOnE5()
        {
            // E5 = 659.25 Hz. In the first 70 ms only that note is playing.
            const int rate = ProceduralSynth.DefaultSampleRate;
            float[] samples = ProceduralSynth.Render(SfxKind.Collect, rate);
            int from = (int)(0.004 * rate);
            int count = (int)(0.066 * rate);

            double target = PowerAt(samples, from, count, 659.25, rate);
            double offPitch = PowerAt(samples, from, count, 880.0, rate);
            double farAway = PowerAt(samples, from, count, 3000.0, rate);

            Assert.Greater(target, offPitch * 8.0, "E5 should dominate A5");
            Assert.Greater(target, farAway * 50.0, "E5 should dominate 3 kHz");
        }

        [Test]
        public void StarBellRingsAtA5()
        {
            const int rate = ProceduralSynth.DefaultSampleRate;
            float[] samples = ProceduralSynth.Render(SfxKind.Star, rate);
            int from = (int)(0.01 * rate);
            int count = (int)(0.15 * rate);

            Assert.Greater(PowerAt(samples, from, count, 880.0, rate), PowerAt(samples, from, count, 600.0, rate) * 20.0);
        }

        [Test]
        public void MusicLoopHasTheExpectedLengthAndLevels()
        {
            const int rate = ProceduralSynth.DefaultSampleRate;
            var renderer = new ProceduralSynth.MusicRenderer(rate);
            int beat = (int)Math.Round(rate * 60.0 / ProceduralSynth.MusicRenderer.Bpm);
            Assert.AreEqual(beat * 4 * ProceduralSynth.MusicRenderer.Bars, renderer.LoopSamples);

            while (!renderer.Step(64))
            {
            }

            float[] loop = renderer.Result;
            Assert.AreEqual(renderer.LoopSamples, loop.Length);
            Assert.AreEqual(1f, renderer.Progress, 0.0001);
            Assert.LessOrEqual(Peak(loop), 1.0f);
            Assert.Greater(Peak(loop), 0.5f);
            Assert.Greater(Rms(loop), 0.04);
            Assert.Less(Rms(loop), 0.5);

            for (int i = 0; i < loop.Length; i++)
                Assert.True(!float.IsNaN(loop[i]) && !float.IsInfinity(loop[i]));
        }

        [Test]
        public void MusicLoopWrapsWithoutAClick()
        {
            float[] loop = ProceduralSynth.RenderMusicLoop();
            // the sample jump across the loop point must not be larger than the biggest jump anywhere else
            double maxStep = 0;
            for (int i = 1; i < loop.Length; i++)
                maxStep = Math.Max(maxStep, Math.Abs(loop[i] - loop[i - 1]));

            double seam = Math.Abs(loop[0] - loop[loop.Length - 1]);
            Assert.LessOrEqual(seam, Math.Max(maxStep, 0.05), "seam " + seam + " vs biggest step " + maxStep);
            Assert.Less(Math.Abs(loop[0]), 0.1f);
        }

        [Test]
        public void MusicRendererCanBeSteppedInSmallPieces()
        {
            var renderer = new ProceduralSynth.MusicRenderer();
            int steps = 0;
            while (!renderer.Step(7))
            {
                steps++;
                Assert.Less(steps, 10000, "renderer never finished");
            }

            Assert.Greater(steps, 5);
            Assert.True(renderer.Step(1), "stepping a finished renderer must stay finished");
        }

        [Test]
        public void MidiNoteToFrequency()
        {
            Assert.AreEqual(440.0, ProceduralSynth.MidiToHz(69), 0.0001);
            Assert.AreEqual(880.0, ProceduralSynth.MidiToHz(81), 0.0001);
            Assert.AreEqual(261.6256, ProceduralSynth.MidiToHz(60), 0.001);
        }

        [Test]
        public void WavEncoderWritesAValidHeader()
        {
            var samples = new[] { 0f, 0.5f, -0.5f, 1f, -1f, 2f };
            byte[] wav = ProceduralSynth.ToWav16(samples, 22050);

            Assert.AreEqual(44 + samples.Length * 2, wav.Length);
            Assert.AreEqual((byte)'R', wav[0]);
            Assert.AreEqual((byte)'I', wav[1]);
            Assert.AreEqual((byte)'F', wav[2]);
            Assert.AreEqual((byte)'F', wav[3]);
            Assert.AreEqual((byte)'W', wav[8]);
            Assert.AreEqual((byte)'d', wav[36]);
            Assert.AreEqual(22050, BitConverter.ToInt32(wav, 24));
            Assert.AreEqual(samples.Length * 2, BitConverter.ToInt32(wav, 40));
            Assert.AreEqual(16384, BitConverter.ToInt16(wav, 44 + 2), 2.0);
            Assert.AreEqual(32767, BitConverter.ToInt16(wav, 44 + 10), "values above 1 are clamped");
        }
    }
}
