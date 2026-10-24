using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace EchoGame.Tests
{
    public class Level03RecordingTests
    {
        [Test] public void BuilderRebuildsOnlyThirdScene()
        {
            var one = System.IO.File.ReadAllBytes("Assets/_ECHO/Scenes/Levels/Level_01.unity");
            var two = System.IO.File.ReadAllBytes("Assets/_ECHO/Scenes/Levels/Level_02.unity");
            EchoGame.Editor.Level03Builder.BuildScene(false); EchoGame.Editor.Level03Builder.BuildScene(false);
            Assert.That(Object.FindObjectsByType<PulseButton>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
            Assert.That(Object.FindObjectsByType<TimedDoor>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(System.IO.File.ReadAllBytes("Assets/_ECHO/Scenes/Levels/Level_01.unity"), Is.EqualTo(one));
            Assert.That(System.IO.File.ReadAllBytes("Assets/_ECHO/Scenes/Levels/Level_02.unity"), Is.EqualTo(two));
        }
        static EchoRecording Clip(float start, float end) => new EchoRecording(new List<EchoFrame> { new EchoFrame(start, Vector3.zero, 0, 0), new EchoFrame(end, Vector3.zero, 0, 0) });
        [Test] public void LateStationaryPressIsMeaningfulAndHalfOpen()
        {
            var r = new EchoRecording(new List<EchoFrame> { new EchoFrame(10, Vector3.one, 20, 0), new EchoFrame(13, Vector3.one, 20, 0) },
                new List<RecordedInteraction> { new RecordedInteraction(12, "B"), new RecordedInteraction(13, "B") });
            Assert.That(r.StartTime, Is.EqualTo(10)); Assert.That(r.EndTime, Is.EqualTo(13)); Assert.That(r.Length, Is.EqualTo(3));
            Assert.That(r.Actions.Count, Is.EqualTo(1)); Assert.That(r.HasActivity(.75f), Is.True);
            Assert.That(Clip(10, 13).HasActivity(.75f), Is.False);
        }
        [Test] public void IntervalTouchAllowedOverlapRejectedWithoutEviction()
        {
            var root = new GameObject("Interval test"); var prefab = new GameObject("Playback");
            try
            {
                var manager = root.AddComponent<EchoManager>(); manager.Container = root.transform; manager.ConcurrentIntervals = true;
                manager.Prefab = prefab.AddComponent<EchoPlayback>(); manager.Prefab.Body = new GameObject("Body").transform; manager.Prefab.Body.SetParent(prefab.transform);
                manager.Add(Clip(0, 8)); manager.Add(Clip(8, 16));
                Assert.That(manager.Echoes.Count, Is.EqualTo(2)); Assert.That(manager.FindConflict(Clip(7.99f, 8.01f)), Is.Not.Null);
                manager.Add(Clip(2, 3)); Assert.That(manager.Echoes.Count, Is.EqualTo(2));
                Assert.That(manager.Echoes[0].Id, Is.EqualTo(1)); Assert.That(manager.Echoes[1].Id, Is.EqualTo(2));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(prefab); }
        }
    }
}
