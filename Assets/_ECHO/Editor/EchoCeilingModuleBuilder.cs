using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace EchoGame.Editor
{
    public static class EchoCeilingModuleBuilder
    {
        public const string Art="Assets/_ECHO/Art/Modular/Ceiling01";
        public const string Reports="C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-21-EchoCeiling01";
        static Material paint,metal,dark,mark,glow;
        static Material Mat(string name,Color color,float metallic,float smooth)
        {
            string path=Art+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
        }
        static void Part(Transform t,string name,Vector3 p,Vector3 size,Material m,float bevel=.003f)
        {
            var go=new GameObject(name);go.transform.SetParent(t,false);go.transform.localPosition=p;
            go.AddComponent<MeshFilter>().sharedMesh=EchoWallModuleBuilder.BeveledBox(name,size,bevel,Art+"/Meshes");go.AddComponent<MeshRenderer>().sharedMaterial=m;
        }
        static void Downlight(Transform t,string name,Vector3 p,float intensity)
        {
            var go=new GameObject(name);go.transform.SetParent(t,false);go.transform.localPosition=p;go.transform.localRotation=Quaternion.LookRotation(Vector3.down);
            var light=go.AddComponent<Light>();light.type=LightType.Spot;light.range=7f;light.intensity=intensity;light.spotAngle=110;light.innerSpotAngle=65;
            light.color=new Color(1,.95f,.86f);light.shadows=LightShadows.Soft;light.shadowBias=.025f;light.shadowNormalBias=.08f;
            var data=new SerializedObject(light.GetUniversalAdditionalLightData());data.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium;data.ApplyModifiedPropertiesWithoutUndo();
        }
        static void PerforatedInsert(Transform t)
        {
            string path=Art+"/Meshes/Perforated insert.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh)
            {
                var vertices=new List<Vector3>();var indices=new List<int>();var uv=new List<Vector2>();const float cell=.05f,radius=.008f;
                for(int x=0;x<18;x++)for(int z=0;z<30;z++)
                {
                    var outer=new[]{new Vector2(1,0),new Vector2(1,1),new Vector2(0,1),new Vector2(-1,1),new Vector2(-1,0),new Vector2(-1,-1),new Vector2(0,-1),new Vector2(1,-1)};
                    for(int k=0;k<8;k++)
                    {
                        int n=vertices.Count;int next=(k+1)%8;float a=k*Mathf.PI/4,b=(k+1)*Mathf.PI/4;
                        foreach(var p in new[]{outer[k]*cell*.5f,outer[next]*cell*.5f,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius})
                        {vertices.Add(new Vector3((x-8.5f)*cell+p.x,0,(z-14.5f)*cell+p.y));uv.Add(p);}
                        indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
                    }
                }
                mesh=new Mesh{name="Perforated insert"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            var go=new GameObject("Perforated acoustic insert");go.transform.SetParent(t,false);go.transform.localPosition=new Vector3(0,-.068f,0);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=metal;
        }
        static void BuildPrefab(string kind)
        {
            var root=new GameObject("ECHO_Ceiling_"+kind+"_250");var t=root.transform;
            Part(t,"Composite ceiling",new Vector3(0,-.026f,0),new Vector3(2.488f,.04f,2.488f),paint,.005f);
            foreach(float x in new[]{-1.16f,1.16f})foreach(float z in new[]{-1.16f,1.16f})Part(t,"Fastener "+x+" "+z,new Vector3(x,-.047f,z),new Vector3(.018f,.002f,.018f),metal,.0004f);
            if(kind!="01_Plain")Part(t,"Sector mark",new Vector3(.88f,-.047f,.90f),new Vector3(.24f,.002f,.025f),mark,.0004f);
            if(kind=="02_Vent")
            {
                Part(t,"Vent recess",new Vector3(0,-.049f,-.91f),new Vector3(1.95f,.006f,.24f),dark);
                for(int i=0;i<6;i++)Part(t,"Vent louver "+i,new Vector3(0,-.055f,-1.01f+i*.04f),new Vector3(1.86f,.008f,.014f),metal,.002f);
                Part(t,"Small service cover",new Vector3(.75f,-.050f,.62f),new Vector3(.40f,.008f,.40f),metal);
            }
            if(kind=="05_Split")Part(t,"Offset recessed joint",new Vector3(-.35f,-.047f,0),new Vector3(.009f,.001f,2.47f),dark,.0002f);
            if(kind=="07_Acoustic")
            {
                Part(t,"Acoustic recess",new Vector3(0,-.052f,0),new Vector3(.94f,.008f,1.54f),dark);PerforatedInsert(t);
            }
            if(kind=="08_TwinEdge")foreach(int side in new[]{-1,1})
            {
                Part(t,"Edge bezel "+side,new Vector3(side*.92f,-.055f,0),new Vector3(.14f,.025f,1.88f),metal);
                Part(t,"Edge diffuser "+side,new Vector3(side*.92f,-.069f,0),new Vector3(.075f,.004f,1.78f),glow,.001f);
                Downlight(t,"Edge downlight "+side,new Vector3(side*.92f,-.10f,0),12f);
            }
            if(kind=="09_Corner")
            {
                Part(t,"Corner bezel X",new Vector3(-.34f,-.055f,-.95f),new Vector3(1.34f,.025f,.14f),metal);
                Part(t,"Corner bezel Z",new Vector3(-.95f,-.055f,-.34f),new Vector3(.14f,.025f,1.34f),metal);
                Part(t,"Corner diffuser X",new Vector3(-.34f,-.069f,-.95f),new Vector3(1.23f,.004f,.075f),glow,.001f);
                Part(t,"Corner diffuser Z",new Vector3(-.95f,-.069f,-.34f),new Vector3(.075f,.004f,1.23f),glow,.001f);
                Downlight(t,"Corner downlight",new Vector3(-.65f,-.10f,-.65f),16f);
            }
            PrefabUtility.SaveAsPrefabAsset(root,Art+"/Prefabs/"+root.name+".prefab");Object.DestroyImmediate(root);
        }
        [MenuItem("Tools/ECHO/Art/Try selected ceiling modules in Level 02")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;EditorApplication.delayCall+=Apply;return;}
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene first.");
            Directory.CreateDirectory(Reports);File.Copy(Level02Builder.ScenePath,Reports+"/Level_02-before-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".unity");
            var scene=EditorSceneManager.OpenScene(Level02Builder.ScenePath);var before=Object.FindObjectsByType<Collider>().Select(c=>c.GetEntityId()).OrderBy(i=>i).ToArray();
            var old=GameObject.Find("ECHO Ceiling01 trial");if(old)Object.DestroyImmediate(old);
            foreach(string folder in new[]{"Materials","Meshes","Prefabs"})Directory.CreateDirectory(Art+"/"+folder);AssetDatabase.Refresh();
            paint=Mat("Warm composite",new Color(.48f,.49f,.46f),.05f,.22f);metal=Mat("Graphite metal",new Color(.17f,.20f,.21f),.4f,.28f);dark=Mat("Recess",new Color(.035f,.045f,.05f),.05f,.15f);mark=Mat("Turquoise marker",new Color(.16f,.36f,.37f),.05f,.24f);
            glow=Mat("Neutral diffuser",new Color(.95f,.94f,.88f),0,.2f);glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1,.96f,.85f)*2.5f);glow.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
            string[] kinds={"01_Plain","02_Vent","05_Split","07_Acoustic","08_TwinEdge","09_Corner"};foreach(var kind in kinds)BuildPrefab(kind);
            var trial=new GameObject("ECHO Ceiling01 trial");trial.transform.SetParent(GameObject.Find("_Environment").transform,false);
            Part(trial.transform,"Joint backing",new Vector3(6.2f,3.989f,4.9f),new Vector3(5,.01f,7.5f),dark);
            for(int i=0;i<6;i++)
            {
                var tile=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Prefabs/ECHO_Ceiling_"+kinds[i]+"_250.prefab"));tile.transform.SetParent(trial.transform,false);tile.transform.position=new Vector3(i<3?4.95f:7.45f,4,2.4f+i%3*2.5f);
            }
            if(!before.SequenceEqual(Object.FindObjectsByType<Collider>().Select(c=>c.GetEntityId()).OrderBy(i=>i)))throw new InvalidOperationException("Ceiling changed colliders.");
            if(trial.GetComponentsInChildren<Collider>().Length!=0)throw new InvalidOperationException("Ceiling decoration has colliders.");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(Reports+"/verified.txt",DateTime.UtcNow.ToString("O")+"\nSix selected original 2.5m modules: 01/02/05/07 unlit, 08/09 lit. Trial 5x7.5m above floor, anchor Y4m.\nGameplay colliders preserved: "+before.Length+"; decorative colliders: 0. Existing walls, floor and lighting unchanged. Not added to accepted package yet.");
            EditorSceneManager.OpenScene(Level02Builder.ScenePath);EditorApplication.QueuePlayerLoopUpdate();
        }
        public static void Capture()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            var go=new GameObject("Ceiling trial camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Object.FindAnyObjectByType<TimeCycleManager>().Player.View.GetComponent<Camera>());camera.fieldOfView=75;
            var positions=new[]{new Vector3(2.7f,1.65f,1),new Vector3(6.1f,1.65f,7.9f),new Vector3(6.1f,1.65f,2.2f),new Vector3(3,1.65f,2.8f)};var targets=new[]{new Vector3(6.4f,3.6f,5),new Vector3(6.5f,3.95f,5.4f),new Vector3(6.5f,3.95f,4.9f),new Vector3(7,.9f,6)};
            for(int i=0;i<positions.Length;i++)
            {
                go.transform.position=positions[i];go.transform.LookAt(targets[i]);var rt=new RenderTexture(1600,1000,24);var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Reports+"/After-"+i+".png",image.EncodeToPNG());if(i==3){var lights=GameObject.Find("ECHO Ceiling01 trial").GetComponentsInChildren<Light>();foreach(var light in lights)light.enabled=false;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Reports+"/LightingOff.png",image.EncodeToPNG());foreach(var light in lights)light.enabled=true;}camera.targetTexture=null;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
            }Object.DestroyImmediate(go);
        }
    }
}
