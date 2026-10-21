using UnityEngine;
namespace EchoGame
{
    [System.Serializable]
    public struct EchoFrame
    {
        public float Time;
        public Vector3 Position;
        public float Yaw, Pitch;
        public ScanFrame Scan;
        public EchoFrame(float time, Vector3 position, float yaw, float pitch)
        { Time = time; Position = position; Yaw = yaw; Pitch = pitch; Scan = default; }
        public static EchoFrame Lerp(EchoFrame a, EchoFrame b, float time)
        {
            float t = b.Time > a.Time ? Mathf.InverseLerp(a.Time, b.Time, time) : 0;
            var result = new EchoFrame(time, Vector3.Lerp(a.Position, b.Position, t),
                Mathf.LerpAngle(a.Yaw, b.Yaw, t), Mathf.LerpAngle(a.Pitch, b.Pitch, t));
            result.Scan = ScanFrame.Interpolate(a.Scan, b.Scan, t); return result;
        }
    }
}
