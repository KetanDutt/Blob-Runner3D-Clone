using BlobRunner.Core;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BlobRunner
{
    /// <summary>
    /// One primitive of the jelly (head, torso half, arm segment, leg segment).
    ///
    /// Each part owns a trigger collider that detects obstacles. When hit, the <see cref="Player"/> cuts the part
    /// (and the parts that depend on it) off: they converge in a <see cref="JellyContainer"/>, wobble and shrink
    /// away. Loot later regrows them (<see cref="Restore"/>).
    /// </summary>
    public class BodyPart : MonoBehaviour
    {
        private const string ObstacleTag = "Obstacle";

        /// <summary>World space radius of the wobble of cut off pieces.</summary>
        private const float JiggleRadius = 0.18f;

        private const float VanishScale = 0.04f;

        [SerializeField] private GameObject containerPrefab = null;

        [SerializeField] private string shaderParam = "";

        [SerializeField] private string shaderColorParam = "";

        [SerializeField] private BodyPart[] relatedBodyParts;

        [SerializeField] private BodyPart[] requiredBodyParts;

        [SerializeField] private bool hasBroken = false;

        [SerializeField] private BodyPartState bodyState = BodyPartState.None;

        private Player _owner;
        private Collider[] _colliders;
        private Transform _originalParent;
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private Vector3 _restScale;
        private JellyContainer _container;

        public string ShaderParam { get { return shaderParam; } }

        public string ShaderColorParam { get { return shaderColorParam; } }

        /// <summary>This part plus everything that has to come off with it.</summary>
        public BodyPart[] RelatedBodyParts { get { return relatedBodyParts; } }

        /// <summary>Parts this one hangs on (informational).</summary>
        public BodyPart[] RequiredBodyParts { get { return requiredBodyParts; } }

        public bool HasBroken { get { return hasBroken; } }

        public BodyPartState State { get { return bodyState; } }

        private void Awake()
        {
            _owner = GetComponentInParent<Player>();
            _colliders = GetComponents<Collider>();
            _originalParent = transform.parent;
            _restPosition = transform.localPosition;
            _restRotation = transform.localRotation;
            _restScale = transform.localScale;
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasBroken || _owner == null || !other.CompareTag(ObstacleTag))
                return;

            _owner.NotifyObstacleHit(this);
        }

        // ---- Cutting ---------------------------------------------------------------------------------

        /// <summary>Spawns the (invisible) container the cut off pieces gather in.</summary>
        internal JellyContainer CreateContainer()
        {
            GameObject instance = containerPrefab != null
                ? Instantiate(containerPrefab, transform.position, containerPrefab.transform.rotation)
                : new GameObject("JellyContainer");

            instance.transform.position = transform.position;

            var container = instance.GetComponent<JellyContainer>();
            if (container == null)
                container = instance.AddComponent<JellyContainer>();

            return container;
        }

        internal void Break(JellyContainer container, float vanishDelay)
        {
            hasBroken = true;
            _container = container;

            DOTween.Kill(this);
            SetCollidersEnabled(false);

            var t = transform;
            t.SetParent(container.transform, true);

            // gather in the middle of the container, then wobble a little
            float parentScale = Mathf.Max(0.0001f, container.transform.lossyScale.x);
            float radius = JiggleRadius / parentScale;

            var sequence = DOTween.Sequence().SetId(this).SetLink(gameObject);
            sequence.Append(t.DOLocalMove(Vector3.zero, 0.5f).SetEase(Ease.OutQuad));
            for (int i = 0; i < 2; i++)
            {
                sequence.Append(t.DOLocalMove(Random.insideUnitSphere * radius, Random.Range(0.2f, 0.35f)).SetEase(Ease.InOutSine));
                sequence.Append(t.DOLocalMove(Vector3.zero, Random.Range(0.2f, 0.35f)).SetEase(Ease.InOutSine));
            }

            DOVirtual.DelayedCall(vanishDelay, Vanish, false).SetId(this).SetLink(gameObject);
        }

        private void Vanish()
        {
            if (!hasBroken)
                return;

            transform.DOScale(transform.localScale * VanishScale, 0.4f).SetEase(Ease.InBack).SetId(this).SetLink(gameObject);
        }

        // ---- Regrowing -------------------------------------------------------------------------------

        /// <summary>
        /// Re-attaches the part: it flies from <paramref name="worldFrom"/> to its slot while growing back.
        /// The part counts as attached immediately but stays harmless (no colliders) until it has arrived.
        /// </summary>
        internal void Restore(Vector3 worldFrom, float delay)
        {
            DOTween.Kill(this);
            hasBroken = false;

            var oldContainer = _container;
            _container = null;

            var t = transform;
            t.SetParent(_originalParent, false);
            t.localRotation = _restRotation;
            t.localScale = _restScale * 0.35f;
            t.position = worldFrom;

            var sequence = DOTween.Sequence().SetId(this).SetLink(gameObject);
            if (delay > 0f)
                sequence.AppendInterval(delay);
            sequence.Append(t.DOLocalMove(_restPosition, 0.8f).SetEase(Ease.OutCubic));
            sequence.Join(t.DOScale(_restScale, 0.55f).SetEase(Ease.OutBack));
            sequence.OnComplete(OnRestored);

            if (oldContainer != null)
                oldContainer.ReleaseIfEmpty();
        }

        private void OnRestored()
        {
            // make sure we end up exactly in the rest pose, then become hittable again
            transform.localPosition = _restPosition;
            transform.localScale = _restScale;
            SetCollidersEnabled(true);
        }

        private void SetCollidersEnabled(bool value)
        {
            if (_colliders == null)
                return;

            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null)
                    _colliders[i].enabled = value;
            }
        }
    }
}
