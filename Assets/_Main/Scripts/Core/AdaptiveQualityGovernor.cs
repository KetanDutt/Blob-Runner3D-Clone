using System;

namespace BlobRunner.Core
{
    /// <summary>
    /// Decides the render resolution scale from measured frame times.
    ///
    /// The blob is ray marched, so the cost is dominated by fill rate. When the device cannot hold the target
    /// frame rate the governor lowers the resolution in small steps; it only raises it again after a long, stable
    /// period and gives up on raising after it has had to lower twice (prevents flip-flopping).
    /// </summary>
    public sealed class AdaptiveQualityGovernor
    {
        private readonly float _targetFps;
        private readonly float _minScale;
        private readonly float _maxScale;
        private readonly float _step;
        private readonly float _windowSeconds;

        private float _windowTime;
        private int _windowFrames;
        private int _slowWindows;
        private int _fastWindows;
        private int _downgrades;

        /// <summary>Current resolution scale (maxScale .. minScale).</summary>
        public float Scale { get; private set; }

        public int Downgrades { get { return _downgrades; } }

        /// <summary>Frame times longer than this (loading, app switching...) are ignored.</summary>
        public float HitchThreshold { get; set; }

        public AdaptiveQualityGovernor(float targetFps = 60f, float minScale = 0.6f, float maxScale = 1f, float step = 0.1f, float windowSeconds = 2f)
        {
            _targetFps = Math.Max(10f, targetFps);
            _minScale = Math.Max(0.25f, Math.Min(minScale, maxScale));
            _maxScale = Math.Max(_minScale, maxScale);
            _step = Math.Max(0.02f, step);
            _windowSeconds = Math.Max(0.5f, windowSeconds);
            HitchThreshold = 0.25f;
            Scale = _maxScale;
        }

        /// <summary>Feed one frame (unscaled delta time in seconds). Returns true when <see cref="Scale"/> changed.</summary>
        public bool Tick(float unscaledDeltaTime)
        {
            if (float.IsNaN(unscaledDeltaTime) || unscaledDeltaTime <= 0f || unscaledDeltaTime > HitchThreshold)
                return false;

            _windowTime += unscaledDeltaTime;
            _windowFrames++;

            if (_windowTime < _windowSeconds)
                return false;

            float fps = _windowFrames / _windowTime;
            _windowTime = 0f;
            _windowFrames = 0;

            bool changed = false;

            if (fps < _targetFps * 0.88f)
            {
                _fastWindows = 0;
                _slowWindows++;

                // two consecutive slow windows -> step down
                if (_slowWindows >= 2 && Scale > _minScale + 0.0001f)
                {
                    Scale = Math.Max(_minScale, Scale - _step);
                    _downgrades++;
                    _slowWindows = 0;
                    changed = true;
                }
            }
            else if (fps >= _targetFps * 0.97f)
            {
                _slowWindows = 0;
                _fastWindows++;

                // ~16 s of rock solid frames, and never after having had to downgrade twice
                if (_fastWindows >= 8 && _downgrades > 0 && _downgrades < 2 && Scale < _maxScale - 0.0001f)
                {
                    Scale = Math.Min(_maxScale, Scale + _step);
                    _fastWindows = 0;
                    changed = true;
                }
            }
            else
            {
                _slowWindows = 0;
                _fastWindows = 0;
            }

            return changed;
        }

        public void Reset()
        {
            Scale = _maxScale;
            _windowTime = 0f;
            _windowFrames = 0;
            _slowWindows = 0;
            _fastWindows = 0;
            _downgrades = 0;
        }
    }
}
