using EchoGame.UI;
using TMPro;
using UnityEngine;
namespace EchoGame
{
    public sealed class Level05Controller : MonoBehaviour, IUIContextSource, IResearchLevel
    {
        public TimeCycleManager Cycles;
        public GeometryCheckpointManager Checkpoints;
        public PressurePlate PlateA, PlateB;
        // Per tier: bridge A, bridge B/wall, rising section. All must settle before a checkpoint.
        public GeometryMechanism[] Lower, Middle, Upper;
        public GeometryTerminal MiddleWall, MiddleBridge, Lift, UpperA, UpperB;
        public GeometryTerminal LiftSecond, LiftFinal;
        public GeometryTerminal[] Terminals;
        public PhaseNode FinalNode;
        public DoorController ExitDoor;
        public TMP_Text Status;
        public int ActiveSection => Checkpoints.ActiveCheckpoint;
        public bool CanExit { get; private set; }
        public bool IsResearch { get; private set; }
        public void EnterResearch()
        {
            Cycles.Restart(false, false); IsResearch = true; Cycles.Echoes.Evaluate(-1);
            foreach (var m in Checkpoints.Mechanisms) { m.SetCommand(true); m.Tick(10); }
            ExitDoor.SetCommand(true); ExitDoor.Tick(5, Cycles.Player.Feet);
            Cycles.Game.SetState(GameState.Research); Refresh();
        }
        public void StartCycle() { IsResearch = false; Cycles.Restart(false); }
        public void ResearchTick(float dt) { Cycles.Player.Simulate(dt); Cycles.Scanning.Analyze(); Refresh(); }
        public EchoLimitModel LimitModel => EchoLimitModel.ConcurrentEchoLimit;
        public int ConcurrentEchoCapacity => 2;
        public RecordingRejection CurrentRejection => null;
        public int ActiveEchoCount { get { int n = 0; foreach (var e in Cycles.Echoes.Echoes) if (e.IsActive) n++; return n; } }
        public int LiftSequenceStep { get; private set; }
        public bool AdvanceLiftSequence(GeometryTerminal terminal)
        {
            if (terminal.SequenceOrder!=LiftSequenceStep+1) return false;
            LiftSequenceStep++;return true;
        }
        public void ResetLiftSequence()
        {
            LiftSequenceStep=0;
            if (Lift) Lift.ResetTerminal();
            if (LiftSecond) LiftSecond.ResetTerminal();
            if (LiftFinal) LiftFinal.ResetTerminal();
        }
        void OnEnable() { if (FinalNode) FinalNode.Activated += OnNode; }
        void OnDisable() { if (FinalNode) FinalNode.Activated -= OnNode; }
        void OnNode(PhaseNode node, int source) { if (ActiveSection == 2) CanExit = true; }
        public bool LowerReady => TwoPlateEchoes() && Settled(Lower);
        public bool MiddleReady => LiftSequenceStep==3 && LiftFinal.IsActive && Middle[2].AtTarget && !Middle[2].IsMoving;
        public bool UpperPowered => UpperA.IsActive && UpperB.IsActive;
        public bool UpperReady => UpperPowered && Settled(Upper);
        static bool Settled(GeometryMechanism[] set) { foreach (var m in set) if (!m.AtTarget || m.IsMoving) return false; return true; }
        bool TwoPlateEchoes()
        {
            foreach (var a in Cycles.Echoes.Echoes) if (a.IsActive && PlateA.Contains(a.Feet))
                foreach (var b in Cycles.Echoes.Echoes) if (b != a && b.IsActive && PlateB.Contains(b.Feet)) return true;
            return false;
        }
        public void Replay(EchoPlayback echo, RecordedInteraction action)
        {
            if (!echo.IsActive) return;
            var frame = echo.Recording.Sample(action.Time);
            var direction = Quaternion.Euler(0, frame.Yaw, 0) * Quaternion.Euler(frame.Pitch, 0, 0) * Vector3.forward;
            var target = InteractionAccess.Trace(frame.Position + Vector3.up * 1.65f, direction, Cycles.Game.Interaction.Range, Cycles.Game.Interaction.Mask);
            if (target is GeometryTerminal terminal && terminal.StableId == action.TargetId && !terminal.PlayerOnly) terminal.Pulse(echo.Id);
        }
        public void ResetMechanisms(bool full)
        {
            IsResearch = false;
            Checkpoints.ResetState(full); LiftSequenceStep=0;CanExit = false;
            foreach (var terminal in Terminals) terminal.ResetTerminal();
            PlateA.SetActive(false); PlateB.SetActive(false); ExitDoor.ResetMechanism(); Refresh();
        }
        public void Tick(float dt)
        {
            foreach (var echo in Cycles.Echoes.Echoes) echo.ReplayEvents(Cycles.Elapsed, Replay);
            if (ActiveSection == 0)
            {
                PlateA.Evaluate(Cycles.Player, Cycles.Echoes); PlateB.Evaluate(Cycles.Player, Cycles.Echoes);
                Lower[0].SetCommand(PlateA.IsActive); Lower[1].SetCommand(PlateB.IsActive);
                Lower[2].SetCommand(TwoPlateEchoes() && Lower[0].AtTarget && Lower[1].AtTarget);
                foreach (var e in Cycles.Echoes.Echoes) if (e.IsActive && (PlateA.Contains(e.Feet) || PlateB.Contains(e.Feet))) Cycles.MarkEchoUsed(e.Id);
            }
            else if (ActiveSection == 1)
            {
                Middle[0].SetCommand(MiddleBridge.IsActive); Middle[1].SetCommand(MiddleWall.IsActive);
                if (LiftSequenceStep>0 && (LiftSequenceStep<3 && (!Lift.IsActive || LiftSequenceStep==2 && !LiftSecond.IsActive) || LiftSequenceStep==3 && !LiftFinal.IsActive)) ResetLiftSequence();
                Middle[2].SetCommand(LiftSequenceStep==3 && LiftFinal.IsActive);
            }
            else
            {
                Upper[0].SetCommand(UpperA.IsActive); Upper[1].SetCommand(UpperA.IsActive);
                Upper[2].SetCommand(UpperPowered);
            }
            foreach (var m in Checkpoints.Mechanisms) m.Tick(dt);
            foreach (var t in Terminals) t.Refresh();
            ExitDoor.SetCommand(CanExit); ExitDoor.Tick(dt, Cycles.Player.Feet);
            Checkpoints.Tick(dt); Refresh();
            if (Checkpoints.ContainsNext(Cycles.Player.Feet)) Checkpoints.TryActivate(ActiveSection + 1, ActiveSection == 0 ? LowerReady : MiddleReady);
        }
        void Refresh()
        {
            if (Status) Status.text = $"КОНТРОЛЬНАЯ ТОЧКА {ActiveSection}/2 · " + new[] { "ОСНОВАНИЕ", "ПЕРЕСТРОЙКА", "СОГЛАСОВАНИЕ" }[ActiveSection] +
                (IsResearch ? "\nИССЛЕДОВАНИЕ · R — начать запись · ПКМ — анализ" : "\nR — запись / сохранить · T — повтор · F — рычаг · ПКМ — сканер · Tab — следы");
        }
    }
}
