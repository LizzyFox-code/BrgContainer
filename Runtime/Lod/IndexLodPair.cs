namespace Lod
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct IndexLodPair : IEquatable<IndexLodPair>
    {
        public readonly int Index;
        public readonly int LOD;

        public IndexLodPair(int index, int lod)
        {
            Index = index;
            LOD = lod;
        }

        public bool Equals(IndexLodPair other)
        {
            return Index == other.Index && LOD == other.LOD;
        }

        public override bool Equals(object obj)
        {
            return obj is IndexLodPair other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Index, LOD);
        }

        public static bool operator ==(IndexLodPair left, IndexLodPair right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IndexLodPair left, IndexLodPair right)
        {
            return !left.Equals(right);
        }
    }
}