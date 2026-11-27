using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace EchoGame.Editor
{
    public static class EchoDoorModuleBuilder
    {
        public const string Art="Assets/_ECHO/Art/Modular/Door01";
        public const string Reports="C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-21-EchoDoors01";
        static Material paint,metal,dark,mark,glass,closed,open;
        static Material Mat(string name,Color color,float metallic=.1f,float smooth=.28f)
        {
            string path=Art+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
        }
        static Transform Group(string name,Transform t){var go=new GameObject(name);go.transform.SetParent(t,false);return go.transform;}
        static GameObject Part(Transform t,string name,Vector3 p,Vector3 size,Material m,float bevel=.008f)
        {
            var go=new GameObject(name);go.transform.SetParent(t,false);go.transform.localPosition=p;
            go.AddComponent<MeshFilter>().sharedMesh=EchoWallModuleBuilder.BeveledBox(name,size,bevel,Art+"/Meshes");go.AddComponent<MeshRenderer>().sharedMaterial=m;return go;
        }
        static Mesh Polygon(string name,Vector2[] outline)
        {
            string path=Art+"/Meshes/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh)return mesh;
            var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
            Action<Vector3,Vector3,Vector3> tri=(a,b,c)=>{int n=vertices.Count;vertices.AddRange(new[]{a,b,c});triangles.AddRange(new[]{n,n+1,n+2});foreach(var p in new[]{a,b,c})uv.Add(new Vector2(p.x/3+.5f,p.y/3.4f+.5f));};
            for(int i=1;i<outline.Length-1;i++)
            {
                tri(new Vector3(outline[0].x,outline[0].y,-.19f),new Vector3(outline[i+1].x,outline[i+1].y,-.19f),new Vector3(outline[i].x,outline[i].y,-.19f));
                tri(new Vector3(outline[0].x,outline[0].y,.19f),new Vector3(outline[i].x,outline[i].y,.19f),new Vector3(outline[i+1].x,outline[i+1].y,.19f));
            }
            for(int i=0;i<outline.Length;i++)
            {
                var a=outline[i];var b=outline[(i+1)%outline.Length];var af=new Vector3(a.x,a.y,-.19f);var bf=new Vector3(b.x,b.y,-.19f);var ab=new Vector3(a.x,a.y,.19f);var bb=new Vector3(b.x,b.y,.19f);tri(af,bf,bb);tri(af,bb,ab);
            }
            mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Transform Leaf(Transform parent,string name,Vector2[] polygon,Material material)
        {
            var t=Group(name,parent);var mesh=Polygon(name,polygon);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
            var collider=t.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh;collider.convex=true;return t;
        }
        static void Build(string kind)
        {
            var root=new GameObject("ECHO_Door_"+kind+"_300x340");var t=root.transform;var door=root.AddComponent<DoorController>();
            foreach(int side in new[]{-1,1})Part(t,"Frame guide "+side,new Vector3(side*1.57f,1.75f,0),new Vector3(.14f,3.5f,.54f),metal);
            Part(t,"Header",new Vector3(0,3.52f,0),new Vector3(3.28f,.24f,.54f),metal);
            Part(t,"Header recess front",new Vector3(0,3.52f,-.279f),new Vector3(.86f,.085f,.02f),dark);
            Part(t,"Header recess rear",new Vector3(0,3.52f,.279f),new Vector3(.86f,.085f,.02f),dark);
            door.Indicator=Part(t,"Status front",new Vector3(0,3.52f,-.294f),new Vector3(.70f,.042f,.012f),closed).GetComponent<Renderer>();
            door.AdditionalIndicators=new[]{Part(t,"Status rear",new Vector3(0,3.52f,.294f),new Vector3(.70f,.042f,.012f),closed).GetComponent<Renderer>()};door.ClosedMaterial=closed;door.OpenMaterial=open;
            door.Panel=Group("Moving carrier",t);door.Panel.localPosition=Vector3.up*1.7f;
            if(kind=="05_Diagonal")
            {
                door.SplitDiagonally=true;
                door.UpperLeaf=Leaf(door.Panel,"Upper diagonal leaf",new[]{new Vector2(-1.5f,-1.15f),new Vector2(1.5f,.55f),new Vector2(1.5f,1.7f),new Vector2(-1.5f,1.7f)},paint);
                door.LowerLeaf=Leaf(door.Panel,"Lower diagonal leaf",new[]{new Vector2(-1.5f,-1.7f),new Vector2(1.5f,-1.7f),new Vector2(1.5f,.55f),new Vector2(-1.5f,-1.15f)},metal);
                var seam=Part(door.UpperLeaf,"Diagonal edge trim",new Vector3(0,-.30f,-.196f),new Vector3(3.44f,.025f,.012f),dark,.002f);seam.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(1.7f,3)*Mathf.Rad2Deg);
                Part(door.LowerLeaf,"Lower marker",new Vector3(.95f,-1.20f,-.198f),new Vector3(.25f,.045f,.01f),mark,.002f);
            }
            else
            {
                var physical=door.Panel.gameObject.AddComponent<BoxCollider>();physical.size=new Vector3(3,3.4f,.38f);
                if(kind=="03_Window")
                {
                    Part(door.Panel,"Window lower panel",new Vector3(0,-.65f,0),new Vector3(3,2.1f,.38f),paint);
                    Part(door.Panel,"Window upper panel",new Vector3(0,1.35f,0),new Vector3(3,.7f,.38f),paint);
                    foreach(int side in new[]{-1,1})Part(door.Panel,"Window side "+side,new Vector3(side*1.3f,.7f,0),new Vector3(.4f,.6f,.38f),paint);
                    foreach(int side in new[]{-1,1})
                    {
                        Part(door.Panel,"Glazing horizontal frame "+side,new Vector3(0,.7f+side*.27f,0),new Vector3(2.2f,.06f,.31f),metal);
                        Part(door.Panel,"Glazing vertical frame "+side,new Vector3(side*1.065f,.7f,0),new Vector3(.07f,.48f,.31f),metal);
                    }
                    // Two thin panes on either face; solid door collider still blocks scanner rays.
                    Part(door.Panel,"Frosted glazing front",new Vector3(0,.7f,-.145f),new Vector3(2.05f,.45f,.015f),glass,.003f);
                    Part(door.Panel,"Frosted glazing rear",new Vector3(0,.7f,.145f),new Vector3(2.05f,.45f,.015f),glass,.003f);
                }
                else Part(door.Panel,"Plain slab",Vector3.zero,new Vector3(3,3.4f,.38f),paint);
                foreach(int side in new[]{-1,1})
                {
                    Part(door.Panel,"Kick plate "+side,new Vector3(0,-1.50f,side*.199f),new Vector3(2.96f,.38f,.02f),metal);
                    Part(door.Panel,"Identification "+side,new Vector3(.98f,-1.19f,side*.202f),new Vector3(.08f,.08f,.012f),mark,.002f);
                }
            }
            DoorFrameGeometry.FitModular(t);door.ResetMechanism();PrefabUtility.SaveAsPrefabAsset(root,Art+"/Prefabs/"+root.name+".prefab");Object.DestroyImmediate(root);
        }
        [MenuItem("Tools/ECHO/Art/Build door variants 01 03 05")]
        public static void BuildAssets()
        {
            foreach(string folder in new[]{"Materials","Meshes","Prefabs"})Directory.CreateDirectory(Art+"/"+folder);Directory.CreateDirectory(Reports);AssetDatabase.Refresh();
            paint=Mat("Warm composite",new Color(.55f,.56f,.52f));metal=Mat("Graphite metal",new Color(.15f,.19f,.20f),.45f,.32f);dark=Mat("Recess",new Color(.035f,.045f,.05f));mark=Mat("Turquoise",new Color(.16f,.42f,.43f));
            glass=Mat("Frosted glass",new Color(.28f,.39f,.42f,.68f),.05f,.48f);
            glass.SetFloat("_Surface",1);glass.SetFloat("_Blend",0);glass.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);glass.SetFloat("_ZWrite",0);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=3000;
            closed=Mat("Closed status",new Color(.8f,.045f,.025f));open=Mat("Open status",new Color(.08f,.7f,.35f));
            foreach(var m in new[]{closed,open}){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",m.GetColor("_BaseColor")*2);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;EditorUtility.SetDirty(m);}
            foreach(string kind in new[]{"01_Basic","03_Window","05_Diagonal"})Build(kind);AssetDatabase.SaveAssets();
        }
        internal static void Mount(DoorController target,string kind)
        {
            target.Panel.gameObject.SetActive(false);target.Indicator.gameObject.SetActive(false);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Prefabs/ECHO_Door_"+kind+"_300x340.prefab"));instance.transform.SetParent(target.transform,false);
            var source=instance.GetComponent<DoorController>();target.Panel=source.Panel;target.Indicator=source.Indicator;target.AdditionalIndicators=source.AdditionalIndicators;target.ClosedMaterial=source.ClosedMaterial;target.OpenMaterial=source.OpenMaterial;
            target.SplitDiagonally=source.SplitDiagonally;target.UpperLeaf=source.UpperLeaf;target.LowerLeaf=source.LowerLeaf;target.SplitDirection=source.SplitDirection;target.SplitTravel=source.SplitTravel;
            Object.DestroyImmediate(source);target.ResetMechanism();EditorUtility.SetDirty(target);
        }
        [MenuItem("Tools/ECHO/Art/Try window and diagonal doors in Level 02")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;EditorApplication.delayCall+=Apply;return;}
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene first.");
            Directory.CreateDirectory(Reports);File.Copy(Level02Builder.ScenePath,Reports+"/Level_02-before-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".unity");
            var scene=EditorSceneManager.OpenScene(Level02Builder.ScenePath);BuildAssets();var puzzle=Object.FindAnyObjectByType<DualPhasePuzzleController>();
            if(puzzle.FirstDoor.Panel.name=="Moving carrier")throw new InvalidOperationException("Door trial already applied; restore the before-door scene before applying again.");
            Mount(puzzle.FirstDoor,"05_Diagonal");Mount(puzzle.FinalDoor,"03_Window");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(Reports+"/applied.txt",DateTime.UtcNow.ToString("O")+"\nDoor 1:05 Diagonal (opposed convex leaves); Door 2:03 Window (lift); 01 Basic prefab separate. Original puzzle/controller references preserved. Original panel/status objects inactive, new door physics in place.\nSplit travel2.9m along normalized(-.493,.870,0). No accepted package export yet.");
            EditorSceneManager.OpenScene(Level02Builder.ScenePath);EditorApplication.QueuePlayerLoopUpdate();
        }
        public static void Capture()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            Directory.CreateDirectory(Reports);var preview=new GameObject("Door preview set");
            // A temporary facade outside the playable room shows all three assets without adding a third game door.
            foreach(int side in new[]{-1,1})Part(preview.transform,"Preview side "+side,new Vector3(50+side*2.75f,2,0),new Vector3(2.5f,4,.5f),paint??AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/Warm composite.mat"));
            Part(preview.transform,"Preview lintel",new Vector3(50,3.75f,0),new Vector3(3,.70f,.5f),AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/Graphite metal.mat"));
            Part(preview.transform,"Preview floor",new Vector3(50,-.06f,0),new Vector3(8,.1f,8),AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/Graphite metal.mat"));
            Part(preview.transform,"Preview rear wall",new Vector3(50,2,4),new Vector3(8,4,.2f),AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/Warm composite.mat"));
            var lamp=new GameObject("Preview light");lamp.transform.SetParent(preview.transform,false);lamp.transform.position=new Vector3(48,3,-3);var source=lamp.AddComponent<Light>();source.type=LightType.Point;source.range=10;source.intensity=12;
            var go=new GameObject("Door preview camera");go.transform.SetParent(preview.transform,false);var camera=go.AddComponent<Camera>();camera.CopyFrom(Object.FindAnyObjectByType<TimeCycleManager>().Player.View.GetComponent<Camera>());camera.fieldOfView=40;go.transform.position=new Vector3(50,1.8f,-5.5f);go.transform.LookAt(new Vector3(50,1.75f,0));
            Action<string> save=name=>{
                var rt=new RenderTexture(1200,1200,24);var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1200,1200,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,1200),0,0);image.Apply();File.WriteAllBytes(Reports+"/"+name+".png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
            };
            foreach(string kind in new[]{"01_Basic","03_Window","05_Diagonal"})
            {
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Prefabs/ECHO_Door_"+kind+"_300x340.prefab"));instance.transform.position=new Vector3(50,0,0);instance.transform.SetParent(preview.transform,true);var door=instance.GetComponent<DoorController>();door.ResetMechanism();Physics.SyncTransforms();save(kind+"-Closed");
                if(kind=="05_Diagonal"){door.SetCommand(true);door.Tick(.175f,Vector3.zero);Physics.SyncTransforms();save(kind+"-Half");door.Tick(1,Vector3.zero);Physics.SyncTransforms();save(kind+"-Open");}
                Object.DestroyImmediate(instance);
            }
            Object.DestroyImmediate(preview);
            go=new GameObject("Level door review camera");camera=go.AddComponent<Camera>();camera.CopyFrom(Object.FindAnyObjectByType<TimeCycleManager>().Player.View.GetComponent<Camera>());camera.fieldOfView=50;
            var puzzle=Object.FindAnyObjectByType<DualPhasePuzzleController>();int index=0;
            foreach(var door in new[]{puzzle.FirstDoor,puzzle.FinalDoor})
            {
                index++;go.transform.position=door.transform.position+new Vector3(0,1.8f,-5);go.transform.LookAt(door.transform.position+Vector3.up*1.75f);door.ResetMechanism();Physics.SyncTransforms();save("LevelDoor"+index+"-Closed");
                door.SetCommand(true);door.Tick(door.OpenSeconds*.5f,door.transform.position+Vector3.back*5);Physics.SyncTransforms();save("LevelDoor"+index+"-Half");
                door.Tick(1,door.transform.position+Vector3.back*5);Physics.SyncTransforms();save("LevelDoor"+index+"-Open");door.ResetMechanism();
            }
            Object.DestroyImmediate(go);EditorSceneManager.OpenScene(Level02Builder.ScenePath);
        }
    }
}
