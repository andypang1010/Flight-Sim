Shader "Custom/OneChannel"
{
    Properties
    {
        _BaseMap("Texture", 3D) = "" {}
        _SliceDepth("Slice Depth", Range(0, 1)) = 0.5
        _MaskColor("Mask Color", Color) = (1, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

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

            sampler3D _BaseMap;
            float _SliceDepth;
            fixed4 _MaskColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 uvw = float3(i.uv, _SliceDepth);
                float value = dot(tex3D(_BaseMap, uvw), _MaskColor);
                return float4(value, value, value, 1);
            }
            ENDCG
        }
    }
}
