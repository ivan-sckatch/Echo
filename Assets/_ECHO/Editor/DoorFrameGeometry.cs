using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoGame.Editor
{
    // Physical apertures remain unchanged. Visible frame lips occupy separate depth planes.
    public static class DoorFrameGeometry
    {
        public const float FrameLip=.02f, StructuralLip=.01f, Depth=.90f;
        public static Vector3 GuidePosition(int side)=>new Vector3(side*1.56f,1.69f,0);
        public static Vector3 GuideSize=>new Vector3(.16f,3.38f,Depth);
        public static Vector3 HeaderPosition=>new Vector3(0,3.51f,0);
        public static Vector3 HeaderSize=>new Vector3(3.28f,.26f,Depth);
        const string Meshes="Assets/_ECHO/Art/Modular/Door01/Meshes";
        const string StructuralMeshes="Assets/_ECHO/Art/DoorFrames/Meshes";
        static void Record(UnityEngine.Object value){if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);EditorUtility.SetDirty(value);}
        public static void FitModular(Transform root)
        {
            Directory.CreateDirectory(Meshes);
            foreach(int side in new[]{-1,1})
            {
                var guide=root.Find("Frame guide "+side);guide.localPosition=GuidePosition(side);
                var mesh=guide.GetComponent<MeshFilter>();mesh.sharedMesh=EchoWallModuleBuilder.BeveledBox("Portal guide separated v2",GuideSize,.006f,Meshes);Record(guide);Record(mesh);
            }
            var header=root.Find("Header");header.localPosition=HeaderPosition;
            var filter=header.GetComponent<MeshFilter>();filter.sharedMesh=EchoWallModuleBuilder.BeveledBox("Portal header separated v2",HeaderSize,.006f,Meshes);Record(header);Record(filter);
            foreach(string name in new[]{"Header recess front","Header recess rear","Status front","Status rear"})
            {
                var part=root.Find(name);var p=part.localPosition;p.z=(name.EndsWith("front",StringComparison.Ordinal)?-1:1)*(Depth/2+(name.StartsWith("Status",StringComparison.Ordinal)?.024f:.009f));part.localPosition=p;Record(part);
            }
        }
        public static void FitLegacy(Transform root)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                if(t.name=="Jamb"){t.localPosition=new Vector3(Mathf.Sign(t.localPosition.x)*1.665f,1.725f,0);t.localScale=new Vector3(.37f,3.45f,.8f);Record(t);}
                if(t.name=="Guide rail"){t.localPosition=new Vector3(Mathf.Sign(t.localPosition.x)*1.665f,1.725f,-.43f);t.localScale=new Vector3(.065f,3.30f,.08f);Record(t);}
            }
        }
        public static int FitStructuralReveals(Scene scene)
        {
            var roots=scene.GetRootGameObjects();var filters=roots.SelectMany(r=>r.GetComponentsInChildren<MeshFilter>(true)).Where(m=>m.gameObject.activeInHierarchy&&m.GetComponent<Renderer>()&&m.GetComponent<Renderer>().enabled&&m.GetComponent<BoxCollider>()).ToArray();
            int changed=0;foreach(var door in roots.SelectMany(r=>r.GetComponentsInChildren<DoorController>(true)))changed+=FitStructuralReveals(door,filters);return changed;
        }
        public static int FitStructuralReveals(DoorController door,MeshFilter[] filters)
        {
            float halfWidth=1.5f,height=3.4f,halfDepth=.19f;var collider=door.Panel.GetComponent<BoxCollider>();
            if(collider)
            {
                var matrix=door.transform.worldToLocalMatrix*collider.transform.localToWorldMatrix;var ext=collider.size/2;
                var lo=Vector3.one*float.PositiveInfinity;var hi=Vector3.one*float.NegativeInfinity;
                for(int c=0;c<8;c++){var p=matrix.MultiplyPoint3x4(collider.center+Vector3.Scale(ext,new Vector3((c&1)==0?-1:1,(c&2)==0?-1:1,(c&4)==0?-1:1)));lo=Vector3.Min(lo,p);hi=Vector3.Max(hi,p);}
                halfWidth=(hi.x-lo.x)/2;height=hi.y-lo.y;halfDepth=(hi.z-lo.z)/2;
            }
            int changed=0;
            foreach(var filter in filters)
            {
                if(!filter||filter.transform.IsChildOf(door.transform)||!filter.sharedMesh||!filter.GetComponent<BoxCollider>())continue;
                var bounds=filter.GetComponent<BoxCollider>().bounds;
                // Broad phase in door space, including tall lift pockets, never unrelated rooms.
                var centre=door.transform.InverseTransformPoint(bounds.center);
                if(Mathf.Abs(centre.z)>bounds.extents.magnitude+.6f||Mathf.Abs(centre.x)>bounds.extents.magnitude+halfWidth+.1f||centre.y+bounds.extents.y<0||centre.y-bounds.extents.y>height+3.8f)continue;
                var source=filter.sharedMesh;var vertices=source.vertices;var matrix=door.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;var inverse=matrix.inverse;bool edited=false;
                for(int i=0;i<vertices.Length;i++)
                {
                    var p=matrix.MultiplyPoint3x4(vertices[i]);var q=p;
                    if(Mathf.Abs(Mathf.Abs(p.x)-halfWidth)<.0001f&&centre.y<height&&Mathf.Abs(centre.z)<.7f)q.x-=Mathf.Sign(p.x)*StructuralLip;
                    if(Mathf.Abs(p.y-height)<.0001f&&Mathf.Abs(centre.z)<.7f)q.y-=StructuralLip;
                    if(Mathf.Abs(Mathf.Abs(p.z)-halfDepth)<.0001f&&Mathf.Abs(centre.z)<.7f)q.z+=Mathf.Sign(p.z)*StructuralLip;
                    if(q!=p){vertices[i]=inverse.MultiplyPoint3x4(q);edited=true;}
                }
                if(!edited)continue;
                if(!Directory.Exists(StructuralMeshes)){Directory.CreateDirectory(StructuralMeshes);AssetDatabase.Refresh();}
                string key=source.name+"/"+string.Join("/",vertices.Select(v=>v.x.ToString("R",CultureInfo.InvariantCulture)+","+v.y.ToString("R",CultureInfo.InvariantCulture)+","+v.z.ToString("R",CultureInfo.InvariantCulture)));
                string path=StructuralMeshes+"/Reveal-"+Hash128.Compute(key)+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh){mesh=UnityEngine.Object.Instantiate(source);mesh.name="Separated structural reveal";mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);}
                filter.sharedMesh=mesh;Record(filter);changed++;
            }
            return changed;
        }
    }
}
