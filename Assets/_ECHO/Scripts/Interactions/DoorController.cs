using UnityEngine;
namespace EchoGame
{
    public enum DoorState { Closed, Opening, Open, Closing }
    public sealed class DoorController : MonoBehaviour
    {
        public Transform Panel;
        public Renderer Indicator;
        public Material ClosedMaterial, OpenMaterial;
        public bool SplitDiagonally;
        public Transform UpperLeaf, LowerLeaf;
        public Vector3 SplitDirection = new Vector3(-.493f,.870f,0);
        public float SplitTravel = 2.9f;
        public Renderer[] AdditionalIndicators = new Renderer[0];
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
            ApplyPose();
            State = amount == 0 ? DoorState.Closed : amount == 1 ? DoorState.Open : open ? DoorState.Opening : DoorState.Closing;
            ApplyIndicator();
        }
        public void ResetMechanism()
        {
            amount = 0; Command = false; State = DoorState.Closed;
            ApplyPose();
            ApplyIndicator();
        }
        void ApplyPose()
        {
            bool split = SplitDiagonally && UpperLeaf && LowerLeaf;
            Panel.localPosition = Vector3.up * (1.7f + (split ? 0 : amount * Travel));
            if (!split) return;
            Vector3 offset = SplitDirection.normalized * (amount * SplitTravel);
            UpperLeaf.localPosition = offset; LowerLeaf.localPosition = -offset;
        }
        void ApplyIndicator()
        {
            var material = Command ? OpenMaterial : ClosedMaterial;
            if (Indicator) Indicator.sharedMaterial = material;
            if (AdditionalIndicators == null) return;
            foreach (var indicator in AdditionalIndicators) if (indicator) indicator.sharedMaterial = material;
        }
    }
}
