using DG.Tweening;
using UnityEngine;

namespace BlobRunner.Services
{
    /// <summary>
    /// Access point for the services that live for the whole session (save game, audio, haptics, performance).
    /// They are hosted by a <c>DontDestroyOnLoad</c> object that is created before the first scene loads, so restarting a
    /// level (which reloads the scene) never re-synthesizes audio or re-reads the save game.
    /// </summary>
    public static class GameServices
    {
        private static GameServicesHost _host;
        private static bool _quitting;

        /// <summary>The host object (created on demand; null while the application is quitting).</summary>
        public static GameServicesHost Host
        {
            get
            {
                if (_host == null && !_quitting && Application.isPlaying)
                    Create();

                return _host;
            }
        }

        public static SaveService Save { get { return Host != null ? Host.Save : null; } }

        public static AudioDirector Audio { get { return Host != null ? Host.Audio : null; } }

        public static HapticsService Haptics { get { return Host != null ? Host.Haptics : null; } }

        public static PerformanceDirector Performance { get { return Host != null ? Host.Performance : null; } }

        // With "Enter Play Mode Options" (domain reload disabled) statics survive between play sessions.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _host = null;
            _quitting = false;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var ignored = Host;
        }

        private static void OnQuitting()
        {
            _quitting = true;
        }

        private static void Create()
        {
            var go = new GameObject("[GameServices]");
            Object.DontDestroyOnLoad(go);

            _host = go.AddComponent<GameServicesHost>();
            _host.Initialize(); // after the assignment, so services may use GameServices.* from their Awake
        }
    }

    /// <summary>Owns the long lived services.</summary>
    public sealed class GameServicesHost : MonoBehaviour
    {
        public SaveService Save { get; private set; }

        public AudioDirector Audio { get; private set; }

        public HapticsService Haptics { get; private set; }

        public PerformanceDirector Performance { get; private set; }

        internal void Initialize()
        {
            ConfigureTweening();

            Save = gameObject.AddComponent<SaveService>();
            Performance = gameObject.AddComponent<PerformanceDirector>();
            Haptics = gameObject.AddComponent<HapticsService>();
            Audio = gameObject.AddComponent<AudioDirector>();
        }

        private static void ConfigureTweening()
        {
            // Recycling tweens avoids garbage; safe mode keeps a failing tween from breaking the frame.
            // All tweens in this project are addressed by id / link, never by a stored reference.
            DOTween.Init(true, true, Debug.isDebugBuild ? LogBehaviour.Default : LogBehaviour.ErrorsOnly);
            DOTween.SetTweensCapacity(300, 60);
        }
    }
}
