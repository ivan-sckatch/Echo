using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static EchoGame.Editor.Level02Builder;
namespace EchoGame.Editor
{
    public static class Level05LeverUpgrade
    {
        public const string Reports="C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-12-Level05-Levers";
        public const string UpperReports="C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-12-Level05-Upper";
        static Material grey,yellow,green,cyan;
        static TMP_FontAsset font;
        [MenuItem("Tools/ECHO/Level 05/Apply lever sequence")]
        public static void Apply()=>ApplyTo(Reports);
        public static void ApplyUpper()=>ApplyTo(UpperReports);
        static void ApplyTo(string reportDirectory)
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.isDirty)throw new System.InvalidOperationException("Save the scene first.");
            if(scene.path!=Level05Builder.ScenePath)scene=EditorSceneManager.OpenScene(Level05Builder.ScenePath);
            Directory.CreateDirectory(reportDirectory);
            File.Copy(Level05Builder.ScenePath,reportDirectory+"/Before_levers_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity");
            Install(Object.FindAnyObjectByType<Level05Controller>());
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(reportDirectory+"/SceneUpgrade.txt","Middle: sequential circuit retained. Upper: A at checkpoint feeds both bridges and ramp input 1; B at far island feeds ramp input 2. Both windows 10 seconds. Live wiring and two input indicators. Exit node independent of power, source and Echo count. B faces the walking area.");
        }
        public static void Install(Level05Controller level)
        {
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_ECHO/UI/Fonts/ECHO UI SDF.asset");
            Material ExistingMat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_ECHO/Materials/Level05/"+name+".mat");
            grey=ExistingMat("Static");yellow=ExistingMat("Terminal");green=ExistingMat("Checkpoint");cyan=ExistingMat("Bridge");
            level.Lift.Label="Цепь подъёма · 1";level.Lift.StableId="L05_LiftStep1";level.Lift.PlayerOnly=false;
            level.Lift.transform.SetPositionAndRotation(new Vector3(-12.5f,9.65f,9.5f),Quaternion.Euler(0,-90,0));
            if(!level.LiftSecond)level.LiftSecond=Terminal(level,"Цепь подъёма · 2",new Vector3(-12.5f,9.65f,6.5f));
            level.LiftSecond.transform.rotation=Quaternion.Euler(0,-90,0);
            level.UpperA.Label="Питание моста";level.UpperA.Window=10;
            level.UpperB.Label="Питание рампы";level.UpperB.Window=10;
            level.UpperB.transform.SetPositionAndRotation(new Vector3(12.5f,17.65f,-10),Quaternion.Euler(0,90,0));
            if(!level.LiftFinal)level.LiftFinal=Terminal(level,"Цепь подъёма · 3",new Vector3(9.5f,9.65f,12.5f));
            level.Lift.SequenceOrder=1;level.LiftSecond.SequenceOrder=2;level.LiftFinal.SequenceOrder=3;
            level.MiddleWall.Window=level.MiddleBridge.Window=level.Lift.Window=level.LiftSecond.Window=level.LiftFinal.Window=10;
            level.Terminals=new[]{level.MiddleWall,level.MiddleBridge,level.Lift,level.LiftSecond,level.LiftFinal,level.UpperA,level.UpperB};
            foreach(var root in level.gameObject.scene.GetRootGameObjects())
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
                if(t && t.name=="Terminal support")Object.DestroyImmediate(t.gameObject);
            foreach(var terminal in level.Terminals)MakeLever(terminal);
            var previous=level.transform.Find("Signal wiring");if(previous)Object.DestroyImmediate(previous.gameObject);
            var wires=Node("Signal wiring",level.gameObject);
            Link(wires,level.MiddleWall,new[]{new Vector3(12,8.08f,12.5f),new Vector3(12,8.08f,13.5f),new Vector3(1,8.08f,13.5f),new Vector3(1,9.7f,12.9f)});
            Link(wires,level.MiddleBridge,new[]{new Vector3(7,8.08f,12.5f),new Vector3(7,8.08f,11),new Vector3(5.8f,8.08f,11)});
            Link(wires,level.Lift,new[]{new Vector3(-12.5f,8.08f,9.5f),new Vector3(-12.5f,8.08f,6.5f)});
            Link(wires,level.LiftSecond,new[]{new Vector3(-12.5f,8.1f,6.5f),new Vector3(-13.6f,8.1f,6.5f),new Vector3(-13.6f,8.1f,13.8f),new Vector3(9.5f,8.1f,13.8f),new Vector3(9.5f,8.1f,12.5f)});
            Link(wires,level.LiftFinal,new[]{new Vector3(9.5f,8.12f,12.5f),new Vector3(9.5f,8.12f,14.2f),new Vector3(-14.2f,8.12f,14.2f),new Vector3(-14.2f,8.12f,6),new Vector3(-11,8.12f,6)});
            Link(wires,level.UpperA,new[]{new Vector3(-12,16.1f,-10.3f),new Vector3(-12,16.1f,-12.6f),new Vector3(11,16.1f,-12.6f),new Vector3(11,16.1f,-10.3f)});
            Link(wires,level.UpperB,new[]{new Vector3(12.5f,16.12f,-10),new Vector3(12.5f,16.12f,-8.6f),new Vector3(11.8f,16.12f,-8.6f)});
            var combined=Link(wires,level.UpperA,new[]{new Vector3(11,16.12f,-10.3f),new Vector3(11,16.12f,-8)});combined.SecondSource=level.UpperB;
            var junction=Node("Ramp power junction",wires);junction.transform.position=new Vector3(12.3f,16.7f,-8.6f);
            Part("Power junction housing",junction,Vector3.zero,new Vector3(.7f,.45f,.18f),grey);
            JunctionLamp(junction,level.UpperA,new Vector3(-.18f,0,-.12f));JunctionLamp(junction,level.UpperB,new Vector3(.18f,0,-.12f));
            var exitWire=Link(wires,level.UpperA,new[]{new Vector3(11,24.12f,8.7f),new Vector3(12.7f,24.12f,8.7f),new Vector3(12.7f,25,9.3f)});exitWire.Source=null;exitWire.NodeSource=level.FinalNode;
            EditorUtility.SetDirty(level);foreach(var terminal in level.Terminals)EditorUtility.SetDirty(terminal);
        }
        static GeometryTerminal Terminal(Level05Controller level,string label,Vector3 position)
        {
            var go=Cube("Lever "+label,level.gameObject,position,Vector3.one,yellow);var t=go.AddComponent<GeometryTerminal>();
            t.Level=level;t.Label=label;t.Section=1;t.StableId="L05_Sequence_"+label;t.Window=10;return t;
        }
        static GameObject Part(string name,GameObject parent,Vector3 position,Vector3 size,Material mat)
        {
            var go=Cube(name,parent,Vector3.zero,size,mat);go.transform.localPosition=position;
            Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        static void MakeLever(GeometryTerminal terminal)
        {
            var old=terminal.transform.Find("Lever visuals");if(old)Object.DestroyImmediate(old.gameObject);
            old=terminal.transform.Find("Lever countdown");if(old)Object.DestroyImmediate(old.gameObject);
            terminal.transform.localScale=Vector3.one;
            var original=terminal.GetComponent<Renderer>();if(original)original.enabled=false;
            terminal.GetComponent<BoxCollider>().size=new Vector3(.75f,.65f,.35f);
            var visual=Node("Lever visuals",terminal.gameObject);
            Part("Housing",visual,Vector3.zero,new Vector3(.75f,.65f,.25f),grey);
            Part("Stand",visual,new Vector3(0,-.85f,.02f),new Vector3(.2f,1.1f,.2f),grey);
            terminal.Face=Part("Signal",visual,new Vector3(.28f,.12f,-.15f),new Vector3(.12f,.3f,.04f),yellow).GetComponent<Renderer>();
            var pivot=Node("Handle pivot",visual).transform;pivot.localPosition=new Vector3(0,0,-.23f);
            Part("Lever arm",pivot.gameObject,new Vector3(0,.24f,0),new Vector3(.07f,.48f,.07f),grey);
            Part("Grip",pivot.gameObject,new Vector3(0,.48f,0),new Vector3(.38f,.13f,.13f),yellow);
            for(int i=0;i<terminal.SequenceOrder;i++)Part("Circuit step pip",visual,new Vector3(-.24f+i*.14f,.25f,-.15f),new Vector3(.08f,.08f,.04f),cyan);
            terminal.Handle=pivot;terminal.IdleMaterial=yellow;terminal.ActiveMaterial=green;terminal.ReadyMaterial=cyan;
            var timer=Node("Lever countdown",terminal.gameObject).AddComponent<TextMeshPro>();timer.font=font;timer.fontSize=2.2f;timer.color=Color.cyan;timer.alignment=TextAlignmentOptions.Center;
            timer.rectTransform.sizeDelta=new Vector2(1.2f,.45f);timer.rectTransform.anchoredPosition3D=new Vector3(0,.65f,-.25f);terminal.Countdown=timer;timer.gameObject.SetActive(false);
            terminal.ResetTerminal();
        }
        static Material WireMat(string name,Color color)
        {
            string path="Assets/_ECHO/Materials/Level05/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",color);return mat;
        }
        static void JunctionLamp(GameObject parent,GeometryTerminal source,Vector3 position)
        {
            var lamp=Part("Power input",parent,position,new Vector3(.16f,.22f,.06f),grey);
            var signal=lamp.AddComponent<GeometryCircuitLink>();signal.Source=source;signal.Indicator=lamp.GetComponent<Renderer>();signal.IdleMaterial=WireMat("Wire idle",new Color(.15f,.24f,.28f));signal.ActiveMaterial=WireMat("Wire active",new Color(.15f,1,.55f));
        }
        static GeometryCircuitLink Link(GameObject parent,GeometryTerminal source,Vector3[] points)
        {
            var line=Node("Wire "+source.Label,parent).AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=points.Length;line.SetPositions(points);line.widthMultiplier=.065f;
            var link=line.gameObject.AddComponent<GeometryCircuitLink>();link.Source=source;link.Wire=line;link.IdleMaterial=WireMat("Wire idle",new Color(.15f,.24f,.28f));link.ActiveMaterial=WireMat("Wire active",new Color(.15f,1,.55f));line.sharedMaterial=link.IdleMaterial;
            return link;
        }
    }
}
