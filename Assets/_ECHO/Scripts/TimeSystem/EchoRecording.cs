using System.Collections.Generic;
using UnityEngine;
namespace EchoGame
{
    public sealed class EchoRecording
    {
        readonly List<EchoFrame> frames;
        public IReadOnlyList<EchoFrame> Frames => frames;
        readonly List<RecordedInteraction> actions;
        public IReadOnlyList<RecordedInteraction> Actions => actions;
        public float StartTime => frames[0].Time;
        public float EndTime => Duration;
        public float Length => EndTime - StartTime;
        public float Duration => frames[frames.Count - 1].Time;
        readonly List<EchoRecording> fragments;
        public IReadOnlyList<EchoRecording> Fragments => fragments;
        public EchoRecording(List<EchoFrame> source, List<RecordedInteraction> interactions = null)
        {
            if (source == null || source.Count == 0) throw new System.ArgumentException("Recording requires frames.");
            frames = new List<EchoFrame>(source);
            fragments = new List<EchoRecording> { this };
            actions = interactions == null ? new List<RecordedInteraction>() : new List<RecordedInteraction>(interactions);
            actions.Sort((a, b) => a.Time.CompareTo(b.Time));
            actions.RemoveAll(a => a.Time < StartTime || a.Time >= EndTime);
            for (int i = 1; i < frames.Count; i++)
                if (frames[i].Time < frames[i - 1].Time) throw new System.ArgumentException("Frames must be ordered.");
        }
        public static EchoRecording Combine(IEnumerable<EchoRecording> recordings)
        {
            var parts = new List<EchoRecording>();
            foreach (var r in recordings) parts.AddRange(r.Fragments);
            parts.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            if (parts.Count == 0) throw new System.ArgumentException("No fragments");
            for (int i = 1; i < parts.Count; i++) if (parts[i].StartTime < parts[i - 1].EndTime) throw new System.ArgumentException("Overlapping fragments");
            var frames = new List<EchoFrame>(); var actions = new List<RecordedInteraction>();
            foreach (var p in parts) { frames.AddRange(p.Frames); actions.AddRange(p.Actions); }
            var result = new EchoRecording(frames, actions); result.fragments.Clear(); result.fragments.AddRange(parts); return result;
        }
        public bool IsActiveAt(float time)
        { foreach (var p in fragments) if (time >= p.StartTime && time < p.EndTime) return true; return false; }
        public EchoFrame Sample(float time)
        {
            if (fragments.Count > 1)
            {
                foreach (var p in fragments) if (time >= p.StartTime && time < p.EndTime) return p.Sample(time);
                // No interpolation or movement through a gap. Playback hides the body there.
                foreach (var p in fragments) if (time < p.StartTime) return p.Frames[0];
                return fragments[fragments.Count - 1].Frames[fragments[fragments.Count - 1].Frames.Count - 1];
            }
            if (time <= frames[0].Time) return frames[0];
            if (time >= Duration) return frames[frames.Count - 1];
            int lo = 0, hi = frames.Count - 1;
            while (hi - lo > 1) { int m = (hi + lo) / 2; if (frames[m].Time <= time) lo = m; else hi = m; }
            return EchoFrame.Lerp(frames[lo], frames[hi], time);
        }
        public bool HasActivity(float distance)
        {
            if (actions.Count > 0) return true;
            // Displacement from start avoids counting gravity jitter or idle mouse motion.
            foreach (var frame in frames)
                if ((frame.Position - frames[0].Position).sqrMagnitude >= distance * distance) return true;
            float scanTime = 0;
            for (int i = 1; i < frames.Count; i++)
                if (frames[i - 1].Scan.Enabled && !string.IsNullOrEmpty(frames[i - 1].Scan.TargetId))
                    scanTime += frames[i].Time - frames[i - 1].Time;
            if (scanTime >= .1f) return true;
            return false;
        }
    }
}
