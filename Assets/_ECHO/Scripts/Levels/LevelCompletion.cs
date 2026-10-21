using UnityEngine;
namespace EchoGame
{
    public sealed class LevelCompletion : MonoBehaviour
    {
        public Vector3 HalfExtents = new Vector3(2, 2, 1.5f);
        public bool Contains(PlayerController player)
        {
            var p = transform.InverseTransformPoint(player.Feet);
            return Mathf.Abs(p.x) <= HalfExtents.x && Mathf.Abs(p.z) <= HalfExtents.z && Mathf.Abs(p.y) <= HalfExtents.y;
        }
    }
}
