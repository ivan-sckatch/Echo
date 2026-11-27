using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace EchoGame.Editor
{
    public static partial class EchoWallModuleBuilder
    {
        public const string VariantReports="C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-21-EchoWallVariants";
        // Wear belongs to independent decal projectors, never to a repeating wall material.
        static Material CleanPaint(string name, Color color)
        {
            var material=Mat(name,color,.06f,.28f);
            foreach(string property in new[]{"_BaseMap","_MainTex","_DetailAlbedoMap","_DetailNormalMap"})
                if(material.HasProperty(property))material.SetTexture(property,null);
            material.DisableKeyword("_DETAIL_MULX2");material.DisableKeyword("_DETAIL_SCALED");
            EditorUtility.SetDirty(material);return material;
        }
        public static void CleanBaseMaterials()
        {
            CleanPaint("Composite clean",new Color(.57f,.58f,.55f));
            CleanPaint("Composite worn",new Color(.57f,.56f,.53f));
            CleanPaint("Service paint",new Color(.54f,.56f,.55f));
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Tools/ECHO/Art/Try three own wall variants in Level 02")]
        public static void ApplyVariants()
        {
            if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;EditorApplication.delayCall+=ApplyVariants;return;}
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene first.");
            Directory.CreateDirectory(VariantReports);File.Copy(Level02Builder.ScenePath,VariantReports+"/Level_02-before-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".unity");
            var scene=EditorSceneManager.OpenScene(Level02Builder.ScenePath);
            var colliders=Object.FindObjectsByType<Collider>().Select(c=>c.GetEntityId()).OrderBy(i=>i).ToArray();
            foreach(string name in new[]{"ECHO Wall01 trial","ECHO Wall variants trial"}){var old=GameObject.Find(name);if(old)Object.DestroyImmediate(old);}
            // Upgrade original box UVs in place so prefab references stay stable.
            foreach(string guid in AssetDatabase.FindAssets("t:Mesh",new[]{Art+"/Meshes"}))
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));var bounds=mesh.bounds;var vertices=mesh.vertices;var normals=mesh.normals;
                mesh.uv=vertices.Select((v,i)=>Mathf.Abs(normals[i].z)>.5f?new Vector2((v.x-bounds.min.x)/bounds.size.x,(v.y-bounds.min.y)/bounds.size.y):new Vector2((v.z-bounds.min.z)/bounds.size.z,(v.y-bounds.min.y)/bounds.size.y)).ToArray();EditorUtility.SetDirty(mesh);
            }
            metal=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/Graphite.mat");recess=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/Recess.mat");marker=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/Turquoise marker.mat");
            var paints=new[]{CleanPaint("Composite clean",new Color(.57f,.58f,.55f)),CleanPaint("Composite worn",new Color(.57f,.56f,.53f)),CleanPaint("Service paint",new Color(.54f,.56f,.55f))};
            var hatchPaint=Mat("Hatch steel",new Color(.23f,.28f,.29f),.4f,.28f);
            var trial=new GameObject("ECHO Wall variants trial");trial.transform.SetParent(GameObject.Find("_Environment").transform,false);
            string[] kinds={"Plain","Light","Service"};
            for(int variant=0;variant<3;variant++)
            {
                var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Prefabs/ECHO_Wall_Light_250x380.prefab"));
                PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);root.name="ECHO_Wall_"+kinds[variant]+"_250x380";
                foreach(var r in root.GetComponentsInChildren<MeshRenderer>())if(r.name=="Main composite"||r.name=="Lower composite")r.sharedMaterial=paints[variant];
                // Panels almost meet; joints are recessed rather than protruding shadow-casting posts.
                foreach(string part in new[]{"Frame -1","Frame 1","Main composite","Lower composite","Continuous main","Continuous lower","Recessed joint"})
                {var child=root.transform.Find(part);if(child)Object.DestroyImmediate(child.gameObject);}
                Part(root.transform,"Continuous main",new Vector3(0,1.98f,-.069f),new Vector3(2.488f,2.52f,.09f),paints[variant],.003f);
                Part(root.transform,"Continuous lower",new Vector3(0,.50f,-.069f),new Vector3(2.488f,.39f,.09f),paints[variant],.003f);
                foreach(string part in new[]{"Plinth","Upper rail","Upper service panel"})
                {var child=root.transform.Find(part);if(child){var mesh=child.GetComponent<MeshFilter>().sharedMesh;child.localScale=new Vector3((part=="Upper service panel"?2.48f:2.5f)/mesh.bounds.size.x,1,1);}}
                Part(root.transform,"Recessed joint",new Vector3(1.247f,1.77f,-.09f),new Vector3(.006f,2.94f,.015f),recess,.001f);
                var fixture=root.GetComponentInChildren<Light>();
                if(fixture){fixture.range=6;fixture.innerSpotAngle=70;fixture.spotAngle=155;fixture.transform.localPosition=new Vector3(0,3.22f,-.60f);}
                if(variant!=1)foreach(string part in new[]{"Light housing","Light diffuser","Fixture downlight"}){var child=root.transform.Find(part);if(child)Object.DestroyImmediate(child.gameObject);}
                if(variant==0)
                {
                    Part(root.transform,"Plain lower stripe",new Vector3(0,.9f,-.12f),new Vector3(2.28f,.06f,.008f),marker,.002f);
                }
                if(variant==2)
                {
                    Part(root.transform,"Service recess",new Vector3(0,1.9f,-.132f),new Vector3(1.40f,1.60f,.055f),recess);
                    Part(root.transform,"Inspection hatch",new Vector3(0,1.98f,-.17f),new Vector3(1.28f,1.36f,.06f),hatchPaint);
                    Part(root.transform,"Hatch label",new Vector3(-.35f,2.43f,-.205f),new Vector3(.32f,.045f,.006f),marker,.002f);
                    Part(root.transform,"Recessed handle",new Vector3(.43f,1.92f,-.21f),new Vector3(.09f,.28f,.025f),recess,.004f);
                    for(int i=0;i<6;i++)Part(root.transform,"Vent slot "+i,new Vector3(-.12f,1.58f+i*.055f,-.205f),new Vector3(.65f,.022f,.012f),recess,.004f);
                    foreach(float x in new[]{-.55f,.55f})foreach(float y in new[]{1.40f,2.56f})Part(root.transform,"Hatch screw "+x+" "+y,new Vector3(x,y,-.208f),new Vector3(.027f,.027f,.015f),marker,.003f);
                    // Keep the wall's sector marker above the complete service assembly.
                    foreach(Transform child in root.transform)
                        if(child.name=="Service recess"||child.name=="Inspection hatch"||child.name=="Hatch label"||child.name=="Recessed handle"||child.name.StartsWith("Vent slot ")||child.name.StartsWith("Hatch screw "))
                            child.localPosition += Vector3.down * .30f;
                }
                string path=Art+"/Prefabs/"+root.name+".prefab";PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));instance.transform.SetParent(trial.transform,false);instance.transform.position=new Vector3(8.70f,.02f,2.4f+variant*2.5f);instance.transform.rotation=Quaternion.Euler(0,90,0);
            }
            if(!colliders.SequenceEqual(Object.FindObjectsByType<Collider>().Select(c=>c.GetEntityId()).OrderBy(i=>i)))throw new InvalidOperationException("Wall variants changed colliders.");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(VariantReports+"/verified.txt",DateTime.UtcNow.ToString("O")+"\nPlain, Light, Service prefabs; clean base materials; wear is supplied separately as WallWear01 decals. Right wall x8.70, z2.4/4.9/7.4.\nGameplay colliders preserved: "+colliders.Length+"; decorative colliders: 0. Previous single trial replaced.");
            EditorSceneManager.OpenScene(Level02Builder.ScenePath);EditorApplication.QueuePlayerLoopUpdate();
        }
        public static void CaptureVariants()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            var go=new GameObject("Wall variants camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Object.FindAnyObjectByType<TimeCycleManager>().Player.View.GetComponent<Camera>());camera.fieldOfView=65;
            var positions=new[]{new Vector3(.5f,1.8f,4.9f),new Vector3(4.5f,1.8f,4.4f)};var targets=new[]{new Vector3(8.5f,1.9f,4.9f),new Vector3(8.5f,1.9f,6.4f)};
            for(int i=0;i<positions.Length;i++){
                camera.fieldOfView=i==0?42:65;go.transform.position=positions[i];go.transform.LookAt(targets[i]);var rt=new RenderTexture(1600,1000,24);var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes(VariantReports+"/After-"+i+".png",tex.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
            }Object.DestroyImmediate(go);
        }
    }
}
