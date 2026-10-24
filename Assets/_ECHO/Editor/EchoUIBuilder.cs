using System.Collections.Generic;
using System.IO;
using System.Text;
using EchoGame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
namespace EchoGame.Editor
{
    public static class EchoUIBuilder
    {
        public const string MenuPath = "Assets/_ECHO/Scenes/MainMenu.unity";
        const string PrefabPath = "Assets/_ECHO/UI/Prefabs/EchoUI.prefab";
        static TMP_FontAsset font;
        static TMP_Text statusPrefab;
        static TimelineTrackUI trackPrefab;
        static Image segmentPrefab;
        static Sprite whiteSprite;
        public static void RefreshPrefabOnly() { Prepare(); BuildPrefab(); AssetDatabase.SaveAssets(); }
        [MenuItem("Tools/ECHO/Apply Compact UI")]
        public static void ApplyCompactUI()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            RefreshPrefabOnly();
            foreach (var path in new[] { MenuPath, Level01Builder.ScenePath, "Assets/_ECHO/Scenes/Levels/Level_02.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var ui = Object.FindAnyObjectByType<UIManager>();
                if (!ui || !ui.HUD || !ui.Timeline) throw new System.InvalidOperationException("Missing UI: " + path);
                if (path != MenuPath && !ui.DataProvider) throw new System.InvalidOperationException("Missing provider: " + path);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup);
            Debug.Log("ECHO Compact UI applied to prefab and all three scenes.");
        }
        [MenuItem("Tools/ECHO/Build UI and Main Menu")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("ECHO: остановите Play Mode перед сборкой UI."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare(); BuildPrefab();
            var menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var menuCamera = new GameObject("Menu Camera", typeof(Camera)).GetComponent<Camera>();
            menuCamera.clearFlags = CameraClearFlags.SolidColor; menuCamera.backgroundColor = UITheme.Background; menuCamera.cullingMask = 0;
            var shell = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            shell.GetComponent<UIManager>().IsMainMenu = true; EventSystemSetup();
            EditorSceneManager.SaveScene(menu, MenuPath);
            var level = EditorSceneManager.OpenScene(Level01Builder.ScenePath);
            var game = Object.FindAnyObjectByType<GameManager>();
            Install(game, game.Cycles);
            foreach (var label in Object.FindObjectsByType<TextMesh>())
                label.text = label.text.Replace("Q —", "R —");
            EditorSceneManager.SaveScene(level);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == MenuPath);
            scenes.Insert(0, new EditorBuildSettingsScene(MenuPath, true));
            EditorBuildSettings.scenes = scenes.ToArray(); AssetDatabase.SaveAssets();
            Debug.Log("ECHO UI: prefab, MainMenu и Level_01 сохранены.");
        }
        public static GameplayUI Install(GameManager game, TimeCycleManager cycles)
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) { Prepare(); BuildPrefab(); }
            Transform parent = cycles.UI ? cycles.UI.transform.parent : null;
            if (!parent) { var p = new GameObject("_UI"); p.transform.SetParent(game.transform.root); parent = p.transform; }
            foreach (var old in parent.GetComponentsInChildren<UIManager>(true)) Object.DestroyImmediate(old.gameObject);
            foreach (var old in parent.GetComponentsInChildren<GameplayUI>(true)) Object.DestroyImmediate(old.gameObject);
            foreach (var old in parent.GetComponentsInChildren<UIEventBridge>(true)) Object.DestroyImmediate(old.gameObject);
            var canvas = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), parent);
            var adapter = new GameObject("UIEventBridge"); adapter.transform.SetParent(parent, false);
            var bridge = adapter.AddComponent<UIEventBridge>(); bridge.Game = game; bridge.Cycles = cycles;
            var interaction = game.GetComponent<PlayerInteraction>();
            if (!interaction) interaction = game.gameObject.AddComponent<PlayerInteraction>();
            interaction.Game = game; interaction.View = cycles.Player.View.GetComponent<Camera>();
            game.Interaction = interaction; bridge.InteractionSource = interaction;
            canvas.GetComponent<UIManager>().DataProvider = bridge;
            var compatibility = adapter.AddComponent<GameplayUI>(); compatibility.Cycles = cycles; compatibility.Game = game; compatibility.Bridge = bridge;
            cycles.UI = compatibility;
            EventSystemSetup(); return compatibility;
        }
        static void Prepare()
        {
            Directory.CreateDirectory("Assets/_ECHO/UI/Prefabs"); Directory.CreateDirectory("Assets/_ECHO/UI/Fonts");
            Directory.CreateDirectory("Assets/_ECHO/UI/Sprites");
            if (!AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                string package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly).resolvedPath;
                UnityEditor.AssetPackage.Package.Import(Path.Combine(package, "Package Resources/TMP Essential Resources.unitypackage"), false);
            }
            AssetDatabase.Refresh();
            const string whitePath = "Assets/_ECHO/UI/Sprites/White.asset";
            whiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(whitePath);
            if (!whiteSprite)
            {
                var texture = new Texture2D(2, 2); texture.name = "UI White";
                texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); texture.Apply();
                AssetDatabase.CreateAsset(texture, whitePath);
                whiteSprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * .5f); whiteSprite.name = "White";
                AssetDatabase.AddObjectToAsset(whiteSprite, texture); AssetDatabase.SaveAssets();
            }
            const string path = "Assets/_ECHO/UI/Fonts/ECHO UI SDF.asset";
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (!font)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
                font = TMP_FontAsset.CreateFontAsset(source, 64, 9, GlyphRenderMode.SDFAA, 2048, 2048);
                if (!font) throw new System.InvalidOperationException("ECHO: не удалось создать кириллический SDF-шрифт.");
                font.name = "ECHO UI SDF";
                var characters = new StringBuilder();
                for (int i = 32; i < 256; i++) if (!char.IsControl((char)i)) characters.Append((char)i);
                for (int i = 0x400; i <= 0x45f; i++) characters.Append((char)i);
                characters.Append("—–·●×→←…");
                font.TryAddCharacters(characters.ToString(), out string missing);
                if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("ECHO font: отсутствуют символы " + missing);
                font.atlasPopulationMode = AtlasPopulationMode.Static;
                AssetDatabase.CreateAsset(font, path);
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
                EditorUtility.SetDirty(font); AssetDatabase.SaveAssets();
            }
        }
        static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            var rect = (RectTransform)go.transform; if (parent) rect.SetParent(parent, false); return rect;
        }
        static void Stretch(RectTransform rect, float left = 0, float top = 0, float right = 0, float bottom = 0)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top); }
        static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size; }
        static Image Image(string name, Transform parent, Color color)
        { var image = Rect(name, parent).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image; }
        static TMP_Text Text(string name, Transform parent, string value, int size, Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color ?? UITheme.Text;
            var shadow = text.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .65f); shadow.effectDistance = new Vector2(1, -2);
            text.alignment = alignment; text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false;
            return text;
        }
        static TMP_Text Label(Transform parent, string value, int size, float x, float y, float width, float height, Color? color = null)
        { var t = Text(value, parent, value, size, color); Place(t.rectTransform, new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height)); return t; }
        static UIPanelMotion Group(string name, Transform parent, bool background = false)
        {
            var rect = Rect(name, parent); Stretch(rect);
            var group = rect.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
            if (background) { var bg = rect.gameObject.AddComponent<Image>(); bg.color = new Color(.025f, .04f, .055f, .94f); }
            return rect.gameObject.AddComponent<UIPanelMotion>();
        }
        static RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var image = Image(name, parent, new Color(.063f, .094f, .125f, .96f));
            image.gameObject.AddComponent<UIVerticalGradient>();
            Place(image.rectTransform, anchor, position, size);
            var edge = Image("Accent", image.transform, UITheme.Cyan); Stretch(edge.rectTransform); edge.rectTransform.anchorMax = new Vector2(0, 1); edge.rectTransform.sizeDelta = new Vector2(3, 0);
            return image.rectTransform;
        }
        static Button Button(Transform parent, string title, float x, float y, float width = 500, float height = 64)
        {
            var image = Image(title, parent, new Color(.11f, .19f, .23f)); image.raycastTarget = true;
            Place(image.rectTransform, new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.4f, 1.6f, 1.6f); colors.pressedColor = UITheme.Cyan;
            colors.disabledColor = new Color(.45f, .5f, .53f); colors.fadeDuration = .12f; button.colors = colors;
            var label = Text("Label", image.transform, title, 30); Stretch(label.rectTransform, 24, 6, 24, 6);
            image.gameObject.AddComponent<UIButtonFeedback>(); return button;
        }
        static Toggle Toggle(Transform parent, string title, float x, float y, float width = 500)
        {
            var rect = Rect(title, parent); Place(rect, new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, 44));
            var bg = Image("Box", rect, new Color(.14f, .24f, .29f)); bg.raycastTarget = true;
            Place(bg.rectTransform, new Vector2(0, .5f), Vector2.zero, new Vector2(32, 32));
            var check = Image("Check", bg.transform, UITheme.Cyan); Stretch(check.rectTransform, 6, 6, 6, 6);
            var toggle = rect.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = bg; toggle.graphic = check; toggle.isOn = true;
            var label = Text("Label", rect, title, 27); Stretch(label.rectTransform, 50, 0, 0, 0); return toggle;
        }
        static UISmoothFill Progress(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color, bool marker = false)
        {
            var bg = Image(name, parent, new Color(.16f, .23f, .27f)); Place(bg.rectTransform, anchor, position, size);
            var fill = Image("Fill", bg.transform, color); Stretch(fill.rectTransform); fill.sprite = whiteSprite; fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillAmount = 0;
            var smooth = bg.gameObject.AddComponent<UISmoothFill>(); smooth.Fill = fill;
            if (marker) { var m = Image("Marker", bg.transform, UITheme.Text); m.rectTransform.anchorMin = new Vector2(0, 0); m.rectTransform.anchorMax = new Vector2(0, 1); m.rectTransform.sizeDelta = new Vector2(3, 12); m.rectTransform.anchoredPosition = Vector2.zero; smooth.Marker = m.rectTransform; }
            return smooth;
        }
        static RectTransform Scroll(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, out ScrollRect scroll)
        {
            var root = Rect(name, parent); Place(root, anchor, position, size);
            var bg = root.gameObject.AddComponent<Image>(); bg.color = new Color(.04f, .07f, .09f, .3f);
            scroll = root.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.scrollSensitivity = 40;
            var viewport = Rect("Viewport", root); Stretch(viewport); viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 8; layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content; return content;
        }
        static void EventSystemSetup()
        {
            if (Object.FindAnyObjectByType<EventSystem>()) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        static void BuildPrefab()
        {
            MakeTemplates();
            var root = Rect("EchoUI", null);
            var canvas = root.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = root.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = 1;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var ui = root.gameObject.AddComponent<UIManager>(); ui.Scaler = scaler;
            ui.HUD = BuildHUD(root); ui.Timeline = BuildTimeline(root);
            ui.Main = BuildMain(root); ui.Pause = BuildPause(root); ui.Levels = BuildLevels(root); ui.Complete = BuildComplete(root); ui.Settings = BuildSettings(root);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath); Object.DestroyImmediate(root.gameObject);
        }
        static void MakeTemplates()
        {
            var label = Text("Echo row", null, "", 22, UITheme.Cyan); label.gameObject.AddComponent<LayoutElement>().preferredHeight = 58;
            statusPrefab = PrefabUtility.SaveAsPrefabAsset(label.gameObject, "Assets/_ECHO/UI/Prefabs/EchoStatusRow.prefab").GetComponent<TMP_Text>(); Object.DestroyImmediate(label.gameObject);
            var seg = Image("Action segment", null, UITheme.Cyan);
            segmentPrefab = PrefabUtility.SaveAsPrefabAsset(seg.gameObject, "Assets/_ECHO/UI/Prefabs/TimelineSegment.prefab").GetComponent<Image>(); Object.DestroyImmediate(seg.gameObject);
            var row = Rect("Timeline track", null); row.sizeDelta = new Vector2(1200, 64); row.gameObject.AddComponent<LayoutElement>().preferredHeight = 64;
            var track = row.gameObject.AddComponent<TimelineTrackUI>();
            track.Label = Label(row, "ЭХО", 24, 58, 0, 230, 32); track.State = Label(row, "ОЖИДАНИЕ", 16, 58, 32, 235, 28, UITheme.Muted);
            track.Label.enableAutoSizing = true; track.Label.fontSizeMin = 16; track.Label.fontSizeMax = 24; track.Label.textWrappingMode = TextWrappingModes.NoWrap;
            track.Visibility = Toggle(row, "", 8, 10, 42);
            var lane = Image("Lane", row, new Color(.12f, .2f, .24f)); Stretch(lane.rectTransform, 300, 14, 24, 14); track.Lane = lane.rectTransform; track.SegmentPrefab = segmentPrefab;
            trackPrefab = PrefabUtility.SaveAsPrefabAsset(row.gameObject, "Assets/_ECHO/UI/Prefabs/TimelineTrack.prefab").GetComponent<TimelineTrackUI>(); Object.DestroyImmediate(row.gameObject);
        }
        static HUDController BuildHUD(Transform root)
        {
            var motion = Group("HUD", root); var hud = motion.gameObject.AddComponent<HUDController>(); hud.Motion = motion;
            var echoes = Rect("Echo status", motion.transform);
            Place(echoes, new Vector2(0, 1), new Vector2(40, -32), new Vector2(360, 44));
            hud.Echoes = echoes.gameObject.AddComponent<EchoStatusUI>();
            hud.Echoes.Count = Label(echoes, "", 26, 0, 0, 360, 44, UITheme.Cyan);
            var timer = Rect("Cycle timer", motion.transform);
            Place(timer, Vector2.one, new Vector2(-40, -32), new Vector2(280, 84));
            hud.Timer = timer.gameObject.AddComponent<CycleTimerUI>();
            hud.Timer.Clock = Label(timer, "00:30", 36, 0, 0, 280, 48);
            hud.Timer.Cycle = Label(timer, "", 18, 0, 48, 280, 30, UITheme.Muted);
            hud.Timer.Clock.alignment = hud.Timer.Cycle.alignment = TextAlignmentOptions.Right;
            var dot = Image("Crosshair", motion.transform, UITheme.Text); Place(dot.rectTransform, Vector2.one * .5f, Vector2.zero, new Vector2(5, 5));
            hud.Crosshair = dot.gameObject.AddComponent<CrosshairUI>(); hud.Crosshair.Dot = dot;
            var outline = dot.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(0, 0, 0, .8f); outline.effectDistance = new Vector2(1, -1);
            var prompt = Group("Interaction prompt", motion.transform); Place((RectTransform)prompt.transform, Vector2.one * .5f, new Vector2(0, -65), new Vector2(700, 54));

            hud.Prompt = prompt.gameObject.AddComponent<InteractionPromptUI>(); hud.Prompt.Motion = prompt;
            hud.Prompt.Label = Text("Prompt", prompt.transform, "", 30, UITheme.Cyan, TextAlignmentOptions.Center); Stretch(hud.Prompt.Label.rectTransform, 15, 0, 15, 0);
            var compact = Rect("Compact timeline", motion.transform);
            Place(compact, new Vector2(.5f, 0), new Vector2(0, 36), new Vector2(680, 8));
            hud.CompactTimeline = Progress(compact, "Cycle progress", Vector2.zero, new Vector2(0, 0), new Vector2(680, 8), UITheme.Muted, true);
            var recording = Group("Recording", compact);
            Stretch((RectTransform)recording.transform);
            var rec = recording.gameObject.AddComponent<RecordingIndicatorUI>(); hud.Recording = rec; rec.Motion = recording;
            rec.Status = Label(recording.transform, "", 20, 0, -32, 490, 28, UITheme.Amber);
            rec.Elapsed = Label(recording.transform, "", 20, 550, -32, 130, 28, UITheme.Amber);
            rec.Progress = Progress(recording.transform, "Recording interval", Vector2.zero, Vector2.zero, new Vector2(680, 8), UITheme.Amber);
            foreach (float endpoint in new[] { 0f, 1f })
            {
                var cap = Image(endpoint == 0 ? "Recording start" : "Recording end", rec.Progress.Fill.transform, UITheme.Amber);
                cap.rectTransform.anchorMin = cap.rectTransform.anchorMax = new Vector2(endpoint, .5f);
                cap.rectTransform.sizeDelta = new Vector2(2, 14); cap.rectTransform.anchoredPosition = Vector2.zero;
            }
            hud.Schedule = compact.gameObject.AddComponent<CompactEchoTimelineUI>();
            hud.Schedule.Player = hud.CompactTimeline; hud.Schedule.Recording = rec; hud.Schedule.SegmentPrefab = segmentPrefab;
            hud.Schedule.LabelPrefab = Text("Compact segment ID template", compact, "", 12, UITheme.Background, TextAlignmentOptions.Center);
            hud.Schedule.LabelPrefab.enableAutoSizing = true; hud.Schedule.LabelPrefab.fontSizeMin = 6; hud.Schedule.LabelPrefab.fontSizeMax = 12;
            hud.Schedule.LabelPrefab.textWrappingMode = TextWrappingModes.NoWrap;
            hud.Schedule.LabelPrefab.gameObject.SetActive(false);
            hud.Schedule.Conflict = Image("Conflict interval", compact, new Color(UITheme.Error.r, UITheme.Error.g, UITheme.Error.b, .5f));
            hud.Schedule.Conflict.gameObject.SetActive(false);
            var notice = Group("Notification", motion.transform); Place((RectTransform)notice.transform, new Vector2(.5f, 0), new Vector2(0, 234), new Vector2(960, 94));

            hud.NoticePanel = notice; hud.NoticeText = Text("Message", notice.transform, "", 28, null, TextAlignmentOptions.Center); Stretch(hud.NoticeText.rectTransform, 24, 12, 24, 12);
            hud.Scanner = BuildScannerHUD(motion.transform);
            return hud;
        }
        static ScannerHUD BuildScannerHUD(Transform parent)
        {
            var root = Rect("Scanner HUD", parent); Stretch(root);
            var scanner = root.gameObject.AddComponent<ScannerHUD>();
            scanner.ScannerPanel = Group("Scan status", root);
            Place((RectTransform)scanner.ScannerPanel.transform, new Vector2(.5f, 0), new Vector2(0, 232), new Vector2(740, 108));

            scanner.Status = Label(scanner.ScannerPanel.transform, "СКАНИРОВАНИЕ", 25, 18, 6, 704, 36, UITheme.Cyan);
            scanner.ProgressLabel = Label(scanner.ScannerPanel.transform, "", 23, 18, 44, 704, 36);
            scanner.Progress = Progress(scanner.ScannerPanel.transform, "Scan progress", Vector2.zero, new Vector2(18, 12), new Vector2(704, 7), UITheme.Cyan);
            scanner.PhasePanel = Group("Phase controller", root);
            Place((RectTransform)scanner.PhasePanel.transform, Vector2.one, new Vector2(-40, -204), new Vector2(420, 224));

            Label(scanner.PhasePanel.transform, "ФАЗОВЫЙ КОНТРОЛЛЕР", 24, 20, 12, 380, 38, UITheme.Cyan);
            scanner.NodeA = Label(scanner.PhasePanel.transform, "", 23, 20, 58, 380, 42);
            scanner.NodeB = Label(scanner.PhasePanel.transform, "", 23, 20, 100, 380, 42);
            scanner.Synchronization = Label(scanner.PhasePanel.transform, "", 23, 20, 148, 380, 66);
            return scanner;
        }
        static TimelineUI BuildTimeline(Transform root)
        {
            var motion = Group("Timeline", root); var timeline = motion.gameObject.AddComponent<TimelineUI>(); timeline.Motion = motion;
            var panel = Panel("Tracks panel", motion.transform, new Vector2(.5f, 0), new Vector2(0, 110), new Vector2(1320, 220));
            Label(panel, "ВРЕМЕННЫЕ СЛЕДЫ", 28, 24, 12, 800, 44);
            timeline.Clock = Label(panel, "00:00", 26, 1020, 12, 120, 44, UITheme.Amber);
            Label(panel, "/", 26, 1140, 12, 30, 44, UITheme.Muted);
            timeline.Duration = Label(panel, "00:30", 26, 1170, 12, 130, 44, UITheme.Muted);
            timeline.Content = Scroll(panel, "Tracks scroll", new Vector2(0, 1), new Vector2(24, -68), new Vector2(1272, 100), out _);
            timeline.Summary = Label(panel, "", 19, 24, 54, 1272, 28, UITheme.Muted);
            timeline.Summary.enableAutoSizing = true; timeline.Summary.fontSizeMin = 14; timeline.Summary.fontSizeMax = 19; timeline.Summary.textWrappingMode = TextWrappingModes.NoWrap;
            timeline.TrackPrefab = trackPrefab;
            var marker = Image("Shared time marker", timeline.Content, UITheme.Amber); timeline.Marker = marker.rectTransform;
            marker.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var close = Label(panel, "Tab — закрыть", 20, 24, 0, 1200, 32, UITheme.Muted);
            Place(close.rectTransform, Vector2.zero, new Vector2(24, 8), new Vector2(1200, 32));
            timeline.Conflict = Image("Timeline conflict interval", timeline.Content, new Color(UITheme.Error.r, UITheme.Error.g, UITheme.Error.b, .4f));
            timeline.Conflict.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            timeline.Conflict.gameObject.SetActive(false);
            return timeline;
        }
        static RectTransform MenuPanel(UIPanelMotion motion, string title, float height = 680)
        {
            var panel = Panel("Panel", motion.transform, Vector2.one * .5f, Vector2.zero, new Vector2(760, height));
            Label(panel, title, 42, 44, 28, 672, 68); return panel;
        }
        static MainMenuUI BuildMain(Transform root)
        {
            var motion = Group("Main menu", root, true); var menu = motion.gameObject.AddComponent<MainMenuUI>(); menu.Motion = motion;
            // Inexpensive technological backdrop, built from UI geometry.
            for (int i = 0; i < 6; i++) { var line = Image("Time trace " + i, motion.transform, new Color(.34f, .86f, .9f, .05f + i * .01f)); Place(line.rectTransform, new Vector2(1, .5f), new Vector2(-120 - i * 48, (i - 3) * 90), new Vector2(620 + i * 50, 2)); line.transform.localRotation = Quaternion.Euler(0, 0, -24); }
            var panel = Panel("Navigation", motion.transform, new Vector2(0, .5f), new Vector2(100, 0), new Vector2(670, 740));
            Label(panel, "E C H O", 88, 48, 28, 590, 120, UITheme.Cyan);
            Label(panel, "ВАШЕ ПРОШЛОЕ ОТКРЫВАЕТ ПУТЬ", 24, 48, 152, 590, 42, UITheme.Muted);
            menu.Continue = Button(panel, "Продолжить", 48, 234, 574);
            menu.NewGame = Button(panel, "Новая игра", 48, 312, 574);
            menu.LevelSelect = Button(panel, "Выбор уровня", 48, 390, 574);
            menu.Settings = Button(panel, "Настройки", 48, 468, 574);
            menu.Quit = Button(panel, "Выход", 48, 546, 574);
            Label(panel, "Продолжение недоступно: сохранений пока нет.\nВ Editor выход из приложения отключён.", 23, 48, 636, 574, 78, UITheme.Muted);
            return menu;
        }
        static PauseMenuUI BuildPause(Transform root)
        {
            var motion = Group("Pause", root, true); var menu = motion.gameObject.AddComponent<PauseMenuUI>(); menu.Motion = motion;
            var panel = MenuPanel(motion, "ПАУЗА", 750);
            Label(panel, "Время остановлено. Эхо ждут вас.", 27, 44, 106, 670, 54, UITheme.Muted);
            menu.Resume = Button(panel, "Продолжить", 44, 194, 672);
            menu.Restart = Button(panel, "Перезапустить цикл", 44, 282, 672);
            menu.Settings = Button(panel, "Настройки", 44, 370, 672);
            menu.MainMenu = Button(panel, "Главное меню", 44, 458, 672); Label(panel, "WASD — движение · Space — прыжок\nR — запись · T — повтор · Tab — шкала\nF — действие · ПКМ — сканер · Esc — пауза", 22, 44, 560, 672, 140, UITheme.Muted); return menu;
        }
        static LevelSelectUI BuildLevels(Transform root)
        {
            var motion = Group("Level select", root, true); var menu = motion.gameObject.AddComponent<LevelSelectUI>(); menu.Motion = motion;
            var panel = MenuPanel(motion, "ВЫБОР УРОВНЯ", 580);
            Label(panel, "Испытательные комплексы", 26, 44, 116, 670, 64, UITheme.Muted);
            menu.FirstLevel = Button(panel, "01 · Первый отголосок", 44, 210, 672, 80);
            menu.SecondLevel = Button(panel, "02 · Скрытый сигнал", 44, 306, 672, 80);
            menu.Back = Button(panel, "Назад", 44, 450, 672); return menu;
        }
        static LevelCompleteUI BuildComplete(Transform root)
        {
            var motion = Group("Level complete", root, true); var menu = motion.gameObject.AddComponent<LevelCompleteUI>(); menu.Motion = motion;
            var panel = MenuPanel(motion, "УРОВЕНЬ ПРОЙДЕН", 630);
            menu.Summary = Label(panel, "", 29, 44, 128, 672, 150);
            menu.NextLevel = Button(panel, "Следующий уровень", 44, 320, 672);
            menu.Replay = Button(panel, "Пройти ещё раз", 44, 408, 672);
            menu.MainMenu = Button(panel, "Главное меню", 44, 496, 672); return menu;
        }
        static Slider Slider(Transform parent, float x, float y, float min, float max)
        {
            var bg = Image("Slider", parent, new Color(.14f, .23f, .28f)); bg.raycastTarget = true;
            Place(bg.rectTransform, new Vector2(0, 1), new Vector2(x, -y), new Vector2(330, 22));
            var fill = Image("Fill", bg.transform, UITheme.Cyan); Stretch(fill.rectTransform);
            var handle = Image("Handle", bg.transform, UITheme.Text); handle.rectTransform.sizeDelta = new Vector2(18, 30); handle.raycastTarget = true;
            var slider = bg.gameObject.AddComponent<Slider>(); slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            slider.minValue = min; slider.maxValue = max; return slider;
        }
        static TMP_Dropdown Dropdown(Transform parent, float x, float y)
        {
            var bg = Image("Resolution dropdown", parent, new Color(.12f, .2f, .25f)); bg.raycastTarget = true;
            Place(bg.rectTransform, new Vector2(0, 1), new Vector2(x, -y), new Vector2(440, 52));
            var dropdown = bg.gameObject.AddComponent<TMP_Dropdown>(); dropdown.targetGraphic = bg;
            dropdown.captionText = Text("Value", bg.transform, "1920 × 1080", 26); Stretch(dropdown.captionText.rectTransform, 14, 0, 40, 0);
            Label(bg.transform, "...", 28, 396, 0, 40, 48, UITheme.Cyan);
            var template = Image("Template", bg.transform, UITheme.Background); template.raycastTarget = true;
            Place(template.rectTransform, new Vector2(0, 0), new Vector2(0, -260), new Vector2(440, 260));
            var scroll = template.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
            var viewport = Rect("Viewport", template.transform); Stretch(viewport); viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport); Place(content, new Vector2(0, 1), Vector2.zero, new Vector2(440, 44));
            var itemBg = Image("Item", content, new Color(.15f, .24f, .29f)); itemBg.raycastTarget = true; Stretch(itemBg.rectTransform);
            var toggle = itemBg.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = itemBg;
            var check = Image("Check", itemBg.transform, UITheme.Cyan); Place(check.rectTransform, new Vector2(0, .5f), new Vector2(8, 0), new Vector2(4, 24)); toggle.graphic = check;
            dropdown.itemText = Text("Item label", itemBg.transform, "Resolution", 25); Stretch(dropdown.itemText.rectTransform, 24, 0, 12, 0);
            scroll.viewport = viewport; scroll.content = content; scroll.scrollSensitivity = 35;
            dropdown.template = template.rectTransform; template.gameObject.SetActive(false); return dropdown;
        }
        static SettingsMenuUI BuildSettings(Transform root)
        {
            var motion = Group("Settings", root, true); var menu = motion.gameObject.AddComponent<SettingsMenuUI>(); menu.Motion = motion;
            var panel = Panel("Settings panel", motion.transform, Vector2.one * .5f, Vector2.zero, new Vector2(1240, 780));
            Label(panel, "НАСТРОЙКИ", 42, 40, 22, 1160, 66);
            Label(panel, "ЗВУК", 27, 40, 112, 500, 40, UITheme.Cyan);
            Label(panel, "Общая громкость", 26, 40, 170, 400, 40);
            menu.Master = Slider(panel, 40, 232, 0, 1); menu.MasterValue = Label(panel, "100%", 25, 400, 214, 130, 50);
            Label(panel, "Музыка и эффекты отсутствуют в прототипе.\nРаздельная громкость пока недоступна.", 23, 40, 280, 510, 90, UITheme.Muted);
            Label(panel, "УПРАВЛЕНИЕ", 27, 40, 404, 500, 40, UITheme.Cyan);
            Label(panel, "Чувствительность мыши", 26, 40, 454, 500, 42);
            menu.Sensitivity = Slider(panel, 40, 516, .02f, .4f); menu.SensitivityValue = Label(panel, "0.10", 25, 400, 498, 130, 50);
            Label(panel, "WASD — движение  ·  Space — прыжок\nF — действие  ·  R — запись  ·  T — повтор\nTab — шкала  ·  Esc — пауза", 23, 40, 564, 550, 108, UITheme.Muted);
            Label(panel, "ВИДЕО", 27, 660, 112, 500, 40, UITheme.Cyan);
            menu.Fullscreen = Toggle(panel, "Полноэкранный режим", 660, 162);
            menu.VSync = Toggle(panel, "Вертикальная синхронизация", 660, 214);
            menu.Resolution = Dropdown(panel, 660, 280);
            Label(panel, "ИНТЕРФЕЙС", 27, 660, 404, 500, 40, UITheme.Cyan);
            Label(panel, "Масштаб UI", 26, 660, 454, 500, 42);
            menu.Scale = Slider(panel, 660, 516, .75f, 1.25f); menu.ScaleValue = Label(panel, "100%", 25, 1020, 498, 140, 50);
            menu.Animations = Toggle(panel, "Анимации интерфейса", 660, 572);
            menu.Feedback = Label(panel, "", 22, 660, 626, 540, 58, UITheme.Muted);
            menu.Back = Button(panel, "Назад", 40, 700, 520, 58); menu.Apply = Button(panel, "Применить", 660, 700, 540, 58);
            return menu;
        }
    }
}
