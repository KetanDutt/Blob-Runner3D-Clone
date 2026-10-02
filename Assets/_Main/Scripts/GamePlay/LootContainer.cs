using BlobRunner.Rendering;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BlobRunner
{
    /// <summary>
    /// A wobbling jelly pickup. Collecting it regrows every cut off body part (in the loot's colour).
    /// </summary>
    public class LootContainer : MonoBehaviour
    {
        private static readonly int ScaleId = Shader.PropertyToID("_Scale");
        private static readonly int ShapeColorId = Shader.PropertyToID("_ShapeColor");

        [SerializeField] private Transform[] lootPieces;

        [SerializeField] private Color lootColor;

        private Material _material;
        private TransformProvider _provider;
        private float _baseScale = 0.1f;
        private bool _collected;

        public Color LootColor { get { return lootColor; } }

        public bool IsCollected { get { return _collected; } }

        private void Awake()
        {
            _provider = GetComponentInChildren<TransformProvider>(true);
            if (_provider != null)
                _material = _provider.RuntimeMaterial;

            if (_material != null && _material.HasProperty(ScaleId))
                _baseScale = _material.GetFloat(ScaleId);
        }

        private void Start()
        {
            StartIdleAnimation();
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }

        /// <summary>Changes the loot colour (runtime generated levels use a palette).</summary>
        public void SetColor(Color color)
        {
            lootColor = color;
            if (_material != null)
                _material.SetColor(ShapeColorId, color);
        }

        // ---- Idle wobble -----------------------------------------------------------------------------

        private void StartIdleAnimation()
        {
            if (lootPieces == null)
                return;

            foreach (var piece in lootPieces)
            {
                if (piece == null)
                    continue;

                // each piece oscillates between its authored position and a random corner of a small cube
                var target = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) / 6.5f;
                piece.DOLocalMove(target, 0.75f)
                    .SetDelay(Random.Range(0f, 0.75f))
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetId(this)
                    .SetLink(gameObject);
            }
        }

        // ---- Collecting ------------------------------------------------------------------------------

        private void OnTriggerEnter(Collider other)
        {
            if (_collected)
                return;

            Player player;
            if (!other.TryGetComponent(out player) || !player.IsInteractive)
                return;

            Collect(player);
        }

        private void Collect(Player player)
        {
            _collected = true;
            DOTween.Kill(this);

            var trigger = GetComponent<Collider>();
            if (trigger != null)
                trigger.enabled = false;

            // the player regrows its parts from the position of the loot ...
            player.CollectLoot(this);

            // ... while the loot itself rises with the player and melts away
            transform.SetParent(player.transform, true);
            transform.DOLocalMove(new Vector3(0f, 1.1f, 0f), 0.5f).SetEase(Ease.OutQuad).SetId(this).SetLink(gameObject);

            if (_material != null)
            {
                DOVirtual.Float(_baseScale, -0.1f, 0.6f, v => _material.SetFloat(ScaleId, v))
                    .SetEase(Ease.InQuad)
                    .SetId(this)
                    .SetLink(gameObject)
                    .OnComplete(() => Destroy(gameObject));
            }
            else
            {
                Destroy(gameObject, 0.6f);
            }
        }
    }
}
