using System.Collections.Generic;
using UnityEngine;
namespace EchoGame
{
    public sealed class EchoRecording
    {
        readonly List<EchoFrame> frames;
        public IReadOnlyList<EchoFrame> Frames => frames;
        public float Duration => frames[frames.Count - 1].Time;
        public EchoRecording(List<EchoFrame> source)
        {
            if (source == null || source.Count == 0) throw new System.ArgumentException("Recording requires frames.");
            frames = new List<EchoFrame>(source);
            for (int i = 1; i < frames.Count; i++)
                if (frames[i].Time < frames[i - 1].Time) throw new System.ArgumentException("Frames must be ordered.");
        }
        public EchoFrame Sample(float time)
        {
            if (time <= frames[0].Time) return frames[0];
            if (time >= Duration) return frames[frames.Count - 1];
            int lo = 0, hi = frames.Count - 1;
            while (hi - lo > 1) { int m = (hi + lo) / 2; if (frames[m].Time <= time) lo = m; else hi = m; }
            return EchoFrame.Lerp(frames[lo], frames[hi], time);
        }
        public bool HasActivity(float distance)
        {
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
