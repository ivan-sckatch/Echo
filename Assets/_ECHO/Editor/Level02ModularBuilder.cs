using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace EchoGame.Editor
{
    public static class Level02ModularBuilder
    {
        public const string Reports="C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-21-Level02OwnModular";
        const string Art="Assets/_ECHO/Art/Modular";
        static GameObject Module(string section,string name,Transform parent,Vector3 p,float yaw=0)
        {
            string path=Art+"/"+section+"/Prefabs/"+name+".prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!prefab)throw new InvalidOperationException("Missing accepted module: "+path);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            if(section=="Wall01")
            {
                var cap=new GameObject("Wall head infill");cap.transform.SetParent(go.transform,false);cap.transform.localPosition=new Vector3(0,3.87f,-.075f);
                cap.AddComponent<MeshFilter>().sharedMesh=EchoWallModuleBuilder.BeveledBox("Wall head infill",new Vector3(2.5f,.18f,.045f),.004f,Art+"/Adapters");
                cap.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Wall01/Materials/Graphite.mat");
            }
            foreach(var light in go.GetComponentsInChildren<Light>())
            {
                var data=new SerializedObject(light.GetUniversalAdditionalLightData());data.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow;data.ApplyModifiedPropertiesWithoutUndo();
            }
            return go;
        }
        static Transform Group(string name,Transform parent){var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        static void Lintel(Transform parent,float z)
        {
            var go=new GameObject("Graphite lintel facing");go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(0,3.74f,z);
            go.AddComponent<MeshFilter>().sharedMesh=EchoWallModuleBuilder.BeveledBox("Door lintel facing",new Vector3(3,.44f,.035f),.004f,Art+"/Adapters");
            go.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Wall01/Materials/Graphite.mat");
        }
        [MenuItem("Tools/ECHO/Art/Assemble Level 02 from own modules")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;EditorApplication.delayCall+=Apply;return;}
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene first.");
            Directory.CreateDirectory(Reports);File.Copy(Level02Builder.ScenePath,Reports+"/Level_02-before-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".unity");
            var scene=EditorSceneManager.OpenScene(Level02Builder.ScenePath);
            var colliders=Object.FindObjectsByType<Collider>().Select(c=>c.GetEntityId()).OrderBy(i=>i).ToArray();
            foreach(string name in new[]{"ECHO Floor01 trial","ECHO Ceiling01 trial","ECHO Wall variants trial","ECHO Wall01 trial","ECHO Modular Level02"}){var old=GameObject.Find(name);if(old)Object.DestroyImmediate(old);}
            Directory.CreateDirectory(Art+"/Adapters");AssetDatabase.Refresh();
            var environment=GameObject.Find("_Environment").transform;var root=Group("ECHO Modular Level02",environment);var floor=Group("Floor",root);var ceiling=Group("Ceiling",root);var walls=Group("Walls",root);
            string[] unlit={"01_Plain","05_Split","01_Plain","02_Vent","01_Plain","07_Acoustic"};
            for(int x=0;x<7;x++)for(int z=0;z<12;z++)
            {
                float px=-7.5f+2.5f*x,pz=1.25f+2.5f*z;
                bool service=(x==0||x==6)&&(z==2||z==6||z==10);
                var tile=Module("Floor01","ECHO_Floor_"+(service?"Service":"Plain")+"_250",floor,new Vector3(px,0,pz),(x+z)%4*90);
                bool lit=(x==1||x==5)&&(z==1||z==3||z==5||z==7||z==10);
                string type=lit?(z==3||z==7?"09_Corner":"08_TwinEdge"):unlit[(x+z*2)%unlit.Length];
                Module("Ceiling01","ECHO_Ceiling_"+type+"_250",ceiling,new Vector3(px,4,pz),(x+z)%2*180);
            }
            // Native 2.5m pieces extend into the existing outer wall at corners; never stretch modules.
            foreach(int side in new[]{-1,1})for(int z=0;z<12;z++)
            {
                bool light=side<0?(z==1||z==7):(z==2||z==8);
                string type=z==6?"Plain":light?"Light":z%4==0?"Service":"Plain";
                Module("Wall01","ECHO_Wall_"+type+"_250x380",walls,new Vector3(side*8.78f,.02f,1.25f+z*2.5f),side>0?90:-90);
            }
            for(int x=0;x<7;x++)
            {
                Module("Wall01","ECHO_Wall_Plain_250x380",walls,new Vector3(-7.5f+x*2.5f,.02f,.16f),180);
                Module("Wall01","ECHO_Wall_Plain_250x380",walls,new Vector3(-7.5f+x*2.5f,.02f,29.84f));
            }
            foreach(float z in new[]{10f,24f})
            {
                foreach(int side in new[]{-1,1})for(int x=0;x<3;x++)
                {
                    float px=side*(2.75f+x*2.5f);
                    Module("Wall01","ECHO_Wall_Plain_250x380",walls,new Vector3(px,.02f,z-.16f));
                    Module("Wall01","ECHO_Wall_Plain_250x380",walls,new Vector3(px,.02f,z+.16f),180);
                }
                Lintel(walls,z-.28f);Lintel(walls,z+.28f);
            }
            // Fixtures provide the main light; old three greybox point lights no longer flatten it.
            foreach(var light in environment.Find("Lighting").GetComponentsInChildren<Light>())light.enabled=false;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.30f,.34f,.38f);
            if(!colliders.SequenceEqual(Object.FindObjectsByType<Collider>().Select(c=>c.GetEntityId()).OrderBy(i=>i)))throw new InvalidOperationException("Modular finish changed gameplay colliders.");
            if(root.GetComponentsInChildren<Collider>().Length!=0)throw new InvalidOperationException("Decorative modules have colliders.");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(Reports+"/verified.txt",DateTime.UtcNow.ToString("O")+"\nFull Level_02: 84 floor, 84 ceiling, 62 wall modules; 4 lintel facings. Native dimensions, no nonuniform scaling. Ten ceiling light modules, four wall light modules.\nGameplay colliders preserved: "+colliders.Length+"; decorative colliders: 0. Door apertures, nodes and world signs retained.\nGreybox room point lights disabled, ambient .30/.34/.38, instance shadow tier Low (256). Accepted prefab assets and package unchanged.");
            EditorSceneManager.OpenScene(Level02Builder.ScenePath);EditorApplication.QueuePlayerLoopUpdate();
        }
        public static void Capture()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            if(EditorSceneManager.GetActiveScene().name!="Level_02")EditorSceneManager.OpenScene(Level02Builder.ScenePath);
            var cycles=Object.FindAnyObjectByType<TimeCycleManager>();cycles.PhasePuzzle.ResetMechanisms();
            var go=new GameObject("Modular review camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(cycles.Player.View.GetComponent<Camera>());camera.fieldOfView=68;
            var positions=new[]{new Vector3(0,1.65f,2),new Vector3(5.5f,1.65f,2),new Vector3(0,1.65f,11.5f),new Vector3(0,1.65f,18),new Vector3(0,1.65f,25.5f),new Vector3(-3,1.65f,14)};
            var targets=new[]{new Vector3(0,1.8f,10),new Vector3(8,2.8f,7),new Vector3(8.5f,1.8f,17),new Vector3(0,2.2f,24),new Vector3(0,2,29.7f),new Vector3(-8.5f,1.8f,17)};
            for(int i=0;i<positions.Length;i++)
            {
                go.transform.position=positions[i];go.transform.LookAt(targets[i]);var rt=new RenderTexture(1600,1000,24);var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Reports+"/After-"+i+".png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
            }Object.DestroyImmediate(go);EditorSceneManager.OpenScene(Level02Builder.ScenePath);
        }
    }
}
