using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace EchoGame.Editor
{
    public static class EchoFloorModuleBuilder
    {
        public const string Art="Assets/_ECHO/Art/Modular/Floor01";
        public const string Reports="C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-21-EchoFloor01";
        static Material Material(string name,Color color,float metallic,float smooth)
        {
            string path=Art+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
        }
        static void Part(Transform parent,string name,Vector3 p,Vector3 size,Material material,float bevel=.003f)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=p;
            go.AddComponent<MeshFilter>().sharedMesh=EchoWallModuleBuilder.BeveledBox(name,size,bevel,Art+"/Meshes");go.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        static Texture2D Surface()
        {
            const int n=512;var pixels=new Color[n*n];var random=new System.Random(729);
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float value=.93f+Mathf.PerlinNoise(x*.06f,y*.06f)*.04f+((float)random.NextDouble()-.5f)*.035f;
                pixels[y*n+x]=new Color(value,value,value,1);
            }
            for(int i=0;i<65;i++)
            {
                int x=random.Next(10,n-30),y=random.Next(10,n-30),length=random.Next(2,14);
                for(int k=0;k<length;k++){float c=i%3==0?1f:.82f;pixels[(y+k/3)*n+x+k]=new Color(c,c,c,1);}
            }
            var texture=new Texture2D(n,n,TextureFormat.RGBA32,false);texture.SetPixels(pixels);texture.Apply();string path=Art+"/Textures/Graphite wear.png";
            File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        [MenuItem("Tools/ECHO/Art/Try own floor in Level 02")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;EditorApplication.delayCall+=Apply;return;}
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene first.");
            Directory.CreateDirectory(Reports);File.Copy(Level02Builder.ScenePath,Reports+"/Level_02-before-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".unity");
            var scene=EditorSceneManager.OpenScene(Level02Builder.ScenePath);var before=Object.FindObjectsByType<Collider>().Select(c=>c.GetEntityId()).OrderBy(i=>i).ToArray();
            var old=GameObject.Find("ECHO Floor01 trial");if(old)Object.DestroyImmediate(old);
            foreach(string folder in new[]{"Materials","Meshes","Textures","Prefabs"})Directory.CreateDirectory(Art+"/"+folder);AssetDatabase.Refresh();
            var graphite=Material("Graphite floor",new Color(.23f,.27f,.29f),.25f,.29f);graphite.SetTexture("_BaseMap",Surface());
            var steel=Material("Service steel",new Color(.30f,.33f,.34f),.5f,.34f);
            var dark=Material("Recess",new Color(.035f,.045f,.05f),.15f,.2f);
            var marking=Material("Faded turquoise",new Color(.16f,.36f,.37f),.05f,.24f);
            for(int kind=0;kind<2;kind++)
            {
                var root=new GameObject(kind==0?"ECHO_Floor_Plain_250":"ECHO_Floor_Service_250");var t=root.transform;
                Part(t,"Floor slab",new Vector3(0,-.012f,0),new Vector3(2.488f,.04f,2.488f),graphite,.006f);
                if(kind==1)
                {
                    Part(t,"Service seam",new Vector3(0,.0085f,0),new Vector3(1.38f,.003f,1.38f),dark,.001f);
                    Part(t,"Flush access panel",new Vector3(0,.0105f,0),new Vector3(1.35f,.003f,1.35f),steel,.001f);
                    foreach(int side in new[]{-1,1})
                    {
                        Part(t,"Recessed grip "+side,new Vector3(side*.48f,.0125f,0),new Vector3(.025f,.001f,.20f),dark,.0002f);
                        Part(t,"Access mark "+side,new Vector3(side*.48f,.0125f,-.52f),new Vector3(.19f,.001f,.025f),marking,.0002f);
                    }
                    foreach(float x in new[]{-.59f,.59f})foreach(float z in new[]{-.59f,.59f})Part(t,"Fastener "+x+" "+z,new Vector3(x,.0125f,z),new Vector3(.022f,.001f,.022f),dark,.0002f);
                }
                PrefabUtility.SaveAsPrefabAsset(root,Art+"/Prefabs/"+root.name+".prefab");Object.DestroyImmediate(root);
            }
            var trial=new GameObject("ECHO Floor01 trial");trial.transform.SetParent(GameObject.Find("_Environment").transform,false);
            Part(trial.transform,"Joint backing",new Vector3(6.2f,-.018f,4.9f),new Vector3(5,.04f,7.5f),dark);
            for(int x=0;x<2;x++)for(int z=0;z<3;z++)
            {
                string kind=x==1&&z==2?"Service":"Plain";
                var tile=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Prefabs/ECHO_Floor_"+kind+"_250.prefab"));tile.transform.SetParent(trial.transform,false);tile.transform.position=new Vector3(4.95f+x*2.5f,0,2.4f+z*2.5f);tile.transform.rotation=Quaternion.Euler(0,(x+z)%4*90,0);
            }
            if(!before.SequenceEqual(Object.FindObjectsByType<Collider>().Select(c=>c.GetEntityId()).OrderBy(i=>i)))throw new InvalidOperationException("Floor trial changed gameplay colliders.");
            if(trial.GetComponentsInChildren<Collider>().Length!=0)throw new InvalidOperationException("Decorative floor has colliders.");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(Reports+"/verified.txt",DateTime.UtcNow.ToString("O")+"\nSix 2.5m floor tiles, 5 x 7.5m trial adjacent to accepted walls. Plain + flush service prefabs. Top height .008m/.013m.\nGameplay colliders preserved: "+before.Length+"; decorative colliders: 0. Walls and gameplay not rebuilt.");
            EditorSceneManager.OpenScene(Level02Builder.ScenePath);EditorApplication.QueuePlayerLoopUpdate();
        }
        public static void Capture()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            var go=new GameObject("Floor trial camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Object.FindAnyObjectByType<TimeCycleManager>().Player.View.GetComponent<Camera>());camera.fieldOfView=62;
            var positions=new[]{new Vector3(2.2f,1.7f,1.2f),new Vector3(6,1.65f,5.5f)};var targets=new[]{new Vector3(7.2f,.9f,5.5f),new Vector3(7.9f,.2f,7.1f)};
            for(int i=0;i<2;i++)
            {
                go.transform.position=positions[i];go.transform.LookAt(targets[i]);var rt=new RenderTexture(1600,1000,24);var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Reports+"/After-"+i+".png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
            }Object.DestroyImmediate(go);
        }
    }
}
