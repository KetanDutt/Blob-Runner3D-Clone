using System;
using System.Collections;
using BlobRunner.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BlobRunner.Services
{
    /// <summary>
    /// Plays all sound effects and the music loop.
    ///
    /// Sounds are synthesized at start-up by <see cref="ProceduralSynth"/>, spread over several frames so there is no
    /// hitch. To use real recordings instead, put an AudioClip with the lower case name of the effect
    /// (<c>click</c>, <c>cut</c>, <c>collect</c>, <c>win</c>, ...) into <c>Assets/**/Resources/Audio</c>; the music loop is
    /// picked up from <c>Resources/Audio/music</c>.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        private const int VoiceCount = 8;
        private const float MusicVolume = 0.32f;

        private static readonly float[] BaseVolumes =
        {
            0.60f, // Click
            0.80f, // Start
            1.00f, // Cut
            0.80f, // Regrow
            0.90f, // Collect
            0.80f, // Star
            1.00f, // Win
            1.00f, // Lose
            0.60f, // Whoosh
            0.60f  // Pop
        };

        private AudioSource[] _voices;
        private AudioSource _music;
        private AudioClip[] _clips;
        private AudioClip _musicClip;
        private SaveService _save;
        private int _nextVoice;
        private float _duck = 1f;
        private float _duckTarget = 1f;

        /// <summary>True once every effect has been generated.</summary>
        public bool IsReady { get; private set; }

        private void Awake()
        {
            _clips = new AudioClip[Enum.GetValues(typeof(SfxKind)).Length];

            _voices = new AudioSource[VoiceCount];
            for (int i = 0; i < _voices.Length; i++)
                _voices[i] = CreateSource();

            _music = CreateSource();
            _music.loop = true;
            _music.volume = 0f;

            _save = GameServices.Save;
            if (_save != null)
                _save.SettingsChanged += ApplySettings;

            StartCoroutine(Build());
        }

        private void OnDestroy()
        {
            if (_save != null)
                _save.SettingsChanged -= ApplySettings;
        }

        private AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = false;
            return source;
        }

        // ---- Building ----------------------------------------------------------------------------------

        private IEnumerator Build()
        {
            // 22.05 kHz is plenty for these sounds and keeps the memory footprint tiny
            int synthRate = ProceduralSynth.DefaultSampleRate;

            foreach (SfxKind kind in Enum.GetValues(typeof(SfxKind)))
            {
                var clip = Resources.Load<AudioClip>("Audio/" + kind.ToString().ToLowerInvariant());
                if (clip == null)
                {
                    float[] samples = ProceduralSynth.Render(kind, synthRate);
                    clip = AudioClip.Create("sfx_" + kind, samples.Length, 1, synthRate, false);
                    clip.SetData(samples, 0);
                }

                _clips[(int)kind] = clip;
                yield return null; // one effect per frame
            }

            IsReady = true;

            _musicClip = Resources.Load<AudioClip>("Audio/music");
            if (_musicClip == null)
            {
                var renderer = new ProceduralSynth.MusicRenderer(synthRate);
                while (!renderer.Step(4))
                    yield return null;

                float[] loop = renderer.Result;
                _musicClip = AudioClip.Create("music_loop", loop.Length, 1, synthRate, false);
                _musicClip.SetData(loop, 0);
            }

            _music.clip = _musicClip;
            ApplySettings();
        }

        // ---- Playback ----------------------------------------------------------------------------------

        public void Play(SfxKind kind, float volume = 1f, float pitch = 1f, float pitchJitter = 0f)
        {
            if (_save != null && !_save.Data.sfxOn)
                return;

            var clip = _clips != null ? _clips[(int)kind] : null;
            if (clip == null)
                return;

            var source = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;

            source.clip = clip;
            source.volume = Mathf.Clamp01(volume * BaseVolumeOf(kind));
            source.pitch = pitch * (1f + Random.Range(-pitchJitter, pitchJitter));
            source.Play();
        }

        // A new SfxKind without an entry in BaseVolumes simply plays at full volume.
        private static float BaseVolumeOf(SfxKind kind)
        {
            int index = (int)kind;
            return index >= 0 && index < BaseVolumes.Length ? BaseVolumes[index] : 1f;
        }

        /// <summary>Lowers the music (pause menu, result screens).</summary>
        public void DuckMusic(bool ducked)
        {
            _duckTarget = ducked ? 0.35f : 1f;
        }

        private void ApplySettings()
        {
            if (_music == null || _music.clip == null)
                return;

            bool musicOn = _save == null || _save.Data.musicOn;
            _music.mute = !musicOn;

            if (musicOn && !_music.isPlaying)
                _music.Play();
        }

        private void Update()
        {
            if (_music == null)
                return;

            _duck = Mathf.MoveTowards(_duck, _duckTarget, Time.unscaledDeltaTime * 1.5f);
            _music.volume = MusicVolume * _duck;
        }
    }
}
