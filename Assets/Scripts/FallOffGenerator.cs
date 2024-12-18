using UnityEngine;
using System.Collections;
using System.IO;

public static class FalloffGenerator {

	public static float[,] GenerateFalloffMap(int size, float a, float b) {
		float[,] map = new float[size,size];
		Texture2D fallOffMap = new Texture2D(size, size);
		for (int i = 0; i < size; i++) {
			for (int j = 0; j < size; j++) {
				float x = i / (float)size * 2 - 1;
				float y = j / (float)size * 2 - 1;

				float value = Mathf.Max (Mathf.Abs (x), Mathf.Abs (y));
				map [i, j] = Evaluate(value, a, b);
				fallOffMap.SetPixel(i, j, Color.white * map[i,j]);
			}
		}

		fallOffMap.Apply();

		byte[] bytes = fallOffMap.EncodeToPNG();
		File.WriteAllBytes(Application.dataPath + "/Fall Off Map.png", bytes);

		return map;
	}

	static float Evaluate(float value, float a, float b) {

		return Mathf.Pow (value, a) / (Mathf.Pow (value, a) + Mathf.Pow (b - b * value, a));
	}
}