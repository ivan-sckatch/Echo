using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EchoGame.Tests
{
    public class DoorModuleTests
    {
        [TestCase("01_Basic")]
        [TestCase("03_Window")]
        [TestCase("05_Diagonal")]
        public void ClosedDoorBlocksAndOpenDoorClearsFullPlayerHeight(string kind)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ECHO/Art/Modular/Door01/Prefabs/ECHO_Door_"+kind+"_300x340.prefab");
            Assert.That(asset,Is.Not.Null);var go=Object.Instantiate(asset);go.transform.position=new Vector3(100,0,0);
            try
            {
                var door=go.GetComponent<DoorController>();door.ResetMechanism();Physics.SyncTransforms();
                foreach(float x in new[]{-.9f,0,.9f})foreach(float y in new[]{.3f,1.65f,2.9f})Assert.That(Blocked(x,y),Is.True,"Closed gap "+kind+" at "+x+","+y);
                door.SetCommand(true);door.Tick(door.OpenSeconds*.5f,new Vector3(100,0,10));door.SetCommand(false);door.Tick(1,new Vector3(100,0,0));
                Assert.That(door.State,Is.EqualTo(DoorState.Open),"Occupant must prevent closing");Physics.SyncTransforms();
                foreach(float x in new[]{-.9f,0,.9f})foreach(float y in new[]{.3f,1.65f,2.9f})Assert.That(Blocked(x,y),Is.False,"Open passage obstructed "+kind);
                door.Tick(1,new Vector3(100,0,10));Assert.That(door.State,Is.EqualTo(DoorState.Closed));Physics.SyncTransforms();Assert.That(Blocked(0,1.65f),Is.True);
                door.SetCommand(true);door.Tick(1,new Vector3(100,0,10));door.ResetMechanism();Physics.SyncTransforms();Assert.That(Blocked(0,1.65f),Is.True);
                Assert.That(door.Indicator.sharedMaterial,Is.EqualTo(door.ClosedMaterial));foreach(var indicator in door.AdditionalIndicators)Assert.That(indicator.sharedMaterial,Is.EqualTo(door.ClosedMaterial));
            }
            finally{Object.DestroyImmediate(go);}
        }
        static bool Blocked(float x,float y)=>Physics.Raycast(new Vector3(100+x,y,-2),Vector3.forward,4,~0,QueryTriggerInteraction.Ignore);
    }
}
