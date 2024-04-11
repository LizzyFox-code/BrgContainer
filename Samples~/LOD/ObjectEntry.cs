using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Samples_.LOD
{
    [Serializable]
    public sealed class ObjectEntry
    {
        public LODFadeMode FadeMode = LODFadeMode.CrossFade;
        public LODDescription[] LODs;
        
        public Color Color = Color.white;

        public int MaxCount = 100;

        public ShadowCastingMode ShadowCastingMode = ShadowCastingMode.On;
        public bool ReceiveShadows = true;
        public bool StaticShadowCaster = true;
    }

    [Serializable]
    public sealed class LODDescription
    {
        public Mesh Mesh;
        public Material Material;
        
        [Range(0.0f, 1.0f)]
        public float ScreenRelativeTransitionHeight; // relative distance in percent (from 0 to 1)
        [Range(0.0f, 1.0f)]
        public float FadeTransitionWidth = 0.5f; // transition width (maxDistance - minDistance) * FadeTransitionWidth;
    }
}