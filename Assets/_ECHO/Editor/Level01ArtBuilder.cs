using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoGame.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace EchoGame.Editor
{
    public static class Level01ArtBuilder
    {
        public const string Art = "Assets/_ECHO/Art/Level01";
        public const string Reports = "C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-02-Level01Art";
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        static readonly Dictionary<string, GameObject> modules = new Dictionary<string, GameObject>();
        static Mesh chamfer;
        static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        static GameObject Node(string name, Transform parent = null)
        { var g = new GameObject(name); g.transform.SetParent(parent, false); return g; }
        static GameObject Box(Transform parent, string name, Vector3 p, Vector3 s, string material)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name;
            Object.DestroyImmediate(g.GetComponent<Collider>()); g.transform.SetParent(parent, false);
            g.transform.localPosition = p; g.transform.localScale = s;
            if (chamfer && (name == "Upper composite" || name == "Sliding armour" || name == "Moving tread"))
                g.GetComponent<MeshFilter>().sharedMesh = chamfer;
            g.GetComponent<Renderer>().sharedMaterial = mats[material]; g.isStatic = true; return g;
        }
        static GameObject Instance(string module, Transform parent, Vector3 p, Vector3? scale = null, float yaw = 0)
        {
            var g = (GameObject)PrefabUtility.InstantiatePrefab(modules[module], parent);
            g.transform.localPosition = p; g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            if (scale.HasValue) g.transform.localScale = scale.Value;
            return g;
        }
        static Material Mat(string name, string hex, float metal = 0, float smooth = .25f, float emission = 0, bool transparent = false)
        {
            string path = Art + "/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            var color = Hex(hex); if (transparent) color.a = .36f;
            m.SetColor("_BaseColor", color); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth);
            if (emission > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * emission); }
            if (transparent)
            {
                m.SetFloat("_Surface", 1); m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = 3000;
            }
            EditorUtility.SetDirty(m); mats[name] = m; return m;
        }
        static void Materials()
        {
            Mat("Concrete", "747D87"); Mat("PaintedMetal", "747D87", .55f, .36f);
            Mat("Graphite", "252D36", .7f); Mat("Composite", "B5C0C9", .15f, .4f);
            Mat("WornMetal", "515C67", .65f, .18f); Mat("Cyan", "45E4E0", .2f, .35f, 3);
            Mat("Amber", "F3B45B", .25f, .3f, 2.6f); Mat("IdleAmber", "936732", .25f, .3f, .55f);
            Mat("Green", "76D8AA", .2f, .3f, 3); Mat("Screen", "10232C", .2f, .4f, .25f);
            Mat("Lamp", "B8D8E8", .1f, .35f, 4); Mat("EchoProjection", "45E4E0", .1f, .5f, 1.2f, true);
            var tex = new Texture2D(128, 128); var pixels = new Color[128 * 128]; var random = new System.Random(101);
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            { float n = .95f + (float)random.NextDouble() * .05f; if (y % 47 == 0 && x > 65) n *= .94f; pixels[y * 128 + x] = new Color(n, n, n); }
            tex.SetPixels(pixels); tex.Apply(); string path = Art + "/Textures/SurfaceWear.png";
            File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex); AssetDatabase.ImportAsset(path);
            foreach (string name in new[] { "Concrete", "WornMetal" })
            { mats[name].SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path)); mats[name].mainTextureScale = new Vector2(3, 3); }
        }
        static void ChamferMesh()
        {
            string path = Art + "/Models/ChamferedPanel.asset";
            chamfer = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (chamfer) return;
            chamfer = new Mesh { name = "ChamferedPanel" };
            var profile = new[] { new Vector2(-.44f,-.5f), new Vector2(.44f,-.5f), new Vector2(.5f,-.44f), new Vector2(.5f,.44f),
                new Vector2(.44f,.5f), new Vector2(-.44f,.5f), new Vector2(-.5f,.44f), new Vector2(-.5f,-.44f) };
            var vertices = new List<Vector3>(); var indices = new List<int>(); var uv = new List<Vector2>();
            Action<Vector3,Vector3,Vector3> tri = (a,b,c) => {
                int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
                indices.Add(n); indices.Add(n+1); indices.Add(n+2);
                uv.Add(new Vector2(a.x+.5f,a.y+.5f)); uv.Add(new Vector2(b.x+.5f,b.y+.5f)); uv.Add(new Vector2(c.x+.5f,c.y+.5f)); };
            for (int i=0;i<8;i++) {
                Vector2 a=profile[i], b=profile[(i+1)%8];
                var af=new Vector3(a.x,a.y,-.5f); var bf=new Vector3(b.x,b.y,-.5f);
                var ab=new Vector3(a.x,a.y,.5f); var bb=new Vector3(b.x,b.y,.5f);
                tri(new Vector3(0,0,-.5f),bf,af); tri(new Vector3(0,0,.5f),ab,bb);
                tri(af,bf,bb); tri(af,bb,ab);
            }
            chamfer.SetVertices(vertices); chamfer.SetTriangles(indices,0); chamfer.SetUVs(0,uv); chamfer.RecalculateNormals(); chamfer.RecalculateBounds();
            AssetDatabase.CreateAsset(chamfer,path);
        }
        static TextMesh Label(Transform parent, string text, Vector3 position, float size = .1f, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = Node("Legend", parent); go.transform.localPosition = position;
            var t = go.AddComponent<TextMesh>(); t.text = text; t.fontSize = 96; t.characterSize = size;
            t.anchor = anchor; t.alignment = TextAlignment.Center; t.color = Hex("B5C0C9");
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_ECHO/Materials/Greybox/SignText.mat");
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off; return t;
        }
        static void Module(string name, Action<Transform> build)
        {
            var g = Node(name); build(g.transform);
            modules[name] = PrefabUtility.SaveAsPrefabAsset(g, Art + "/Prefabs/" + name + ".prefab"); Object.DestroyImmediate(g);
        }
        static void Fit(TextMesh label, float width, float height)
        {
            label.transform.localScale = Vector3.one;
            var size = label.GetComponent<Renderer>().localBounds.size;
            label.transform.localScale = Vector3.one * Mathf.Min(width / Mathf.Max(.01f, size.x), height / Mathf.Max(.01f, size.y));
        }
        static void Modules()
        {
            Module("WallPanel", t => {
                Box(t, "Recess", Vector3.zero, new Vector3(2.36f, 2.8f, .12f), "Graphite");
                Box(t, "Upper composite", new Vector3(0, .65f, -.08f), new Vector3(2.22f, 1.25f, .08f), "Composite");
                Box(t, "Lower concrete", new Vector3(0, -.65f, -.08f), new Vector3(2.22f, 1.25f, .08f), "Concrete");
                Box(t, "Service seam", new Vector3(0, 0, -.13f), new Vector3(2.1f, .045f, .025f), "WornMetal");
                foreach (float x in new[] { -1.02f, 1.02f }) foreach (float y in new[] { -1.17f, 1.17f })
                    Box(t, "Fastener", new Vector3(x, y, -.14f), new Vector3(.045f, .045f, .02f), "Graphite"); });
            Module("WallTrim", t => Box(t, "Joint", Vector3.zero, new Vector3(.07f, 3.6f, .15f), "WornMetal"));
            Module("FloorPanel", t => {
                Box(t, "Tile", Vector3.zero, new Vector3(1.96f, .012f, 1.96f), "PaintedMetal");
                Box(t, "Inset", new Vector3(0, .007f, 0), new Vector3(1.8f, .004f, 1.8f), "Concrete"); });
            Module("CeilingPanel", t => {
                Box(t, "Ceiling cassette", Vector3.zero, new Vector3(2.36f, .08f, 3.8f), "Graphite");
                for (int i = -2; i <= 2; i++) Box(t, "Cooling rib", new Vector3(i * .38f, -.05f, 0), new Vector3(.025f, .06f, 3.6f), "WornMetal"); });
            Module("LightStrip", t => {
                Box(t, "Recess", Vector3.zero, new Vector3(1, .035f, .14f), "Graphite");
                Box(t, "Diffuser", new Vector3(0, .021f, 0), new Vector3(.9f, .012f, .055f), "Cyan"); });
            Module("CeilingLamp", t => {
                Box(t, "Housing", Vector3.zero, new Vector3(2.5f, .14f, .7f), "WornMetal");
                Box(t, "Diffuser", new Vector3(0, -.08f, 0), new Vector3(2.25f, .025f, .48f), "Lamp");
                for (int i = -2; i <= 2; i++) Box(t, "Guard", new Vector3(i * .45f, -.105f, 0), new Vector3(.035f, .03f, .53f), "Graphite"); });
            Module("SupportColumn", t => {
                Box(t, "Pier", Vector3.up * 1.9f, new Vector3(.32f, 3.8f, .5f), "WornMetal");
                Box(t, "Cap", Vector3.up * 3.65f, new Vector3(.55f, .3f, .7f), "Graphite");
                Box(t, "Foot", Vector3.up * .2f, new Vector3(.48f, .4f, .6f), "Graphite"); });
            Module("VentilationGrille", t => {
                Box(t, "Frame", Vector3.zero, new Vector3(1.3f, .6f, .08f), "Graphite");
                for (int i = 0; i < 7; i++) Box(t, "Louvre", new Vector3(0, -.24f + i * .08f, -.07f), new Vector3(1.15f, .035f, .06f), "WornMetal"); });
            Module("CableChannel", t => {
                Box(t, "Trunk", Vector3.zero, new Vector3(1, .12f, .16f), "Graphite");
                Box(t, "Cover", new Vector3(0, 0, -.1f), new Vector3(.9f, .07f, .06f), "WornMetal"); });
            Module("DoorFrame", t => {
                foreach (float x in new[] { -1.69f, 1.69f }) {
                    Box(t, "Jamb", new Vector3(x, 1.8f, 0), new Vector3(.32f, 3.6f, .8f), "WornMetal");
                    Box(t, "Guide rail", new Vector3(x, 1.8f, -.43f), new Vector3(.065f, 3.4f, .08f), "Composite"); }
                Box(t, "Header", new Vector3(0, 3.7f, 0), new Vector3(3.7f, .5f, .85f), "Graphite"); });
            Module("PressurePlateVisual", t => {
                Box(t, "Foundation", new Vector3(0, .04f, 0), new Vector3(2.5f, .08f, 2.5f), "Graphite");
                var deck = Node("Deck", t); deck.transform.localPosition = Vector3.up * .145f;
                Box(deck.transform, "Moving tread", Vector3.zero, new Vector3(2.13f, .045f, 2.13f), "WornMetal");
                for (int i = -3; i <= 3; i++) Box(deck.transform, "Grip", new Vector3(i * .25f, .025f, 0), new Vector3(.025f, .008f, 1.75f), "Composite");
                foreach (float x in new[] { -1.17f, 1.17f }) Box(t, "Signal", new Vector3(x, .09f, 0), new Vector3(.06f, .025f, 2.2f), "IdleAmber");
                foreach (float z in new[] { -1.17f, 1.17f }) Box(t, "Signal", new Vector3(0, .09f, z), new Vector3(2.2f, .025f, .06f), "IdleAmber"); });
            Module("SignalLine", t => Box(t, "Signal", Vector3.zero, new Vector3(1, .012f, .06f), "IdleAmber"));
            Module("StatusIndicator", t => {
                Box(t, "Status housing", Vector3.zero, new Vector3(2.6f, .26f, .09f), "Graphite");
                for (int i = -3; i <= 3; i++) Box(t, "Signal", new Vector3(i * .34f, 0, -.06f), new Vector3(.26f, .1f, .025f), "IdleAmber"); });
            Module("WallSign", t => {
                Box(t, "Enclosure", Vector3.zero, new Vector3(3.6f, 1.2f, .1f), "Graphite");
                Box(t, "Screen", new Vector3(0, 0, -.06f), new Vector3(3.4f, 1.02f, .025f), "Screen");
                Label(t, "TEST SECTOR 01", new Vector3(0, 0, -.09f), .065f); });
            Module("ExitMarker", t => {
                Box(t, "Exit header", Vector3.zero, new Vector3(4.8f, 1.1f, .13f), "Graphite");
                Label(t, "EXIT / SECTOR 02", new Vector3(0, 0, -.1f), .075f);
                Box(t, "Completion band", new Vector3(0, -.48f, -.08f), new Vector3(4.6f, .05f, .04f), "Green"); });
        }
        [MenuItem("Tools/ECHO/Art/Apply Level 01")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before applying art.");
            if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene before applying art.");
            Directory.CreateDirectory(Reports);
            foreach (string folder in new[] { "Materials", "Models", "Prefabs", "Textures", "VFX" }) Directory.CreateDirectory(Art + "/" + folder);
            AssetDatabase.Refresh(); Materials(); ChamferMesh(); Modules();
            var scene = EditorSceneManager.OpenScene(Level01Builder.ScenePath);
            string backup = Reports + "/Level_01.before-art." + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity";
            File.Copy(Level01Builder.ScenePath, backup);
            var cycles = Object.FindAnyObjectByType<TimeCycleManager>();
            var before = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Select(c => c.GetEntityId().ToString()).OrderBy(i => i).ToArray();
            var old = GameObject.Find("Level01_ArtRoot"); if (old) Object.DestroyImmediate(old);
            var root = Node("Level01_ArtRoot").transform;
            var arch = Node("Architecture", root).transform; var floor = Node("FloorAndCeiling", root).transform;
            var props = Node("DecorativeProps", root).transform; var lighting = Node("Lighting", root).transform;
            var signs = Node("Signs", root).transform; var mechanisms = Node("MechanismVisuals", root).transform;
            Node("VFX", root); Node("EnvironmentAudio", root);
            // Existing structural geometry and physics remain in place.
            foreach (var r in GameObject.Find("_Environment").GetComponentsInChildren<Renderer>())
                r.sharedMaterial = mats[r.name == "Floor" ? "Graphite" : "Concrete"];
            for (int x = -9; x <= 9; x += 2) for (int z = 1; z <= 23; z += 2)
                Instance("FloorPanel", floor, new Vector3(x, .008f, z));
            foreach (float side in new[] { -1f, 1f }) for (int i = 0; i < 10; i++)
            {
                float z = 1.3f + i * 2.36f;
                Instance("WallPanel", arch, new Vector3(side * 9.64f, 1.9f, z), null, side < 0 ? -90 : 90);
                Instance("WallTrim", arch, new Vector3(side * 9.58f, 1.9f, z + 1.18f), null, 90);
                if (i % 2 == 0) {
                    Instance("SupportColumn", arch, new Vector3(side * 9.35f, 0, z));
                    Instance("VentilationGrille", props, new Vector3(side * 9.42f, .7f, z + .55f), null, side < 0 ? -90 : 90); }
                Instance("CableChannel", props, new Vector3(side * 9.4f, 3.5f, z), new Vector3(2.3f, 1, 1), side < 0 ? -90 : 90);
            }
            foreach (float wallZ in new[] { .36f, 23.64f }) for (int i = -3; i <= 3; i++)
                Instance("WallPanel", arch, new Vector3(i * 2.4f, 1.9f, wallZ), null, wallZ < 1 ? 180 : 0);
            foreach (float side in new[] { -1f, 1f }) foreach (float z in new[] { 13.56f, 14.44f }) for (int i = 0; i < 3; i++)
                Instance("WallPanel", arch, new Vector3(side * (2.9f + i * 2.4f), 1.9f, z), null, z > 14 ? 180 : 0);
            for (int x = -8; x <= 8; x += 4) for (int z = 2; z <= 22; z += 4)
                Instance("CeilingPanel", floor, new Vector3(x, 3.94f, z));
            // The four areas are visual zones in the original single-puzzle plan.
            foreach (float z in new[] { 4.5f, 7f, 18f }) {
                Box(arch, "Overhead portal", new Vector3(0, 3.72f, z), new Vector3(19, .3f, .24f), "Graphite");
                foreach (float x in new[] { -2.3f, 2.3f }) Instance("LightStrip", floor, new Vector3(x, .03f, z), new Vector3(2.6f, 1, 1), 90); }
            for (int z = 2; z <= 22; z += 4) foreach (float x in new[] { -5f, 5f })
                Instance("CeilingLamp", lighting, new Vector3(x, 3.84f, z));
            foreach (var l in GameObject.Find("_Environment").GetComponentsInChildren<Light>()) l.enabled = false;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Hex("B8D8E8") * .48f;
            for (int i = 0; i < 3; i++) {
                var l = Node("Sector fill " + i, lighting).AddComponent<Light>(); l.type = LightType.Point;
                l.transform.position = new Vector3(i == 1 ? 4 : -2, 3.5f, 3 + 8 * i);
                l.range = 13; l.intensity = 2.6f; l.color = Hex("B8D8E8"); l.shadows = i == 1 ? LightShadows.Soft : LightShadows.None;
            }
            var camera = cycles.Player.View.GetComponent<Camera>(); camera.backgroundColor = Hex("252D36"); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = true; camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Art + "/VFX/Level01Atmosphere.asset");
            if (!profile) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, Art + "/VFX/Level01Atmosphere.asset"); }
            if (!profile.TryGet<Bloom>(out var bloom)) { bloom = profile.Add<Bloom>(); AssetDatabase.AddObjectToAsset(bloom, profile); }
            bloom.threshold.Override(1.1f); bloom.intensity.Override(.18f); bloom.scatter.Override(.35f);
            if (!profile.TryGet<Tonemapping>(out var tone)) { tone = profile.Add<Tonemapping>(); AssetDatabase.AddObjectToAsset(tone, profile); }
            tone.mode.Override(TonemappingMode.Neutral);
            var volume = Node("Atmosphere", lighting).AddComponent<Volume>(); volume.isGlobal = true; volume.sharedProfile = profile; EditorUtility.SetDirty(profile);
            var sector = Instance("WallSign", signs, new Vector3(-8.98f, 2.6f, 2.8f), null, -90); sector.GetComponentInChildren<TextMesh>().text = "TEST SECTOR 01\nTEMPORAL RESEARCH";
            Instance("WallSign", signs, new Vector3(8.98f, 2.6f, 5.8f), null, 90).GetComponentInChildren<TextMesh>().text = "CHAMBER 01-A\nLOAD INTERFACE";
            Instance("WallSign", signs, new Vector3(-5.5f, 2.6f, 13.4f)).GetComponentInChildren<TextMesh>().text = "CHAMBER 01-B\nTEMPORAL TEST";
            Instance("ExitMarker", signs, new Vector3(0, 2.7f, 23.38f));
            foreach (var label in signs.GetComponentsInChildren<TextMesh>()) Fit(label, label.transform.parent.name == "ExitMarker" ? 4.25f : 3.05f, .8f);
            foreach (string name in new[] { "Door instructions", "Plate label", "Exit label" }) {
                var g = GameObject.Find(name); if (g) foreach (var r in g.GetComponentsInChildren<Renderer>()) r.enabled = false; }
            var plate = cycles.Puzzle.Plate; var door = cycles.Puzzle.Door;
            plate.Surface.enabled = false;
            var pv = Instance("PressurePlateVisual", mechanisms, plate.transform.position);
            var adapter = pv.AddComponent<MechanismArt>(); adapter.Plate = plate; adapter.Door = door; adapter.Deck = pv.transform.Find("Deck");
            Instance("DoorFrame", mechanisms, door.transform.position);
            door.ClosedMaterial = mats["IdleAmber"]; door.OpenMaterial = mats["Green"]; door.Indicator.sharedMaterial = mats["IdleAmber"];
            door.Panel.GetComponent<Renderer>().sharedMaterial = mats["Graphite"];
            var oldSkin = door.Panel.Find("Level01_DoorSkin"); if (oldSkin) Object.DestroyImmediate(oldSkin.gameObject);
            var skin = Node("Level01_DoorSkin", door.Panel).transform;
            // Inverse scale keeps visual dimensions in metres while inheriting the controller's motion.
            skin.localScale = new Vector3(1 / door.Panel.lossyScale.x, 1 / door.Panel.lossyScale.y, 1 / door.Panel.lossyScale.z);
            foreach (float face in new[] { -1f, 1f }) {
                for (int row = 0; row < 4; row++) foreach (float x in new[] { -.73f, .73f })
                    Box(skin, "Sliding armour", new Vector3(x, -1.26f + row * .83f, face * .255f), new Vector3(1.4f, .76f, .08f), "PaintedMetal");
                Box(skin, "Central seam", new Vector3(0, 0, face * .31f), new Vector3(.035f, 3.25f, .025f), "Amber"); }
            var indicator = Instance("StatusIndicator", mechanisms, new Vector3(0, 3.68f, 13.46f));
            adapter.Status = Label(mechanisms, "01 / LOCKED", new Vector3(0, 3.3f, 13.4f), .055f);
            Fit(adapter.Status, 1.8f, .18f);
            var signals = new List<Renderer>();
            signals.AddRange(pv.GetComponentsInChildren<Renderer>().Where(r => r.name == "Signal"));
            signals.AddRange(indicator.GetComponentsInChildren<Renderer>().Where(r => r.name == "Signal"));
            foreach (var segment in new[] { new Vector4(6, 9, 6, 12.6f), new Vector4(6, 12.6f, 1.85f, 12.6f), new Vector4(1.85f, 12.6f, 1.85f, 13.45f) }) {
                var a = new Vector3(segment.x, .035f, segment.y); var b = new Vector3(segment.z, .035f, segment.w);
                var line = Instance("SignalLine", mechanisms, (a + b) * .5f, new Vector3(Vector3.Distance(a, b), 1, 1), Mathf.Abs(a.x - b.x) < .01f ? 90 : 0);
                signals.AddRange(line.GetComponentsInChildren<Renderer>()); }
            adapter.Signals = signals.ToArray(); adapter.Idle = mats["IdleAmber"]; adapter.Active = mats["Green"];
            foreach (var r in skin.GetComponentsInChildren<Renderer>()) r.gameObject.isStatic = false;
            foreach (var r in adapter.Deck.GetComponentsInChildren<Renderer>()) r.gameObject.isStatic = false;
            // Exit remains the existing LevelCompletion trigger, with a distinct architectural frame.
            foreach (float x in new[] { -2.4f, 2.4f }) {
                Box(arch, "Exit reveal", new Vector3(x, 1.7f, 22.7f), new Vector3(.2f, 3.4f, .5f), "Composite");
                Box(arch, "Exit light", new Vector3(x, 1.7f, 22.4f), new Vector3(.05f, 3.2f, .03f), "Green"); }
            cycles.Exit.GetComponentInChildren<Renderer>().sharedMaterial = mats["Graphite"];
            cycles.Echoes.MaximumEchoes = 1;
            MakeEcho(cycles);
            var after = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Select(c => c.GetEntityId().ToString()).OrderBy(i => i).ToArray();
            if (!before.SequenceEqual(after)) throw new InvalidOperationException("Art must preserve every existing collider.");
            Capture();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllText(Reports + "/apply.txt", "Applied art to Level_01. Original colliders preserved: " + before.Length + "\nPipeline: " + GraphicsSettings.currentRenderPipeline + "\n" + SystemInfo.graphicsDeviceName + "\n" + SystemInfo.processorType);
            Debug.Log("ECHO Level01 art saved; collider identities preserved: " + before.Length);
        }
        static void MakeEcho(TimeCycleManager cycles)
        {
            // Level-specific prefab avoids changing the projection used by later levels.
            var g = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ECHO/Prefabs/Echoes/Echo.prefab")); g.name = "Echo_Level01";
            var playback = g.GetComponent<EchoPlayback>();
            Object.DestroyImmediate(playback.Body.gameObject);
            var body = Node("Projection silhouette", g.transform).transform; body.localPosition = Vector3.up * .9f; playback.Body = body;
            Action<string, Vector3, Vector3, PrimitiveType> limb = (name, position, scale, type) => {
                var part = GameObject.CreatePrimitive(type); part.name = name; part.transform.SetParent(body, false);
                part.transform.localPosition = position; part.transform.localScale = scale; Object.DestroyImmediate(part.GetComponent<Collider>());
                var renderer = part.GetComponent<Renderer>(); renderer.sharedMaterial = mats["EchoProjection"]; renderer.shadowCastingMode = ShadowCastingMode.Off;
            };
            limb("Head", new Vector3(0, .68f, 0), Vector3.one * .28f, PrimitiveType.Sphere);
            limb("Torso", new Vector3(0, .22f, 0), new Vector3(.42f, .34f, .25f), PrimitiveType.Capsule);
            limb("Pelvis", new Vector3(0, -.2f, 0), new Vector3(.34f, .14f, .24f), PrimitiveType.Sphere);
            foreach (float side in new[] { -1f, 1f }) {
                limb("Arm", new Vector3(side * .29f, .1f, 0), new Vector3(.12f, .325f, .14f), PrimitiveType.Capsule);
                limb("Leg", new Vector3(side * .105f, -.55f, 0), new Vector3(.16f, .325f, .18f), PrimitiveType.Capsule);
            }
            var v = g.AddComponent<EchoProjectionArt>(); v.Playback = playback;
            // Thin scan bands keep the body readable without particle occlusion.
            for (int i = 0; i < 9; i++) {
                var band = Box(body, "Scan band", new Vector3(0, -.25f + i * .095f, -.145f), new Vector3(.34f, .006f, .012f), "Cyan");
                band.isStatic = false; band.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off; }
            v.Silhouette = body.GetComponentsInChildren<Renderer>();
            var trail = Node("Temporal afterimage", g.transform).AddComponent<TrailRenderer>(); trail.transform.localPosition = Vector3.up * .9f;
            trail.sharedMaterial = mats["EchoProjection"]; trail.time = .22f; trail.startWidth = .34f; trail.endWidth = 0;
            trail.minVertexDistance = .12f; trail.startColor = new Color(.27f, .89f, .88f, .25f); trail.endColor = Color.clear;
            trail.shadowCastingMode = ShadowCastingMode.Off; trail.emitting = false; v.Trail = trail;
            var ring = Node("Arrival ring", g.transform); ring.transform.localPosition = Vector3.up * .04f;
            var lr = ring.AddComponent<LineRenderer>(); lr.sharedMaterial = mats["EchoProjection"]; lr.useWorldSpace = false;
            lr.loop = true; lr.positionCount = 40; lr.startWidth = .025f; lr.endWidth = .025f;
            for (int i = 0; i < 40; i++) { float a = i * Mathf.PI * 2 / 40; lr.SetPosition(i, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))); }
            v.ArrivalRing = ring.transform;
            var prefab = PrefabUtility.SaveAsPrefabAsset(g, Art + "/Prefabs/Echo_Level01.prefab"); Object.DestroyImmediate(g);
            cycles.Echoes.Prefab = prefab.GetComponent<EchoPlayback>();
        }
        public static void Capture()
        {
            Directory.CreateDirectory(Reports);
            var g = Node("Art review camera"); var c = g.AddComponent<Camera>(); c.CopyFrom(Object.FindAnyObjectByType<PlayerController>().View.GetComponent<Camera>());
            c.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var rt = new RenderTexture(1920, 1080, 24); c.targetTexture = rt;
            var positions = new[] { new Vector3(0, 1.67f, 2), new Vector3(-5, 1.67f, 7), new Vector3(6, 1.67f, 11), new Vector3(0, 1.67f, 18) };
            var targets = new[] { new Vector3(3, 1.6f, 13), new Vector3(6, .8f, 9), new Vector3(0, 1.7f, 14), new Vector3(0, 2, 24) };
            for (int i = 0; i < positions.Length; i++) {
                g.transform.position = positions[i]; g.transform.LookAt(targets[i]); c.Render();
                var previous = RenderTexture.active; RenderTexture.active = rt; var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply(); File.WriteAllBytes(Reports + "/View" + i + ".png", image.EncodeToPNG());
                Object.DestroyImmediate(image); RenderTexture.active = previous;
            }
            c.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(g);
        }
    }
}

