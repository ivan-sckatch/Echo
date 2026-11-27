using EchoGame.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace EchoGame.Editor
{
    public static class EchoMenuSceneBuilder
    {
        const string Art = "Assets/_ECHO/Art/Modular/";
        static GameObject Module(Transform parent, string section, string name, Vector3 position, float yaw = 0)
        {
            string path = Art + section + "/Prefabs/" + name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) throw new System.InvalidOperationException("Missing ECHO module " + path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            foreach (var collider in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            return go;
        }
        static void Box(Transform parent, string name, Vector3 position, Vector3 size, string material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, false); go.transform.position = position; go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + material);
        }
        public static void Build(bool additive = false)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, additive ? NewSceneMode.Additive : NewSceneMode.Single);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.25f, .29f, .33f);
            RenderSettings.skybox = null; RenderSettings.fog = false;
            var root = new GameObject("Menu corridor — ECHO modules").transform;
            // The near wall occupies the left of the camera frame; no UI texture backdrop.
            for (int i = 0; i < 3; i++)
                Module(root, "Wall01", "ECHO_Wall_Plain_250x380", new Vector3(-2.5f - i * 2.5f, 0, 0));
            // Close the near right return too: ultrawide cameras must not see outside the set.
            for (int i = 0; i < 2; i++)
                Module(root, "Wall01", "ECHO_Wall_Plain_250x380", new Vector3(5 + i * 2.5f, 0, 0));
            for (int z = 0; z < 6; z++)
            {
                float pz = 1.25f + z * 2.5f;
                Module(root, "Wall01", "ECHO_Wall_" + (z == 1 || z == 4 ? "Service" : "Plain") + "_250x380", new Vector3(-1.4f, 0, pz), -90);
                Module(root, "Wall01", "ECHO_Wall_" + (z == 2 ? "Service" : "Plain") + "_250x380", new Vector3(3.9f, 0, pz), 90);
                for (int x = 0; x < 2; x++)
                {
                    Module(root, "Floor01", "ECHO_Floor_Plain_250", new Vector3(x * 2.5f, 0, pz));
                    var ceiling = Module(root, "Ceiling01", "ECHO_Ceiling_" + (z % 2 == 0 ? "08_TwinEdge" : "01_Plain") + "_250", new Vector3(x * 2.5f, 3.8f, pz));
                    foreach (var light in ceiling.GetComponentsInChildren<Light>())
                    {
                        light.color = new Color(.78f, .88f, .94f); light.intensity = 1.7f;
                        light.shadows = LightShadows.None;
                    }
                }
            }
            for (int x = -2; x <= 1; x++)
                Module(root, "Floor01", "ECHO_Floor_Plain_250", new Vector3(x * 2.5f, 0, -1.25f));
            Module(root, "Door01", "ECHO_Door_05_Diagonal_300x340", new Vector3(1.25f, 0, 15));
            Box(root, "Left door surround", new Vector3(-.85f, 1.9f, 15.05f), new Vector3(1.15f, 3.8f, .3f), "Wall01/Materials/Graphite.mat");
            Box(root, "Right door surround", new Vector3(3.35f, 1.9f, 15.05f), new Vector3(1.15f, 3.8f, .3f), "Wall01/Materials/Graphite.mat");
            Box(root, "Door lintel", new Vector3(1.25f, 3.65f, 15.05f), new Vector3(3.1f, .3f, .3f), "Wall01/Materials/Graphite.mat");
            Box(root, "Foreground graphite jamb", new Vector3(-1.22f, 1.9f, -.03f), new Vector3(.14f, 3.8f, .28f), "Wall01/Materials/Graphite.mat");
            // A soft fill brings out the surface under the title without flattening the corridor.
            var fill = new GameObject("Foreground soft fill").AddComponent<Light>(); fill.transform.SetParent(root);
            fill.transform.position = new Vector3(-1, 2.7f, -2.4f); fill.type = LightType.Point;
            fill.color = new Color(.72f, .8f, .86f); fill.intensity = .65f; fill.range = 5; fill.shadows = LightShadows.None;
            var camera = new GameObject("Menu Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera"; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.045f, .055f, .065f); camera.fieldOfView = 58;
            camera.nearClipPlane = .08f; camera.farClipPlane = 60;
            camera.transform.position = new Vector3(-.65f, 1.8f, -3.25f);
            camera.transform.LookAt(new Vector3(1.05f, 1.8f, 12));
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var ui = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ECHO/UI/Prefabs/EchoUI.prefab"));
            ui.GetComponent<UIManager>().IsMainMenu = true;
            EchoUIBuilder.EnsureEventSystem();
            EditorSceneManager.SaveScene(scene, EchoUIBuilder.MenuPath);
        }
    }
}
