using UnityEngine;

namespace BlobRunner.Levels
{
    /// <summary>Sweeps a bar sideways with a sine motion (uses scaled time, so it freezes while paused).</summary>
    public sealed class SlidingObstacle : MonoBehaviour
    {
        private float _centerX;
        private float _range;
        private float _speed;
        private float _phase;

        public void Initialize(float centerX, float range, float speed, float phase)
        {
            _centerX = centerX;
            _range = range;
            _speed = speed;
            _phase = phase;
            Apply();
        }

        private void Update()
        {
            Apply();
        }

        private void Apply()
        {
            var p = transform.position;
            p.x = _centerX + Mathf.Sin(_phase + Time.time * _speed) * _range;
            transform.position = p;
        }
    }
}
