using System;
using System.Collections.Generic;

namespace BlobRunner.Core
{
    /// <summary>The sound effects that are synthesized by <see cref="ProceduralSynth"/>.</summary>
    public enum SfxKind
    {
        Click = 0,
        Start = 1,
        Cut = 2,
        Regrow = 3,
        Collect = 4,
        Star = 5,
        Win = 6,
        Lose = 7,
        Whoosh = 8,
        Pop = 9
    }

    /// <summary>
    /// Tiny additive / noise synthesizer that renders every sound effect of the game as raw mono samples.
    ///
    /// Why synthesize? The repository ships no audio binaries, the result is fully deterministic and unit
    /// tested (no clipping, no clicks, correct pitch) and any effect can be replaced by dropping an
    /// <c>AudioClip</c> named like the <see cref="SfxKind"/> into <c>Resources/Audio</c>.
    /// The class has no engine dependency.
    /// </summary>
    public static class ProceduralSynth
    {
        public const int DefaultSampleRate = 22050;

        private const double TwoPi = Math.PI * 2.0;

        // ---- Public API -----------------------------------------------------------------------

        /// <summary>Renders one sound effect as mono samples in [-1, 1].</summary>
        public static float[] Render(SfxKind kind, int sampleRate = DefaultSampleRate)
        {
            var rng = new Rng(1000 + (int)kind * 31);

            switch (kind)
            {
                case SfxKind.Click: return RenderClick(sampleRate);
                case SfxKind.Pop: return RenderPop(sampleRate);
                case SfxKind.Start: return RenderStart(sampleRate, rng);
                case SfxKind.Cut: return RenderCut(sampleRate, rng);
                case SfxKind.Regrow: return RenderRegrow(sampleRate);
                case SfxKind.Collect: return RenderCollect(sampleRate);
                case SfxKind.Star: return RenderStar(sampleRate);
                case SfxKind.Win: return RenderWin(sampleRate, rng);
                case SfxKind.Lose: return RenderLose(sampleRate);
                case SfxKind.Whoosh: return RenderWhoosh(sampleRate, rng);
                default: return RenderClick(sampleRate);
            }
        }

        /// <summary>Renders the complete music loop in one call (tests / tooling). The game uses <see cref="MusicRenderer"/> to spread the work over frames.</summary>
        public static float[] RenderMusicLoop(int sampleRate = DefaultSampleRate)
        {
            var renderer = new MusicRenderer(sampleRate);
            while (!renderer.Step(1000))
            {
            }

            return renderer.Result;
        }

        /// <summary>Encodes mono samples as a 16 bit PCM WAV file.</summary>
        public static byte[] ToWav16(float[] samples, int sampleRate)
        {
            int dataBytes = samples.Length * 2;
            var bytes = new byte[44 + dataBytes];

            WriteAscii(bytes, 0, "RIFF");
            WriteInt(bytes, 4, 36 + dataBytes);
            WriteAscii(bytes, 8, "WAVE");
            WriteAscii(bytes, 12, "fmt ");
            WriteInt(bytes, 16, 16);
            WriteShort(bytes, 20, 1);            // PCM
            WriteShort(bytes, 22, 1);            // mono
            WriteInt(bytes, 24, sampleRate);
            WriteInt(bytes, 28, sampleRate * 2); // byte rate
            WriteShort(bytes, 32, 2);            // block align
            WriteShort(bytes, 34, 16);           // bits per sample
            WriteAscii(bytes, 36, "data");
            WriteInt(bytes, 40, dataBytes);

            for (int i = 0; i < samples.Length; i++)
            {
                float v = samples[i];
                if (v > 1f) v = 1f;
                if (v < -1f) v = -1f;
                short s = (short)Math.Round(v * 32767f);
                bytes[44 + i * 2] = (byte)(s & 0xFF);
                bytes[45 + i * 2] = (byte)((s >> 8) & 0xFF);
            }

            return bytes;
        }

        /// <summary>Equal tempered frequency of a MIDI note number (A4 = 69 = 440 Hz).</summary>
        public static double MidiToHz(int midi)
        {
            return 440.0 * Math.Pow(2.0, (midi - 69) / 12.0);
        }

        // ---- Sound effect recipes -------------------------------------------------------------

        private static float[] RenderClick(int sr)
        {
            var t = new Track(0.08, sr);
            t.Tone(0, 0.08, 1800, 0.60, new[] { 1.0, 1.5 }, new[] { 1.0, 0.4 }, 0.001, 0.012);
            return t.Finish(0.55f, false);
        }

        private static float[] RenderPop(int sr)
        {
            var t = new Track(0.16, sr);
            t.Sweep(0, 0.16, 880, 280, 0.8, 0.001, 0.04);
            return t.Finish(0.6f, false);
        }

        private static float[] RenderStart(int sr, Rng rng)
        {
            var t = new Track(0.55, sr);
            t.Noise(rng, 0, 0.5, f => 250 + 2400 * f, f => 120 + 700 * f, 0.8, 0.18, 0.28);
            t.Sweep(0, 0.45, 300, 900, 0.30, 0.10, 0.30);
            t.Tone(0.30, 0.25, 880, 0.30, new[] { 1.0, 2.0 }, new[] { 1.0, 0.3 }, 0.004, 0.10);
            return t.Finish(0.7f, false);
        }

        private static float[] RenderCut(int sr, Rng rng)
        {
            // squelchy pop = falling sine + dull noise burst + low thud
            var t = new Track(0.38, sr);
            t.Sweep(0, 0.30, 520, 90, 0.85, 0.002, 0.09);
            t.Noise(rng, 0, 0.22, f => 2200 - 1900 * f, f => 60, 0.55, 0.002, 0.06);
            t.Tone(0, 0.30, 68, 0.70, new[] { 1.0, 2.0 }, new[] { 1.0, 0.25 }, 0.004, 0.12);
            return t.Finish(0.9f, true);
        }

        private static float[] RenderRegrow(int sr)
        {
            // three rising bubbles
            var t = new Track(0.46, sr);
            double[] starts = { 0.0, 0.11, 0.21 };
            double[] bases = { 220.0, 277.0, 330.0 };
            double[] amps = { 0.9, 0.75, 0.6 };
            for (int i = 0; i < starts.Length; i++)
                t.Sweep(starts[i], 0.14, bases[i], bases[i] * 2.4, amps[i], 0.003, 0.06);
            return t.Finish(0.7f, false);
        }

        private static float[] RenderCollect(int sr)
        {
            // quick rising arpeggio of bell like notes: E5 G5 B5 E6
            var t = new Track(0.62, sr);
            int[] notes = { 76, 79, 83, 88 };
            for (int i = 0; i < notes.Length; i++)
                t.Tone(i * 0.075, 0.5, MidiToHz(notes[i]), 0.55, new[] { 1.0, 2.0, 3.0 }, new[] { 1.0, 0.35, 0.12 }, 0.002, 0.16, 1.5);
            return t.Finish(0.75f, false);
        }

        private static float[] RenderStar(int sr)
        {
            // glassy bell (inharmonic partials)
            var t = new Track(0.8, sr);
            t.Tone(0, 0.75, MidiToHz(81), 0.7, new[] { 1.0, 2.76, 5.40 }, new[] { 1.0, 0.45, 0.2 }, 0.001, 0.30, 2.5);
            return t.Finish(0.7f, false);
        }

        private static float[] RenderWin(int sr, Rng rng)
        {
            var t = new Track(2.0, sr);
            var brass = new[] { 1.0, 2.0, 3.0, 4.0 };
            var brassAmps = new[] { 1.0, 0.5, 0.33, 0.2 };

            int[] run = { 72, 76, 79, 84 }; // C5 E5 G5 C6
            for (int i = 0; i < run.Length; i++)
                t.Tone(i * 0.14, 0.26, MidiToHz(run[i]), 0.40, brass, brassAmps, 0.01, 0.25);

            int[] chord = { 72, 76, 79, 84 };
            for (int i = 0; i < chord.Length; i++)
                t.Tone(0.60, 1.30, MidiToHz(chord[i]), 0.26, brass, brassAmps, 0.015, 0.75, 1.0);

            // sparkles
            for (int i = 0; i < 7; i++)
            {
                double start = 0.62 + i * 0.17 + rng.Range(0f, 0.06f);
                double hz = 2000.0 + rng.Range(0f, 2200f);
                t.Tone(start, 0.14, hz, 0.12, new[] { 1.0 }, new[] { 1.0 }, 0.001, 0.05);
            }

            return t.Finish(0.88f, false);
        }

        private static float[] RenderLose(int sr)
        {
            var t = new Track(1.2, sr);
            // falling "wah" slide G4 -> G3 with vibrato
            Func<double, double> freq = x => 392.0 * Math.Pow(0.5, Math.Min(1.0, x / 0.85)) * (1.0 + 0.03 * Math.Sin(TwoPi * 5.5 * x));
            t.Tone(0, 1.0, freq, 0.55, new[] { 1.0, 2.0, 3.0, 4.0 }, new[] { 1.0, 0.5, 0.3, 0.2 }, 0.02, 0.9);
            t.Tone(0.78, 0.4, 58, 0.8, new[] { 1.0, 2.0 }, new[] { 1.0, 0.3 }, 0.004, 0.14);
            t.LowPass(f => 2400 - 2000 * Math.Min(1.0, f * 1.1));
            return t.Finish(0.8f, false);
        }

        private static float[] RenderWhoosh(int sr, Rng rng)
        {
            var t = new Track(0.55, sr);
            t.Noise(rng, 0, 0.55, f => 300 + 2200 * (1.0 - Math.Abs(2.0 * f - 1.0)), f => 100, 1.0, 0.18, 0.5);
            return t.Finish(0.6f, false);
        }

        // ---- Music ----------------------------------------------------------------------------

        /// <summary>
        /// Incremental renderer of a 8 bar C - Am - F - G loop (bass, arpeggio, pad, light drums at 112 bpm).
        /// Call <see cref="Step"/> once per frame until it returns true to avoid hitches on mobile devices.
        /// Notes that ring past the end of the loop wrap around to the beginning, so it loops seamlessly.
        /// </summary>
        public sealed class MusicRenderer
        {
            public const int Bpm = 112;
            public const int Bars = 8;

            private struct Evt
            {
                public int Kind;       // 0 bass, 1 arp, 2 pad, 3 kick, 4 snare, 5 hat
                public int Start;      // sample
                public double Hz;
                public double Amp;
                public double Length;  // seconds
            }

            private readonly int _rate;
            private readonly Track _track;
            private readonly List<Evt> _events = new List<Evt>();
            private readonly Rng _rng;
            private int _next;
            private bool _finished;

            public int LoopSamples { get; private set; }

            /// <summary>Valid once <see cref="Step"/> returned true.</summary>
            public float[] Result { get { return _track.Data; } }

            public float Progress { get { return _events.Count == 0 ? 1f : (float)_next / _events.Count; } }

            public MusicRenderer(int sampleRate = DefaultSampleRate)
            {
                _rate = sampleRate;
                _rng = new Rng(4242);

                int beat = (int)Math.Round(sampleRate * 60.0 / Bpm);
                int bar = beat * 4;
                LoopSamples = bar * Bars;
                _track = new Track(LoopSamples, sampleRate, true);

                BuildEvents(beat, bar);
            }

            private void BuildEvents(int beat, int bar)
            {
                // root (bass) and chord tones (arp, one octave above the pad) for C - Am - F - G
                int[] bassRoots = { 48, 45, 41, 43 };
                int[][] chords =
                {
                    new[] { 72, 76, 79 },
                    new[] { 69, 72, 76 },
                    new[] { 65, 69, 72 },
                    new[] { 67, 71, 74 }
                };
                int[][] arpPatterns =
                {
                    new[] { 0, 1, 2, 1, 2, 1, 0, 1 },
                    new[] { 0, 2, 1, 2, 0, 2, 1, 2 }
                };

                int eighth = beat / 2;

                for (int b = 0; b < Bars; b++)
                {
                    int chordIndex = b % 4;
                    int barStart = b * bar;
                    int[] chord = chords[chordIndex];
                    int[] pattern = arpPatterns[b < 4 ? 0 : 1];

                    // pad: chord an octave below the arpeggio, long and soft
                    for (int k = 0; k < chord.Length; k++)
                        Add(2, barStart, MidiToHz(chord[k] - 12), 0.05, bar / (double)_rate + 0.35);

                    // bass: root on beats 1 and 3, octave pop on the "and" of 4
                    Add(0, barStart, MidiToHz(bassRoots[chordIndex]), 0.30, 0.45);
                    Add(0, barStart + beat * 2, MidiToHz(bassRoots[chordIndex]), 0.27, 0.40);
                    Add(0, barStart + beat * 3 + eighth, MidiToHz(bassRoots[chordIndex] + 12), 0.18, 0.25);

                    // arpeggio on every eighth note
                    for (int i = 0; i < 8; i++)
                    {
                        int note = chord[pattern[i]];
                        double accent = (i % 2 == 0) ? 0.17 : 0.13;
                        Add(1, barStart + i * eighth, MidiToHz(note), accent, 0.38);
                    }

                    // drums
                    Add(3, barStart, 0, 0.42, 0.28);
                    Add(3, barStart + beat * 2, 0, 0.38, 0.28);
                    Add(4, barStart + beat, 0, 0.16, 0.17);
                    Add(4, barStart + beat * 3, 0, 0.16, 0.17);
                    for (int i = 0; i < 8; i++)
                        Add(5, barStart + i * eighth, 0, (i % 2 == 1) ? 0.07 : 0.035, 0.06);
                }
            }

            private void Add(int kind, int start, double hz, double amp, double length)
            {
                _events.Add(new Evt { Kind = kind, Start = start, Hz = hz, Amp = amp, Length = length });
            }

            /// <summary>Renders up to <paramref name="maxEvents"/> events. Returns true when the loop is complete.</summary>
            public bool Step(int maxEvents)
            {
                if (_finished)
                    return true;

                int end = Math.Min(_events.Count, _next + Math.Max(1, maxEvents));
                for (; _next < end; _next++)
                    Render(_events[_next]);

                if (_next >= _events.Count)
                {
                    _track.Finish(0.80f, false);
                    _finished = true;
                }

                return _finished;
            }

            private void Render(Evt e)
            {
                double start = e.Start / (double)_rate;
                switch (e.Kind)
                {
                    case 0: // bass: triangle-ish (odd harmonics) with a soft pluck
                        _track.Tone(start, e.Length, e.Hz, e.Amp, new[] { 1.0, 2.0, 3.0 }, new[] { 1.0, 0.45, 0.15 }, 0.006, 0.20, 1.0);
                        break;
                    case 1: // arpeggio: plucked
                        _track.Tone(start, e.Length, e.Hz, e.Amp, new[] { 1.0, 2.0, 3.0 }, new[] { 1.0, 0.40, 0.14 }, 0.003, 0.13, 2.0);
                        break;
                    case 2: // pad: slow attack, long release
                        _track.Tone(start, e.Length, e.Hz, e.Amp, new[] { 1.0, 2.0 }, new[] { 1.0, 0.25 }, 0.45, 0, 0, 0.35);
                        break;
                    case 3: // kick
                        _track.Sweep(start, e.Length, 130, 42, e.Amp, 0.002, 0.075);
                        break;
                    case 4: // snare: noise + body
                        _track.Noise(_rng, start, e.Length, f => 6500, f => 1400, e.Amp, 0.001, 0.05);
                        _track.Tone(start, e.Length, 190, e.Amp * 0.5, new[] { 1.0 }, new[] { 1.0 }, 0.001, 0.04);
                        break;
                    default: // hat
                        _track.Noise(_rng, start, e.Length, f => 9500, f => 6000, e.Amp, 0.001, 0.018);
                        break;
                }
            }
        }

        // ---- Sample buffer + primitives -------------------------------------------------------

        private sealed class Track
        {
            public readonly float[] Data;
            private readonly int _rate;
            private readonly bool _wrap;

            public Track(double seconds, int rate)
                : this((int)Math.Ceiling(seconds * rate), rate, false)
            {
            }

            public Track(int samples, int rate, bool wrap)
            {
                Data = new float[Math.Max(1, samples)];
                _rate = rate;
                _wrap = wrap;
            }

            private void Add(int index, float value)
            {
                if (_wrap)
                {
                    index %= Data.Length;
                    if (index < 0)
                        index += Data.Length;
                }
                else if (index < 0 || index >= Data.Length)
                {
                    return;
                }

                Data[index] += value;
            }

            /// <summary>Attack ramp * exponential decay * short fade out (prevents clicks).</summary>
            private static double Envelope(double t, double duration, double attack, double tau, double release)
            {
                double a = attack > 0 ? Math.Min(1.0, t / attack) : 1.0;
                a = a * a * (3.0 - 2.0 * a); // smoothstep
                double d = tau > 0 ? Math.Exp(-t / tau) : 1.0;
                double rel = release > 0 ? release : 0.006;
                double r = Math.Min(1.0, Math.Max(0.0, (duration - t) / rel));
                return a * d * r;
            }

            public void Tone(double start, double duration, double hz, double amp, double[] ratios, double[] amps, double attack, double tau, double upperDecay = 0, double release = 0)
            {
                Tone(start, duration, x => hz, amp, ratios, amps, attack, tau, upperDecay, release);
            }

            public void Tone(double start, double duration, Func<double, double> hzAt, double amp, double[] ratios, double[] amps, double attack, double tau, double upperDecay = 0, double release = 0)
            {
                int first = (int)(start * _rate);
                int count = (int)(duration * _rate);
                var phases = new double[ratios.Length];

                for (int i = 0; i < count; i++)
                {
                    double t = i / (double)_rate;
                    double hz = hzAt(t);
                    double env = Envelope(t, duration, attack, tau, release);
                    double sum = 0;

                    for (int p = 0; p < ratios.Length; p++)
                    {
                        phases[p] += TwoPi * hz * ratios[p] / _rate;
                        double partialEnv = 1.0;
                        if (upperDecay > 0 && p > 0 && tau > 0)
                            partialEnv = Math.Exp(-t * upperDecay * p / tau * 0.5);
                        sum += Math.Sin(phases[p]) * amps[p] * partialEnv;
                    }

                    Add(first + i, (float)(sum * amp * env));
                }
            }

            /// <summary>Sine with an exponential frequency sweep.</summary>
            public void Sweep(double start, double duration, double fromHz, double toHz, double amp, double attack, double tau)
            {
                int first = (int)(start * _rate);
                int count = (int)(duration * _rate);
                double phase = 0;
                double ratio = toHz / fromHz;

                for (int i = 0; i < count; i++)
                {
                    double t = i / (double)_rate;
                    double progress = duration > 0 ? t / duration : 1.0;
                    double hz = fromHz * Math.Pow(ratio, progress);
                    phase += TwoPi * hz / _rate;
                    double env = Envelope(t, duration, attack, tau, 0);
                    Add(first + i, (float)(Math.Sin(phase) * amp * env));
                }
            }

            /// <summary>Band limited noise: difference of two one pole low passes. Cutoffs are functions of the normalised time (0..1).</summary>
            public void Noise(Rng rng, double start, double duration, Func<double, double> highCutoff, Func<double, double> lowCutoff, double amp, double attack, double tau)
            {
                int first = (int)(start * _rate);
                int count = (int)(duration * _rate);
                double lpHigh = 0;
                double lpLow = 0;

                for (int i = 0; i < count; i++)
                {
                    double t = i / (double)_rate;
                    double progress = duration > 0 ? t / duration : 1.0;
                    double x = rng.Signed();

                    double kh = 1.0 - Math.Exp(-TwoPi * Math.Min(highCutoff(progress), _rate * 0.45) / _rate);
                    double kl = 1.0 - Math.Exp(-TwoPi * Math.Min(lowCutoff(progress), _rate * 0.45) / _rate);
                    lpHigh += kh * (x - lpHigh);
                    lpLow += kl * (x - lpLow);

                    double env = Envelope(t, duration, attack, tau, 0);
                    Add(first + i, (float)((lpHigh - lpLow) * 2.0 * amp * env));
                }
            }

            /// <summary>One pole low pass over the whole buffer with a time varying cutoff (function of normalised time).</summary>
            public void LowPass(Func<double, double> cutoffHz)
            {
                double y = 0;
                for (int i = 0; i < Data.Length; i++)
                {
                    double progress = i / (double)Data.Length;
                    double k = 1.0 - Math.Exp(-TwoPi * Math.Min(Math.Max(60.0, cutoffHz(progress)), _rate * 0.45) / _rate);
                    y += k * (Data[i] - y);
                    Data[i] = (float)y;
                }
            }

            /// <summary>Optionally soft clips, then scales the buffer so that its peak equals <paramref name="peak"/>.</summary>
            public float[] Finish(float peak, bool softClip)
            {
                if (softClip)
                {
                    for (int i = 0; i < Data.Length; i++)
                        Data[i] = (float)Math.Tanh(Data[i] * 1.4);
                }

                float max = 0f;
                for (int i = 0; i < Data.Length; i++)
                {
                    float a = Math.Abs(Data[i]);
                    if (a > max) max = a;
                }

                if (max > 1e-6f)
                {
                    float gain = peak / max;
                    for (int i = 0; i < Data.Length; i++)
                        Data[i] *= gain;
                }

                // hard guarantee: no non finite samples, nothing outside [-1, 1]
                for (int i = 0; i < Data.Length; i++)
                {
                    float v = Data[i];
                    if (float.IsNaN(v) || float.IsInfinity(v)) v = 0f;
                    Data[i] = v > 1f ? 1f : (v < -1f ? -1f : v);
                }

                return Data;
            }
        }

        // ---- WAV helpers ----------------------------------------------------------------------

        private static void WriteAscii(byte[] b, int offset, string s)
        {
            for (int i = 0; i < s.Length; i++)
                b[offset + i] = (byte)s[i];
        }

        private static void WriteInt(byte[] b, int offset, int v)
        {
            b[offset] = (byte)(v & 0xFF);
            b[offset + 1] = (byte)((v >> 8) & 0xFF);
            b[offset + 2] = (byte)((v >> 16) & 0xFF);
            b[offset + 3] = (byte)((v >> 24) & 0xFF);
        }

        private static void WriteShort(byte[] b, int offset, int v)
        {
            b[offset] = (byte)(v & 0xFF);
            b[offset + 1] = (byte)((v >> 8) & 0xFF);
        }
    }
}
