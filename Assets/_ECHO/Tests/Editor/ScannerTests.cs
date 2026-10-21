using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace EchoGame.Tests
{
    public class ScannerTests
    {
        static PhaseNode MakeNode(GameObject root)
        { var node = root.AddComponent<PhaseNode>(); node.StableId = "test-node"; node.Mode = PhaseNodeMode.Continuous; node.ResetNode(); return node; }
        static void Step(PhaseNode node, int count, bool hit = true, float dt = .02f, int actor = 0)
        { for (int i = 0; i < count; i++) { node.BeginStep(); if (hit) node.Submit(actor); node.EndStep(dt); } }
        [Test] public void DiscoveryAndActivationAreSeparateAndLossResetsProgress()
        {
            var go = new GameObject(); var node = MakeNode(go);
            try
            {
                Step(node, 24); Assert.That(node.IsDiscovered, Is.False);
                Step(node, 1); Assert.That(node.IsDiscovered, Is.True); Assert.That(node.ActivationProgress(0), Is.LessThan(.01f));
                Step(node, 99); Assert.That(node.IsSignaling, Is.False);
                Step(node, 1); Assert.That(node.IsSignaling, Is.True);
                Step(node, 1, false); Assert.That(node.IsSignaling, Is.False); Assert.That(node.ActivationProgress(0), Is.Zero);
                Step(node, 230, false); Assert.That(node.IsDiscovered, Is.True);
                Step(node, 30, false); Assert.That(node.IsDiscovered, Is.False);
                Step(node, 15); Step(node, 1, false); Step(node, 10); Assert.That(node.IsDiscovered, Is.False);
                node.ResetNode(); Assert.That(node.HasActivated, Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void ImpulseFiresOncePerContinuousExposureAndSourcesDoNotShareWarmup()
        {
            var go = new GameObject(); var node = MakeNode(go); int pulses = 0; node.Mode = PhaseNodeMode.Impulse;
            node.Activated += (_, __) => pulses++;
            try
            {
                Step(node, 126); Assert.That(pulses, Is.EqualTo(1));
                Step(node, 150); Assert.That(pulses, Is.EqualTo(1));
                Step(node, 1, false); Step(node, 100); Assert.That(pulses, Is.EqualTo(2));
                node.Mode = PhaseNodeMode.Continuous; node.ResetNode(); Step(node, 80, true, .02f, 1);
                Assert.That(node.IsSignaling, Is.False);
                Step(node, 60, true, .02f, 2); Assert.That(node.IsSignaling, Is.False);
                Step(node, 40, true, .02f, 2); Assert.That(node.IsSignaling, Is.True);
                Assert.That(node.HasEchoSignal, Is.True); Assert.That(node.HasPlayerSignal, Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void RaycastEnforcesRangeOcclusionAndRecordedIdentityEvenWithHiddenRenderer()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); var node = MakeNode(go);
            go.transform.position = new Vector3(1000, 1, 1000); go.GetComponent<Collider>().isTrigger = true;
            go.GetComponent<Renderer>().enabled = false;
            var registry = new ScanRegistry(); registry.Rebuild(new[] { node });
            var frame = new ScanFrame(true, new Vector3(1000, 1, 990), Vector3.forward, node.StableId);
            GameObject wall = null;
            try
            {
                Physics.SyncTransforms();
                Assert.That(ScanDetector.Trace(frame, 12, ~0, registry, true, out _), Is.EqualTo(node));
                Assert.That(ScanDetector.Trace(frame, 9, ~0, registry, false, out _), Is.Null);
                frame.TargetId = "wrong-id"; Assert.That(ScanDetector.Trace(frame, 12, ~0, registry, true, out _), Is.Null);
                frame.TargetId = node.StableId; wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = new Vector3(1000, 1, 995); wall.transform.localScale = Vector3.one * 2;
                Physics.SyncTransforms(); Assert.That(ScanDetector.Trace(frame, 12, ~0, registry, true, out var end), Is.Null);
                Assert.That(end.z, Is.LessThan(996));
                frame.Enabled = false; Assert.That(ScanDetector.Trace(frame, 12, ~0, registry, false, out _), Is.Null);
                Assert.Throws<System.InvalidOperationException>(() => registry.Rebuild(new[] { node, node }));
            }
            finally { if (wall) Object.DestroyImmediate(wall); Object.DestroyImmediate(go); }
        }
        [Test] public void ScannerFlagsAndTargetsAreStepwiseButPoseIsInterpolated()
        {
            var a = new EchoFrame(0, Vector3.zero, 350, 0) { Scan = new ScanFrame(true, Vector3.up, Vector3.forward, "A") };
            var b = new EchoFrame(1, Vector3.right, 10, 20) { Scan = new ScanFrame(false, Vector3.one, Vector3.right) };
            var mid = EchoFrame.Lerp(a, b, .5f);
            Assert.That(mid.Scan.Enabled, Is.True); Assert.That(mid.Scan.TargetId, Is.EqualTo("A"));
            Assert.That(mid.Scan.Origin.x, Is.EqualTo(.5f)); Assert.That(mid.Scan.Direction.magnitude, Is.EqualTo(1).Within(.001));
            Assert.That(EchoFrame.Lerp(a, b, 1).Scan.Enabled, Is.False);
            var recording = new EchoRecording(new List<EchoFrame> { a, b }); Assert.That(recording.HasActivity(50), Is.True);
        }
        [TestCase(30)] [TestCase(60)] [TestCase(144)]
        public void RecordingResamplesScannerAtFiftyHertzAcrossRenderRates(int fps)
        {
            var go = new GameObject(); var recorder = go.AddComponent<EchoRecorder>();
            EchoFrame Frame(float t) => new EchoFrame(t, Vector3.right * t, 0, 0) { Scan = new ScanFrame(t >= .5f && t < 3, new Vector3(t, 1.65f, 0), Vector3.forward, t >= .5f && t < 3 ? "node" : null) };
            try
            {
                recorder.Begin(Frame(0)); for (int i = 1; i <= fps * 4; i++) recorder.Capture(Frame(i / (float)fps));
                var recording = recorder.Finish(Frame(4));
                Assert.That(recording.Frames.Count, Is.EqualTo(201));
                float first = -1;
                foreach (var frame in recording.Frames) if (frame.Scan.Enabled) { first = frame.Time; break; }
                Assert.That(first, Is.InRange(.5f - .001f, .5f + 1f / fps + .021f));
                Assert.That(recording.Sample(1).Scan.TargetId, Is.EqualTo("node"));
                Assert.That(recording.Sample(3.1f).Scan.Enabled, Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void Level02BuilderIsIdempotentAndPreservesFirstSceneBytes()
        {
            string original = File.ReadAllText(Editor.Level01Builder.ScenePath);
            Editor.Level02Builder.BuildScene(false); Editor.Level02Builder.BuildScene(false);
            var cycles = Object.FindAnyObjectByType<TimeCycleManager>();
            Assert.That(Object.FindObjectsByType<TimeCycleManager>().Length, Is.EqualTo(1));
            Assert.That(cycles.Echoes.MaximumEchoes, Is.EqualTo(1)); Assert.That(cycles.Duration, Is.EqualTo(30));
            Assert.That(cycles.Scanning.Nodes.Length, Is.EqualTo(3));
            Assert.That(cycles.Player.Scanner, Is.Not.Null); Assert.That(cycles.PhasePuzzle.ControlPanel, Is.Not.Null);
            Assert.That(cycles.UI.Bridge, Is.Not.Null); Assert.That(cycles.Puzzle, Is.Null);
            Assert.That(File.ReadAllText(Editor.Level01Builder.ScenePath), Is.EqualTo(original));
        }
    }
}
