using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class NoiseGenerator : MonoBehaviour
{
    [Header("Display")]
    public Material displayMaterial;
    [Range(0.0f, 1.0f)] public float sliceDepth = 0.5f;
    public MeshRenderer visualizer;

    [Button]
    private void ShowShape()
    {
        visualizer.sharedMaterial = displayMaterial;
        visualizer.sharedMaterial.SetTexture("_BaseMap", shapeTexture);
    }
    
    [Header("Worley")]
    public int resolution = 128;
    public int seed = 0;
    public int axisCellCount = 8;
    public Vector4 channelMask = new Vector4(1, 0, 0, 0);
    public ComputeShader compute;
    public RenderTexture worleyTexture;
    
    [Header("Perlin")]
    public int frequency = 5;
    public int tiling = 256;
    public Texture3D perlinTexture;
    
    [Header("Cloud Textures")]
    public RenderTexture shapeTexture;
    public RenderTexture detailTexture;

    private void Start()
    {
        GenerateCloudNoises();
    }

    private void OnValidate()
    {
        displayMaterial.SetColor("_MaskColor", channelMask);
        displayMaterial.SetFloat("_SliceDepth", sliceDepth);
    }

    private RenderTexture GenerateWorleyTexture()
    {
        Random.InitState(seed);
        RenderTexture wtex = null;
        CreateRenderTexture(ref wtex, resolution, "Worley");
        var featurePointsBuffer1 = GenerateWorleyPoints(axisCellCount);
        var featurePointsBuffer2 = GenerateWorleyPoints(axisCellCount * 2);
        var featurePointsBuffer3 = GenerateWorleyPoints(axisCellCount * 4);
        
        int kernel = compute.FindKernel("CSWorley3D");
        compute.SetInt("resolution", resolution);
        compute.SetInt("axisCellCount", axisCellCount);
        compute.SetVector("channelMask", channelMask);
        compute.SetBuffer(kernel, "featurePoints1", featurePointsBuffer1);
        compute.SetBuffer(kernel, "featurePoints2", featurePointsBuffer2);
        compute.SetBuffer(kernel, "featurePoints3", featurePointsBuffer3);
        compute.SetTexture(kernel, "Result", wtex);
        
        int threadsPerGroup = Mathf.CeilToInt(resolution / (float) 8);
        
        compute.Dispatch(kernel, threadsPerGroup, threadsPerGroup, threadsPerGroup);
        
        featurePointsBuffer1.Release();
        featurePointsBuffer2.Release();
        featurePointsBuffer3.Release();
        return wtex;
    }
    
    [Button]
    private void GenerateWorley()
    {
        visualizer.sharedMaterial = displayMaterial;
        worleyTexture = GenerateWorleyTexture();
        visualizer.sharedMaterial.SetTexture("_BaseMap", worleyTexture);
    }

    private ComputeBuffer GenerateWorleyPoints(int axisCellCount)
    {
        int n = axisCellCount * axisCellCount * axisCellCount;
        var featurePoints = new Vector3[n];
        
        for (int i = 0; i < n; i++)
        {
            // these are in cell space
            featurePoints[i] = new Vector3(Random.value, Random.value, Random.value);
        }
        
        var buffer = new ComputeBuffer(n, sizeof(float) * 3);
        buffer.SetData(featurePoints);
        return buffer;
    }

    private Texture3D GeneratePerlinTexture()
    {
        float perlinScale = (float) frequency / resolution;
        
        var ptex = new Texture3D(resolution, resolution, resolution, TextureFormat.RFloat, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };
        
        var pixels = new Color[resolution * resolution * resolution];
        for (int z = 0; z < resolution; z++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float val = Perlin.Fbm(x * perlinScale, y * perlinScale, z * perlinScale, 3, tiling);
                    pixels[z * resolution * resolution + y * resolution + x] = new Color(val, 0, 0, 1);
                }
            }
        }
        
        ptex.SetPixels(pixels);
        ptex.Apply();
        return ptex;
    }
    
    [Button]
    private void GeneratePerlin()
    {
        visualizer.sharedMaterial = displayMaterial;
        perlinTexture = GeneratePerlinTexture();
        visualizer.sharedMaterial.SetTexture("_BaseMap", perlinTexture);
    }

    [Button]
    private void GenerateCloudNoises()
    {
        // TODO: Implement cloud noise generation
        // also all fbm functions should be normalized to 0-1 already
        // remember to change channel mask in between

        var perlin = GeneratePerlinTexture();
        var worley = GenerateWorleyTexture();
        
        int kernel = compute.FindKernel("CSRemapPerlin");
        compute.SetTexture(kernel, "Result", worley);
        compute.SetTexture(kernel, "PerlinNoise", perlin);
        
        int threadsPerGroup = Mathf.CeilToInt(resolution / (float) 8);
        compute.Dispatch(kernel, threadsPerGroup, threadsPerGroup, threadsPerGroup);
        
        visualizer.sharedMaterial = displayMaterial;
        visualizer.sharedMaterial.SetTexture("_BaseMap", worley);
        shapeTexture = worley;
    }
    
    private void CreateRenderTexture(ref RenderTexture renderTexture, int resolution, string name)
    {
        if (renderTexture == null || !renderTexture.IsCreated() || renderTexture.width != resolution)
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
            }

            renderTexture = new RenderTexture(resolution, resolution, 0)
            {
                enableRandomWrite = true,
                dimension = TextureDimension.Tex3D,
                volumeDepth = resolution,
                name = name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            renderTexture.Create();
        }
    }
}
