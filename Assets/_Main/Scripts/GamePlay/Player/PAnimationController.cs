using BlobRunner.Core;
using DG.Tweening;
using UnityEngine;

namespace BlobRunner
{
    /// <summary>Thin wrapper around the player <see cref="Animator"/> (parameters are hashed once).</summary>
    public class PAnimationController : MonoBehaviour
    {
        private static readonly int SpeedRate = Animator.StringToHash("speed");
        private static readonly int ShouldOnStand = Animator.StringToHash("shouldOnStand");
        private static readonly int ShouldOnRight = Animator.StringToHash("shouldOnRight");
        private static readonly int ShouldOnLeft = Animator.StringToHash("shouldOnLeft");
        private static readonly int ShouldOnCrawl = Animator.StringToHash("shouldOnCrawl");

        private Animator _animator;
        private float _speed = 1f;

        /// <summary>The visual root (the object with the Animator), used for idle / celebration animations.</summary>
        public Transform ModelRoot { get { return _animator != null ? _animator.transform : null; } }

        public float Speed { get { return _speed; } }

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }

        /// <summary>Switches between running upright, hopping on one leg and crawling.</summary>
        public void Apply(PlayerState state)
        {
            if (_animator == null)
                return;

            _animator.SetBool(ShouldOnStand, state == PlayerState.OnStandRun);
            _animator.SetBool(ShouldOnLeft, state == PlayerState.OnLeftRun);
            _animator.SetBool(ShouldOnRight, state == PlayerState.OnRightRun);
            _animator.SetBool(ShouldOnCrawl, state == PlayerState.OnCrawlRun);
        }

        /// <summary>Animation playback speed multiplier (0 freezes the pose).</summary>
        public void SetSpeed(float value)
        {
            _speed = value;
            if (_animator != null)
                _animator.SetFloat(SpeedRate, value);
        }

        public void TweenSpeed(float target, float duration)
        {
            DOTween.Kill(this);
            DOTween.To(() => _speed, SetSpeed, target, duration).SetEase(Ease.OutQuad).SetId(this).SetLink(gameObject);
        }

        public void DisableAnimator()
        {
            if (_animator != null)
                _animator.enabled = false;
        }

        /// <summary>Gentle "breathing" of the model while the pose is frozen (main menu).</summary>
        public void StartIdle()
        {
            var model = ModelRoot;
            if (model == null)
                return;

            DOTween.Kill(model);
            model.localScale = Vector3.one;
            model.DOScale(new Vector3(1.025f, 0.975f, 1.025f), 0.9f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetId(model)
                .SetLink(model.gameObject);
        }

        public void StopIdle()
        {
            var model = ModelRoot;
            if (model == null)
                return;

            DOTween.Kill(model);
            model.localScale = Vector3.one;
        }
    }
}
