using System.Globalization;
using BlobRunner.Services;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlobRunner.UI
{
    /// <summary>In-run overlay: level, progress to the finish, jelly (body parts) meter, pause button and the first-level hint.</summary>
    public sealed class HudScreen : UIScreen
    {
        private const float TrackWidth = 760f;

        private readonly GameManager _game;
        private readonly RectTransform _progressFill;
        private readonly RectTransform _marker;
        private readonly RectTransform _integrityFill;
        private readonly Image _integrityImage;
        private readonly Text _integrityText;
        private readonly RectTransform _integrityRoot;
        private readonly Text _hint;

        private float _shownProgress = -1f;
        private int _shownIntegrity = -1;
        private bool _hintHidden;

        public HudScreen(Transform canvas, GameManager game) : base(canvas, "Hud")
        {
            _game = game;

            // level badge (top left)
            var badge = UIFactory.CreateImage(Content, "LevelBadge", UISprites.RoundedRect, UIColors.PanelSoft, false, true);
            UIFactory.Place(badge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(290f, 90f));
            var badgeText = UIFactory.CreateText(badge.transform, "Text", UIStrings.Level + game.Level.ToString(CultureInfo.InvariantCulture), 46, Color.white);
            UIFactory.Stretch(badgeText.rectTransform);

            // pause button (top right): rounded square with two bars
            var pause = UIFactory.CreateButton(Content, "Pause", string.Empty, UIColors.PanelSoft, new Vector2(96f, 96f), 10, game.Pause);
            UIFactory.Place(pause.Rect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -27f), new Vector2(96f, 96f));
            for (int i = 0; i < 2; i++)
            {
                var bar = UIFactory.CreateImage(pause.Rect, "Bar" + i, UISprites.RoundedRect, Color.white, false, true);
                UIFactory.Place(bar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(i == 0 ? -15f : 15f, 0f), new Vector2(20f, 46f));
            }

            // progress track with the runner marker and the finish star
            var track = UIFactory.CreateImage(Content, "ProgressTrack", UISprites.RoundedRect, UIColors.PanelSoft, false, true);
            UIFactory.Place(track.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -165f), new Vector2(TrackWidth, 40f));

            var fill = UIFactory.CreateImage(track.transform, "Fill", UISprites.RoundedRect, UIColors.Accent, false, true);
            _progressFill = fill.rectTransform;
            _progressFill.anchorMin = new Vector2(0f, 0f);
            _progressFill.anchorMax = new Vector2(0.05f, 1f);
            _progressFill.offsetMin = new Vector2(5f, 5f);
            _progressFill.offsetMax = new Vector2(-5f, -5f);

            var star = UIFactory.CreateImage(track.transform, "FinishStar", UISprites.Star, UIColors.Warning);
            UIFactory.Place(star.rectTransform, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(12f, 0f), new Vector2(84f, 84f));

            var marker = UIFactory.CreateImage(track.transform, "Marker", UISprites.Circle, Color.white);
            _marker = marker.rectTransform;
            UIFactory.Place(_marker, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f));
            var markerInner = UIFactory.CreateImage(_marker, "Inner", UISprites.Circle, UIColors.Accent);
            UIFactory.Place(markerInner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));

            // jelly (body parts) meter
            var integrity = UIFactory.CreateImage(Content, "IntegrityTrack", UISprites.RoundedRect, UIColors.PanelSoft, false, true);
            _integrityRoot = integrity.rectTransform;
            UIFactory.Place(_integrityRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -235f), new Vector2(430f, 36f));

            _integrityImage = UIFactory.CreateImage(integrity.transform, "Fill", UISprites.RoundedRect, UIColors.Good, false, true);
            _integrityFill = _integrityImage.rectTransform;
            _integrityFill.anchorMin = new Vector2(0f, 0f);
            _integrityFill.anchorMax = new Vector2(1f, 1f);
            _integrityFill.offsetMin = new Vector2(4f, 4f);
            _integrityFill.offsetMax = new Vector2(-4f, -4f);

            _integrityText = UIFactory.CreateText(integrity.transform, "Text", UIStrings.Jelly, 24, Color.white);
            UIFactory.Stretch(_integrityText.rectTransform);

            // first level hint
            _hint = UIFactory.CreateText(Content, "Hint", UIStrings.DragHint, 54, Color.white);
            UIFactory.Place(_hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 420f), new Vector2(1000f, 120f));
            _hint.gameObject.SetActive(false);

            Joystick.OnJoystickPress += HideHint;
        }

        protected override void OnShown()
        {
            _shownProgress = -1f;
            _shownIntegrity = -1;
            Tick();

            if (_game.TutorialPending && !_hintHidden)
            {
                _hint.gameObject.SetActive(true);
                _hint.color = Color.white;
                DOTween.Kill(_hint);
                _hint.DOFade(0.3f, 0.7f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetId(_hint).SetLink(_hint.gameObject);
                DOVirtual.DelayedCall(6f, HideHint, true).SetId(_hint).SetLink(_hint.gameObject);
            }
        }

        public void HideHint()
        {
            if (_hintHidden)
                return;

            _hintHidden = true;
            DOTween.Kill(_hint);
            if (_hint != null)
                _hint.gameObject.SetActive(false);
        }

        /// <summary>Called every frame while the HUD is visible; only touches the UI when a value changed.</summary>
        public void Tick()
        {
            float progress = Mathf.Clamp01(_game.Progress);
            if (Mathf.Abs(progress - _shownProgress) > 0.0005f)
            {
                _shownProgress = progress;
                float p = Mathf.Max(progress, 0.05f);
                _progressFill.anchorMax = new Vector2(p, 1f);
                _marker.anchorMin = new Vector2(progress, 0.5f);
                _marker.anchorMax = new Vector2(progress, 0.5f);
                _marker.anchoredPosition = Vector2.zero;
            }

            var player = _game.Player;
            if (player == null)
                return;

            int percent = Mathf.RoundToInt(player.Integrity * 100f);
            if (percent != _shownIntegrity)
            {
                bool dropped = _shownIntegrity >= 0 && percent < _shownIntegrity;
                _shownIntegrity = percent;

                float integrity = player.Integrity;
                _integrityFill.anchorMax = new Vector2(Mathf.Max(0.03f, integrity), 1f);
                _integrityImage.color = integrity > 0.5f
                    ? Color.Lerp(UIColors.Warning, UIColors.Good, (integrity - 0.5f) * 2f)
                    : Color.Lerp(UIColors.Danger, UIColors.Warning, integrity * 2f);
                _integrityText.text = UIStrings.Jelly + " " + percent.ToString(CultureInfo.InvariantCulture) + "%";

                if (dropped)
                {
                    DOTween.Kill(_integrityRoot);
                    _integrityRoot.localScale = Vector3.one;
                    _integrityRoot.DOPunchScale(new Vector3(0.18f, 0.35f, 0f), 0.35f, 8, 0.6f).SetUpdate(true).SetId(_integrityRoot).SetLink(_integrityRoot.gameObject);
                }
            }
        }

        public override void Dispose()
        {
            Joystick.OnJoystickPress -= HideHint;
            if (_hint != null)
                DOTween.Kill(_hint);
            if (_integrityRoot != null)
                DOTween.Kill(_integrityRoot);
            base.Dispose();
        }
    }
}
