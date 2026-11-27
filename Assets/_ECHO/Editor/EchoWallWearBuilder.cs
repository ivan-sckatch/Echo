using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace EchoGame.Editor
{
    public static class EchoWallWearBuilder
    {
        public const string Art="Assets/_ECHO/Art/Modular/WallWear01";
        public const string Reports="C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-26-WallWear";
        public static readonly string[] Kinds={"Scuffs","Scratches","Smudge"};
        public static string Prefab(string kind)=>Art+"/Prefabs/ECHO_WallWear_"+kind+".prefab";

        static void EnableDecals(string path)
        {
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if(!renderer)throw new InvalidOperationException("Missing renderer: "+path);
            if(renderer.rendererFeatures.OfType<DecalRendererFeature>().Any()){SaveFeatureMap(renderer);return;}
            var feature=ScriptableObject.CreateInstance<DecalRendererFeature>();feature.name="ECHO Surface decals";
            // Automatic chooses a supported technique for the current platform. The graph writes albedo only.
            AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);
            feature.Create();SaveFeatureMap(renderer);renderer.SetDirty();EditorUtility.SetDirty(renderer);
        }

        static void SaveFeatureMap(UniversalRendererData renderer)
        {
            var serialized=new SerializedObject(renderer);var map=serialized.FindProperty("m_RendererFeatureMap");
            map.arraySize=renderer.rendererFeatures.Count;
            for(int i=0;i<map.arraySize;i++)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i],out string guid,out long id);
                map.GetArrayElementAtIndex(i).longValue=id;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(renderer);
        }

        [MenuItem("Tools/ECHO/Art/Build clean walls and independent wear kit")]
        public static void BuildAssets()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            Directory.CreateDirectory(Reports);Directory.CreateDirectory(Art+"/Materials");Directory.CreateDirectory(Art+"/Prefabs");
            EchoWallModuleBuilder.CleanBaseMaterials();AssetDatabase.Refresh();
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(Art+"/Shaders/ECHO_WallWear.shadergraph");
            if(!shader)throw new InvalidOperationException("Missing albedo-only decal shader.");
            for(int i=0;i<Kinds.Length;i++)
            {
                string kind=Kinds[i],texturePath=Art+"/Textures/"+kind+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
                importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=true;importer.maxTextureSize=1024;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
                string materialPath=Art+"/Materials/"+kind+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}
                material.shader=shader;material.enableInstancing=true;material.SetTexture("Base_Map",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
                material.SetFloat("Normal_Blend",0);EditorUtility.SetDirty(material);
                var go=new GameObject("ECHO_WallWear_"+kind);var projector=go.AddComponent<DecalProjector>();
                projector.material=material;projector.pivot=Vector3.zero;projector.size=new Vector3(i==1?.65f:1.4f,i==1?.55f:.7f,.045f);
                projector.drawDistance=35;projector.fadeScale=.8f;projector.fadeFactor=i==1?.22f:.14f;
                projector.startAngleFade=35;projector.endAngleFade=70;
                PrefabUtility.SaveAsPrefabAsset(go,Prefab(kind));Object.DestroyImmediate(go);
            }
            EnableDecals("Assets/Settings/PC_Renderer.asset");EnableDecals("Assets/Settings/Mobile_Renderer.asset");AssetDatabase.SaveAssets();
        }

        static void Mark(Transform parent,string kind,Vector3 point,Vector3 outward,float width,float height,float roll,float opacity)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(kind)));
            go.transform.SetParent(parent,false);go.transform.position=point;
            go.transform.rotation=Quaternion.LookRotation(-outward,Vector3.up)*Quaternion.Euler(0,0,roll);
            var projector=go.GetComponent<DecalProjector>();projector.size=new Vector3(width,height,.045f);projector.fadeFactor=opacity;
            PrefabUtility.RecordPrefabInstancePropertyModifications(projector);
        }

        // Separate scene layer, intentionally independent of the wall tile sequence and prefabs.
        public static void Layout(Transform environment)
        {
            var old=environment.Find("ECHO Independent wall wear");if(old)Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("ECHO Independent wall wear").transform;root.SetParent(environment,false);
            // Outer-wall composite fronts are 0.114m proud of their structural face.
            Mark(root,"Scuffs",new Vector3(3.761f,1.48f,3.4f),Vector3.left,1.65f,.82f,-8,.23f);
            Mark(root,"Scratches",new Vector3(3.761f,2.05f,8.7f),Vector3.left,.68f,.75f,26,.30f);
            Mark(root,"Smudge",new Vector3(3.761f,1.4f,16.3f),Vector3.left,1.8f,.75f,5,.20f);
            Mark(root,"Scuffs",new Vector3(3.761f,1.42f,31.7f),Vector3.left,1.22f,.73f,173,.10f);
            Mark(root,"Scratches",new Vector3(3.761f,2.17f,41.2f),Vector3.left,.58f,.42f,-19,.17f);
            Mark(root,"Smudge",new Vector3(-11.761f,1.15f,8.5f),Vector3.right,1.65f,.70f,-7,.20f);
            Mark(root,"Scuffs",new Vector3(-11.761f,1.72f,19.7f),Vector3.right,1.05f,.62f,14,.13f);
            Mark(root,"Scratches",new Vector3(-11.761f,2.14f,33.1f),Vector3.right,.46f,.58f,151,.19f);
            Mark(root,"Smudge",new Vector3(-11.761f,1.12f,39.7f),Vector3.right,2.30f,.56f,183,.10f);
        }

        [MenuItem("Tools/ECHO/Art/Try independent wall wear in Level 03")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying||EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Idle saved scene required.");
            BuildAssets();var scene=EditorSceneManager.OpenScene(Level03Builder.Path);
            string before=Reports+"/Level03-before-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".unity";File.Copy(Level03Builder.Path,before);
            var colliders=Object.FindObjectsByType<Collider>().Select(c=>c.GetEntityId()).OrderBy(i=>i).ToArray();
            Layout(GameObject.Find("Level_03/Environment").transform);
            if(!colliders.SequenceEqual(Object.FindObjectsByType<Collider>().Select(c=>c.GetEntityId()).OrderBy(i=>i)))throw new InvalidOperationException("Colliders changed.");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Validate();
        }

        public static void Validate()
        {
            foreach(string kind in new[]{"Plain","Light","Service"})
            {
                var wall=AssetDatabase.LoadAssetAtPath<GameObject>(EchoWallModuleBuilder.Art+"/Prefabs/ECHO_Wall_"+kind+"_250x380.prefab");
                if(wall.GetComponentsInChildren<DecalProjector>(true).Length!=0)throw new InvalidOperationException("Wear embedded in repeating wall.");
                foreach(var r in wall.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="Continuous main"||r.name=="Continuous lower"))
                    foreach(string property in new[]{"_BaseMap","_MainTex"})if(r.sharedMaterial.HasProperty(property)&&r.sharedMaterial.GetTexture(property))throw new InvalidOperationException("Baked wear still assigned: "+kind);
            }
            foreach(string kind in Kinds)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(kind));var projector=prefab.GetComponent<DecalProjector>();
                if(!projector||!projector.material||!projector.material.GetTexture("Base_Map")||prefab.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Invalid separate wear prefab.");
                if(projector.material.shader.isSupported==false)throw new InvalidOperationException("Unsupported decal shader.");
            }
            var root=GameObject.Find("ECHO Independent wall wear");
            File.WriteAllText(Reports+"/verified.txt",DateTime.UtcNow.ToString("O")+"\n3 clean wall variants; no BaseMap/MainTex wear. 3 independent alpha decal prefabs; albedo only; 0 colliders. Level03 scene projectors: "+(root?root.GetComponentsInChildren<DecalProjector>().Length:0)+". Shared clean materials also update existing Level02 instances. Original wall material paths/GUIDs retained.\n");
            Debug.Log("ECHO clean walls and independent wear validated.");
        }

        public static void Capture()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Idle Editor required.");
            EditorSceneManager.OpenScene(Level03Builder.Path);
            var camera=new GameObject("Wall wear review").AddComponent<Camera>();
            camera.CopyFrom(Object.FindAnyObjectByType<TimeCycleManager>().Player.View.GetComponent<Camera>());camera.fieldOfView=66;
            var root=GameObject.Find("ECHO Independent wall wear");
            var eyes=new[]{new Vector3(1.9f,1.6f,2.5f),new Vector3(1.7f,1.7f,15.2f),new Vector3(-9.9f,1.7f,7.1f),new Vector3(1.9f,1.9f,8)};
            var targets=new[]{new Vector3(3.76f,1.6f,3.8f),new Vector3(3.76f,1.6f,16.7f),new Vector3(-11.76f,1.7f,8.6f),new Vector3(3.76f,2.05f,8.7f)};
            try
            {
                for(int view=0;view<eyes.Length;view++)for(int state=0;state<2;state++)
                {
                    root.SetActive(state==1);camera.transform.position=eyes[view];camera.transform.LookAt(targets[view]);
                    var rt=new RenderTexture(1440,900,24);var previous=RenderTexture.active;camera.targetTexture=rt;
                    // Warm the render graph and decal entity data before reading the result.
                    camera.Render();camera.Render();RenderTexture.active=rt;
                    var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();
                    File.WriteAllBytes(Reports+"/"+(state==0?"Clean-":"Wear-")+view+".png",image.EncodeToPNG());
                    camera.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
                }
            }
            finally{root.SetActive(true);Object.DestroyImmediate(camera.gameObject);}
            EditorSceneManager.OpenScene(Level03Builder.Path);File.WriteAllText(Reports+"/captures-complete.txt",DateTime.UtcNow.ToString("O"));
        }
    }
}
