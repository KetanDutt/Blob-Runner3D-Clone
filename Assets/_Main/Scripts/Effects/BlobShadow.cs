using UnityEngine;

namespace BlobRunner.Effects
{
    /// <summary>
    /// Cheap fake contact shadow under the blob (the ray marched character casts no real shadows).
    /// It shrinks as the blob loses body parts.
    /// </summary>
    public sealed class BlobShadow : MonoBehaviour
    {
        private const float FloorHeight = 0.115f;

        private Transform _target;
        private Player _player;
        private Transform _quad;
        private Material _material;

        public static BlobShadow Create(Player player)
        {
            var shader = Shader.Find("Sprites/Default");
            if (player == null || shader == null)
                return null;

            var go = new GameObject("BlobShadow");
            var shadow = go.AddComponent<BlobShadow>();
            shadow.Initialize(player, shader);
            return shadow;
        }

        private void Initialize(Player player, Shader shader)
        {
            _player = player;
            _target = player.transform;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "ShadowQuad";
            var collider = quad.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            _quad = quad.transform;
            _quad.SetParent(transform, false);
            _quad.localRotation = Quaternion.Euler(90f, 0f, 0f);

            _material = new Material(shader) { mainTexture = ProceduralTextures.SoftCircle, color = new Color(0f, 0f, 0f, 0.38f), hideFlags = HideFlags.DontSave };

            var meshRenderer = quad.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            LateUpdate();
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            Vector3 p = _target.position;
            transform.position = new Vector3(p.x, FloorHeight, p.z);

            float integrity = _player != null ? _player.Integrity : 1f;
            float scale = Mathf.Lerp(0.55f, 1.15f, integrity);
            _quad.localScale = new Vector3(scale, scale * 1.15f, 1f);
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }
    }
}
