using UnityEngine;
namespace EchoGame
{
    public enum DoorState { Closed, Opening, Open, Closing }
    public sealed class DoorController : MonoBehaviour
    {
        public Transform Panel;
        public Renderer Indicator;
        public Material ClosedMaterial, OpenMaterial;
        public float Travel = 3.8f;
        [Min(.01f)] public float OpenSeconds = .35f, CloseSeconds = .18f;
        public DoorState State { get; private set; }
        public bool Command { get; private set; }
        float amount;
        public void SetCommand(bool open) => Command = open;
        public void Tick(float dt, Vector3 feet)
        {
            Vector3 local = transform.InverseTransformPoint(feet);
            bool occupied = Mathf.Abs(local.x) < 1.85f && Mathf.Abs(local.z) < .85f;
            bool open = Command || (occupied && amount > 0);
            amount = Mathf.MoveTowards(amount, open ? 1 : 0, dt / (open ? OpenSeconds : CloseSeconds));
            Panel.localPosition = Vector3.up * (1.7f + amount * Travel);
            State = amount == 0 ? DoorState.Closed : amount == 1 ? DoorState.Open : open ? DoorState.Opening : DoorState.Closing;
            Indicator.sharedMaterial = Command ? OpenMaterial : ClosedMaterial;
        }
        public void ResetMechanism()
        {
            amount = 0; Command = false; State = DoorState.Closed;
            Panel.localPosition = Vector3.up * 1.7f;
            Indicator.sharedMaterial = ClosedMaterial;
        }
    }
}
