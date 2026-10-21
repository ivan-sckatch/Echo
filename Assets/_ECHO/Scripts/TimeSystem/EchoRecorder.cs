using System.Collections.Generic;
using UnityEngine;
namespace EchoGame
{
    public sealed class EchoRecorder : MonoBehaviour
    {
        [Range(10, 100)] public int SampleRate = 50;
        [Min(.1f)] public float MinimumActivity = .75f;
        readonly List<EchoFrame> frames = new List<EchoFrame>(4096);
        EchoFrame previous;
        int nextSample;
        public void Begin(EchoFrame initial) { frames.Clear(); frames.Add(initial); previous = initial; nextSample = Mathf.FloorToInt(initial.Time * SampleRate) + 1; }
        public void Capture(EchoFrame current)
        {
            // Resample the observed trajectory on an exact grid, independent of rendering FPS.
            while (nextSample / (float)SampleRate <= current.Time)
            {
                frames.Add(EchoFrame.Lerp(previous, current, nextSample / (float)SampleRate));
                nextSample++;
            }
            previous = current;
        }
        public EchoRecording Finish(EchoFrame final, bool meaningfulInteraction = false)
        {
            Capture(final);
            if (frames[frames.Count - 1].Time < final.Time) frames.Add(final);
            else frames[frames.Count - 1] = final;
            var result = new EchoRecording(frames);
            return result.HasActivity(MinimumActivity) || (meaningfulInteraction && result.Duration - result.Frames[0].Time >= .1f) ? result : null;
        }
    }
}
