namespace BrgContainer.Runtime.Lod
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Represents a group of LOD (Level of Detail) meshes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LodGroup : IEquatable<LodGroup>
    {
        public LodMeshData[] LODs;
        public float Culled;

        public bool Equals(LodGroup other)
        {
            return Equals(LODs, other.LODs) && Culled.Equals(other.Culled);
        }

        public override bool Equals(object obj)
        {
            return obj is LodGroup other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = 17;
            if(LODs != null)
            {
                for (var i = 0; i < LODs.Length; i++)
                {
                    hashCode = HashCode.Combine(hashCode, LODs[i].GetHashCode());
                }
            }
            return HashCode.Combine(hashCode, Culled);
        }

        public static bool operator ==(LodGroup left, LodGroup right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LodGroup left, LodGroup right)
        {
            return !left.Equals(right);
        }
    }
}