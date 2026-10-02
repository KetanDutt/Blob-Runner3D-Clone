using BlobRunner.Core;
using UnityEngine;

namespace BlobRunner.Services
{
    /// <summary>
    /// Frame rate / power configuration and adaptive resolution.
    ///
    ///  * 60 fps cap (the old code requested 120, which only burns battery on a ray marched character);
    ///  * on mobile, v-sync is off so <c>targetFrameRate</c> is honoured;
    ///  * the screen never sleeps during a run;
    ///  * the render resolution is lowered in small steps when the device can not hold the target frame rate
    ///    (see <see cref="AdaptiveQualityGovernor"/>).
    /// </summary>
    public sealed class PerformanceDirector : MonoBehaviour
    {
        public const int TargetFrameRate = 60;

        private AdaptiveQualityGovernor _governor;
        private int _baseWidth;
        private int _baseHeight;

        /// <summary>Current resolution scale (1 = native).</summary>
        public float ResolutionScale { get { return _governor != null ? _governor.Scale : 1f; } }

        private void Awake()
        {
            Application.targetFrameRate = TargetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Time.maximumDeltaTime = 0.1f; // a hitch must not teleport the blob through an obstacle

            if (Application.isMobilePlatform)
                QualitySettings.vSyncCount = 0;

            _governor = new AdaptiveQualityGovernor(TargetFrameRate, 0.6f, 1f, 0.1f);
            _baseWidth = Screen.width;
            _baseHeight = Screen.height;

            // Dynamic resolution only makes sense on devices; the editor game view keeps its size.
            enabled = Application.isMobilePlatform;
        }

        private void Update()
        {
            if (_governor.Tick(Time.unscaledDeltaTime))
                ApplyScale(_governor.Scale);
        }

        private void ApplyScale(float scale)
        {
            int width = Mathf.Max(320, Mathf.RoundToInt(_baseWidth * scale));
            int height = Mathf.Max(480, Mathf.RoundToInt(_baseHeight * scale));
            Screen.SetResolution(width, height, true);
        }
    }
}
