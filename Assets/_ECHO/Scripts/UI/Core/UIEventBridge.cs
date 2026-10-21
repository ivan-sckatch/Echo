using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoGame.UI
{
    public sealed class UIEventBridge : MonoBehaviour, IUIDataProvider, IUICommands
    {
        public GameManager Game;
        public TimeCycleManager Cycles;
        public PlayerInteraction InteractionSource;
        public UISnapshot Current { get; private set; } = new UISnapshot();
        public string CurrentNotice { get; private set; }
        public event Action<UISnapshot> Changed;
        public event Action<string, bool> Notice;
        public event Action<bool, string> Interaction;
        public event Action<string, float> InteractionOccurred;
        public event Action CycleRestarted;
        public event Action<UISnapshot> LevelCompleted;
        public event Action<int> SynchronizationError;
        public event Action EscapePressed;
        readonly Dictionary<EchoRecording, IReadOnlyList<ActionInterval>> intervals = new Dictionary<EchoRecording, IReadOnlyList<ActionInterval>>();
        int clockBucket = -1;
        bool subscribed;
        readonly HashSet<int> errors = new HashSet<int>();
        void OnEnable()
        {
            if (!Game || !Cycles) return;
            Game.StateChanged += OnState;
            Game.EscapeRequested += OnEscape;
            Cycles.ClockChanged += OnClock;
            Cycles.CycleRestarted += OnRestart;
            Cycles.RecordingChanged += OnRecording;
            Cycles.Notification += PushNotice;
            Cycles.Echoes.Changed += Publish;
            if (Cycles.PhasePuzzle) Cycles.PhasePuzzle.StateChanged += Publish;
            if (InteractionSource) { InteractionSource.TargetChanged += OnInteraction; InteractionSource.InteractionPerformed += OnInteractionPerformed; }
            subscribed = true; Publish();
        }
        void Start()
        {
            if (!Game || !Cycles) { Debug.LogError("ECHO UI: назначьте GameManager и TimeCycleManager в UIEventBridge.", this); enabled = false; }
        }
        void OnDisable()
        {
            if (!subscribed) return;
            Game.StateChanged -= OnState; Game.EscapeRequested -= OnEscape; Cycles.ClockChanged -= OnClock;
            Cycles.CycleRestarted -= OnRestart; Cycles.RecordingChanged -= OnRecording;
            Cycles.Notification -= PushNotice; Cycles.Echoes.Changed -= Publish;
            if (Cycles.PhasePuzzle) Cycles.PhasePuzzle.StateChanged -= Publish;
            if (InteractionSource) { InteractionSource.TargetChanged -= OnInteraction; InteractionSource.InteractionPerformed -= OnInteractionPerformed; }
            subscribed = false;
        }
        void OnState(GameState state) { Publish(); if (state == GameState.LevelCompleted) LevelCompleted?.Invoke(Current); }
        void OnEscape() { if (EscapePressed != null) EscapePressed.Invoke(); else Game.TogglePause(); }
        void OnClock(float time) { int bucket = Mathf.FloorToInt(time * 10); if (bucket != clockBucket) { clockBucket = bucket; Publish(); } }
        void OnRestart() { clockBucket = -1; errors.Clear(); PruneCache(); Publish(); CycleRestarted?.Invoke(); }
        void OnRecording() => Publish();
        void OnInteraction(bool available, string text) => Interaction?.Invoke(available, text);
        void OnInteractionPerformed(string text) => InteractionOccurred?.Invoke(text, Cycles.Elapsed);
        public void PushNotice(string text, float seconds = 8) { CurrentNotice = text; Notice?.Invoke(text, false); }
        void PruneCache()
        {
            var live = new HashSet<EchoRecording>();
            foreach (var echo in Cycles.Echoes.Echoes) live.Add(echo.Recording);
            var expired = new List<EchoRecording>();
            foreach (var recording in intervals.Keys) if (!live.Contains(recording)) expired.Add(recording);
            foreach (var recording in expired) intervals.Remove(recording);
        }
        public void Publish()
        {
            if (!Cycles || !Game || !Cycles.Echoes) return;
            var list = new List<EchoUIData>(Cycles.Echoes.Echoes.Count);
            foreach (var echo in Cycles.Echoes.Echoes)
            {
                if (echo.Recording == null) continue;
                if (!intervals.TryGetValue(echo.Recording, out var actions))
                { actions = BuildIntervals(echo.Recording); intervals.Add(echo.Recording, actions); }
                list.Add(new EchoUIData { Id = echo.Id, Start = echo.Recording.Frames[0].Time, End = echo.Recording.Duration,
                    State = (EchoState)echo.State, Intervals = actions });
            }
            Current = new UISnapshot {
                Cycle = Cycles.CycleNumber, Time = Cycles.Elapsed, Duration = Cycles.Duration,
                Capacity = Cycles.Echoes.MaximumEchoes, Attempts = Cycles.Attempts, EchoesUsed = Cycles.UsedEchoCount,
                RecordStart = Cycles.RecordingStart, RecordEnd = Cycles.IsRecording ? Cycles.Elapsed : Cycles.RecordingEnd,
                Recording = Cycles.IsRecording ? RecordingState.Recording : Cycles.HasFinishedRecording ? RecordingState.Completed : RecordingState.Waiting,
                Phase = Game.State == GameState.Playing ? SessionPhase.Playing : Game.State == GameState.Paused ? SessionPhase.Paused :
                    Game.State == GameState.Timeline ? SessionPhase.Timeline : Game.State == GameState.LevelCompleted ? SessionPhase.Completed : SessionPhase.Resetting,
                Echoes = list, Level = Cycles.LevelName,
                NextScene = !string.IsNullOrEmpty(Cycles.NextLevelScene) ? Cycles.NextLevelScene :
                    SceneManager.GetActiveScene().name == "Level_01" && Application.CanStreamedLevelBeLoaded("Level_02") ? "Level_02" : null,
                Scanner = BuildScannerData()
            };
            Changed?.Invoke(Current);
            foreach (var echo in list) if (echo.State == EchoState.SyncError && errors.Add(echo.Id))
            { SynchronizationError?.Invoke(echo.Id); Notice?.Invoke($"Эхо {echo.Id:00}: ошибка синхронизации", true); }
        }
        public static IReadOnlyList<ActionInterval> BuildIntervals(EchoRecording recording)
        {
            // Derive real movement/hold intervals from recorded world positions; no fabricated actions.
            var result = new List<ActionInterval>(); var frames = recording.Frames;
            if (frames.Count < 2) return result;
            bool moving = false, scanning = frames[0].Scan.Enabled; string target = frames[0].Scan.TargetId; float start = frames[0].Time;
            for (int i = 1; i < frames.Count; i++)
            {
                float dt = frames[i].Time - frames[i - 1].Time;
                bool current = dt > 0 && Vector3.Distance(frames[i].Position, frames[i - 1].Position) / dt > .3f;
                if (i == 1) moving = current;
                bool scanningNow = frames[i - 1].Scan.Enabled;
                string targetNow = frames[i - 1].Scan.TargetId;
                if ((current != moving || scanning != scanningNow || target != targetNow) && frames[i - 1].Time > start)
                { result.Add(new ActionInterval(start, frames[i - 1].Time, scanning ? "Сканирование" : moving ? "Движение" : "Ожидание", moving, scanning, target)); start = frames[i - 1].Time; moving = current; scanning = scanningNow; target = targetNow; }
            }
            result.Add(new ActionInterval(start, recording.Duration, scanning ? "Сканирование" : moving ? "Движение" : "Ожидание", moving, scanning, target));
            return result;
        }
        ScannerUIData BuildScannerData()
        {
            if (!Cycles.Scanning || !Cycles.PhasePuzzle) return null;
            var world = Cycles.Scanning; var target = world.PlayerTarget; var puzzle = Cycles.PhasePuzzle;
            return new ScannerUIData {
                Enabled = Game.State == GameState.Playing && world.PlayerScanner.Current.Enabled,
                HasTarget = target, Target = target ? target.DisplayName : "", Discovered = target && target.IsDiscovered,
                Activated = target && (target.IsSignaling || target.Mode == PhaseNodeMode.Impulse && target.HasActivated),
                Discovery = target ? target.DiscoveryProgress(0) : 0, Activation = target ? target.ActivationProgress(0) : 0,
                NodeAActive = puzzle.NodeA.IsSignaling, NodeBActive = puzzle.NodeB.IsSignaling,
                NodeAOwner = puzzle.NodeA.SignalOwner, NodeBOwner = puzzle.NodeB.SignalOwner,
                Overlap = puzzle.Overlap, RequiredOverlap = puzzle.RequiredOverlap, DoorUnlocked = puzzle.FinalDoorUnlocked
            };
        }
        public void Resume() => Game.Resume();
        public void Pause() => Game.TogglePause();
        public void SetSensitivity(float sensitivity) => Cycles.Player.MouseSensitivity = sensitivity;
        public void RestartCycle() => Cycles.Restart(false);
        public void ReplayLevel() => Cycles.Restart(true);
        public void MainMenu() { Game.EndSession(); SceneManager.LoadScene("MainMenu"); }
        public void NextLevel()
        { if (Game.State == GameState.LevelCompleted && !string.IsNullOrEmpty(Current.NextScene) && Application.CanStreamedLevelBeLoaded(Current.NextScene)) { Game.EndSession(); SceneManager.LoadScene(Current.NextScene); } }
    }
}
