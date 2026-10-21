using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace EchoGame.Tests
{
    public class RecordingTests
    {
        [Test] public void ManualRecordingCanStartMidCycleAndKeepMeaningfulHold()
        {
            var go = new GameObject(); var recorder = go.AddComponent<EchoRecorder>();
            try
            {
                recorder.Begin(new EchoFrame(5.113f, Vector3.zero, 0, 0));
                var recording = recorder.Finish(new EchoFrame(6.117f, Vector3.right * 2, 0, 0));
                Assert.That(recording.Frames[0].Time, Is.EqualTo(5.113f));
                Assert.That(recording.Duration, Is.EqualTo(6.117f));
                for (int i = 1; i < recording.Frames.Count; i++) Assert.That(recording.Frames[i].Time, Is.GreaterThan(recording.Frames[i - 1].Time));
                recorder.Begin(new EchoFrame(2, Vector3.zero, 0, 0));
                Assert.That(recorder.Finish(new EchoFrame(4, Vector3.zero, 0, 0), true), Is.Not.Null);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void InterpolationClampsAndWrapsAngles()
        {
            var recording = new EchoRecording(new List<EchoFrame> {
                new EchoFrame(0, Vector3.zero, 350, 0), new EchoFrame(2, Vector3.right * 10, 10, 20) });
            Assert.That(recording.Sample(1).Position.x, Is.EqualTo(5).Within(.001));
            Assert.That(Mathf.DeltaAngle(recording.Sample(1).Yaw, 0), Is.EqualTo(0).Within(.001));
            Assert.That(recording.Sample(-1).Position, Is.EqualTo(Vector3.zero));
            Assert.That(recording.Sample(9).Pitch, Is.EqualTo(20));
        }
        [Test] public void RecordingOwnsIndependentCopyAndRejectsIdle()
        {
            var frames = new List<EchoFrame> { new EchoFrame(0, Vector3.zero, 0, 0), new EchoFrame(1, Vector3.zero, 90, 0) };
            var recording = new EchoRecording(frames); frames.Clear();
            Assert.That(recording.Frames.Count, Is.EqualTo(2)); Assert.That(recording.HasActivity(.75f), Is.False);
        }
        [Test] public void RecorderResamplesAndPreservesExactEnd()
        {
            var go = new GameObject(); var recorder = go.AddComponent<EchoRecorder>();
            try
            {
                recorder.Begin(new EchoFrame(0, Vector3.zero, 0, 0));
                recorder.Capture(new EchoFrame(.073f, Vector3.right, 0, 0));
                var result = recorder.Finish(new EchoFrame(.105f, Vector3.right * 2, 0, 0));
                Assert.That(result.Duration, Is.EqualTo(.105f));
                Assert.That(result.Frames.Count, Is.EqualTo(7));
                Assert.That(result.Frames[3].Time, Is.EqualTo(.06f).Within(.00001));
                recorder.Begin(new EchoFrame(0, Vector3.zero, 0, 0));
                Assert.That(recorder.Finish(new EchoFrame(2, Vector3.zero, 0, 0)), Is.Null);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void PlaybackStopsInteractingAtEndAndRestarts()
        {
            var go = new GameObject(); var echo = go.AddComponent<EchoPlayback>();
            echo.Body = new GameObject("Body").transform; echo.Body.SetParent(go.transform);
            try
            {
                echo.Initialize(new EchoRecording(new List<EchoFrame> { new EchoFrame(0, Vector3.zero, 0, 0), new EchoFrame(2, Vector3.right, 0, 0) }));
                echo.Evaluate(1); Assert.That(echo.IsActive, Is.True);
                echo.Evaluate(2); Assert.That(echo.IsActive, Is.False);
                echo.Evaluate(0); Assert.That(echo.IsActive, Is.True); Assert.That(echo.Feet, Is.EqualTo(Vector3.zero));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void PlateUsesFeetAndRejectsJumpingAboveIt()
        {
            var go = new GameObject(); var plate = go.AddComponent<PressurePlate>();
            try
            {
                go.transform.position = new Vector3(6, 0, 9);
                Assert.That(plate.Contains(new Vector3(6, .1f, 9)), Is.True);
                Assert.That(plate.Contains(new Vector3(6, 1, 9)), Is.False);
                Assert.That(plate.Contains(new Vector3(0, .1f, 9)), Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void BuilderIsIdempotentAndAllReferencesAreWired()
        {
            Editor.Level01Builder.Build(); Editor.Level01Builder.Build();
            var games = Object.FindObjectsByType<GameManager>();
            Assert.That(games.Length, Is.EqualTo(1));
            var c = games[0].Cycles;
            Assert.That(c.Player.View, Is.Not.Null); Assert.That(c.Recorder, Is.Not.Null);
            Assert.That(c.Echoes.Prefab, Is.Not.Null); Assert.That(c.Puzzle.Door.Panel, Is.Not.Null);
            Assert.That(c.Exit, Is.Not.Null); Assert.That(c.UI, Is.Not.Null);
            Assert.That(c.Duration, Is.EqualTo(30));
        }
    }
}

