using System.IO;
using System.Linq;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace SimpleLabels.Utils
{
    /// <summary>
    /// Loads sprites from embedded resources or game assets, and computes average color from sprites (e.g. for {itemId}).
    /// </summary>
    /// <remarks>
    /// LoadEmbeddedSprite reads from assembly manifest (SimpleLabels.Resources.*). LoadGameSprite looks up
    /// by name via Resources.FindObjectsOfTypeAll. GetAverageColor samples the sprite texture, skips transparent
    /// pixels, optionally brightens; used for item-derived label colors.
    /// </remarks>
    public class SpriteManager
    {
        /// <summary>
        /// Loads a sprite from the mod's embedded resources (e.g. UIBigSprite.png, UISmallSprite.png).
        /// </summary>
        /// <param name="resourceName">File name under Resources (e.g. UIBigSprite.png).</param>
        /// <param name="customBorders">Border sizes for 9-slice (left, bottom, right, top).</param>
        public static Sprite LoadEmbeddedSprite(string resourceName, Vector4 customBorders)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = $"{assembly.GetName().Name}.Resources.{resourceName}";
    
            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
        
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(texture, buffer))
                {
                    MelonLogger.Error("Failed to load texture");
                    return null;
                }
                
                texture.name = resourceName.Replace(".png","");
        
                // Create sprite preserving original dimensions
                return Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), // pivot center
                    100f, // pixels per unit
                    0, // extra vertices
                    SpriteMeshType.FullRect,
                    customBorders // border sizes (left, bottom, right, top)
                );
            }
        }

        /// <summary>
        /// Finds a sprite in the game by name via Resources.FindObjectsOfTypeAll. Logs warning if missing.
        /// </summary>
        public static Sprite LoadGameSprite(string spriteName, Vector4 customBorders = default)
        {
            Sprite foundSprite = null;
            foundSprite = Resources.FindObjectsOfTypeAll<Sprite>()
                .Where(s => s.name == spriteName)
                .FirstOrDefault();
            if (foundSprite != null)
            {
                return foundSprite;
            }
            Logger.Warning($"Could not find sprite '{spriteName}'");
            return null;
        }
        
        /// <summary>
        /// Computes average RGB of non-transparent pixels in the sprite, with optional brightness scaling. Used for {itemId} colors.
        /// </summary>
        /// <remarks>
        /// Creates a readable texture copy, samples the sprite rect, skips transparent pixels, applies
        /// brightnessAdjustment, and returns the result. Default 1.5f brightens slightly.
        /// </remarks>
        public static Color GetAverageColor(Sprite sprite, float brightnessAdjustment = 1.5f)
        {
            if (sprite == null || sprite.texture == null)
                return Color.white; // Default fallback
        
            // Create a readable copy of the texture
            Texture2D readableTexture = MakeReadableCopy(sprite.texture);
    
            // Calculate sprite boundaries in texture
            int startX = Mathf.FloorToInt(sprite.rect.x);
            int startY = Mathf.FloorToInt(sprite.rect.y);
            int width = Mathf.FloorToInt(sprite.rect.width);
            int height = Mathf.FloorToInt(sprite.rect.height);
    
            // Read pixels only from the sprite's area in the texture
            Color[] pixels = readableTexture.GetPixels(startX, startY, width, height);
    
            // Calculate average RGB values
            float r = 0, g = 0, b = 0;
            int pixelCount = 0;
    
            foreach (Color pixel in pixels)
            {
                // Skip fully transparent pixels
                if (pixel.a < 0.01f)
                    continue;
            
                r += pixel.r;
                g += pixel.g;
                b += pixel.b;
                pixelCount++;
            }
    
            // Avoid division by zero
            if (pixelCount == 0)
            {
                Logger.Error("No non-transparent pixels found in sprite");
                UnityEngine.Object.Destroy(readableTexture);
                return Color.white;
            }
    
            // Calculate average
            Color averageColor = new Color(
                r / pixelCount,
                g / pixelCount,
                b / pixelCount
            );
    
            // Apply brightness adjustment
            Color adjustedColor = AdjustBrightness(averageColor, brightnessAdjustment);
    
            // Clean up
            UnityEngine.Object.Destroy(readableTexture);
    
            return adjustedColor;
        }
        
        private static Color AdjustBrightness(Color color, float factor)
        {
            // Convert to HSV (Hue, Saturation, Value)
            Color.RGBToHSV(color, out float h, out float s, out float v);
    
            // Adjust the Value (brightness) component
            v = Mathf.Clamp01(v * factor);
    
            // Convert back to RGB
            return Color.HSVToRGB(h, s, v);
        }
        
        private static Texture2D MakeReadableCopy(Texture2D source)
        {
            // Create a temporary RenderTexture
            RenderTexture rt = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);
        
            // Copy the texture to the RenderTexture
            Graphics.Blit(source, rt);
    
            // Remember previous RenderTexture and set our temporary one
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
    
            // Create a new readable texture
            Texture2D readableCopy = new Texture2D(source.width, source.height);
            readableCopy.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            readableCopy.Apply();
    
            // Restore previous RenderTexture
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
    
            return readableCopy;
        }
        
        public static void ExportSprite(Sprite sprite, string fileName) //For debug purposes
        {
            string gameDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string filePath = System.IO.Path.Combine(gameDir, "ExportedSprites", $"{fileName}.png");
            try
            {
                if (sprite == null)
                {
                    Logger.Error("Cannot export null sprite");
                    return;
                }

                // Create a texture from the sprite
                var texture = sprite.texture;
                if (texture == null)
                {
                    Logger.Error("Sprite has no texture");
                    return;
                }

                // Create a readable texture if the original isn't readable
                var readableTexture = new Texture2D(texture.width, texture.height);
                var renderTexture = RenderTexture.GetTemporary(texture.width, texture.height);
        
                Graphics.Blit(texture, renderTexture);
                RenderTexture.active = renderTexture;
                readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                readableTexture.Apply();
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(renderTexture);

                // Encode texture to PNG
                byte[] bytes = readableTexture.EncodeToPNG();
                System.IO.File.WriteAllBytes(filePath, bytes);
        
            }
            catch (System.Exception e)
            {
                Logger.Error($"Failed to export sprite: {e.Message}");
            }
        }
        
    }
}