using System.Collections.Generic;
using System.IO;
using EchoGame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace EchoGame.Editor
{
    public static class Level02Builder
    {
        public const string ScenePath = "Assets/_ECHO/Scenes/Levels/Level_02.unity";
        internal static TMP_FontAsset font;
        [MenuItem("Tools/ECHO/Build Level 02")]
        public static void Build() => BuildScene(true);
        public static void BuildScene(bool promptToSave)
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("ECHO: сначала остановите Play Mode."); return; }
            if (promptToSave && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // Refresh the shared UI asset only. Never open, regenerate or save Level_01 here.
            EchoUIBuilder.RefreshPrefabOnly();
            Directory.CreateDirectory("Assets/_ECHO/Scenes/Levels"); Directory.CreateDirectory("Assets/_ECHO/Materials/Scanner"); AssetDatabase.Refresh();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_ECHO/UI/Fonts/ECHO UI SDF.asset");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = Node("Level_02"); var managers = Node("_Managers", root); var environment = Node("_Environment", root);
            var mechanisms = Node("_Mechanisms", root); var players = Node("_Player", root); var runtime = Node("_Runtime", root);
            var floor = Existing("Floor"); var wall = Existing("Walls"); var red = Existing("Locked"); var green = Existing("Active");
            var discovered = Material("Discovered node", new Color(.34f, .86f, .9f));
            var inactive = Material("Idle node", new Color(1, .75f, .25f));
            var active = Material("Signaling node", new Color(.1f, .92f, .68f));
            var backing = Material("Technical sign", new Color(.025f, .045f, .06f));
            var playerBeam = Material("Player scan beam", new Color(.34f, .86f, .9f, .85f), true);
            var echoBeam = Material("Echo scan beam", new Color(.55f, .62f, 1, .85f), true);
            Cube("Floor", environment, new Vector3(0, -.25f, 15), new Vector3(18, .5f, 30), floor);
            Cube("Ceiling", environment, new Vector3(0, 4.25f, 15), new Vector3(18, .5f, 30), wall);
            Cube("West wall", environment, new Vector3(-9, 2, 15), new Vector3(.5f, 4, 30.5f), wall);
            Cube("East wall", environment, new Vector3(9, 2, 15), new Vector3(.5f, 4, 30.5f), wall);
            Cube("South wall", environment, new Vector3(0, 2, 0), new Vector3(18, 4, .5f), wall);
            Cube("North wall", environment, new Vector3(0, 2, 30), new Vector3(18, 4, .5f), wall);
            Partition(environment, 10, wall); Partition(environment, 24, wall);
            var lightRoot = Node("Lighting", environment);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.6f, .64f, .69f);
            for (int i = 0; i < 3; i++)
            {
                var light = Node("Room light " + i, lightRoot).AddComponent<Light>(); light.type = LightType.Point;
                light.range = 18; light.intensity = 3.5f; light.transform.position = new Vector3(0, 3.6f, 4 + 11 * i);
            }
            var spawn = Node("PlayerSpawn", players).transform; spawn.position = new Vector3(0, .02f, 2);
            var playerObject = Node("Player", players); playerObject.transform.position = spawn.position;
            var cc = playerObject.AddComponent<CharacterController>(); cc.height = 1.8f; cc.radius = .32f; cc.center = Vector3.up * .9f; cc.stepOffset = .25f; cc.skinWidth = .025f;
            var player = playerObject.AddComponent<PlayerController>();
            var camera = Node("First Person Camera", playerObject).AddComponent<Camera>(); camera.transform.localPosition = Vector3.up * 1.65f;
            camera.nearClipPlane = .06f; camera.farClipPlane = 100; camera.fieldOfView = 75; camera.tag = "MainCamera"; camera.gameObject.AddComponent<AudioListener>(); player.View = camera.transform;
            var scanner = playerObject.AddComponent<TemporalScanner>(); scanner.View = camera.transform; scanner.Range = 12; player.Scanner = scanner;
            var doorOne = Door("Door_01", mechanisms, 10, red, green); var doorTwo = Door("Door_02", mechanisms, 24, red, green);
            var hidden = Phase("HiddenNode_01", "L02_HiddenNode_01", mechanisms, new Vector3(2.2f, 1.65f, 9.48f), new Vector3(.9f, .9f, .24f), PhaseNodeMode.Impulse, discovered, inactive, active);
            Cube("Hidden device housing", mechanisms, new Vector3(2.2f, 1.65f, 9.66f), new Vector3(1.15f, 1.15f, .08f), wall);
            var a = Phase("PhaseNode_A", "L02_PhaseNode_A", mechanisms, new Vector3(-8.5f, 1.65f, 17), new Vector3(.24f, .9f, .9f), PhaseNodeMode.Continuous, discovered, inactive, active);
            var b = Phase("PhaseNode_B", "L02_PhaseNode_B", mechanisms, new Vector3(8.5f, 1.65f, 17), new Vector3(.24f, .9f, .9f), PhaseNodeMode.Continuous, discovered, inactive, active);
            Cube("Node A housing", mechanisms, new Vector3(-8.68f, 1.65f, 17), new Vector3(.08f, 1.15f, 1.15f), wall);
            Cube("Node B housing", mechanisms, new Vector3(8.68f, 1.65f, 17), new Vector3(.08f, 1.15f, 1.15f), wall);
            var exit = Node("LevelExit", mechanisms).AddComponent<LevelCompletion>(); exit.transform.position = new Vector3(0, 0, 28);
            Cube("Exit pad", mechanisms, new Vector3(0, .025f, 28), new Vector3(4, .05f, 3), green);
            Sign("Start instructions", environment, new Vector3(-5.1f, 2.25f, 9.65f), "ECHO / 02\nСКРЫТЫЙ СИГНАЛ\nПКМ — СКАНЕР", new Vector2(6.2f, 2.2f), 4, UITheme.Text, backing);
            Sign("Node A label", environment, new Vector3(-8.36f, 2.9f, 17), "УЗЕЛ A", new Vector2(3.5f, .8f), 5, UITheme.Cyan, backing, -90);
            Sign("Node B label", environment, new Vector3(8.36f, 2.9f, 17), "УЗЕЛ B", new Vector2(3.5f, .8f), 5, UITheme.Cyan, backing, 90);
            Sign("Resonance title", environment, new Vector3(-5, 2.4f, 23.65f), "ДВОЙНОЙ\nРЕЗОНАНС", new Vector2(5, 1.8f), 5, UITheme.Text, backing);
            var board = Sign("Phase controller panel", mechanisms, new Vector3(4.8f, 2.2f, 23.65f), "", new Vector2(6.3f, 2.1f), 3.1f, UITheme.Cyan, backing);
            Sign("Exit label", environment, new Vector3(0, 2, 29.65f), "ВЫХОД", new Vector2(4, 1.2f), 7, UITheme.Cyan, backing);
            var game = Node("GameManager", managers).AddComponent<GameManager>();
            var cycles = Node("TimeCycleManager", managers).AddComponent<TimeCycleManager>();
            var echoes = Node("EchoManager", managers).AddComponent<EchoManager>(); echoes.MaximumEchoes = 1;
            echoes.Container = Node("EchoContainer", runtime).transform;
            var echoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ECHO/Prefabs/Echoes/Echo.prefab");
            if (!echoPrefab) throw new System.InvalidOperationException("ECHO: отсутствует существующий Echo.prefab.");
            echoes.Prefab = echoPrefab.GetComponent<EchoPlayback>();
            var puzzle = Node("Level02PuzzleController", managers).AddComponent<DualPhasePuzzleController>();
            puzzle.HiddenNode = hidden; puzzle.NodeA = a; puzzle.NodeB = b; puzzle.FirstDoor = doorOne; puzzle.FinalDoor = doorTwo; puzzle.RequiredOverlap = 3; puzzle.ControlPanel = board;
            var world = Node("ScanSimulation", managers).AddComponent<ScanSimulation>();
            world.Game = game; world.Cycles = cycles; world.PlayerScanner = scanner; world.Nodes = new[] { hidden, a, b }; world.PlayerBeamMaterial = playerBeam; world.EchoBeamMaterial = echoBeam;
            game.Cycles = cycles; cycles.Game = game; cycles.Player = player; cycles.Spawn = spawn; cycles.Echoes = echoes;
            cycles.Recorder = playerObject.AddComponent<EchoRecorder>(); cycles.Exit = exit; cycles.Scanning = world; cycles.PhasePuzzle = puzzle;
            cycles.LevelName = "02 · Скрытый сигнал"; cycles.Duration = 30; cycles.AutoStartRecording = true;
            cycles.UI = EchoUIBuilder.Install(game, cycles);
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes); scenes.RemoveAll(s => s.path == ScenePath); scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray(); AssetDatabase.SaveAssets();
            Debug.Log("ECHO: Level 02 «Скрытый сигнал» создан и сохранён. Level_01 не изменён.");
        }
        internal static GameObject Node(string name, GameObject parent = null)
        { var go = new GameObject(name); if (parent) go.transform.SetParent(parent.transform, false); return go; }
        internal static Material Existing(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/_ECHO/Materials/Greybox/" + name + ".mat");
        static Material Material(string name, Color color, bool beam = false)
        {
            string path = "Assets/_ECHO/Materials/Scanner/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", color);
            if (beam)
            {
                mat.SetFloat("_Surface", 1); mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)BlendMode.One);
                mat.SetFloat("_ZWrite", 0); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.renderQueue = 3000;
            }
            EditorUtility.SetDirty(mat); return mat;
        }
        internal static GameObject Cube(string name, GameObject parent, Vector3 position, Vector3 size, Material material)
        { var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent.transform, false); go.transform.position = position; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = material; return go; }
        static void Partition(GameObject parent, float z, Material material)
        {
            Cube("Partition west " + z, parent, new Vector3(-5.25f, 2, z), new Vector3(7.5f, 4, .5f), material);
            Cube("Partition east " + z, parent, new Vector3(5.25f, 2, z), new Vector3(7.5f, 4, .5f), material);
            Cube("Lintel " + z, parent, new Vector3(0, 3.75f, z), new Vector3(3, .5f, .5f), material);
        }
        internal static DoorController Door(string name, GameObject parent, float z, Material red, Material green)
        {
            var go = Node(name, parent); go.transform.position = new Vector3(0, 0, z); var door = go.AddComponent<DoorController>();
            door.Panel = Cube("Sliding panel", go, new Vector3(0, 1.7f, z), new Vector3(3, 3.4f, .4f), red).transform;
            door.Indicator = Cube("Status", go, new Vector3(0, 3.5f, z - .4f), new Vector3(2.8f, .14f, .1f), red).GetComponent<Renderer>();
            door.ClosedMaterial = red; door.OpenMaterial = green; door.ResetMechanism(); return door;
        }
        static PhaseNode Phase(string name, string id, GameObject parent, Vector3 position, Vector3 size, PhaseNodeMode mode, Material discovered, Material inactive, Material active)
        {
            var go = Cube(name, parent, position, size, discovered); go.GetComponent<Collider>().isTrigger = true;
            var node = go.AddComponent<PhaseNode>(); node.StableId = id; node.DisplayName = "ФАЗОВЫЙ УЗЕЛ"; node.Mode = mode;
            node.Visual = go.GetComponent<Renderer>(); node.DiscoveredMaterial = discovered; node.InactiveMaterial = inactive; node.ActiveMaterial = active; node.ResetNode(); return node;
        }
        internal static TMP_Text Sign(string name, GameObject parent, Vector3 position, string content, Vector2 size, float fontSize, Color color, Material backing, float yaw = 0)
        {
            var root = Node(name, parent); root.transform.position = position; root.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var panel = Cube("Backing", root, position + root.transform.forward * .04f, new Vector3(size.x, size.y, .05f), backing);
            panel.transform.rotation = root.transform.rotation; Object.DestroyImmediate(panel.GetComponent<Collider>());
            var textObject = Node("Text", root); textObject.transform.localPosition = -Vector3.forward * .01f;
            var text = textObject.AddComponent<TextMeshPro>(); text.font = font; text.fontSize = fontSize; text.color = color;
            text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.Normal;
            text.rectTransform.sizeDelta = size - new Vector2(.25f, .2f); text.text = content; return text;
        }
    }
}
