using System.Linq;
using EchoGame.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace EchoGame.Tests
{
    public class DoorFrameGeometryTests
    {
        [TestCase("01_Basic")][TestCase("03_Window")][TestCase("05_Diagonal")]
        public void ImportedDoorFrameHasNoSharedVisiblePlanesWithThePhysicalOpening(string kind)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ECHO/Art/Modular/Door01/Prefabs/ECHO_Door_"+kind+"_300x340.prefab");var go=Object.Instantiate(asset);
            try
            {
                float headerBottom=BoundsInRoot(go.transform.Find("Header").GetComponent<MeshFilter>(),go.transform).min.y;
                Assert.That(3.4f-headerBottom,Is.GreaterThan(.005f));
                foreach(int side in new[]{-1,1})
                {
                    var bounds=BoundsInRoot(go.transform.Find("Frame guide "+side).GetComponent<MeshFilter>(),go.transform);
                    float inner=side<0?-bounds.max.x:bounds.min.x;
                    Assert.That(1.5f-inner,Is.GreaterThan(.005f),"Jamb remains coplanar with wall end/panel side");
                    Assert.That(bounds.max.y,Is.LessThanOrEqualTo(headerBottom+.0001f),"Guide overlaps header face");
                }
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void LegacyArtJambsDoNotOverlapTheirHeader()
        {
            var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ECHO/Art/Level01/Prefabs/DoorFrame.prefab"));
            try
            {
                var header=BoundsInRoot(go.transform.Find("Header").GetComponent<MeshFilter>(),go.transform);
                foreach(var jamb in go.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Jamb"))
                    Assert.That(BoundsInRoot(jamb,go.transform).max.y,Is.LessThanOrEqualTo(header.min.y+.0001f));
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void StructuralRepairMovesVisibleFacesWithoutChangingCollidersAndIsIdempotent()
        {
            var root=new GameObject("Frame test");
            try
            {
                var go=new GameObject("Door");go.transform.SetParent(root.transform);var door=go.AddComponent<DoorController>();
                var panel=GameObject.CreatePrimitive(PrimitiveType.Cube);panel.transform.SetParent(go.transform);panel.transform.localPosition=Vector3.up*1.7f;panel.transform.localScale=new Vector3(3,3.4f,.4f);door.Panel=panel.transform;
                var jamb=GameObject.CreatePrimitive(PrimitiveType.Cube);jamb.transform.SetParent(root.transform);jamb.transform.localPosition=new Vector3(1.95f,1.7f,0);jamb.transform.localScale=new Vector3(.9f,3.4f,.4f);
                var header=GameObject.CreatePrimitive(PrimitiveType.Cube);header.transform.SetParent(root.transform);header.transform.localPosition=new Vector3(0,3.95f,0);header.transform.localScale=new Vector3(3,1.1f,.4f);
                Physics.SyncTransforms();var physicalJamb=jamb.GetComponent<BoxCollider>().bounds;var physicalHeader=header.GetComponent<BoxCollider>().bounds;
                var filters=new[]{jamb.GetComponent<MeshFilter>(),header.GetComponent<MeshFilter>()};
                Assert.That(DoorFrameGeometry.FitStructuralReveals(door,filters),Is.EqualTo(2));
                var visual=BoundsInRoot(filters[0],root.transform);var top=BoundsInRoot(filters[1],root.transform);
                Assert.That(1.5f-visual.min.x,Is.GreaterThan(.005f));Assert.That(3.4f-top.min.y,Is.GreaterThan(.005f));Assert.That(visual.max.y,Is.LessThanOrEqualTo(top.min.y+.0001f));
                Assert.That(jamb.GetComponent<BoxCollider>().bounds,Is.EqualTo(physicalJamb));Assert.That(header.GetComponent<BoxCollider>().bounds,Is.EqualTo(physicalHeader));
                Assert.That(DoorFrameGeometry.FitStructuralReveals(door,filters),Is.Zero);
            }
            finally{Object.DestroyImmediate(root);}
        }
        static Bounds BoundsInRoot(MeshFilter filter,Transform root)
        {
            var vertices=filter.sharedMesh.vertices;var bounds=new Bounds(root.InverseTransformPoint(filter.transform.TransformPoint(vertices[0])),Vector3.zero);
            foreach(var vertex in vertices)bounds.Encapsulate(root.InverseTransformPoint(filter.transform.TransformPoint(vertex)));return bounds;
        }
    }
}
