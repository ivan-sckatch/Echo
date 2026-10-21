using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace EchoGame.Tests
{
    public class Level01Tests
    {
        TimeCycleManager c;
        static readonly WaitForFixedUpdate Step = new WaitForFixedUpdate();
        [UnitySetUp] public IEnumerator Load()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Level_01");
            yield return null;
            c = Object.FindAnyObjectByType<TimeCycleManager>();
            Assert.That(c, Is.Not.Null);
            Time.timeScale = 8;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { c.Game.SetState(GameState.Playing); Time.timeScale = 1; yield return null; }
        IEnumerator Walk(Vector3 target, int maximumSteps = 400)
        {
            var cc = c.Player.GetComponent<CharacterController>();
            for (int i = 0; i < maximumSteps; i++)
            {
                if (c.Game.State != GameState.Playing) yield break;
                var delta = target - c.Player.Feet; delta.y = 0;
                if (delta.magnitude < .12f) yield break;
                cc.Move(Vector3.ClampMagnitude(delta, c.Player.Speed * Time.fixedDeltaTime));
                yield return Step;
            }
        }
        IEnumerator WaitSeconds(float seconds)
        {
            int steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (int i = 0; i < steps; i++) yield return Step;
        }
        [UnityTest] public IEnumerator CannotPassDoorAloneButOneEchoSolvesLevel()
        {
            yield return Walk(new Vector3(6, 0, 9));
            yield return WaitSeconds(.5f);
            Assert.That(c.Puzzle.Plate.IsActive, Is.True);
            Assert.That(c.Puzzle.Door.State, Is.EqualTo(DoorState.Open));
            yield return Walk(new Vector3(0, 0, 16), 180);
            Assert.That(c.Player.Feet.z, Is.LessThan(13.6f), "Closed door must block the solo player");
            Assert.That(c.Game.State, Is.EqualTo(GameState.Playing));
            c.Restart(true); Time.timeScale = 8;
            yield return Walk(new Vector3(6, 0, 9));
            yield return WaitSeconds(14);
            c.SaveAndRestart(); Time.timeScale = 8;
            Assert.That(c.Echoes.Echoes.Count, Is.EqualTo(1));
            Assert.That(c.Player.Feet.z, Is.EqualTo(2).Within(.1f));
            yield return Walk(new Vector3(0, 0, 12));
            yield return WaitSeconds(1);
            Assert.That(c.Puzzle.Plate.IsActive, Is.True, "Recorded echo should hold the platform");
            yield return Walk(new Vector3(0, 0, 20));
            Assert.That(c.Game.State, Is.EqualTo(GameState.LevelCompleted));
            Assert.That(c.UsedEchoCount, Is.EqualTo(1));
            yield return UITests.CaptureShell(Object.FindAnyObjectByType<EchoGame.UI.UIManager>(), "LevelComplete");
            float finished = c.Elapsed;
            yield return null; yield return null;
            Assert.That(c.Elapsed, Is.EqualTo(finished));
        }
        [UnityTest] public IEnumerator TenCyclesLimitResetPauseAndExpiry()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return Walk(new Vector3(2, 0, 3));
                c.SaveAndRestart(); Time.timeScale = 8;
                Assert.That(c.Echoes.Echoes.Count, Is.EqualTo(Mathf.Min(i + 1, 3)));
                c.Restart(false); Time.timeScale = 8;
                Assert.That(c.Elapsed, Is.Zero);
                Assert.That(c.Echoes.Echoes.Count, Is.EqualTo(Mathf.Min(i + 1, 3)));
                yield return Step;
            }
            var oldest = c.Echoes.Echoes[0];
            Assert.That(c.Echoes.Echoes[1].Recording, Is.Not.SameAs(oldest.Recording));
            c.Game.SetState(GameState.Paused);
            float paused = c.Elapsed; Vector3 echoPosition = oldest.Feet;
            yield return null; yield return null;
            Assert.That(c.Elapsed, Is.EqualTo(paused)); Assert.That(oldest.Feet, Is.EqualTo(echoPosition));
            c.Game.Resume(); Time.timeScale = 8;
            yield return WaitSeconds(2);
            foreach (var echo in c.Echoes.Echoes) Assert.That(echo.IsActive, Is.False);
            c.Restart(false); Time.timeScale = 8;
            foreach (var echo in c.Echoes.Echoes) Assert.That(echo.IsActive, Is.True);
            c.Restart(true); Time.timeScale = 8;
            Assert.That(c.Echoes.Echoes.Count, Is.Zero); Assert.That(c.CycleNumber, Is.EqualTo(1));
            Assert.That(c.Puzzle.Plate.IsActive, Is.False); Assert.That(c.Puzzle.Door.State, Is.EqualTo(DoorState.Closed));
            yield return null;
            Assert.That(c.Echoes.Container.childCount, Is.Zero);
        }
        [UnityTest] public IEnumerator AutoTimeoutAndOverlappingEchoes()
        {
            var record = new EchoRecording(new List<EchoFrame> { new EchoFrame(0, new Vector3(6, .1f, 9), 0, 0), new EchoFrame(2, new Vector3(6, .1f, 9), 0, 0) });
            c.Echoes.Add(record); c.Echoes.Add(record);
            yield return Step;
            Assert.That(c.Puzzle.Plate.IsActive, Is.True);
            c.Echoes.Echoes[0].Evaluate(2);
            c.Puzzle.Plate.Evaluate(c.Player, c.Echoes);
            Assert.That(c.Puzzle.Plate.IsActive, Is.True);
            c.Echoes.Echoes[1].Evaluate(2);
            c.Puzzle.Plate.Evaluate(c.Player, c.Echoes);
            Assert.That(c.Puzzle.Plate.IsActive, Is.False);
            c.Restart(true); Time.timeScale = 8;
            c.Duration = 1;
            yield return Walk(new Vector3(2, 0, 2), 30);
            yield return WaitSeconds(1);
            Assert.That(c.CycleNumber, Is.GreaterThan(1));
            Assert.That(c.Echoes.Echoes.Count, Is.GreaterThanOrEqualTo(1));
        }
        [UnityTest] public IEnumerator KeyboardMovementJumpAndCycleCommands()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                yield return null;
                yield return WaitSeconds(.8f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Assert.That(c.Player.Feet.z, Is.GreaterThan(4), "W should move the player forward");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                yield return null; yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return WaitSeconds(.12f);
                Assert.That(c.Player.Feet.y, Is.GreaterThan(.2f), "Space should jump");
                yield return WaitSeconds(1.5f);
                Assert.That(c.Player.Feet.y, Is.LessThan(.1f), "Gravity should return to floor");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
                yield return null; yield return null;
                Assert.That(c.Echoes.Echoes.Count, Is.EqualTo(1));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.T)); yield return null; yield return null;
                Assert.That(c.Echoes.Echoes.Count, Is.EqualTo(1));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); yield return null; yield return null;
                Assert.That(c.Game.State, Is.EqualTo(GameState.Paused));
                float paused = c.Elapsed;
                yield return null; yield return null;
                Assert.That(c.Elapsed, Is.EqualTo(paused));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); yield return null; yield return null;
                Assert.That(c.Game.State, Is.EqualTo(GameState.Playing));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift, Key.R)); yield return null; yield return null;
                Assert.That(c.Echoes.Echoes.Count, Is.Zero);
                Assert.That(c.Player.Feet.z, Is.EqualTo(2).Within(.1f));
                var controller = c.Player.GetComponent<CharacterController>();
                controller.Move(Vector3.right * 30);
                Assert.That(c.Player.Feet.x, Is.LessThan(9.5f), "Outer wall must block movement");
                controller.Move(Vector3.up * 20);
                Assert.That(c.Player.Feet.y, Is.LessThan(2.3f), "Ceiling must prevent jumping over walls");
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }
    }
}

