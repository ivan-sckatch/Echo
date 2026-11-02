using UnityEngine;

namespace EchoGame.Visuals
{
    // Presentation only: never writes gameplay state, transforms or colliders.
    public sealed class MechanismArt : MonoBehaviour
    {
        public PressurePlate Plate;
        public DoorController Door;
        public Transform Deck;
        public Renderer[] Signals;
        public Material Idle, Active;
        public TextMesh Status;
        bool? previous;
        void LateUpdate()
        {
            if (!Plate || !Door) return;
            bool active = Plate.IsActive;
            if (Deck) Deck.localPosition = Vector3.Lerp(Deck.localPosition,
                new Vector3(0, active ? .104f : .145f, 0), 1 - Mathf.Exp(-12 * Time.deltaTime));
            if (previous != active)
            {
                foreach (var signal in Signals) if (signal) signal.sharedMaterial = active ? Active : Idle;
                previous = active;
            }
            if (Status) Status.text = Door.State == DoorState.Open ? "01 / OPEN" :
                Door.State == DoorState.Closed ? "01 / LOCKED" : "01 / MOVING";
        }
    }
}
