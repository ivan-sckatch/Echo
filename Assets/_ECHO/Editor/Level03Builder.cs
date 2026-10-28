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
    public static class Level03Builder
    {
        public const string Path = "Assets/_ECHO/Scenes/Levels/Level_03.unity";
        public const string Reports = "C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-10-28-Level03";
        [MenuItem("Tools/ECHO/Build Level 03")] public static void Build() => BuildScene(true);
        public static void BuildScene(bool ask)
        {
            if (EditorApplication.isPlaying) return;
            if (ask && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_ECHO/UI/Fonts/ECHO UI SDF.asset");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = Node("Level_03"); var env = Node("Environment", root); var mechanisms = Node("Mechanisms", root);
            var managers = Node("LevelLogic", root); var players = Node("Player", root); var runtime = Node("Runtime", root);
            var floor = Existing("Floor"); var wall = Existing("Walls"); var red = Existing("Locked"); var green = Existing("Active");
            var backing = AssetDatabase.LoadAssetAtPath<Material>("Assets/_ECHO/Materials/Scanner/Technical sign.mat");
            var amber = AssetDatabase.LoadAssetAtPath<Material>("Assets/_ECHO/Materials/Scanner/Idle node.mat");
            var glass = Glass();
            Cube("Floor", env, new Vector3(-4, -.25f, 22), new Vector3(16, .5f, 44), floor);
            Cube("Ceiling", env, new Vector3(-4, 4.75f, 22), new Vector3(16, .5f, 44), wall);
            Cube("West outer wall", env, new Vector3(-12, 2.25f, 22), new Vector3(.25f, 4.5f, 44), wall);
            Cube("East outer wall", env, new Vector3(4, 2.25f, 22), new Vector3(.25f, 4.5f, 44), wall);
            foreach (float z in new[] { 0f, 44f }) Cube("End wall", env, new Vector3(-4, 2.25f, z), new Vector3(16, 4.5f, .25f), wall);
            // Only the starting area connects the two routes. Gallery never crosses a main gate.
            var start = Node("StartingArea", env); var gallery = Node("TechnicalGallery", env); var corridor = Node("MainCorridor", env);
            WallZ(env, 0, 6, 9, wall); WallZ(env, 0, 13, 21, wall); WallZ(env, 0, 25, 34, wall); WallZ(env, 0, 38, 44, wall);
            for (int i = 0; i < 3; i++)
            {
                float startZ = i == 0 ? 6 : i == 1 ? 18 : 30;
                float endZ = i == 0 ? 14 : i == 1 ? 26 : 40;
                float entryZ = i == 0 ? 10 : i == 1 ? 22 : 35;
                WallZ(gallery, -9, startZ, entryZ - 1.5f, wall); WallZ(gallery, -9, entryZ + 1.5f, endZ, wall);
                Cube("Entrance lintel", gallery, new Vector3(-9, 3.95f, entryZ), new Vector3(.25f, 1.1f, 3), wall);
                foreach (float z in new[] { startZ, endZ }) Cube("Chamber partition", env, new Vector3(-4.5f, 2.25f, z), new Vector3(9, 4.5f, .25f), wall);
            }
            WallZ(gallery, -9, 14, 18, wall); WallZ(gallery, -9, 26, 30, wall); WallZ(gallery, -9, 40, 44, wall);
            var spawn = Node("PlayerSpawn", players).transform; spawn.position = new Vector3(2, .02f, 2);
            var po = Node("Player", players); po.transform.position = spawn.position;
            var cc = po.AddComponent<CharacterController>(); cc.height = 1.8f; cc.radius = .32f; cc.center = Vector3.up * .9f; cc.stepOffset = .25f; cc.skinWidth = .025f;
            var player = po.AddComponent<PlayerController>();
            var camera = Node("First Person Camera", po).AddComponent<Camera>(); camera.transform.localPosition = Vector3.up * 1.65f; camera.nearClipPlane = .06f; camera.farClipPlane = 100; camera.fieldOfView = 75; camera.tag = "MainCamera"; camera.gameObject.AddComponent<AudioListener>(); player.View = camera.transform;
            player.Scanner = po.AddComponent<TemporalScanner>(); player.Scanner.View = camera.transform;
            var game = Node("GameManager", managers).AddComponent<GameManager>(); var c = Node("TimeCycleManager", managers).AddComponent<TimeCycleManager>();
            var e = Node("EchoManager", managers).AddComponent<EchoManager>(); e.MaximumEchoes = 1; e.ConcurrentIntervals = true; e.ShareSingleSlot = true; e.Container = Node("EchoContainer", runtime).transform;
            e.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ECHO/Prefabs/Echoes/Echo.prefab").GetComponent<EchoPlayback>();
            var puzzle = Node("Level03Controller", managers).AddComponent<Level03Controller>(); puzzle.Cycles = c;
            puzzle.ScannerMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_ECHO/Materials/Scanner/Player scan beam.mat");
            c.Game = game; game.Cycles = c; c.Player = player; c.Spawn = spawn; c.Echoes = e; c.Recorder = po.AddComponent<EchoRecorder>(); c.TimedPuzzle = puzzle;
            c.AutoStartRecording = false; c.Duration = 30; c.LevelName = "03 · Поймай момент";
            puzzle.Buttons = new PulseButton[3]; puzzle.Doors = new TimedDoor[3]; puzzle.Chambers = new ChamberIsolation[3];
            for (int i = 0; i < 3; i++)
            {
                string letter = ((char)('A' + i)).ToString(); float z = i == 0 ? 10 : i == 1 ? 22 : 35; float dz = i == 0 ? 12 : i == 1 ? 24 : 38;
                var chamberRoot = Node("Chamber_" + letter, mechanisms);
                var entrance = MakeDoor("ChamberLock_" + letter, chamberRoot, new Vector3(-9, 0, z), 90, red, green);
                var isolation = chamberRoot.AddComponent<ChamberIsolation>(); isolation.ChamberName = letter; isolation.Entrance = entrance;
                var observation = Node("ObservationPoint", chamberRoot).transform; observation.position = new Vector3(-2, .02f, z); isolation.ObservationPoint = observation;
                float wz = i == 0 ? 11 : i == 1 ? 23 : 36;
                Cube("Window sill", chamberRoot, new Vector3(0, .4f, wz), new Vector3(.25f, .8f, 4), wall);
                Cube("Window lintel", chamberRoot, new Vector3(0, 3.9f, wz), new Vector3(.25f, 1.2f, 4), wall);
                isolation.Window = Cube("Observation glass " + letter, chamberRoot, new Vector3(0, 2.05f, wz), new Vector3(.12f, 2.5f, 4), glass).GetComponent<Collider>();
                foreach (float edge in new[] { wz - 2, wz + 2 }) Cube("Window frame", chamberRoot, new Vector3(0, 2.05f, edge), new Vector3(.3f, 2.5f, .09f), backing);
                var door = MakeDoor("Door_D" + (i + 1), mechanisms, new Vector3(2, 0, dz), 0, red, green);
                foreach (float x in new[] { .25f, 3.75f }) Cube("Gate jamb", corridor, new Vector3(x, 2.25f, dz), new Vector3(.5f, 4.5f, .4f), wall);
                Cube("Gate lintel", corridor, new Vector3(2, 3.95f, dz), new Vector3(3, 1.1f, .4f), wall);
                var timed = door.gameObject.AddComponent<TimedDoor>(); timed.Door = door; timed.LatchUntilReset = i < 2; timed.OncePerCycle = true; timed.Retrigger = false; timed.DoorLabel = "D" + (i + 1);
                timed.Indicator = Sign("Door display D" + (i + 1), mechanisms, new Vector3(2, 3.05f, dz - .29f), "", new Vector2(2.9f, .7f), 1.9f, UITheme.Amber, backing);
                Cube("Button pedestal", chamberRoot, new Vector3(-.45f, .72f, z + .5f), new Vector3(.25f, 1.44f, .45f), backing);
                var face = Disc("Button_" + letter, chamberRoot, new Vector3(-.65f, 1.5f, z + .5f), .42f, .12f, amber);
                var button = face.AddComponent<PulseButton>(); button.StableId = "L03_Button_" + letter; button.Level = puzzle; button.Isolation = isolation; button.Door = timed;
                button.Face = face.GetComponent<Renderer>(); button.ReadyMaterial = amber; button.PressedMaterial = green;
                Sign("Button label " + letter, chamberRoot, new Vector3(-.82f, .85f, z + .5f), letter + " → D" + (i + 1), new Vector2(.85f, .35f), 1.4f, UITheme.Amber, backing, 90);
                Sign("Chamber label " + letter, env, new Vector3(-9.2f, 2.7f, z), "КАМЕРА " + letter, new Vector2(2.5f, .65f), 2.3f, UITheme.Cyan, backing, 90);
                var link = Cube("Connection " + letter, chamberRoot, new Vector3(-.1f, .75f, wz), new Vector3(.035f, .035f, 3.8f), amber); Object.DestroyImmediate(link.GetComponent<Collider>());
                puzzle.Buttons[i] = button; puzzle.Doors[i] = timed; puzzle.Chambers[i] = isolation; timed.ResetDoor(); isolation.ResetChamber();
            }
            var exit = Node("ExitTrigger", mechanisms).AddComponent<LevelCompletion>(); exit.transform.position = new Vector3(2, 0, 42); c.Exit = exit;
            Cube("Exit pad", mechanisms, new Vector3(2, .025f, 42), new Vector3(3, .05f, 2), green);
            Sign("Exit", env, new Vector3(2, 2, 43.8f), "ВЫХОД", new Vector2(3, 1), 4, UITheme.Cyan, backing);
            Sign("Start map", start, new Vector3(-4.5f, 2.2f, .25f), "ECHO / 03 · ПОЙМАЙ МОМЕНТ\nГАЛЕРЕЯ — КАМЕРЫ A / B / C\nОСНОВНОЙ КОРИДОР — D1 / D2 / D3", new Vector2(7, 1.7f), 2.5f, UITheme.Text, backing, 180);
            Sign("Gallery direction", start, new Vector3(-8.5f, 2.6f, 5.7f), "ТЕХНИЧЕСКАЯ ГАЛЕРЕЯ\nA → B → C", new Vector2(5, 1.2f), 2.5f, UITheme.Cyan, backing);
            FragmentHint(start, backing);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.6f, .64f, .69f);
            for (int i = 0; i < 5; i++) foreach (float x in new[] { -10.5f, -4.5f, 2f }) { var l = Node("Ceiling light", env).AddComponent<Light>(); l.type = LightType.Point; l.range = 11; l.intensity = 2; l.transform.position = new Vector3(x, 4, 4 + i * 8); }
            c.UI = EchoUIBuilder.Install(game, c); c.UI.GetComponent<UIEventBridge>().ContextSource = puzzle;
            var hud = root.GetComponentInChildren<HUDController>(true); var controls = new GameObject("Level03 controls", typeof(RectTransform)); controls.transform.SetParent(hud.transform, false);
            var label = controls.AddComponent<TextMeshProUGUI>(); label.font = font; label.fontSize = 21; label.color = UITheme.Text; label.alignment = TextAlignmentOptions.BottomLeft;
            var rect = label.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero; rect.anchoredPosition = new Vector2(38, 110); rect.sizeDelta = new Vector2(1200, 90); puzzle.Controls = label;
            ResearchInspectionBuilder.Clean(scene);
            EditorSceneManager.SaveScene(scene, Path);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes); scenes.RemoveAll(s => s.path == Path); scenes.Add(new EditorBuildSettingsScene(Path, true)); EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets(); Debug.Log("ECHO Level03: three chambers, isolated gallery and three-fragment echo.");
        }
        static void WallZ(GameObject root, float x, float a, float b, Material mat) { Cube("Route wall", root, new Vector3(x, 2.25f, (a+b)/2), new Vector3(.25f, 4.5f, b-a), mat); }
        static void FragmentHint(GameObject parent, Material backing)
        {
            Sign("Echo fragment hint", parent, new Vector3(-11.8f, 2.1f, 3),
                "<b>ЭХО МОЖНО ДРОБИТЬ</b>\n<size=70%>Записывай отдельные фрагменты\nв разных циклах — они дополняют одно Эхо.</size>",
                new Vector2(4.6f, 1.35f), 2.8f, UITheme.Text, backing, -90);
        }
        public static void PreviewFragmentHint()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode before refreshing the scene.");
            EditorSceneManager.OpenScene(Path);
            var hint = GameObject.Find("Level_03/Environment/StartingArea/Echo fragment hint");
            if (!hint) throw new System.InvalidOperationException("Missing fragment hint in saved scene.");
            foreach (var text in hint.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
            var camera = Camera.main; var position = camera.transform.position; var rotation = camera.transform.rotation;
            var target = new RenderTexture(1280, 720, 24); var previous = RenderTexture.active;
            try
            {
                camera.transform.SetPositionAndRotation(new Vector3(-7, 1.67f, 3), Quaternion.Euler(0, -90, 0));
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                var image = new Texture2D(1280, 720, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                System.IO.File.WriteAllBytes(Reports + "/Captures/L03_FragmentHint.png", image.EncodeToPNG()); Object.DestroyImmediate(image);
            }
            finally { camera.targetTexture = null; RenderTexture.active = previous; camera.transform.SetPositionAndRotation(position, rotation); Object.DestroyImmediate(target); }
            Debug.Log("ECHO: saved scene reloaded; fragment hint camera preview captured.");
        }
        static DoorController MakeDoor(string name, GameObject root, Vector3 position, float yaw, Material red, Material green)
        {
            var door = Door(name, root, 0, red, green); door.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0)); return door;
        }
        static Material Glass()
        {
            System.IO.Directory.CreateDirectory("Assets/_ECHO/Materials/Level03"); const string path = "Assets/_ECHO/Materials/Level03/Observation glass.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path); if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", new Color(.34f, .7f, .8f, .08f)); mat.SetFloat("_Surface", 1); mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); mat.SetFloat("_ZWrite", 0); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.renderQueue = 3000; EditorUtility.SetDirty(mat); return mat;
        }
        public static void BuildBatch() => BuildScene(false);
        static GameObject Disc(string name, GameObject parent, Vector3 position, float diameter, float depth, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = name; go.transform.SetParent(parent.transform, false); go.transform.SetPositionAndRotation(position, Quaternion.Euler(0,0,90)); go.transform.localScale = new Vector3(diameter, depth/2, diameter);
            Object.DestroyImmediate(go.GetComponent<Collider>()); var col = go.AddComponent<MeshCollider>(); col.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh; col.convex = true; go.GetComponent<Renderer>().sharedMaterial = mat; return go;
        }
        public static void BuildPlayer()
        {
            var scenes = new List<string>(); foreach (var s in EditorBuildSettings.scenes) if(s.enabled) scenes.Add(s.path);
            var result = BuildPipeline.BuildPlayer(scenes.ToArray(), "Builds/Windows/ECHO.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
            System.IO.Directory.CreateDirectory(Reports); System.IO.File.WriteAllText(Reports + "/WindowsBuild.txt", $"{result.summary.result}\n{result.summary.totalErrors} errors, {result.summary.totalWarnings} warnings\n{result.summary.totalTime}");
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Windows build failed");
        }
    }
}
