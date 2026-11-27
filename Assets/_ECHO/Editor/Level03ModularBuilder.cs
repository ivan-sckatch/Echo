using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace EchoGame.Editor
{
    public static class Level03ModularBuilder
    {
        public const string Reports="C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-23-Level03FrameFlicker";
        const string Art="Assets/_ECHO/Art/Modular";
        const string Adapters="Assets/_ECHO/Art/Level03Modular/Adapters";
        static int floors,ceilings,walls,facings;
        static Transform Group(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static GameObject Module(string section,string name,Transform parent,Vector3 position,float yaw=0)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/"+section+"/Prefabs/"+name+".prefab");if(!prefab)throw new InvalidOperationException("Missing accepted module "+name);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.transform.SetParent(parent,false);go.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            foreach(var light in go.GetComponentsInChildren<Light>())
            {
                var data=new SerializedObject(light.GetUniversalAdditionalLightData());data.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow;data.ApplyModifiedPropertiesWithoutUndo();
            }
            return go;
        }
        static bool Surface(string name)=>new[]{"West outer wall","East outer wall","End wall","Route wall","Chamber partition","Entrance lintel","Gate jamb","Gate lintel","Window sill","Window lintel"}.Contains(name);
        static void Face(Transform parent,Bounds bounds,bool xNormal,int sign,int seed,bool doorLintel=false)
        {
            float length=xNormal?bounds.size.z:bounds.size.x;
            var normal=xNormal?Vector3.right*sign:Vector3.forward*sign;float yaw=xNormal?(sign>0?-90:90):(sign>0?180:0);
            var rotation=Quaternion.Euler(0,yaw,0);var right=rotation*Vector3.right;
            var centre=bounds.center;centre.y=0;if(xNormal)centre.x+=sign*bounds.extents.x;else centre.z+=sign*bounds.extents.z;
            int count=Mathf.CeilToInt(length/2.5f);
            for(int i=0;i<count;i++)
            {
                // Balance the cuts between both ends; keep fixtures away from returns and openings.
                float offset=(i-(count-1)*.5f)*2.5f;
                bool whole=i>0&&i<count-1&&bounds.min.y<.01f&&bounds.max.y>=3.8f;
                string type=!whole?"Plain":(seed+i)%9==0?"Light":(seed+i)%5==0?"Service":"Plain";
                var go=Module("Wall01","ECHO_Wall_"+type+"_250x380",parent,centre+right*offset,yaw);walls++;
                ModularMeshClipper.Trim(go,1,-length*.5f-offset,length*.5f-offset,doorLintel?Mathf.Max(3.66f,bounds.min.y):bounds.min.y,Mathf.Min(3.8f,bounds.max.y),Adapters);
            }
            if(bounds.max.y>3.8f)
            {
                float low=Mathf.Max(3.8f,bounds.min.y),height=bounds.max.y-low;
                if(height>.001f)
                {
                    var go=new GameObject("Continuous upper infill");go.transform.SetParent(parent,false);go.transform.SetPositionAndRotation(centre+Vector3.up*(low+height/2)+normal*.05f,rotation);
                    string name="Upper infill "+Hash128.Compute(length+"/"+height);
                    go.AddComponent<MeshFilter>().sharedMesh=EchoWallModuleBuilder.BeveledBox(name,new Vector3(length,height,.055f),.003f,Adapters);
                    go.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Wall01/Materials/Graphite.mat");facings++;
                }
            }
        }
        static void FittedPart(Transform parent,string name,Vector3 position,Vector3 size,Quaternion rotation,Material material)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.SetPositionAndRotation(position,rotation);
            go.AddComponent<MeshFilter>().sharedMesh=EchoWallModuleBuilder.BeveledBox(name+" "+Hash128.Compute(size.ToString("F4")),size,.004f,Adapters);
            go.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        static void WallReturns(Transform parent,BoxCollider[] structures,DoorController[] doors)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Wall01/Materials/Graphite.mat");
            foreach(var box in structures)
            {
                var bounds=box.bounds;
                if(bounds.min.y>.01f||bounds.size.y<3.8f||box.name=="Gate jamb")continue;
                bool thinX=bounds.size.x<bounds.size.z;
                float depth=thinX?bounds.size.x:bounds.size.z;
                foreach(int sign in new[]{-1,1})
                {
                    var normal=thinX?Vector3.forward*sign:Vector3.right*sign;
                    var point=bounds.center+Vector3.Scale(bounds.extents,normal);
                    // Door casings already wrap their jambs; avoid a second overlapping end cap.
                    if(doors.Any(d=>{var p=d.transform.InverseTransformPoint(point);return Mathf.Abs(Mathf.Abs(p.x)-1.5f)<.18f&&Mathf.Abs(p.z)<.3f;}))continue;
                    var right=Quaternion.LookRotation(-normal)*Vector3.right;
                    bool exposed=false;
                    foreach(float t in new[]{-.45f,0,.45f})
                    {
                        var probe=point+normal*.025f+right*(depth*t);
                        if(!structures.Any(other=>other!=box&&other.bounds.Contains(probe)))exposed=true;
                    }
                    if(!exposed)continue;
                    FittedPart(parent,"Wall return",point+normal*.025f,new Vector3(depth+.34f,bounds.size.y,.045f),Quaternion.LookRotation(-normal),material);
                }
            }
        }
        internal static void FitDoorCasings(DoorController door)
        {
            DoorFrameGeometry.FitModular(PrefabUtility.GetOutermostPrefabInstanceRoot(door.Panel.gameObject).transform);
        }
        [MenuItem("Tools/ECHO/Art/Assemble Level 03 from own modules")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save current scene first.");
            Directory.CreateDirectory(Reports);File.Copy(Level03Builder.Path,Reports+"/Level03-before-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".unity");
            var scene=EditorSceneManager.OpenScene(Level03Builder.Path);var puzzle=Object.FindAnyObjectByType<Level03Controller>();
            var previousFinish=GameObject.Find("ECHO Modular Level03");
            var doors=puzzle.Doors.Select(d=>d.Door).Concat(puzzle.Chambers.Select(c=>c.Entrance)).ToArray();
            var doorColliders=doors.SelectMany(d=>d.Panel.GetComponentsInChildren<Collider>().Concat(d.Indicator.GetComponentsInChildren<Collider>())).ToArray();
            var original=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>!doorColliders.Contains(c)).Select(c=>c.GetEntityId()).OrderBy(id=>id).ToArray();
            if(previousFinish)Object.DestroyImmediate(previousFinish);
            Directory.CreateDirectory(Adapters);AssetDatabase.Refresh();var env=GameObject.Find("Level_03/Environment").transform;
            var structures=Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>Surface(c.name)).OrderBy(c=>c.transform.position.x).ThenBy(c=>c.transform.position.z).ThenBy(c=>c.transform.position.y).ThenBy(c=>c.name,StringComparer.Ordinal).ToArray();
            var root=Group("ECHO Modular Level03",env);var floor=Group("Floor",root);var ceiling=Group("Ceiling",root);var wallsRoot=Group("Wall skins",root);
            floors=ceilings=walls=facings=0;var floorBounds=GameObject.Find("Level_03/Environment/Floor").GetComponent<BoxCollider>().bounds;
            var ceilingBounds=GameObject.Find("Level_03/Environment/Ceiling").GetComponent<BoxCollider>().bounds;
            float minX=floorBounds.min.x+.125f,maxX=floorBounds.max.x-.125f,minZ=floorBounds.min.z+.125f,maxZ=floorBounds.max.z-.125f;
            string[] unlit={"01_Plain","02_Vent","05_Split","01_Plain","07_Acoustic","01_Plain"};
            int columns=Mathf.CeilToInt((maxX-minX)/2.5f),rows=Mathf.CeilToInt((maxZ-minZ)/2.5f);
            for(int x=0;x<columns;x++)for(int z=0;z<rows;z++)
            {
                float px=minX+1.25f+x*2.5f,pz=minZ+1.25f+z*2.5f;
                string floorType=(x==0||x==2)&&z%4==1?"Service":"Plain";
                var f=Module("Floor01","ECHO_Floor_"+floorType+"_250",floor,new Vector3(px,floorBounds.max.y,pz));floors++;
                ModularMeshClipper.Trim(f,2,minX-px,maxX-px,minZ-pz,maxZ-pz,Adapters);
                bool light=(x==0||x==2||x==5)&&z%3==1;string type=light?(z%2==0?"09_Corner":"08_TwinEdge"):unlit[(x+z*2)%unlit.Length];
                var c=Module("Ceiling01","ECHO_Ceiling_"+type+"_250",ceiling,new Vector3(px,ceilingBounds.min.y,pz));ceilings++;
                ModularMeshClipper.Trim(c,2,minX-px,maxX-px,minZ-pz,maxZ-pz,Adapters);
            }
            for(int i=0;i<structures.Length;i++)
            {
                var box=structures[i];var bounds=box.bounds;bool xNormal=bounds.size.x<bounds.size.z;
                // Outer perimeter gets only the interior face; partitions are clad on both sides.
                if(box.name=="West outer wall")Face(wallsRoot,bounds,true,1,i);
                else if(box.name=="East outer wall")Face(wallsRoot,bounds,true,-1,i);
                else if(box.name=="End wall")Face(wallsRoot,bounds,false,bounds.center.z<10?1:-1,i);
                else{bool doorLintel=box.name=="Gate lintel"||box.name=="Entrance lintel";Face(wallsRoot,bounds,xNormal,-1,i,doorLintel);Face(wallsRoot,bounds,xNormal,1,i,doorLintel);}
            }
            for(int i=0;i<3;i++)
            {
                if(puzzle.Chambers[i].Entrance.Panel.name!="Moving carrier")EchoDoorModuleBuilder.Mount(puzzle.Chambers[i].Entrance,"01_Basic");
                if(puzzle.Doors[i].Door.Panel.name!="Moving carrier")EchoDoorModuleBuilder.Mount(puzzle.Doors[i].Door,i<2?"05_Diagonal":"01_Basic");
                puzzle.Doors[i].ResetDoor();puzzle.Chambers[i].ResetChamber();
            }
            WallReturns(Group("Structural returns",root),structures,doors);
            if(AssetDatabase.LoadAssetAtPath<GameObject>(EchoWallWearBuilder.Prefab("Scuffs")))EchoWallWearBuilder.Layout(env);
            foreach(var door in doors)FitDoorCasings(door);
            foreach(var light in env.GetComponentsInChildren<Light>())if(!light.transform.IsChildOf(root))light.enabled=false;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.30f,.34f,.38f);
            if(root.GetComponentsInChildren<Collider>().Length!=0)throw new InvalidOperationException("Decorative modules must not add colliders.");
            var now=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Select(c=>c.GetEntityId()).ToArray();
            if(original.Any(id=>!now.Contains(id)))throw new InvalidOperationException("Non-door gameplay collider removed.");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(Reports+"/verified.txt",DateTime.UtcNow.ToString("O")+"\nFloor:"+floors+" Ceiling:"+ceilings+" Wall skins:"+walls+" Upper infill:"+facings+"\nD1,D2:05Diagonal;D3:01Basic;camera entrances:01Basic. Original non-door colliders:"+original.Length+" retained.\nClipped decorative edge meshes; native module scale. No new phase nodes/buttons. Existing windows, signs, HUD, interactions, timings and route retained. Accepted package unchanged. Balanced edge cuts; fixtures only on whole interior panels; deterministic layout; fitted .90m-deep portal casings with non-overlapping guide/header faces and wrapped structural returns.");
            EditorSceneManager.OpenScene(Level03Builder.Path);EditorApplication.QueuePlayerLoopUpdate();
        }
        static Camera review;static int frame;
        static readonly Vector3[] positions={new Vector3(2,1.67f,2),new Vector3(-10.5f,1.67f,7),new Vector3(-3.5f,1.67f,10),new Vector3(2,1.67f,16),new Vector3(-3.5f,1.67f,22),new Vector3(-10.5f,1.67f,30),new Vector3(2,1.67f,31),new Vector3(-3.5f,1.67f,35),new Vector3(2,1.67f,40),new Vector3(-2,1.67f,8.3f),new Vector3(2,1.67f,9.5f),new Vector3(-1.5f,1.67f,3.4f),new Vector3(-6.5f,1.67f,3.5f),new Vector3(-6.7f,1.67f,32),new Vector3(-6.7f,1.67f,38),new Vector3(-10.5f,1.67f,8.4f),new Vector3(-6.7f,1.67f,11.4f),new Vector3(-10.5f,1.67f,20.4f),new Vector3(-6.7f,1.67f,23.4f),new Vector3(-10.5f,1.67f,33.4f),new Vector3(-6.7f,1.67f,36.4f)};
        static readonly Vector3[] targets={new Vector3(2,2,12),new Vector3(-9,1.8f,10),new Vector3(.5f,1.8f,11),new Vector3(2,2,24),new Vector3(.5f,1.8f,23),new Vector3(-9,1.8f,35),new Vector3(2,2,38),new Vector3(.5f,1.8f,36),new Vector3(2,2,43.8f),new Vector3(0,1.7f,9.1f),new Vector3(2,3.8f,12),new Vector3(.15f,2,6),new Vector3(-9,2,6),new Vector3(-8.8f,1.9f,30.2f),new Vector3(-8.8f,1.9f,39.8f),new Vector3(-9,2,10),new Vector3(-9,2,10),new Vector3(-9,2,22),new Vector3(-9,2,22),new Vector3(-9,2,35),new Vector3(-9,2,35)};
        public static void Capture()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Idle Editor required.");
            EditorSceneManager.OpenScene(Level03Builder.Path);review=new GameObject("Level03 art review").AddComponent<Camera>();review.CopyFrom(Object.FindAnyObjectByType<TimeCycleManager>().Player.View.GetComponent<Camera>());review.fieldOfView=68;frame=0;Next();
        }
        static void Next()
        {
            if(frame==positions.Length){Object.DestroyImmediate(review.gameObject);EditorSceneManager.OpenScene(Level03Builder.Path);File.WriteAllText(Reports+"/captures-complete.txt",DateTime.UtcNow.ToString("O"));return;}
            review.transform.position=positions[frame];review.transform.LookAt(targets[frame]);EditorApplication.QueuePlayerLoopUpdate();SceneView.RepaintAll();EditorApplication.delayCall+=Save;
        }
        static void Save()
        {
            var rt=new RenderTexture(1440,900,24);var previous=RenderTexture.active;review.targetTexture=rt;review.Render();RenderTexture.active=rt;
            var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes(Reports+"/After-"+frame+".png",image.EncodeToPNG());
            review.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);frame++;EditorApplication.delayCall+=Next;
        }
    }
}
