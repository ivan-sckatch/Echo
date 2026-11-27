using System.Collections;
using System.IO;
using System.Linq;
using EchoGame.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EchoGame.Tests
{
    public class MenuDesignTests
    {
        public const string Reports = "C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-27-MenuImplementation";
        [UnityTest]
        public IEnumerator ApprovedMenusKeepSceneContextAndApplySettings()
        {
            string original = JsonUtility.ToJson(UISettingsManager.Current);
            try
            {
                yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
                var ui = Object.FindAnyObjectByType<UIManager>();
                Assert.That(ui.State, Is.EqualTo(UIState.MainMenu));
                Assert.That(Camera.main.cullingMask, Is.Not.EqualTo(0));
                Assert.That(GameObject.Find("Menu corridor — ECHO modules"), Is.Not.Null);
                Assert.That(ui.Main.Continue.interactable, Is.False);
                Assert.That(ui.Main.NewGame.GetComponent<Image>().color.a, Is.Zero);
                Assert.That(EventSystem.current, Is.Not.Null, "MainMenu needs its own EventSystem when loaded alone");
                var buttonRect = ui.Main.NewGame.GetComponent<RectTransform>();
                var pointer = new PointerEventData(EventSystem.current) {
                    position = RectTransformUtility.WorldToScreenPoint(null, buttonRect.TransformPoint(buttonRect.rect.center)) };
                var hits = new System.Collections.Generic.List<RaycastResult>();
                ui.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
                Assert.That(hits.Any(hit => hit.gameObject == ui.Main.NewGame.gameObject), Is.True, "Text button row must accept pointer hits");
                yield return Capture(ui, "MainMenu");
                ui.Main.Settings.onClick.Invoke();
                Assert.That(ui.State, Is.EqualTo(UIState.Settings));
                yield return Capture(ui, "SettingsFromMain");
                ui.Escape(); Assert.That(ui.State, Is.EqualTo(UIState.MainMenu));
                ui.Main.NewGame.onClick.Invoke(); yield return null; yield return null;
                ui = Object.FindAnyObjectByType<UIManager>();
                var cycles = Object.FindAnyObjectByType<TimeCycleManager>();
                cycles.Game.TogglePause(); yield return null;
                Assert.That(ui.State, Is.EqualTo(UIState.Paused));
                Assert.That(Time.timeScale, Is.Zero);
                var camera = cycles.Player.View.GetComponent<Camera>();
                Vector3 position = camera.transform.position; Quaternion rotation = camera.transform.rotation;
                float elapsed = cycles.Elapsed;
                yield return Capture(ui, "Pause");
                ui.Pause.Settings.onClick.Invoke();
                Assert.That(ui.State, Is.EqualTo(UIState.Settings));
                ui.Settings.Master.value = .73f; ui.Settings.Sensitivity.value = .17f;
                ui.Settings.Scale.value = 1.25f; ui.Settings.Animations.isOn = false;
                ui.Settings.Apply.onClick.Invoke();
                Assert.That(UISettingsManager.Current.Master, Is.EqualTo(.73f).Within(.001f));
                Assert.That(AudioListener.volume, Is.EqualTo(.73f).Within(.001f));
                Assert.That(UISettingsManager.Current.Sensitivity, Is.EqualTo(.17f).Within(.001f));
                Assert.That(UISettingsManager.Current.Scale, Is.EqualTo(1.25f));
                Assert.That(ui.Settings.Feedback.text, Is.EqualTo("Настройки сохранены"));
                Assert.That(cycles.Elapsed, Is.EqualTo(elapsed));
                Assert.That(camera.transform.position, Is.EqualTo(position));
                Assert.That(camera.transform.rotation, Is.EqualTo(rotation));
                yield return Capture(ui, "SettingsFromPause");
                ui.Escape(); Assert.That(ui.State, Is.EqualTo(UIState.Paused));
                ui.Pause.Resume.onClick.Invoke();
                Assert.That(cycles.Game.State, Is.EqualTo(GameState.Playing));
                Assert.That(Time.timeScale, Is.GreaterThan(0));
            }
            finally { UISettingsManager.Save(JsonUtility.FromJson<UISettingsData>(original), false); Time.timeScale = 1; }
        }
        static IEnumerator Capture(UIManager ui, string name)
        {
            yield return new WaitForSecondsRealtime(.25f);
            var canvas = ui.GetComponent<Canvas>();
            var source = Camera.main ? Camera.main : Object.FindAnyObjectByType<Camera>();
            var go = new GameObject("Menu verification camera"); var camera = go.AddComponent<Camera>();
            camera.CopyFrom(source); camera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            var mode = canvas.renderMode; var oldCamera = canvas.worldCamera;
            string original = JsonUtility.ToJson(UISettingsManager.Current);
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .5f;
                foreach (var sample in new[] { new Vector3(1920, 1080, 1), new Vector3(1280, 720, 1.25f), new Vector3(1920, 1200, .75f), new Vector3(3440, 1440, 1) })
                {
                    var settings = JsonUtility.FromJson<UISettingsData>(original); settings.Scale = sample.z;
                    UISettingsManager.Save(settings, false);
                    int width = (int)sample.x, height = (int)sample.y;
                    var texture = new RenderTexture(width, height, 24); texture.Create(); camera.targetTexture = texture;
                    yield return null; Canvas.ForceUpdateCanvases(); camera.Render();
                    Transform panel = ui.State == UIState.MainMenu ? ui.Main.transform : ui.State == UIState.Paused ? ui.Pause.transform : ui.Settings.transform;
                    var title = panel.GetComponentInChildren<UIStrokeTitle>();
                    Assert.That(title, Is.Not.Null);
                    Assert.That(title.canvasRenderer, Is.Not.Null, "Title needs a CanvasRenderer");
                    Assert.That(title.canvasRenderer.GetColor().a, Is.GreaterThan(0), "Title renderer must be visible");
                    foreach (var control in panel.GetComponentsInChildren<Selectable>().Where(s => s.gameObject.activeInHierarchy))
                    {
                        if (control.transform.IsChildOf(ui.Settings.Resolution.template)) continue;
                        var corners = new Vector3[4]; ((RectTransform)control.transform).GetWorldCorners(corners);
                        foreach (var corner in corners)
                        {
                            var screen = camera.WorldToScreenPoint(corner);
                            Assert.That(screen.x, Is.InRange(0, width), control.name + " x");
                            Assert.That(screen.y, Is.InRange(0, height), control.name + " y");
                        }
                    }
                    if (sample.z == 1 || width == 1280) Save(texture, name + "-" + width + "x" + height);
                    camera.targetTexture = null; texture.Release(); Object.Destroy(texture);
                }
            }
            finally
            {
                camera.targetTexture = null; canvas.renderMode = mode; canvas.worldCamera = oldCamera;
                UISettingsManager.Save(JsonUtility.FromJson<UISettingsData>(original), false); Object.Destroy(go);
            }
        }
        static void Save(RenderTexture texture, string name)
        {
            var active = RenderTexture.active; RenderTexture.active = texture;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); image.Apply();
            Directory.CreateDirectory(Reports); File.WriteAllBytes(Reports + "/" + name + ".png", image.EncodeToPNG());
            RenderTexture.active = active; Object.Destroy(image);
        }
    }
}
