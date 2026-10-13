#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace CutFreePets.Editor
{
    /// <summary>
    /// Automatically configures extracted cat animation frames as 2D Sprites.
    /// </summary>
    public class CatSpriteImporterHelper : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath.Contains("Assets/Art/cat/Frames_") || assetPath.Contains("Assets/Art/cat/Frames"))
            {
                TextureImporter importer = (TextureImporter)assetImporter;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.spritePixelsPerUnit = 256;
                importer.filterMode = FilterMode.Bilinear;
                
                TextureImporterPlatformSettings settings = importer.GetDefaultPlatformTextureSettings();
                settings.format = TextureImporterFormat.RGBA32;
                settings.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(settings);
            }
        }

        [MenuItem("Cut Free Pets/Reimport Cat Sprites", false, 10)]
        public static void ReimportCatSprites()
        {
            string[] folders = new string[] { 
                "Assets/Art/cat/Frames_Happy", 
                "Assets/Art/cat/Frames_Sad",
                "Assets/Art/Environment/Debris",
                "Assets/Resources/Debris",
                "Assets/Art/Environment"
            };
            int count = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string folder in folders)
                {
                    if (Directory.Exists(folder))
                    {
                        string[] files = Directory.GetFiles(folder, "*.png");
                        foreach (string file in files)
                        {
                            string unityPath = file.Replace("\\", "/");
                            TextureImporter importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
                            if (importer != null)
                            {
                                importer.textureType = TextureImporterType.Sprite;
                                importer.spriteImportMode = SpriteImportMode.Single;
                                importer.alphaIsTransparency = true;
                                importer.mipmapEnabled = false;
                                importer.spritePixelsPerUnit = 256;
                                importer.filterMode = FilterMode.Bilinear;
                                importer.SaveAndReimport();
                                count++;
                            }
                            else
                            {
                                AssetDatabase.ImportAsset(unityPath, ImportAssetOptions.ForceUpdate);
                                count++;
                            }
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[CatSpriteImporterHelper] Configured and reimported {count} cat sprite frames.");
        }
    }
}
#endif
