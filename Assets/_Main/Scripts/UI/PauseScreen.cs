using UnityEngine;
using UnityEngine.UI;

namespace BlobRunner.UI
{
    /// <summary>Pause menu: resume, restart, back to the menu and the sound / music / vibration switches.</summary>
    public sealed class PauseScreen : UIScreen
    {
        public PauseScreen(Transform canvas, GameManager game) : base(canvas, "Pause")
        {
            // dimmed background that also blocks touches (the steering area lies below)
            var dim = UIFactory.CreateImage(Root, "Dim", null, UIColors.Dim, true);
            UIFactory.Stretch(dim.rectTransform);
            dim.transform.SetAsFirstSibling();

            var card = UIFactory.CreateImage(Content, "Card", UISprites.RoundedRect, UIColors.Panel, false, true);
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 1020f));

            var title = UIFactory.CreateText(card.transform, "Title", UIStrings.Paused, 110, Color.white);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(800f, 140f));

            var resume = UIFactory.CreateButton(card.transform, "Resume", UIStrings.Resume, UIColors.Good, new Vector2(620f, 160f), 78, game.Resume);
            UIFactory.Place(resume.Rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(620f, 160f));

            var restart = UIFactory.CreateButton(card.transform, "Restart", UIStrings.Restart, UIColors.Accent, new Vector2(620f, 130f), 62, game.Restart);
            UIFactory.Place(restart.Rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -460f), new Vector2(620f, 130f));

            var menu = UIFactory.CreateButton(card.transform, "Menu", UIStrings.Menu, UIColors.Neutral, new Vector2(620f, 130f), 62, game.QuitToMenu);
            UIFactory.Place(menu.Rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -620f), new Vector2(620f, 130f));

            var save = game.Save;
            if (save != null)
            {
                AddToggle(card.transform, "Sound", UIStrings.Sound, -255f, () => save.Data.sfxOn, save.SetSfx);
                AddToggle(card.transform, "Music", UIStrings.Music, 0f, () => save.Data.musicOn, save.SetMusic);
                AddToggle(card.transform, "Vibration", UIStrings.Vibration, 255f, () => save.Data.hapticsOn, save.SetHaptics);
            }
        }

        private static void AddToggle(Transform parent, string name, string label, float x, System.Func<bool> get, System.Action<bool> set)
        {
            var toggle = UIFactory.CreateToggle(parent, name, label, new Vector2(240f, 100f), 28, get, set);
            UIFactory.Place(toggle.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 90f), new Vector2(240f, 100f));
        }
    }
}
