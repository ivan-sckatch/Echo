using UnityEngine;
namespace EchoGame
{
    public sealed class PuzzleController : MonoBehaviour
    {
        public PressurePlate Plate;
        public DoorController Door;
        public void Evaluate(PlayerController player, EchoManager echoes, float dt)
        { Plate.Evaluate(player, echoes); Door.SetCommand(Plate.IsActive); Door.Tick(dt, player.Feet); }
        public void ResetMechanisms() { Plate.SetActive(false); Door.ResetMechanism(); }
    }
}
