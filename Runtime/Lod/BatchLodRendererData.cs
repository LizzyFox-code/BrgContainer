namespace BrgContainer.Runtime.Lod
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using UnityEngine.Rendering;

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("MeshID = {MeshID.value}, MaterialID = {MaterialID.value}")]
    public readonly struct BatchLodRendererData // 12 bytes
    {
        public readonly BatchMeshID MeshID;
        public readonly BatchMaterialID MaterialID;
        public readonly uint SubMeshIndex;

        public BatchLodRendererData(BatchMeshID meshID, BatchMaterialID materialID, uint subMeshIndex)
        {
            MeshID = meshID;
            MaterialID = materialID;
            SubMeshIndex = subMeshIndex;
        }
    }
}