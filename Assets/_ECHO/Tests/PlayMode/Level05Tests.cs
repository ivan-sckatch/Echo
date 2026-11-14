using System.Collections;
using EchoGame.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoGame.Tests
{
    public class Level05Tests
    {
        TimeCycleManager c;
        Level05Controller p;
        Keyboard keyboard;
        Mouse mouse;
        float lastWalkTime;
        static readonly WaitForFixedUpdate Step = new WaitForFixedUpdate();
        [UnitySetUp] public IEnumerator Load()
        {
            Time.timeScale=1;yield return SceneManager.LoadSceneAsync("Level_05_Soglasie");yield return null;
            c=Object.FindAnyObjectByType<TimeCycleManager>();p=c.GeometryPuzzle;
            Assert.That(c.Game.State,Is.EqualTo(GameState.Research));p.StartCycle();
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();Time.timeScale=8;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);if(c)c.Game.SetState(GameState.Playing);Time.timeScale=1;yield return null; }
        void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        IEnumerator Until(float time)
        {int cycle=c.CycleNumber;for(int i=0;c.Elapsed<time && i<1600;i++){Assert.That(c.CycleNumber,Is.EqualTo(cycle),"Unexpected cycle reset");yield return Step;}Assert.That(c.Elapsed,Is.GreaterThanOrEqualTo(time));}
        IEnumerator Wait(float seconds){for(int i=0;i<Mathf.CeilToInt(seconds/Time.fixedDeltaTime);i++)yield return Step;}
        IEnumerator Capture(string name)
        {
            float speed=Time.timeScale;Time.timeScale=0;
            yield return new WaitForSecondsRealtime(.5f);
            yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            var folder="C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-12-Level05-Upper/Captures";
            System.IO.Directory.CreateDirectory(folder);System.IO.File.WriteAllBytes(folder+"/"+name+".png",image.EncodeToPNG());Object.Destroy(image);
            Time.timeScale=speed;
        }
        IEnumerator Walk(float x,float z,int expectedSection=-1)
        {
            int cycle=c.CycleNumber;Keys(Key.W);yield return null;
            var previous=c.Player.Feet;float previousTime=c.Elapsed;
            for(int i=0;i<650;i++)
            {
                if(expectedSection>=0 && p.ActiveSection==expectedSection){Keys();yield return null;yield break;}
                if(c.Game.State==GameState.LevelCompleted){Keys();yield break;}
                Assert.That(c.CycleNumber,Is.EqualTo(cycle),"Reset walking from "+previous+" t="+previousTime+" to "+x+","+z);
                var delta=new Vector3(x,0,z)-new Vector3(c.Player.Feet.x,0,c.Player.Feet.z);
                if(delta.magnitude<.2f){Keys();yield return null;yield break;}
                previous=c.Player.Feet;previousTime=c.Elapsed;lastWalkTime=c.Elapsed;c.Player.transform.rotation=Quaternion.LookRotation(delta);Keys(Key.W);yield return Step;
            }
            bool held=Keyboard.current.wKey.isPressed;Keys();Assert.Fail("Blocked at "+c.Player.Feet+" to "+x+","+z+" t="+c.Elapsed+" key="+held+" grounded="+c.Player.GetComponent<CharacterController>().isGrounded+" state="+c.Game.State);
        }
        IEnumerator Press(GeometryTerminal terminal,bool expectedActive=true)
        {
            c.Player.transform.rotation=Quaternion.LookRotation(terminal.transform.position-c.Player.View.position);
            // Terminals are mounted at eye height: use the real gameplay ray and recorder path.
            c.Player.View.localRotation=Quaternion.identity;
            c.Game.Interaction.TryInteract();yield return null;
            Assert.That(terminal.IsActive,Is.EqualTo(expectedActive),"Terminal trace failed: "+terminal.name+" from "+c.Player.View.position);
        }
        IEnumerator RecordPlate(PressurePlate plate)
        {
            c.ToggleRecording();yield return Walk(plate.transform.position.x,plate.transform.position.z);
            yield return Until(29.5f);c.SaveAndRestart();yield return null;
        }
        IEnumerator RecordTerminal(GeometryTerminal terminal,float x,float z,float time,float end=29.5f)
        {
            c.ToggleRecording();yield return Walk(x,z);yield return Until(time);yield return Press(terminal);
            yield return Until(end);c.SaveAndRestart();yield return null;
        }
        IEnumerator Lower()
        {
            yield return RecordPlate(p.PlateA);yield return RecordPlate(p.PlateB);
            Assert.That(c.Echoes.Echoes.Count,Is.EqualTo(2));yield return Until(7);
            Assert.That(p.LowerReady,Is.True);yield return Walk(-6,-10);yield return Walk(11,-10);yield return Walk(11,10,1);
            Assert.That(p.ActiveSection,Is.EqualTo(1));Assert.That(c.Echoes.Echoes.Count,Is.Zero);
        }
        IEnumerator Middle(bool afterExpiry=false)
        {
            c.ToggleRecording();yield return Walk(12,11);yield return Until(3);yield return Press(p.MiddleWall);
            yield return Walk(9.5f,11);yield return Until(16);yield return Press(p.LiftFinal,false);
            float recordingEnd=afterExpiry ? 24 : 29.5f;
            yield return Until(recordingEnd);c.SaveAndRestart();yield return null;
            Assert.That(c.Echoes.Echoes[0].Recording.Actions.Count,Is.EqualTo(2),"Record both the partition and final circuit interaction");
            yield return RecordTerminal(p.MiddleBridge,7,11,5,recordingEnd);
            yield return Until(6.2f);yield return Walk(5,11);yield return Walk(-11,11);yield return Walk(-11,9.5f);yield return Press(p.Lift);
            float first=c.Elapsed;
            yield return Walk(-11,6.5f);yield return Press(p.LiftSecond);float second=c.Elapsed;
            if (!afterExpiry) yield return Capture("MiddleSequence");
            yield return Until(16.2f);Assert.That(p.LiftSequenceStep,Is.EqualTo(3),"Recorded final lever must complete the circuit");
            yield return Wait(1.6f);Assert.That(p.Middle[2].AtTarget,Is.True,"Middle route did not settle at "+c.Elapsed);
            Assert.That(p.MiddleBridge.IsActive,Is.False,"Lift must work after the bridge closes");
            if (afterExpiry)
            {
                yield return Walk(-11,-10.35f);
                yield return Until(26.15f);
                Assert.That(p.ActiveEchoCount,Is.Zero);
                Assert.That(p.LiftFinal.IsActive,Is.False);
                Assert.That(p.Middle[2].IsMoving,Is.True);
                yield return Walk(-11,-12,2);
                Assert.That(p.ActiveSection,Is.EqualTo(2));
                Assert.That(p.Middle[2].Locked,Is.True);
                Assert.That(p.Middle[2].IsMoving,Is.False);
                Assert.That(p.Middle[2].Progress,Is.InRange(.01f,.99f));
                var frozen=p.Middle[2].Capture();
                c.Restart(false);yield return Wait(.3f);
                Assert.That(p.ActiveSection,Is.EqualTo(2));
                Assert.That(p.Middle[2].Progress,Is.EqualTo(frozen.Progress));
                Assert.That(p.Middle[2].transform.localRotation,Is.EqualTo(frozen.Rotation));
                yield break;
            }
            yield return Walk(-11,-12,2);
            Assert.That(lastWalkTime,Is.LessThan(25),"Keep at least one second of lift-window margin");
            var folder="C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-12-Level05-Upper";
            System.IO.File.WriteAllText(folder+"/MiddleTiming.txt",$"First island lever: {first:0.00}s; second: {second:0.00}s; recorded final lever: 16s; checkpoint entry: {lastWalkTime:0.00}s; lift deadline: 26s. Physical walking, no teleport to solve the route.");
            Assert.That(p.ActiveSection,Is.EqualTo(2));Assert.That(c.Echoes.Echoes.Count,Is.Zero);
        }
        [UnityTest] public IEnumerator CompleteByRecordedInputsAndWalkingAllThreeTiers()
        {
            yield return Capture("Start");yield return Lower();yield return Capture("Checkpoint01");yield return Middle();yield return Capture("Checkpoint02");
            Assert.That(p.Lower[0].Locked && p.Middle[0].Locked,Is.True);
            Keys(Key.LeftShift,Key.R);yield return null;yield return null;Keys();yield return null;
            Assert.That(p.ActiveSection,Is.EqualTo(2));Assert.That(p.Middle[0].Locked,Is.True);
            Assert.That(c.Player.Feet.y,Is.EqualTo(16).Within(.1f));
            Assert.That(c.UsedEchoCount,Is.EqualTo(4),"Completed checkpoint contributions survive Shift+R");
            c.Restart(false);yield return null;Assert.That(p.ActiveSection,Is.EqualTo(2));Assert.That(p.Middle[0].Locked,Is.True);
            yield return RecordUpperBridge();
            c.ToggleRecording();yield return CrossUpperBridge();yield return Until(12);yield return Press(p.UpperB);
            yield return Until(29.5f);c.SaveAndRestart();yield return null;
            yield return CrossUpperBridge();yield return Until(13.8f);Assert.That(p.UpperReady,Is.True);
            yield return Capture("UpperPowered");
            yield return Walk(11,-8);yield return Walk(11,7.5f);
            Assert.That(c.Player.Feet.y,Is.GreaterThan(23));
            yield return Walk(12,8.2f);yield return Wait(.2f);
            var direction=(p.FinalNode.transform.position-c.Player.View.position).normalized;
            c.Player.transform.rotation=Quaternion.LookRotation(new Vector3(direction.x,0,direction.z));
            c.Player.View.rotation=Quaternion.LookRotation(direction);
            InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right,true));yield return null;yield return Wait(1.2f);
            InputSystem.QueueStateEvent(mouse,new MouseState());yield return null;
            Assert.That(p.CanExit,Is.True,"Final scan must open exit at "+c.Elapsed+" from "+c.Player.View.position+" target="+c.Scanning.PlayerTarget+" activation="+p.FinalNode.ActivationProgress(0));
            // Walk off-centre around the small phase-node housing.
            yield return Walk(12,8);yield return Walk(12,11);
            Assert.That(c.Game.State,Is.EqualTo(GameState.LevelCompleted));
            Assert.That(c.UsedEchoCount,Is.EqualTo(6));yield return Capture("Complete");
        }
        IEnumerator RecordUpperBridge(float end=29.5f)
        {
            c.ToggleRecording();yield return Walk(-12,-11);yield return Until(3);yield return Press(p.UpperA);
            yield return Until(11);yield return Press(p.UpperA,false);yield return Wait(.1f);yield return Press(p.UpperA);
            yield return Until(end);c.SaveAndRestart();yield return null;
        }
        IEnumerator CrossUpperBridge()
        {
            yield return Until(4.2f);yield return Walk(-6,-11);yield return Walk(11,-11);yield return Walk(11,-10);
        }
        [UnityTest] public IEnumerator UpperUsesVisiblePowerAndExitSurvivesEchoAndPowerExpiry()
        {
            // Isolate the third tier; the complete-route test covers reaching it physically.
            foreach(var m in p.Lower){m.SetCommand(true);m.Tick(10);}Assert.That(p.Checkpoints.TryActivate(1,true),Is.True);
            foreach(var m in p.Middle){m.SetCommand(true);m.Tick(10);}Assert.That(p.Checkpoints.TryActivate(2,true),Is.True);
            p.UpperA.Pulse(0);yield return Wait(1.2f);
            Assert.That(p.Upper[0].AtTarget && p.Upper[1].AtTarget,Is.True);Assert.That(p.Upper[2].AtTarget,Is.False);
            p.UpperB.Pulse(0);yield return Wait(1.6f);Assert.That(p.UpperReady,Is.True,"Both live-player signals are valid without Echo ownership");
            p.UpperA.Pulse(0);yield return Wait(1.6f);Assert.That(p.Upper[2].Progress,Is.Zero);
            c.Restart(false);yield return null;
            yield return RecordUpperBridge(19.5f);yield return CrossUpperBridge();yield return Until(12);yield return Press(p.UpperB);
            yield return Until(13.8f);Assert.That(p.UpperReady,Is.True);
            yield return Walk(11,-8);yield return Walk(11,7.5f);yield return Walk(12,8.2f);
            float arrived=c.Elapsed;yield return Until(23);
            Assert.That(c.Echoes.Echoes[0].IsActive,Is.False);Assert.That(p.UpperPowered,Is.False);
            var direction=(p.FinalNode.transform.position-c.Player.View.position).normalized;
            c.Player.transform.rotation=Quaternion.LookRotation(new Vector3(direction.x,0,direction.z));c.Player.View.rotation=Quaternion.LookRotation(direction);
            InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right,true));yield return null;yield return Wait(1.2f);
            InputSystem.QueueStateEvent(mouse,new MouseState());yield return null;
            Assert.That(p.CanExit,Is.True,"Reachable final node must open the gate after both timers and Echo playback end");
            yield return Capture("UpperExitWithoutPower");yield return Walk(12,11);Assert.That(c.Game.State,Is.EqualTo(GameState.LevelCompleted));
            System.IO.File.WriteAllText("C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-12-Level05-Upper/UpperTiming.txt",$"One Echo + player route: exit island arrival {arrived:0.00}s; A deadline approximately 21.2s; exit scan after power and Echo expiry at 23s.");
        }
        [UnityTest] public IEnumerator OneEchoCannotRaiseFirstRouteAndFallKeepsRecordings()
        {
            yield return RecordPlate(p.PlateA);yield return Walk(-9,-12);yield return Until(6);
            Assert.That(p.PlateA.IsActive && p.PlateB.IsActive,Is.True);
            Assert.That(p.Lower[2].AtTarget,Is.False);Assert.That(p.LowerReady,Is.False);
            var fall=new GameObject("Test fall").transform;fall.position=new Vector3(0,-5,0);c.Player.Teleport(fall);Object.Destroy(fall.gameObject);
            yield return Wait(.1f);Assert.That(p.ActiveSection,Is.Zero);Assert.That(c.Echoes.Echoes.Count,Is.EqualTo(1));Assert.That(c.Player.Feet.y,Is.GreaterThan(-.1f));
        }
        [UnityTest] public IEnumerator CheckpointSnapshotSurvivesRepeatTimeoutAndFallButFullRestartClears()
        {
            Assert.That(p.Checkpoints.TryActivate(1,false),Is.False);
            yield return Lower();
            var snapshot=p.Lower[2].Capture();
            c.Restart(false);yield return null;Assert.That(p.ActiveSection,Is.EqualTo(1));Assert.That(p.Lower[2].Locked,Is.True);
            Assert.That(p.Lower[2].transform.localRotation,Is.EqualTo(snapshot.Rotation));
            var fall=new GameObject("Test fall").transform;fall.position=new Vector3(0,3,0);c.Player.Teleport(fall);Object.Destroy(fall.gameObject);yield return Wait(.1f);
            Assert.That(p.ActiveSection,Is.EqualTo(1));Assert.That(c.Player.Feet.y,Is.GreaterThan(7.9f));
            int cycle=c.CycleNumber;yield return Until(29.5f);yield return Wait(.7f);Assert.That(c.CycleNumber,Is.GreaterThan(cycle));Assert.That(p.Lower[2].Locked,Is.True);
            yield return RecordTerminal(p.MiddleWall,12,11,6);
            Assert.That(c.Echoes.Echoes.Count,Is.EqualTo(1));
            c.ToggleRecording();p.MiddleWall.Pulse(0);yield return Wait(.1f);
            Assert.That(p.Middle[1].IsMoving,Is.True);
            Keys(Key.RightShift,Key.R);yield return null;yield return null;Keys();yield return null;
            Assert.That(p.ActiveSection,Is.EqualTo(1));Assert.That(p.Lower[2].Locked,Is.True);
            Assert.That(p.Lower[2].transform.localRotation,Is.EqualTo(snapshot.Rotation));
            Assert.That(p.Middle[1].Progress,Is.Zero);Assert.That(p.MiddleWall.Source,Is.EqualTo(-1));
            Assert.That(c.Echoes.Echoes.Count,Is.Zero);Assert.That(c.IsRecording,Is.False);
            Assert.That(c.Elapsed,Is.LessThan(1));Assert.That(Vector3.Distance(c.Player.Feet,p.Checkpoints.SectionSpawns[1].position),Is.LessThan(.1f));
            c.Restart(true);yield return null;Assert.That(p.ActiveSection,Is.Zero);Assert.That(p.Lower[2].Locked,Is.False);Assert.That(p.Lower[2].Progress,Is.Zero);
        }
        [UnityTest] public IEnumerator SecondCheckpointAcceptsArrivalWhileRampRetractsAfterEchoesExpire()
        {
            yield return Lower();
            yield return Middle(afterExpiry:true);
        }
        [UnityTest] public IEnumerator TerminalExpiryClosesPartitionAndRepeatClearsSignal()
        {
            yield return Lower();
            yield return Walk(12,11);yield return Press(p.MiddleWall);Assert.That(p.MiddleWall.Source,Is.Zero);
            yield return Wait(.1f);Assert.That(p.Middle[1].IsMoving,Is.True);
            yield return Until(14);Assert.That(p.MiddleWall.IsActive,Is.False);yield return Wait(1.5f);Assert.That(p.Middle[1].Progress,Is.Zero);
            c.Restart(false);Assert.That(p.MiddleWall.Source,Is.EqualTo(-1));Assert.That(p.Middle[1].Progress,Is.Zero);
        }
        [UnityTest] public IEnumerator LeverSequenceOrderToggleExpiryAndReset()
        {
            yield return Lower();
            Assert.That(p.MiddleWall.Countdown.gameObject.activeSelf,Is.False);
            p.LiftFinal.Pulse(0);p.LiftSecond.Pulse(0);
            Assert.That(p.LiftSequenceStep,Is.Zero);Assert.That(p.Middle[2].AtTarget,Is.False);
            p.Lift.Pulse(0);yield return null;
            Assert.That(p.LiftSequenceStep,Is.EqualTo(1));Assert.That(p.Lift.Countdown.gameObject.activeSelf,Is.True);
            Assert.That(p.Lift.Remaining,Is.InRange(9,10));Assert.That(Quaternion.Angle(p.Lift.Handle.localRotation,Quaternion.Euler(-125,0,0)),Is.LessThan(.01f));
            p.Lift.Pulse(0);Assert.That(p.LiftSequenceStep,Is.Zero);Assert.That(p.Lift.Countdown.gameObject.activeSelf,Is.False);
            p.Lift.Pulse(0);yield return Wait(10.2f);
            Assert.That(p.LiftSequenceStep,Is.Zero);Assert.That(p.Lift.IsActive,Is.False);
            Assert.That(Quaternion.Angle(p.Lift.Handle.localRotation,Quaternion.Euler(-55,0,0)),Is.LessThan(.01f));
            // Ownership does not gate the circuit: spatial separation makes an Echo useful.
            p.Lift.Pulse(0);p.LiftSecond.Pulse(0);p.LiftFinal.Pulse(0);yield return Wait(1.6f);
            Assert.That(p.Middle[2].AtTarget,Is.True);Assert.That(p.MiddleBridge.IsActive,Is.False);
            Keys(Key.LeftShift,Key.R);yield return null;yield return null;Keys();yield return null;
            Assert.That(p.ActiveSection,Is.EqualTo(1));Assert.That(p.LiftSequenceStep,Is.Zero);Assert.That(p.Middle[2].Progress,Is.Zero);
        }
        [UnityTest] public IEnumerator ResearchFreezesClockAndCannotCommitProgress()
        {
            p.EnterResearch();yield return Wait(.3f);Assert.That(c.Elapsed,Is.Zero);Assert.That(p.ActiveSection,Is.Zero);
            Assert.That(c.IsRecording,Is.False);Assert.That(p.Upper[2].AtTarget,Is.True);
            Assert.That(p.MiddleWall.CanInteract,Is.False);p.StartCycle();yield return null;
            Assert.That(p.Upper[2].Progress,Is.Zero);Assert.That(p.ActiveSection,Is.Zero);Assert.That(c.Player.Feet.y,Is.LessThan(.1f));
        }
    }
}
