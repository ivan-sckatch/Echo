using System.Collections;
using EchoGame.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoGame.Tests
{
    public class ResearchInspectionTests
    {
        [UnityTest] public IEnumerator ResearchObjectsHaveDescriptionsAndNoTechnicalSigns()
        {
            foreach (string scene in new[] { "Level_03", "Level_04" })
            {
                yield return SceneManager.LoadSceneAsync(scene); yield return null;
                var c = Object.FindAnyObjectByType<TimeCycleManager>();
                Assert.That(c.Game.State, Is.EqualTo(GameState.Research));
                int described = 0;
                foreach (var collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
                    if (ResearchInspection.Describe(collider, c, out var title, out var action))
                    { Assert.That(title, Is.Not.Empty); Assert.That(action.Length, Is.GreaterThan(20)); described++; }
                Assert.That(described, Is.GreaterThanOrEqualTo(scene == "Level_03" ? 9 : 7));
                foreach (var door in Object.FindObjectsByType<DoorController>(FindObjectsSortMode.None))
                {
                    var area = door.GetComponent<BoxCollider>();
                    Assert.That(area && area.isTrigger, Is.True, "Open doors must remain inspectable without blocking movement");
                    Assert.That(ResearchInspection.Describe(area, c, out var title, out var action), Is.True);
                    Assert.That(action.ToLowerInvariant(), Does.Contain("сброс").Or.Contain("закрывается"));
                }
                foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                    Assert.That((text is TextMeshPro ? text.transform.parent.name : text.name), Does.Not.StartWith("Door display ").And.Not.StartWith("Button label ")
                        .And.Not.StartWith("Chamber label ").And.Not.StartWith("Camera display ").And.Not.StartWith("Node U"));
            }
        }
        [UnityTest] public IEnumerator ResearchCardRefreshesWithFrozenClockAndClearsOnReleaseOrWall()
        {
            yield return SceneManager.LoadSceneAsync("Level_04"); yield return null;
            var c = Object.FindAnyObjectByType<TimeCycleManager>();
            var bridge = Object.FindAnyObjectByType<UIEventBridge>();
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                var node = c.SecurityPuzzle.FirstNode;
                c.Player.GetComponent<CharacterController>().enabled = false;
                c.Player.transform.position = new Vector3(0, 3.02f, 29.7f);
                c.Player.GetComponent<CharacterController>().enabled = true;
                c.Player.Gravity = 0;
                c.Player.transform.rotation = Quaternion.LookRotation(new Vector3(node.transform.position.x - c.Player.View.position.x, 0, node.transform.position.z - c.Player.View.position.z));
                Physics.SyncTransforms();
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right));
                for(int i=0;i<5;i++) yield return new WaitForFixedUpdate();
                Assert.That(bridge.Current.Scanner.Enabled, Is.True);
                Assert.That(bridge.Current.Scanner.Target, Does.Contain("ПРОХОДА"));
                Assert.That(bridge.Current.Scanner.Action, Does.Contain("Открывает"));
                Assert.That(c.Elapsed, Is.Zero); Assert.That(node.HasActivated, Is.False);
                var hud = Object.FindAnyObjectByType<ScannerHUD>();
                Assert.That(hud.Status.text, Is.EqualTo(bridge.Current.Scanner.Target));
                Assert.That(hud.Progress.gameObject.activeSelf, Is.False);
                yield return new WaitForEndOfFrame();
                yield return UITests.CaptureShell(Object.FindAnyObjectByType<UIManager>(), "L04_ResearchInspection");
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position = c.Player.View.position + c.Player.View.forward;
                wall.transform.localScale = new Vector3(.5f,.5f,.1f); Physics.SyncTransforms();
                yield return null; yield return null;
                Assert.That(bridge.Current.Scanner.Enabled, Is.False, "Scanner must not identify through a wall");
                Object.Destroy(wall); yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState());
                for(int i=0;i<3;i++) yield return new WaitForFixedUpdate();
                Assert.That(bridge.Current.Scanner.Enabled, Is.False);
            }
            finally { InputSystem.RemoveDevice(mouse); c.Player.GetComponent<CharacterController>().enabled = true; }
        }
    }
}



