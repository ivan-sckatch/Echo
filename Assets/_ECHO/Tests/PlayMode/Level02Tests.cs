using System.Collections;
using System.Collections.Generic;
using EchoGame.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoGame.Tests
{
    public class Level02Tests
    {
        TimeCycleManager c;
        DualPhasePuzzleController puzzle;
        Mouse mouse;
        static readonly WaitForFixedUpdate Step = new WaitForFixedUpdate();
        [UnitySetUp] public IEnumerator Load()
        {
            Time.timeScale = 1; yield return SceneManager.LoadSceneAsync("Level_02"); yield return null;
            c = Object.FindAnyObjectByType<TimeCycleManager>(); puzzle = c.PhasePuzzle;
            mouse = InputSystem.AddDevice<Mouse>(); InputSystem.QueueStateEvent(mouse, new MouseState());
            Time.timeScale = 8; yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { if (mouse != null) InputSystem.RemoveDevice(mouse); if (c) c.Game.SetState(GameState.Playing); Time.timeScale = 1; yield return null; }
        void Aim(PhaseNode node)
        {
            Vector3 direction = node.transform.position - c.Player.View.position; direction.y = 0;
            c.Player.transform.rotation = Quaternion.LookRotation(direction);
            c.Player.View.localRotation = Quaternion.identity;
        }
        IEnumerator Wait(float seconds)
        { for (int i = 0, count = Mathf.CeilToInt(seconds / Time.fixedDeltaTime); i < count; i++) yield return Step; }
        IEnumerator Beam(bool enabled)
        { InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right, enabled)); yield return null; yield return null; }
        IEnumerator Scan(float seconds)
        { yield return Beam(true); yield return Wait(seconds); }
        IEnumerator Walk(Vector3 target, int maxSteps = 450)
        {
            var cc = c.Player.GetComponent<CharacterController>();
            for (int i = 0; i < maxSteps; i++)
            {
                if (c.Game.State != GameState.Playing) yield break;
                Vector3 delta = target - c.Player.Feet; delta.y = 0;
                if (delta.magnitude < .12f) yield break;
                cc.Move(Vector3.ClampMagnitude(delta, c.Player.Speed * Time.fixedDeltaTime)); yield return Step;
            }
            Assert.Fail("Маршрут заблокирован: " + c.Player.Feet + " → " + target);
        }
        void Place(Vector3 position)
        {
            var marker = new GameObject("Test position"); marker.transform.position = position;
            c.Player.Teleport(marker.transform); Object.Destroy(marker); Physics.SyncTransforms();
        }
        EchoRecording EchoAtA(float end = 24)
        {
            var first = new EchoFrame(0, new Vector3(-5, .02f, 17), 270, 0) {
                Scan = new ScanFrame(true, new Vector3(-5, 1.67f, 17), Vector3.left, puzzle.NodeA.StableId) };
            var last = first; last.Time = end;
            return new EchoRecording(new List<EchoFrame> { first, last });
        }
        [UnityTest] public IEnumerator RealTwoCycleRouteOpensBothDoorsAndCompletesWithOneEcho()
        {
            Assert.That(c.Echoes.MaximumEchoes, Is.EqualTo(1)); Assert.That(puzzle.HiddenNode.Visual.enabled, Is.False);
            Aim(puzzle.HiddenNode); yield return Scan(3);
            Assert.That(puzzle.FirstDoorUnlocked, Is.True); Assert.That(c.Echoes.Echoes.Count, Is.Zero);
            yield return Beam(false); yield return Walk(new Vector3(0, 0, 12)); yield return Walk(new Vector3(-5, 0, 17));
            Aim(puzzle.NodeA); yield return Scan(2.7f);
            Assert.That(puzzle.NodeA.HasPlayerSignal, Is.True); Assert.That(puzzle.FinalDoorUnlocked, Is.False);
            for (int i = 0; c.Elapsed < 24 && i < 1500; i++) yield return Step;
            Assert.That(c.Elapsed, Is.InRange(24, 25));
            c.SaveAndRestart(); Time.timeScale = 8;
            Assert.That(c.Echoes.Echoes.Count, Is.EqualTo(1)); Assert.That(puzzle.FirstDoorUnlocked, Is.False);
            var saved = c.Echoes.Echoes[0].Recording;
            Assert.That(saved.Frames[0].Time, Is.Zero);
            Assert.That(saved.Frames[saved.Frames.Count - 1].Scan.TargetId, Is.EqualTo(puzzle.NodeA.StableId));
            bool hiddenRecorded = false;
            foreach (var frame in saved.Frames) if (frame.Scan.Enabled && frame.Scan.TargetId == puzzle.HiddenNode.StableId) hiddenRecorded = true;
            Assert.That(hiddenRecorded, Is.True);
            yield return Beam(false);
            yield return Walk(new Vector3(0, 0, 8)); yield return Walk(new Vector3(0, 0, 12));
            Assert.That(puzzle.FirstDoorUnlocked, Is.True, "Echo scanning must reopen the first door in the new cycle");
            yield return Walk(new Vector3(5, 0, 17)); Aim(puzzle.NodeB); yield return Scan(2.7f);
            for (int i = 0; !puzzle.FinalDoorUnlocked && c.Elapsed < 22 && i < 700; i++) yield return Step;
            Assert.That(puzzle.NodeA.HasEchoSignal, Is.True); Assert.That(puzzle.NodeB.HasPlayerSignal, Is.True);
            Assert.That(puzzle.FinalDoorUnlocked, Is.True); Assert.That(puzzle.Overlap, Is.EqualTo(3));
            var ui = Object.FindAnyObjectByType<UIManager>();
            var data = ui.Provider.Current;
            Assert.That(data.Scanner.DoorUnlocked, Is.True); Assert.That(data.Level, Does.StartWith("02"));
            yield return CaptureWorld(ui, "Level02_DualResonance");
            yield return Beam(false); yield return Walk(new Vector3(0, 0, 25.5f)); yield return Walk(new Vector3(0, 0, 28));
            Assert.That(c.Game.State, Is.EqualTo(GameState.LevelCompleted)); Assert.That(c.Elapsed, Is.LessThan(30)); Assert.That(c.UsedEchoCount, Is.EqualTo(1));
            yield return UITests.CaptureShell(ui, "Level02_Complete");
        }
        [UnityTest] public IEnumerator ScannerInputDiscoveryHighlightAndRelease()
        {
            Aim(puzzle.HiddenNode); yield return Beam(true);
            Assert.That(c.Player.Scanner.Current.Enabled, Is.True); Assert.That(c.Scanning.PlayerVisual.Beam.enabled, Is.True);
            yield return Wait(.6f); Assert.That(puzzle.HiddenNode.IsDiscovered, Is.True);
            Assert.That(puzzle.HiddenNode.Visual.enabled, Is.True); Assert.That(puzzle.FirstDoorUnlocked, Is.False);
            var ui = Object.FindAnyObjectByType<UIManager>();
            yield return CaptureWorld(ui, "Level02_Discovery");
            yield return Beam(false); yield return Step;
            Assert.That(c.Scanning.PlayerVisual.Beam.enabled, Is.False); Assert.That(puzzle.HiddenNode.ActivationProgress(0), Is.Zero);
            yield return Wait(4); Assert.That(puzzle.HiddenNode.IsDiscovered, Is.True);
            yield return Wait(1.2f); Assert.That(puzzle.HiddenNode.IsDiscovered, Is.False);
        }
        [UnityTest] public IEnumerator OneScannerCannotHoldOpposingNodesOrOpenFinalDoor()
        {
            Place(new Vector3(0, .02f, 17)); Aim(puzzle.NodeA); yield return Scan(3);
            Assert.That(puzzle.NodeA.IsSignaling, Is.True);
            Aim(puzzle.NodeB); yield return Wait(3);
            Assert.That(puzzle.NodeA.IsSignaling, Is.False); Assert.That(puzzle.NodeB.IsSignaling, Is.True);
            Assert.That(puzzle.Overlap, Is.Zero); Assert.That(puzzle.FinalDoorUnlocked, Is.False);
            var cc = c.Player.GetComponent<CharacterController>();
            cc.Move(Vector3.forward * 20); Assert.That(c.Player.Feet.z, Is.LessThan(23.6f));
        }
        [UnityTest] public IEnumerator OverlapResetsOnLossThenDoorLatchesForCycle()
        {
            c.Echoes.Add(EchoAtA()); Place(new Vector3(5, .02f, 17)); Aim(puzzle.NodeB);
            yield return Scan(3.3f);
            Assert.That(puzzle.Overlap, Is.GreaterThan(0)); Assert.That(puzzle.Overlap, Is.LessThan(3));
            yield return Beam(false); yield return Step;
            Assert.That(puzzle.Overlap, Is.Zero); Assert.That(puzzle.NodeB.IsSignaling, Is.False);
            yield return Scan(5.7f); Assert.That(puzzle.FinalDoorUnlocked, Is.True);
            yield return Beam(false); yield return Wait(.5f); Assert.That(puzzle.FinalDoor.State, Is.EqualTo(DoorState.Open));
            c.Restart(false);
            Assert.That(puzzle.FirstDoorUnlocked, Is.False); Assert.That(puzzle.FinalDoorUnlocked, Is.False); Assert.That(puzzle.Overlap, Is.Zero);
            Assert.That(puzzle.FinalDoor.State, Is.EqualTo(DoorState.Closed));
        }
        [UnityTest] public IEnumerator PauseTimelineAndEchoExpiryDoNotLeaveSignalsOrBeams()
        {
            c.Echoes.Add(EchoAtA(4)); yield return Wait(2.8f);
            Assert.That(puzzle.NodeA.HasEchoSignal, Is.True);
            foreach (var state in new[] { GameState.Paused, GameState.Timeline })
            {
                c.Game.SetState(state); float time = c.Elapsed; float progress = puzzle.NodeA.ActivationProgress(c.Echoes.Echoes[0].Id);
                Assert.That(c.Scanning.PlayerVisual.Beam.enabled, Is.False);
                foreach (var beam in c.Echoes.Container.GetComponentsInChildren<LineRenderer>()) Assert.That(beam.enabled, Is.False);
                yield return null; yield return null;
                Assert.That(c.Elapsed, Is.EqualTo(time)); Assert.That(puzzle.NodeA.ActivationProgress(c.Echoes.Echoes[0].Id), Is.EqualTo(progress));
                c.Game.Resume(); Time.timeScale = 8; yield return Step;
            }
            yield return Wait(1.5f);
            Assert.That(c.Echoes.Echoes[0].IsActive, Is.False); Assert.That(puzzle.NodeA.IsSignaling, Is.False);
            foreach (var beam in c.Echoes.Container.GetComponentsInChildren<LineRenderer>()) Assert.That(beam.enabled, Is.False);
        }
        [UnityTest] public IEnumerator TenScanningCyclesResetCleanlyAndKeepOneEchoLimit()
        {
            c.Echoes.Add(EchoAtA(12));
            for (int i = 0; i < 10; i++)
            {
                yield return Wait(2.8f); Assert.That(puzzle.NodeA.HasEchoSignal, Is.True);
                c.Restart(false); Time.timeScale = 8;
                Assert.That(c.Echoes.Echoes.Count, Is.EqualTo(1)); Assert.That(puzzle.NodeA.IsSignaling, Is.False);
                Assert.That(puzzle.NodeA.IsDiscovered, Is.False); Assert.That(puzzle.FirstDoorUnlocked, Is.False); Assert.That(puzzle.FinalDoorUnlocked, Is.False);
                Assert.That(c.Scanning.PlayerScanner.Current.Enabled, Is.False);
                foreach (var beam in c.Echoes.Container.GetComponentsInChildren<LineRenderer>()) Assert.That(beam.enabled, Is.False);
            }
            for (int i = 0; i < 4; i++) c.Echoes.Add(EchoAtA(12));
            Assert.That(c.Echoes.Echoes.Count, Is.EqualTo(1));
            c.Restart(true); yield return null;
            Assert.That(c.Echoes.Echoes.Count, Is.Zero); Assert.That(c.Echoes.Container.childCount, Is.Zero);
            Assert.That(puzzle.NodeA.IsSignaling, Is.False);
        }
        [UnityTest] public IEnumerator LevelSelectionAndNextLevelUseExistingMenus()
        {
            yield return SceneManager.LoadSceneAsync("Level_01"); yield return null;
            var first = Object.FindAnyObjectByType<TimeCycleManager>(); var ui = Object.FindAnyObjectByType<UIManager>();
            first.Game.SetState(GameState.LevelCompleted); yield return null;
            Assert.That(ui.Complete.NextLevel.gameObject.activeSelf, Is.True);
            ui.Complete.NextLevel.onClick.Invoke(); yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Level_02"));
            c = Object.FindAnyObjectByType<TimeCycleManager>();
            var secondUi = Object.FindAnyObjectByType<UIManager>(); secondUi.Commands.MainMenu(); yield return null; yield return null;
            var menu = Object.FindAnyObjectByType<UIManager>(); menu.Main.LevelSelect.onClick.Invoke();
            Assert.That(menu.Levels.SecondLevel.gameObject.activeSelf, Is.True); menu.Levels.SecondLevel.onClick.Invoke(); yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Level_02")); c = Object.FindAnyObjectByType<TimeCycleManager>();
        }
        public static IEnumerator CaptureWorld(UIManager shell, string name)
        {
            var camera = Object.FindAnyObjectByType<TimeCycleManager>().Player.View.GetComponent<Camera>(); var canvas = shell.GetComponent<Canvas>();
            var previousCamera = canvas.worldCamera; var previousMode = canvas.renderMode;
            var texture = new RenderTexture(1920, 1080, 24); texture.Create(); camera.targetTexture = texture;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .5f;
            yield return null; Canvas.ForceUpdateCanvases(); camera.Render(); UITests.SaveCapture(texture, name);
            canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; camera.targetTexture = null; Object.Destroy(texture);
        }
    }
}
