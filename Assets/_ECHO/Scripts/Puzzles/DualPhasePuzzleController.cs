using TMPro;
using UnityEngine;
namespace EchoGame
{
    public sealed class DualPhasePuzzleController : MonoBehaviour
    {
        public PhaseNode HiddenNode, NodeA, NodeB;
        public DoorController FirstDoor, FinalDoor;
        [Min(.01f)] public float RequiredOverlap = 3;
        public bool RequirePlayerAndEcho = true;
        public TMP_Text ControlPanel;
        public float Overlap { get; private set; }
        public bool FirstDoorUnlocked { get; private set; }
        public bool FinalDoorUnlocked { get; private set; }
        string previousPanel;
        bool previousA, previousB;
        string previousOwnerA, previousOwnerB;
        public event System.Action StateChanged;
        public void Evaluate(float dt, Vector3 playerFeet)
        {
            bool first = FirstDoorUnlocked, final = FinalDoorUnlocked;
            FirstDoorUnlocked |= HiddenNode.ActivatedThisStep;
            bool independent = !RequirePlayerAndEcho || (NodeA.HasPlayerSignal && NodeB.HasEchoSignal) || (NodeA.HasEchoSignal && NodeB.HasPlayerSignal);
            if (!FinalDoorUnlocked)
            {
                Overlap = NodeA.IsSignaling && NodeB.IsSignaling && independent ? Overlap + dt : 0;
                if (Overlap + .00001f >= RequiredOverlap) { Overlap = RequiredOverlap; FinalDoorUnlocked = true; }
            }
            FirstDoor.SetCommand(FirstDoorUnlocked); FinalDoor.SetCommand(FinalDoorUnlocked);
            FirstDoor.Tick(dt, playerFeet); FinalDoor.Tick(dt, playerFeet); RefreshPanel();
            bool changed = first != FirstDoorUnlocked || final != FinalDoorUnlocked || previousA != NodeA.IsSignaling || previousB != NodeB.IsSignaling || previousOwnerA != NodeA.SignalOwner || previousOwnerB != NodeB.SignalOwner;
            previousA = NodeA.IsSignaling; previousB = NodeB.IsSignaling; previousOwnerA = NodeA.SignalOwner; previousOwnerB = NodeB.SignalOwner;
            if (changed) StateChanged?.Invoke();
        }
        public void ResetMechanisms()
        {
            FirstDoorUnlocked = FinalDoorUnlocked = false; Overlap = 0;
            FirstDoor.ResetMechanism(); FinalDoor.ResetMechanism(); previousA = previousB = false; previousOwnerA = previousOwnerB = null; RefreshPanel(); StateChanged?.Invoke();
        }
        void RefreshPanel()
        {
            if (!ControlPanel) return;
            string text = $"ФАЗОВЫЙ КОНТРОЛЛЕР\nA: {NodeA.SignalOwner}\nB: {NodeB.SignalOwner}\n" +
                (FinalDoorUnlocked ? "ДВЕРЬ РАЗБЛОКИРОВАНА" : $"СИНХРОНИЗАЦИЯ: {Overlap:0.0} / {RequiredOverlap:0.0}");
            if (text != previousPanel) { ControlPanel.text = text; previousPanel = text; }
        }
    }
}
