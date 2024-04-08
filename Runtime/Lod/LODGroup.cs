namespace BrgContainer.Runtime.Lod
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Represents a group of LOD (Level of Detail) meshes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LODGroup : IEquatable<LODGroup>
    {
        public LODMeshData[] LODs;

        public bool Equals(LODGroup other)
        {
            return Equals(LODs, other.LODs);
        }

        public override bool Equals(object obj)
        {
            return obj is LODGroup other && Equals(other);
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
            return hashCode;
        }

        public static bool operator ==(LODGroup left, LODGroup right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LODGroup left, LODGroup right)
        {
            return !left.Equals(right);
        }
    }
}