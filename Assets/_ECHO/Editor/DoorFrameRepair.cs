using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace EchoGame.Editor
{
    public static class DoorFrameRepair
    {
        public const string Reports="C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-23-GlobalDoorFrames";
        static readonly string[] Scenes={"Level_01","Level_02","Level_03","Level_04","Level_05_Soglasie"};
        static bool[] priorStates;
        static DoorController[] captureDoors;
        static Camera review;static int frame;static string label;
        public static void Apply()
        {
            if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play Mode and save current scene before frame repair.");
            Directory.CreateDirectory(Reports);string stamp=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            var prefabs=new[]{"Assets/_ECHO/Art/Modular/Door01/Prefabs/ECHO_Door_01_Basic_300x340.prefab","Assets/_ECHO/Art/Modular/Door01/Prefabs/ECHO_Door_03_Window_300x340.prefab","Assets/_ECHO/Art/Modular/Door01/Prefabs/ECHO_Door_05_Diagonal_300x340.prefab","Assets/_ECHO/Art/Level01/Prefabs/DoorFrame.prefab"};
            foreach(string path in prefabs)
            {
                File.Copy(path,Reports+"/"+Path.GetFileNameWithoutExtension(path)+"-before-"+stamp+".prefab");
                var root=PrefabUtility.LoadPrefabContents(path);
                try{if(root.GetComponent<DoorController>())DoorFrameGeometry.FitModular(root.transform);else DoorFrameGeometry.FitLegacy(root.transform);PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            var report=new List<string>();
            foreach(string name in Scenes)
            {
                string path="Assets/_ECHO/Scenes/Levels/"+name+".unity";
                File.Copy(path,Reports+"/"+name+"-before-"+stamp+".unity");var scene=EditorSceneManager.OpenScene(path);
                var originalColliders=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(c=>c.GetEntityId()).OrderBy(i=>i).ToArray();
                var doors=Object.FindObjectsByType<DoorController>(FindObjectsSortMode.None);int modular=0;
                foreach(var door in doors)
                {
                    var root=PrefabUtility.GetOutermostPrefabInstanceRoot(door.Panel.gameObject);
                    if(root&&root.transform.Find("Frame guide 1")){DoorFrameGeometry.FitModular(root.transform);modular++;}
                }
                foreach(var instance in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="DoorFrame"))DoorFrameGeometry.FitLegacy(instance);
                int structural=DoorFrameGeometry.FitStructuralReveals(scene);
                if(!originalColliders.SequenceEqual(Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(c=>c.GetEntityId()).OrderBy(i=>i)))throw new InvalidOperationException("Frame repair changed colliders in "+name);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                report.Add(name+":doors="+doors.Length+",modular="+modular+",structural render meshes="+structural+",colliders retained="+originalColliders.Length);
            }
            AssetDatabase.SaveAssets();File.WriteAllLines(Reports+"/repaired.txt",report);EditorSceneManager.OpenScene(Level03Builder.Path);
        }
        public static void CaptureBefore()=>Capture("Before");
        public static void CaptureAfter()=>Capture("After");
        static void Capture(string stage)
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Idle Editor required.");
            Directory.CreateDirectory(Reports+"/"+stage);label=stage;frame=0;EditorSceneManager.OpenScene(Level03Builder.Path);
            captureDoors=Object.FindAnyObjectByType<Level03Controller>().Chambers.Select(c=>c.Entrance).ToArray();
            priorStates=captureDoors.Select(d=>d.Command).ToArray();foreach(var d in captureDoors){d.SetCommand(true);d.Tick(10,Vector3.one*1000);}
            review=new GameObject("Door frame motion review").AddComponent<Camera>();review.CopyFrom(Object.FindAnyObjectByType<TimeCycleManager>().Player.View.GetComponent<Camera>());review.fieldOfView=55;
            Next();
        }
        static void Next()
        {
            if(frame==36)
            {
                Object.DestroyImmediate(review.gameObject);EditorSceneManager.OpenScene(Level03Builder.Path);File.WriteAllText(Reports+"/"+label+"-capture-complete.txt",DateTime.UtcNow.ToString("O"));return;
            }
            var door=captureDoors[frame/12];float t=frame%12/11f;
            review.transform.position=door.transform.TransformPoint(new Vector3(.60f+Mathf.Sin(t*Mathf.PI)*.15f,1.7f,.65f+t*.70f));
            review.transform.LookAt(door.transform.TransformPoint(new Vector3(1.5f,2.1f,0)));
            EditorApplication.QueuePlayerLoopUpdate();SceneView.RepaintAll();EditorApplication.delayCall+=Save;
        }
        static void Save()
        {
            var rt=new RenderTexture(800,600,24);var active=RenderTexture.active;review.targetTexture=rt;review.Render();RenderTexture.active=rt;
            var image=new Texture2D(800,600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,600),0,0);image.Apply();
            File.WriteAllBytes(Reports+"/"+label+"/Frame-"+frame.ToString("D2")+".png",image.EncodeToPNG());
            review.targetTexture=null;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);frame++;EditorApplication.delayCall+=Next;
        }
    }
}
