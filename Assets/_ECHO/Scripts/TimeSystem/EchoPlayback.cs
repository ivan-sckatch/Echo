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
        public EchoFrame CurrentFrame { get; private set; }
        int actionIndex;
        public void ResetEvents() => actionIndex = 0;
        public void ReplayEvents(float time, Level03Controller level)
        {
            while (actionIndex < Recording.Actions.Count && Recording.Actions[actionIndex].Time <= time)
            {
                var action = Recording.Actions[actionIndex++];
                if (action.Time >= Recording.StartTime && action.Time < Recording.EndTime) level.Replay(this, action);
            }
        }
        public void Initialize(EchoRecording recording) { Recording = recording; Evaluate(0); }
        public void Evaluate(float time)
        {
            State = float.IsNaN(time) || float.IsInfinity(time) ? EchoPlaybackState.SyncError :
                Recording == null || time < Recording.Frames[0].Time ? EchoPlaybackState.Waiting :
                time >= Recording.Duration ? EchoPlaybackState.Completed : Recording.IsActiveAt(time) ? EchoPlaybackState.Playback : EchoPlaybackState.Waiting;
            IsActive = State == EchoPlaybackState.Playback;
            Body.gameObject.SetActive(IsActive);
            if (!IsActive) { CurrentFrame = default; return; }
            var frame = Recording.Sample(time);
            CurrentFrame = frame;
            transform.SetPositionAndRotation(frame.Position, Quaternion.Euler(0, frame.Yaw, 0));
            CameraPitch = frame.Pitch;
        }
    }
}
