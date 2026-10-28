using UnityEngine;
using UnityEngine.InputSystem;
namespace EchoGame
{
    public sealed class TemporalScanner : MonoBehaviour
    {
        public Transform View;
        [Min(.1f)] public float Range = 12;
        public ScanFrame Current { get; private set; }
        public string InspectionTitle { get; private set; } = "";
        public string InspectionAction { get; private set; } = "";
        public void Inspect(TimeCycleManager cycles)
        {
            string title = "", action = "";
            if (cycles.IsResearch && Current.Enabled && Physics.Raycast(Current.Origin, Current.Direction, out var hit, Range, ~0, QueryTriggerInteraction.Collide))
                ResearchInspection.Describe(hit.collider, cycles, out title, out action);
            InspectionTitle = title; InspectionAction = action;
        }
        public ScanFrame ReadInput()
        {
            var target = GetComponent<CaptureTarget>();
            Current = new ScanFrame((!target || !target.Immobilized) && Mouse.current != null && Mouse.current.rightButton.isPressed, View.position, View.forward);
            return Current;
        }
        public void SetTarget(string id) { var frame = Current; frame.TargetId = id; Current = frame; }
        public void ResetScanner()
        { Current = new ScanFrame(false, View.position, View.forward); InspectionTitle = InspectionAction = ""; }
    }
}
