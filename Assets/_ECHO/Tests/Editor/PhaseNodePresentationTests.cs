using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace EchoGame.Tests
{
    public class PhaseNodePresentationTests
    {
        [TestCase("03_Iris")]
        [TestCase("04_Coil")]
        public void RevealActivationSignalLossAndResetFollowSimulation(string kind)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ECHO/Art/Modular/PhaseNode01/Prefabs/ECHO_PhaseNode_"+kind+".prefab");
            Assert.That(asset,Is.Not.Null);var go=Object.Instantiate(asset);
            try
            {
                var node=go.GetComponent<PhaseNode>();var view=node.Presentation;node.ResetNode();
                Assert.That(view.Reveal,Is.Zero);Assert.That(node.Visual.enabled,Is.False);
                Assert.That(view.Anomaly[0].enabled,Is.True,"Anomaly hint must exist before discovery");
                Scan(node,.24f);Assert.That(node.IsDiscovered,Is.False);Assert.That(view.Reveal,Is.GreaterThan(0).And.LessThan(1));
                Scan(node,.30f);Assert.That(node.IsDiscovered,Is.True);Assert.That(node.HasActivated,Is.False);
                Scan(node,.9f);Assert.That(view.Opening,Is.GreaterThan(.3f).And.LessThan(.8f));Assert.That(node.HasActivated,Is.False);
                Scan(node,1.1f);Assert.That(node.HasActivated,Is.True);Assert.That(view.Opening,Is.GreaterThan(.95f));
                Assert.That(node.GetComponent<Collider>().enabled,Is.True);
                for(int i=0;i<280;i++){node.BeginStep();node.EndStep(.02f);}
                Assert.That(node.IsDiscovered,Is.False);Assert.That(view.Reveal,Is.Zero);Assert.That(view.Opening,Is.Zero);
                Scan(node,2.6f);node.ResetNode();Assert.That(view.Reveal,Is.Zero);Assert.That(view.Opening,Is.Zero);
                if(view.Iris)foreach(var pivot in view.IrisPivots)Assert.That(Quaternion.Angle(pivot.localRotation,Quaternion.identity),Is.LessThan(.001f));
                else{Assert.That(view.LeftGuard.localPosition,Is.EqualTo(Vector3.zero));Assert.That(view.RightGuard.localPosition,Is.EqualTo(Vector3.zero));}
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void VisibleNodeNeverGetsAnomalyAndDoesNotHideWhenSignalStops()
        {
            var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ECHO/Art/Modular/PhaseNode01/Prefabs/ECHO_PhaseNode_04_Coil.prefab"));
            try{var node=go.GetComponent<PhaseNode>();node.InitiallyHidden=false;node.ResetNode();Scan(node,2.1f);for(int i=0;i<300;i++){node.BeginStep();node.EndStep(.02f);}Assert.That(node.Presentation.Reveal,Is.EqualTo(1));Assert.That(node.Presentation.Opening,Is.Zero);Assert.That(node.Presentation.Anomaly[0].enabled,Is.False);}
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void IrisLeavesExposeTheCoreInsteadOfRotatingOverIt()
        {
            var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ECHO/Art/Modular/PhaseNode01/Prefabs/ECHO_PhaseNode_03_Iris.prefab"));
            try
            {
                var node=go.GetComponent<PhaseNode>();node.ResetNode();
                Assert.That(Covers(node.Presentation,new Vector2(.04f,.01f)),Is.True,"Closed blades must cover the core");
                Scan(node,2.6f);
                foreach(var point in new[]{Vector2.zero,new Vector2(.04f,.01f),new Vector2(-.04f,-.01f)})
                    Assert.That(Covers(node.Presentation,point),Is.False,"Open iris still covers centre "+point);
            }
            finally{Object.DestroyImmediate(go);}
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        static bool Covers(PhaseNodePresentation view,Vector2 point)
        {
            foreach(var pivot in view.IrisPivots)
            {
                var filter=pivot.GetComponentInChildren<MeshFilter>();var vertices=filter.sharedMesh.vertices;var triangles=filter.sharedMesh.triangles;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    Vector2 a=view.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[triangles[i]]));
                    Vector2 b=view.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[triangles[i+1]]));
                    Vector2 c=view.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[triangles[i+2]]));
                    if(Mathf.Abs(Cross(b-a,c-a))<.000001f)continue;
                    float x=Cross(b-a,point-a),y=Cross(c-b,point-b),z=Cross(a-c,point-c);
                    if((x>=0&&y>=0&&z>=0)||(x<=0&&y<=0&&z<=0))return true;
                }
            }
            return false;
        }
        static void Scan(PhaseNode node,float seconds)
        {
            for(float t=0;t<seconds-.00001f;t+=.02f){node.BeginStep();node.Submit(0);node.EndStep(Mathf.Min(.02f,seconds-t));}
        }
    }
}
