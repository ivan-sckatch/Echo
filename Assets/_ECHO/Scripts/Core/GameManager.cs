using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace EchoGame
{
    public enum GameState { Initializing, Playing, Paused, ResettingCycle, LevelCompleted, Timeline, Research }
    public sealed class GameManager : MonoBehaviour
    {
        public TimeCycleManager Cycles;
        public PlayerInteraction Interaction;
        public GameState State { get; private set; } = GameState.Initializing;
        public event Action<GameState> StateChanged;
        public event Action EscapeRequested;
        float resumeTimeScale = 1;
        GameState resumeState = GameState.Playing;
        void Start() { if (Cycles.TimedPuzzle) { Cycles.TimedPuzzle.Initialize(); Cycles.TimedPuzzle.EnterResearch(); } else Cycles.Restart(true); }
        void Update()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (Cycles.TimedPuzzle)
            {
                if (State == GameState.Research && k.enterKey.wasPressedThisFrame) { Cycles.TimedPuzzle.StartCycle(); return; }
                if ((State == GameState.Playing || State == GameState.LevelCompleted) && k.backspaceKey.wasPressedThisFrame) { Cycles.TimedPuzzle.EnterResearch(); return; }
                if (State == GameState.Timeline && k.deleteKey.wasPressedThisFrame) { Cycles.TimedPuzzle.DeleteLast(); return; }
                if (State == GameState.Research) Cycles.Player.Look();
            }
            if (k.escapeKey.wasPressedThisFrame)
            {
                if (EscapeRequested != null) EscapeRequested.Invoke();
                else TogglePause();
                return;
            }
            if (k.tabKey.wasPressedThisFrame && (State == GameState.Playing || State == GameState.Research || State == GameState.Timeline))
            { if (State == GameState.Timeline) Resume(); else SetState(GameState.Timeline); return; }
            if (State != GameState.Playing) return;
            if (k.rKey.wasPressedThisFrame && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed)) Cycles.Restart(true);
            else if (k.tKey.wasPressedThisFrame) Cycles.Restart(false);
            else if (k.rKey.wasPressedThisFrame || k.qKey.wasPressedThisFrame) Cycles.ToggleRecording();
            if (k.fKey.wasPressedThisFrame && Interaction) Interaction.TryInteract();
            if (State == GameState.Playing) Cycles.Player.Look();
        }
        static bool Frozen(GameState state) => state == GameState.Paused || state == GameState.Timeline || state == GameState.LevelCompleted;
        public void SetState(GameState state)
        {
            if (State == state) return;
            if (!Frozen(State) && Frozen(state)) resumeTimeScale = Time.timeScale > 0 ? Time.timeScale : 1;
            bool wasFrozen = Frozen(State);
            State = state;
            Time.timeScale = Frozen(state) ? 0 : wasFrozen ? resumeTimeScale : Time.timeScale > 0 ? Time.timeScale : 1;
            Cursor.lockState = state == GameState.Playing || state == GameState.Research ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = state != GameState.Playing && state != GameState.Research;
            StateChanged?.Invoke(state);
        }
        public void TogglePause()
        {
            if (State == GameState.Playing || State == GameState.Timeline || State == GameState.Research) { resumeState = Cycles.TimedPuzzle && Cycles.TimedPuzzle.IsResearch ? GameState.Research : GameState.Playing; SetState(GameState.Paused); }
            else if (State == GameState.Paused) Resume();
        }
        public void Resume() { if (State == GameState.Timeline) SetState(Cycles.TimedPuzzle && Cycles.TimedPuzzle.IsResearch ? GameState.Research : GameState.Playing); else if (State == GameState.Paused) SetState(resumeState); }
        public void Replay() => Cycles.Restart(true);
        public void EndSession() { Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void OnDestroy() => EndSession();
    }
}
