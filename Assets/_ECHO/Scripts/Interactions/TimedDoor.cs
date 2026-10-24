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
        public float OpenUntil { get; private set; }
        public float Remaining { get; private set; }
        public void Pulse(float time) { if (Retrigger || time >= OpenUntil) OpenUntil = time + Window; }
        public void Tick(float time, float dt, Vector3 feet)
        {
            Remaining = Mathf.Max(0, OpenUntil - time); Door.SetCommand(Remaining > 0); Door.Tick(dt, feet);
            if (Indicator) Indicator.text = Remaining > 0 ? $"ОТКРЫТО · {Remaining:0.0} с" : "ЗАКРЫТО · ИМПУЛЬС 6 с";
        }
        public void ResetDoor() { OpenUntil = Remaining = 0; Door.ResetMechanism(); Tick(0, 0, Vector3.one * -1000); }
    }
}
