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
        [MenuItem("Tools/ECHO/Build Level 03")] public static void Build() => BuildScene(true);
        public static void BuildScene(bool ask)
        {
            if (EditorApplication.isPlaying) return;
            if (ask && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EchoUIBuilder.RefreshPrefabOnly();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_ECHO/UI/Fonts/ECHO UI SDF.asset");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = Node("Level_03"); var env = Node("_Environment", root); var mechanisms = Node("_Mechanisms", root);
            var managers = Node("_Managers", root); var players = Node("_Player", root); var runtime = Node("_Runtime", root);
            var floor = Existing("Floor"); var wall = Existing("Walls"); var red = Existing("Locked"); var green = Existing("Active");
            var sign = AssetDatabase.LoadAssetAtPath<Material>("Assets/_ECHO/Materials/Scanner/Technical sign.mat");
            Cube("Floor", env, new Vector3(0, -.25f, 26), new Vector3(12, .5f, 52), floor);
            Cube("Ceiling", env, new Vector3(0, 4.25f, 26), new Vector3(12, .5f, 52), wall);
            Cube("West", env, new Vector3(-6, 2, 26), new Vector3(.5f, 4, 52), wall);
            Cube("East", env, new Vector3(6, 2, 26), new Vector3(.5f, 4, 52), wall);
            foreach (float z in new[] {0f, 52f}) Cube("End wall", env, new Vector3(0, 2, z), new Vector3(12, 4, .5f), wall);
            foreach (float z in new[] {8f, 48f})
            {
                Cube("Partition left", env, new Vector3(-3.75f, 2, z), new Vector3(4.5f, 4, .5f), wall);
                Cube("Partition right", env, new Vector3(3.75f, 2, z), new Vector3(4.5f, 4, .5f), wall);
                Cube("Lintel", env, new Vector3(0, 3.75f, z), new Vector3(3, .5f, .5f), wall);
            }
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.6f, .64f, .69f);
            for (int i = 0; i < 6; i++) { var l = Node("Light", env).AddComponent<Light>(); l.type = LightType.Point; l.range = 16; l.intensity = 3; l.transform.position = new Vector3(0, 3.6f, 4 + i * 9); }
            var spawn = Node("Spawn", players).transform; spawn.position = new Vector3(0, .02f, 2);
            var po = Node("Player", players); po.transform.position = spawn.position;
            var cc = po.AddComponent<CharacterController>(); cc.height = 1.8f; cc.radius = .32f; cc.center = Vector3.up * .9f; cc.stepOffset = .25f; cc.skinWidth = .025f;
            var player = po.AddComponent<PlayerController>();
            var camera = Node("First Person Camera", po).AddComponent<Camera>(); camera.transform.localPosition = Vector3.up * 1.65f; camera.nearClipPlane = .06f; camera.farClipPlane = 120; camera.fieldOfView = 75; camera.tag = "MainCamera"; camera.gameObject.AddComponent<AudioListener>(); player.View = camera.transform;
            player.Scanner = po.AddComponent<TemporalScanner>(); player.Scanner.View = camera.transform;
            var game = Node("GameManager", managers).AddComponent<GameManager>(); var c = Node("TimeCycleManager", managers).AddComponent<TimeCycleManager>();
            var e = Node("EchoManager", managers).AddComponent<EchoManager>(); e.MaximumEchoes = 1; e.ConcurrentIntervals = true; e.Container = Node("EchoContainer", runtime).transform;
            e.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ECHO/Prefabs/Echoes/Echo.prefab").GetComponent<EchoPlayback>();
            var puzzle = Node("Level03Controller", managers).AddComponent<Level03Controller>(); puzzle.Cycles = c;
            puzzle.ScannerMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_ECHO/Materials/Scanner/Player scan beam.mat");
            c.Game = game; game.Cycles = c; c.Player = player; c.Spawn = spawn; c.Echoes = e; c.Recorder = po.AddComponent<EchoRecorder>(); c.TimedPuzzle = puzzle;
            c.AutoStartRecording = false; c.Duration = 30; c.LevelName = "03 · Поймай момент";
            puzzle.Buttons = new PulseButton[2]; puzzle.Doors = new TimedDoor[2];
            for (int i = 0; i < 2; i++)
            {
                float z = i == 0 ? 8 : 48; float bz = i == 0 ? 4 : 12;
                var door = Door("Timed door " + (i + 1), mechanisms, z, red, green);
                var timed = door.gameObject.AddComponent<TimedDoor>(); timed.Door = door;
                timed.Indicator = Sign("Door countdown " + i, mechanisms, new Vector3(0, 3.05f, z - .35f), "ЗАКРЫТО · ИМПУЛЬС 6 с", new Vector2(2.9f, .82f), 2.0f, UITheme.Amber, sign);
                Cube("Wall button housing " + i, mechanisms, new Vector3(5.65f, 1.65f, bz), new Vector3(.14f, 1.1f, 1.1f), sign);
                Disc("Button rim " + i, mechanisms, new Vector3(5.55f, 1.65f, bz), .94f, .12f, wall);
                var face = Disc("Pulse button " + (i + 1), mechanisms, new Vector3(5.47f, 1.65f, bz), .72f, .12f, AssetDatabase.LoadAssetAtPath<Material>("Assets/_ECHO/Materials/Scanner/Idle node.mat"));
                var button = face.AddComponent<PulseButton>();
                button.StableId = "L03_Button_0" + (i + 1); button.Door = timed; button.Level = puzzle;
                button.Face = face.GetComponent<Renderer>(); button.ReadyMaterial = button.Face.sharedMaterial; button.PressedMaterial = green;
                Sign("Button label " + i, mechanisms, new Vector3(5.69f, 2.65f, bz), $"КНОПКА {i + 1} · F\nДВЕРЬ {i + 1} · 6 СЕКУНД", new Vector2(2.8f, .85f), 2.1f, UITheme.Amber, sign, 90);
                // Wall-mounted wiring stays out of the passage and points to the numbered door.
                var cable = Cube("Cable " + (i + 1), env, new Vector3(5.72f, .65f, (bz + z) / 2), new Vector3(.035f, .065f, z - bz), i == 0 ? green : red); Object.DestroyImmediate(cable.GetComponent<Collider>());
                var lead = Cube("Cable drop " + i, env, new Vector3(5.72f, 1.05f, bz), new Vector3(.035f, .8f, .065f), i == 0 ? green : red); Object.DestroyImmediate(lead.GetComponent<Collider>());
                var connector = Cube("Door cable " + i, env, new Vector3(3.6f, .65f, z - .29f), new Vector3(4.25f, .065f, .035f), i == 0 ? green : red); Object.DestroyImmediate(connector.GetComponent<Collider>());
                puzzle.Buttons[i] = button; puzzle.Doors[i] = timed; timed.ResetDoor();
            }
            Sign("Title", env, new Vector3(-5.69f, 2.1f, 4.5f), "ECHO / 03\nПОЙМАЙ МОМЕНТ\nENTER — НАЧАТЬ ЦИКЛ", new Vector2(3.8f, 1.65f), 2.5f, UITheme.Text, sign, -90);
            Sign("Manual recording", env, new Vector3(-5.69f, 2.1f, 13), "R — НАЧАТЬ ЗАПИСЬ\nR — ЗАВЕРШИТЬ И СОХРАНИТЬ\nДВЕРЬ 2 — ВРЕМЕННОЕ ОКНО", new Vector2(4.5f, 1.65f), 2.3f, UITheme.Text, sign, -90);
            var exit = Node("LevelExit", mechanisms).AddComponent<LevelCompletion>(); exit.transform.position = new Vector3(0, 0, 50.5f); c.Exit = exit;
            Cube("Exit pad", mechanisms, new Vector3(0, .025f, 50.5f), new Vector3(3, .05f, 2), green);
            Sign("Exit", env, new Vector3(0, 2, 51.65f), "ВЫХОД", new Vector2(4, 1), 5, UITheme.Cyan, sign);
            c.UI = EchoUIBuilder.Install(game, c); c.UI.GetComponent<UIEventBridge>().ContextSource = puzzle;
            var hud = root.GetComponentInChildren<HUDController>(true);
            var controls = new GameObject("Level 03 controls", typeof(RectTransform)); controls.transform.SetParent(hud.transform, false);
            var label = controls.AddComponent<TextMeshProUGUI>(); label.font = font; label.fontSize = 21; label.color = UITheme.Text;
            label.alignment = TextAlignmentOptions.BottomLeft; label.textWrappingMode = TextWrappingModes.Normal;
            var rect = label.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero; rect.anchoredPosition = new Vector2(38, 110); rect.sizeDelta = new Vector2(1200, 90);
            puzzle.Controls = label;
            EditorSceneManager.SaveScene(scene, Path);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes); scenes.RemoveAll(s => s.path == Path); scenes.Add(new EditorBuildSettingsScene(Path, true)); EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets(); Debug.Log("ECHO Level 03 generated.");
        }
        public static void BuildBatch() => BuildScene(false);
        static GameObject Disc(string name, GameObject parent, Vector3 position, float diameter, float depth, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = name; go.transform.SetParent(parent.transform, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, 0, 90)); go.transform.localScale = new Vector3(diameter, depth / 2, diameter);
            Object.DestroyImmediate(go.GetComponent<Collider>()); var collider = go.AddComponent<MeshCollider>(); collider.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh; collider.convex = true;
            go.GetComponent<Renderer>().sharedMaterial = mat; return go;
        }
        public static void BuildPlayer()
        {
            var scenes = new List<string>(); foreach (var s in EditorBuildSettings.scenes) if (s.enabled) scenes.Add(s.path);
            var report = BuildPipeline.BuildPlayer(scenes.ToArray(), "Builds/Windows/ECHO.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
            System.IO.File.WriteAllText("Assets/_ECHO/Tests/Results/WindowsBuild.txt", $"{report.summary.result}\n{report.summary.totalSize} bytes\n{report.summary.totalErrors} errors, {report.summary.totalWarnings} warnings\n{report.summary.totalTime}\n");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Windows build failed");
        }
    }
}
