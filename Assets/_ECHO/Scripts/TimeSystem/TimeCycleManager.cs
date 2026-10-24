using UnityEngine;
namespace EchoGame
{
    public sealed class TimeCycleManager : MonoBehaviour
    {
        [Min(1)] public float Duration = 30;
        public GameManager Game;
        public PlayerController Player;
        public Transform Spawn;
        public EchoRecorder Recorder;
        public EchoManager Echoes;
        public PuzzleController Puzzle;
        public ScanSimulation Scanning;
        public DualPhasePuzzleController PhasePuzzle;
        public string LevelName = "01 · Первый отголосок";
        public string NextLevelScene;
        public LevelCompletion Exit;
        public GameplayUI UI;
        public float Elapsed { get; private set; }
        public int CycleNumber { get; private set; }
        public int Attempts { get; private set; }
        public Level03Controller TimedPuzzle;
        public void MarkEchoUsed(int id) => usedEchoes.Add(id);
        readonly System.Collections.Generic.HashSet<int> usedEchoes = new System.Collections.Generic.HashSet<int>();
        public int UsedEchoCount => usedEchoes.Count;
        public bool AutoStartRecording = true;
        public bool IsRecording { get; private set; }
        public bool HasFinishedRecording { get; private set; }
        public float RecordingStart { get; private set; }
        public float RecordingEnd { get; private set; }
        public event System.Action<float> ClockChanged;
        public event System.Action CycleRestarted, RecordingChanged;
        public event System.Action<string, float> Notification;
        bool plateHint, echoHint;
        bool recordedMechanism;
        void FixedUpdate()
        {
            if (Game.State == GameState.Research && TimedPuzzle) { TimedPuzzle.ResearchTick(Time.fixedDeltaTime); return; }
            if (Game.State != GameState.Playing) return;
            float dt = Mathf.Min(Time.fixedDeltaTime, Duration - Elapsed);
            Player.Simulate(dt);
            Elapsed = Mathf.Min(Duration, Elapsed + dt);
            Echoes.Evaluate(Elapsed);
            if (TimedPuzzle)
            {
                foreach (var echo in Echoes.Echoes) echo.ReplayEvents(Elapsed, TimedPuzzle);
                TimedPuzzle.Tick(dt);
            }
            if (Scanning)
            {
                Scanning.Tick(dt); if (PhasePuzzle) PhasePuzzle.Evaluate(dt, Player.Feet);
                foreach (int id in Scanning.ContributingEchoes) usedEchoes.Add(id);
                recordedMechanism |= IsRecording && Scanning.PlayerTarget;
            }
            if (Puzzle)
            {
                Puzzle.Evaluate(Player, Echoes, dt);
                foreach (var echo in Echoes.Echoes) if (echo.IsActive && Puzzle.Plate.Contains(echo.Feet)) usedEchoes.Add(echo.Id);
                recordedMechanism |= IsRecording && Puzzle.Plate.Contains(Player.Feet);
                if (!plateHint && Puzzle.Plate.IsActive) { plateHint = true; Notification?.Invoke("Платформа держит дверь открытой. Оставьте здесь своё Эхо.", 8); }
            }
            if (IsRecording) Recorder.Capture(Player.Capture(Elapsed));
            ClockChanged?.Invoke(Elapsed);
            if (Exit.Contains(Player) && (!PhasePuzzle || PhasePuzzle.FinalDoorUnlocked)) { IsRecording = false; RecordingEnd = Elapsed; RecordingChanged?.Invoke(); Game.SetState(GameState.LevelCompleted); return; }
            if (Elapsed >= Duration) SaveAndRestart();
        }
        public void SaveAndRestart()
        {
            if (Game.State != GameState.Playing) return;
            if (IsRecording)
            {
                var recording = Recorder.Finish(Player.Capture(Elapsed), recordedMechanism);
                if (recording != null && Echoes.ConcurrentIntervals)
                {
                    var conflict = Echoes.FindConflict(recording);
                    if (conflict)
                    {
                        IsRecording = false; HasFinishedRecording = false; RecordingEnd = Elapsed;
                        TimedPuzzle.Reject("Запись отклонена: пересечение интервалов", Mathf.Max(recording.StartTime, conflict.Recording.StartTime), Mathf.Min(recording.EndTime, conflict.Recording.EndTime), conflict.Id);
                        RecordingChanged?.Invoke(); return;
                    }
                }
                Echoes.Add(recording);
                IsRecording = false; HasFinishedRecording = true; RecordingEnd = Elapsed;
                RecordingChanged?.Invoke();
                Notification?.Invoke("Запись завершена. Начинается новый цикл.", 4);
            }
            Restart(false);
            if (!echoHint && Echoes.Echoes.Count > 0)
            { echoHint = true; Notification?.Invoke(TimedPuzzle ? "Эхо появится в начале сохранённого интервала и повторит нажатие F." : Scanning ? "Эхо воспроизводит ваш маршрут и применение сканера." : "Эхо повторяет ваш маршрут. Пока оно удерживает платформу, идите к выходу.", 9); }
        }
        public void Notify(string message, float seconds = 8) => Notification?.Invoke(message, seconds);
        public void ToggleRecording()
        {
            if (Game.State != GameState.Playing) return;
            if (IsRecording) SaveAndRestart();
            else { RecordingStart = Elapsed; RecordingEnd = Elapsed; HasFinishedRecording = false; IsRecording = true; recordedMechanism = Puzzle && Puzzle.Plate.Contains(Player.Feet); Recorder.Begin(Player.Capture(Elapsed)); RecordingChanged?.Invoke(); }
        }
        public void Restart(bool full)
        {
            if (Game.State == GameState.ResettingCycle) return;
            Game.SetState(GameState.ResettingCycle);
            if (full) { Echoes.Clear(); usedEchoes.Clear(); CycleNumber = 0; Attempts = 0; plateHint = false; echoHint = false; }
            Player.Teleport(Spawn);
            if (Scanning) Scanning.ResetSimulation();
            if (PhasePuzzle) PhasePuzzle.ResetMechanisms();
            if (TimedPuzzle) TimedPuzzle.ResetMechanisms();
            if (Puzzle) Puzzle.ResetMechanisms();
            Elapsed = 0; CycleNumber++; Attempts++;
            Echoes.Evaluate(0);
            foreach (var echo in Echoes.Echoes) echo.ResetEvents();
            Physics.SyncTransforms();
            if (Puzzle) Puzzle.Evaluate(Player, Echoes, 0);
            RecordingStart = 0; RecordingEnd = 0; IsRecording = AutoStartRecording; HasFinishedRecording = false;
            recordedMechanism = false;
            if (IsRecording) Recorder.Begin(Player.Capture(0));
            if (full && !Scanning && !TimedPuzzle) Notification?.Invoke("Доберитесь до выхода. R — завершить запись, T — повторить цикл, Tab — временная шкала.", 10);
            Game.SetState(GameState.Playing);
            CycleRestarted?.Invoke(); RecordingChanged?.Invoke();
        }
    }
}
