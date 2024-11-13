using UnityEngine;
using System.IO;

public class SplatmapGenerator : MonoBehaviour
{
    public Texture2D heightmap;  // Assign your heightmap texture in the Inspector
    [HideInInspector] public float maxHeight;  // Maximum height of the terrain

    // Thresholds for each channel layer
    [Range(0,1)]
    public float sandHeightThreshold = 0.25f;

    [Range(0,1)]
    public float grassHeightThreshold = 0.5f;

    [Range(0,1)]
    public float rockHeightThreshold = 0.75f;

    [Range(0,1)]
    public float snowHeightThreshold = 1.0f;

    public void ExportSplatmap()
    {
        int width = heightmap.width;
        int height = heightmap.height;

        // Create a new texture to hold the splatmap
        Texture2D splatmap = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float normalizedHeight = heightmap.GetPixel(x, y).grayscale * maxHeight;
                Color splatColor = new Color(0, 0, 0, 1);  // Initialize to black and transparent

                // Assign values to RGBA channels based on height thresholds
                if (normalizedHeight > rockHeightThreshold * maxHeight)
                {
                    splatColor.r = 1;
                }
                else if (normalizedHeight > sandHeightThreshold * maxHeight)
                {
                    splatColor.g = 1;
                }
                else
                {
                    splatColor.b = 1;
                }
                // else
                // {
                //     splatColor.a = 1;
                // }

                splatmap.SetPixel(x, y, splatColor);
            }
        }

        splatmap.Apply();

        // Save the splatmap as a PNG file in the project's Assets folder
        byte[] bytes = splatmap.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Splat Map.png", bytes);
    }
}
