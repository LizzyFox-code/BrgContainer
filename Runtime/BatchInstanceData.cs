namespace BrgContainer.Runtime
{
    using System.Runtime.InteropServices;
    using Lod;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    internal struct BatchInstanceData
    {
        public unsafe fixed int InstanceCountPerLod[FixedBatchLodRendererData4.Count];
        [NativeDisableUnsafePtrRestriction]
        public unsafe int* Indices;
    }
}