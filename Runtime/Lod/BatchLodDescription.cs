namespace Lod
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct BatchLodDescription
    {
        [FieldOffset(0)]
        internal fixed float m_Distances[4];

        [FieldOffset(0)]
        public float Distance0;
        [FieldOffset(4)]
        public float Distance1;
        [FieldOffset(8)]
        public float Distance2;
        [FieldOffset(12)]
        public float Distance3;

        public float this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Distances[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_Distances[index] = value;
        }
    }
}