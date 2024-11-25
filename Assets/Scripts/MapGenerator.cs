using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;


public class MapGenerator : MonoBehaviour
{

	public enum DrawMode { NoiseMap, ColourMap, Mesh, FalloffMap };
	public DrawMode drawMode;

	public Noise.NormalizeMode normalizeMode;

	public const int mapChunkSize = 241;
	[Range(0, 6)]
	public int editorPreviewLOD;
	public float noiseScale;

	public int octaves;
	[Range(0, 1)]
	public float persistance;
	public float lacunarity;

	public int seed;
	public Vector2 offset;

	public bool useFalloff;

	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;

	public float vegetationSpreadFactor = 10f;
	public bool autoUpdate;

	public TerrainType[] regions;

	public VegetationRule[] vegetationRules;
	public bool autoUpdateVegetation = true;
	private Transform vegetationParent;
	public float placementThreshold = 0.6f;

	public MapData md;

	float[,] falloffMap;

	Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();
	Queue<VegetationPlacementRequest> vegetationPlacementQueue = new Queue<VegetationPlacementRequest>();

	void Awake()
	{
		falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
	}

	public void DrawMapInEditor()
	{
		MapData mapData = GenerateMapData(Vector2.zero);
		this.md = mapData;

		MapDisplay display = FindObjectOfType<MapDisplay>();
		if (drawMode == DrawMode.NoiseMap)
		{
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
		}
		else if (drawMode == DrawMode.ColourMap)
		{
			display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
		}
		else if (drawMode == DrawMode.Mesh)
		{
			display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD),
					TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));

			// Place vegetation in editor
			if (autoUpdateVegetation)
			{
				PlaceVegetation(mapData, Vector2.zero);
			}
		}
		else if (drawMode == DrawMode.FalloffMap)
		{
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
		}
	}

	public void RequestMapData(Vector2 centre, Action<MapData> callback)
	{
		ThreadStart threadStart = delegate
		{
			MapDataThread(centre, callback);
		};

		new Thread(threadStart).Start();
	}

	void MapDataThread(Vector2 centre, Action<MapData> callback)
	{
		MapData mapData = GenerateMapData(centre);
		lock (mapDataThreadInfoQueue)
		{
			mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
			if (autoUpdateVegetation)
			{
				lock (vegetationPlacementQueue)
				{
					vegetationPlacementQueue.Enqueue(new VegetationPlacementRequest(mapData, centre));
				}
			}
		}
	}

	public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
	{
		ThreadStart threadStart = delegate
		{
			MeshDataThread(mapData, lod, callback);
		};

		new Thread(threadStart).Start();
	}

	void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
	{
		MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
		lock (meshDataThreadInfoQueue)
		{
			meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
		}
	}

	void Update()
	{
		if (mapDataThreadInfoQueue.Count > 0)
		{
			for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
			{
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
		}

		if (meshDataThreadInfoQueue.Count > 0)
		{
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
			{
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
		}

		// Process vegetation placement requests
		if (vegetationPlacementQueue.Count > 0)
		{
			lock (vegetationPlacementQueue)
			{
				while (vegetationPlacementQueue.Count > 0)
				{
					VegetationPlacementRequest request = vegetationPlacementQueue.Dequeue();
					PlaceVegetation(this.md, request.centre);
				}
			}
		}
	}

	MapData GenerateMapData(Vector2 centre)
	{
		float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, centre + offset, normalizeMode);

		Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
		for (int y = 0; y < mapChunkSize; y++)
		{
			for (int x = 0; x < mapChunkSize; x++)
			{
				if (useFalloff)
				{
					noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
				}
				float currentHeight = noiseMap[x, y];
				for (int i = 0; i < regions.Length; i++)
				{
					if (currentHeight >= regions[i].height)
					{
						colourMap[y * mapChunkSize + x] = regions[i].colour;
					}
					else
					{
						break;
					}
				}
			}
		}

		return new MapData(noiseMap, colourMap);
	}

	void OnValidate()
	{
		if (lacunarity < 1)
		{
			lacunarity = 1;
		}
		if (octaves < 0)
		{
			octaves = 0;
		}

		falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
	}

	struct MapThreadInfo<T>
	{
		public readonly Action<T> callback;
		public readonly T parameter;

		public MapThreadInfo(Action<T> callback, T parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}

	}

	// Add this method to calculate terrain normals
	private Vector3[,] CalculateTerrainNormals(float[,] heightMap)
	{
		int width = heightMap.GetLength(0);
		int height = heightMap.GetLength(1);
		Vector3[,] normals = new Vector3[width, height];

		for (int y = 1; y < height - 1; y++)
		{
			for (int x = 1; x < width - 1; x++)
			{
				float heightL = heightMap[x - 1, y];
				float heightR = heightMap[x + 1, y];
				float heightU = heightMap[x, y + 1];
				float heightD = heightMap[x, y - 1];

				Vector3 tangentX = new Vector3(2, (heightR - heightL) * meshHeightMultiplier, 0).normalized;
				Vector3 tangentY = new Vector3(0, (heightU - heightD) * meshHeightMultiplier, 2).normalized;

				normals[x, y] = Vector3.Cross(tangentX, tangentY).normalized;
			}
		}
		return normals;
	}

	//vegetation placement
	public void PlaceVegetation(MapData mapData, Vector2 centre)
	{
		// Clear existing vegetation
		if (vegetationParent != null)
		{
			DestroyImmediate(vegetationParent.gameObject);
		}
		vegetationParent = new GameObject("Vegetation_" + centre.ToString()).transform;
		vegetationParent.parent = transform;


		float[,] heightMap = mapData.heightMap;
		Vector3[,] normals = CalculateTerrainNormals(heightMap);

		Vector3 positionOffset = new Vector3(centre.x * vegetationSpreadFactor, 0, centre.y * vegetationSpreadFactor);

		for (int y = 1; y < (mapChunkSize - 1); y++)
		{
			for (int x = 1; x < (mapChunkSize - 1); x++)
			{
				float evaluatedHeight = meshHeightCurve.Evaluate(heightMap[x, y]) * meshHeightMultiplier;
				Vector3 normal = normals[x, y];
				float slope = Vector3.Angle(normal, Vector3.up);

				foreach (VegetationRule rule in vegetationRules)
				{
					if (evaluatedHeight >= rule.minHeight &&
									evaluatedHeight <= rule.maxHeight &&
									slope >= rule.minSlopeAngle &&
									slope <= rule.maxSlopeAngle)
					{
						if (UnityEngine.Random.value < rule.density * placementThreshold)
						{
							Vector3 position = new Vector3(
											x * vegetationSpreadFactor - 1200f,
											evaluatedHeight * vegetationSpreadFactor,
											-y * vegetationSpreadFactor + 1200f
							) + positionOffset;

							position += new Vector3(
											(UnityEngine.Random.value - 0.5f) * vegetationSpreadFactor,
											0,
											(UnityEngine.Random.value - 0.5f) * vegetationSpreadFactor
							);

							GameObject vegetation = Instantiate(
											rule.prefab,
											position,
											Quaternion.Euler(0, 0, 0),
											vegetationParent
							);

							float scale = UnityEngine.Random.value * (rule.maxScale - rule.minScale) + rule.minScale;
							vegetation.transform.localScale *= scale;
						}
					}
				}
			}
		}
	}
}

[System.Serializable]
public struct TerrainType
{
	public string name;
	public float height;
	public Color colour;
}


[System.Serializable]
public class VegetationRule
{
	public string name;
	public GameObject prefab;
	public float minHeight;
	public float maxHeight;
	public float density; // 0-1
	public float minSlopeAngle = 0f; // New minimum slope angle parameter
	public float maxSlopeAngle = 45f;
	public float minScale = 0.8f;
	public float maxScale = 1.2f;
}

public struct MapData
{
	public readonly float[,] heightMap;
	public readonly Color[] colourMap;

	public MapData(float[,] heightMap, Color[] colourMap)
	{
		this.heightMap = heightMap;
		this.colourMap = colourMap;
	}
}

public struct VegetationPlacementRequest
{
	public MapData mapData;
	public Vector2 centre;

	public VegetationPlacementRequest(MapData mapData, Vector2 centre)
	{
		this.mapData = mapData;
		this.centre = centre;
	}
}