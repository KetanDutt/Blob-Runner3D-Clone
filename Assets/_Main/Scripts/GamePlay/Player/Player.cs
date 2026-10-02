using System;
using System.Collections.Generic;
using BlobRunner.Core;
using BlobRunner.Rendering;
using UnityEngine;

namespace BlobRunner
{
    /// <summary>
    /// The jelly runner. Owns the body parts, aggregates their state and raises gameplay events:
    /// <see cref="Hit"/> (an obstacle cut something off), <see cref="LootCollected"/>, <see cref="Died"/>.
    /// </summary>
    [RequireComponent(typeof(PController), typeof(PAnimationController))]
    public class Player : MonoBehaviour
    {
        /// <summary>Aggregated result of the obstacle hits of one frame.</summary>
        public struct HitInfo
        {
            public Vector3 Position;
            public Color Color;
            public int PartsLost;
            public BodyPartState Origin;
        }

        private readonly Dictionary<BodyPartState, BodyPart> _byState = new Dictionary<BodyPartState, BodyPart>();

        private BodyPart[] _bodyParts = new BodyPart[0];
        private PController _controller;
        private MergeController _merge;
        private Material _material;
        private int _brokenCount;
        private bool _dead;
        private bool _finished;
        private bool _hitPending;
        private HitInfo _pendingHit;

        /// <summary>Raised once per frame in which at least one body part was cut off.</summary>
        public event Action<HitInfo> Hit;

        /// <summary>Raised when loot was picked up: (loot, number of regrown parts).</summary>
        public event Action<LootContainer, int> LootCollected;

        /// <summary>Raised whenever the set of attached parts changes (cut or regrown).</summary>
        public event Action PartsChanged;

        /// <summary>Raised when the last body part has been cut off.</summary>
        public event Action Died;

        public BodyPart[] BodyParts { get { return _bodyParts; } }

        public PController Controller { get { return _controller; } }

        public int PartCount { get { return _bodyParts.Length; } }

        public int PartsAlive { get { return _bodyParts.Length - _brokenCount; } }

        /// <summary>Share (0..1) of the body parts that are still attached.</summary>
        public float Integrity { get { return _bodyParts.Length == 0 ? 0f : (float)PartsAlive / _bodyParts.Length; } }

        public bool HasDead { get { return _dead; } }

        public bool HasFinished { get { return _finished; } }

        /// <summary>False once the run is over; loot and obstacles are ignored afterwards.</summary>
        public bool IsInteractive { get { return !_dead && !_finished; } }

        private void Awake()
        {
            _bodyParts = GetComponentsInChildren<BodyPart>(true);
            _controller = GetComponent<PController>();
            _merge = GetComponent<MergeController>();
            if (_merge == null)
                _merge = gameObject.AddComponent<MergeController>();

            var provider = GetComponentInChildren<TransformProvider>(true);
            if (provider != null)
                _material = provider.RuntimeMaterial;

            for (int i = 0; i < _bodyParts.Length; i++)
            {
                var part = _bodyParts[i];
                if (!_byState.ContainsKey(part.State))
                    _byState.Add(part.State, part);
            }

            ValidateSetup();
        }

        private void LateUpdate()
        {
            // Hits that happened in the physics step(s) of this frame are reported together, so one
            // multi-part cut produces a single burst of sound / particles / camera shake.
            if (_hitPending)
            {
                _hitPending = false;
                var handler = Hit;
                if (handler != null)
                    handler(_pendingHit);
            }

            if (!_dead && !_finished && _bodyParts.Length > 0 && _brokenCount >= _bodyParts.Length)
                Die();
        }

        public BodyPart FindBodyPart(BodyPartState searchingPart)
        {
            BodyPart part;
            return _byState.TryGetValue(searchingPart, out part) ? part : null;
        }

        // ---- Colours -----------------------------------------------------------------------------

        public Color GetPartColor(BodyPart part)
        {
            if (_material == null || part == null || string.IsNullOrEmpty(part.ShaderColorParam))
                return Color.white;

            return _material.GetColor(part.ShaderColorParam);
        }

        public void SetPartColor(BodyPart part, Color color)
        {
            if (_material == null || part == null || string.IsNullOrEmpty(part.ShaderColorParam))
                return;

            _material.SetColor(part.ShaderColorParam, color);
        }

        // ---- Obstacles -----------------------------------------------------------------------------

        /// <summary>Called by a <see cref="BodyPart"/> whose trigger touched an obstacle.</summary>
        internal void NotifyObstacleHit(BodyPart origin)
        {
            if (!IsInteractive || origin == null || origin.HasBroken)
                return;

            Vector3 hitPosition = origin.transform.position;
            Color hitColor = GetPartColor(origin);

            JellyContainer container = origin.CreateContainer();
            float vanishDelay = ComputeVanishDelay();

            // The hit part and everything that depends on it (a hip hit takes both legs, ...).
            int lost = BreakPart(origin, container, vanishDelay);
            var related = origin.RelatedBodyParts;
            if (related != null)
            {
                for (int i = 0; i < related.Length; i++)
                    lost += BreakPart(related[i], container, vanishDelay);
            }

            if (lost == 0)
            {
                container.ReleaseIfEmpty();
                return;
            }

            _brokenCount += lost;
            container.StartAnimation();

            if (!_hitPending)
            {
                _hitPending = true;
                _pendingHit = new HitInfo { Position = hitPosition, Color = hitColor, PartsLost = lost, Origin = origin.State };
            }
            else
            {
                _pendingHit.PartsLost += lost;
            }

            RaisePartsChanged();
        }

        private static int BreakPart(BodyPart part, JellyContainer container, float vanishDelay)
        {
            if (part == null || part.HasBroken)
                return 0;

            part.Break(container, vanishDelay);
            return 1;
        }

        /// <summary>
        /// Cut off pieces shrink away shortly before they would leave the ray marching volume
        /// (a 10 unit long box around the blob), instead of being clipped abruptly.
        /// </summary>
        private float ComputeVanishDelay()
        {
            float speed = _controller != null ? Mathf.Max(1f, _controller.CurrentSpeed) : 4f;
            return Mathf.Clamp(5.2f / speed - 0.4f, 0.25f, 1.2f);
        }

        // ---- Loot ------------------------------------------------------------------------------------

        internal void CollectLoot(LootContainer loot)
        {
            if (!IsInteractive || loot == null)
                return;

            int restored = _merge != null ? _merge.Merge(loot.LootColor, loot.transform.position) : 0;
            RecountBroken();

            var handler = LootCollected;
            if (handler != null)
                handler(loot, restored);

            if (restored > 0)
                RaisePartsChanged();
        }

        // ---- Run lifecycle -----------------------------------------------------------------------------

        public void Finish()
        {
            if (_finished || _dead)
                return;

            _finished = true;
            if (_controller != null)
                _controller.FinishRun();
        }

        private void Die()
        {
            if (_dead)
                return;

            _dead = true;
            if (_controller != null)
                _controller.StopMovement();

            var handler = Died;
            if (handler != null)
                handler();
        }

        private void RecountBroken()
        {
            int broken = 0;
            for (int i = 0; i < _bodyParts.Length; i++)
            {
                if (_bodyParts[i].HasBroken)
                    broken++;
            }

            _brokenCount = broken;
        }

        private void RaisePartsChanged()
        {
            var handler = PartsChanged;
            if (handler != null)
                handler();
        }

        // ---- Setup validation (editor + development builds) ----------------------------------------------

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void ValidateSetup()
        {
            var seen = new HashSet<BodyPartState>();
            for (int i = 0; i < _bodyParts.Length; i++)
            {
                var part = _bodyParts[i];
                if (part.State == BodyPartState.None)
                    Debug.LogError("[Player] Body part '" + part.name + "' has no body state assigned.", part);
                else if (!seen.Add(part.State))
                    Debug.LogError("[Player] Body state " + part.State + " is assigned to more than one part (see '" + part.name + "'). " +
                                   "The limp / crawl animation relies on unique states.", part);

                var related = part.RelatedBodyParts;
                if (related == null || related.Length == 0)
                {
                    Debug.LogWarning("[Player] Body part '" + part.name + "' has no related body parts; it will not be cut off.", part);
                    continue;
                }

                for (int k = 0; k < related.Length; k++)
                {
                    if (related[k] == null)
                        Debug.LogError("[Player] Body part '" + part.name + "' has an empty entry in its related parts.", part);
                }
            }

            if (_material == null)
                Debug.LogWarning("[Player] No ray marching material found: part colours will not change.", this);
        }
    }
}
