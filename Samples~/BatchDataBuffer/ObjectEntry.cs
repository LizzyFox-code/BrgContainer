namespace Samples.Batch_Data_Buffer
{
    using System;
    using UnityEngine;
    using UnityEngine.Rendering;
    
    [Serializable]
    public sealed class ObjectEntry
    {
        public Mesh Mesh;
        public Material Material;

        public int MaxCount = 100;

        public ShadowCastingMode ShadowCastingMode = ShadowCastingMode.On;
        public bool ReceiveShadows = true;
        public bool StaticShadowCaster = true;
    }
}