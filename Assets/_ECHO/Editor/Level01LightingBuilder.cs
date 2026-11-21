using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace EchoGame.Editor
{
    public static class Level01LightingBuilder
    {
        const string Art = "Assets/_ECHO/Art/Level01";
        public const string Reports = "C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-21-Level01Lighting";
        public static void ConfigureFixture(Transform fixture)
        {
            var child = fixture.Find("Downlight");
            if (!child) { child = new GameObject("Downlight").transform; child.SetParent(fixture, false); }
            child.localPosition = new Vector3(0, -.14f, 0);
            child.localRotation = Quaternion.Euler(90, 0, 0);
            var light = child.GetComponent<Light>(); if (!light) light = child.gameObject.AddComponent<Light>();
            light.type = LightType.Spot; light.range = 7; light.intensity = 2.3f;
            light.color = new Color(.72f, .85f, .91f); light.innerSpotAngle = 80; light.spotAngle = 115;
            light.shadows = LightShadows.None; light.bounceIntensity = 0; light.enabled = true;
        }
        public static void ConfigureLighting(Transform lighting)
        {
            foreach (var l in lighting.GetComponentsInChildren<Light>(true))
                if (l.name.StartsWith("Sector fill ")) Object.DestroyImmediate(l.gameObject);
            foreach (var fixture in lighting.GetComponentsInChildren<Transform>(true).Where(t => t.name == "CeilingLamp").ToArray())
            {
                ConfigureFixture(fixture);
                // Four single-cone shadow maps around the partition replace a six-face point shadow.
                var light = fixture.Find("Downlight").GetComponent<Light>();
                if (Mathf.Abs(fixture.position.z - 10) < .1f || Mathf.Abs(fixture.position.z - 18) < .1f)
                { light.shadows = LightShadows.Soft; light.shadowCustomResolution = 256; light.shadowBias = .04f; light.shadowNormalBias = .15f; }
            }
        }
        [MenuItem("Tools/ECHO/Art/Fix Level 01 lighting")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Exit Play mode and save the current scene before updating lighting.");
            var scene = EditorSceneManager.OpenScene(Level01Builder.ScenePath);
            Directory.CreateDirectory(Reports); Capture("Before");
            var colliders = Object.FindObjectsByType<Collider>().Select(c => c.GetEntityId()).OrderBy(x => x).ToArray();
            var lamp = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/Lamp.mat");
            lamp.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            lamp.SetColor("_EmissionColor", new Color(.72f, .85f, .91f) * 4);
            lamp.EnableKeyword("_EMISSION"); EditorUtility.SetDirty(lamp);
            var prefab = PrefabUtility.LoadPrefabContents(Art + "/Prefabs/CeilingLamp.prefab");
            try { ConfigureFixture(prefab.transform); PrefabUtility.SaveAsPrefabAsset(prefab, Art + "/Prefabs/CeilingLamp.prefab"); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var lighting = GameObject.Find("Level01_ArtRoot/Lighting");
            if (!lighting) throw new InvalidOperationException("Level01 lighting root was not found.");
            ConfigureLighting(lighting.transform);
            var after = Object.FindObjectsByType<Collider>().Select(c => c.GetEntityId()).OrderBy(x => x).ToArray();
            if (!colliders.SequenceEqual(after)) throw new InvalidOperationException("Lighting must preserve gameplay colliders.");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Capture("After");
            var lights = lighting.GetComponentsInChildren<Light>().Where(l => l.enabled).ToArray();
            File.WriteAllText(Reports + "/applied.txt", DateTime.UtcNow.ToString("O") +
                "\nLamp emission: " + lamp.IsKeywordEnabled("_EMISSION") + "; GI flags: " + lamp.globalIlluminationFlags +
                "\nDownward fixtures: " + lights.Length + "; point lights: " + lights.Count(l => l.type == LightType.Point) +
                "; shadowed fixtures: " + lights.Count(l => l.shadows != LightShadows.None) +
                "\nGameplay colliders preserved: " + colliders.Length);
            EditorSceneManager.OpenScene(Level01Builder.ScenePath);
            Debug.Log("ECHO: Level_01 lamp emission restored and ceiling hotspots removed.");
        }
        static void Capture(string stage)
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            var go = new GameObject("Lighting review camera"); var camera = go.AddComponent<Camera>();
            camera.CopyFrom(player.View.GetComponent<Camera>());
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            camera.allowHDR = true;
            go.transform.position = new Vector3(0, 1.65f, 5);
            go.transform.LookAt(new Vector3(0, 3.7f, 10)); Save(camera, stage + "-Ceiling.png");
            go.transform.LookAt(new Vector3(0, 1.65f, 14)); Save(camera, stage + "-Room.png");
            Object.DestroyImmediate(go);
        }
        static void Save(Camera camera, string filename)
        {
            var rt = new RenderTexture(1440, 810, 24); var active = RenderTexture.active;
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            var image = new Texture2D(1440, 810, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1440, 810), 0, 0); image.Apply();
            File.WriteAllBytes(Reports + "/" + filename, image.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = active; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
        }
    }
}
