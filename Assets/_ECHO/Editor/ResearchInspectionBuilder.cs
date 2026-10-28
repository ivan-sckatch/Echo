using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoGame.Editor
{
    public static class ResearchInspectionBuilder
    {
        public static void Clean(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true).ToArray())
            {
                var sign = text is TextMeshPro && text.transform.parent ? text.transform.parent.gameObject : text.gameObject;
                string n = sign.name;
                if (n.StartsWith("Door display ") || n.StartsWith("Button label ") || n.StartsWith("Chamber label ") ||
                    n.StartsWith("Camera display ") || n == "Node U1 label" || n == "Node U2 label")
                    Object.DestroyImmediate(sign);
                else if (n == "Start map") text.text = "ECHO / 03 · ПОЙМАЙ МОМЕНТ\nТЕХНИЧЕСКАЯ ГАЛЕРЕЯ\nОСНОВНОЙ КОРИДОР";
                else if (n == "Gallery direction") text.text = "ТЕХНИЧЕСКАЯ ГАЛЕРЕЯ";
            }
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var door in root.GetComponentsInChildren<TimedDoor>(true)) door.Indicator = null;
                foreach (var door in root.GetComponentsInChildren<DoorController>(true))
                {
                    // The open doorway remains inspectable; this trigger never blocks movement or active-cycle beams.
                    if (!door.GetComponent<BoxCollider>())
                    {
                        var area = door.gameObject.AddComponent<BoxCollider>(); area.isTrigger = true;
                        area.center = Vector3.up * 1.7f; area.size = new Vector3(3.4f, 3.4f, .3f);
                    }
                }
                foreach (var camera in root.GetComponentsInChildren<SecurityCameraController>(true))
                {
                    camera.Display = null;
                    if (!camera.Head.GetComponent<BoxCollider>())
                    {
                        var collider = camera.Head.gameObject.AddComponent<BoxCollider>();
                        collider.size = new Vector3(.5f, .32f, .8f); collider.center = Vector3.forward * .05f;
                    }
                    camera.Head.GetComponent<BoxCollider>().isTrigger = true;
                }
            }
        }
        [MenuItem("Tools/ECHO/Apply research inspection")]
        public static void Apply()
        {
            if (EditorSceneManager.GetActiveScene().isDirty)
            { Debug.LogError("Save the current scene before applying inspection changes."); return; }
            foreach (string name in new[] { "Level_03", "Level_04" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/_ECHO/Scenes/Levels/" + name + ".unity");
                Clean(scene); EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("ECHO research inspection scenes saved.");
        }
    }
}

