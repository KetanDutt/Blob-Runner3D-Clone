using UnityEngine;

namespace BlobRunner.Rendering
{
    /// <summary>
    /// Feeds the inverse world matrices of a set of transforms to a ray marching material.
    /// The shader evaluates one signed distance primitive per matrix, so moving / scaling a transform moves /
    /// scales the corresponding part of the jelly.
    ///
    /// Notes
    ///  * Runs in <c>LateUpdate</c> (after animation and movement) so the SDF never lags a frame behind the bones.
    ///  * In play mode a private material instance is used, so the shared material asset is never modified and
    ///    several renderers can share one material (a previous version wrote into <c>sharedMaterial</c>).
    ///  * Property ids are cached; no string hashing or allocation happens per frame.
    /// </summary>
    [ExecuteInEditMode]
    [DefaultExecutionOrder(100)]
    public class TransformProvider : MonoBehaviour
    {
        [System.Serializable]
        public class NameTransformPair
        {
            public string name;
            public Transform transform;
        }

        [SerializeField] private Renderer targetRenderer = null;

        [SerializeField] private NameTransformPair[] pairs;

        private int[] _ids;
        private Material _runtimeMaterial;
        private Renderer _renderer;

        /// <summary>
        /// The renderer that receives the matrices. Falls back to the renderer on the same GameObject when the field
        /// is not assigned (the LootT1 prefab ships without a reference and relies on per-instance overrides).
        /// </summary>
        public Renderer TargetRenderer
        {
            get
            {
                if (_renderer == null)
                    _renderer = targetRenderer != null ? targetRenderer : GetComponent<Renderer>();

                return _renderer;
            }
        }

        /// <summary>The per-renderer material instance (play mode only; null in edit mode).</summary>
        public Material RuntimeMaterial
        {
            get
            {
                var target = TargetRenderer;
                if (!Application.isPlaying || target == null)
                    return null;

                if (_runtimeMaterial == null)
                    _runtimeMaterial = target.material; // instantiates once, later calls return the same instance

                return _runtimeMaterial;
            }
        }

        private void OnEnable()
        {
            CacheIds();
        }

        private void OnValidate()
        {
            _renderer = null;
            CacheIds();
        }

        private void LateUpdate()
        {
            Push();
        }

        private void OnDestroy()
        {
            if (Application.isPlaying && _runtimeMaterial != null)
                Destroy(_runtimeMaterial);
        }

        private void CacheIds()
        {
            if (pairs == null)
            {
                _ids = null;
                return;
            }

            _ids = new int[pairs.Length];
            for (int i = 0; i < pairs.Length; i++)
                _ids[i] = Shader.PropertyToID(pairs[i].name);
        }

        private void Push()
        {
            var target = TargetRenderer;
            if (target == null || pairs == null)
                return;

            var material = Application.isPlaying ? RuntimeMaterial : target.sharedMaterial;
            if (material == null)
                return;

            if (_ids == null || _ids.Length != pairs.Length)
                CacheIds();

            for (int i = 0; i < pairs.Length; i++)
            {
                var t = pairs[i].transform;
                if (t == null)
                    continue;

                material.SetMatrix(_ids[i], t.worldToLocalMatrix);
            }
        }
    }
}
