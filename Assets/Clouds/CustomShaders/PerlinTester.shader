Shader"Custom/PerlinTester"
{
    Properties
    {
        _Frequency("Frequency", Int) = 10
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            int _Frequency;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Permutation array
            static const int perm[] = {
                151, 160, 137, 91, 90, 15, // Initialize with standard Perlin permutation values
                131, 13, 201, 95, 96, 53, 194, 233, 7, 225,
                140, 36, 103, 30, 69, 142, 8, 99, 37, 240,
                21, 10, 23, 190, 6, 148, 247, 120, 234, 75,
                0, 26, 197, 62, 94, 252, 219, 203, 117, 35,
                11, 32, 57, 177, 33, 88, 237, 149, 56, 87,
                174, 20, 125, 136, 171, 168, 68, 175, 74, 165,
                71, 134, 139, 48, 27, 166, 77, 146, 158, 231,
                83, 111, 229, 122, 60, 211, 133, 230, 220, 105,
                92, 41, 55, 46, 245, 40, 244, 102, 143, 54,
                65, 25, 63, 161, 1, 216, 80, 73, 209, 76,
                132, 187, 208, 89, 18, 169, 200, 196, 135, 130,
                116, 188, 159, 86, 164, 100, 109, 198, 173, 186,
                3, 64, 52, 217, 226, 250, 124, 123, 5, 202,
                38, 147, 118, 126, 255, 82, 85, 212, 207, 206,
                59, 227, 47, 16, 58, 17, 182, 189, 28, 42,
                223, 183, 170, 213, 119, 248, 152, 2, 44, 154,
                163, 70, 221, 153, 101, 155, 167, 43, 172, 9,
                129, 22, 39, 253, 19, 98, 108, 110, 79, 113,
                224, 232, 178, 185, 112, 104, 218, 246, 97, 228,
                251, 34, 242, 193, 238, 210, 144, 12, 191, 179,
                162, 241, 81, 51, 145, 235, 249, 14, 239, 107,
                49, 192, 214, 31, 181, 199, 106, 157, 184, 84,
                204, 176, 115, 121, 50, 45, 127, 4, 150, 254,
                138, 236, 205, 93, 222, 114, 67, 29, 24, 72,
                243, 141, 128, 195, 78, 66, 215, 61, 156, 180
            };

            // Fade function
            float Fade(float t)
            {
                return t * t * t * (t * (t * 6 - 15) + 10);
            }

            // Linear interpolation function
            float Lerp(float a, float b, float t)
            {
                return a + t * (b - a);
            }

            // Gradient function
            float Grad(int hash, float x, float y, float z)
            {
                int h = hash % 16; // Equivalent to hash & 15
                float u = h < 8 ? x : y;
                float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
                return ((h % 2) == 0 ? u : -u) + ((h % 4) < 2 ? v : -v); // Equivalent to (h & 1) and (h & 2)
            }

            // Perlin noise function
            float PerlinNoise3D(float x, float y, float z)
{
    int X = int(floor(x)) % 256;
    int Y = int(floor(y)) % 256;
    int Z = int(floor(z)) % 256;

    x -= floor(x);
    y -= floor(y);
    z -= floor(z);

    float u = Fade(x);
    float v = Fade(y);
    float w = Fade(z);

    int A = (perm[X] + Y) % 256;
    int B = (perm[X + 1] + Y) % 256;
    int AA = (perm[A] + Z) % 256;
    int BA = (perm[B] + Z) % 256;
    int AB = (perm[A + 1] + Z) % 256;
    int BB = (perm[B + 1] + Z) % 256;

    return Lerp(w,
         Lerp(v,
              Lerp(u,
                   Grad(perm[AA], x, y, z),
                   Grad(perm[BA], x - 1, y, z)),
              Lerp(u,
                   Grad(perm[AB], x, y - 1, z),
                   Grad(perm[BB], x - 1, y - 1, z))),
         Lerp(v,
              Lerp(u,
                   Grad(perm[AA + 1], x, y, z - 1),
                   Grad(perm[BA + 1], x - 1, y, z - 1)),
              Lerp(u,
                   Grad(perm[AB + 1], x, y - 1, z - 1),
                   Grad(perm[BB + 1], x - 1, y - 1, z - 1))));
}


            float4 frag(v2f i) : SV_Target
            {
                float3 pos = float3(i.uv * _Frequency, 11.2131); // Sample position
                float noiseValue = PerlinNoise3D(pos.x, pos.y, pos.z);
                return float4(noiseValue, noiseValue, noiseValue, 1.0);
            }
            ENDCG
        }
    }
}