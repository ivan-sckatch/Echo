using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
[assembly: UnityEditor.TestTools.TestPlayerBuildModifier(typeof(EchoGame.Editor.Level01ArtCleanBuild))]
namespace EchoGame.Editor
{
    // Rebuild serialized URP runtime resources rather than reusing an incremental test-player cache.
    public sealed class Level01ArtCleanBuild : UnityEditor.TestTools.ITestPlayerBuildModifier
    {
        public BuildPlayerOptions ModifyOptions(BuildPlayerOptions options)
        {
            if (SessionState.GetString("EchoTestReport", "") == "Level01ArtWindows") options.options |= BuildOptions.CleanBuildCache;
            return options;
        }
    }
    // Small local test harness: menu commands also work in an already open Editor.
    [InitializeOnLoad]
    public static class Level01Validation
    {
        static TestRunnerApi runner;
        const string Request = "Temp/EchoValidation.request";
        static Level01Validation()
        {
            runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            runner.RegisterCallbacks(new Results());
            EditorApplication.update += Poll;
        }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
            string command;
            try { command = File.ReadAllText(Request).Trim(); File.Delete(Request); }
            catch (IOException) { return; } // A request may still be being written by a local tool.
            if (command == "art01") Level01ArtBuilder.Apply();
            else if (command == "capture01") Level01ArtBuilder.Capture();
            else if (command == "testart01") Run(TestMode.PlayMode, "Echo.PlayTests", "Level01ArtPlayMode", "^EchoGame.Tests.Level01");
            else if (command == "playerart01")
            {
                SessionState.SetString("EchoTestReport", "Level01ArtWindows");
                runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode, assemblyNames = new[] { "Echo.PlayTests" },
                    groupNames = new[] { "^EchoGame.Tests.Level01" }, targetPlatform = BuildTarget.StandaloneWindows64 }) { playerHeartbeatTimeout = 180 });
            }
            else if (command == "build") Level01Builder.Build();
            else if (command == "level02") Level02Builder.Build();
            else if (command == "level03") Level03Builder.Build();
            else if (command == "level03batch") Level03Builder.BuildBatch();
            else if (command == "level04batch") Level04Builder.BuildBatch();
            else if (command == "level05batch") Level05Builder.BuildBatch();
            else if (command == "signs05") Level05Builder.RemoveWorldSigns();
            else if (command == "levers05") Level05LeverUpgrade.Apply();
            else if (command == "upper05") Level05LeverUpgrade.ApplyUpper();
            else if (command == "testupper05") Run(TestMode.PlayMode, "Echo.PlayTests", "Level05Upper", "^EchoGame.Tests.Level05Tests");
            else if (command == "researchrecord") Run(TestMode.PlayMode, "Echo.PlayTests", "ResearchRecord", "^EchoGame.Tests.ResearchInspectionTests");
            else if (command == "testlevers05") Run(TestMode.PlayMode, "Echo.PlayTests", "Level05Levers", "^EchoGame.Tests.Level05Tests");
            else if (command == "regressionlevers05") Run(TestMode.PlayMode, "Echo.PlayTests", "Level05LeverRegression");
            else if (command == "recordinglevers05") Run(TestMode.EditMode, "Echo.Tests", "Level05LeverRecording", "^EchoGame.Tests.RecordingTests");
            else if (command == "scanner05") Run(TestMode.PlayMode, "Echo.PlayTests", "Level05Scanner", "^EchoGame.Tests.Level05Tests");
            else if (command == "reset05") Run(TestMode.PlayMode, "Echo.PlayTests", "Level05CheckpointReset", "^EchoGame.Tests.Level05Tests");
            else if (command == "play05") Run(TestMode.PlayMode, "Echo.PlayTests", "Level05PlayMode", "^EchoGame.Tests.Level05Tests");
            else if (command == "route05") Run(TestMode.PlayMode, "Echo.PlayTests", "Level05Route", "^EchoGame.Tests.Level05Tests.Complete");
            else if (command == "player05")
            {
                SessionState.SetString("EchoTestReport", "Level05Windows");
                runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode, assemblyNames = new[] { "Echo.PlayTests" },
                    groupNames = new[] { "^EchoGame.Tests.Level05Tests" }, targetPlatform = BuildTarget.StandaloneWindows64 }) { playerHeartbeatTimeout = 180 });
            }
            else if (command == "edit05") Run(TestMode.EditMode, "Echo.Tests", "Level05EditMode");
            else if (command == "regression05") Run(TestMode.PlayMode, "Echo.PlayTests", "Level05Regression");
            else if (command == "preview04") Level04Builder.PreviewStart();
            else if (command == "inspection") ResearchInspectionBuilder.Apply();
            else if (command == "vision") Run(TestMode.PlayMode, "Echo.PlayTests", "Level04FloorVision", "^EchoGame.Tests.CameraVisionFloorTests");
            else if (command == "playerfloor")
            {
                SessionState.SetString("EchoTestReport", "Level04WindowsFloorVision");
                runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode, assemblyNames = new[] { "Echo.PlayTests" },
                    groupNames = new[] { "^EchoGame.Tests.Level04Tests", "^EchoGame.Tests.ResearchInspectionTests", "^EchoGame.Tests.CameraVisionFloorTests" }, targetPlatform = BuildTarget.StandaloneWindows64 }) { playerHeartbeatTimeout = 180 });
            }
            else if (command == "playerinspection")
            {
                SessionState.SetString("EchoTestReport", "Level04WindowsInspection");
                runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode, assemblyNames = new[] { "Echo.PlayTests" },
                    groupNames = new[] { "^EchoGame.Tests.Level04Tests", "^EchoGame.Tests.ResearchInspectionTests" }, targetPlatform = BuildTarget.StandaloneWindows64 }) { playerHeartbeatTimeout = 180 });
            }
            else if (command == "play04") Run(TestMode.PlayMode, "Echo.PlayTests", "Level04PlayMode", "^EchoGame.Tests.Level04Tests");
            else if (command == "regression04") Run(TestMode.PlayMode, "Echo.PlayTests", "Level04AllPlayMode");
            else if (command == "edit04") Run(TestMode.EditMode, "Echo.Tests", "Level04EditMode");
            else if (command == "windows04") Level04Builder.BuildPlayer();
            else if (command == "player04") PlayerTests04();
            else if (command == "hint-preview") Level03Builder.PreviewFragmentHint();
            else if (command == "windows") Level03Builder.BuildPlayer();
            else if (command == "player-tests") PlayerTests();
            else if (command == "ui") EchoUIBuilder.BuildAll();
            else if (command == "compact") EchoUIBuilder.ApplyCompactUI();
            else if (command == "edit") EditTests();
            else if (command == "play") PlayTests();
        }
        [MenuItem("Tools/ECHO/Validate/Edit Mode tests")]
        public static void EditTests() => Run(TestMode.EditMode, "Echo.Tests", "EditMode");
        [MenuItem("Tools/ECHO/Validate/Play Mode tests")]
        public static void PlayTests() => Run(TestMode.PlayMode, "Echo.PlayTests", "PlayMode");
        [MenuItem("Tools/ECHO/Validate/Level 03 Windows Player tests")]
        public static void PlayerTests()
        {
            Directory.CreateDirectory(Level03Builder.Reports); SessionState.SetString("EchoTestReport", "WindowsPlayMode");
            // This package exposes the public legacy platform on Filter and forwards it to ExecutionSettings.
            runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode, assemblyNames = new[] { "Echo.PlayTests" },
                groupNames = new[] { "^EchoGame.Tests.Level03Tests" }, targetPlatform = BuildTarget.StandaloneWindows64 }) { playerHeartbeatTimeout = 180 });
        }
        static void Run(TestMode mode, string assembly, string report, string group = null)
        {
            Directory.CreateDirectory(Level03Builder.Reports);
            SessionState.SetString("EchoTestReport", report);
            runner.Execute(new ExecutionSettings(new Filter { testMode = mode, assemblyNames = new[] { assembly }, groupNames = group == null ? null : new[] { group } }));
        }
        static void PlayerTests04()
        {
            SessionState.SetString("EchoTestReport", "Level04WindowsPlayMode");
            runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode, assemblyNames = new[] { "Echo.PlayTests" }, groupNames = new[] { "^EchoGame.Tests.Level04Tests" }, targetPlatform = BuildTarget.StandaloneWindows64 }) { playerHeartbeatTimeout = 180 });
        }
        class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                string name = SessionState.GetString("EchoTestReport", "Tests");
                string directory = name=="Level05CheckpointReset" ? "C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-12-Level05-CheckpointReset" : name=="Level05Scanner" ? Level05Builder.ScannerReports : name.StartsWith("Level05") ? Level05Builder.Reports : name.StartsWith("Level01Art") ? Level01ArtBuilder.Reports : name.StartsWith("Level04") ? Level04Builder.Reports : Level03Builder.Reports;
                if(name.StartsWith("Level05Lever"))directory=Level05LeverUpgrade.Reports;
                if(name=="Level05Upper")directory=Level05LeverUpgrade.UpperReports;
                if(name=="ResearchRecord")directory="C:/Users/user/Desktop/4-ый курс/Диплом/.work-gdd/validation/2026-11-12-ResearchRecord";
                Directory.CreateDirectory(directory);
                string path = directory + "/" + name + ".xml";
                TestRunnerApi.SaveResultToFile(result, path);
                Debug.Log($"ECHO tests: {result.PassCount} passed, {result.FailCount} failed. {path}");
            }
        }
    }
}
