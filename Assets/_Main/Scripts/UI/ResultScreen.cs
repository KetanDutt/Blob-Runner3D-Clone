using System.Globalization;
using BlobRunner.Core;
using BlobRunner.Services;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlobRunner.UI
{
    /// <summary>
    /// Level complete / game over popup. The win version reveals the stars one by one, counts the score up and
    /// highlights a new best; the lose version shows how far the blob got.
    /// </summary>
    public sealed class ResultScreen : UIScreen
    {
        private readonly GameManager _game;
        private readonly Image _card;
        private readonly Text _title;
        private readonly Image[] _stars = new Image[3];
        private readonly Text _scoreLabel;
        private readonly Text _scoreValue;
        private readonly Text _detail;
        private readonly Text _newBest;
        private readonly RectTransform _primaryHolder;
        private readonly RectTransform _secondaryHolder;
        private readonly UIButton _primary;
        private readonly UIButton _secondary;

        public ResultScreen(Transform canvas, GameManager game) : base(canvas, "Result")
        {
            _game = game;

            var dim = UIFactory.CreateImage(Root, "Dim", null, UIColors.Dim, true);
            UIFactory.Stretch(dim.rectTransform);
            dim.transform.SetAsFirstSibling();

            _card = UIFactory.CreateImage(Content, "Card", UISprites.RoundedRect, UIColors.Panel, false, true);
            UIFactory.Place(_card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 1260f));

            _title = UIFactory.CreateText(_card.transform, "Title", string.Empty, 96, Color.white);
            UIFactory.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(860f, 140f));

            for (int i = 0; i < _stars.Length; i++)
            {
                var star = UIFactory.CreateImage(_card.transform, "Star" + i, UISprites.Star, UIColors.Warning);
                float size = i == 1 ? 230f : 180f;
                UIFactory.Place(star.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 230f, i == 1 ? -372f : -350f), new Vector2(size, size));
                _stars[i] = star;
            }

            _scoreLabel = UIFactory.CreateText(_card.transform, "ScoreLabel", UIStrings.Score, 44, new Color(1f, 1f, 1f, 0.7f));
            UIFactory.Place(_scoreLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -500f), new Vector2(800f, 60f));

            _scoreValue = UIFactory.CreateText(_card.transform, "ScoreValue", "0", 120, Color.white);
            UIFactory.Place(_scoreValue.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -565f), new Vector2(860f, 150f));

            _newBest = UIFactory.CreateText(_card.transform, "NewBest", UIStrings.NewBest, 56, UIColors.Warning);
            UIFactory.Place(_newBest.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -715f), new Vector2(800f, 70f));
            _newBest.gameObject.SetActive(false);

            _detail = UIFactory.CreateText(_card.transform, "Detail", string.Empty, 44, new Color(1f, 1f, 1f, 0.85f));
            UIFactory.Place(_detail.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -790f), new Vector2(860f, 70f));

            _primary = UIFactory.CreateButton(_card.transform, "Primary", string.Empty, UIColors.Good, new Vector2(660f, 170f), 80, null);
            _primaryHolder = _primary.Rect;
            UIFactory.Place(_primaryHolder, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 185f), new Vector2(660f, 170f));

            _secondary = UIFactory.CreateButton(_card.transform, "Secondary", string.Empty, UIColors.Accent, new Vector2(520f, 110f), 52, null);
            _secondaryHolder = _secondary.Rect;
            UIFactory.Place(_secondaryHolder, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(520f, 110f));
        }

        // ---- Win ---------------------------------------------------------------------------------------

        public void ShowWin(ScoreResult result, bool newBest)
        {
            Prepare(UIStrings.LevelComplete, UIColors.Warning);

            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].gameObject.SetActive(true);
                _stars[i].color = new Color(1f, 1f, 1f, 0.16f); // empty slot
                _stars[i].rectTransform.localScale = Vector3.one;
            }

            _scoreLabel.text = UIStrings.Score;
            _scoreLabel.gameObject.SetActive(true);
            _scoreValue.gameObject.SetActive(true);
            _scoreValue.text = "0";
            _detail.text = UIStrings.Jelly + " " + Mathf.RoundToInt(result.Integrity * 100f).ToString(CultureInfo.InvariantCulture) + "%   -   " +
                           UIStrings.Best + Mathf.Max(_game.BestScore, result.Score).ToString("N0", CultureInfo.InvariantCulture);

            SetButton(_primary, UIStrings.NextLevel, UIColors.Good, _game.NextLevel);
            SetButton(_secondary, UIStrings.Replay, UIColors.Accent, _game.Restart);

            Show(true);
            RevealWin(result, newBest);
        }

        private void RevealWin(ScoreResult result, bool newBest)
        {
            var audio = GameServices.Audio;
            var haptics = GameServices.Haptics;

            // stars pop in one after the other
            float t = 0.45f;
            for (int i = 0; i < _stars.Length; i++)
            {
                bool earned = i < result.Stars;
                if (!earned)
                    continue;

                var star = _stars[i];
                int index = i;
                DOVirtual.DelayedCall(t, () =>
                {
                    star.color = UIColors.Warning;
                    star.rectTransform.localScale = Vector3.zero;
                    star.rectTransform.DOScale(1f, 0.45f).SetEase(Ease.OutBack, 2.2f).SetUpdate(true).SetId(Root).SetLink(Root.gameObject);
                    if (audio != null) audio.Play(SfxKind.Star, 0.9f, 1f + index * 0.14f);
                    if (haptics != null) haptics.Play(HapticKind.Light);
                }, true).SetId(Root).SetLink(Root.gameObject);
                t += 0.32f;
            }

            // score counts up once the stars are in
            float delay = t + 0.1f;
            int target = result.Score;
            DOVirtual.Float(0f, target, 1.0f, v => _scoreValue.text = Mathf.RoundToInt(v).ToString("N0", CultureInfo.InvariantCulture))
                .SetDelay(delay).SetEase(Ease.OutCubic).SetUpdate(true).SetId(Root).SetLink(Root.gameObject);

            if (newBest)
            {
                _newBest.gameObject.SetActive(true);
                _newBest.rectTransform.localScale = Vector3.zero;
                _newBest.rectTransform.DOScale(1f, 0.5f).SetDelay(delay + 1.0f).SetEase(Ease.OutBack).SetUpdate(true).SetId(Root).SetLink(Root.gameObject)
                    .OnComplete(() => _newBest.rectTransform.DOScale(1.08f, 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetId(Root).SetLink(Root.gameObject));
            }

            RevealButtons(delay + 0.8f);
        }

        // ---- Lose --------------------------------------------------------------------------------------

        public void ShowLose(float progress)
        {
            Prepare(UIStrings.OhNo, UIColors.Danger);

            foreach (var star in _stars)
                star.gameObject.SetActive(false);

            _scoreLabel.text = UIStrings.Reached;
            _scoreLabel.gameObject.SetActive(true);
            _scoreValue.gameObject.SetActive(true);
            _scoreValue.text = Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f).ToString(CultureInfo.InvariantCulture) + "%";
            _detail.text = UIStrings.Level + _game.Level.ToString(CultureInfo.InvariantCulture);

            SetButton(_primary, UIStrings.TryAgain, UIColors.Good, _game.Restart);
            SetButton(_secondary, UIStrings.Menu, UIColors.Neutral, _game.QuitToMenu);

            Show(true);

            _title.rectTransform.DOShakeAnchorPos(0.5f, new Vector2(18f, 6f), 24, 90f, false, true).SetDelay(0.2f).SetUpdate(true).SetId(Root).SetLink(Root.gameObject);
            RevealButtons(0.7f);
        }

        // ---- Helpers -----------------------------------------------------------------------------------

        private void Prepare(string title, Color titleColor)
        {
            DOTween.Kill(Root);
            _title.text = title;
            _title.color = titleColor;
            _newBest.gameObject.SetActive(false);
            _scoreValue.color = Color.white;
            _title.rectTransform.anchoredPosition = new Vector2(0f, -120f);

            _primaryHolder.gameObject.SetActive(false);
            _secondaryHolder.gameObject.SetActive(false);
        }

        private static void SetButton(UIButton button, string label, Color color, UnityEngine.Events.UnityAction action)
        {
            button.Label.text = label;
            button.Background.color = color;
            button.Button.onClick.RemoveAllListeners();
            button.Button.onClick.AddListener(action);
        }

        private void RevealButtons(float delay)
        {
            DOVirtual.DelayedCall(delay, () =>
            {
                _primaryHolder.gameObject.SetActive(true);
                _primaryHolder.localScale = Vector3.zero;
                _primaryHolder.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true).SetId(Root).SetLink(Root.gameObject)
                    .OnComplete(() => _primaryHolder.DOScale(1.05f, 0.7f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetId(Root).SetLink(Root.gameObject));

                _secondaryHolder.gameObject.SetActive(true);
                _secondaryHolder.localScale = Vector3.zero;
                _secondaryHolder.DOScale(1f, 0.4f).SetDelay(0.12f).SetEase(Ease.OutBack).SetUpdate(true).SetId(Root).SetLink(Root.gameObject);
            }, true).SetId(Root).SetLink(Root.gameObject);
        }
    }
}
