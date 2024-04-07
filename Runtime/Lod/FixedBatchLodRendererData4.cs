namespace BrgContainer.Runtime.Lod
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct FixedBatchLodRendererData4
    {
        private const int FloatPerData = 3;
        public const int Count = 4;
        
        [FieldOffset(0)]
        private fixed float m_Buffer[Count * FloatPerData];

        [FieldOffset(0)]
        public BatchLodRendererData LodRendererData0;
        [FieldOffset(12)]
        public BatchLodRendererData LodRendererData1;
        [FieldOffset(24)]
        public BatchLodRendererData LodRendererData2;
        [FieldOffset(36)]
        public BatchLodRendererData LodRendererData3;

        public ref BatchLodRendererData this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // every 3 float (every 12 bytes)
                fixed (float* ptr = &m_Buffer[index * FloatPerData])
                    return ref *(BatchLodRendererData*)ptr;
            }
        }
    }
}