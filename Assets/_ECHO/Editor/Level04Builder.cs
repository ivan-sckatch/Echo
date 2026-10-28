using System.Collections.Generic;
using EchoGame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using static EchoGame.Editor.Level02Builder;
namespace EchoGame.Editor
{
    public static class Level04Builder
    {
        public const string Path = "Assets/_ECHO/Scenes/Levels/Level_04.unity";
        public const string Reports = "C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-10-28-Level04";
        [MenuItem("Tools/ECHO/Build Level 04")] public static void Build() => BuildScene(true);
        public static void BuildBatch() => BuildScene(false);
        public static void BuildScene(bool ask)
        {
            if (EditorApplication.isPlaying) return;
            if(ask && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var current = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!ask && current.isDirty)
            {
                System.IO.Directory.CreateDirectory(Reports);
                EditorSceneManager.SaveScene(current, Reports + "/UnsavedSceneBeforeLevel04_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity", true);
            }
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_ECHO/UI/Fonts/ECHO UI SDF.asset");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=Node("Level_04"); var env=Node("Atrium",root); var logic=Node("LevelLogic",root); var devices=Node("Mechanisms",root);
            var floor=Existing("Floor"); var wall=Existing("Walls"); var red=Existing("Locked"); var green=Existing("Active");
            var backing=Mat("Technical sign"); var amber=Mat("Idle node");
            foreach(float x in new[]{-7.5f,7.5f}) Cube("Atrium outer wall",env,new Vector3(x,4,24),new Vector3(.3f,8,48),wall);
            foreach(float z in new[]{0f,48f}) Cube("Atrium end wall",env,new Vector3(0,4,z),new Vector3(15,8,.3f),wall);
            // The lower corridor has no connection to the upper landing except the staircase.
            Cube("Lower corridor",env,new Vector3(0,-.25f,9),new Vector3(4.5f,.5f,18),floor);
            Cube("Lower ceiling",env,new Vector3(0,3.05f,6),new Vector3(4.5f,.2f,12),wall);
            foreach(float x in new[]{-2.4f,2.4f}) Cube("Lower side wall",env,new Vector3(x,1.5f,9),new Vector3(.3f,3,18),wall);
            Cube("Start wall",env,new Vector3(0,1.5f,0),new Vector3(5,3,.3f),wall);
            Cube("Stair foundation",env,new Vector3(0,1.4f,18),new Vector3(4.5f,2.8f,.3f),wall);
            for(int i=0;i<15;i++) Cube("Stair "+i,env,new Vector3(0,(i+1)*.1f,12.2f+i*.4f),new Vector3(4.5f,(i+1)*.2f,.4f),floor);
            Slab("Upper approach",env,0,18,24,4.5f,floor);
            Slab("Bait balcony",env,-3.2f,19,23.6f,2, floor);
            Slab("Upper landing",env,0,26,33,4.5f,floor);
            Slab("Final fork",env,0,33,47,4.5f,floor);
            Slab("Node room",env,-4.6f,36,42,4.4f,floor);
            Slab("Exit shelter",env,3.7f,37,40,3.1f,floor);
            // Rails bound both ends of the jump; no side bridge or lower shortcut.
            foreach(float x in new[]{-2.3f,2.3f})
            {
                Rail(env,x,23.6f,33,wall); Rail(env,x,18,19,wall);
            }
            Rail(env,2.3f,19,23.6f,wall); Rail(env,-4.3f,19,23.6f,wall);
            Cube("Balcony end",env,new Vector3(-3.3f,3.65f,23.6f),new Vector3(2,1.3f,.15f),wall);
            Cube("Pit bottom",env,new Vector3(0,-3.7f,25),new Vector3(7,.4f,8),floor);
            // D1 fills the full route width; its side walls also screen K3 from the landing.
            foreach(float x in new[]{-1.95f,1.95f}) Cube("D1 jamb",env,new Vector3(x,4.7f,32),new Vector3(.9f,3.4f,.4f),wall);
            Cube("D1 lintel",env,new Vector3(0,7,32),new Vector3(4.5f,1.2f,.4f),wall);
            // Opaque doglegs allow C to end without exposing either safe position to K3.
            Wall(env,-2.4f,33,37,wall); Wall(env,-2.4f,39,47,wall);
            Wall(env,-6.9f,36,42,wall);
            Cube("Room rear",env,new Vector3(-4.6f,4.8f,42),new Vector3(4.6f,3.6f,.2f),wall);
            Cube("Room front",env,new Vector3(-4.6f,4.8f,36),new Vector3(4.6f,3.6f,.2f),wall);
            Wall(env,2.4f,33,37,wall); Wall(env,2.4f,39,47,wall);
            Wall(env,5.3f,37,40,wall);
            Cube("Shelter rear",env,new Vector3(3.8f,4.8f,40),new Vector3(3,3.6f,.25f),wall);
            Cube("Atrium roof",env,new Vector3(0,7.8f,24),new Vector3(15,.3f,48),wall);
            foreach(float x in new[]{-1.95f,1.95f}) Cube("Exit jamb",env,new Vector3(x,4.7f,44.5f),new Vector3(.9f,3.4f,.4f),wall);
            Cube("Exit rear",env,new Vector3(0,4.8f,47),new Vector3(4.8f,3.6f,.25f),wall);
            var spawn=Node("PlayerSpawn",root).transform; spawn.position=new Vector3(0,.02f,1.5f);
            var po=Node("Player",root);po.transform.position=spawn.position;
            var cc=po.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.32f;cc.center=Vector3.up*.9f;cc.stepOffset=.25f;cc.skinWidth=.025f;
            var player=po.AddComponent<PlayerController>();
            var view=Node("First Person Camera",po).AddComponent<Camera>();view.tag="MainCamera";view.transform.localPosition=Vector3.up*1.65f;view.nearClipPlane=.06f;view.farClipPlane=100;view.fieldOfView=75;view.gameObject.AddComponent<AudioListener>();player.View=view.transform;
            var scanner=po.AddComponent<TemporalScanner>();scanner.View=view.transform;player.Scanner=scanner;
            po.AddComponent<CaptureTarget>().Player=player;
            var game=Node("GameManager",logic).AddComponent<GameManager>();var c=Node("TimeCycleManager",logic).AddComponent<TimeCycleManager>();
            var echoes=Node("EchoManager",logic).AddComponent<EchoManager>();echoes.MaximumEchoes=1;echoes.ConcurrentIntervals=true;echoes.ShareSingleSlot=true;echoes.Container=Node("EchoContainer",root).transform;
            echoes.Prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ECHO/Prefabs/Echoes/Echo.prefab").GetComponent<EchoPlayback>();
            var p=Node("Level04Controller",logic).AddComponent<Level04Controller>();p.Cycles=c;c.SecurityPuzzle=p;
            c.Game=game;game.Cycles=c;c.Player=player;c.Spawn=spawn;c.Echoes=echoes;c.Recorder=po.AddComponent<EchoRecorder>();c.Duration=30;c.AutoStartRecording=false;c.LevelName="04 · Ложная цель";
            p.FirstDoor=Gate("Door_D1",devices,new Vector3(0,3,32),red,green);p.ExitDoor=Gate("ExitDoor",devices,new Vector3(0,3,44.5f),red,green);
            p.FirstNode=Phase("Node_U1",devices,new Vector3(1.98f,4.65f,29.8f));p.FirstNode.transform.rotation=Quaternion.Euler(0,90,0);p.ExitNode=Phase("Node_U2",devices,new Vector3(-5,4.65f,40.8f));
            Cube("U1 mounting panel",devices,new Vector3(2.22f,4.6f,29.8f),new Vector3(.12f,2.7f,1.3f),wall);
            Cube("U2 mounting panel",devices,new Vector3(-5,4.6f,41),new Vector3(1.3f,2.7f,.12f),wall);
            var simulation=Node("ScanSimulation",logic).AddComponent<ScanSimulation>();simulation.Game=game;simulation.Cycles=c;simulation.PlayerScanner=scanner;simulation.Nodes=new[]{p.FirstNode,p.ExitNode};simulation.SynchronizationHints=false;
            simulation.PlayerBeamMaterial=Mat("Player scan beam");simulation.EchoBeamMaterial=Mat("Echo scan beam");c.Scanning=simulation;
            p.Cameras=new[]{CameraDevice("К1",devices,new Vector3(0,2.7f,10),new Vector3(0,1,5),7.5f,80,backing),
                CameraDevice("К2",devices,new Vector3(1,5.8f,29),new Vector3(-.5f,4,23),9.4f,100,backing),
                CameraDevice("К3",devices,new Vector3(0,5.8f,43),new Vector3(0,4,37),9,100,backing)};
            p.BaitA=Marker("Bait_A",root,new Vector3(-.7f,.02f,4));p.BaitB=Marker("Bait_B",root,new Vector3(-3,3.02f,21));p.BaitC=Marker("Bait_C",root,new Vector3(0,3.02f,35.5f));
            p.RoomSafe=Marker("Node room safe position",root,new Vector3(-4.6f,3.02f,38));p.ExitSafe=Marker("Exit waiting niche",root,new Vector3(3.7f,3.02f,38));
            p.JumpStart=Marker("Jump takeoff",root,new Vector3(0,3.02f,23.5f));p.JumpEnd=Marker("Jump landing",root,new Vector3(0,3.02f,26.7f));
            var exit=Node("LevelExit",devices).AddComponent<LevelCompletion>();exit.transform.position=new Vector3(0,3,46);exit.HalfExtents=new Vector3(1.4f,1.3f,.65f);c.Exit=exit;
            Cube("Exit pad",devices,new Vector3(0,3.025f,46),new Vector3(3,.05f,1),green);
            Sign("Trial title",env,new Vector3(0,2,.2f),"Испытание 04.\nЛожная цель",new Vector2(3.8f,1.1f),2.7f,UITheme.Text,backing,180);
            Sign("Past hint",env,new Vector3(-2.2f,1.8f,2.7f),"Камеры не отличают тебя\nот твоего прошлого",new Vector2(4.6f,1.2f),2.5f,UITheme.Text,backing,-90);
            Sign("Node U1 label",env,new Vector3(2.1f,5.5f,29.8f),"У1 → Д1",new Vector2(1.6f,.45f),1.8f,UITheme.Cyan,backing,90);
            Sign("Node U2 label",env,new Vector3(-5,5.5f,40.9f),"У2 → ВЫХОД",new Vector2(2,.45f),1.8f,UITheme.Cyan,backing);
            Sign("Exit label",env,new Vector3(0,5.6f,46.8f),"ВЫХОД",new Vector2(3,.6f),3,UITheme.Cyan,backing);
            // Rail tops and landing markers communicate the required jump without giving a solution.
            foreach(float z in new[]{23.85f,26.15f}) Cube("Jump edge",env,new Vector3(0,3.035f,z),new Vector3(4.3f,.07f,.12f),amber);
            var interaction=po.AddComponent<PlayerInteraction>();interaction.Game=game;interaction.View=view;game.Interaction=interaction;
            c.UI=EchoUIBuilder.Install(game,c);c.UI.GetComponent<UIEventBridge>().ContextSource=p;
            var hud=root.GetComponentInChildren<HUDController>(true);var control=Node("Level04 controls");control.transform.SetParent(hud.transform,false);
            var text=control.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=21;text.color=UITheme.Text;text.alignment=TextAlignmentOptions.BottomLeft;
            var rt=text.rectTransform;rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.zero;rt.anchoredPosition=new Vector2(38,110);rt.sizeDelta=new Vector2(1350,90);p.Controls=text;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.64f,.69f);
            foreach(float z in new[]{3f,10,18,23,29,36,43}) { var light=Node("Atrium light",env).AddComponent<Light>();light.type=LightType.Point;light.range=12;light.intensity=2.5f;light.transform.position=new Vector3(0,z<12?2.7f:7,z); }
            ResearchInspectionBuilder.Clean(scene);
            EditorSceneManager.SaveScene(scene,Path);var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);scenes.RemoveAll(s=>s.path==Path);scenes.Add(new EditorBuildSettingsScene(Path,true));EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();
            System.IO.Directory.CreateDirectory(Reports+"/Captures");Debug.Log("ECHO: Level04 saved. Three cameras, four fragments, two nodes, required jump.");
        }
        static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_ECHO/Materials/Scanner/"+name+".mat");
        public static void PreviewStart()
        {
            if(EditorApplication.isPlaying) return;
            EditorSceneManager.OpenScene(Path);
            foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None)) text.ForceMeshUpdate();
            var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;
            var target=new RenderTexture(1280,720,24);var previous=RenderTexture.active;
            try
            {
                camera.transform.SetPositionAndRotation(new Vector3(0,1.67f,2.7f),Quaternion.Euler(0,-90,0));camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                System.IO.File.WriteAllBytes(Reports+"/Captures/L04_StartHint.png",image.EncodeToPNG());Object.DestroyImmediate(image);
            }
            finally {camera.targetTexture=null;RenderTexture.active=previous;camera.transform.SetPositionAndRotation(position,rotation);Object.DestroyImmediate(target);}
        }
        public static void BuildPlayer()
        {
            var scenes = new List<string>(); foreach(var scene in EditorBuildSettings.scenes) if(scene.enabled) scenes.Add(scene.path);
            var result = BuildPipeline.BuildPlayer(scenes.ToArray(), "Builds/Windows/ECHO.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
            System.IO.Directory.CreateDirectory(Reports); System.IO.File.WriteAllText(Reports + "/WindowsBuild.txt", $"{result.summary.result}\n{result.summary.totalErrors} errors, {result.summary.totalWarnings} warnings\n{result.summary.totalTime}");
            if(result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Windows build failed");
        }
        static Transform Marker(string name,GameObject parent,Vector3 p) { var t=Node(name,parent).transform;t.position=p;return t; }
        static void Slab(string name,GameObject root,float x,float a,float b,float width,Material material)=>Cube(name,root,new Vector3(x,2.8f,(a+b)/2),new Vector3(width,.4f,b-a),material);
        static void Rail(GameObject root,float x,float a,float b,Material material)=>Cube("Gallery rail",root,new Vector3(x,3.7f,(a+b)/2),new Vector3(.15f,1.4f,b-a),material);
        static void Wall(GameObject root,float x,float a,float b,Material material)=>Cube("Opaque partition",root,new Vector3(x,4.8f,(a+b)/2),new Vector3(.25f,3.6f,b-a),material);
        static DoorController Gate(string name,GameObject root,Vector3 pos,Material red,Material green) { var d=Door(name,root,0,red,green);d.transform.position=pos;return d; }
        static PhaseNode Phase(string name,GameObject root,Vector3 pos)
        {
            var go=Cube(name,root,pos,new Vector3(.7f,.7f,.24f),Mat("Discovered node"));var n=go.AddComponent<PhaseNode>();n.StableId="L04_"+name;n.DisplayName=name=="Node_U1"?"УЗЕЛ У1":"УЗЕЛ У2";n.Mode=PhaseNodeMode.Impulse;n.InitiallyHidden=false;n.Visual=go.GetComponent<Renderer>();n.DiscoveredMaterial=Mat("Discovered node");n.InactiveMaterial=Mat("Idle node");n.ActiveMaterial=Mat("Signaling node");n.ResetNode();return n;
        }
        static SecurityCameraController CameraDevice(string id,GameObject parent,Vector3 position,Vector3 aim,float range,float fov,Material backing)
        {
            var go=Node("Security camera "+id,parent);go.transform.position=position;var c=go.AddComponent<SecurityCameraController>();c.CameraId=id;c.Range=range;c.FieldOfView=fov;c.SweepAngle=4;
            c.Head=Node("Pan tilt head",go).transform;c.Head.rotation=Quaternion.LookRotation(aim-position);
            var housing=Cube("Camera housing",c.Head.gameObject,position,new Vector3(.5f,.32f,.6f),backing);housing.transform.localRotation=Quaternion.identity;Object.DestroyImmediate(housing.GetComponent<Collider>());
            var lens=Cube("Camera lens",c.Head.gameObject,position,new Vector3(.24f,.24f,.08f),Mat("Discovered node"));lens.transform.localPosition=Vector3.forward*.35f;lens.transform.localRotation=Quaternion.identity;Object.DestroyImmediate(lens.GetComponent<Collider>());
            c.LockBeam=Node("Capture beam",go).AddComponent<LineRenderer>();c.LockBeam.sharedMaterial=Mat("Player scan beam");c.LockBeam.positionCount=2;c.LockBeam.startWidth=c.LockBeam.endWidth=.035f;c.LockBeam.enabled=false;
            c.Cone=Node("Detection sector",go).AddComponent<LineRenderer>();c.Cone.sharedMaterial=Mat("Echo scan beam");c.Cone.startWidth=c.Cone.endWidth=.012f;
            c.Display=Sign("Camera display "+id,parent,position+new Vector3(0,.5f,-.1f),id+" · НАБЛЮДЕНИЕ",new Vector2(3,.5f),1.5f,UITheme.Amber,backing);
            return c;
        }
    }
}
