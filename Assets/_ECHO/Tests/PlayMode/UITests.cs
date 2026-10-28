using System.Collections;
using System.Collections.Generic;
using System.IO;
using EchoGame.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace EchoGame.Tests
{
    public sealed class UITestInteractable : MonoBehaviour, IInteractable
    {
        public string Prompt => "Проверить механизм";
        public bool CanInteract => true;
        public int Count;
        public void Interact() => Count++;
    }
    public sealed class UITestContext : MonoBehaviour, IUIContextSource
    {
        public bool IsResearch => true;
        public EchoLimitModel LimitModel => EchoLimitModel.ConcurrentEchoLimit;
        public int ConcurrentEchoCapacity => 2;
        public int ActiveEchoCount => 1;
        public RecordingRejection CurrentRejection => null;
    }
    public class UITests
    {
        UIManager ui;
        TimeCycleManager cycles;
        [UnitySetUp] public IEnumerator Load()
        {
            Time.timeScale = 1; yield return SceneManager.LoadSceneAsync("Level_01"); yield return null;
            ui = Object.FindAnyObjectByType<UIManager>(); cycles = Object.FindAnyObjectByType<TimeCycleManager>();
            Assert.That(ui, Is.Not.Null); Assert.That(ui.Provider, Is.Not.Null);
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (cycles) cycles.Game.SetState(GameState.Playing); Time.timeScale = 1; yield return null; }
        [UnityTest] public IEnumerator CompactHUDCountsSavedEchoesWithoutStatusRows()
        {
            cycles.AutoStartRecording = false; cycles.Restart(true);
            cycles.Echoes.Add(Recording());
            cycles.Echoes.Evaluate(cycles.Duration);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(ui.HUD.Echoes.Count.text, Is.EqualTo("ЗАПИСИ: 1/" + cycles.Echoes.MaximumEchoes));
            Assert.That(ui.HUD.Echoes.GetComponentsInChildren<ScrollRect>().Length, Is.Zero);
            Assert.That(ui.HUD.Timer.Clock.text, Is.EqualTo(UITheme.Clock(Mathf.Ceil(cycles.Duration - cycles.Elapsed))));
            Assert.That(ui.HUD.Recording.Motion.GetComponent<CanvasGroup>().alpha, Is.Zero);
        }
        [UnityTest] public IEnumerator TimelineFitsContentAndBoundsWithOverflow()
        {
            cycles.AutoStartRecording = false; cycles.Restart(true);
            ui.SetState(UIState.Timeline); yield return null;
            var scroll = ui.Timeline.Content.GetComponentInParent<ScrollRect>();
            var panel = (RectTransform)scroll.transform.parent;
            float emptyHeight = panel.rect.height;
            int capacity = cycles.Echoes.MaximumEchoes;
            cycles.Echoes.MaximumEchoes = 12;
            for (int i = 0; i < 10; i++) cycles.Echoes.Add(Recording());
            ui.SetState(UIState.Timeline); yield return null;
            Assert.That(panel.rect.height, Is.GreaterThan(emptyHeight));
            Assert.That(panel.rect.height, Is.LessThanOrEqualTo(((RectTransform)ui.Timeline.Motion.transform).rect.height * .4f + 1));
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
            cycles.Echoes.MaximumEchoes = capacity;
        }
        [UnityTest] public IEnumerator CountdownResearchAndConcurrentScheduleUseSnapshot()
        {
            cycles.Game.SetState(GameState.Paused); ui.SetState(UIState.Gameplay);
            var echoes = new List<EchoUIData>();
            for (int i = 0; i < 4; i++) echoes.Add(new EchoUIData {
                Id = i + 1, Start = i * 8, End = Mathf.Min(30, (i + 1) * 8), State = EchoState.Playback,
                Intervals = new List<ActionInterval>() });
            var data = new UISnapshot { Time = 7, Duration = 30, Phase = SessionPhase.Playing,
                Recording = RecordingState.Recording, RecordStart = 5, RecordEnd = 7,
                Echoes = echoes, LimitModel = EchoLimitModel.ConcurrentEchoLimit, ConcurrentEchoCapacity = 1, ActiveEchoCount = 1 };
            ui.HUD.Apply(data); yield return new WaitForSecondsRealtime(.2f);
            Assert.That(ui.HUD.Timer.Clock.text, Is.EqualTo("00:23"));
            Assert.That(ui.HUD.Echoes.Count.text, Is.EqualTo("АКТИВНО: 1/1"));
            Assert.That(ui.HUD.Schedule.LaneCount, Is.EqualTo(1));
            Assert.That(ui.HUD.Schedule.SegmentCount, Is.EqualTo(4));
            Assert.That(ui.HUD.CompactTimeline.Marker.anchorMin.x, Is.EqualTo(7f/30).Within(.001));
            Assert.That(ui.HUD.Recording.Progress.Fill.rectTransform.anchorMin.x, Is.EqualTo(5f/30).Within(.001));
            Assert.That(ui.HUD.Recording.Progress.Fill.rectTransform.anchorMax.x, Is.EqualTo(7f/30).Within(.001));
            Assert.That(ui.HUD.Recording.Motion.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
            yield return CaptureShell(ui, "CompactScheduleSequential");
            var rejection = new RecordingRejection { Reason = "Лимит Эхо превышен:", Start = 10, End = 15 };
            data.Rejection = rejection; ui.HUD.Apply(data); ui.Timeline.Apply(data);
            Assert.That(ui.HUD.Schedule.Conflict.gameObject.activeSelf, Is.True);
            Assert.That(ui.HUD.Schedule.Conflict.rectTransform.anchorMin.x, Is.EqualTo(10f/30).Within(.001));
            Assert.That(ui.Timeline.Conflict.gameObject.activeSelf, Is.True);
            Assert.That(data.Echoes.Count, Is.EqualTo(4));
            yield return CaptureShell(ui, "CompactScheduleConflict");
            data.IsResearch = true; data.Rejection = null;
            ui.HUD.Apply(data); yield return new WaitForSecondsRealtime(.2f);
            Assert.That(ui.HUD.Timer.Clock.text, Is.EqualTo("ИССЛЕДОВАНИЕ"));
            Assert.That(ui.HUD.CompactTimeline.Marker.gameObject.activeSelf, Is.False);
            Assert.That(ui.HUD.Recording.Motion.GetComponent<CanvasGroup>().alpha, Is.Zero);
            ui.Timeline.Apply(data);
            Assert.That(ui.Timeline.Summary.text, Does.Contain("Сохранено: 4"));
        }
        [UnityTest] public IEnumerator SavedLimitWarnsAndReplacementKeepsRealSchedule()
        {
            cycles.AutoStartRecording = false; cycles.Restart(true);
            for (int i = 0; i < cycles.Echoes.MaximumEchoes; i++) cycles.Echoes.Add(Recording());
            int oldest = ui.Provider.Current.Echoes[0].Id;
            Assert.That(ui.HUD.NoticeText.text, Does.Contain("заменит старейшую"));
            cycles.Echoes.Add(Recording()); yield return null;
            Assert.That(ui.Provider.Current.Echoes.Count, Is.EqualTo(cycles.Echoes.MaximumEchoes));
            Assert.That(ui.Provider.Current.Echoes[0].Id, Is.Not.EqualTo(oldest));
            Assert.That(ui.HUD.Schedule.SegmentCount, Is.EqualTo(cycles.Echoes.MaximumEchoes));
            Assert.That(ui.HUD.Schedule.LaneCount, Is.EqualTo(cycles.Echoes.MaximumEchoes));
            yield return CaptureShell(ui, "CompactScheduleOverlap");
        }
        [UnityTest] public IEnumerator OptionalContextAdapterUsesExplicitModeAndCanBeDisconnected()
        {
            var bridge = (UIEventBridge)ui.DataProvider;
            var context = bridge.gameObject.AddComponent<UITestContext>();
            bridge.ContextSource = context; bridge.Publish(); yield return null;
            Assert.That(ui.Provider.Current.IsResearch, Is.True);
            Assert.That(ui.HUD.Timer.Clock.text, Is.EqualTo("ИССЛЕДОВАНИЕ"));
            Assert.That(ui.HUD.Echoes.Count.text, Is.EqualTo("АКТИВНО: 1/2"));
            cycles.Game.SetState(GameState.Timeline); yield return null;
            Assert.That(ui.Provider.Current.IsResearch, Is.True);
            Assert.That(ui.Timeline.Summary.text, Does.Contain("ИССЛЕДОВАНИЕ"));
            Assert.That(ui.Timeline.Clock.text, Is.EqualTo("—"));
            cycles.Game.SetState(GameState.Playing);
            bridge.ContextSource = null; bridge.Publish();
            Assert.That(ui.Provider.Current.IsResearch, Is.False);
            Assert.That(ui.Provider.Current.LimitModel, Is.EqualTo(EchoLimitModel.SavedRecordingsLimit));
            Assert.That(ui.Provider.Current.Phase, Is.EqualTo(SessionPhase.Playing));
            Object.Destroy(context);
        }
        static EchoRecording Recording() => new EchoRecording(new List<EchoFrame> {
            new EchoFrame(0, Vector3.zero, 0, 0), new EchoFrame(4, Vector3.right * 6, 0, 0),
            new EchoFrame(8, Vector3.right * 6, 0, 0) });
        [UnityTest] public IEnumerator ManualRecordingStartsAtActualCycleTime()
        {
            cycles.AutoStartRecording = false; cycles.Restart(true);
            yield return new WaitForSecondsRealtime(.12f);
            Assert.That(ui.Provider.Current.Recording, Is.EqualTo(RecordingState.Waiting));
            cycles.ToggleRecording(); float started = cycles.RecordingStart;
            Assert.That(started, Is.GreaterThan(0));
            cycles.Player.GetComponent<CharacterController>().Move(Vector3.right * 2);
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            cycles.ToggleRecording();
            Assert.That(cycles.Echoes.Echoes.Count, Is.EqualTo(1));
            var echo = cycles.Echoes.Echoes[0];
            Assert.That(echo.Recording.Frames[0].Time, Is.EqualTo(started));
            Assert.That(echo.State, Is.EqualTo(EchoPlaybackState.Waiting));
            echo.Evaluate(started + .001f); Assert.That(echo.State, Is.EqualTo(EchoPlaybackState.Playback));
        }
        [UnityTest] public IEnumerator TimelinePauseTracksAndUniqueIds()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                for (int i = 0; i < 5; i++) cycles.Echoes.Add(Recording());
                var snapshot = ui.Provider.Current;
                Assert.That(snapshot.Echoes.Count, Is.EqualTo(3));
                Assert.That(snapshot.Echoes[0].Id, Is.EqualTo(3));
                Assert.That(snapshot.Echoes[2].Id, Is.EqualTo(5));
                Assert.That(snapshot.Echoes[0].Intervals.Count, Is.GreaterThanOrEqualTo(2));
                float scale = Time.timeScale = .7f;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab)); yield return null; yield return null;
                Assert.That(ui.State, Is.EqualTo(UIState.Timeline));
                Assert.That(Time.timeScale, Is.Zero); Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                yield return CaptureShell(ui, "Timeline");
                float paused = cycles.Elapsed; yield return null; Assert.That(cycles.Elapsed, Is.EqualTo(paused));
                var tracks = ui.Timeline.Content.GetComponentsInChildren<TimelineTrackUI>();
                Assert.That(tracks.Length, Is.EqualTo(4));
                tracks[0].Visibility.isOn = false; Assert.That(tracks[0].Lane.gameObject.activeSelf, Is.False);
                tracks[0].Visibility.isOn = true;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab)); yield return null; yield return null;
                Assert.That(cycles.Game.State, Is.EqualTo(GameState.Playing)); Assert.That(Time.timeScale, Is.EqualTo(scale));
                cycles.Game.TogglePause(); yield return null;
                yield return CaptureShell(ui, "Pause");
                ui.OpenSettings(); Assert.That(ui.State, Is.EqualTo(UIState.Settings));
                yield return CaptureShell(ui, "Settings");
                ui.Escape(); Assert.That(ui.State, Is.EqualTo(UIState.Paused)); Assert.That(Time.timeScale, Is.Zero);
                ui.Pause.Resume.onClick.Invoke(); Assert.That(cycles.Game.State, Is.EqualTo(GameState.Playing));
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }
        [UnityTest] public IEnumerator GameplayInteractionDrivesCrosshairAndPrompt()
        {
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                target.transform.position = cycles.Player.View.position + cycles.Player.View.forward * 2;
                target.transform.localScale = Vector3.one * .5f;
                var interactable = target.AddComponent<UITestInteractable>(); Physics.SyncTransforms();
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(ui.HUD.Crosshair.Dot.color, Is.EqualTo(UITheme.Cyan));
                Assert.That(ui.HUD.Prompt.Label.text, Does.Contain("[F] Проверить механизм"));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); yield return null; yield return null;
                Assert.That(interactable.Count, Is.EqualTo(1));
                Assert.That(ui.Timeline.InteractionCount, Is.EqualTo(1));
                cycles.Game.SetState(GameState.Timeline); yield return null;
                Assert.That(ui.HUD.Crosshair.gameObject.activeSelf, Is.False);
                Assert.That(ui.Timeline.Content.GetComponentsInChildren<Image>(), Has.Some.Matches<Image>(marker => marker.name == "F interaction tick"));
                cycles.Restart(false); Assert.That(ui.Timeline.InteractionCount, Is.Zero);
            }
            finally { Object.Destroy(target); InputSystem.RemoveDevice(keyboard); }
        }
        [UnityTest] public IEnumerator SettingsApplyAndRoundTripWithoutChangingDisplay()
        {
            string original = JsonUtility.ToJson(UISettingsManager.Current);
            try
            {
                var changed = JsonUtility.FromJson<UISettingsData>(original);
                changed.Scale = 1.25f; changed.Sensitivity = .23f; changed.Master = .42f; changed.Animations = false;
                UISettingsManager.Save(changed, false); yield return null;
                Assert.That(AudioListener.volume, Is.EqualTo(.42f).Within(.001));
                Assert.That(cycles.Player.MouseSensitivity, Is.EqualTo(.23f));
                Assert.That(ui.Scaler.referenceResolution, Is.EqualTo(new Vector2(1536, 864)));
                var stored = JsonUtility.FromJson<UISettingsData>(PlayerPrefs.GetString("ECHO.UI.Settings.v1"));
                Assert.That(stored.Scale, Is.EqualTo(1.25f)); Assert.That(stored.Animations, Is.False);
                cycles.Game.TogglePause(); ui.OpenSettings(); ui.Settings.Master.value = .6f; ui.Settings.Apply.onClick.Invoke();
                Assert.That(UISettingsManager.Current.Master, Is.EqualTo(.6f).Within(.001));
            }
            finally { UISettingsManager.Save(JsonUtility.FromJson<UISettingsData>(original), false); }
        }
        [UnityTest] public IEnumerator ResponsiveLayoutsAndVisualCaptures()
        {
            string original = JsonUtility.ToJson(UISettingsManager.Current);
            var canvas = ui.GetComponent<Canvas>(); var camera = cycles.Player.View.GetComponent<Camera>();
            Assert.That(ui.HUD.CompactTimeline.Fill.sprite, Is.Not.Null, "Filled images require a sprite to render partial progress");
            Assert.That(ui.HUD.Recording.Progress.Fill.sprite, Is.Not.Null);
            var sizes = new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(1920, 1200), new Vector2Int(3440, 1440) };
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .5f;
                foreach (var size in sizes)
                {
                    var texture = new RenderTexture(size.x, size.y, 24); texture.Create(); camera.targetTexture = texture;
                    foreach (float scale in new[] { .75f, 1f, 1.25f })
                    {
                        var settings = JsonUtility.FromJson<UISettingsData>(original); settings.Scale = scale; settings.Animations = false;
                        UISettingsManager.Save(settings, false); yield return null; Canvas.ForceUpdateCanvases();
                        foreach (var rect in new[] { ui.HUD.Timer.GetComponent<RectTransform>(), ui.HUD.Echoes.GetComponent<RectTransform>(), ui.HUD.CompactTimeline.transform.parent.GetComponent<RectTransform>(), ui.Settings.Motion.transform.GetChild(0).GetComponent<RectTransform>(), ui.Timeline.Motion.transform.GetChild(0).GetComponent<RectTransform>() })
                            AssertInside(rect, camera, size);
                        if (scale == 1)
                        {
                            camera.Render(); SaveCapture(texture, "HUD_" + size.x + "x" + size.y);
                        }
                    }
                    camera.targetTexture = null; Object.Destroy(texture);
                }
            }
            finally { camera.targetTexture = null; canvas.renderMode = RenderMode.ScreenSpaceOverlay; UISettingsManager.Save(JsonUtility.FromJson<UISettingsData>(original), false); }
        }
        static void AssertInside(RectTransform rect, Camera camera, Vector2Int size)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var point = camera.WorldToScreenPoint(corner);
                Assert.That(point.x, Is.InRange(-2f, size.x + 2f), rect.name + " horizontal bounds");
                Assert.That(point.y, Is.InRange(-2f, size.y + 2f), rect.name + " vertical bounds");
            }
        }
        public static void SaveCapture(RenderTexture texture, string name)
        {
            var previous = RenderTexture.active; RenderTexture.active = texture;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); image.Apply();
            var sampled = new HashSet<Color32>();
            for (int x = 20; x < texture.width; x += 37) for (int y = 20; y < texture.height; y += 37) sampled.Add(image.GetPixel(x, y));
            Assert.That(sampled.Count, Is.GreaterThan(4), "UI capture must contain rendered content, not a blank camera background");
            string directory = "C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-10-28-" + (name.StartsWith("L04_") ? "Level04" : "Level03") + "/Captures";
            Directory.CreateDirectory(directory); File.WriteAllBytes(directory + "/" + name + ".png", image.EncodeToPNG());
            Object.Destroy(image); RenderTexture.active = previous;
        }
        public static IEnumerator CaptureShell(UIManager shell, string name)
        {
            yield return new WaitForSecondsRealtime(.2f);
            var canvas = shell.GetComponent<Canvas>(); var originalMode = canvas.renderMode; var originalCamera = canvas.worldCamera;
            var cameraObject = new GameObject("UI capture camera"); var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = UITheme.Background; camera.cullingMask = 1 << 5;
            var texture = new RenderTexture(1920, 1080, 24); texture.Create(); camera.targetTexture = texture;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            yield return null; Canvas.ForceUpdateCanvases(); camera.Render(); SaveCapture(texture, name);
            canvas.renderMode = originalMode; canvas.worldCamera = originalCamera; camera.targetTexture = null;
            Object.Destroy(texture); Object.Destroy(cameraObject);
        }
        [UnityTest] public IEnumerator MainMenuOnlyExposesExistingLevel()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
            ui = Object.FindAnyObjectByType<UIManager>();
            Assert.That(ui.State, Is.EqualTo(UIState.MainMenu)); Assert.That(ui.Main.Continue.interactable, Is.False);
            yield return CaptureShell(ui, "MainMenu");
            ui.Main.LevelSelect.onClick.Invoke(); Assert.That(ui.State, Is.EqualTo(UIState.LevelSelect));
            ui.Levels.Back.onClick.Invoke(); Assert.That(ui.State, Is.EqualTo(UIState.MainMenu));
            ui.Main.Settings.onClick.Invoke(); Assert.That(ui.State, Is.EqualTo(UIState.Settings));
            ui.Settings.Back.onClick.Invoke(); ui.Main.NewGame.onClick.Invoke(); yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Level_01"));
        }
    }
}
