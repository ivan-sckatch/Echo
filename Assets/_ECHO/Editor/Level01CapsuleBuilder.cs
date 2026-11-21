using System;
using System.IO;
using EchoGame.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace EchoGame.Editor
{
    public static class Level01CapsuleBuilder
    {
        const string PrefabPath = "Assets/_ECHO/Art/Level01/Prefabs/Echo_Level01.prefab";
        const string Reports = "C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-20-Level01Capsule";
        [MenuItem("Tools/ECHO/Art/Use capsule on Level 01")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Exit Play mode and save the current scene before restoring the capsule.");
            var scene = EditorSceneManager.OpenScene(Level01Builder.ScenePath);
            var cycles = Object.FindAnyObjectByType<TimeCycleManager>();
            var hero = cycles.Player.transform.Find("Hero Civilian");
            if (hero) Object.DestroyImmediate(hero.gameObject);
            var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var playback = prefab.GetComponent<EchoPlayback>();
                Object.DestroyImmediate(playback.Body.gameObject);
                var body = new GameObject("Capsule projection"); body.transform.SetParent(prefab.transform, false);
                body.transform.localPosition = Vector3.up * .9f; playback.Body = body.transform;
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule); capsule.name = "Capsule";
                capsule.transform.SetParent(body.transform, false); capsule.transform.localScale = new Vector3(.64f, .9f, .64f);
                Object.DestroyImmediate(capsule.GetComponent<Collider>());
                var renderer = capsule.GetComponent<Renderer>();
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_ECHO/Art/Level01/Materials/EchoProjection.mat");
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                var art = prefab.GetComponent<EchoProjectionArt>();
                art.Silhouette = new[] { renderer };
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            cycles.Echoes.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<EchoPlayback>();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Reports);
            File.WriteAllText(Reports + "/restored.txt", "Level_01: player CharacterController capsule retained; civilian model removed. Echo uses one visual capsule. No new model animation.\n" + DateTime.UtcNow.ToString("O"));
            Debug.Log("ECHO: capsule restored on Level_01; rejected civilian model removed.");
        }
    }
}
