using UnityEditor;

namespace LogitracDrive.EditorTools
{
    /// <summary>Anything placed in Assets/Art/Thumbnails imports as a single UI sprite.</summary>
    public class ThumbnailImportRules : AssetPostprocessor
    {
        public const string Folder = "Assets/Art/Thumbnails";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder + "/")) return;
            var importer = (TextureImporter)assetImporter;
            ApplyTo(importer);
        }

        public static void ApplyTo(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048; // source art is 1672x941
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }

        public static bool IsConfigured(TextureImporter importer)
        {
            return importer != null
                && importer.textureType == TextureImporterType.Sprite
                && importer.spriteImportMode == SpriteImportMode.Single;
        }
    }
}
