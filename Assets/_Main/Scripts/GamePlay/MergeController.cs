using UnityEngine;

namespace BlobRunner
{
    /// <summary>Regrows cut off body parts when loot is collected.</summary>
    public class MergeController : MonoBehaviour
    {
        /// <summary>Delay between two parts starting to fly back (gives a pleasant cascade).</summary>
        [SerializeField] private float stagger = 0.045f;

        private Player _player;

        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        /// <summary>
        /// Regrows every cut off part in <paramref name="color"/>, flying in from <paramref name="worldFrom"/>.
        /// Returns the number of parts that were restored.
        /// </summary>
        public int Merge(Color color, Vector3 worldFrom)
        {
            if (_player == null)
                _player = GetComponent<Player>();

            var parts = _player.BodyParts;
            int restored = 0;
            float delay = 0f;

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (!part.HasBroken)
                    continue;

                _player.SetPartColor(part, color);
                part.Restore(worldFrom, delay);

                delay += stagger;
                restored++;
            }

            return restored;
        }
    }
}
