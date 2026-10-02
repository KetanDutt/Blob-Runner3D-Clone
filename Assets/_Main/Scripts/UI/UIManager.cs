using System;
using BlobRunner.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlobRunner.UI
{
    /// <summary>
    /// Builds the whole user interface from code (canvas, menu, HUD, pause, result popups, floating texts, damage flash,
    /// scene transition curtain) and drives it from the <see cref="GameManager"/> state.
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        private const int FloatingTextPoolSize = 8;

        private GameManager _game;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private MenuScreen _menu;
        private HudScreen _hud;
        private PauseScreen _pause;
        private ResultScreen _result;
        private Image _curtain;
        private Image _damageFlash;
        private Text[] _floatingTexts;
        private int _nextFloatingText;
        private Camera _camera;

        private void Awake()
        {
            EnsureEventSystem();
            BuildCanvas();

            // Start fully covered: the theme / level are applied in the first frames and must not flash.
            _curtain = UIFactory.CreateImage(_canvas.transform, "Curtain", null, Color.black, true);
            UIFactory.Stretch(_curtain.rectTransform);
        }

        /// <summary>Called once the level is built.</summary>
        public void Initialize(GameManager game)
        {
            _game = game;
            _camera = Camera.main;

            _menu = new MenuScreen(_canvas.transform, game);
            _hud = new HudScreen(_canvas.transform, game);
            _pause = new PauseScreen(_canvas.transform, game);
            _result = new ResultScreen(_canvas.transform, game);

            BuildFloatingTexts();
            BuildDamageFlash();
            JoystickSkin.Apply();

            // overlays must stay above the screens, the curtain above everything
            _damageFlash.transform.SetAsLastSibling();
            _curtain.transform.SetAsLastSibling();

            game.StateChanged += OnStateChanged;
            game.RunWon += OnRunWon;
            game.RunLost += OnRunLost;

            FadeIn(0.45f);
            ApplyState(game.State);
        }

        private void OnDestroy()
        {
            if (_game != null)
            {
                _game.StateChanged -= OnStateChanged;
                _game.RunWon -= OnRunWon;
                _game.RunLost -= OnRunLost;
            }

            if (_menu != null) _menu.Dispose();
            if (_hud != null) _hud.Dispose();
            if (_pause != null) _pause.Dispose();
            if (_result != null) _result.Dispose();

            if (_canvas != null)
                Destroy(_canvas.gameObject);
        }

        private void Update()
        {
            if (_hud != null && _hud.IsVisible)
                _hud.Tick();
        }

        // ---- Construction ------------------------------------------------------------------------------

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
                return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private void BuildCanvas()
        {
            var go = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;

            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 20; // above the steering canvas of the scene
            _canvasRect = go.GetComponent<RectTransform>();

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void BuildFloatingTexts()
        {
            _floatingTexts = new Text[FloatingTextPoolSize];
            for (int i = 0; i < _floatingTexts.Length; i++)
            {
                var text = UIFactory.CreateText(_canvas.transform, "FloatingText" + i, string.Empty, 72, Color.white);
                text.rectTransform.sizeDelta = new Vector2(500f, 120f);
                text.gameObject.SetActive(false);
                _floatingTexts[i] = text;
            }
        }

        private void BuildDamageFlash()
        {
            _damageFlash = UIFactory.CreateImage(_canvas.transform, "DamageFlash", UISprites.Vignette, new Color(1f, 0.1f, 0.1f, 0f));
            UIFactory.Stretch(_damageFlash.rectTransform);
        }

        // ---- State -----------------------------------------------------------------------------------

        private void OnStateChanged(GameState from, GameState to)
        {
            ApplyState(to);
        }

        private void ApplyState(GameState state)
        {
            switch (state)
            {
                case GameState.Menu:
                    _hud.Hide(false);
                    _pause.Hide(false);
                    _result.Hide(false);
                    _menu.Show(false);
                    break;

                case GameState.Playing:
                    _menu.Hide(true);
                    _pause.Hide(true);
                    _hud.Show(true);
                    break;

                case GameState.Paused:
                    _pause.Show(true);
                    break;
            }
        }

        private void OnRunWon(ScoreResult result, bool newBest)
        {
            _hud.Hide(true);
            DOVirtual.DelayedCall(1.4f, () => _result.ShowWin(result, newBest), true).SetLink(gameObject);
        }

        private void OnRunLost()
        {
            _hud.Hide(true);
            DOVirtual.DelayedCall(1.1f, () => _result.ShowLose(_game.Progress), true).SetLink(gameObject);
        }

        // ---- Effects ---------------------------------------------------------------------------------

        /// <summary>Red vignette pulse (damage).</summary>
        public void FlashDamage()
        {
            if (_damageFlash == null)
                return;

            DOTween.Kill(_damageFlash);
            _damageFlash.color = new Color(1f, 0.1f, 0.1f, 0.6f);
            _damageFlash.DOFade(0f, 0.45f).SetEase(Ease.OutQuad).SetUpdate(true).SetId(_damageFlash).SetLink(_damageFlash.gameObject);
        }

        /// <summary>Rising text at the screen position of a world point.</summary>
        public void ShowFloatingText(Vector3 worldPosition, string content, Color color)
        {
            if (_floatingTexts == null)
                return;

            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
                return;

            Vector3 screen = _camera.WorldToScreenPoint(worldPosition);
            if (screen.z <= 0f)
                return;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out local))
                return;

            var text = _floatingTexts[_nextFloatingText];
            _nextFloatingText = (_nextFloatingText + 1) % _floatingTexts.Length;

            DOTween.Kill(text);
            text.gameObject.SetActive(true);
            text.text = content;
            text.color = color;
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            text.rectTransform.anchoredPosition = local;
            text.rectTransform.localScale = Vector3.zero;

            var sequence = DOTween.Sequence().SetUpdate(true).SetId(text).SetLink(text.gameObject);
            sequence.Append(text.rectTransform.DOScale(1f, 0.22f).SetEase(Ease.OutBack));
            sequence.Join(text.rectTransform.DOAnchorPosY(local.y + 170f, 0.9f).SetEase(Ease.OutQuad));
            sequence.Insert(0.55f, text.DOFade(0f, 0.35f));
            sequence.OnComplete(() => text.gameObject.SetActive(false));
        }

        /// <summary>Fades the black curtain in (scene start) or out.</summary>
        public void FadeIn(float duration)
        {
            if (_curtain == null)
                return;

            DOTween.Kill(_curtain);
            _curtain.raycastTarget = true;
            _curtain.DOFade(0f, duration).SetEase(Ease.OutQuad).SetUpdate(true).SetId(_curtain).SetLink(_curtain.gameObject)
                .OnComplete(() => _curtain.raycastTarget = false);
        }

        /// <summary>Covers the screen, then invokes <paramref name="onCovered"/> (used right before reloading the scene).</summary>
        public void FadeOutThen(Action onCovered, float duration)
        {
            if (_curtain == null)
            {
                onCovered();
                return;
            }

            DOTween.Kill(_curtain);
            _curtain.raycastTarget = true;
            _curtain.DOFade(1f, duration).SetEase(Ease.InQuad).SetUpdate(true).SetId(_curtain).SetLink(_curtain.gameObject)
                .OnComplete(() => onCovered());
        }
    }
}
