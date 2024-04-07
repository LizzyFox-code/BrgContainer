namespace BrgContainer.Runtime.Lod
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Represents a group of LOD (Level of Detail) meshes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LodGroup
    {
        public LodMeshData[] LODs;
        public float Culled;
    }
}