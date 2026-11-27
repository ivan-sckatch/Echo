using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace EchoGame.Editor
{
    public static class EchoPhaseNodeBuilder
    {
        public const string Art="Assets/_ECHO/Art/Modular/PhaseNode01";
        public const string Reports="C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-23-PhaseNodeImplementation";
        static Material paint,metal,dark,cyan,anomaly;
        static Transform Group(Transform parent,string name){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static Material Mat(string name,Color color,Color emission)
        {
            string path=Art+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("ECHO/Phase Surface"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetColor("_EmissionColor",emission);m.SetFloat("_Reveal",1);EditorUtility.SetDirty(m);return m;
        }
        static Renderer MeshPart(Transform parent,string name,Mesh mesh,Vector3 pos,Material mat)
        {
            var t=Group(parent,name);t.localPosition=pos;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;return renderer;
        }
        static Renderer Box(Transform t,string name,Vector3 pos,Vector3 size,Material mat)
            =>MeshPart(t,name,EchoWallModuleBuilder.BeveledBox(name,size,.006f,Art+"/Meshes"),pos,mat);
        static Mesh Prism(string name,Vector2[] outline,float depth)
        {
            string path=Art+"/Meshes/"+name+".asset";var found=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(found)return found;
            var v=new List<Vector3>();var tr=new List<int>();
            Action<Vector3,Vector3,Vector3> tri=(a,b,c)=>{int n=v.Count;v.AddRange(new[]{a,b,c});tr.AddRange(new[]{n,n+1,n+2});};
            for(int i=1;i<outline.Length-1;i++)
            {
                tri(new Vector3(outline[0].x,outline[0].y,-depth/2),new Vector3(outline[i+1].x,outline[i+1].y,-depth/2),new Vector3(outline[i].x,outline[i].y,-depth/2));
                tri(new Vector3(outline[0].x,outline[0].y,depth/2),new Vector3(outline[i].x,outline[i].y,depth/2),new Vector3(outline[i+1].x,outline[i+1].y,depth/2));
            }
            for(int i=0;i<outline.Length;i++)
            {
                var a=outline[i];var b=outline[(i+1)%outline.Length];var af=new Vector3(a.x,a.y,-depth/2);var bf=new Vector3(b.x,b.y,-depth/2);var ab=new Vector3(a.x,a.y,depth/2);var bb=new Vector3(b.x,b.y,depth/2);tri(af,bf,bb);tri(af,bb,ab);
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Vector2 Polar(float radius,float angle)=>new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad))*radius;
        static Vector2[] Circle(float radius,int n)=>Enumerable.Range(0,n).Select(i=>Polar(radius,i*360f/n)).ToArray();
        static Renderer Disc(Transform t,string name,float radius,float depth,Vector3 pos,Material mat,int sides=32)
            =>MeshPart(t,name,Prism(name,Circle(radius,sides),depth),pos,mat);
        static Renderer Bar(Transform parent,string name,Vector3 a,Vector3 b,float width,Material mat)
        {
            var r=Box(parent,name,(a+b)/2,new Vector3((b-a).magnitude,width,.012f),mat);
            r.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);return r;
        }
        static void Build(bool iris)
        {
            string kind=iris?"03_Iris":"04_Coil";var go=new GameObject("ECHO_PhaseNode_"+kind);var root=go.transform;
            var node=go.AddComponent<PhaseNode>();node.StableId="AssignUniqueId";node.InitiallyHidden=true;
            node.Mode=iris?PhaseNodeMode.Impulse:PhaseNodeMode.Continuous;
            var hit=go.AddComponent<BoxCollider>();hit.size=new Vector3(.9f,.9f,.24f);hit.isTrigger=true;
            var presentation=go.AddComponent<PhaseNodePresentation>();node.Presentation=presentation;presentation.Iris=iris;
            var body=Group(root,"Phased hardware");var core=Group(root,"Core");var trace=Group(root,"Anomaly traces");
            if(iris)
            {
                Disc(body,"Hexagonal back",.445f,.065f,new Vector3(0,0,.035f),dark,6);
                for(int i=0;i<6;i++)
                {
                    var outline=new[]{Polar(.335f,i*60),Polar(.445f,i*60),Polar(.445f,(i+1)*60),Polar(.335f,(i+1)*60)};
                    MeshPart(body,"Hex rim sector "+i,Prism("Hex rim sector "+i,outline,.075f),new Vector3(0,0,-.035f),paint);
                    var bolt=Disc(body,"Iris fastener "+i,.012f,.01f,(Vector3)Polar(.39f,i*60+30)+new Vector3(0,0,-.079f),metal,8);
                }
                Disc(core,"Iris emitter",.225f,.026f,new Vector3(0,0,-.018f),cyan);
                Disc(core,"Iris centre",.13f,.028f,new Vector3(0,0,-.037f),cyan,6);
                var pivots=new List<Transform>();
                for(int i=0;i<6;i++)
                {
                    var assembly=Group(body,"Blade assembly "+i);assembly.localRotation=Quaternion.Euler(0,0,i*60);
                    var pivot=Group(assembly,"Iris pivot "+i);pivot.localPosition=new Vector3(.29f,0,-.067f);pivots.Add(pivot);
                    var points=new[]{new Vector2(-.292f,-.006f),new Vector2(.035f,-.006f),new Vector2(-.125f,.29f)};
                    MeshPart(pivot,"Iris blade "+i,Prism("Iris blade",points,.013f),Vector3.zero,i%2==0?metal:dark);
                }
                presentation.IrisPivots=pivots.ToArray();
                for(int i=0;i<6;i++)
                {
                    Vector3 a=Polar(.35f,i*60+5),b=Polar(.35f,i*60+36);a.z=b.z=-.085f;Bar(trace,"Hex ghost "+i,a,b,.003f,anomaly);
                }
            }
            else
            {
                Box(body,"Coil back",new Vector3(0,0,.04f),new Vector3(.54f,.86f,.065f),dark);
                foreach(int side in new[]{-1,1})
                {
                    Box(body,"Coil cap "+side,new Vector3(0,side*.30f,-.045f),new Vector3(.26f,.11f,.15f),metal);
                    Box(body,"Coil cross brace "+side,new Vector3(0,side*.39f,-.02f),new Vector3(.56f,.065f,.09f),metal);
                }
                // Cylinder mesh is extruded on Z and rotated so its axis is vertical.
                var emitter=Disc(core,"Coil emitter",.098f,.5f,new Vector3(0,0,-.055f),cyan);emitter.transform.localRotation=Quaternion.Euler(90,0,0);
                for(int i=0;i<7;i++)
                {
                    var band=Disc(body,"Coil winding "+i,.112f,.012f,new Vector3(0,-.225f+i*.075f,-.055f),metal);band.transform.localRotation=Quaternion.Euler(90,0,0);
                }
                presentation.LeftGuard=Group(body,"Left sliding guard");presentation.RightGuard=Group(body,"Right sliding guard");
                foreach(int side in new[]{-1,1})
                {
                    var guard=side<0?presentation.LeftGuard:presentation.RightGuard;
                    Box(guard,"Guard backing "+side,new Vector3(side*.145f,0,-.125f),new Vector3(.28f,.78f,.07f),metal);
                    Box(guard,"Guard face "+side,new Vector3(side*.145f,0,-.172f),new Vector3(.245f,.72f,.04f),paint);
                    for(int end=-1;end<=1;end+=2)Disc(guard,"Guard screw "+side+" "+end,.013f,.008f,new Vector3(side*.19f,end*.28f,-.198f),metal,8);
                }
                for(int side=-1;side<=1;side+=2)
                {
                    Bar(trace,"Coil ghost side "+side,new Vector3(side*.245f,-.24f,-.21f),new Vector3(side*.245f,.29f,-.21f),.003f,anomaly);
                    Bar(trace,"Coil ghost end "+side,new Vector3(-.12f,side*.37f,-.21f),new Vector3(.13f,side*.37f,-.21f),.003f,anomaly);
                }
            }
            // Small identification strokes share the art set's turquoise accent.
            for(int i=0;i<3;i++)Bar(body,"Identification "+i,new Vector3(.15f+i*.025f,.30f,-.10f),new Vector3(.165f+i*.025f,.32f,-.10f),.006f,cyan);
            presentation.Body=body.GetComponentsInChildren<Renderer>();presentation.Core=core.GetComponentsInChildren<Renderer>();presentation.Anomaly=trace.GetComponentsInChildren<Renderer>();
            // Compatibility renderer keeps existing scanner/UI and tests wired to the node.
            node.Visual=presentation.Core[0];node.DiscoveredMaterial=cyan;node.InactiveMaterial=cyan;node.ActiveMaterial=cyan;node.ResetNode();
            PrefabUtility.SaveAsPrefabAsset(go,Art+"/Prefabs/"+go.name+".prefab");Object.DestroyImmediate(go);
        }
        [MenuItem("Tools/ECHO/Art/Build phase nodes 03 and 04")]
        public static void BuildAssets()
        {
            foreach(string folder in new[]{"Materials","Meshes","Prefabs"})Directory.CreateDirectory(Art+"/"+folder);Directory.CreateDirectory(Reports);AssetDatabase.Refresh();
            paint=Mat("Composite",new Color(.56f,.57f,.53f),Color.black);metal=Mat("Graphite",new Color(.18f,.23f,.24f),Color.black);dark=Mat("Recess",new Color(.065f,.085f,.095f),Color.black);cyan=Mat("Core",new Color(.12f,.76f,.84f),new Color(.1f,.4f,.45f));
            string path=Art+"/Materials/Anomaly.mat";anomaly=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!anomaly){anomaly=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(anomaly,path);}
            anomaly.SetColor("_BaseColor",new Color(.25f,.5f,.55f,.07f));anomaly.SetFloat("_Surface",1);anomaly.SetFloat("_Blend",0);anomaly.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);anomaly.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);anomaly.SetFloat("_ZWrite",0);anomaly.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");anomaly.SetOverrideTag("RenderType","Transparent");anomaly.renderQueue=3000;EditorUtility.SetDirty(anomaly);
            Build(true);Build(false);AssetDatabase.SaveAssets();
        }
        static void Mount(PhaseNode node,bool iris)
        {
            if(node.Presentation)throw new InvalidOperationException("Phase presentation already installed.");
            var old=node.Visual;old.enabled=false;
            // Keep art under the unscaled mechanisms root, independent of the target cube's nonuniform scale.
            string path=Art+"/Prefabs/ECHO_PhaseNode_"+(iris?"03_Iris":"04_Coil")+".prefab";
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            instance.name="Phase art "+node.StableId;instance.transform.SetParent(node.transform.parent,false);
            instance.transform.position=node.transform.position;
            instance.transform.rotation=Quaternion.Euler(0,iris?0:node.transform.position.x<0?-90:90,0);
            var prefabNode=instance.GetComponent<PhaseNode>();node.Presentation=instance.GetComponent<PhaseNodePresentation>();node.Visual=prefabNode.Visual;
            node.DiscoveredMaterial=prefabNode.DiscoveredMaterial;node.InactiveMaterial=prefabNode.InactiveMaterial;node.ActiveMaterial=prefabNode.ActiveMaterial;
            foreach(var collider in instance.GetComponents<Collider>())Object.DestroyImmediate(collider);
            Object.DestroyImmediate(prefabNode);node.ResetNode();EditorUtility.SetDirty(node);
        }
        [MenuItem("Tools/ECHO/Art/Try phase nodes in Level 02")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save current scene first.");
            Directory.CreateDirectory(Reports);File.Copy(Level02Builder.ScenePath,Reports+"/Level02-before-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".unity");
            BuildAssets();var scene=EditorSceneManager.OpenScene(Level02Builder.ScenePath);var puzzle=Object.FindAnyObjectByType<DualPhasePuzzleController>();
            Mount(puzzle.HiddenNode,true);Mount(puzzle.NodeA,false);Mount(puzzle.NodeB,false);
            foreach(string name in new[]{"Hidden device housing","Node A housing","Node B housing"})
            {
                var go=GameObject.Find(name);if(go){foreach(var r in go.GetComponentsInChildren<Renderer>())r.enabled=false;}
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(Reports+"/applied.txt","03 iris: hidden node;04 coil:A,B. Original node IDs, transforms, trigger colliders, timings and puzzle references retained. Old housing renderers disabled; their colliders retained. Trial assets; accepted package unchanged.");
            EditorSceneManager.OpenScene(Level02Builder.ScenePath);
        }
        static Camera camera;
        static int captureIndex;
        static PhaseNode[] captureNodes;
        static readonly string[] stages={"Anomaly","Discovery","Revealed","Opening","Active","Expired"};
        public static void Capture()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Capture requires idle Editor.");
            captureIndex=0;var puzzle=Object.FindAnyObjectByType<DualPhasePuzzleController>();captureNodes=new[]{puzzle.HiddenNode,puzzle.NodeA};
            camera=new GameObject("Phase node review camera").AddComponent<Camera>();camera.CopyFrom(Object.FindAnyObjectByType<TimeCycleManager>().Player.View.GetComponent<Camera>());camera.fieldOfView=42;NextCapture();
        }
        static void NextCapture()
        {
            if(captureIndex>=12){Object.DestroyImmediate(camera.gameObject);EditorSceneManager.OpenScene(Level02Builder.ScenePath);File.WriteAllText(Reports+"/captures-complete.txt",DateTime.UtcNow.ToString("O"));return;}
            int step=captureIndex%6;var node=captureNodes[captureIndex/6];node.ResetNode();
            if(step>0)
            {
                float duration=step==1?.25f:step==2?.55f:step==3?1.5f:2.6f;
                for(float time=0;time<duration-.00001f;time+=.02f){node.BeginStep();node.Submit(0);node.EndStep(Mathf.Min(.02f,duration-time));}
                if(step==5){for(int i=0;i<270;i++){node.BeginStep();node.EndStep(.02f);}}
            }
            var outward=node.Presentation.transform.rotation*Vector3.back;
            camera.transform.position=node.transform.position+outward*1.75f+node.Presentation.transform.right*.15f+Vector3.up*.06f;camera.transform.LookAt(node.transform.position);
            Physics.SyncTransforms();EditorApplication.QueuePlayerLoopUpdate();SceneView.RepaintAll();EditorApplication.delayCall+=SaveCapture;
        }
        static void SaveCapture()
        {
            var rt=new RenderTexture(1000,1000,24);var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1000,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1000,1000),0,0);image.Apply();
            File.WriteAllBytes(Reports+"/"+(captureIndex/6==0?"03-Iris-":"04-Coil-")+stages[captureIndex%6]+".png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);captureIndex++;EditorApplication.delayCall+=NextCapture;
        }
    }
}
