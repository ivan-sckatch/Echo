using System.Collections;
using EchoGame.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoGame.Tests
{
    public class CameraVisionFloorTests
    {
        [UnityTest] public IEnumerator FloorZonesRespectWallsAndFocusOnCapturedPlayerAndEcho()
        {
            yield return SceneManager.LoadSceneAsync("Level_04"); yield return null;
            var c=Object.FindAnyObjectByType<TimeCycleManager>(); var p=c.SecurityPuzzle;
            foreach(var camera in p.Cameras)
            {
                var floor=camera.GetComponent<CameraVisionFloor>(); Assert.That(floor,Is.Not.Null);floor.Rebuild();
                Assert.That(floor.Surface.vertexCount,Is.GreaterThan(20));Assert.That(floor.Focused,Is.False);
                Assert.That(camera.Cone.enabled,Is.False);
                foreach(var v in floor.Surface.vertices)
                    Assert.That(v.y,Is.GreaterThan(c.FallResetHeight));
            }
            var first=p.Cameras[0];var zone=first.GetComponent<CameraVisionFloor>();
            c.Player.transform.rotation=Quaternion.identity;
            yield return Level02Tests.CaptureWorld(Object.FindAnyObjectByType<UIManager>(),"L04_CameraFloorResearch");
            int survey=zone.Surface.vertexCount;
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=new Vector3(0,1.5f,7);blocker.transform.localScale=new Vector3(5,3,.4f);Physics.SyncTransforms();
            zone.Rebuild();Assert.That(zone.Surface.vertexCount,Is.LessThan(survey));Object.Destroy(blocker);yield return null;
            p.StartCycle();
            var marker=new GameObject("Capture placement");marker.transform.position=p.BaitA.position;c.Player.Teleport(marker.transform);Object.Destroy(marker);Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();yield return null;
            Assert.That(first.Target.Player,Is.SameAs(c.Player));zone.Rebuild();Assert.That(zone.Focused,Is.True);
            var target=first.Target.transform.position;var origin=first.Head.position;var direction=new Vector3(target.x-origin.x,0,target.z-origin.z).normalized;
            foreach(var v in zone.Surface.vertices)
            {
                var delta=new Vector3(v.x-origin.x,0,v.z-origin.z);
                Assert.That(Vector3.Cross(direction,delta).magnitude,Is.LessThan(.23f),"Lock footprint must point only at its target");
                Assert.That(Vector3.Dot(delta,direction),Is.LessThan(Vector3.Distance(new Vector3(origin.x,0,origin.z),new Vector3(target.x,0,target.z))+.1f));
            }
            yield return Level02Tests.CaptureWorld(Object.FindAnyObjectByType<UIManager>(),"L04_CameraFloorPlayerLock");
            c.Restart(false);zone.Rebuild();Assert.That(zone.Focused,Is.False);Assert.That(zone.Surface.vertexCount,Is.GreaterThan(20));
            c.Echoes.Add(new EchoRecording(new System.Collections.Generic.List<EchoFrame>{new EchoFrame(0,p.BaitA.position,0,0),new EchoFrame(2,p.BaitA.position,0,0)}));
            yield return new WaitForFixedUpdate();yield return null;
            Assert.That(first.Target.Echo,Is.Not.Null);zone.Rebuild();Assert.That(zone.Focused,Is.True);
            yield return Level02Tests.CaptureWorld(Object.FindAnyObjectByType<UIManager>(),"L04_CameraFloorEchoLock");
            p.EnterResearch();zone.Rebuild();Assert.That(zone.Focused,Is.False);Assert.That(c.Elapsed,Is.Zero);
        }
    }
}

