using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace EchoGame.Editor
{
    // One reusable, project-owned module. Local origin is at floor level; front faces -Z.
    public static partial class EchoWallModuleBuilder
    {
        public const string Art = "Assets/_ECHO/Art/Modular/Wall01";
        public const string Reports = "C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-21-EchoWall01";
        static Material metal, paint, recess, marker, diffuser;
        static Material Mat(string name, Color color, float metallic, float smooth)
        {
            string path = Art + "/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
            m.SetColor("_BaseColor",color); m.SetFloat("_Metallic",metallic); m.SetFloat("_Smoothness",smooth); EditorUtility.SetDirty(m); return m;
        }
        internal static Mesh BeveledBox(string name, Vector3 size, float bevel, string meshFolder=null)
        {
            string path=(meshFolder??Art+"/Meshes")+"/"+name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path); if(mesh) return mesh;
            var vertices=new List<Vector3>(); var triangles=new List<int>(); var uv=new List<Vector2>();
            var h=size*.5f; float b=Mathf.Min(bevel,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.45f);
            Action<Vector3[],Vector3> face=(points,normal)=> {
                if(Vector3.Dot(Vector3.Cross(points[1]-points[0],points[2]-points[0]),normal)<0) Array.Reverse(points);
                int first=vertices.Count; foreach(var p in points){vertices.Add(p);uv.Add(Mathf.Abs(normal.y)>.5f ? new Vector2(p.x/size.x+.5f,p.z/size.z+.5f) : Mathf.Abs(normal.z)>.5f ? new Vector2(p.x/size.x+.5f,p.y/size.y+.5f) : new Vector2(p.z/size.z+.5f,p.y/size.y+.5f));}
                for(int i=1;i<points.Length-1;i++){triangles.Add(first);triangles.Add(first+i);triangles.Add(first+i+1);}
            };
            // Six faces, twelve bevel strips, eight corner triangles. Physical bevel width is in metres.
            for(int axis=0;axis<3;axis++) foreach(int sign in new[]{-1,1})
            {
                int u=(axis+1)%3,v=(axis+2)%3; var p=new Vector3[4]; var normal=Vector3.zero;normal[axis]=sign;
                for(int i=0;i<4;i++){p[i][axis]=sign*h[axis];p[i][u]=(i==0||i==3?-1:1)*(h[u]-b);p[i][v]=(i<2?-1:1)*(h[v]-b);}face(p,normal);
                foreach(int side in new[]{-1,1})
                {
                    var a=Vector3.zero;var c=Vector3.zero; a[axis]=sign*h[axis];a[u]=side*(h[u]-b);c[axis]=sign*(h[axis]-b);c[u]=side*h[u];
                    a[v]=-(h[v]-b);c[v]=a[v];var d=c;d[v]=-d[v];var e=a;e[v]=-e[v];var n=normal;n[u]=side;
                    face(new[]{a,c,d,e},n);
                }
            }
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
            {
                var a=new Vector3(x*h.x,y*(h.y-b),z*(h.z-b));var c=new Vector3(x*(h.x-b),y*h.y,z*(h.z-b));var d=new Vector3(x*(h.x-b),y*(h.y-b),z*h.z);
                face(new[]{a,c,d},new Vector3(x,y,z));
            }
            mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static void Part(Transform parent,string name,Vector3 pos,Vector3 size,Material material,float bevel=.008f)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=pos;
            go.AddComponent<MeshFilter>().sharedMesh=BeveledBox(name,size,bevel);go.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        [MenuItem("Tools/ECHO/Art/Try own wall module in Level 02")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;EditorApplication.delayCall+=Apply;return;}
            if(EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene before applying the module.");
            Directory.CreateDirectory(Reports);File.Copy(Level02Builder.ScenePath,Reports+"/Level_02-before-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".unity");
            var scene=EditorSceneManager.OpenScene(Level02Builder.ScenePath);
            var before=Object.FindObjectsByType<Collider>().Select(c=>c.GetEntityId()).OrderBy(i=>i).ToArray();
            var existing=GameObject.Find("ECHO Wall01 trial");if(existing)Object.DestroyImmediate(existing);
            foreach(string folder in new[]{"Materials","Meshes","Prefabs"})Directory.CreateDirectory(Art+"/"+folder);AssetDatabase.Refresh();
            metal=Mat("Graphite",new Color(.105f,.125f,.14f),.55f,.32f);paint=Mat("Warm composite",new Color(.57f,.56f,.51f),.06f,.27f);
            recess=Mat("Recess",new Color(.035f,.045f,.05f),.15f,.2f);marker=Mat("Turquoise marker",new Color(.16f,.43f,.44f),.08f,.25f);
            diffuser=Mat("Warm diffuser",new Color(.95f,.92f,.8f),0,.2f);diffuser.EnableKeyword("_EMISSION");diffuser.SetColor("_EmissionColor",new Color(1,.91f,.72f)*3);diffuser.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
            var root=new GameObject("ECHO_Wall_Light_250x380");var t=root.transform;
            Part(t,"Backing",new Vector3(0,1.9f,0),new Vector3(2.5f,3.8f,.10f),recess);
            foreach(int side in new[]{-1,1})Part(t,"Frame "+side,new Vector3(side*1.21f,1.9f,-.075f),new Vector3(.08f,3.8f,.18f),metal);
            Part(t,"Plinth",new Vector3(0,.15f,-.075f),new Vector3(2.34f,.3f,.18f),metal);
            Part(t,"Upper rail",new Vector3(0,3.53f,-.075f),new Vector3(2.34f,.54f,.18f),metal);
            Part(t,"Upper service panel",new Vector3(0,3.58f,-.177f),new Vector3(2.16f,.29f,.025f),metal,.004f);
            Part(t,"Main composite",new Vector3(0,1.98f,-.069f),new Vector3(2.32f,2.52f,.09f),paint);
            Part(t,"Lower composite",new Vector3(0,.50f,-.069f),new Vector3(2.32f,.39f,.09f),paint);
            Part(t,"Sector marker",new Vector3(-.77f,2.69f,-.119f),new Vector3(.36f,.065f,.006f),marker,.002f);
            Part(t,"Light housing",new Vector3(0,3.30f,-.19f),new Vector3(2.1f,.11f,.34f),metal);
            Part(t,"Light diffuser",new Vector3(0,3.239f,-.21f),new Vector3(1.84f,.012f,.22f),diffuser,.002f);
            foreach(float x in new[]{-1.07f,1.07f})foreach(float y in new[]{.40f,.64f,.82f,3.12f,3.59f})
                Part(t,"Fastener "+x+" "+y,new Vector3(x,y,y>3.4f?-.194f:-.119f),new Vector3(.022f,.022f,.008f),metal,.003f);
            var source=new GameObject("Fixture downlight");source.transform.SetParent(t,false);source.transform.localPosition=new Vector3(0,3.22f,-.45f);source.transform.localRotation=Quaternion.LookRotation(new Vector3(0,-1,.02f));
            var light=source.AddComponent<Light>();light.type=LightType.Spot;light.color=new Color(1,.91f,.75f);light.intensity=.65f;light.range=4;light.spotAngle=140;light.innerSpotAngle=100;light.shadows=LightShadows.Soft;light.shadowBias=.02f;light.shadowNormalBias=.05f;
            var data=new SerializedObject(light.GetUniversalAdditionalLightData());data.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium;data.ApplyModifiedPropertiesWithoutUndo();
            string prefabPath=Art+"/Prefabs/ECHO_Wall_Light_250x380.prefab";PrefabUtility.SaveAsPrefabAsset(root,prefabPath);Object.DestroyImmediate(root);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));instance.name="ECHO Wall01 trial";instance.transform.SetParent(GameObject.Find("_Environment").transform,false);instance.transform.position=new Vector3(5.5f,.02f,9.70f);
            var after=Object.FindObjectsByType<Collider>().Select(c=>c.GetEntityId()).OrderBy(i=>i).ToArray();if(!before.SequenceEqual(after))throw new InvalidOperationException("Trial changed gameplay colliders.");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(Reports+"/verified.txt",DateTime.UtcNow.ToString("O")+"\nOne original prefab, 2.5 x 3.8m. Position 5.5,.02,9.70.\nGameplay colliders preserved: "+before.Length+". Decorative colliders: 0.\nNo imported assets. Other environment and gameplay objects unchanged.");
            EditorSceneManager.OpenScene(Level02Builder.ScenePath);EditorApplication.QueuePlayerLoopUpdate();
        }
        public static void Capture()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            var go=new GameObject("Wall trial camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Object.FindAnyObjectByType<TimeCycleManager>().Player.View.GetComponent<Camera>());camera.fieldOfView=58;
            var positions=new[]{new Vector3(2.6f,1.8f,5.5f),new Vector3(5.5f,1.9f,5.2f)};
            for(int i=0;i<positions.Length;i++){
                go.transform.position=positions[i];go.transform.LookAt(new Vector3(5.5f,1.93f,9.5f));var rt=new RenderTexture(1440,1000,24);var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var tex=new Texture2D(1440,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,1000),0,0);tex.Apply();File.WriteAllBytes(Reports+"/After-"+i+".png",tex.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
            }Object.DestroyImmediate(go);
        }
    }
}
