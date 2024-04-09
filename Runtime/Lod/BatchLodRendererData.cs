namespace BrgContainer.Runtime.Lod
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using UnityEngine;
    using UnityEngine.Rendering;

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("MeshID = {MeshID.value}, MaterialID = {MaterialID.value}")]
    public struct BatchLodRendererData // 20 bytes
    {
        public BatchMeshID MeshID;
        public BatchMaterialID MaterialID;
        public uint SubMeshIndex;

        public LODFadeMode FadeMode;
        public float FadeTransitionWidth;

        public BatchLodRendererData(BatchMeshID meshID, BatchMaterialID materialID, uint subMeshIndex, LODFadeMode fadeMode, float fadeTransitionWidth)
        {
            MeshID = meshID;
            MaterialID = materialID;
            SubMeshIndex = subMeshIndex;

            FadeMode = fadeMode;
            FadeTransitionWidth = fadeTransitionWidth;
        }
    }
}