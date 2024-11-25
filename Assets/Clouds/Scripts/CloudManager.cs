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
        private Material cloudMaterial;

        public float stepSize;
        public float densityThreshold;
        public float densityMultiplier;
        
        public NoiseGenerator noiseGenerator;

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
            cloudMaterial.SetFloat("stepSize", stepSize);
            cloudMaterial.SetFloat("densityThreshold", densityThreshold);
            cloudMaterial.SetFloat("densityMultiplier", densityMultiplier);
            
            cloudMaterial.SetTexture("CloudTexture", noiseGenerator.shapeTexture);
            Graphics.Blit(source, destination, cloudMaterial);
        }
    }
}