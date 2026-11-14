using System;
using UnityEngine;
using UnityEngine.Events;
namespace EchoGame
{
    [Serializable] public struct GeometrySnapshot
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Progress;
        public bool Command, Completed, Locked;
        public int Section;
    }
    // Driven only by the cycle's fixed-step clock. The pivot may carry a bridge, ramp or wall.
    public sealed class GeometryMechanism : MonoBehaviour
    {
        public int Section;
        public Vector3 RestPosition, ActivePosition;
        public Vector3 RestEuler, ActiveEuler;
        [Min(.02f)] public float TravelSeconds = 1;
        public Renderer Indicator;
        public Material IdleMaterial, MovingMaterial, ReadyMaterial;
        public UnityEvent MovementStarted = new UnityEvent(), MovementCompleted = new UnityEvent();
        public float Progress { get; private set; }
        public bool Command { get; private set; }
        public bool Locked { get; private set; }
        public bool AtTarget => Progress >= 1;
        public bool IsMoving => !Locked && !Mathf.Approximately(Progress, Command ? 1 : 0);
        public void SetCommand(bool value) { if (!Locked) Command = value; }
        bool moving;
        public void Tick(float dt)
        {
            if (Locked) return;
            bool next = IsMoving;
            if (next && !moving) MovementStarted.Invoke();
            Progress = Mathf.MoveTowards(Progress, Command ? 1 : 0, dt / Mathf.Max(.02f, TravelSeconds));
            Apply();
            if (next && !IsMoving) MovementCompleted.Invoke();
            moving = IsMoving;
        }
        void Apply()
        {
            transform.localPosition = Vector3.Lerp(RestPosition, ActivePosition, Progress);
            transform.localRotation = Quaternion.Slerp(Quaternion.Euler(RestEuler), Quaternion.Euler(ActiveEuler), Progress);
            Refresh();
        }
        void Refresh() { if (Indicator) Indicator.sharedMaterial = Locked || AtTarget ? ReadyMaterial : IsMoving ? MovingMaterial : IdleMaterial; }
        public GeometrySnapshot Capture() => new GeometrySnapshot { Position = transform.localPosition, Rotation = transform.localRotation,
            Progress = Progress, Command = Command, Completed = !IsMoving, Locked = Locked, Section = Section };
        public void Restore(GeometrySnapshot state)
        {
            Section = state.Section; Progress = state.Progress; Command = state.Command; Locked = state.Locked; moving = !state.Completed;
            transform.localPosition = state.Position; transform.localRotation = state.Rotation; Refresh();
        }
        public void ResetMechanism() { Locked = Command = moving = false; Progress = 0; Apply(); }
        public void Freeze() { Locked = true; Refresh(); }
    }
}
