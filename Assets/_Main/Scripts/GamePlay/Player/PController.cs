using System;
using BlobRunner.Core;
using UnityEngine;

namespace BlobRunner
{
    /// <summary>
    /// Runs the blob down the track and steers it sideways (touch joystick or keyboard).
    ///
    /// Movement happens in <c>Update</c> with smoothed speed, so it is independent of the physics rate
    /// (the previous implementation moved the transform in FixedUpdate at 50 Hz, which judders on 60+ Hz screens).
    /// </summary>
    [DisallowMultipleComponent]
    public class PController : MonoBehaviour
    {
        private const string FinishTag = "AFinish";

        [SerializeField] private float speed = 2;

        [SerializeField] private float rotationSpeed = 5;

        [SerializeField] private Vector2 movementLimit = Vector2.one;

        [SerializeField] private GameObject model = null;

        [Header("Feel")]
        [Tooltip("Forward acceleration in units / second² when a run starts.")]
        [SerializeField] private float acceleration = 8f;

        [Tooltip("Sideways acceleration in units / second² (higher = snappier steering).")]
        [SerializeField] private float lateralAcceleration = 28f;

        [Tooltip("Maximum yaw of the model while steering, in degrees.")]
        [SerializeField] private float maxYaw = 28f;

        [Tooltip("Seconds the blob needs to stop after crossing the finish line.")]
        [SerializeField] private float finishBrakeTime = 1.6f;

        private Player _player;
        private PAnimationController _animation;

        private bool _running;
        private bool _finished;
        private bool _stopped;
        private bool _locomotionApplied;

        private float _targetSpeed;
        private float _currentSpeed;
        private float _brakeRate = 12f;
        private float _lateralVelocity;
        private float _centerVelocity;
        private float _yaw;

        private Vector2 _joystick = Vector2.zero;

        /// <summary>Raised when the blob touches the finish gate.</summary>
        public event Action FinishReached;

        public PlayerState CurrentState { get; private set; } = PlayerState.OnStandRun;

        /// <summary>Current forward speed in units / second.</summary>
        public float CurrentSpeed { get { return _currentSpeed; } }

        /// <summary>Forward speed of the current level.</summary>
        public float TargetSpeed { get { return _targetSpeed; } }

        public bool IsRunning { get { return _running && !_finished && !_stopped; } }

        /// <summary>The visual root, used by idle / celebration animations.</summary>
        public GameObject Model { get { return model; } }

        private void Awake()
        {
            _player = GetComponent<Player>();
            _animation = GetComponent<PAnimationController>();
            _targetSpeed = speed;
        }

        private void Start()
        {
            if (_player != null)
                _player.PartsChanged += RefreshLocomotion;

            RefreshLocomotion();
        }

        private void OnDestroy()
        {
            if (_player != null)
                _player.PartsChanged -= RefreshLocomotion;
        }

        private void OnEnable()
        {
            Joystick.OnJoystickDrag += OnDragged;
            Joystick.OnJoystickPress += OnPressed;
            Joystick.OnJoystickRelease += OnReleased;
        }

        private void OnDisable()
        {
            Joystick.OnJoystickDrag -= OnDragged;
            Joystick.OnJoystickPress -= OnPressed;
            Joystick.OnJoystickRelease -= OnReleased;
            _joystick = Vector2.zero;
        }

        // ---- Run control ---------------------------------------------------------------------------

        /// <summary>Sets the forward speed of the level.</summary>
        public void Configure(float forwardSpeed)
        {
            _targetSpeed = Mathf.Max(0f, forwardSpeed);
        }

        public void BeginRun()
        {
            _running = true;
            _finished = false;
            _stopped = false;
            _brakeRate = 12f;
        }

        /// <summary>Stops immediately (all parts lost).</summary>
        public void StopMovement()
        {
            _stopped = true;
            _brakeRate = 14f;
        }

        /// <summary>Slows down smoothly and centres the blob (finish line crossed).</summary>
        public void FinishRun()
        {
            if (_finished)
                return;

            _finished = true;
            _brakeRate = Mathf.Max(2f, _currentSpeed / Mathf.Max(0.2f, finishBrakeTime));
        }

        // ---- Update ----------------------------------------------------------------------------------

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            bool driving = _running && !_finished && !_stopped;

            float forwardTarget = driving ? _targetSpeed : 0f;
            float rate = driving ? acceleration : _brakeRate;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, forwardTarget, rate * dt);

            float steer = driving ? ReadSteering() : 0f;
            float lateralTarget = steer * _targetSpeed;
            _lateralVelocity = Mathf.MoveTowards(_lateralVelocity, lateralTarget, lateralAcceleration * dt);

            Vector3 position = transform.position;
            position.z += _currentSpeed * dt;

            if (_finished && !_stopped)
            {
                // walk to the middle of the lane while braking
                _lateralVelocity = 0f;
                position.x = Mathf.SmoothDamp(position.x, 0f, ref _centerVelocity, 0.45f, Mathf.Infinity, dt);
            }
            else
            {
                position.x += _lateralVelocity * dt;
            }

            if (position.x > movementLimit.y)
            {
                position.x = movementLimit.y;
                if (_lateralVelocity > 0f) _lateralVelocity = 0f;
            }
            else if (position.x < movementLimit.x)
            {
                position.x = movementLimit.x;
                if (_lateralVelocity < 0f) _lateralVelocity = 0f;
            }

            transform.position = position;

            // lean into the turn
            float yawTarget = 0f;
            if (driving && _targetSpeed > 0.01f)
                yawTarget = Mathf.Clamp(_lateralVelocity / _targetSpeed, -1f, 1f) * maxYaw;

            _yaw = Mathf.Lerp(_yaw, yawTarget, 1f - Mathf.Exp(-rotationSpeed * dt));
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        private float ReadSteering()
        {
            float x = _joystick.x;
            if (Mathf.Abs(x) < 0.01f)
                x = Input.GetAxisRaw("Horizontal"); // keyboard / gamepad (editor and desktop)

            return Mathf.Clamp(x, -1f, 1f);
        }

        // ---- Animation state ---------------------------------------------------------------------------

        private void RefreshLocomotion()
        {
            if (_player == null || _animation == null)
                return;

            var leftLeg = _player.FindBodyPart(BodyPartState.LeftLegUpper);
            var rightLeg = _player.FindBodyPart(BodyPartState.RightLegUpper);
            bool leftBroken = leftLeg != null && leftLeg.HasBroken;
            bool rightBroken = rightLeg != null && rightLeg.HasBroken;

            PlayerState state;
            if (leftBroken && rightBroken)
                state = PlayerState.OnCrawlRun;
            else if (leftBroken)
                state = PlayerState.OnRightRun;   // hopping on the right leg
            else if (rightBroken)
                state = PlayerState.OnLeftRun;    // hopping on the left leg
            else
                state = PlayerState.OnStandRun;

            if (_locomotionApplied && state == CurrentState)
                return;

            _locomotionApplied = true;
            CurrentState = state;
            _animation.Apply(state);
        }

        // ---- Input ---------------------------------------------------------------------------------------

        private void OnDragged(Vector2 direction)
        {
            _joystick = direction;
        }

        private void OnReleased()
        {
            _joystick = Vector2.zero;
        }

        private void OnPressed()
        {
            // reserved for tutorials / analytics
        }

        // ---- Finish --------------------------------------------------------------------------------------

        private void OnTriggerEnter(Collider other)
        {
            if (_finished || !other.CompareTag(FinishTag))
                return;

            var handler = FinishReached;
            if (handler != null)
                handler();
        }
    }
}
