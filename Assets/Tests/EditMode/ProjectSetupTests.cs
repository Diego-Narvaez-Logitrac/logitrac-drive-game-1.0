using System.IO;
using System.Linq;
using LogitracDrive.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LogitracDrive.Tests
{
    public class ProjectSetupTests
    {
        [Test]
        public void PortraitOnly()
        {
            Assert.AreEqual(UIOrientation.Portrait, PlayerSettings.defaultInterfaceOrientation);
        }

        [Test]
        public void GamesSceneIsFirstInBuildSettings()
        {
            Assert.IsTrue(File.Exists(ProjectSetup.ScenePath), "Run Logitrac Drive > Build Games Scene");
            var first = EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled);
            Assert.IsNotNull(first, "No scenes in Build Settings");
            Assert.AreEqual(ProjectSetup.ScenePath, first.path);
        }

        [Test]
        public void ThumbnailsImportAsSprites()
        {
            foreach (var e in GameCatalog.CreateDefaults())
            {
                var path = ProjectSetup.ThumbnailPath(e);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.IsNotNull(importer, $"Missing thumbnail: {path}");
                Assert.IsTrue(ThumbnailImportRules.IsConfigured(importer), $"{path} is not imported as a single sprite");
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(path), $"{path} has no sprite");
            }
        }
    }
}
