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
    public sealed class CaptureInteractionProbe : MonoBehaviour, IInteractable
    {
        public int Calls;
        public string Prompt => "Probe";
        public bool CanInteract => true;
        public void Interact() => Calls++;
    }
    public class Level04Tests
    {
        TimeCycleManager c; Level04Controller p; Keyboard keyboard; Mouse mouse;
        static readonly WaitForFixedUpdate Step = new WaitForFixedUpdate();
        [UnitySetUp] public IEnumerator Load()
        {
            Time.timeScale=1; yield return SceneManager.LoadSceneAsync("Level_04"); yield return null;
            c=Object.FindAnyObjectByType<TimeCycleManager>();p=c.SecurityPuzzle;
            Assert.That(c.Game.State, Is.EqualTo(GameState.Research));
            p.StartCycle();
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();Time.timeScale=3;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);if(c)c.Game.SetState(GameState.Playing);Time.timeScale=1;yield return null; }
        void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        IEnumerator Until(float time)
        { for(int i=0;c.Elapsed<time && i<1600;i++)yield return Step;Assert.That(c.Elapsed,Is.GreaterThanOrEqualTo(time)); }
        IEnumerator Wait(float seconds) { for(int i=0;i<Mathf.CeilToInt(seconds/Time.fixedDeltaTime);i++)yield return Step; }
        IEnumerator Walk(float x,float z,bool stopOnCapture=false)
        {
            Keys(Key.W);
            for(int i=0;i<450;i++)
            {
                if(c.Game.State==GameState.LevelCompleted) { Keys();yield break; }
                if(c.Player.Immobilized) { Keys();if(stopOnCapture)yield break;Assert.Fail("Captured on route at "+c.Player.Feet+" t="+c.Elapsed); }
                Vector3 delta=new Vector3(x,0,z)-new Vector3(c.Player.Feet.x,0,c.Player.Feet.z);
                if(delta.magnitude<.19f) { Keys();yield return null;yield break; }
                c.Player.transform.rotation=Quaternion.LookRotation(delta);yield return Step;
            }
            Keys();Assert.Fail("Blocked walking to "+x+","+z+" at "+c.Player.Feet+" t="+c.Elapsed);
        }
        IEnumerator Beam(bool value) { InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right,value));yield return null;yield return null; }
        IEnumerator Scan(PhaseNode node)
        {
            var d=node.transform.position-c.Player.View.position;d.y=0;c.Player.transform.rotation=Quaternion.LookRotation(d);
            yield return Beam(true);yield return Wait(2.22f);yield return Beam(false);Assert.That(node.HasActivated,Is.True,"Failed scan "+node.name+" at "+c.Player.Feet);
        }
        IEnumerator LowerRoute()
        { yield return Until(1.4f);yield return Walk(0,11.5f);Debug.Log("L04_TIMING lower corridor "+c.Elapsed);yield return Walk(0,18.3f);Debug.Log("L04_TIMING staircase "+c.Elapsed);Assert.That(c.Player.Feet.y,Is.InRange(2.95f,3.1f)); }
        IEnumerator Jump()
        {
            yield return Walk(0,23.5f);Keys();yield return Wait(.12f);
            Assert.That(c.Player.GetComponent<CharacterController>().isGrounded,Is.True,"Jump input requires landing after test teleport");
            c.Player.transform.rotation=Quaternion.identity;Keys(Key.W,Key.Space);yield return null;yield return null;Keys(Key.W);
            float peak=c.Player.Feet.y;
            for(int i=0;i<70 && c.Player.Feet.z<26.7f;i++) { peak=Mathf.Max(peak,c.Player.Feet.y);yield return Step; }
            Keys();yield return Wait(.18f);Assert.That(c.Player.Feet.z,Is.GreaterThan(26.4f));Assert.That(peak,Is.GreaterThan(3.7f));Assert.That(c.Player.Feet.y,Is.GreaterThan(2.9f));
        }
        IEnumerator ToFinal()
        {
            yield return LowerRoute();
            if(c.Echoes.Echoes[0].Recording.Fragments.Count==4) { c.Player.transform.rotation=Quaternion.identity;yield return Level02Tests.CaptureWorld(Object.FindAnyObjectByType<UIManager>(),"L04_UpperGallery"); }
            yield return Until(7.6f);yield return Jump();Debug.Log("L04_TIMING jump landing "+c.Elapsed);
            yield return Walk(0,29.7f);yield return Scan(p.FirstNode);Debug.Log("L04_TIMING U1 "+c.Elapsed);yield return Walk(0,33);Debug.Log("L04_TIMING third zone "+c.Elapsed);
        }
        IEnumerator CaptureUntil(float x,float z,float end)
        { yield return Walk(x,z,true);Assert.That(c.Player.Immobilized,Is.True);Debug.Log("L04_TIMING captured "+c.Elapsed);yield return Until(end);Keys();c.SaveAndRestart();Time.timeScale=3;yield return null; }
        void Place(Vector3 point)
        { var go=new GameObject("Test marker");go.transform.position=point;c.Player.Teleport(go.transform);Object.Destroy(go);Physics.SyncTransforms(); }
        [UnityTest] public IEnumerator ResearchFreezesClockDisablesDevicesAndPreservesPreparation()
        {
            var saved=new EchoRecording(new List<EchoFrame>{new EchoFrame(0,p.BaitA.position,0,0),new EchoFrame(3,p.BaitA.position,0,0)});
            c.Echoes.Add(saved);p.EnterResearch();int cycle=c.CycleNumber;
            Assert.That(c.Game.State,Is.EqualTo(GameState.Research));Assert.That(p.FirstDoor.State,Is.EqualTo(DoorState.Open));Assert.That(p.ExitDoor.State,Is.EqualTo(DoorState.Open));
            Assert.That(c.Echoes.Echoes[0].IsActive,Is.False);
            Place(new Vector3(0,3.02f,29.7f));var d=p.FirstNode.transform.position-c.Player.View.position;d.y=0;c.Player.transform.rotation=Quaternion.LookRotation(d);
            yield return Beam(true);yield return Wait(2.5f);c.ToggleRecording();
            Assert.That(c.IsRecording,Is.False);Assert.That(p.FirstNode.HasActivated,Is.False);Assert.That(c.Scanning.PlayerTarget,Is.SameAs(p.FirstNode));
            Assert.That(c.Elapsed,Is.Zero);Assert.That(c.CycleNumber,Is.EqualTo(cycle));foreach(var camera in p.Cameras)Assert.That(camera.Target,Is.Null);
            yield return Beam(false);c.Game.SetState(GameState.Timeline);c.Game.Resume();Assert.That(c.Game.State,Is.EqualTo(GameState.Research));
            c.Game.TogglePause();c.Game.Resume();Assert.That(c.Game.State,Is.EqualTo(GameState.Research));
            Place(c.Exit.transform.position);yield return Wait(.1f);Assert.That(c.Game.State,Is.EqualTo(GameState.Research));
            Keys(Key.Enter);yield return null;yield return null;Keys();
            Assert.That(c.Game.State,Is.EqualTo(GameState.Playing));Assert.That(c.Echoes.Echoes[0].Recording,Is.SameAs(saved));Assert.That(p.FirstDoor.Command,Is.False);Assert.That(p.ExitDoor.Command,Is.False);
            Keys(Key.Backspace);yield return null;yield return null;Keys();
            Assert.That(c.Game.State,Is.EqualTo(GameState.Research));Assert.That(c.Echoes.Echoes[0].IsActive,Is.False);
            yield return Level02Tests.CaptureWorld(Object.FindAnyObjectByType<UIManager>(),"L04_Research");
        }
        [UnityTest] public IEnumerator FallingResetsAtAnyHorizontalPositionInPlayAndResearch()
        {
            var saved=new EchoRecording(new List<EchoFrame>{new EchoFrame(.7f,p.BaitA.position,0,0),new EchoFrame(3,p.BaitA.position,0,0)});c.Echoes.Add(saved);
            foreach(float z in new[]{2f,25f,45f})
            {
                int cycle=c.CycleNumber;c.ToggleRecording();Place(new Vector3(0,c.FallResetHeight-1,z));yield return Step;yield return Step;
                Assert.That(c.Game.State,Is.EqualTo(GameState.Playing));Assert.That(c.CycleNumber,Is.EqualTo(cycle+1));Assert.That(c.Player.Feet.y,Is.GreaterThan(c.FallResetHeight));Assert.That(c.IsRecording,Is.False);Assert.That(c.Echoes.Echoes[0].Recording,Is.SameAs(saved));
            }
            p.EnterResearch();int count=c.CycleNumber;Place(new Vector3(0,c.FallResetHeight-1,2));yield return Step;yield return Step;
            Assert.That(c.Game.State,Is.EqualTo(GameState.Research));Assert.That(c.CycleNumber,Is.EqualTo(count));Assert.That(c.Elapsed,Is.Zero);Assert.That(p.FirstDoor.Command,Is.True);Assert.That(c.Player.Feet.y,Is.GreaterThan(c.FallResetHeight));
        }
        [UnityTest] public IEnumerator CaptureBlocksMovementJumpScanAndStillSavesRecording()
        {
            yield return Until(.7f);c.ToggleRecording();yield return Walk(0,8,true);Assert.That(c.Player.Immobilized,Is.True);
            Vector3 before=c.Player.Feet;Keys(Key.W,Key.Space);yield return Beam(true);yield return Wait(1.5f);
            Assert.That(Vector3.Distance(before,c.Player.Feet),Is.LessThan(.001f));Assert.That(c.Player.Scanner.Current.Enabled,Is.False);Assert.That(c.IsRecording,Is.True);
            var probeObject=GameObject.CreatePrimitive(PrimitiveType.Cube);probeObject.transform.position=c.Player.View.position+c.Player.View.forward*2;probeObject.transform.localScale=Vector3.one*.2f;
            var probe=probeObject.AddComponent<CaptureInteractionProbe>();Physics.SyncTransforms();c.Game.Interaction.TryInteract();Assert.That(probe.Calls,Is.Zero);Object.Destroy(probeObject);
            c.Game.SetState(GameState.Paused);float t=c.Elapsed;yield return null;Assert.That(c.Elapsed,Is.EqualTo(t));c.Game.Resume();Time.timeScale=3;
            Keys();yield return Beam(false);c.ToggleRecording();Assert.That(c.Player.Immobilized,Is.False);Assert.That(c.Echoes.Echoes.Count,Is.EqualTo(1));Assert.That(c.Elapsed,Is.Zero);
            for(int i=0;i<3;i++){c.Restart(false);Assert.That(c.Echoes.Echoes.Count,Is.EqualTo(1));foreach(var camera in p.Cameras)Assert.That(camera.Target,Is.Null);}
            c.Restart(true);Assert.That(c.Echoes.Echoes.Count,Is.Zero);
        }
        [UnityTest] public IEnumerator CameraRespectsOcclusionElevationAndHoldsEchoUntilFragmentEnds()
        {
            var player=c.Player.GetComponent<CaptureTarget>();
            Place(p.RoomSafe.position);Assert.That(p.Cameras[2].CanSee(player),Is.False);
            Place(p.ExitSafe.position);Assert.That(p.Cameras[2].CanSee(player),Is.False);
            Place(new Vector3(0,3.02f,20));Assert.That(p.Cameras[0].CanSee(player),Is.False);
            Place(new Vector3(0,3.2f,5));Assert.That(p.Cameras[0].CanSee(player),Is.False,"Lower ceiling must hide a target above it");
            var frames=new List<EchoFrame>{new EchoFrame(.5f,p.BaitA.position,0,0),new EchoFrame(2.5f,p.BaitA.position,0,0)};
            c.Echoes.Add(new EchoRecording(frames));Place(c.Spawn.position);yield return Until(.8f);
            var held=p.Cameras[0].Target;Assert.That(held,Is.Not.Null);Assert.That(held.Echo,Is.Not.Null);
            Place(new Vector3(1,0,5));yield return Wait(.3f);Assert.That(p.Cameras[0].Target,Is.SameAs(held));Assert.That(c.Player.Immobilized,Is.False);
            Place(c.Spawn.position);yield return Until(2.7f);Assert.That(p.Cameras[0].Target,Is.Null);Assert.That(held.Echo.IsActive,Is.False);
            // Opaque geometry, rather than a special target mask, blocks acquisition.
            Place(p.BaitA.position);var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=new Vector3(0,1.6f,7);blocker.transform.localScale=new Vector3(5,3,.3f);Physics.SyncTransforms();
            Assert.That(p.Cameras[0].CanSee(player),Is.False);Object.Destroy(blocker);
        }
        [UnityTest] public IEnumerator WalkingCannotCrossGapAndFallPreservesFragments()
        {
            c.Echoes.Add(new EchoRecording(new List<EchoFrame>{new EchoFrame(0,p.BaitB.position,0,0),new EchoFrame(30,p.BaitB.position,0,0)}));
            yield return Wait(.12f);int cycle=c.CycleNumber;
            Place(new Vector3(0,3.02f,23));Keys(Key.W);yield return Wait(1.1f);Keys();
            Assert.That(c.Player.Feet.z,Is.LessThan(26.3f),"Walking must not bridge the jump");
            Assert.That(c.CycleNumber,Is.GreaterThan(cycle));Assert.That(c.Echoes.Echoes.Count,Is.EqualTo(1));
            c.Restart(false);Place(new Vector3(0,3.02f,23.5f));yield return Jump();
        }
        [UnityTest] public IEnumerator FourRealFragmentsOneEchoFinalRouteCompletesWithinThirtySeconds()
        {
            yield return Until(.7f);c.ToggleRecording();yield return CaptureUntil(-.7f,8,5.8f);
            yield return LowerRoute();yield return Walk(-3,19.3f);yield return Until(6.4f);c.ToggleRecording();yield return CaptureUntil(-3,22,13.8f);
            yield return ToFinal();yield return Until(16);c.ToggleRecording();yield return CaptureUntil(0,38,18.8f);
            yield return ToFinal();yield return Until(16.7f);yield return Walk(-1.8f,37.8f);yield return Walk(-4.6f,38);
            Debug.Log("L04_TIMING protected room "+c.Elapsed);
            Assert.That(c.Player.Immobilized,Is.False);yield return Until(19.2f);c.ToggleRecording();yield return Scan(p.ExitNode);
            Debug.Log("L04_TIMING U2 "+c.Elapsed);
            yield return CaptureUntil(-.5f,38,28);
            Assert.That(c.Echoes.Echoes.Count,Is.EqualTo(1));var echo=c.Echoes.Echoes[0];Assert.That(echo.Id,Is.EqualTo(1));Assert.That(echo.Recording.Fragments.Count,Is.EqualTo(4));
            yield return ToFinal();yield return Until(16.7f);yield return Walk(1.8f,37.8f);yield return Walk(3.7f,38);
            Debug.Log("L04_TIMING waiting niche "+c.Elapsed);
            yield return Until(19);Assert.That(echo.IsActive,Is.False);Assert.That(p.Cameras[2].Target,Is.Null);Assert.That(c.Player.Immobilized,Is.False);
            yield return Until(23.5f);Assert.That(p.ExitNode.HasActivated,Is.True);Assert.That(p.Cameras[2].Target.Echo,Is.SameAs(echo));
            yield return Level02Tests.CaptureWorld(Object.FindAnyObjectByType<UIManager>(),"L04_ExitShelter");
            yield return Walk(1.8f,37.8f);yield return Walk(0,38);c.Player.transform.rotation=Quaternion.identity;
            yield return Level02Tests.CaptureWorld(Object.FindAnyObjectByType<UIManager>(),"L04_FinalCamera");yield return Walk(0,46);
            Assert.That(c.Game.State,Is.EqualTo(GameState.LevelCompleted));Assert.That(c.Elapsed,Is.LessThan(30));Assert.That(c.UsedEchoCount,Is.EqualTo(1));
            Assert.That(p.Cameras[2].CaptureCount,Is.EqualTo(2));Assert.That(p.Cameras[0].CaptureCount,Is.EqualTo(1));Assert.That(p.Cameras[1].CaptureCount,Is.EqualTo(1));
            Debug.Log("L04_TIMING complete "+c.Elapsed);
            yield return UITests.CaptureShell(Object.FindAnyObjectByType<UIManager>(),"L04_Complete");
        }
    }
}
