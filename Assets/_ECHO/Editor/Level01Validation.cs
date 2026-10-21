using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
namespace EchoGame.Editor
{
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
            if (command == "build") Level01Builder.Build();
            else if (command == "ui") EchoUIBuilder.BuildAll();
            else if (command == "edit") EditTests();
            else if (command == "play") PlayTests();
        }
        [MenuItem("Tools/ECHO/Validate/Edit Mode tests")]
        public static void EditTests() => Run(TestMode.EditMode, "Echo.Tests", "EditMode");
        [MenuItem("Tools/ECHO/Validate/Play Mode tests")]
        public static void PlayTests() => Run(TestMode.PlayMode, "Echo.PlayTests", "PlayMode");
        static void Run(TestMode mode, string assembly, string report)
        {
            Directory.CreateDirectory("Assets/_ECHO/Tests/Results");
            SessionState.SetString("EchoTestReport", report);
            runner.Execute(new ExecutionSettings(new Filter { testMode = mode, assemblyNames = new[] { assembly } }));
        }
        class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                string path = "Assets/_ECHO/Tests/Results/" + SessionState.GetString("EchoTestReport", "Tests") + ".xml";
                TestRunnerApi.SaveResultToFile(result, path);
                Debug.Log($"ECHO tests: {result.PassCount} passed, {result.FailCount} failed. {path}");
            }
        }
    }
}
