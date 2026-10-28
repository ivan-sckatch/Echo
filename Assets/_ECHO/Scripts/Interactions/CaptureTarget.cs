using System.Collections.Generic;
using UnityEngine;
namespace EchoGame
{
    // Cameras own the lock. Echo playback remains the sole owner of recorded transforms.
    public sealed class CaptureTarget : MonoBehaviour
    {
        public PlayerController Player;
        public EchoPlayback Echo;
        readonly HashSet<SecurityCameraController> holders = new HashSet<SecurityCameraController>();
        public bool Immobilized => holders.Count > 0;
        public bool Exists => isActiveAndEnabled && (Player || Echo && Echo.IsActive);
        public Vector3 AimPoint => transform.position + Vector3.up * .95f;
        public EchoRecording FragmentAt(float time)
        {
            if (Echo && Echo.Recording != null) foreach (var f in Echo.Recording.Fragments)
                if (time >= f.StartTime && time < f.EndTime) return f;
            return null;
        }
        public void Capture(SecurityCameraController camera) => holders.Add(camera);
        public void Release(SecurityCameraController camera) => holders.Remove(camera);
        public void ResetCapture() => holders.Clear();
    }
}
