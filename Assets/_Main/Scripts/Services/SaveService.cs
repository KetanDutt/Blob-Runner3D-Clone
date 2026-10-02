using System;
using BlobRunner.Core;
using UnityEngine;

namespace BlobRunner.Services
{
    /// <summary>Loads / saves <see cref="SaveData"/> as JSON in PlayerPrefs.</summary>
    public sealed class SaveService : MonoBehaviour
    {
        private const string Key = "blobrunner.save.v1";

        private bool _dirty;

        public SaveData Data { get; private set; }

        /// <summary>Raised when one of the sound / haptics switches changed.</summary>
        public event Action SettingsChanged;

        private void Awake()
        {
            Load();
        }

        public void Load()
        {
            Data = null;

            try
            {
                string json = PlayerPrefs.GetString(Key, string.Empty);
                if (!string.IsNullOrEmpty(json))
                    Data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Could not read the save game, starting fresh: " + e.Message);
            }

            if (Data == null)
                Data = new SaveData();

            Data.Sanitize();
            _dirty = false;
        }

        public void Save()
        {
            try
            {
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
                PlayerPrefs.Save();
                _dirty = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Could not write the save game: " + e.Message);
            }
        }

        public void MarkDirty()
        {
            _dirty = true;
        }

        public void SetSfx(bool on)
        {
            if (Data.sfxOn == on) return;
            Data.sfxOn = on;
            SettingsToggled();
        }

        public void SetMusic(bool on)
        {
            if (Data.musicOn == on) return;
            Data.musicOn = on;
            SettingsToggled();
        }

        public void SetHaptics(bool on)
        {
            if (Data.hapticsOn == on) return;
            Data.hapticsOn = on;
            SettingsToggled();
        }

        private void SettingsToggled()
        {
            Save();
            var handler = SettingsChanged;
            if (handler != null)
                handler();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _dirty)
                Save();
        }

        private void OnApplicationQuit()
        {
            if (_dirty)
                Save();
        }
    }
}
