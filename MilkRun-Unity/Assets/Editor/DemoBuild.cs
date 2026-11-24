using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DemoBuild
{
    public const string ScenePath = "Assets/Scenes/ToyGarden.unity";

    [MenuItem("Milk Run/Build for Mac")]
    public static void Mac() => Build(BuildTarget.StandaloneOSX, "Milk Run.app");
    [MenuItem("Milk Run/Build for Windows")]
    public static void Windows() => Build(BuildTarget.StandaloneWindows64, "Milk Run.exe");

    static void Build(BuildTarget target, string filename)
    {
        if (!File.Exists(ScenePath))
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.companyName = "Toy Garden Club";
        PlayerSettings.productName = "Milk Run";
        PlayerSettings.bundleVersion = "0.18.0";
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/ToyIcons/AppIcon.png");
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] {icon});
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "games.toygarden.milkrun");
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 800;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = false;
        PlayerSettings.usePlayerLog = true;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, UnityEditor.ScriptingImplementation.Mono2x);
        string dir = Environment.GetEnvironmentVariable("MILKRUN_BUILD_DIR") ?? Path.Combine("Builds", target.ToString());
        Directory.CreateDirectory(dir);
        AssetDatabase.SaveAssets();
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { ScenePath },
            target = target,
            locationPathName = Path.Combine(dir, filename),
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Build failed: " + report.summary.result + ", errors: " + report.summary.totalErrors);
        Debug.Log("MILKRUN_BUILD_COMPLETE: " + report.summary.outputPath);
    }
}
