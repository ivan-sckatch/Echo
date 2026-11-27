using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EchoGame.Editor
{
    public static class EchoDoorPlacement
    {
        public const string Reports="C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-23-Level02DoorSwap";
        sealed class ArtReferences
        {
            readonly Transform panel,upper,lower;
            readonly Renderer indicator;
            readonly Renderer[] additional;
            readonly Material closed,open;
            readonly bool split;
            readonly Vector3 direction;
            readonly float travel;
            public ArtReferences(DoorController door)
            {
                panel=door.Panel;upper=door.UpperLeaf;lower=door.LowerLeaf;indicator=door.Indicator;additional=door.AdditionalIndicators;
                closed=door.ClosedMaterial;open=door.OpenMaterial;split=door.SplitDiagonally;direction=door.SplitDirection;travel=door.SplitTravel;
            }
            public void Assign(DoorController door)
            {
                door.Panel=panel;door.UpperLeaf=upper;door.LowerLeaf=lower;door.Indicator=indicator;door.AdditionalIndicators=additional;
                door.ClosedMaterial=closed;door.OpenMaterial=open;door.SplitDiagonally=split;door.SplitDirection=direction;door.SplitTravel=travel;
                door.ResetMechanism();EditorUtility.SetDirty(door);
            }
        }
        [MenuItem("Tools/ECHO/Art/Swap Level 02 door models")]
        public static void Swap()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before swapping doors.");
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save current scene first.");
            Directory.CreateDirectory(Reports);
            var scene=EditorSceneManager.OpenScene(Level02Builder.ScenePath);
            var puzzle=UnityEngine.Object.FindAnyObjectByType<DualPhasePuzzleController>();
            var first=puzzle.FirstDoor;var final=puzzle.FinalDoor;
            var firstArt=PrefabUtility.GetOutermostPrefabInstanceRoot(first.Panel.gameObject);
            var finalArt=PrefabUtility.GetOutermostPrefabInstanceRoot(final.Panel.gameObject);
            if(!firstArt||!finalArt||firstArt.transform.parent!=first.transform||finalArt.transform.parent!=final.transform)
                throw new InvalidOperationException("Expected separate door prefab instances under gameplay controllers.");
            File.Copy(Level02Builder.ScenePath,Reports+"/Level02-before-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".unity");
            var firstReferences=new ArtReferences(first);var finalReferences=new ArtReferences(final);
            firstArt.transform.SetParent(final.transform,false);finalArt.transform.SetParent(first.transform,false);
            finalReferences.Assign(first);firstReferences.Assign(final);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Reports+"/placement.txt","First: "+finalArt.name+"; second: "+firstArt.name+". Gameplay controller references, positions, timings and puzzle logic retained.");
            EditorSceneManager.OpenScene(Level02Builder.ScenePath);EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
