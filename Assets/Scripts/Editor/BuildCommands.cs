using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LogitracDrive.EditorTools
{
    /// <summary>
    /// Development device builds.
    /// Menu: Logitrac Drive > Build Android APK / Build iOS Xcode Project.
    /// Batch:
    ///   Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod LogitracDrive.EditorTools.BuildCommands.BuildAndroidFromCommandLine
    ///   Unity -batchmode -quit -projectPath . -buildTarget iOS -executeMethod LogitracDrive.EditorTools.BuildCommands.BuildiOSFromCommandLine [-appleTeamId TEAMID]
    /// </summary>
    public static class BuildCommands
    {
        public const string AndroidPath = "Builds/Android/LogitracDrive.apk";
        public const string IOSPath = "Builds/iOS";

        [MenuItem("Logitrac Drive/Build Android APK")]
        public static void BuildAndroid() => Report(Build(BuildTarget.Android, BuildTargetGroup.Android, AndroidPath), interactive: true);

        [MenuItem("Logitrac Drive/Build iOS Xcode Project")]
        public static void BuildiOS() => Report(Build(BuildTarget.iOS, BuildTargetGroup.iOS, IOSPath), interactive: true);

        public static void BuildAndroidFromCommandLine() => Exit(Build(BuildTarget.Android, BuildTargetGroup.Android, AndroidPath));
        public static void BuildiOSFromCommandLine() => Exit(Build(BuildTarget.iOS, BuildTargetGroup.iOS, IOSPath));

        static BuildReport Build(BuildTarget target, BuildTargetGroup group, string output)
        {
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
            {
                Debug.LogError($"[LogitracDrive] {target} Build Support is not installed for this Editor. Add it in Unity Hub > Installs.");
                return null;
            }

            ProjectSetup.ApplyPlayerSettings();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            Directory.CreateDirectory(target == BuildTarget.iOS ? output : Path.GetDirectoryName(output));

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = target,
                targetGroup = group,
                options = BuildOptions.Development,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[LogitracDrive] {target} build {s.result}: {s.outputPath} ({s.totalSize / (1024f * 1024f):0.0} MB, " +
                      $"{s.totalErrors} errors, {s.totalWarnings} warnings, {s.totalTime.TotalSeconds:0}s). Scenes: {string.Join(", ", scenes)}");
            return report;
        }

        static void Report(BuildReport report, bool interactive)
        {
            if (!interactive) return;
            var ok = report != null && report.summary.result == BuildResult.Succeeded;
            EditorUtility.DisplayDialog("Logitrac Drive", ok ? $"Build succeeded:\n{report.summary.outputPath}" : "Build failed. See the Console.", "OK");
        }

        static void Exit(BuildReport report)
        {
            bool ok = report != null && report.summary.result == BuildResult.Succeeded;
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
