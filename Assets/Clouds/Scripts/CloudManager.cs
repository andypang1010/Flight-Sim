using System;
using Unity.VisualScripting;
using UnityEngine;

namespace DefaultNamespace
{
    [ExecuteInEditMode, ImageEffectAllowedInSceneView]
    public class CloudManager : MonoBehaviour
    {
        public Transform cloudBox;
        public Shader cloudShader;
        public NoiseGenerator noiseGenerator;

        [Header("Settings")]
        public float stepSize;
        public float densityThreshold;
        public float densityMultiplier;
        public float ditherMultiplier;
        
        [Header("Sphere Shape")]
        public bool useSphere;
        public float innerRadius;
        public float outerRadius;
        public Vector3 sphereCenter;
        
        
        private Material cloudMaterial;

        [ImageEffectOpaque]
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (noiseGenerator.shapeTexture == null)
            {
                return;
            }
            noiseGenerator.shapeTexture.wrapMode = TextureWrapMode.Repeat;
            if (cloudMaterial == null)
            {
                cloudMaterial = new Material(cloudShader);
            }
            
            cloudMaterial.SetVector("boundsMin", cloudBox.position - cloudBox.localScale / 2);
            cloudMaterial.SetVector("boundsMax", cloudBox.position + cloudBox.localScale / 2);
            cloudMaterial.SetVector("sphereCenter", sphereCenter);
            cloudMaterial.SetFloat("innerRadius", innerRadius);
            cloudMaterial.SetFloat("outerRadius", outerRadius);
            cloudMaterial.SetFloat("useSphere", useSphere ? 1 : 0);
            cloudMaterial.SetFloat("stepSize", stepSize);
            cloudMaterial.SetFloat("densityThreshold", densityThreshold);
            cloudMaterial.SetFloat("densityMultiplier", densityMultiplier);
            cloudMaterial.SetFloat("ditherMultiplier", ditherMultiplier);
            
            cloudMaterial.SetTexture("CloudTexture", noiseGenerator.shapeTexture);
            cloudMaterial.SetTexture("BlueNoiseTexture", noiseGenerator.blueNoiseTexture);
            Graphics.Blit(source, destination, cloudMaterial);
        }
    }
}