using DG.Tweening;
using UnityEngine;

namespace BlobRunner
{
    /// <summary>
    /// Invisible anchor that cut off body parts gather in. It slides forward a little and sinks to the floor.
    /// It is destroyed when the last part has been taken back by the loot.
    /// </summary>
    public class JellyContainer : MonoBehaviour
    {
        private const float DriftDistance = 2f;
        private const float RestHeight = 0.25f;

        private void Awake()
        {
            // The prefab ships a (static, non convex) mesh collider that nothing uses. Moving it every frame is
            // expensive for the physics engine, so it is removed at runtime.
            var meshCollider = GetComponent<Collider>();
            if (meshCollider != null)
                Destroy(meshCollider);
        }

        public void StartAnimation(float delay = 0f)
        {
            var target = transform.forward * DriftDistance + transform.position;
            target.y = RestHeight;

            transform.DOMove(target, 2f).SetDelay(delay).SetEase(Ease.OutQuad).SetId(this).SetLink(gameObject);
        }

        /// <summary>Destroys the container when no body part is attached to it any more.</summary>
        public void ReleaseIfEmpty()
        {
            if (transform.childCount == 0)
            {
                DOTween.Kill(this);
                Destroy(gameObject);
            }
        }
    }
}
