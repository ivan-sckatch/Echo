using UnityEngine;
namespace EchoGame
{
    public enum InteractionKind { Press }
    [System.Serializable] public struct RecordedInteraction
    {
        public float Time;
        public string TargetId;
        public InteractionKind Kind;
        public RecordedInteraction(float time, string id) { Time = time; TargetId = id; Kind = InteractionKind.Press; }
    }
    public static class InteractionAccess
    {
        public static IInteractable Trace(Vector3 origin, Vector3 direction, float range, int mask)
        {
            Physics.SyncTransforms();
            return Physics.Raycast(origin, direction, out var hit, range, mask, QueryTriggerInteraction.Ignore)
                ? hit.collider.GetComponentInParent<IInteractable>() : null;
        }
    }
}
