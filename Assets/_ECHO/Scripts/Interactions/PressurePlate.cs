using UnityEngine;
using UnityEngine.Events;
namespace EchoGame
{
    public sealed class PressurePlate : MonoBehaviour
    {
        public Vector3 HalfExtents = new Vector3(1.25f, .35f, 1.25f);
        public Renderer Surface;
        public Material InactiveMaterial, ActiveMaterial;
        public UnityEvent<bool> OnActiveChanged = new UnityEvent<bool>();
        public bool IsActive { get; private set; }
        public bool Contains(Vector3 feet)
        {
            Vector3 p = transform.InverseTransformPoint(feet);
            return Mathf.Abs(p.x) <= HalfExtents.x && Mathf.Abs(p.z) <= HalfExtents.z &&
                p.y >= -.15f && p.y <= HalfExtents.y;
        }
        public void Evaluate(PlayerController player, EchoManager echoes)
        {
            bool active = Contains(player.Feet);
            foreach (var echo in echoes.Echoes) active |= echo.IsActive && Contains(echo.Feet);
            SetActive(active);
        }
        public void SetActive(bool active)
        {
            if (IsActive == active) return;
            IsActive = active;
            Surface.sharedMaterial = active ? ActiveMaterial : InactiveMaterial;
            OnActiveChanged.Invoke(active);
        }
    }
}
