using UnityEngine;

public class MountainSphere : MonoBehaviour
{
    public Material material;
    public Texture2D grassTexture;
    public Texture2D snowTexture;
    public float snowHeight = 0.5f;
    public float grassHeight = -0.5f;
    public int textureResolution = 512;
    public float noiseScale = 5f;
    public float noiseMagnitude = 0.1f;
    public bool useRandomSeed = true;
    public int seed = 0;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private Mesh sphereMesh;

    void Start()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(transform);
        sphere.transform.localPosition = Vector3.zero;

        meshRenderer = sphere.GetComponent<MeshRenderer>();
        meshFilter = sphere.GetComponent<MeshFilter>();
        sphereMesh = meshFilter.mesh;

        if (material == null || grassTexture == null || snowTexture == null)
        {
            Debug.LogError("Please assign all required assets in the Inspector.");
            return;
        }

        meshRenderer.material = material;

        if (useRandomSeed)
        {
            seed = Random.Range(0, 10000);
        }
        Random.InitState(seed);

        Texture2D proceduralTexture = GenerateProceduralTexture();
        material.SetTexture("_BaseMap", proceduralTexture);

        Debug.Log($"Texture generated with seed: {seed}");
    }

    float SeamlessNoise(float x, float y)
    {
        float xSin = Mathf.Sin(x * 2 * Mathf.PI) * noiseScale;
        float xCos = Mathf.Cos(x * 2 * Mathf.PI) * noiseScale;

        return Mathf.PerlinNoise(xSin + y * noiseScale + seed, xCos + seed);
    }

    Texture2D GenerateProceduralTexture()
    {
        Texture2D texture = new Texture2D(textureResolution, textureResolution);
        texture.wrapMode = TextureWrapMode.Repeat;

        for (int y = 0; y < textureResolution; y++)
        {
            for (int x = 0; x < textureResolution; x++)
            {
                float normalizedX = (float)x / textureResolution;
                float normalizedY = (float)y / textureResolution;

                float noise = SeamlessNoise(normalizedX, normalizedY);
                float height = normalizedY + (noise - 0.5f) * noiseMagnitude;

                Color pixelColor;

                if (height >= snowHeight)
                {
                    pixelColor = snowTexture.GetPixelBilinear(normalizedX, normalizedY);
                }
                else if (height <= grassHeight)
                {
                    pixelColor = grassTexture.GetPixelBilinear(normalizedX, normalizedY);
                }
                else
                {
                    float t = Mathf.InverseLerp(grassHeight, snowHeight, height);
                    Color grassPixel = grassTexture.GetPixelBilinear(normalizedX, normalizedY);
                    Color snowPixel = snowTexture.GetPixelBilinear(normalizedX, normalizedY);
                    pixelColor = Color.Lerp(grassPixel, snowPixel, t);
                }

                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();
        return texture;
    }
}