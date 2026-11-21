using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EchoGame.Editor
{
    public static class EchoWallSetExporter
    {
        public const string Destination="C:/Users/user/Desktop/4-ый курс/Диплом/Художка/ECHO_Walls_01";
        [Serializable] public class Catalog
        {
            public string name="ECHO Walls 01", status="Accepted by author on 2026-11-21";
            public string unity="6000.6.3f1", pipeline="Universal Render Pipeline";
            public float width=2.5f,height=3.8f,depth; public string pivot="Bottom centre",front="-Z";
            public string usage="Decorative wall skins; use structural colliders behind them. Service hatch is decorative.";
            public string[] prefabs,dependencies; public string package;
            public ModuleInfo[] modules;
        }
        [Serializable] public class ModuleInfo
        {
            public string prefab,pivot,front;public float width,height,depth;
        }
        [MenuItem("Tools/ECHO/Art/Export unified ECHO asset set")]
        public static void ExportAll()
        {
            var walls=new[]{"Plain","Light","Service"}.Select(k=>EchoWallModuleBuilder.Art+"/Prefabs/ECHO_Wall_"+k+"_250x380.prefab");
            var floors=new[]{"Plain","Service"}.Select(k=>EchoFloorModuleBuilder.Art+"/Prefabs/ECHO_Floor_"+k+"_250.prefab");
            var ceilings=new[]{"01_Plain","02_Vent","05_Split","07_Acoustic","08_TwinEdge","09_Corner"}.Select(k=>EchoCeilingModuleBuilder.Art+"/Prefabs/ECHO_Ceiling_"+k+"_250.prefab");
            var prefabs=walls.Concat(floors).Concat(ceilings).ToArray();
            ExportSet("Assets/_ECHO/Art/Modular","C:/Users/user/Desktop/4-ый курс/Диплом/Художка","ECHO_Assets",new Catalog{
                name="ECHO Asset Set",width=0,height=0,depth=0,pivot="See modules",front="See modules",prefabs=prefabs,
                usage="Unified accepted ECHO modules. Decorative skins require structural colliders; service hatches are decorative. Requires URP.",
                modules=prefabs.Select(p=>new ModuleInfo{prefab=p,width=2.5f,height=p.Contains("ECHO_Wall_")?3.8f:p.Contains("ECHO_Ceiling_")?.071f:.045f,depth=p.Contains("ECHO_Wall_")?.41f:2.5f,pivot=p.Contains("ECHO_Wall_")?"Bottom centre":p.Contains("ECHO_Ceiling_")?"Centre at structural ceiling plane; underside down to -0.071m; lights face down":"Centre at structural floor level; bottom -0.032m, top up to +0.013m",front=p.Contains("ECHO_Wall_")?"-Z":p.Contains("ECHO_Ceiling_")?"-Y":"+Y"}).ToArray()
            },true);
        }
        [MenuItem("Tools/ECHO/Art/Archive/Export ECHO wall set 01")]
        public static void Export()
        {
            string art=EchoWallModuleBuilder.Art;
            var prefabs=new[]{"Plain","Light","Service"}.Select(k=>art+"/Prefabs/ECHO_Wall_"+k+"_250x380.prefab").ToArray();
            ExportSet(art,Destination,"ECHO_Walls_01",new Catalog{prefabs=prefabs});
        }
        [MenuItem("Tools/ECHO/Art/Archive/Export ECHO floor set 01")]
        public static void ExportFloors()
        {
            string art=EchoFloorModuleBuilder.Art;
            ExportSet(art,"C:/Users/user/Desktop/4-ый курс/Диплом/Художка/ECHO_Floors_01","ECHO_Floors_01",new Catalog{
                name="ECHO Floors 01",width=2.5f,depth=2.5f,height=.013f,pivot="Centre at structural floor level",front="Top +Y",
                usage="Decorative floor skins; structural floor collider required. Slab extends down to -0.032m; visible top +0.008m, service details up to +0.013m. Service hatch is decorative.",
                prefabs=new[]{"Plain","Service"}.Select(k=>art+"/Prefabs/ECHO_Floor_"+k+"_250.prefab").ToArray()});
        }
        static void ExportSet(string art,string destination,string filename,Catalog catalog,bool unified=false)
        {
            var prefabs=catalog.prefabs;
            foreach(string path in prefabs)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(!prefab||prefab.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Invalid decorative prefab: "+path);
                if(prefab.GetComponentsInChildren<MeshFilter>(true).Any(m=>!m.sharedMesh)||prefab.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>!m)))throw new InvalidOperationException("Missing mesh/material: "+path);
            }
            var dependencies=AssetDatabase.GetDependencies(prefabs,true).Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)).OrderBy(p=>p).ToArray();
            if(dependencies.Any(p=>!p.StartsWith(art+"/",StringComparison.Ordinal)))throw new InvalidOperationException("Wall set depends on other project art.");
            Directory.CreateDirectory(destination);AssetDatabase.SaveAssets();
            string exportFolder=unified?destination+"/Archive_AssetExports":destination;Directory.CreateDirectory(exportFolder);
            string package=exportFolder+"/"+filename+"_"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".unitypackage";
            AssetDatabase.ExportPackage(dependencies,package,ExportPackageOptions.Default);
            if(unified){string current=destination+"/ECHO_Assets.unitypackage";File.Copy(package,current,true);package=current;}
            catalog.dependencies=dependencies;catalog.package=package;
            File.WriteAllText(destination+(unified?"/ECHO_Assets.catalog.json":"/catalog.json"),JsonUtility.ToJson(catalog,true));
            Debug.Log("ECHO: accepted modular set exported to "+package);
        }
    }
}
