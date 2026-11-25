using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ForestioWindowsBuild
{
    [MenuItem("Forestio/Build/Windows release")]
    public static void Build()
    {
        string output = Environment.GetEnvironmentVariable("FORESTIO_BUILD_PATH");
        if (string.IsNullOrEmpty(output))
            output = Path.GetFullPath("Builds/Forestio-Windows/Forestio.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ForestioStageElevenSetup.LevelPath },
            target = BuildTarget.StandaloneWindows64,
            locationPathName = output,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Windows build failed: " + report.summary.result);
        Debug.Log("FORESTIO_WINDOWS_RELEASE_OK: " + output + "; bytes=" + report.summary.totalSize);
    }
}
