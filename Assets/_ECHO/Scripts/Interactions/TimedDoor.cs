using TMPro;
using UnityEngine;
namespace EchoGame
{
    public sealed class TimedDoor : MonoBehaviour
    {
        public DoorController Door;
        public TMP_Text Indicator;
        [Min(.1f)] public float Window = 6;
        public bool Retrigger = true;
        public bool LatchUntilReset, OncePerCycle;
        public string DoorLabel = "";
        public bool Activated { get; private set; }
        public float OpenUntil { get; private set; }
        public float Remaining { get; private set; }
        public bool IsOpenAt(float time) => Activated && (LatchUntilReset || time < OpenUntil);
        public void Pulse(float time) { if (OncePerCycle && Activated) return; if (Retrigger || time >= OpenUntil) { Activated = true; OpenUntil = time + Window; } }
        public void Tick(float time, float dt, Vector3 feet)
        {
            Remaining = Mathf.Max(0, OpenUntil - time); Door.SetCommand(IsOpenAt(time)); Door.Tick(dt, feet);
            if (Indicator) Indicator.text = DoorLabel + "\n" + (IsOpenAt(time) ? LatchUntilReset ? "ОТКРЫТО ДО СБРОСА" : $"ОТКРЫТО · {Remaining:0.0} с" : LatchUntilReset ? "ЗАКРЫТО" : $"ОКНО {Window:0} С · ЗАКРЫТО");
        }
        public void ResetDoor() { Activated = false; OpenUntil = Remaining = 0; Door.ResetMechanism(); Tick(0, 0, Vector3.one * -1000); }
    }
}
