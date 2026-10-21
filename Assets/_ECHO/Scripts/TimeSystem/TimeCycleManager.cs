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
        public LevelCompletion Exit;
        public GameplayUI UI;
        public float Elapsed { get; private set; }
        public int CycleNumber { get; private set; }
        public int Attempts { get; private set; }
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
            if (Game.State != GameState.Playing) return;
            float dt = Mathf.Min(Time.fixedDeltaTime, Duration - Elapsed);
            Player.Simulate(dt);
            Elapsed = Mathf.Min(Duration, Elapsed + dt);
            Echoes.Evaluate(Elapsed);
            if (IsRecording) Recorder.Capture(Player.Capture(Elapsed));
            Puzzle.Evaluate(Player, Echoes, dt);
            foreach (var echo in Echoes.Echoes) if (echo.IsActive && Puzzle.Plate.Contains(echo.Feet)) usedEchoes.Add(echo.Id);
            recordedMechanism |= IsRecording && Puzzle.Plate.Contains(Player.Feet);
            if (!plateHint && Puzzle.Plate.IsActive) { plateHint = true; Notification?.Invoke("Платформа держит дверь открытой. Оставьте здесь своё Эхо.", 8); }
            ClockChanged?.Invoke(Elapsed);
            if (Exit.Contains(Player)) { IsRecording = false; RecordingEnd = Elapsed; RecordingChanged?.Invoke(); Game.SetState(GameState.LevelCompleted); return; }
            if (Elapsed >= Duration) SaveAndRestart();
        }
        public void SaveAndRestart()
        {
            if (Game.State != GameState.Playing) return;
            if (IsRecording)
            {
                Echoes.Add(Recorder.Finish(Player.Capture(Elapsed), recordedMechanism));
                IsRecording = false; HasFinishedRecording = true; RecordingEnd = Elapsed;
                RecordingChanged?.Invoke();
                Notification?.Invoke("Запись завершена. Начинается новый цикл.", 4);
            }
            Restart(false);
            if (!echoHint && Echoes.Echoes.Count > 0)
            { echoHint = true; Notification?.Invoke("Эхо повторяет ваш маршрут. Пока оно удерживает платформу, идите к выходу.", 9); }
        }
        public void ToggleRecording()
        {
            if (Game.State != GameState.Playing) return;
            if (IsRecording) SaveAndRestart();
            else { RecordingStart = Elapsed; RecordingEnd = Elapsed; HasFinishedRecording = false; IsRecording = true; recordedMechanism = Puzzle.Plate.Contains(Player.Feet); Recorder.Begin(Player.Capture(Elapsed)); RecordingChanged?.Invoke(); }
        }
        public void Restart(bool full)
        {
            if (Game.State == GameState.ResettingCycle) return;
            Game.SetState(GameState.ResettingCycle);
            if (full) { Echoes.Clear(); usedEchoes.Clear(); CycleNumber = 0; Attempts = 0; plateHint = false; echoHint = false; }
            Player.Teleport(Spawn);
            Puzzle.ResetMechanisms();
            Elapsed = 0; CycleNumber++; Attempts++;
            Echoes.Evaluate(0);
            Physics.SyncTransforms();
            Puzzle.Evaluate(Player, Echoes, 0);
            RecordingStart = 0; RecordingEnd = 0; IsRecording = AutoStartRecording; HasFinishedRecording = false;
            recordedMechanism = false;
            if (IsRecording) Recorder.Begin(Player.Capture(0));
            if (full) Notification?.Invoke("Доберитесь до выхода. R — завершить запись, T — повторить цикл, Tab — временная шкала.", 10);
            Game.SetState(GameState.Playing);
            CycleRestarted?.Invoke(); RecordingChanged?.Invoke();
        }
    }
}
