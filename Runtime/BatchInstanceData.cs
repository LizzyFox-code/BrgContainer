namespace BrgContainer.Runtime
{
    using System.Runtime.InteropServices;
    using Lod;

    [StructLayout(LayoutKind.Sequential)]
    internal struct BatchInstanceData
    {
        public unsafe int* Indices;
        public unsafe fixed int InstanceCountPerLod[FixedBatchLodRendererData4.Count];
    }
}