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

            var app = new GameObject("LogitracDrive").AddComponent<GamesApp>();
            app.Configure(entries, FindFont("Inter"), FindFont("RussoOne", "Russo One", "Russo_One"));

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            var others = EditorBuildSettings.scenes.Where(s => s.path != ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }.Concat(others).ToArray();

            PlayerSettings.productName = "Logitrac Drive";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            AssetDatabase.SaveAssets();

            Debug.Log($"[LogitracDrive] Built {ScenePath} with {entries.Length - missing.Count}/{entries.Length} thumbnails.");
            return missing;
        }

        public static string ThumbnailPath(GameEntry e) => $"{ThumbnailImportRules.Folder}/{e.artworkFile}.png";

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
