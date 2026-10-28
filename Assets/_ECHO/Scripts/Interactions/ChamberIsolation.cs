using UnityEngine;
namespace EchoGame
{
    public sealed class ChamberIsolation : MonoBehaviour
    {
        public string ChamberName;
        public DoorController Entrance;
        public Collider Window;
        public Transform ObservationPoint;
        public bool Isolated { get; private set; }
        public void Lock() { Isolated = true; Entrance.SetCommand(false); }
        public void ResetChamber() { Isolated = false; Entrance.ResetMechanism(); Entrance.SetCommand(true); Entrance.Tick(100, Vector3.one * 10000); }
        public void Tick(float dt) { Entrance.SetCommand(!Isolated); Entrance.Tick(dt, Vector3.one * 10000); }
    }
}
