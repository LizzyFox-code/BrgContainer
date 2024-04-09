namespace BrgContainer.Runtime.Lod
{
    using System.Runtime.InteropServices;
    using UnityEngine;

    [StructLayout(LayoutKind.Sequential)]
    public struct BatchLodDescription
    {
        public unsafe fixed float LodDistances[FixedBatchLodRendererData.Count];
        public unsafe fixed float FadeDistances[FixedBatchLodRendererData.Count];
        
        public readonly int LodCount;
        public readonly LODFadeMode FadeMode;

        public unsafe BatchLodDescription(int lodCount, LODFadeMode fadeMode)
        {
            LodCount = lodCount;
            FadeMode = fadeMode;

            for (var i = 0; i < FixedBatchLodRendererData.Count; i++)
            {
                LodDistances[i] = float.PositiveInfinity;
            }
        }
    }
}