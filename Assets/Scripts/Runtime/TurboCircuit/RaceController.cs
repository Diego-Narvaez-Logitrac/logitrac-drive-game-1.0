using UnityEngine;
using UnityEngine.SceneManagement;

namespace LogitracDrive.TurboCircuit
{
    /// <summary>
    /// Turbo Circuit time trial. Builds the track, car, camera and HUD at start (like GamesApp),
    /// advances the simulation in fixed 1/120 s steps, and saves the best lap.
    /// </summary>
    [DisallowMultipleComponent]
    public class RaceController : MonoBehaviour
    {
        public const string SceneName = "TurboCircuit";
        public const string SceneName3D = "TurboCircuit3D";
        public const string BestLapKey = "turbo-circuit.best";
        const float Step = 1f / 120f;
        const float MaxFrame = 0.1f;

        [SerializeField] TurboTuning tuning;
        [Tooltip("Unlit sprite material used by every race renderer.")]
        [SerializeField] Material unlitMaterial;
        [SerializeField] Font bodyFont;
        [SerializeField] Font displayFont;
        [Tooltip("Set by the scene: 2D top-down or 3D perspective. Simulation, HUD and input are shared.")]
        [SerializeField] RaceViewMode viewMode = RaceViewMode.TwoD;
        [Tooltip("3D only: URP Lit and Unlit base materials (cloned per colour at runtime).")]
        [SerializeField] Material litMaterial3D;
        [SerializeField] Material unlitMaterial3D;

        TurboHandling _handling;
        Track _track;
        RaceSim _race;
        IRaceView _view;
        Camera _camera;
        RaceHud _hud;
        float _acc;
        float? _best;

        bool _btnLeft, _btnRight, _keyLeft, _keyRight, _left, _right;
        Side _last = Side.Right;

        public RaceSim Race => _race;
        public RaceHud Hud => _hud;
        public RaceViewMode ViewMode => viewMode;

        /// <summary>Called by the editor scene builder.</summary>
        public void Configure(TurboTuning t, Material unlit, Font body, Font display)
        {
            tuning = t;
            unlitMaterial = unlit;
            bodyFont = body;
            displayFont = display;
        }

        /// <summary>Called by the editor scene builder for the 3D scene.</summary>
        public void Configure3D(Material lit, Material unlit)
        {
            viewMode = RaceViewMode.ThreeD;
            litMaterial3D = lit;
            unlitMaterial3D = unlit;
        }

        void Awake()
        {
            _handling = tuning != null ? tuning.handling : new TurboHandling();
            _track = new Track();
            _race = new RaceSim(_track, _handling);
            if (PlayerPrefs.HasKey(BestLapKey))
            {
                float v = PlayerPrefs.GetFloat(BestLapKey);
                if (v > 0) _best = v;
            }

            var mat = TurboGfx.UnlitMaterial(unlitMaterial);
            _view = viewMode == RaceViewMode.ThreeD
                ? new RaceView3D(litMaterial3D != null ? litMaterial3D : new Material(Shader.Find("Universal Render Pipeline/Lit")),
                                 unlitMaterial3D != null ? unlitMaterial3D : new Material(Shader.Find("Universal Render Pipeline/Unlit")), mat)
                : (IRaceView)new RaceView2D(mat);
            _view.Build(_track);
            _camera = Camera.main;

            GamesApp.EnsureEventSystem();
            var body = bodyFont != null ? bodyFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _hud = new RaceHud();
            _hud.Build(transform, body, displayFont);
            _hud.LeftButton.HeldChanged += held => _btnLeft = held;
            _hud.RightButton.HeldChanged += held => _btnRight = held;
            _hud.ExitButton.onClick.AddListener(BackToPreview);
            _hud.BackButton.onClick.AddListener(BackToPreview);
            _hud.RetryButton.onClick.AddListener(Retry);

            ResetRace();
        }

        // Keep the screen awake while the race scene is active; give control back to the OS otherwise.
        void OnEnable() => Screen.sleepTimeout = SleepTimeout.NeverSleep;
        void OnDisable() => Screen.sleepTimeout = SleepTimeout.SystemSetting;

        void ResetRace()
        {
            _race.Reset();
            _acc = 0f;
            _view.ResetView(_race.car, _camera, Time.time);
        }

        void Update()
        {
            ReadInput();

            float frame = Mathf.Min(MaxFrame, Time.deltaTime);
            _acc += frame;
            var input = new CarInput { left = _left, right = _right, last = _last };
            while (_acc >= Step)
            {
                bool finished = _race.Step(input, Step);
                _view.StepEffects(_race.car, _race.phase == RacePhase.Racing, Step);
                if (finished) OnFinished();
                _acc -= Step;
            }

            var car = _race.car;
            float steer = car.drifting ? car.driftDir : (_right ? 1f : 0f) - (_left ? 1f : 0f);
            _view.Sync(car, steer, frame, Time.time, _handling.maxSpeed, _camera);
            _hud.Refresh(_race, _best, _handling, _btnLeft || _keyLeft, _btnRight || _keyRight);
        }

        void ReadInput()
        {
#if LOGITRAC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            _keyLeft = kb != null && kb.leftArrowKey.isPressed;
            _keyRight = kb != null && kb.rightArrowKey.isPressed;
#else
            _keyLeft = Input.GetKey(KeyCode.LeftArrow);
            _keyRight = Input.GetKey(KeyCode.RightArrow);
#endif
            if (_race.phase == RacePhase.Finished) { _left = _right = false; return; }

            bool left = _btnLeft || _keyLeft, right = _btnRight || _keyRight;
            // Remember the side pressed most recently (drift direction when both are held).
            if (left && !_left) _last = Side.Left;
            if (right && !_right) _last = Side.Right;
            _left = left;
            _right = right;
        }

        void OnFinished()
        {
            float lap = _race.finishedTime;
            bool isBest = _best == null || lap < _best.Value;
            if (isBest)
            {
                _best = lap;
                PlayerPrefs.SetFloat(BestLapKey, lap);
                PlayerPrefs.Save();
            }
            _hud.LeftButton.Release();
            _hud.RightButton.Release();
            _left = _right = false;
            _hud.ShowFinish(lap, _best, isBest);
        }

        public void Retry()
        {
            _hud.HideFinish();
            ResetRace();
        }

        public void BackToPreview()
        {
            GamesApp.PendingPreviewId = GameCatalog.TurboCircuit;
            SceneManager.LoadScene("Games");
        }
    }
}
