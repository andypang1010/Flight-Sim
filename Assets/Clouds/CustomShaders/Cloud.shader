Shader "Unlit/Cloud"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
//        Tags { "LightMode" = "ForwardBase" }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Maximum number of raymarching samples
            #define MAX_STEP_COUNT 200
            // #define PI 3.14159265359

            struct meshdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewVector : TEXCOORD1;    
            };

            v2f vert (meshdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
                o.viewVector = mul(unity_CameraToWorld, float4(viewVector,0));
                return o;
            }

            // textures
            Texture3D CloudTexture;
            SamplerState samplerCloudTexture;
            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;

            // settings
            float stepSize;
            float densityThreshold;
            float densityMultiplier;

            // other
            float3 boundsMin;
            float3 boundsMax;

            //constants
            static const float PI = 3.14159265359;

            // axis aligned bounding box
            // never returns negative distance
            float2 intersectAABB(float3 rayOrigin, float3 rayDir, float3 boxMin, float3 boxMax) {
                float3 tMin = (boxMin - rayOrigin) / rayDir;
                float3 tMax = (boxMax - rayOrigin) / rayDir;
                float3 t1 = min(tMin, tMax);
                float3 t2 = max(tMin, tMax);
                float tNear = max(max(t1.x, t1.y), t1.z);
                float tFar = min(min(t2.x, t2.y), t2.z);
                float dstToBox = max(0, tNear);
                float dstInsideBox = max(0, tFar - dstToBox);
                return float2(dstToBox, dstInsideBox);
            }

            half beer(float dst) {
                return exp(-dst * 1);
            }

            half powder(float dst) {
                return 1 - exp(-dst * 2);
            }

            half beerPowder(float dst) {
                return beer(dst) * powder(dst);
            }

            float hg(float cosTheta, float g)
            {
                // with a g=0.5, range is 0.02 (cam and sun same dir) to 0.48 (cloud between cam and sun)
                float g2 = g*g;
                return (1 - g2) / pow(1 + g2 - 2*g*cosTheta, 1.5) / (4 * PI);
            }

            float sampleCloud(float3 pos)
            {
                float baseScale = 1/1000.0;
                float3 size = boundsMax - boundsMin;
                float3 uvw = (size * .5 + pos) * baseScale;
                float density = max(0, CloudTexture.Sample(samplerCloudTexture, uvw).r - densityThreshold) * densityMultiplier;
                return density;
            }

            float lightMarch(float3 position)
            {
                float3 dirToLight = _WorldSpaceLightPos0.xyz; // direction TO light, this needs forward rendering mode
                float distInBox = intersectAABB(position, dirToLight, boundsMin, boundsMax).y;
                float stepSize = distInBox / 10; // hard coded
                float totalDensity = 0;
                for (int step = 0; step < 10; step++) {
                    float density = sampleCloud(position);
                    // float density = tex3D(_CloudTexture, position + float3(0.5, 0.5, 0.5)).r;
                    totalDensity += density * stepSize;
                    position += dirToLight * stepSize;
                }
                float transmittance = beerPowder(totalDensity); // use beer powder here because this is light, TWEAK MULTIPLIER
                return 0.2 + transmittance * 0.8; // hard coded for now
            }

            fixed4 frag (v2f p) : SV_Target
            {
                // float4 col = tex2D(_MainTex, p.uv);

                float3 rayOrigin = _WorldSpaceCameraPos.xyz;
                float viewLength = length(p.viewVector);
                float3 rayDir = p.viewVector / viewLength;
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, p.uv);
                float depth = LinearEyeDepth(rawDepth) * viewLength;
                float2 t = intersectAABB(rayOrigin, rayDir, boundsMin, boundsMax);
                float distToBox = t.x;
                float distInBox = t.y;
                float maxDist = min(distInBox, depth - distToBox);
                float3 currentPosition = rayOrigin + rayDir * distToBox;
                
                float totalDistance = 0;
                float lightEnergy = 0.5;
                float transmittance = 1;
                // float density = 0;
                // int steps = 0;
                //
                // while (totalDistance < maxDist)
                // {
                //     steps ++;
                //     if (steps >= MAX_STEP_COUNT) {
                //         break;
                //     }
                //     density += sampleCloud(currentPosition) * stepSize;
                //     totalDistance += stepSize;
                //     currentPosition += rayDir * stepSize;
                // }
                // transmittance = beer(density);
                // return col * transmittance;

                
                int stepCount = 0;
                int maxSteps = 100;
                
                while (totalDistance < maxDist) {
                    stepCount++;
                
                    // sample textures is from (0,0,0) to (1,1,1)
                    float density = sampleCloud(currentPosition);
                    // float density = tex3D(_CloudTexture, currentPosition + float3(0.5, 0.5, 0.5)).r;
                
                    float currentTransmittance = beer(density * stepSize * 2); //multiplier hard coded
                    float luminance = lightMarch(currentPosition);
                    lightEnergy += luminance * density * stepSize * transmittance;
                    transmittance *= currentTransmittance;
                
                    if (stepCount >= maxSteps) {
                        break;
                    }
                
                    if (transmittance < 0.01) {
                        break;
                    }
                    
                    totalDistance += stepSize;
                    currentPosition += rayDir * stepSize;
                }
                float cosAngle = dot(rayDir, _WorldSpaceLightPos0.xyz);
                float phase = hg(cosAngle, 0.5) * 0.5 + 0.8; // TODO: tweak
                lightEnergy *= phase;
                
                float3 background = tex2D(_MainTex, p.uv);
                float3 cloudColor = float3(1,1,1) * lightEnergy;
                float3 finalColor = background * transmittance + cloudColor * (1 - transmittance);
                return float4(finalColor, 0);
            }
            ENDCG
        }
    }
}