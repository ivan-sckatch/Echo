using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace EchoGame
{
    public enum GameState { Initializing, Playing, Paused, ResettingCycle, LevelCompleted, Timeline }
    public sealed class GameManager : MonoBehaviour
    {
        public TimeCycleManager Cycles;
        public PlayerInteraction Interaction;
        public GameState State { get; private set; } = GameState.Initializing;
        public event Action<GameState> StateChanged;
        public event Action EscapeRequested;
        float resumeTimeScale = 1;
        void Start() { Cycles.Restart(true); }
        void Update()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.escapeKey.wasPressedThisFrame)
            {
                if (EscapeRequested != null) EscapeRequested.Invoke();
                else TogglePause();
                return;
            }
            if (k.tabKey.wasPressedThisFrame && (State == GameState.Playing || State == GameState.Timeline))
            { SetState(State == GameState.Playing ? GameState.Timeline : GameState.Playing); return; }
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
            Cursor.lockState = state == GameState.Playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = state != GameState.Playing;
            StateChanged?.Invoke(state);
        }
        public void TogglePause()
        {
            if (State == GameState.Playing || State == GameState.Timeline) SetState(GameState.Paused);
            else if (State == GameState.Paused) Resume();
        }
        public void Resume() { if (State == GameState.Paused || State == GameState.Timeline) SetState(GameState.Playing); }
        public void Replay() => Cycles.Restart(true);
        public void EndSession() { Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void OnDestroy() => EndSession();
    }
}
