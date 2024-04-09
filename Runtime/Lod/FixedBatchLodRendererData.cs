namespace BrgContainer.Runtime.Lod
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct FixedBatchLodRendererData
    {
        private const int FloatPerData = 3;
        public const int Count = 8;

        [FieldOffset(0)]
        private fixed uint m_Buffer[Count * FloatPerData];

        [FieldOffset(0)]
        public BatchLodRendererData LodRendererData0;
        [FieldOffset(12)]
        public BatchLodRendererData LodRendererData1;
        [FieldOffset(24)]
        public BatchLodRendererData LodRendererData2;
        [FieldOffset(36)]
        public BatchLodRendererData LodRendererData3;
        
        [FieldOffset(48)]
        public BatchLodRendererData LodRendererData4;
        [FieldOffset(60)]
        public BatchLodRendererData LodRendererData5;
        [FieldOffset(72)]
        public BatchLodRendererData LodRendererData6;
        [FieldOffset(84)]
        public BatchLodRendererData LodRendererData7;

        public BatchLodRendererData this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                fixed (uint* ptr = &m_Buffer[index * FloatPerData])
                    return *(BatchLodRendererData*)ptr;
            }
            set
            {
                fixed (uint* ptr = &m_Buffer[index * FloatPerData])
                    *(BatchLodRendererData*)ptr = value;
            }
        }
    }
}