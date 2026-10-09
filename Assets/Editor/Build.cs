using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class Build
{
    const string ScenePath = "Assets/Cainos/Pixel Art Top Down - Basic/Scene/SC Demo.unity";
    const string OutDir = "Build";
    const string OutPath = "Build/PixelDemo.exe";

    public static void Run()
    {
        Directory.CreateDirectory(OutDir);

        // Only ship the demo scene
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = OutPath,
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(opts);
        var s = report.summary;

        Debug.Log("BUILD result=" + s.result + " size=" + s.totalSize + " errors=" + s.totalErrors + " warnings=" + s.totalWarnings);

        if (s.result != BuildResult.Succeeded)
        {
            foreach (var step in report.steps)
                foreach (var msg in step.messages)
                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        Debug.LogError("BUILD_ERR " + step.name + ": " + msg.content);

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("BUILD_OK " + Path.GetFullPath(OutPath));
        EditorApplication.Exit(0);
    }
}
