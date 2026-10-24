using System;
using System.Collections.Generic;
using UnityEngine;
namespace EchoGame.UI
{
    public enum UIState { Gameplay, Timeline, Paused, Settings, Completed, MainMenu, LevelSelect }
    public enum SessionPhase { Playing, Paused, Timeline, Completed, Resetting }
    public enum EchoLimitModel { SavedRecordingsLimit, ConcurrentEchoLimit }
    public sealed class RecordingRejection
    {
        public string Reason;
        public float? Start, End;
        public int? RecordingId;
    }
    // Optional future gameplay/configuration adapter. The current bridge defaults to saved recordings.
    public interface IUIContextSource
    {
        bool IsResearch { get; }
        EchoLimitModel LimitModel { get; }
        int ConcurrentEchoCapacity { get; }
        int ActiveEchoCount { get; }
        RecordingRejection CurrentRejection { get; }
    }
    public enum RecordingState { Waiting, Recording, Completed }
    public enum EchoState { Waiting, Playback, Completed, SyncError }
    [Serializable] public struct ActionInterval
    {
        public float Start, End;
        public string Label;
        public bool Moving;
        public bool Scanning;
        public string TargetId;
        public ActionInterval(float start, float end, string label, bool moving, bool scanning = false, string targetId = null)
        { Start = start; End = end; Label = label; Moving = moving; Scanning = scanning; TargetId = targetId; }
    }
    public sealed class EchoUIData
    {
        public int Id;
        public float Start, End;
        public EchoState State;
        public IReadOnlyList<ActionInterval> Intervals;
    }
    public sealed class UISnapshot
    {
        public bool IsResearch;
        public EchoLimitModel LimitModel;
        public int ConcurrentEchoCapacity, ActiveEchoCount;
        public RecordingRejection Rejection;
        public float Remaining => Mathf.Max(0, Duration - Time);
        public string LimitLabel => LimitModel == EchoLimitModel.ConcurrentEchoLimit
            ? $"АКТИВНО: {ActiveEchoCount}/{ConcurrentEchoCapacity}" : $"ЗАПИСИ: {Echoes.Count}/{Capacity}";
        public int Cycle, Capacity, Attempts, EchoesUsed;
        public float Time, Duration, RecordStart, RecordEnd;
        public RecordingState Recording;
        public SessionPhase Phase;
        public IReadOnlyList<EchoUIData> Echoes = Array.Empty<EchoUIData>();
        public string Level = "01 · Первый отголосок";
        public string NextScene;
        public ScannerUIData Scanner;
    }
    public sealed class ScannerUIData
    {
        public bool Enabled, HasTarget, Discovered, Activated, NodeAActive, NodeBActive, DoorUnlocked;
        public string Target, NodeAOwner, NodeBOwner;
        public float Discovery, Activation, Overlap, RequiredOverlap;
    }
    // Views consume this contract and do not reference recorder/playback implementations.
    public interface IUIDataProvider
    {
        UISnapshot Current { get; }
        string CurrentNotice { get; }
        event Action<UISnapshot> Changed;
        event Action<string, bool> Notice;
        event Action<bool, string> Interaction;
        event Action<string, float> InteractionOccurred;
        event Action CycleRestarted;
        event Action<UISnapshot> LevelCompleted;
        event Action<int> SynchronizationError;
    }
    public interface IUICommands
    {
        event Action EscapePressed;
        void Pause();
        void SetSensitivity(float sensitivity);
        void Resume();
        void RestartCycle();
        void ReplayLevel();
        void MainMenu();
        void NextLevel();
    }
    public static class UITheme
    {
        public static readonly Color Background = Hex("101820");
        public static readonly Color Cyan = Hex("57DBE6");
        public static readonly Color Amber = Hex("FFB65C");
        public static readonly Color Error = Hex("F36B75");
        public static readonly Color Text = Hex("EAF5F5");
        public static readonly Color Muted = Hex("96B2BC");
        public static readonly Color Secondary = Hex("8C9EFF");
        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        public static string Clock(float time) { int t = Mathf.Max(0, Mathf.FloorToInt(time)); return $"{t / 60:00}:{t % 60:00}"; }
        public static string StateName(EchoState state) => state == EchoState.Playback ? "ВОСПРОИЗВЕДЕНИЕ" : state == EchoState.Completed ? "ЗАВЕРШЕНО" : state == EchoState.SyncError ? "ОШИБКА СИНХРОНИЗАЦИИ" : "ОЖИДАНИЕ";
    }
}
