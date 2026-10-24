using System.Collections;
using System.Collections.Generic;
using EchoGame.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace EchoGame.Tests
{
    public class Level03Tests
    {
        TimeCycleManager c; Level03Controller p;
        static readonly WaitForFixedUpdate Step = new WaitForFixedUpdate();
        [UnitySetUp] public IEnumerator Load()
        { Time.timeScale = 1; yield return SceneManager.LoadSceneAsync("Level_03"); yield return null; c = Object.FindAnyObjectByType<TimeCycleManager>(); p = c.TimedPuzzle; Time.timeScale = 8; }
        [UnityTearDown] public IEnumerator Cleanup() { c.Game.SetState(GameState.Playing); Time.timeScale = 1; yield return null; }
        IEnumerator Wait(float seconds) { for (int i = 0; i < Mathf.CeilToInt(seconds / Time.fixedDeltaTime); i++) yield return Step; }
        IEnumerator Until(float time) { for (int i = 0; c.Elapsed < time && i < 1600; i++) yield return Step; }
        IEnumerator Walk(float x, float z)
        {
            var cc = c.Player.GetComponent<CharacterController>();
            for (int i = 0; i < 700; i++)
            {
                if (c.Game.State == GameState.LevelCompleted) yield break;
                var delta = new Vector3(x, c.Player.Feet.y, z) - c.Player.Feet;
                if (delta.magnitude < .1f) yield break;
                cc.Move(Vector3.ClampMagnitude(delta, c.Player.Speed * Time.fixedDeltaTime)); yield return Step;
            }
            Assert.Fail("Route blocked at " + c.Player.Feet);
        }
        void Aim(PulseButton b)
        { var delta = b.transform.position - c.Player.View.position; c.Player.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg, 0); c.Player.View.localRotation = Quaternion.identity; }
        void Press(int index) { Aim(p.Buttons[index]); c.Game.Interaction.TryInteract(); }
        IEnumerator FirstDoor()
        { yield return Walk(3.5f, 4); Press(0); yield return Walk(0, 10); }
        [UnityTest] public IEnumerator ResearchManualRecordingAndRealTwoCycleSolution()
        {
            Assert.That(c.Game.State, Is.EqualTo(GameState.Research));
            yield return Level02Tests.CaptureWorld(Object.FindAnyObjectByType<UIManager>(), "Level03_Research");
            yield return Walk(3.5f, 4); Press(0); c.ToggleRecording(); yield return Wait(1);
            yield return Level02Tests.CaptureWorld(Object.FindAnyObjectByType<UIManager>(), "Level03_WallButton");
            Assert.That(c.Elapsed, Is.Zero); Assert.That(c.IsRecording, Is.False); Assert.That(p.Buttons[0].PressCount, Is.Zero);
            p.StartCycle(); Time.timeScale = 8;
            Assert.That(c.CycleNumber, Is.EqualTo(1)); Assert.That(c.IsRecording, Is.False); Assert.That(c.Player.Feet.z, Is.EqualTo(2).Within(.1f));
            yield return FirstDoor(); yield return Walk(3.5f, 12); Aim(p.Buttons[1]); yield return Until(10);
            c.ToggleRecording(); yield return Until(12); Press(1); yield return Until(13); c.SaveAndRestart(); Time.timeScale = 8;
            Assert.That(c.Echoes.Echoes.Count, Is.EqualTo(1)); var echo = c.Echoes.Echoes[0]; var recording = echo.Recording;
            Assert.That(recording.StartTime, Is.InRange(10, 10.1f)); Assert.That(recording.Actions.Count, Is.EqualTo(1)); Assert.That(echo.IsActive, Is.False);
            Assert.That(p.Doors[0].Door.Command, Is.False); Assert.That(p.Doors[1].Door.Command, Is.False);
            yield return FirstDoor(); yield return Walk(0, 46);
            yield return Until(13.2f);
            Assert.That(p.Buttons[1].PressCount, Is.EqualTo(1)); Assert.That(echo.IsActive, Is.False);
            Assert.That(p.Doors[1].Remaining, Is.GreaterThan(4));
            var ui = Object.FindAnyObjectByType<UIManager>();
            c.Player.transform.rotation = Quaternion.identity; c.Player.View.localRotation = Quaternion.identity;
            yield return Level02Tests.CaptureWorld(ui, "Level03_TimedDoor");
            yield return Walk(0, 50.5f);
            Assert.That(c.Game.State, Is.EqualTo(GameState.LevelCompleted)); Assert.That(c.Elapsed, Is.LessThan(18)); Assert.That(c.UsedEchoCount, Is.EqualTo(1));
            yield return UITests.CaptureShell(ui, "Level03_Complete");
        }
        [UnityTest] public IEnumerator SoloRunCannotBeatSecondWindowAndResetClearsTimers()
        {
            p.StartCycle(); Time.timeScale = 8; yield return FirstDoor(); yield return Walk(3.5f, 12); Press(1);
            float start = c.Elapsed;
            var cc = c.Player.GetComponent<CharacterController>();
            for (int i = 0; i < 500 && c.Elapsed < start + 9; i++) { var d = new Vector3(0, c.Player.Feet.y, 50) - c.Player.Feet; cc.Move(d.normalized * c.Player.Speed * Time.fixedDeltaTime); yield return Step; }
            Assert.That(c.Player.Feet.z, Is.LessThan(47.5f)); Assert.That(p.Doors[1].Door.Command, Is.False);
            c.Restart(false); Assert.That(p.Buttons[1].PressCount, Is.Zero); Assert.That(p.Doors[1].OpenUntil, Is.Zero); Assert.That(c.IsRecording, Is.False);
        }
        [UnityTest] public IEnumerator RejectOverlapRetainExistingAndExplicitDelete()
        {
            p.StartCycle(); Time.timeScale = 8;
            var pos = new Vector3(3.5f, .02f, 12);
            var old = new EchoRecording(new List<EchoFrame> { new EchoFrame(10, pos, 0, 0), new EchoFrame(13, pos, 0, 0) });
            c.Echoes.Add(old); yield return FirstDoor(); yield return Walk(3.5f, 12); yield return Until(10);
            c.ToggleRecording(); yield return Until(11); Press(1); yield return Until(12); c.SaveAndRestart();
            Assert.That(p.CurrentRejection, Is.Not.Null); Assert.That(c.Echoes.Echoes.Count, Is.EqualTo(1)); Assert.That(c.Echoes.Echoes[0].Recording, Is.SameAs(old));
            Assert.That(c.Elapsed, Is.GreaterThanOrEqualTo(12)); Assert.That(c.IsRecording, Is.False);
            c.Game.SetState(GameState.Timeline); p.DeleteLast(); Assert.That(c.Echoes.Echoes.Count, Is.Zero);
        }
        [UnityTest] public IEnumerator KeyboardStartsResearchAndHoldingFRecordsOnePress()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R, Key.F)); yield return null; yield return null;
                Assert.That(c.Elapsed, Is.Zero); Assert.That(c.IsRecording, Is.False);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab)); yield return null; yield return null;
                Assert.That(c.Game.State, Is.EqualTo(GameState.Timeline)); Assert.That(c.Elapsed, Is.Zero);
                c.Game.Resume(); Assert.That(c.Game.State, Is.EqualTo(GameState.Research));
                c.Game.TogglePause(); c.Game.Resume(); Assert.That(c.Game.State, Is.EqualTo(GameState.Research));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); yield return null; yield return null;
                Assert.That(c.Game.State, Is.EqualTo(GameState.Playing));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                yield return Walk(3.5f, 4); Aim(p.Buttons[0]); c.ToggleRecording();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); yield return null; yield return Wait(.5f);
                Assert.That(p.Buttons[0].PressCount, Is.EqualTo(1));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; c.SaveAndRestart();
                Assert.That(c.Echoes.Echoes.Count, Is.EqualTo(1)); Assert.That(c.Echoes.Echoes[0].Recording.Actions.Count, Is.EqualTo(1));
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }
        [UnityTest] public IEnumerator CrossedEventsExactlyOncePauseResetExpiryAndWallAccess()
        {
            p.StartCycle(); Time.timeScale = 8;
            var position = new Vector3(3.5f, .02f, 12);
            var r = new EchoRecording(new List<EchoFrame> { new EchoFrame(10, position, 90, 0), new EchoFrame(13, position, 90, 0) }, new List<RecordedInteraction> { new RecordedInteraction(12.013f, p.Buttons[1].StableId) });
            c.Echoes.Add(r); var e = c.Echoes.Echoes[0];
            e.ReplayEvents(12.02f, p); e.ReplayEvents(12.04f, p); Assert.That(p.Buttons[1].PressCount, Is.EqualTo(1));
            c.Game.SetState(GameState.Paused); float until = p.Doors[1].OpenUntil; yield return null; yield return null;
            Assert.That(p.Doors[1].OpenUntil, Is.EqualTo(until)); Assert.That(p.Buttons[1].PressCount, Is.EqualTo(1));
            c.Game.Resume(); c.Restart(false); Time.timeScale = 8;
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.transform.position = new Vector3(4.5f, 1.65f, 12); blocker.transform.localScale = new Vector3(.2f, 3, 2);
            e.ReplayEvents(12.02f, p); Assert.That(p.Buttons[1].PressCount, Is.Zero); Object.Destroy(blocker); yield return null;
            c.Restart(false); Time.timeScale = 8; yield return Until(13.3f);
            Assert.That(p.Buttons[1].PressCount, Is.EqualTo(1)); Assert.That(e.IsActive, Is.False); Assert.That(p.Doors[1].OpenUntil, Is.EqualTo(18.013f).Within(.001f));
            c.Game.SetState(GameState.Timeline); var t = c.Elapsed; yield return null; yield return null; Assert.That(c.Elapsed, Is.EqualTo(t));
            c.Game.Resume(); Time.timeScale = 8; yield return Until(29.9f); yield return Wait(.3f);
            Assert.That(c.Elapsed, Is.LessThan(1)); Assert.That(c.Echoes.Echoes.Count, Is.EqualTo(1));
            c.Restart(true); Assert.That(c.Echoes.Echoes.Count, Is.Zero);
        }
    }
}
