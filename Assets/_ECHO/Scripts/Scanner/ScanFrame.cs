using UnityEngine;
namespace EchoGame
{
    [System.Serializable]
    public struct ScanFrame
    {
        public bool Enabled;
        public Vector3 Origin, Direction;
        public string TargetId;
        public ScanFrame(bool enabled, Vector3 origin, Vector3 direction, string targetId = null)
        { Enabled = enabled; Origin = origin; Direction = direction.normalized; TargetId = targetId; }
        public static ScanFrame Interpolate(ScanFrame a, ScanFrame b, float t)
        {
            var discrete = t >= 1 ? b : a;
            Vector3 direction = a.Direction.sqrMagnitude > .01f && b.Direction.sqrMagnitude > .01f ? Vector3.Slerp(a.Direction, b.Direction, t) : discrete.Direction;
            return new ScanFrame(discrete.Enabled, Vector3.Lerp(a.Origin, b.Origin, t), direction, discrete.TargetId);
        }
    }
}
