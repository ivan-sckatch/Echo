using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace EchoGame.Editor
{
    public static class Level01Builder
    {
        public const string ScenePath = "Assets/_ECHO/Scenes/Levels/Level_01.unity";
        [MenuItem("Tools/ECHO/Build Level 01")]
        public static void Build()
        {
            // A fresh scene is generated, rather than appending to the currently open scene.
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            System.IO.Directory.CreateDirectory("Assets/_ECHO/Scenes/Levels");
            System.IO.Directory.CreateDirectory("Assets/_ECHO/Materials/Greybox");
            System.IO.Directory.CreateDirectory("Assets/_ECHO/Prefabs/Echoes");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = Node("Level_01");
            var managers = Node("_Managers", root);
            var environment = Node("_Environment", root);
            var gameplay = Node("_Gameplay", root);
            var runtime = Node("_Runtime", root);
            var uiRoot = Node("_UI", root);
            var floor = Material("Floor", new Color(.65f, .67f, .69f));
            var walls = Material("Walls", new Color(.22f, .25f, .29f));
            var red = Material("Locked", new Color(.75f, .12f, .12f));
            var green = Material("Active", new Color(.15f, .85f, .35f));
            var yellow = Material("Plate", new Color(.95f, .7f, .06f));
            var cyan = Material("Echo", new Color(.1f, .8f, 1, .42f), true);
            Cube("Floor", environment, new Vector3(0, -.25f, 12), new Vector3(20, .5f, 24), floor);
            var wallRoot = Node("Walls", environment);
            Cube("West", wallRoot, new Vector3(-10, 2, 12), new Vector3(.5f, 4, 24.5f), walls);
            Cube("East", wallRoot, new Vector3(10, 2, 12), new Vector3(.5f, 4, 24.5f), walls);
            Cube("South", wallRoot, new Vector3(0, 2, 0), new Vector3(20, 4, .5f), walls);
            Cube("North", wallRoot, new Vector3(0, 2, 24), new Vector3(20, 4, .5f), walls);
            Cube("Partition left", wallRoot, new Vector3(-5.75f, 2, 14), new Vector3(8.5f, 4, .6f), walls);
            Cube("Partition right", wallRoot, new Vector3(5.75f, 2, 14), new Vector3(8.5f, 4, .6f), walls);
            Cube("Lintel", wallRoot, new Vector3(0, 3.75f, 14), new Vector3(3, .5f, .6f), walls);
            // A real ceiling prevents jumping over the perimeter and gives the room readable volume.
            Cube("Ceiling", environment, new Vector3(0, 4.25f, 12), new Vector3(20, .5f, 24), walls);
            var lights = Node("Lighting", environment);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.65f, .68f, .72f);
            for (int i = 0; i < 3; i++)
            {
                var light = Node("Room light " + i, lights).AddComponent<Light>();
                light.type = LightType.Point; light.range = 18; light.intensity = 3;
                light.transform.position = new Vector3(0, 3.6f, 4 + i * 8);
            }
            var spawn = Node("PlayerSpawn", gameplay).transform;
            spawn.position = new Vector3(0, .02f, 2);
            var playerObject = Node("Player", gameplay);
            playerObject.transform.position = spawn.position;
            var cc = playerObject.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = .32f; cc.center = new Vector3(0, .9f, 0);
            cc.stepOffset = .25f; cc.skinWidth = .025f;
            var player = playerObject.AddComponent<PlayerController>();
            var camera = Node("First Person Camera", playerObject).AddComponent<Camera>();
            camera.transform.localPosition = Vector3.up * 1.65f;
            camera.nearClipPlane = .06f; camera.farClipPlane = 100; camera.fieldOfView = 75;
            camera.tag = "MainCamera"; camera.gameObject.AddComponent<AudioListener>();
            player.View = camera.transform;
            var plateObject = Node("PressurePlate_01", gameplay);
            plateObject.transform.position = new Vector3(6, 0, 9);
            var plate = plateObject.AddComponent<PressurePlate>();
            plate.Surface = Cube("Surface", plateObject, new Vector3(6, .05f, 9), new Vector3(2.5f, .1f, 2.5f), yellow).GetComponent<Renderer>();
            plate.InactiveMaterial = yellow; plate.ActiveMaterial = green;
            var doorObject = Node("Door_01", gameplay);
            doorObject.transform.position = new Vector3(0, 0, 14);
            var door = doorObject.AddComponent<DoorController>();
            door.Panel = Cube("Sliding panel", doorObject, new Vector3(0, 1.7f, 14), new Vector3(3, 3.4f, .45f), red).transform;
            door.Indicator = Cube("Status indicator", doorObject, new Vector3(0, 3.52f, 13.6f), new Vector3(2.8f, .16f, .12f), red).GetComponent<Renderer>();
            door.ClosedMaterial = red; door.OpenMaterial = green;
            var exitObject = Node("LevelExit", gameplay);
            exitObject.transform.position = new Vector3(0, 0, 20);
            var exit = exitObject.AddComponent<LevelCompletion>();
            Cube("Exit pad", exitObject, new Vector3(0, .025f, 20), new Vector3(4, .05f, 3), green);
            Sign("Door instructions", gameplay, new Vector3(-5.5f, 2.15f, 13.57f), "ECHO / 01\nПЕРВЫЙ\nОТГОЛОСОК", Color.white, new Vector2(6.8f, 2.6f));
            Sign("Plate label", gameplay, new Vector3(6, 2.1f, 11), "ПЛАТФОРМА\nR — СОЗДАТЬ ЭХО", new Color(1, .86f, .35f), new Vector2(5.2f, 1.6f));
            Sign("Exit label", gameplay, new Vector3(0, 2, 23.57f), "ВЫХОД", new Color(.5f, 1, .65f), new Vector2(4, 1.3f));
            var template = Node("Echo");
            var playback = template.AddComponent<EchoPlayback>();
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Translucent body"; body.transform.SetParent(template.transform, false);
            body.transform.localPosition = Vector3.up * .9f; body.transform.localScale = new Vector3(.64f, .9f, .64f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = cyan;
            body.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            playback.Body = body.transform;
            var prefab = PrefabUtility.SaveAsPrefabAsset(template, "Assets/_ECHO/Prefabs/Echoes/Echo.prefab");
            Object.DestroyImmediate(template);
            var game = Node("GameManager", managers).AddComponent<GameManager>();
            var cycles = Node("TimeCycleManager", managers).AddComponent<TimeCycleManager>();
            var recorder = playerObject.AddComponent<EchoRecorder>();
            var echoes = Node("EchoManager", managers).AddComponent<EchoManager>();
            echoes.Container = Node("EchoContainer", runtime).transform;
            echoes.Prefab = prefab.GetComponent<EchoPlayback>();
            var puzzle = Node("PuzzleController", managers).AddComponent<PuzzleController>();
            puzzle.Plate = plate; puzzle.Door = door;
            var ui = Node("GameplayCanvas", uiRoot).AddComponent<GameplayUI>();
            ui.Game = game; ui.Cycles = cycles;
            game.Cycles = cycles;
            cycles.Game = game; cycles.Player = player; cycles.Spawn = spawn; cycles.Recorder = recorder;
            cycles.Echoes = echoes; cycles.Puzzle = puzzle; cycles.Exit = exit; cycles.UI = ui;
            cycles.UI = EchoUIBuilder.Install(game, cycles);
            EditorSceneManager.SaveScene(scene, ScenePath);
            // Preserve other build scenes, replacing only this generated entry.
            var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            buildScenes.RemoveAll(s => s.path == ScenePath);
            int menuIndex = buildScenes.FindIndex(s => s.path == EchoUIBuilder.MenuPath);
            if (menuIndex >= 0) { var menu = buildScenes[menuIndex]; buildScenes.RemoveAt(menuIndex); buildScenes.Insert(0, menu); }
            buildScenes.Insert(menuIndex >= 0 ? 1 : 0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("ECHO: Level 01 generated and saved: " + ScenePath);
        }
        static GameObject Node(string name, GameObject parent = null)
        { var go = new GameObject(name); if (parent) go.transform.SetParent(parent.transform, false); return go; }
        static GameObject Cube(string name, GameObject parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent.transform, false); go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        static Material Material(string name, Color color, bool transparent = false)
        {
            string path = "Assets/_ECHO/Materials/Greybox/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", color);
            if (transparent)
            {
                mat.SetFloat("_Surface", 1); mat.SetFloat("_Blend", 0);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.renderQueue = 3000;
            }
            EditorUtility.SetDirty(mat); return mat;
        }
        static void Sign(string name, GameObject parent, Vector3 position, string text, Color color, Vector2 size)
        {
            var sign = Node(name, parent); sign.transform.position = position;
            var backing = Cube("Dark backing", sign, position + Vector3.forward * .04f,
                new Vector3(size.x, size.y, .06f), SignBacking());
            Object.DestroyImmediate(backing.GetComponent<Collider>());
            var go = Node("Text", sign); go.transform.position = position - Vector3.forward * .01f;
            var label = go.AddComponent<TextMesh>(); label.text = text; label.fontSize = 128;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = .1f; label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = color;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            string path = "Assets/_ECHO/Materials/Greybox/SignText.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("ECHO/GreyboxText")); AssetDatabase.CreateAsset(material, path); }
            material.mainTexture = label.font.material.mainTexture;
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var bounds = go.GetComponent<Renderer>().bounds.size;
            float fit = Mathf.Min((size.x - .6f) / Mathf.Max(.01f, bounds.x), (size.y - .35f) / Mathf.Max(.01f, bounds.y));
            go.transform.localScale = Vector3.one * fit;
            EditorUtility.SetDirty(material);
        }
        static Material SignBacking()
        {
            const string path = "Assets/_ECHO/Materials/Greybox/SignBacking.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", new Color(.025f, .045f, .06f));
            EditorUtility.SetDirty(mat); return mat;
        }
    }
}
