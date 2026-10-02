using BlobRunner.Core;
using BlobRunner.Services;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlobRunner.UI
{
    /// <summary>Squash feedback, click sound and haptics for a UI button. Works while the game is paused (unscaled time).</summary>
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Vector3 _baseScale = Vector3.one;
        private bool _baseCaptured;
        private bool _pressed;

        private void Awake()
        {
            Capture();
        }

        private void OnEnable()
        {
            Capture();
            _pressed = false;
            transform.localScale = _baseScale;
        }

        private void OnDisable()
        {
            DOTween.Kill(this);
            transform.localScale = _baseScale;
        }

        private void Capture()
        {
            if (_baseCaptured)
                return;

            _baseScale = transform.localScale;
            _baseCaptured = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            DOTween.Kill(this);
            transform.DOScale(_baseScale * 0.92f, 0.08f).SetEase(Ease.OutQuad).SetUpdate(true).SetId(this).SetLink(gameObject);

            var audio = GameServices.Audio;
            if (audio != null)
                audio.Play(SfxKind.Click);

            var haptics = GameServices.Haptics;
            if (haptics != null)
                haptics.Play(HapticKind.Selection);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        private void Release()
        {
            if (!_pressed)
                return;

            _pressed = false;
            DOTween.Kill(this);
            transform.DOScale(_baseScale, 0.25f).SetEase(Ease.OutBack).SetUpdate(true).SetId(this).SetLink(gameObject);
        }
    }
}
