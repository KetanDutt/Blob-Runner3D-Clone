using Cinemachine;
using DG.Tweening;
using UnityEngine;

namespace BlobRunner.Effects
{
    /// <summary>High level camera effects (shake, FOV punches, speed / victory zoom) on top of the Cinemachine rig.</summary>
    public sealed class CameraDirector : MonoBehaviour
    {
        private CameraShakeExtension _shake;

        private bool Available { get { return _shake != null; } }

        private void Awake()
        {
            var virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            if (virtualCamera == null)
            {
                Debug.LogWarning("[Camera] No CinemachineVirtualCamera in the scene; camera effects are disabled.");
                enabled = false;
                return;
            }

            _shake = virtualCamera.GetComponent<CameraShakeExtension>();
            if (_shake == null)
                _shake = virtualCamera.gameObject.AddComponent<CameraShakeExtension>();
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }

        /// <summary>Adds screen shake (0..1).</summary>
        public void Shake(float trauma)
        {
            if (Available)
                _shake.AddTrauma(trauma);
        }

        /// <summary>Quick FOV kick that eases back.</summary>
        public void Punch(float degrees, float duration = 0.35f)
        {
            if (!Available)
                return;

            DOTween.Kill(this, false);
            float baseline = _shake.FovOffset;
            DOTween.Sequence()
                .Append(DOTween.To(() => _shake.FovOffset, v => _shake.FovOffset = v, baseline + degrees, duration * 0.3f).SetEase(Ease.OutQuad))
                .Append(DOTween.To(() => _shake.FovOffset, v => _shake.FovOffset = v, baseline, duration * 0.7f).SetEase(Ease.InOutQuad))
                .SetUpdate(true)
                .SetId(this)
                .SetLink(gameObject);
        }

        /// <summary>Slightly wider view while running (sense of speed); 0 returns to the default.</summary>
        public void SetRunZoom(float degrees, float duration = 0.8f)
        {
            if (!Available)
                return;

            DOTween.Kill(this, false);
            DOTween.To(() => _shake.FovOffset, v => _shake.FovOffset = v, degrees, duration)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true)
                .SetId(this)
                .SetLink(gameObject);
        }
    }
}
