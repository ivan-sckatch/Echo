using UnityEngine;
using UnityEngine.InputSystem;
namespace EchoGame
{
    public sealed class TemporalScanner : MonoBehaviour
    {
        public Transform View;
        [Min(.1f)] public float Range = 12;
        public ScanFrame Current { get; private set; }
        public ScanFrame ReadInput()
        {
            Current = new ScanFrame(Mouse.current != null && Mouse.current.rightButton.isPressed, View.position, View.forward);
            return Current;
        }
        public void SetTarget(string id) { var frame = Current; frame.TargetId = id; Current = frame; }
        public void ResetScanner() => Current = new ScanFrame(false, View.position, View.forward);
    }
}
