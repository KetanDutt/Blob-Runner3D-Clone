using System.Globalization;
using BlobRunner.Services;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlobRunner.UI
{
    /// <summary>Title screen: tap anywhere (or press PLAY) to start, plus the sound / music / vibration switches.</summary>
    public sealed class MenuScreen : UIScreen
    {
        private readonly GameManager _game;
        private readonly Text _levelText;
        private readonly Text _bestText;
        private readonly RectTransform _title;
        private readonly RectTransform _play;
        private readonly Text _prompt;

        public MenuScreen(Transform canvas, GameManager game) : base(canvas, "Menu")
        {
            _game = game;

            // invisible full screen button: "tap anywhere to start". (alpha is tiny but not zero so the mesh is never culled)
            var catcher = UIFactory.CreateImage(Root, "TapCatcher", null, new Color(0f, 0f, 0f, 0.004f), true);
            UIFactory.Stretch(catcher.rectTransform);
            catcher.transform.SetAsFirstSibling();
            var catcherButton = catcher.gameObject.AddComponent<Button>();
            catcherButton.transition = Selectable.Transition.None;
            catcherButton.onClick.AddListener(game.StartRun);

            // soft gradients behind the texts
            var top = UIFactory.CreateImage(Root, "FadeTop", UISprites.FadeDown, new Color(0f, 0f, 0f, 0.35f));
            UIFactory.Place(top.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1400f, 1000f));
            top.transform.SetSiblingIndex(1);
            var bottom = UIFactory.CreateImage(Root, "FadeBottom", UISprites.FadeDown, new Color(0f, 0f, 0f, 0.35f));
            UIFactory.Place(bottom.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 450f), new Vector2(1400f, 900f));
            bottom.rectTransform.localScale = new Vector3(1f, -1f, 1f); // flipped around its centre: opaque at the bottom edge
            bottom.transform.SetSiblingIndex(2);

            // title
            _title = UIFactory.CreateRect("Title", Content);
            UIFactory.Place(_title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1000f, 520f));

            var blob = UIFactory.CreateText(_title, "Blob", UIStrings.Title1, 230, UIColors.Accent);
            UIFactory.Place(blob.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1000f, 260f));
            var runner = UIFactory.CreateText(_title, "Runner", UIStrings.Title2, 170, Color.white);
            UIFactory.Place(runner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -225f), new Vector2(1000f, 220f));

            // level chip + best score
            var chip = UIFactory.CreateImage(Content, "LevelChip", UISprites.RoundedRect, UIColors.PanelSoft, false, true);
            UIFactory.Place(chip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -700f), new Vector2(440f, 104f));
            _levelText = UIFactory.CreateText(chip.transform, "LevelText", string.Empty, 58, Color.white);
            UIFactory.Stretch(_levelText.rectTransform);

            _bestText = UIFactory.CreateText(Content, "Best", string.Empty, 44, UIColors.Warning);
            UIFactory.Place(_bestText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -835f), new Vector2(800f, 80f));

            // play button + prompt
            var play = UIFactory.CreateButton(Content, "Play", UIStrings.Play, UIColors.Good, new Vector2(620f, 210f), 108, game.StartRun);
            _play = play.Rect;
            UIFactory.Place(_play, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 440f), new Vector2(620f, 210f));

            _prompt = UIFactory.CreateText(Content, "Prompt", UIStrings.TapToStart, 40, Color.white);
            UIFactory.Place(_prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 335f), new Vector2(900f, 80f));

            // settings
            var save = game.Save;
            if (save != null)
            {
                BuildToggle("Sound", UIStrings.Sound, -335f, () => save.Data.sfxOn, save.SetSfx);
                BuildToggle("Music", UIStrings.Music, 0f, () => save.Data.musicOn, save.SetMusic);
                BuildToggle("Vibration", UIStrings.Vibration, 335f, () => save.Data.hapticsOn, save.SetHaptics);
            }
        }

        private void BuildToggle(string name, string label, float x, System.Func<bool> get, System.Action<bool> set)
        {
            var toggle = UIFactory.CreateToggle(Content, name, label, new Vector2(310f, 112f), 34, get, set);
            UIFactory.Place(toggle.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 120f), new Vector2(310f, 112f));
        }

        protected override void OnShown()
        {
            _levelText.text = UIStrings.Level + _game.Level.ToString(CultureInfo.InvariantCulture);

            int best = _game.BestScore;
            _bestText.text = best > 0 ? UIStrings.Best + best.ToString("N0", CultureInfo.InvariantCulture) : string.Empty;

            DOTween.Kill(this);

            _title.localScale = Vector3.one;
            _title.DOScale(1.045f, 1.1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetId(this).SetLink(Root.gameObject);

            _play.localScale = Vector3.one;
            _play.DOScale(1.06f, 0.65f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetId(this).SetLink(Root.gameObject);

            _prompt.color = Color.white;
            _prompt.DOFade(0.35f, 0.8f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetId(this).SetLink(Root.gameObject);
        }

        protected override void OnHidden()
        {
            DOTween.Kill(this);
        }

        public override void Dispose()
        {
            DOTween.Kill(this);
            base.Dispose();
        }
    }
}
