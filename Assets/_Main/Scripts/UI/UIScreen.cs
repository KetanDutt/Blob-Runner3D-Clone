using System;
using DG.Tweening;
using UnityEngine;

namespace BlobRunner.UI
{
    /// <summary>
    /// Base class of the full screen layers. <see cref="Root"/> covers the whole canvas (backgrounds, dimming),
    /// <see cref="Content"/> lies inside the safe area (notches). Fades with unscaled time so it also works while paused.
    /// </summary>
    public abstract class UIScreen
    {
        public RectTransform Root { get; private set; }

        public RectTransform Content { get; private set; }

        public CanvasGroup Group { get; private set; }

        public bool IsVisible { get; private set; }

        protected UIScreen(Transform canvas, string name)
        {
            Root = UIFactory.CreateRect(name, canvas);
            UIFactory.Stretch(Root);

            Group = Root.gameObject.AddComponent<CanvasGroup>();
            Group.alpha = 0f;

            Content = UIFactory.CreateRect("Safe", Root);
            UIFactory.Stretch(Content);
            Content.gameObject.AddComponent<SafeAreaFitter>();

            Root.gameObject.SetActive(false);
        }

        public virtual void Show(bool animate = true)
        {
            if (IsVisible)
                return;

            IsVisible = true;
            DOTween.Kill(Root);

            Root.gameObject.SetActive(true);
            Group.interactable = true;
            Group.blocksRaycasts = true;

            if (animate)
            {
                Group.alpha = 0f;
                Group.DOFade(1f, 0.25f).SetUpdate(true).SetId(Root).SetLink(Root.gameObject);

                Content.localScale = Vector3.one * 0.94f;
                Content.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true).SetId(Root).SetLink(Root.gameObject);
            }
            else
            {
                Group.alpha = 1f;
                Content.localScale = Vector3.one;
            }

            OnShown();
        }

        public virtual void Hide(bool animate = true)
        {
            if (!IsVisible)
                return;

            IsVisible = false;
            DOTween.Kill(Root);

            Group.interactable = false;
            Group.blocksRaycasts = false;

            if (animate)
            {
                Group.DOFade(0f, 0.2f).SetUpdate(true).SetId(Root).SetLink(Root.gameObject)
                    .OnComplete(() => Root.gameObject.SetActive(false));
            }
            else
            {
                Group.alpha = 0f;
                Root.gameObject.SetActive(false);
            }

            OnHidden();
        }

        protected virtual void OnShown()
        {
        }

        protected virtual void OnHidden()
        {
        }

        public virtual void Dispose()
        {
            if (Root != null)
                DOTween.Kill(Root);
        }
    }
}
