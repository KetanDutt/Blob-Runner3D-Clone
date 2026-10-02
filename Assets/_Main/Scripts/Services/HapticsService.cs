using System;
using System.Collections;
using UnityEngine;

namespace BlobRunner.Services
{
    public enum HapticKind
    {
        /// <summary>Tiny tick for UI interaction.</summary>
        Selection,
        Light,
        Medium,
        Heavy,
        Success,
        Failure
    }

    /// <summary>
    /// Vibration feedback. Android gets short one-shot pulses; iOS (which has no public short-pulse API in Unity)
    /// only vibrates for the big moments. Everything is a no-op in the editor and when the player turned it off.
    /// </summary>
    public sealed class HapticsService : MonoBehaviour
    {
        private SaveService _save;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _vibrator;
        private AndroidJavaClass _effectClass;
#endif

        public bool Enabled
        {
            get { return _save == null || _save.Data.hapticsOn; }
        }

        private void Awake()
        {
            _save = GameServices.Save;
            InitializePlatform();
        }

        private void OnDestroy()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_vibrator != null) _vibrator.Dispose();
            if (_effectClass != null) _effectClass.Dispose();
#endif
        }

        public void Play(HapticKind kind)
        {
            if (!Enabled)
                return;

            switch (kind)
            {
                case HapticKind.Selection: Pulse(12, 50); break;
                case HapticKind.Light: Pulse(20, 90); break;
                case HapticKind.Medium: Pulse(35, 170); break;
                case HapticKind.Heavy: Pulse(60, 255); break;
                case HapticKind.Success: StartCoroutine(Sequence(new[] { 30, 40 }, 70, 190)); break;
                case HapticKind.Failure: StartCoroutine(Sequence(new[] { 70, 90 }, 90, 255)); break;
            }
        }

        private IEnumerator Sequence(int[] durations, int gapMs, int amplitude)
        {
            for (int i = 0; i < durations.Length; i++)
            {
                Pulse(durations[i], amplitude);
                yield return new WaitForSecondsRealtime((durations[i] + gapMs) / 1000f);
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void InitializePlatform()
        {
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                int sdk;
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    sdk = version.GetStatic<int>("SDK_INT");

                if (sdk >= 26)
                    _effectClass = new AndroidJavaClass("android.os.VibrationEffect");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Haptics] Vibrator not available: " + e.Message);
                _vibrator = null;
            }
        }

        private void Pulse(int milliseconds, int amplitude)
        {
            try
            {
                if (_vibrator == null)
                {
                    if (milliseconds >= 50) Handheld.Vibrate(); // also makes Unity add the VIBRATE permission
                    return;
                }

                if (_effectClass != null)
                {
                    using (var effect = _effectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)milliseconds, amplitude))
                        _vibrator.Call("vibrate", effect);
                }
                else
                {
                    _vibrator.Call("vibrate", (long)milliseconds);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Haptics] Vibration failed: " + e.Message);
            }
        }
#elif UNITY_IOS && !UNITY_EDITOR
        private void InitializePlatform() { }

        private void Pulse(int milliseconds, int amplitude)
        {
            // Handheld.Vibrate is a long buzz on iOS: keep it for the heavy moments only.
            if (milliseconds >= 55)
                Handheld.Vibrate();
        }
#else
        private void InitializePlatform() { }

        private void Pulse(int milliseconds, int amplitude) { }
#endif
    }
}
