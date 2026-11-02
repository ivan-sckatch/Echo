using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using EchoGame.Visuals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoGame.Tests
{
    public class Level01ArtTests
    {
        const string Reports = "C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-02-Level01Art";
        [UnityTest]
        public IEnumerator ProjectionAndFrameTimingAtNativeResolution()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Level_01"); yield return null;
            int vsync = QualitySettings.vSyncCount, target = Application.targetFrameRate;
            try
            {
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            if (!Application.isEditor) Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            var c = Object.FindAnyObjectByType<TimeCycleManager>();
            c.Echoes.Add(new EchoRecording(new List<EchoFrame> {
                new EchoFrame(0, new Vector3(6, 0, 9), 0, 0), new EchoFrame(30, new Vector3(6, 0, 9), 0, 0) }));
            var cc = c.Player.GetComponent<CharacterController>(); cc.enabled = false;
            c.Player.transform.position = new Vector3(2, .02f, 7); cc.enabled = true;
            c.Player.transform.rotation = Quaternion.LookRotation(new Vector3(4, 0, 2));
            yield return new WaitForSeconds(.8f);
            var echo = c.Echoes.Echoes[0]; Assert.That(echo.IsActive, Is.True);
            var projection = echo.GetComponent<EchoProjectionArt>();
            Assert.That(projection.Silhouette.All(r => r.sharedMaterial.shader.isSupported), Is.True);
            Assert.That(echo.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(projection.ArrivalRing.gameObject.activeSelf, Is.False);
            Directory.CreateDirectory(Reports);
            var camera = c.Player.View.GetComponent<Camera>();
            var rt = new RenderTexture(1920, 1080, 24); var previous = RenderTexture.active;
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
            File.WriteAllBytes(Reports + "/Echo-" + (Application.isEditor ? "Editor" : "Windows") + ".png", image.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = previous; rt.Release(); Object.Destroy(rt); Object.Destroy(image);
            var timings = new List<float>();
            for (int i = 0; i < 240; i++) { yield return null; timings.Add(Time.unscaledDeltaTime * 1000); }
            timings.Sort();
            File.WriteAllText(Reports + "/Timing-" + (Application.isEditor ? "Editor" : "Windows") + ".txt",
                $"{SystemInfo.processorType}\n{SystemInfo.graphicsDeviceName}\n{SystemInfo.systemMemorySize} MB RAM\n" +
                $"{Screen.width}x{Screen.height}; editor={Application.isEditor}; 240 frames; fixed viewpoint, one echo, active plate and door.\n" +
                $"Mean {timings.Average():F2} ms; p95 {timings[227]:F2} ms; p99 {timings[237]:F2} ms; max {timings[239]:F2} ms.\n");
            }
            finally { QualitySettings.vSyncCount = vsync; Application.targetFrameRate = target; }
        }
        [UnityTest]
        public IEnumerator DecorationIsNonBlockingAndTracksRealMechanisms()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Level_01"); yield return null;
            var root = GameObject.Find("Level01_ArtRoot"); Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty);
            var c = Object.FindAnyObjectByType<TimeCycleManager>();
            var art = root.GetComponentInChildren<MechanismArt>(); Assert.That(art, Is.Not.Null);
            Assert.That(art.Plate, Is.SameAs(c.Puzzle.Plate)); Assert.That(art.Door, Is.SameAs(c.Puzzle.Door));
            c.Game.SetState(GameState.Paused);
            c.Puzzle.Plate.SetActive(true); c.Puzzle.Door.SetCommand(true); c.Puzzle.Door.Tick(1, Vector3.zero);
            yield return null;
            Assert.That(art.Signals.All(r => r.sharedMaterial == art.Active), Is.True);
            Assert.That(art.Status.text, Does.Contain("OPEN"));
            Assert.That(c.Puzzle.Door.Panel.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(1));
            Assert.That(c.Echoes.MaximumEchoes, Is.EqualTo(1));
            Assert.That(c.Echoes.Prefab.GetComponent<EchoProjectionArt>(), Is.Not.Null);
            c.Puzzle.Plate.SetActive(false); c.Puzzle.Door.ResetMechanism(); yield return null;
            Assert.That(art.Signals.All(r => r.sharedMaterial == art.Idle), Is.True);
            Assert.That(art.Status.text, Does.Contain("LOCKED"));
            c.Game.SetState(GameState.Playing);
        }
    }
}
