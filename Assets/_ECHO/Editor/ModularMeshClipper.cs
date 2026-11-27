using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace EchoGame.Editor
{
    // Cuts decorative geometry at room edges, without scaling a module or adding colliders.
    internal static class ModularMeshClipper
    {
        struct Vertex
        {
            public Vector3 position,normal;public Vector2 uv;
            public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex{position=Vector3.Lerp(a.position,b.position,t),normal=Vector3.Lerp(a.normal,b.normal,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};
        }
        static List<Vertex> Cut(List<Vertex> input,int axis,float edge,bool greater)
        {
            var output=new List<Vertex>();if(input.Count==0)return output;var a=input[input.Count-1];float da=(a.position[axis]-edge)*(greater?1:-1);
            foreach(var b in input)
            {
                float db=(b.position[axis]-edge)*(greater?1:-1);
                if((da>=0)!=(db>=0))output.Add(Vertex.Lerp(a,b,da/(da-db)));
                if(db>=0)output.Add(b);a=b;da=db;
            }
            return output;
        }
        public static void Trim(GameObject module,int axis,float minX,float maxX,float minOther,float maxOther,string folder)
        {
            foreach(var filter in module.GetComponentsInChildren<MeshFilter>(true))
            {
                var source=filter.sharedMesh;var matrix=module.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                var bounds=source.bounds;var lo=Vector3.one*float.PositiveInfinity;var hi=Vector3.one*float.NegativeInfinity;
                for(int c=0;c<8;c++)
                {
                    var p=matrix.MultiplyPoint3x4(bounds.center+Vector3.Scale(bounds.extents,new Vector3((c&1)==0?-1:1,(c&2)==0?-1:1,(c&4)==0?-1:1)));
                    lo=Vector3.Min(lo,p);hi=Vector3.Max(hi,p);
                }
                if(lo.x>=minX-.00001f&&hi.x<=maxX+.00001f&&lo[axis]>=minOther-.00001f&&hi[axis]<=maxOther+.00001f)continue;
                var renderer=filter.GetComponent<Renderer>();
                if(hi.x<=minX||lo.x>=maxX||hi[axis]<=minOther||lo[axis]>=maxOther){renderer.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);continue;}
                string key=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+"/"+axis;
                foreach(float value in new[]{minX,maxX,minOther,maxOther})key+="/"+value.ToString("R",CultureInfo.InvariantCulture);
                for(int k=0;k<16;k++)key+="/"+matrix[k].ToString("R",CultureInfo.InvariantCulture);
                string path=folder+"/Cut-"+Hash128.Compute(key)+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh)
                {
                    var vertices=source.vertices;var normals=source.normals;var uv=source.uv;var positions=new List<Vector3>();var outputNormals=new List<Vector3>();var outputUv=new List<Vector2>();var triangles=new List<int>();var indices=source.triangles;var inverse=matrix.inverse;
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        var polygon=new List<Vertex>();
                        for(int j=0;j<3;j++){int index=indices[i+j];polygon.Add(new Vertex{position=matrix.MultiplyPoint3x4(vertices[index]),normal=normals.Length>index?normals[index]:Vector3.up,uv=uv.Length>index?uv[index]:Vector2.zero});}
                        polygon=Cut(polygon,0,minX,true);polygon=Cut(polygon,0,maxX,false);polygon=Cut(polygon,axis,minOther,true);polygon=Cut(polygon,axis,maxOther,false);
                        if(polygon.Count<3)continue;int first=positions.Count;
                        foreach(var v in polygon){positions.Add(inverse.MultiplyPoint3x4(v.position));outputNormals.Add(v.normal);outputUv.Add(v.uv);}
                        for(int j=1;j<polygon.Count-1;j++)triangles.AddRange(new[]{first,first+j,first+j+1});
                    }
                    mesh=new Mesh{name=source.name+" trimmed"};if(positions.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
                    mesh.SetVertices(positions);mesh.SetNormals(outputNormals);mesh.SetUVs(0,outputUv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
                }
                filter.sharedMesh=mesh;
                PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            }
            foreach(var light in module.GetComponentsInChildren<Light>())
            {
                var p=module.transform.InverseTransformPoint(light.transform.position);
                if(p.x<minX||p.x>maxX||p[axis]<minOther||p[axis]>maxOther){light.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(light);}
            }
        }
    }
}
