using System;
using BlobRunner.Effects;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BlobRunner.UI
{
    /// <summary>Palette shared by every screen.</summary>
    public static class UIColors
    {
        public static readonly Color Panel = new Color(0.08f, 0.10f, 0.20f, 0.94f);
        public static readonly Color PanelSoft = new Color(0.08f, 0.10f, 0.20f, 0.62f);
        public static readonly Color Dim = new Color(0.02f, 0.03f, 0.08f, 0.66f);
        public static readonly Color Accent = new Color(1.00f, 0.63f, 0.00f);
        public static readonly Color Good = new Color(0.18f, 0.80f, 0.44f);
        public static readonly Color Warning = new Color(1.00f, 0.80f, 0.20f);
        public static readonly Color Danger = new Color(0.95f, 0.28f, 0.30f);
        public static readonly Color Neutral = new Color(0.36f, 0.42f, 0.62f);
        public static readonly Color Off = new Color(0.30f, 0.33f, 0.45f);
        public static readonly Color White = Color.white;
        public static readonly Color TextShadow = new Color(0f, 0f, 0f, 0.55f);
    }

    /// <summary>All user visible strings (single place to translate).</summary>
    public static class UIStrings
    {
        public const string Title1 = "BLOB";
        public const string Title2 = "RUNNER";
        public const string Play = "PLAY";
        public const string TapToStart = "TAP ANYWHERE TO START";
        public const string Level = "LEVEL ";
        public const string Best = "BEST ";
        public const string Paused = "PAUSED";
        public const string Resume = "RESUME";
        public const string Restart = "RESTART";
        public const string Menu = "MENU";
        public const string Sound = "SOUND";
        public const string Music = "MUSIC";
        public const string Vibration = "VIBRATE";
        public const string On = "ON";
        public const string Off = "OFF";
        public const string LevelComplete = "LEVEL COMPLETE!";
        public const string NextLevel = "NEXT LEVEL";
        public const string Replay = "REPLAY";
        public const string OhNo = "OH NO!";
        public const string TryAgain = "TRY AGAIN";
        public const string Score = "SCORE";
        public const string Jelly = "JELLY";
        public const string NewBest = "NEW BEST!";
        public const string Reached = "REACHED";
        public const string DragHint = "DRAG LEFT & RIGHT TO STEER";
        public const string Ouch = "OUCH!";
        public const string Nice = "NICE!";
    }

    /// <summary>Sprites generated at runtime (no texture assets required).</summary>
    public static class UISprites
    {
        private static Sprite _roundedRect;
        private static Sprite _circle;
        private static Sprite _star;
        private static Sprite _vignette;
        private static Sprite _fadeDown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _roundedRect = null;
            _circle = null;
            _star = null;
            _vignette = null;
            _fadeDown = null;
        }

        /// <summary>9-sliced rounded rectangle (white). Use with <see cref="Image.Type.Sliced"/>.</summary>
        public static Sprite RoundedRect
        {
            get
            {
                if (_roundedRect == null)
                {
                    const int size = 96;
                    const float radius = 34f;
                    var texture = ProceduralTextures.Create("UI_RoundedRect", size, size, (u, v) =>
                    {
                        // signed distance to a rounded square, anti-aliased over ~1.2 pixels
                        float x = Mathf.Abs(u * size - size * 0.5f) - (size * 0.5f - radius);
                        float y = Mathf.Abs(v * size - size * 0.5f) - (size * 0.5f - radius);
                        float outside = Mathf.Sqrt(Mathf.Max(x, 0f) * Mathf.Max(x, 0f) + Mathf.Max(y, 0f) * Mathf.Max(y, 0f));
                        float d = outside + Mathf.Min(Mathf.Max(x, y), 0f) - radius;
                        return new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d / 1.2f));
                    });
                    _roundedRect = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                        SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
                }

                return _roundedRect;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circle == null)
                {
                    const int size = 128;
                    var texture = ProceduralTextures.Create("UI_Circle", size, size, (u, v) =>
                    {
                        float x = (u - 0.5f) * size;
                        float y = (v - 0.5f) * size;
                        float d = Mathf.Sqrt(x * x + y * y) - size * 0.5f + 1f;
                        return new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d / 1.2f));
                    });
                    _circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                }

                return _circle;
            }
        }

        public static Sprite Star
        {
            get
            {
                if (_star == null)
                {
                    var texture = ProceduralTextures.Star;
                    _star = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }

                return _star;
            }
        }

        /// <summary>Transparent centre, white edges: tint it red for a damage flash.</summary>
        public static Sprite Vignette
        {
            get
            {
                if (_vignette == null)
                {
                    const int size = 128;
                    var texture = ProceduralTextures.Create("UI_Vignette", size, size, (u, v) =>
                    {
                        float x = u * 2f - 1f;
                        float y = v * 2f - 1f;
                        float r = Mathf.Sqrt(x * x + y * y);
                        float a = Mathf.SmoothStep(0.45f, 1.25f, r);
                        return new Color(1f, 1f, 1f, a);
                    });
                    _vignette = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                }

                return _vignette;
            }
        }

        /// <summary>Vertical gradient, opaque at the top and transparent at the bottom (white).</summary>
        public static Sprite FadeDown
        {
            get
            {
                if (_fadeDown == null)
                {
                    var texture = ProceduralTextures.Create("UI_FadeDown", 4, 64, (u, v) => new Color(1f, 1f, 1f, v * v));
                    _fadeDown = Sprite.Create(texture, new Rect(0, 0, 4, 64), new Vector2(0.5f, 0.5f), 100f);
                }

                return _fadeDown;
            }
        }
    }

    /// <summary>A button created by <see cref="UIFactory.CreateButton"/>.</summary>
    public struct UIButton
    {
        public Button Button;
        public Image Background;
        public Text Label;
        public RectTransform Rect;
    }

    /// <summary>Helpers that build UGUI objects from code (the UI has no prefabs, so it can be reviewed as plain code).</summary>
    public static class UIFactory
    {
        private static Font _font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _font = null;
        }

        /// <summary>Built-in Arial (always available, no import required).</summary>
        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    if (_font == null)
                        _font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                }

                return _font;
            }
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Fixed size rect positioned relative to an anchor.</summary>
        public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        public static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color, bool raycast = false, bool sliced = false)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            image.type = (sliced && sprite != null) ? Image.Type.Sliced : Image.Type.Simple;
            return image;
        }

        public static Text CreateText(Transform parent, string name, string content, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold, bool outline = true)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
            text.raycastTarget = false;

            if (outline)
            {
                var effect = rect.gameObject.AddComponent<Outline>();
                effect.effectColor = UIColors.TextShadow;
                effect.effectDistance = new Vector2(size * 0.045f, -size * 0.045f);
            }

            return text;
        }

        /// <summary>Rounded button with a centred label, press feedback and click sound.</summary>
        public static UIButton CreateButton(Transform parent, string name, string label, Color color, Vector2 size, int fontSize, UnityAction onClick)
        {
            var background = CreateImage(parent, name, UISprites.RoundedRect, color, true, true);
            background.rectTransform.sizeDelta = size;

            var text = CreateText(background.transform, "Label", label, fontSize, Color.white);
            Stretch(text.rectTransform);

            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None; // feedback is done by UIButtonFx
            if (onClick != null)
                button.onClick.AddListener(onClick);

            background.gameObject.AddComponent<UIButtonFx>();

            return new UIButton { Button = button, Background = background, Label = text, Rect = background.rectTransform };
        }

        /// <summary>Button that shows an on / off state (<c>LABEL: ON</c>).</summary>
        public static UIButton CreateToggle(Transform parent, string name, string label, Vector2 size, int fontSize, Func<bool> getValue, Action<bool> setValue)
        {
            UIButton button = default(UIButton);
            Action refresh = () =>
            {
                bool on = getValue();
                button.Background.color = on ? UIColors.Good : UIColors.Off;
                button.Label.text = label + "  " + (on ? UIStrings.On : UIStrings.Off);
            };

            button = CreateButton(parent, name, label, UIColors.Good, size, fontSize, () =>
            {
                setValue(!getValue());
                refresh();
            });

            // the state may also change elsewhere (e.g. pause menu vs. main menu)
            button.Button.gameObject.AddComponent<UIRefreshOnEnable>().Callback = refresh;
            refresh();
            return button;
        }
    }

    /// <summary>Invokes a callback whenever the object becomes active (keeps toggle buttons in sync).</summary>
    public sealed class UIRefreshOnEnable : MonoBehaviour
    {
        public Action Callback;

        private void OnEnable()
        {
            if (Callback != null)
                Callback();
        }
    }
}
