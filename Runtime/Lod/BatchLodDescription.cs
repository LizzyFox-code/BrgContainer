namespace BrgContainer.Runtime.Lod
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct BatchLodDescription
    {
        public const int DistanceCount = 5;
        
        [FieldOffset(0)]
        internal fixed float m_Distances[DistanceCount];

        [FieldOffset(0)]
        public float LOD0;
        [FieldOffset(4)]
        public float LOD1;
        [FieldOffset(8)]
        public float LOD2;
        [FieldOffset(12)]
        public float LOD3;
        [FieldOffset(16)]
        public float Culled;

        public float this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Distances[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_Distances[index] = value;
        }
    }
}