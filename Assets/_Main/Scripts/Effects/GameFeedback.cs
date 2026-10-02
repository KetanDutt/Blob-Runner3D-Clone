using System.Collections;
using BlobRunner.Core;
using BlobRunner.Services;
using BlobRunner.UI;
using UnityEngine;

namespace BlobRunner.Effects
{
    /// <summary>
    /// "Game feel" hub: turns gameplay events into sound, particles, haptics, camera motion and UI feedback.
    /// Keeping the mapping in one place makes it easy to tune (and keeps the gameplay classes free of effect code).
    /// </summary>
    public sealed class GameFeedback : MonoBehaviour
    {
        private GameManager _game;
        private AudioDirector _audio;
        private HapticsService _haptics;
        private VfxDirector _vfx;
        private CameraDirector _camera;
        private UIManager _ui;
        private Coroutine _hitStop;

        public void Initialize(GameManager game)
        {
            _game = game;
            _audio = GameServices.Audio;
            _haptics = GameServices.Haptics;
            _vfx = game.Vfx;
            _camera = game.CameraFx;
            _ui = game.UI;

            game.StateChanged += OnStateChanged;
            game.PlayerHit += OnHit;
            game.LootCollected += OnLoot;
            game.RunWon += OnWon;
            game.RunLost += OnLost;
        }

        private void OnDestroy()
        {
            if (_game == null)
                return;

            _game.StateChanged -= OnStateChanged;
            _game.PlayerHit -= OnHit;
            _game.LootCollected -= OnLoot;
            _game.RunWon -= OnWon;
            _game.RunLost -= OnLost;
        }

        // ---- State -----------------------------------------------------------------------------------

        private void OnStateChanged(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.Playing:
                    if (from == GameState.Paused)
                    {
                        Duck(false);
                    }
                    else
                    {
                        Play(SfxKind.Start);
                        Haptic(HapticKind.Light);
                        Duck(false);
                        if (_camera != null) _camera.SetRunZoom(3f, 0.9f);
                        if (_vfx != null) _vfx.SetDust(true);
                    }
                    break;

                case GameState.Paused:
                    Duck(true);
                    break;
            }
        }

        // ---- Gameplay --------------------------------------------------------------------------------

        private void OnHit(Player.HitInfo info)
        {
            int parts = Mathf.Max(1, info.PartsLost);

            Play(SfxKind.Cut, 1f, 1f, 0.08f);
            Haptic(parts >= 3 ? HapticKind.Heavy : HapticKind.Medium);

            if (_vfx != null)
                _vfx.Splat(info.Position, info.Color, parts);

            if (_camera != null)
            {
                _camera.Shake(Mathf.Min(0.9f, 0.34f + 0.08f * parts));
                _camera.Punch(-3f, 0.3f);
            }

            if (_ui != null)
            {
                _ui.FlashDamage();
                _ui.ShowFloatingText(info.Position + Vector3.up * 0.9f, parts > 1 ? "-" + parts : UIStrings.Ouch, UIColors.Danger);
            }

            HitStop(Mathf.Min(0.11f, 0.04f + 0.015f * parts));
        }

        private void OnLoot(LootContainer loot, int regrown)
        {
            Play(SfxKind.Collect, 1f, 1f, 0.04f);
            if (regrown > 0)
                Play(SfxKind.Regrow, 0.9f);

            Haptic(regrown > 0 ? HapticKind.Success : HapticKind.Light);

            Vector3 position = loot != null ? loot.transform.position : (_game.Player != null ? _game.Player.transform.position : Vector3.zero);
            Color color = loot != null ? loot.LootColor : UIColors.Accent;

            if (_vfx != null)
                _vfx.Sparkle(position, color);

            if (_camera != null)
                _camera.Punch(3f, 0.35f);

            if (_ui != null)
                _ui.ShowFloatingText(position + Vector3.up * 0.7f, regrown > 0 ? "+" + regrown : UIStrings.Nice, color);
        }

        private void OnWon(ScoreResult result, bool newBest)
        {
            Play(SfxKind.Win);
            Haptic(HapticKind.Success);
            Duck(true);

            if (_vfx != null)
            {
                _vfx.SetDust(false);
                if (_game.Player != null)
                    _vfx.Confetti(_game.Player.transform.position);
            }

            if (_camera != null)
            {
                _camera.SetRunZoom(7f, 1.2f);
                _camera.Punch(4f, 0.5f);
            }
        }

        private void OnLost()
        {
            Play(SfxKind.Lose);
            Haptic(HapticKind.Failure);
            Duck(true);

            if (_vfx != null)
            {
                _vfx.SetDust(false);
                if (_game.Player != null)
                    _vfx.DeathBurst(_game.Player.transform.position + Vector3.up * 0.9f, new Color(1f, 0.63f, 0f));
            }

            if (_camera != null)
            {
                _camera.Shake(0.75f);
                _camera.SetRunZoom(-3f, 1.4f);
            }

            if (_ui != null)
                _ui.FlashDamage();
        }

        // ---- Helpers ---------------------------------------------------------------------------------

        private void Play(SfxKind kind, float volume = 1f, float pitch = 1f, float jitter = 0f)
        {
            if (_audio != null)
                _audio.Play(kind, volume, pitch, jitter);
        }

        private void Haptic(HapticKind kind)
        {
            if (_haptics != null)
                _haptics.Play(kind);
        }

        private void Duck(bool ducked)
        {
            if (_audio != null)
                _audio.DuckMusic(ducked);
        }

        /// <summary>Freezes the game for a split second on impact.</summary>
        private void HitStop(float seconds)
        {
            if (_game.State != GameState.Playing)
                return;

            if (_hitStop != null)
                StopCoroutine(_hitStop);

            _hitStop = StartCoroutine(HitStopRoutine(seconds));
        }

        private IEnumerator HitStopRoutine(float seconds)
        {
            Time.timeScale = 0.08f;
            yield return new WaitForSecondsRealtime(seconds);

            // pausing during the freeze keeps the game paused
            if (_game != null && _game.State != GameState.Paused)
                Time.timeScale = 1f;

            _hitStop = null;
        }
    }
}
