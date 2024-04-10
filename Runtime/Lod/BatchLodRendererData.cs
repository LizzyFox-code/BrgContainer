namespace BrgContainer.Runtime.Lod
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using UnityEngine.Rendering;

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("MeshID = {MeshID.value}, MaterialID = {MaterialID.value}")]
    public struct BatchLodRendererData // 16 bytes
    {
        public BatchMeshID MeshID;
        public BatchMaterialID MaterialID;
        public uint SubMeshIndex;
        
        public float FadeTransitionWidth;

        public BatchLodRendererData(BatchMeshID meshID, BatchMaterialID materialID, uint subMeshIndex, float fadeTransitionWidth)
        {
            MeshID = meshID;
            MaterialID = materialID;
            SubMeshIndex = subMeshIndex;
            
            FadeTransitionWidth = fadeTransitionWidth;
        }
    }
}