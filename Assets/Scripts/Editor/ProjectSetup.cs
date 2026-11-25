using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LogitracDrive.EditorTools
{
    /// <summary>
    /// Builds Assets/Scenes/Games.unity, wires the thumbnails into the game catalog, sets portrait orientation
    /// and puts Games first in Build Settings.
    /// Menu: Logitrac Drive > Build Games Scene.
    /// Batch: Unity -batchmode -projectPath . -executeMethod LogitracDrive.EditorTools.ProjectSetup.BuildFromCommandLine -quit
    /// </summary>
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Games.unity";
        public const string FontsFolder = "Assets/Art/Fonts";

        [MenuItem("Logitrac Drive/Build Games Scene")]
        public static void BuildGamesScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var missing = Build();
            if (missing.Count > 0)
                EditorUtility.DisplayDialog("Logitrac Drive", "Missing thumbnails (cards show a placeholder):\n" + string.Join("\n", missing), "OK");
        }

        public static void BuildFromCommandLine()
        {
            var missing = Build();
            if (missing.Count > 0)
            {
                Debug.LogError("[LogitracDrive] Missing thumbnails: " + string.Join(", ", missing));
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Returns the thumbnail paths that could not be loaded as sprites.</summary>
        public static List<string> Build()
        {
            var missing = new List<string>();
            var entries = GameCatalog.CreateDefaults();
            foreach (var e in entries)
            {
                var path = ThumbnailPath(e);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && !ThumbnailImportRules.IsConfigured(importer))
                {
                    ThumbnailImportRules.ApplyTo(importer);
                    importer.SaveAndReimport();
                }
                e.artwork = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (e.artwork == null) missing.Add(path);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;
            camGo.transform.position = new Vector3(0, 0, -10);

            var bodyFont = FindFont("Inter");
            var displayFont = FindFont("RussoOne", "Russo One", "Russo_One");
            var app = new GameObject("LogitracDrive").AddComponent<GamesApp>();
            app.Configure(entries, bodyFont, displayFont);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            BuildTurboCircuitScene(bodyFont, displayFont);

            // Games first, Turbo Circuit second, then anything else already listed.
            var others = EditorBuildSettings.scenes.Where(s => s.path != ScenePath && s.path != TurboCircuitScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(TurboCircuitScenePath, true),
            }.Concat(others).ToArray();

            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();

            Debug.Log($"[LogitracDrive] Built {ScenePath} with {entries.Length - missing.Count}/{entries.Length} thumbnails.");
            return missing;
        }

        public static string ThumbnailPath(GameEntry e) => $"{ThumbnailImportRules.Folder}/{e.artworkFile}.png";

        public const string CompanyName = "Logitrac";
        public const string ProductName = "Logitrac Drive";
        public const string Version = "0.1.0";
        public const int BuildNumber = 1;
        public const string BundleId = "com.logitrac.drive";
        /// <summary>Apple Team ID comes from the environment or the command line, never from source.</summary>
        public const string TeamIdEnv = "LOGITRAC_APPLE_TEAM_ID";

        /// <summary>Player settings for device builds (iOS + Android). Minimum OS/API levels stay at Unity's defaults.</summary>
        [MenuItem("Logitrac Drive/Apply Player Settings")]
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;

            // Portrait only, on every platform.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Android: IL2CPP, ARM64 only, APK with the default debug keystore.
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode = BuildNumber;
            PlayerSettings.Android.useCustomKeystore = false;
            EditorUserBuildSettings.buildAppBundle = false;

            // iOS: automatic signing; Team ID only when supplied (otherwise keep what's already set).
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, BundleId);
            PlayerSettings.iOS.buildNumber = BuildNumber.ToString();
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            var teamId = CommandLineValue("-appleTeamId") ?? System.Environment.GetEnvironmentVariable(TeamIdEnv);
            if (!string.IsNullOrWhiteSpace(teamId)) PlayerSettings.iOS.appleDeveloperTeamID = teamId.Trim();

            AssetDatabase.SaveAssets();
            Debug.Log($"[LogitracDrive] Player settings applied: {CompanyName} / {ProductName} {Version} ({BuildNumber}), {BundleId}, " +
                      $"iOS team {(string.IsNullOrEmpty(PlayerSettings.iOS.appleDeveloperTeamID) ? "not set" : "set")}.");
        }

        static string CommandLineValue(string flag)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == flag) return args[i + 1];
            return null;
        }

        public const string TurboCircuitScenePath = "Assets/Scenes/TurboCircuit.unity";
        public const string DataFolder = "Assets/Data";
        public const string TuningPath = DataFolder + "/TurboTuning.asset";
        public const string UnlitMaterialPath = DataFolder + "/TurboUnlit.mat";

        /// <summary>Camera + one race root; RaceController builds the rest at start.</summary>
        static void BuildTurboCircuitScene(Font bodyFont, Font displayFont)
        {
            if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder("Assets", "Data");

            // Keep an existing tuning asset so hand-tuned values survive a rebuild.
            var tuning = AssetDatabase.LoadAssetAtPath<TurboCircuit.TurboTuning>(TuningPath);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<TurboCircuit.TurboTuning>();
                AssetDatabase.CreateAsset(tuning, TuningPath);
            }

            // A material asset (rather than Shader.Find at runtime) so the shader ships in player builds.
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
            if (unlit == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                unlit = new Material(shader);
                AssetDatabase.CreateAsset(unlit, UnlitMaterialPath);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = GamesApp.ReferenceHeight * TurboCircuit.TurboSpace.Scale / 2f / 0.95f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Hex(0x16201b); // prototype background (outside the walls)
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            camGo.transform.position = new Vector3(0, 0, -10);

            var race = new GameObject("TurboCircuit").AddComponent<TurboCircuit.RaceController>();
            race.Configure(tuning, unlit, bodyFont, displayFont);

            EditorSceneManager.SaveScene(scene, TurboCircuitScenePath);
            Debug.Log($"[LogitracDrive] Built {TurboCircuitScenePath}.");
        }

        /// <summary>Optional: drop Inter / Russo One .ttf or .otf files in Assets/Art/Fonts. Otherwise the built-in font is used.</summary>
        static Font FindFont(params string[] names)
        {
            if (!AssetDatabase.IsValidFolder(FontsFolder)) return null;
            var fonts = AssetDatabase.FindAssets("t:Font", new[] { FontsFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToList();
            foreach (var n in names)
            {
                var regular = fonts.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).Replace(" ", "")
                    .StartsWith(n.Replace(" ", "")) && p.Contains("Regular"));
                var any = regular ?? fonts.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).Replace(" ", "")
                    .StartsWith(n.Replace(" ", "")));
                if (any != null) return AssetDatabase.LoadAssetAtPath<Font>(any);
            }
            return null;
        }
    }
}
