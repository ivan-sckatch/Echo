using UnityEngine;
namespace EchoGame
{
    public enum EchoPlaybackState { Waiting, Playback, Completed, SyncError }
    public sealed class EchoPlayback : MonoBehaviour
    {
        public EchoRecording Recording { get; private set; }
        public bool IsActive { get; private set; }
        public int Id { get; internal set; }
        public EchoPlaybackState State { get; private set; }
        public Vector3 Feet => transform.position;
        public Transform Body;
        public float CameraPitch { get; private set; }
        public void Initialize(EchoRecording recording) { Recording = recording; Evaluate(0); }
        public void Evaluate(float time)
        {
            State = float.IsNaN(time) || float.IsInfinity(time) ? EchoPlaybackState.SyncError :
                Recording == null || time < Recording.Frames[0].Time ? EchoPlaybackState.Waiting :
                time >= Recording.Duration ? EchoPlaybackState.Completed : EchoPlaybackState.Playback;
            IsActive = State == EchoPlaybackState.Playback;
            Body.gameObject.SetActive(IsActive);
            if (!IsActive) return;
            var frame = Recording.Sample(time);
            transform.SetPositionAndRotation(frame.Position, Quaternion.Euler(0, frame.Yaw, 0));
            CameraPitch = frame.Pitch;
        }
    }
}
