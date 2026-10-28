using System.Collections.Generic;
using EchoGame.UI;
using UnityEngine;
namespace EchoGame
{
    public sealed class Level03Controller : MonoBehaviour, IUIContextSource
    {
        public TimeCycleManager Cycles;
        public PulseButton[] Buttons;
        public TimedDoor[] Doors;
        public ChamberIsolation[] Chambers;
        public TMPro.TMP_Text Controls;
        public bool IsResearch => researchSuspended;
        bool researchSuspended, taught;
        public Material ScannerMaterial;
        ScanVisualController scanVisual;
        PulseButton lastAnalysis;
        void OnEnable() { if (Cycles && Cycles.Game) Cycles.Game.StateChanged += OnState; }
        void OnDisable() { if (Cycles && Cycles.Game) Cycles.Game.StateChanged -= OnState; if (scanVisual) scanVisual.Hide(); }
        void OnState(GameState state) { if (state != GameState.Playing && state != GameState.Research && scanVisual) scanVisual.Hide(); }
        public EchoLimitModel LimitModel => EchoLimitModel.ConcurrentEchoLimit;
        public int ConcurrentEchoCapacity => 1;
        public int ActiveEchoCount { get { int n = 0; foreach (var e in Cycles.Echoes.Echoes) if (e.IsActive) n++; return n; } }
        public RecordingRejection CurrentRejection { get; private set; }
        readonly Dictionary<string, PulseButton> registry = new Dictionary<string, PulseButton>();
        public void Initialize()
        {
            registry.Clear(); foreach (var b in Buttons)
            {
                if (string.IsNullOrEmpty(b.StableId) || registry.ContainsKey(b.StableId)) throw new System.InvalidOperationException("Duplicate/empty button ID");
                registry.Add(b.StableId, b);
            }
        }
        public void EnterResearch()
        {
            Cycles.Restart(false, false); researchSuspended = true; Cycles.Game.SetState(GameState.Research);
            Cycles.Notify("ИССЛЕДОВАНИЕ · Enter — начать цикл. ПКМ — анализ объектов. Механизмы отключены.");
        }
        public void StartCycle() { researchSuspended = false; Cycles.Restart(false); }
        public void ResetMechanisms()
        {
            researchSuspended = false; if (scanVisual) scanVisual.Hide();
            CurrentRejection = null; foreach (var d in Doors) d.ResetDoor(); foreach (var b in Buttons) b.ResetButton();
            if (Chambers != null) foreach (var chamber in Chambers) chamber.ResetChamber();
            if (!taught) { taught = true; Cycles.Notify("F — кнопка. R — начать запись, повторное R — сохранить фрагмент. Tab — временная шкала."); }
        }
        public void Reject(string reason, float start, float end, int id)
        { CurrentRejection = new RecordingRejection { Reason = reason, Start = start, End = end, RecordingId = id }; Cycles.Notify(reason + $" · Эхо {id:00}: {start:0.0}–{end:0.0} с"); }
        public void Replay(EchoPlayback echo, RecordedInteraction action)
        {
            if (action.Kind != InteractionKind.Press || !registry.TryGetValue(action.TargetId, out var button)) return;
            var f = echo.Recording.Sample(action.Time);
            var direction = Quaternion.Euler(f.Pitch, f.Yaw, 0) * Vector3.forward;
            var access = Cycles.Game.Interaction;
            var origin = f.Position + Quaternion.Euler(0, f.Yaw, 0) * Cycles.Player.View.localPosition;
            if (!ReferenceEquals(InteractionAccess.Trace(origin, direction, access.Range, access.Mask), button)) return;
            int before = button.PressCount; button.Press(action.Time); if (button.PressCount > before) Cycles.MarkEchoUsed(echo.Id);
        }
        public void Tick(float dt) { Analyze(); foreach (var d in Doors) d.Tick(Cycles.Elapsed, dt, Cycles.Player.Feet); if (Chambers != null) foreach (var chamber in Chambers) chamber.Tick(dt); foreach (var b in Buttons) b.Refresh(Cycles.Elapsed); }
        public void ResearchTick(float dt)
        {
            RefreshControls();
            Cycles.Player.Simulate(dt);
            Analyze();
        }
        void Analyze()
        {
            RefreshControls();
            if (!scanVisual) scanVisual = ScanVisualController.Create(transform, "Analysis scanner", ScannerMaterial, .02f);
            var scanner = Cycles.Player.Scanner;
            var scan = scanner.ReadInput();
            Vector3 end = scan.Origin + scan.Direction * scanner.Range;
            PulseButton target = null;
            if (scan.Enabled && Physics.Raycast(scan.Origin, scan.Direction, out var hit, scanner.Range, ~0, QueryTriggerInteraction.Ignore))
            {
                end = hit.point; target = hit.collider.GetComponentInParent<PulseButton>();
                if (target && target != lastAnalysis) Cycles.Notify($"АНАЛИЗ · {target.Door.DoorLabel}: " + (target.Door.LatchUntilReset ? "открытие до сброса" : $"окно {target.Door.Window:0} с") + ". Камера изолируется.");
            }
            lastAnalysis = target; scanVisual.Render(scan, end, target);
        }
        void RefreshControls()
        {
            if (!Controls) return;
            Controls.text = IsResearch ? "ИССЛЕДОВАНИЕ · Enter — начать цикл · ПКМ — анализ · Tab — фрагменты" :
                (Cycles.IsRecording ? "R — завершить и сохранить" : "R — начать запись") + " · F — кнопка · T — повтор\nTab — анализ · Del в анализе — удалить последний фрагмент · Backspace — исследование";
        }
        public void DeleteLast()
        { if (Cycles.Echoes.Echoes.Count > 0) { Cycles.Echoes.RemoveLastFragment(); Cycles.Notify("Последний фрагмент удалён."); } }
    }
}
