namespace BrgContainer.Runtime.Lod
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct LodGroup
    {
        public LodMeshData[] LODs;
        public float Culled;
    }
}