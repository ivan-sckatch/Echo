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
        TimeCycleManager c; Level03Controller p; Mouse mouse; Keyboard keyboard;
        static readonly WaitForFixedUpdate Step = new WaitForFixedUpdate();
        [UnitySetUp] public IEnumerator Load()
        {
            Time.timeScale = 1; yield return SceneManager.LoadSceneAsync("Level_03"); yield return null;
            c = Object.FindAnyObjectByType<TimeCycleManager>(); p = c.TimedPuzzle;
            mouse = InputSystem.AddDevice<Mouse>(); keyboard = InputSystem.AddDevice<Keyboard>(); Time.timeScale = 8;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard); c.Game.SetState(GameState.Playing); Time.timeScale = 1; yield return null; }
        IEnumerator Wait(float seconds) { for(int i=0;i<Mathf.CeilToInt(seconds/Time.fixedDeltaTime);i++) yield return Step; }
        IEnumerator Until(float time) { for(int i=0;c.Elapsed<time && i<1800;i++) yield return Step; Assert.That(c.Elapsed, Is.GreaterThanOrEqualTo(time)); }
        IEnumerator Walk(float x, float z, int steps=700)
        {
            var cc=c.Player.GetComponent<CharacterController>();
            for(int i=0;i<steps;i++)
            {
                if(c.Game.State==GameState.LevelCompleted) yield break;
                var delta=new Vector3(x,c.Player.Feet.y,z)-c.Player.Feet;
                if(delta.magnitude<.12f) yield break;
                cc.Move(Vector3.ClampMagnitude(delta,c.Player.Speed*Time.fixedDeltaTime)); yield return Step;
            }
            Assert.Fail("Blocked route at "+c.Player.Feet);
        }
        IEnumerator Gallery(int index)
        { yield return Walk(-10.5f,2); yield return Walk(-10.5f,p.Chambers[index].ObservationPoint.position.z); yield return Walk(-2,p.Chambers[index].ObservationPoint.position.z); }
        IEnumerator Aim(Vector3 target)
        {
            Vector3 d=target-c.Player.View.position;
            float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg, pitch=-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;
            c.Player.transform.rotation=Quaternion.Euler(0,yaw,0);
            InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(0,-(pitch-c.Player.Pitch)/c.Player.MouseSensitivity)});
            yield return null; yield return null;
        }
        void Press(int i) => c.Game.Interaction.TryInteract();
        IEnumerator Record(int index,float start,float press,float end)
        {
            yield return Gallery(index); yield return Aim(p.Buttons[index].transform.position);
            yield return Until(start); c.ToggleRecording(); yield return Until(press); Press(index);
            Assert.That(p.Chambers[index].Isolated,Is.True); yield return Until(end); c.SaveAndRestart(); Time.timeScale=8;
        }
        [UnityTest] public IEnumerator ThreeRealFragmentsOneEchoAndMainRouteCompletes()
        {
            Assert.That(c.Game.State,Is.EqualTo(GameState.Research));
            p.StartCycle(); Time.timeScale=8;
            yield return Record(0,7,8,9); Assert.That(c.Echoes.Echoes.Count,Is.EqualTo(1));
            yield return Record(1,11,12,13); Assert.That(c.Echoes.Echoes.Count,Is.EqualTo(1));
            yield return Record(2,19,20,21);
            var e=c.Echoes.Echoes[0]; Assert.That(e.Id,Is.EqualTo(1)); Assert.That(e.Recording.Fragments.Count,Is.EqualTo(3)); Assert.That(e.Recording.Actions.Count,Is.EqualTo(3));
            yield return Walk(2,10.8f); yield return Until(9.5f);
            Assert.That(e.IsActive,Is.False); Assert.That(p.Doors[0].Door.Command,Is.True);
            yield return Walk(2,22.8f); yield return Until(13.5f);
            Assert.That(p.Doors[1].Door.Command,Is.True); Assert.That(e.IsActive,Is.False);
            yield return Walk(2,36.8f); yield return Until(20.5f);
            Assert.That(p.Doors[2].Door.Command,Is.True);
            yield return Aim(new Vector3(2,1.65f,43));
            yield return Level02Tests.CaptureWorld(Object.FindAnyObjectByType<UIManager>(),"L03_FinalGate");
            yield return Walk(2,42);
            Assert.That(c.Game.State,Is.EqualTo(GameState.LevelCompleted)); Assert.That(c.Elapsed,Is.LessThan(26)); Assert.That(c.UsedEchoCount,Is.EqualTo(1));
            foreach(var b in p.Buttons) Assert.That(b.PressCount,Is.EqualTo(1));
            yield return UITests.CaptureShell(Object.FindAnyObjectByType<UIManager>(),"L03_Complete");
        }
        [UnityTest] public IEnumerator ResearchVisitsAllCamerasWindowsAndPreservesFragments()
        {
            for(int i=0;i<3;i++)
            {
                if(i>0) { yield return Walk(-10.5f,p.Chambers[i-1].ObservationPoint.position.z); yield return Walk(-10.5f,p.Chambers[i].ObservationPoint.position.z); yield return Walk(-2,p.Chambers[i].ObservationPoint.position.z); }
                else yield return Gallery(i);
                yield return Aim(p.Buttons[i].transform.position); Press(i); c.ToggleRecording();
                Assert.That(p.Buttons[i].PressCount,Is.Zero); Assert.That(c.IsRecording,Is.False); Assert.That(c.Elapsed,Is.Zero);
                Vector3 origin=c.Player.View.position, door=p.Doors[i].Door.transform.position+Vector3.up*1.7f;
                var hits=Physics.RaycastAll(origin,(door-origin).normalized,Vector3.Distance(origin,door)+.2f,~0,QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
                Assert.That(hits.Length,Is.GreaterThanOrEqualTo(2)); Assert.That(hits[0].collider,Is.SameAs(p.Chambers[i].Window));
                Assert.That(hits[1].collider.transform,Is.EqualTo(p.Doors[i].Door.Panel));
                yield return Level02Tests.CaptureWorld(Object.FindAnyObjectByType<UIManager>(),"L03_Window_"+i);
                // Window cannot be crossed by a physical character or used to reach a device behind it.
                var blocker=c.Player.GetComponent<CharacterController>(); blocker.Move(Vector3.right*8); Assert.That(c.Player.Feet.x,Is.LessThan(0));
                yield return Walk(-2,p.Chambers[i].ObservationPoint.position.z);
            }
            p.StartCycle(); Time.timeScale=8; yield return Record(0,7,8,9);
            var saved=c.Echoes.Echoes[0].Recording; p.EnterResearch(); Assert.That(c.Echoes.Echoes[0].Recording,Is.SameAs(saved));
            c.Game.SetState(GameState.Timeline); c.Game.Resume(); Assert.That(c.Game.State,Is.EqualTo(GameState.Research));
            p.StartCycle(); Assert.That(c.Echoes.Echoes[0].Recording,Is.SameAs(saved));
        }
        [UnityTest] public IEnumerator EveryChamberLocksAndResetsNoSoloEscape()
        {
            for(int i=0;i<3;i++)
            {
                p.StartCycle(); Time.timeScale=8; yield return Gallery(i); yield return Aim(p.Buttons[i].transform.position); Press(i); yield return Wait(.5f);
                Assert.That(p.Doors[i].Door.Command,Is.True); Assert.That(p.Chambers[i].Entrance.State,Is.EqualTo(DoorState.Closed));
                Press(i); Assert.That(p.Buttons[i].PressCount,Is.EqualTo(1));
                var cc=c.Player.GetComponent<CharacterController>();
                for(int s=0;s<110;s++) { cc.Move(Vector3.left*c.Player.Speed*Time.fixedDeltaTime); yield return Step; }
                Assert.That(c.Player.Feet.x,Is.GreaterThan(-8.9f));
                if(i<2) { yield return Wait(6.3f); Assert.That(p.Doors[i].Door.Command,Is.True); }
                else { yield return Wait(4.5f); Assert.That(p.Doors[2].Door.Command,Is.False); Assert.That(p.Doors[2].Door.State,Is.EqualTo(DoorState.Closed)); }
                c.Restart(false); Time.timeScale=8;
                foreach(var chamber in p.Chambers) { Assert.That(chamber.Isolated,Is.False); Assert.That(chamber.Entrance.State,Is.EqualTo(DoorState.Open)); }
                foreach(var d in p.Doors) Assert.That(d.Door.Command,Is.False);
            }
        }
        [UnityTest] public IEnumerator ConflictGapsDeletionResearchAndFullReset()
        {
            p.StartCycle(); Time.timeScale=8; yield return Record(0,7,8,9); yield return Record(1,11,12,13);
            var saved=c.Echoes.Echoes[0].Recording;
            yield return Gallery(1); yield return Aim(p.Buttons[1].transform.position);
            yield return Until(11.5f);
            c.ToggleRecording(); Press(1); yield return Wait(.3f); c.SaveAndRestart();
            Assert.That(p.CurrentRejection,Is.Not.Null); Assert.That(c.Echoes.Echoes[0].Recording,Is.SameAs(saved));
            p.EnterResearch(); Assert.That(c.Echoes.Echoes.Count,Is.EqualTo(1)); p.DeleteLast(); Assert.That(c.Echoes.Echoes[0].Recording.Fragments.Count,Is.EqualTo(1));
            c.Restart(true); Assert.That(c.Echoes.Echoes.Count,Is.Zero);
            c.Echoes.Add(saved.Fragments[0]); Assert.That(c.Echoes.Echoes[0].Id,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator PausedTimelineCrossedEventsAndExpiry()
        {
            p.StartCycle(); Time.timeScale=8; yield return Record(0,7,8,9);
            c.Game.SetState(GameState.Paused); float t=c.Elapsed; yield return null; yield return null; Assert.That(c.Elapsed,Is.EqualTo(t));
            c.Game.Resume(); c.Game.SetState(GameState.Timeline); yield return null; Assert.That(p.Buttons[0].PressCount,Is.Zero);
            c.Game.Resume(); Time.timeScale=8; yield return Until(10); Assert.That(p.Buttons[0].PressCount,Is.EqualTo(1)); Assert.That(c.Echoes.Echoes[0].IsActive,Is.False); Assert.That(p.Doors[0].Door.Command,Is.True);
            yield return Until(29.9f); yield return Wait(.3f); Assert.That(c.Elapsed,Is.LessThan(1)); Assert.That(c.Echoes.Echoes.Count,Is.EqualTo(1)); Assert.That(p.Buttons[0].PressCount,Is.Zero);
        }
    }
}
