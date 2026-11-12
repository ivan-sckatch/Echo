using System.Collections.Generic;
using System.IO;
using EchoGame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using static EchoGame.Editor.Level02Builder;
namespace EchoGame.Editor
{
    public static class Level05Builder
    {
        public const string ScenePath = "Assets/_ECHO/Scenes/Levels/Level_05_Soglasie.unity";
        public const string Reports = "C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-11-Level05";
        public const string ScannerReports = "C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-12-Level05-Scanner";
        static Material grey, cyan, orange, yellow, green, red, white;
        static readonly List<GeometryMechanism> mechanisms = new List<GeometryMechanism>();
        [MenuItem("Tools/ECHO/Build Level 05 Soglasie")]
        public static void Build() { if (!EditorApplication.isPlaying && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) BuildBatch(); }
        public static void BuildBatch()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode before building Level 05.");
            var current = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (current.isDirty) throw new System.InvalidOperationException("Save the current scene before building Level 05.");
            mechanisms.Clear();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_ECHO/UI/Fonts/ECHO UI SDF.asset");
            Directory.CreateDirectory("Assets/_ECHO/Materials/Level05");
            grey = Material("Static", new Color(.38f,.41f,.45f)); cyan = Material("Bridge", new Color(.15f,.65f,.85f));
            orange = Material("Ramp", new Color(.95f,.44f,.12f)); yellow = Material("Terminal", new Color(1,.78f,.13f));
            green = Material("Checkpoint", new Color(.15f,.9f,.4f)); red = Material("Danger", new Color(.8f,.12f,.12f));
            white = Material("Exit", new Color(.93f,.98f,1));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = Node("Level_05_Soglasie"); var geometry = Node("LevelRoot", root);
            var stat = Node("StaticGeometry", geometry); var dynamics = Node("DynamicGeometry", geometry);
            var gameplay = Node("Gameplay", root); var systems = Node("Systems", root); var lights = Node("Lighting", root);
            foreach (float x in new[] { -15f, 15f }) Cube("Shaft wall", stat, new Vector3(x,13.5f,0),new Vector3(.4f,27,30.4f),grey);
            foreach (float z in new[] { -15f, 15f }) Cube("Shaft wall", stat, new Vector3(0,13.5f,z),new Vector3(30,.0f+27,.4f),grey);
            Cube("Fall danger",stat,new Vector3(0,-3.5f,0),new Vector3(30,.2f,30),red);
            Floor(stat,"Start island",new Vector3(-10.5f,0,-9),new Vector2(7,10));
            Floor(stat,"Lower ramp approach",new Vector3(11,0,-10),new Vector2(4,4));
            Floor(stat,"Checkpoint 1 landing",new Vector3(10,8,11),new Vector2(8,6));
            Floor(stat,"Middle ramp approach",new Vector3(-11,8,8.5f),new Vector2(4,9));
            Floor(stat,"Checkpoint 2 landing",new Vector3(-10.5f,16,-12),new Vector2(7,4));
            Floor(stat,"Upper ramp approach",new Vector3(11,16,-10),new Vector2(4,4));
            Floor(stat,"Exit landing",new Vector3(11,24,10),new Vector2(4,4));
            // A narrow exit vestibule contains the entire completion volume behind the gate.
            foreach(float x in new[]{8.9f,13.1f}) Cube("Exit side wall",stat,new Vector3(x,25.6f,10.3f),new Vector3(.2f,3.2f,4),grey);
            Cube("Exit rear wall",stat,new Vector3(11,25.6f,12.2f),new Vector3(4.4f,3.2f,.2f),white);
            Cube("Exit lintel",stat,new Vector3(11,27.1f,9.3f),new Vector3(4.4f,.3f,.4f),white);
            var a = Node("Tier01",dynamics); var b = Node("Tier02",dynamics); var d = Node("Tier03",dynamics);
            var lower = new[]{ Bridge(a,"Bridge A",0,new Vector3(-3,0,-10),8), Bridge(a,"Bridge B",0,new Vector3(5,0,-10),8), Ramp(a,"Rising ramp",0,new Vector3(11,.08f,-8),false) };
            var middleBridge = Bridge(b,"Middle bridge",1,new Vector3(-2,8,11),16);
            var wall = Moving(b,"Sliding partition",1,new Vector3(1,9.6f,11),new Vector3(1,13.2f,11),new Vector3(.4f,3.2f,4),grey);
            var middle = new[]{middleBridge,wall,Ramp(b,"Manual rising ramp",1,new Vector3(-11,8.08f,6),true)};
            var upper = new[]{ Bridge(d,"Final bridge A",2,new Vector3(-3,16,-11),8), Bridge(d,"Final bridge B",2,new Vector3(5,16,-11),8), Ramp(d,"Final rising ramp",2,new Vector3(11,16.08f,-8),false) };
            var game = Node("GameManager",systems).AddComponent<GameManager>();
            var cycles = Node("TimeCycleManager",systems).AddComponent<TimeCycleManager>(); game.Cycles=cycles;cycles.Game=game;
            var cp = Node("CheckpointManager",systems).AddComponent<GeometryCheckpointManager>(); cp.Cycles=cycles;cp.Mechanisms=mechanisms.ToArray();cp.PendingMaterial=yellow;cp.FixedMaterial=green;
            cp.SectionSpawns = new[]{ Marker(gameplay,"PlayerSpawn",new Vector3(-11,.02f,-12)),Marker(gameplay,"Section 2 spawn",new Vector3(11,8.02f,10),-90),Marker(gameplay,"Section 3 spawn",new Vector3(-11,16.02f,-12),90) };
            cp.Checkpoints=new[]{ Marker(gameplay,"Checkpoint_01",new Vector3(11,8,10)),Marker(gameplay,"Checkpoint_02",new Vector3(-11,16,-12)) };
            cp.CheckpointVisuals=new Renderer[2];cp.CheckpointLights=new Light[2];
            for(int i=0;i<2;i++)
            {
                cp.CheckpointVisuals[i]=Cube("Checkpoint pad "+(i+1),gameplay,cp.Checkpoints[i].position+Vector3.up*.015f,new Vector3(2.8f,.03f,2.8f),green).GetComponent<Renderer>();
                var light=Node("Checkpoint glow "+(i+1),lights).AddComponent<Light>();light.transform.position=cp.Checkpoints[i].position+Vector3.up;light.color=Color.green;light.range=4;cp.CheckpointLights[i]=light;
            }
            var playerObject=Node("Player",gameplay);playerObject.transform.position=cp.SectionSpawns[0].position;
            var cc=playerObject.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.32f;cc.center=Vector3.up*.9f;cc.stepOffset=.25f;cc.skinWidth=.025f;
            var player=playerObject.AddComponent<PlayerController>();
            var camera=Node("First Person Camera",playerObject).AddComponent<Camera>();camera.tag="MainCamera";camera.transform.localPosition=Vector3.up*1.65f;camera.nearClipPlane=.06f;camera.farClipPlane=100;camera.fieldOfView=75;camera.gameObject.AddComponent<AudioListener>();player.View=camera.transform;
            var scanner=playerObject.AddComponent<TemporalScanner>();scanner.View=camera.transform;player.Scanner=scanner;
            var echoes=Node("EchoSystem",gameplay).AddComponent<EchoManager>();echoes.MaximumEchoes=2;echoes.ConcurrentIntervals=false;echoes.ShareSingleSlot=false;echoes.Container=Node("EchoContainer",gameplay).transform;
            echoes.Prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ECHO/Prefabs/Echoes/Echo.prefab").GetComponent<EchoPlayback>();
            var level=Node("Tier puzzles",gameplay).AddComponent<Level05Controller>();level.Cycles=cycles;level.Checkpoints=cp;level.Lower=lower;level.Middle=middle;level.Upper=upper;
            cycles.Player=player;cycles.Spawn=cp.SectionSpawns[0];cycles.Echoes=echoes;cycles.Recorder=playerObject.AddComponent<EchoRecorder>();cycles.Duration=30;cycles.AutoStartRecording=false;cycles.LevelName="05 · Согласие";cycles.GeometryPuzzle=level;
            level.PlateA=Plate(gameplay,"A",new Vector3(-12,0,-7));level.PlateB=Plate(gameplay,"B",new Vector3(-9,0,-12));
            level.MiddleWall=Terminal(gameplay,level,"Перегородка",1,new Vector3(12,9.65f,12.5f),20);
            level.MiddleBridge=Terminal(gameplay,level,"Мост",1,new Vector3(7,9.65f,12.5f),20);
            level.Lift=Terminal(gameplay,level,"Подъём",1,new Vector3(-12.5f,9.65f,7),30);level.Lift.PlayerOnly=true;
            level.UpperA=Terminal(gameplay,level,"Сегмент A",2,new Vector3(-12,17.65f,-10.3f),20);
            level.UpperB=Terminal(gameplay,level,"Сегмент B",2,new Vector3(-8.5f,17.65f,-13.5f),20);
            level.Terminals=new[]{level.MiddleWall,level.MiddleBridge,level.Lift,level.UpperA,level.UpperB};
            var node=Cube("Final phase node",gameplay,new Vector3(11,25.65f,8.7f),new Vector3(.7f,.7f,.2f),yellow).AddComponent<PhaseNode>();
            node.StableId="L05_Final";node.DisplayName="ФИНАЛЬНЫЙ ФАЗОВЫЙ УЗЕЛ";node.InitiallyHidden=false;node.Mode=PhaseNodeMode.Impulse;node.ActivationSeconds=1;
            node.Visual=node.GetComponent<Renderer>();node.InactiveMaterial=yellow;node.DiscoveredMaterial=cyan;node.ActiveMaterial=green;node.ResetNode();level.FinalNode=node;
            level.ExitDoor=Door("Exit sluice",gameplay,0,red,green);level.ExitDoor.transform.position=new Vector3(11,24,9.3f);
            level.ExitDoor.Panel.localScale=new Vector3(4.1f,3.4f,.4f);level.ExitDoor.ResetMechanism();
            var exit=Node("ExitTrigger",gameplay).AddComponent<LevelCompletion>();exit.transform.position=new Vector3(11,24,11);exit.HalfExtents=new Vector3(1.5f,.7f,.6f);cycles.Exit=exit;
            var simulation=Node("ScanSimulation",systems).AddComponent<ScanSimulation>();simulation.Game=game;simulation.Cycles=cycles;simulation.PlayerScanner=scanner;simulation.Nodes=new[]{node};simulation.SynchronizationHints=false;
            simulation.TutorialHints=false;
            simulation.PlayerBeamMaterial=ScanMat("Player scan beam");simulation.EchoBeamMaterial=ScanMat("Echo scan beam");cycles.Scanning=simulation;
            var interaction=playerObject.AddComponent<PlayerInteraction>();interaction.Game=game;interaction.View=camera;game.Interaction=interaction;
            cycles.UI=EchoUIBuilder.Install(game,cycles);cycles.UI.GetComponent<UIEventBridge>().ContextSource=level;
            var hud=Object.FindAnyObjectByType<HUDController>();var status=Node("Checkpoint status",hud.gameObject).AddComponent<TextMeshProUGUI>();status.font=font;status.fontSize=20;status.color=UITheme.Text;status.alignment=TextAlignmentOptions.BottomLeft;
            var rt=status.rectTransform;rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.zero;rt.anchoredPosition=new Vector2(38,110);rt.sizeDelta=new Vector2(1300,60);level.Status=status;
            // Device names, functions and timings belong to scanner inspection, without world-space signs.
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.69f,.75f);
            foreach(float y in new[]{5f,13f,22f}) {var light=Node("Shaft light",lights).AddComponent<Light>();light.type=LightType.Point;light.transform.position=new Vector3(0,y,0);light.intensity=5;light.range=28;}
            foreach(var m in mechanisms)m.ResetMechanism();
            ResearchInspectionBuilder.Clean(scene);
            Level05LeverUpgrade.Install(level);
            EditorSceneManager.SaveScene(scene,ScenePath);
            var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);scenes.RemoveAll(s=>s.path==ScenePath);scenes.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Reports);Debug.Log("ECHO: Level 05 saved: three tiers, two echoes, two geometry checkpoints.");
        }
        static Material Material(string name,Color color)
        {
            var path="Assets/_ECHO/Materials/Level05/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",color);EditorUtility.SetDirty(mat);return mat;
        }
        static Material ScanMat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_ECHO/Materials/Scanner/"+name+".mat");
        static Transform Marker(GameObject parent,string name,Vector3 pos,float yaw=0){var t=Node(name,parent).transform;t.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));return t;}
        static void Floor(GameObject parent,string name,Vector3 pos,Vector2 size)=>Cube(name,parent,pos-Vector3.up*.25f,new Vector3(size.x,.5f,size.y),grey);
        static GeometryMechanism Moving(GameObject parent,string name,int section,Vector3 rest,Vector3 end,Vector3 size,Material mat)
        {
            var go=Node(name,parent);var m=go.AddComponent<GeometryMechanism>();m.Section=section;m.RestPosition=rest;m.ActivePosition=end;m.TravelSeconds=1.2f;
            var mesh=Cube("Collidable geometry",go,Vector3.zero,size,mat);mesh.transform.localPosition=Vector3.zero;
            m.Indicator=Cube("Signal indicator",go,Vector3.zero,new Vector3(.3f,.08f,.3f),yellow).GetComponent<Renderer>();m.Indicator.transform.localPosition=new Vector3(0,size.y/2+.05f,0);
            m.IdleMaterial=yellow;m.MovingMaterial=orange;m.ReadyMaterial=green;mechanisms.Add(m);m.ResetMechanism();return m;
        }
        static GeometryMechanism Bridge(GameObject parent,string name,int tier,Vector3 top,float length)
        {
            var end=top-Vector3.up*.2f;var m=Moving(parent,name,tier,end+new Vector3(0,-4,4),end,new Vector3(length,.4f,3),cyan);m.TravelSeconds=1;
            // Low rails move with the deck and prevent walking around the middle partition.
            foreach(float z in new[]{-1.65f,1.65f}) {var rail=Cube("Bridge rail",m.gameObject,Vector3.zero,new Vector3(length,1.5f,.15f),grey);rail.transform.localPosition=new Vector3(0,.95f,z);}
            return m;
        }
        static GeometryMechanism Ramp(GameObject parent,string name,int tier,Vector3 foot,bool reverse)
        {
            float length=Mathf.Sqrt(16*16+8*8);var go=Node(name,parent);var m=go.AddComponent<GeometryMechanism>();m.Section=tier;m.ActivePosition=foot;
            // Stow against the outer shaft casing; a hanging upper ramp must not obstruct the tier below.
            m.RestPosition=foot+Vector3.right*(reverse?-4:4);
            m.RestEuler=new Vector3(80,reverse?180:0,0);m.ActiveEuler=new Vector3(-Mathf.Atan2(8,16)*Mathf.Rad2Deg,reverse?180:0,0);m.TravelSeconds=1.5f;
            var deck=Cube("Collidable ramp",go,Vector3.zero,new Vector3(3,.4f,length),orange);deck.transform.localPosition=new Vector3(0,-.2f,length/2);
            foreach(float x in new[]{-1.65f,1.65f}){var rail=Cube("Ramp rail",go,Vector3.zero,new Vector3(.15f,1.5f,length),grey);rail.transform.localPosition=new Vector3(x,.75f,length/2);}
            mechanisms.Add(m);m.ResetMechanism();return m;
        }
        static PressurePlate Plate(GameObject parent,string name,Vector3 feet)
        {
            var go=Node("Pressure plate "+name,parent);go.transform.position=feet;
            var mesh=Cube("Plate face",go,feet+Vector3.up*.025f,new Vector3(1.6f,.05f,1.6f),yellow);
            var plate=go.AddComponent<PressurePlate>();plate.HalfExtents=new Vector3(.8f,.4f,.8f);plate.Surface=mesh.GetComponent<Renderer>();plate.InactiveMaterial=yellow;plate.ActiveMaterial=green;return plate;
        }
        static GeometryTerminal Terminal(GameObject parent,Level05Controller level,string label,int section,Vector3 pos,float window)
        {
            var go=Cube("Terminal "+label,parent,pos,new Vector3(.65f,.65f,.3f),yellow);var t=go.AddComponent<GeometryTerminal>();t.Level=level;t.Label=label;t.Section=section;t.StableId="L05_"+section+"_"+label;t.Window=window;t.Face=go.GetComponent<Renderer>();t.IdleMaterial=yellow;t.ActiveMaterial=green;
            Cube("Terminal support",parent,pos-Vector3.up*.85f,new Vector3(.25f,1.1f,.25f),grey);return t;
        }
        [MenuItem("Tools/ECHO/Level 05/Remove world signs")]
        public static void RemoveWorldSigns()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before editing the saved scene.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.isDirty) throw new System.InvalidOperationException("Save the current scene before removing signs.");
            if (scene.path!=ScenePath) scene=EditorSceneManager.OpenScene(ScenePath);
            Directory.CreateDirectory(ScannerReports);
            File.Copy(ScenePath,ScannerReports+"/Before_sign_removal_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity");
            int removed=0;
            foreach(var root in scene.GetRootGameObjects())
            foreach(var text in root.GetComponentsInChildren<TextMeshPro>(true))
            {
                if (!text) continue;
                var sign=text.transform.parent ? text.transform.parent.gameObject : text.gameObject;
                if (sign.name=="Level title" || sign.name=="Exit beacon" || sign.name.StartsWith("Plate label ") || sign.name=="Terminal label")
                { Object.DestroyImmediate(sign);removed++; }
            }
            foreach(var root in scene.GetRootGameObjects())
            foreach(var scanning in root.GetComponentsInChildren<ScanSimulation>(true))
            { scanning.TutorialHints=false;EditorUtility.SetDirty(scanning); }
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText(ScannerReports+"/SceneCleanup.txt",$"Removed {removed} sign roots with their text and backing. Gameplay geometry and UI were retained.");
            Debug.Log($"ECHO Level 05: removed {removed} world signs; use scanner inspection.");
        }
    }
}
