using System;
using BlobRunner.Core;
using BlobRunner.Effects;
using BlobRunner.Levels;
using BlobRunner.Services;
using BlobRunner.UI;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlobRunner
{
    /// <summary>
    /// Owns the flow of a run: Menu -> Playing (-> Paused) -> Won / Lost, scoring, progression and restarting.
    ///
    /// The scene is reloaded to start a new run, which resets every object in one go; the long lived services
    /// (<see cref="GameServices"/>) survive the reload. <see cref="AutoStartNextLoad"/> skips the menu after a reload.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        /// <summary>Set before reloading the scene to start running immediately (retry / next level).</summary>
        public static bool AutoStartNextLoad;

        public static GameManager Instance { get; private set; }

        public static bool IsPlaying
        {
            get { return Instance != null && Instance.State == GameState.Playing; }
        }

        [SerializeField] private Player player = null;

        private readonly GameStateMachine _machine = new GameStateMachine();

        private SaveService _save;
        private LevelBuilder _levelBuilder;
        private BlobShadow _shadow;
        private PAnimationController _animation;
        private CameraDirector _camera;
        private VfxDirector _vfx;
        private GameFeedback _feedback;
        private UIManager _ui;
        private bool _reloading;
        private bool _started;
        private bool _tutorialPending;

        // ---- Public state -----------------------------------------------------------------------------

        public GameState State { get { return _machine.Current; } }

        public Player Player { get { return player; } }

        public LevelPlan Plan { get; private set; }

        /// <summary>Level being played (1-based).</summary>
        public int Level { get; private set; }

        /// <summary>0..1 progress along the track.</summary>
        public float Progress { get; private set; }

        public float RunTime { get; private set; }

        public int LootCount { get; private set; }

        public ScoreResult LastResult { get; private set; }

        public bool IsNewBest { get; private set; }

        public int BestScore { get { return _save != null ? _save.Data.bestScore : 0; } }

        public SaveService Save { get { return _save; } }

        public CameraDirector CameraFx { get { return _camera; } }

        public VfxDirector Vfx { get { return _vfx; } }

        public UIManager UI { get { return _ui; } }

        /// <summary>True while the "drag to steer" hint should be shown (first level, never steered yet).</summary>
        public bool TutorialPending { get { return _tutorialPending; } }

        // ---- Events -----------------------------------------------------------------------------------

        public event Action<GameState, GameState> StateChanged;

        public event Action<Player.HitInfo> PlayerHit;

        /// <summary>(loot, number of regrown body parts)</summary>
        public event Action<LootContainer, int> LootCollected;

        /// <summary>(score breakdown, is new best)</summary>
        public event Action<ScoreResult, bool> RunWon;

        public event Action RunLost;

        // ---- Unity ------------------------------------------------------------------------------------

        // With "Enter Play Mode Options" (domain reload disabled) statics survive between play sessions.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            AutoStartNextLoad = false;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (player == null)
                player = FindObjectOfType<Player>();

            _save = GameServices.Save;
            Level = _save != null ? _save.Data.level : 1;
            _tutorialPending = _save != null && !_save.Data.tutorialDone && Level <= 1;

            _machine.Changed += OnMachineChanged;

            RemoveDevOverlayInReleaseBuilds();

            _camera = gameObject.AddComponent<CameraDirector>();
            _vfx = gameObject.AddComponent<VfxDirector>();
            _feedback = gameObject.AddComponent<GameFeedback>();
            _ui = gameObject.AddComponent<UIManager>();
        }

        private void Start()
        {
            if (Instance != this)
                return; // duplicate manager (destroyed in Awake)

            if (player == null)
            {
                Debug.LogError("[GameManager] There is no Player in the scene; the game can not start.");
                enabled = false;
                return;
            }

            _animation = player.GetComponent<PAnimationController>();

            _levelBuilder = new LevelBuilder(player);
            Plan = _levelBuilder.Build(Level);
            player.Controller.Configure(Plan.PlayerSpeed);

            player.Hit += OnPlayerHit;
            player.LootCollected += OnLootCollected;
            player.Died += OnPlayerDied;
            player.Controller.FinishReached += OnFinishReached;
            Joystick.OnJoystickPress += OnSteeringStarted;

            _shadow = BlobShadow.Create(player);
            _vfx.AttachDust(player.transform);

            _ui.Initialize(this);
            _feedback.Initialize(this);

            _started = true;

            if (AutoStartNextLoad)
            {
                AutoStartNextLoad = false;
                _machine.TryTransition(GameState.Playing);
            }
            else
            {
                PoseForMenu();
                _machine.TryTransition(GameState.Menu);
            }
        }

        private void Update()
        {
            if (!_started)
                return;

            if (_machine.Current == GameState.Playing)
            {
                RunTime += Time.deltaTime;
                Progress = Plan.ProgressAt(player.transform.position.z);
            }

            // Escape is also the Android back button
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_machine.Current == GameState.Playing)
                    Pause();
                else if (_machine.Current == GameState.Paused)
                    Resume();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                Pause();
        }

#if !UNITY_EDITOR
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                Pause();
        }
#endif

        private void OnDestroy()
        {
            Joystick.OnJoystickPress -= OnSteeringStarted;

            if (player != null)
            {
                player.Hit -= OnPlayerHit;
                player.LootCollected -= OnLootCollected;
                player.Died -= OnPlayerDied;
                if (player.Controller != null)
                    player.Controller.FinishReached -= OnFinishReached;
            }

            _machine.Changed -= OnMachineChanged;

            if (_levelBuilder != null)
                _levelBuilder.Dispose();

            if (Instance == this)
            {
                Instance = null;
                Time.timeScale = 1f;
            }
        }

        // ---- Commands ---------------------------------------------------------------------------------

        /// <summary>Menu -> Playing.</summary>
        public void StartRun()
        {
            _machine.TryTransition(GameState.Playing);
        }

        public void Pause()
        {
            _machine.TryTransition(GameState.Paused);
        }

        public void Resume()
        {
            if (_machine.Current == GameState.Paused)
                _machine.TryTransition(GameState.Playing);
        }

        /// <summary>Plays the current level again, skipping the menu.</summary>
        public void Restart()
        {
            Reload(true);
        }

        /// <summary>Plays the next level (progress was advanced when the level was won).</summary>
        public void NextLevel()
        {
            Reload(true);
        }

        /// <summary>Reloads the scene and shows the main menu.</summary>
        public void QuitToMenu()
        {
            Reload(false);
        }

        private void Reload(bool autoStart)
        {
            if (_reloading)
                return;

            _reloading = true;
            AutoStartNextLoad = autoStart;
            Time.timeScale = 1f;

            if (_ui != null)
                _ui.FadeOutThen(LoadScene, 0.25f);
            else
                LoadScene();
        }

        private static void LoadScene()
        {
            DOTween.KillAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // ---- State machine ----------------------------------------------------------------------------

        private void OnMachineChanged(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.Playing:
                    if (from == GameState.Paused)
                        Time.timeScale = 1f;
                    else
                        BeginRun();
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;

                case GameState.Won:
                case GameState.Lost:
                    Time.timeScale = 1f;
                    break;
            }

            var handler = StateChanged;
            if (handler != null)
                handler(from, to);
        }

        private void BeginRun()
        {
            Time.timeScale = 1f;
            RunTime = 0f;
            LootCount = 0;
            Progress = 0f;

            if (_save != null)
            {
                _save.Data.RegisterRunStarted();
                _save.MarkDirty();
            }

            if (_animation != null)
            {
                _animation.StopIdle();
                float ratio = Mathf.InverseLerp(4f, 5.5f, Plan.PlayerSpeed);
                _animation.TweenSpeed(Mathf.Lerp(1f, 1.35f, ratio), 0.5f);
            }

            player.Controller.BeginRun();
        }

        /// <summary>Menu pose: the animation is frozen and the blob "breathes".</summary>
        private void PoseForMenu()
        {
            if (_animation == null)
                return;

            _animation.SetSpeed(0f);
            _animation.StartIdle();
        }

        // ---- Gameplay callbacks -----------------------------------------------------------------------

        private void OnPlayerHit(Player.HitInfo info)
        {
            if (_machine.Current != GameState.Playing)
                return;

            var handler = PlayerHit;
            if (handler != null)
                handler(info);
        }

        private void OnLootCollected(LootContainer loot, int regrown)
        {
            if (_machine.Current != GameState.Playing)
                return;

            LootCount++;

            var handler = LootCollected;
            if (handler != null)
                handler(loot, regrown);
        }

        private void OnSteeringStarted()
        {
            if (!_tutorialPending || _machine.Current != GameState.Playing)
                return;

            _tutorialPending = false;
            if (_save != null)
            {
                _save.Data.tutorialDone = true;
                _save.MarkDirty();
            }
        }

        private void OnFinishReached()
        {
            if (_machine.Current != GameState.Playing)
                return;

            float parSeconds = Plan.Length / Mathf.Max(0.1f, Plan.PlayerSpeed) * 1.1f;
            LastResult = ScoreCalculator.Compute(Level, player.PartsAlive, player.PartCount, LootCount, RunTime, parSeconds);
            IsNewBest = LastResult.Score > BestScore;
            Progress = 1f;

            if (_save != null)
            {
                _save.Data.RegisterWin(Level, LastResult.Score, LastResult.Stars, LootCount);
                _save.Save();
            }

            player.Finish();
            _machine.TryTransition(GameState.Won);

            var handler = RunWon;
            if (handler != null)
                handler(LastResult, IsNewBest);
        }

        private void OnPlayerDied()
        {
            if (_machine.Current != GameState.Playing)
                return;

            if (_save != null)
                _save.Save();

            _machine.TryTransition(GameState.Lost);

            var handler = RunLost;
            if (handler != null)
                handler();
        }

        // ---- Misc ---------------------------------------------------------------------------------------

        // The Graphy statistics overlay is a development tool: it is removed from release builds.
        private static void RemoveDevOverlayInReleaseBuilds()
        {
            if (Debug.isDebugBuild)
                return;

            var overlay = GameObject.Find("[Graphy]");
            if (overlay != null)
                Destroy(overlay);
        }
    }
}
