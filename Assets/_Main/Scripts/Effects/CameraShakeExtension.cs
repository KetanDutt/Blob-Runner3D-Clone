using Cinemachine;
using UnityEngine;

namespace BlobRunner.Effects
{
    /// <summary>
    /// Cinemachine extension that adds trauma based camera shake and a field of view offset on top of whatever the
    /// virtual camera computes. (Writing to the camera transform directly would be overwritten by the CinemachineBrain.)
    /// </summary>
    public sealed class CameraShakeExtension : CinemachineExtension
    {
        [Tooltip("Maximum positional shake in world units at full trauma.")]
        public float maxOffset = 0.32f;

        [Tooltip("Maximum roll in degrees at full trauma.")]
        public float maxRoll = 2.2f;

        [Tooltip("Noise frequency in Hz.")]
        public float frequency = 24f;

        [Tooltip("Trauma lost per second.")]
        public float decay = 1.9f;

        /// <summary>Added to the lens field of view (degrees). Animated by <c>CameraDirector</c>.</summary>
        public float FovOffset;

        private float _trauma;
        private float _seed;

        public float Trauma { get { return _trauma; } }

        /// <summary>Adds shake (0..1). Trauma accumulates and decays linearly.</summary>
        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _seed = Random.value * 100f;
        }

        private void Update()
        {
            if (_trauma > 0f)
                _trauma = Mathf.Max(0f, _trauma - decay * Time.unscaledDeltaTime);
        }

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage,
            ref CameraState state, float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize)
                return;

            if (_trauma > 0f)
            {
                float shake = _trauma * _trauma; // quadratic: small hits are subtle, big ones violent
                float t = Time.unscaledTime * frequency;

                float nx = Mathf.PerlinNoise(_seed, t) * 2f - 1f;
                float ny = Mathf.PerlinNoise(_seed + 17.3f, t) * 2f - 1f;
                float nr = Mathf.PerlinNoise(_seed + 41.7f, t) * 2f - 1f;

                Vector3 local = new Vector3(nx, ny, 0f) * (maxOffset * shake);
                state.PositionCorrection += state.RawOrientation * local;
                state.OrientationCorrection = state.OrientationCorrection * Quaternion.Euler(0f, 0f, nr * maxRoll * shake);
            }

            if (Mathf.Abs(FovOffset) > 0.001f)
            {
                var lens = state.Lens;
                lens.FieldOfView = Mathf.Clamp(lens.FieldOfView + FovOffset, 20f, 120f);
                state.Lens = lens;
            }
        }
    }
}
