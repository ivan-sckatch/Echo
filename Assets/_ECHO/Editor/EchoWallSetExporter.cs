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
            public string[] prefabs,overlays,dependencies; public string package;
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
            var doors=new[]{"01_Basic","03_Window","05_Diagonal"}.Select(k=>EchoDoorModuleBuilder.Art+"/Prefabs/ECHO_Door_"+k+"_300x340.prefab");
            var nodes=new[]{"03_Iris","04_Coil"}.Select(k=>EchoPhaseNodeBuilder.Art+"/Prefabs/ECHO_PhaseNode_"+k+".prefab");
            var prefabs=walls.Concat(floors).Concat(ceilings).Concat(doors).Concat(nodes).ToArray();
            ExportSet("Assets/_ECHO/Art/Modular","C:/Users/user/Desktop/4-ый курс/Диплом/Художка","ECHO_Assets",new Catalog{
                name="ECHO Asset Set",status="Accepted walls, floors, ceilings, doors and phase nodes; clean walls and separate wear kit updated 2026-11-26",width=0,height=0,depth=0,pivot="See modules",front="See modules",prefabs=prefabs,overlays=EchoWallWearBuilder.Kinds.Select(EchoWallWearBuilder.Prefab).ToArray(),
                usage="Unified accepted ECHO modules. Decorative skins require structural colliders; service hatches are decorative. Doors include leaf colliders and DoorController: call SetCommand/Tick from your game loop; no automatic proximity opener. Door leaf 3x3.4m; frame 3.28x3.64x0.90m with a 20mm visual reveal lip; physical leaf unchanged. Structural walls must conceal retracted leaves. Phase nodes include trigger, PhaseNode, PhaseNodePresentation and reveal shader. Assign a unique StableId per instance. Call ResetNode on initialization/reset; on each simulation step call BeginStep, Submit(sourceId) for each scanning source, then EndStep(dt), including steps with no sources. Source0=player, positive IDs=echoes. Presentations use this clock; no automatic scanner or puzzle is bundled. Node dimensions include scan volume and full motion envelope. Requires URP. Wall bases are clean; wear is a separate optional kit of 3 alpha decals (Scuffs/Scratches/Smudge). Add a Decal Renderer Feature to your URP renderer. Projector local +Z points into surface; set shallow depth around the visible wall face. Place independently; vary size, Z rotation and Fade Factor; do not nest wear inside repeating wall modules. Historical material filenames retain their GUIDs; Composite worn is now clean.",
                modules=prefabs.Select(Module).ToArray()
            },true);
        }
        static ModuleInfo Module(string path)
        {
            if(path.Contains("ECHO_PhaseNode_"))
            {
                var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                try
                {
                    var node=go.GetComponent<PhaseNode>();node.ResetNode();var bounds=new Bounds(Vector3.zero,new Vector3(.9f,.9f,.24f));
                    for(int pose=0;pose<2;pose++)
                    {
                        if(pose==1){node.BeginStep();node.Submit(0);node.EndStep(2.6f);}
                        foreach(var filter in go.GetComponentsInChildren<MeshFilter>(true))
                        {
                            var local=filter.sharedMesh.bounds;
                            for(int corner=0;corner<8;corner++)bounds.Encapsulate(filter.transform.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1))));
                        }
                    }
                    return new ModuleInfo{prefab=path,width=bounds.size.x,height=bounds.size.y,depth=bounds.size.z,pivot="Scan target centre; dimensions include trigger and moving parts",front="-Z"};
                }
                finally{UnityEngine.Object.DestroyImmediate(go);}
            }
            bool door=path.Contains("ECHO_Door_"),wall=path.Contains("ECHO_Wall_"),ceiling=path.Contains("ECHO_Ceiling_");
            return new ModuleInfo{prefab=path,width=door?3.28f:2.5f,height=door?3.64f:wall?3.8f:ceiling?.071f:.045f,
                depth=door?DoorFrameGeometry.Depth:wall?.41f:2.5f,pivot=door||wall?"Bottom centre":ceiling?"Centre at structural ceiling plane; underside down to -0.071m; lights face down":"Centre at structural floor level; bottom -0.032m, top up to +0.013m",front=door||wall?"-Z":ceiling?"-Y":"+Y"};
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
            var prefabs=catalog.prefabs.Concat(catalog.overlays??Array.Empty<string>()).ToArray();
            foreach(string path in prefabs)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(!prefab)throw new InvalidOperationException("Missing prefab: "+path);
                var door=prefab.GetComponent<DoorController>();var node=prefab.GetComponent<PhaseNode>();var colliders=prefab.GetComponentsInChildren<Collider>(true);
                if(door){
                    if(!unified||!path.StartsWith(EchoDoorModuleBuilder.Art+"/",StringComparison.Ordinal)||!door.Panel||!door.Indicator||!door.ClosedMaterial||!door.OpenMaterial||colliders.Length!=(door.SplitDiagonally?2:1))throw new InvalidOperationException("Invalid functional door: "+path);
                    if(door.SplitDiagonally&&(!door.UpperLeaf||!door.LowerLeaf))throw new InvalidOperationException("Missing diagonal leaves: "+path);
                }else if(node){
                    if(!unified||!path.StartsWith(EchoPhaseNodeBuilder.Art+"/",StringComparison.Ordinal)||!node.Presentation||!node.Visual||colliders.Length!=1||!colliders[0].isTrigger)throw new InvalidOperationException("Invalid functional phase node: "+path);
                    var view=node.Presentation;
                    if(view.Body==null||view.Body.Length==0||view.Core==null||view.Core.Length==0||view.Anomaly==null||view.Anomaly.Length==0||view.Body.Concat(view.Core).Concat(view.Anomaly).Any(r=>!r))throw new InvalidOperationException("Missing phase renderers: "+path);
                    if(view.Iris?(view.IrisPivots==null||view.IrisPivots.Length!=6||view.IrisPivots.Any(t=>!t)):(!view.LeftGuard||!view.RightGuard))throw new InvalidOperationException("Missing phase moving parts: "+path);
                }else if(colliders.Length!=0)throw new InvalidOperationException("Decorative prefab has colliders: "+path);
                if(prefab.GetComponentsInChildren<MeshFilter>(true).Any(m=>!m.sharedMesh)||prefab.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>!m)))throw new InvalidOperationException("Missing mesh/material: "+path);
            }
            var dependencies=AssetDatabase.GetDependencies(prefabs,true).Concat(unified?new[]{EchoWallWearBuilder.Art+"/Unity-Companion-License.txt"}:Array.Empty<string>()).Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)).OrderBy(p=>p).ToArray();
            var runtimeScripts=new[]{"Assets/_ECHO/Scripts/Interactions/DoorController.cs","Assets/_ECHO/Scripts/Scanner/PhaseNode.cs","Assets/_ECHO/Scripts/Scanner/PhaseNodePresentation.cs"};
            if(dependencies.Any(p=>!p.StartsWith(art+"/",StringComparison.Ordinal)&&!(unified&&runtimeScripts.Contains(p))))throw new InvalidOperationException("Unexpected dependency outside modular art and standalone interaction controllers.");
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
