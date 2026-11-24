using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ForestioStageFourteenPreview
{
    public static void Build()
    {
        string output = Environment.GetEnvironmentVariable("FORESTIO_PREVIEW_PATH");
        if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("FORESTIO_PREVIEW_PATH is required.");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ForestioStageElevenSetup.LevelPath },
            target = BuildTarget.StandaloneWindows64,
            locationPathName = output,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Visual preview build failed: " + report.summary.result);
        Debug.Log("FORESTIO_STAGE14_PREVIEW_BUILD_OK");
    }
}
