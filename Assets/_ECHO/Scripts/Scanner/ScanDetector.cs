using UnityEngine;
namespace EchoGame
{
    public static class ScanDetector
    {
        public static PhaseNode Trace(ScanFrame frame, float range, LayerMask mask, ScanRegistry registry, bool replay, out Vector3 end)
        {
            end = frame.Origin + frame.Direction * range;
            if (!frame.Enabled || frame.Direction.sqrMagnitude < .5f) return null;
            if (!Physics.Raycast(frame.Origin, frame.Direction, out var hit, range, mask, QueryTriggerInteraction.Collide)) return null;
            end = hit.point;
            var node = hit.collider.GetComponentInParent<PhaseNode>();
            if (!node || !registry.TryGet(node.StableId, out var registered) || registered != node) return null;
            // The recorded identifier is a constraint, never permission to activate through geometry.
            if (replay && (!registry.TryGet(frame.TargetId, out var expected) || expected != node)) return null;
            return node;
        }
    }
}
