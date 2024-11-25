using UnityEngine;
using System.IO;

public class HeightmapToSplatmapExporter : MonoBehaviour
{
    public Texture2D heightmap;  // Assign your heightmap texture
    public float maxHeight = 100.0f;  // Maximum height value of your terrain
    public float snowHeightThreshold = 0.9f;
    public float rockHeightThreshold = 0.7f;
    public float dirtHeightThreshold = 0.4f;
    public float sandHeightThreshold = 0.2f;

    public void ExportSplatmap()
    {
        int width = heightmap.width;
        int height = heightmap.height;

        Texture2D splatmap = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float normalizedHeight = heightmap.GetPixel(x, y).grayscale;
                Color splatColor = new Color(0, 0, 0, 1);  // Initialize alpha to 1 for opaque

                // Assign colors to channels based on height thresholds
                if (normalizedHeight < snowHeightThreshold)
                {
                    splatColor.r = 1;  // Grass layer in the red channel
                }
                else if (normalizedHeight < sandHeightThreshold)
                {
                    splatColor.g = 1;  // Dirt layer in the green channel
                }
                else
                {
                    splatColor.b = 1;  // Rock layer in the blue channel
                }

                splatmap.SetPixel(x, y, splatColor);
            }
        }

        splatmap.Apply();

        // Save the splatmap to a PNG file
        byte[] bytes = splatmap.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Splatmap.png", bytes);
        Debug.Log("Splatmap exported as Splatmap.png");
    }
}
