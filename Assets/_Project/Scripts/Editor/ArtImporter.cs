using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Import settings for the UI sprites: sprite mode, no mipmaps, and nine-slice borders so panels and buttons
    /// can be stretched without distorting their corners. Runs automatically when the PNGs are imported.
    /// </summary>
    public sealed class ArtImporter : AssetPostprocessor
    {
        private const string SpriteFolder = "Assets/_Project/Art/UI/Sprites/";
        private const float BarBorder = 8f;
        private const float PanelBorder = 16f;
        private const float CardBorder = 20f;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpriteFolder, System.StringComparison.Ordinal))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;

            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            float border = file.StartsWith("bar_") ? BarBorder : file.StartsWith("card_") ? CardBorder : PanelBorder;
            importer.spriteBorder = new Vector4(border, border, border, border);
        }
    }
}
