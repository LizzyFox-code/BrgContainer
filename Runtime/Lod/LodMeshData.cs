namespace BrgContainer.Runtime.Lod
{
    using System;
    using System.Runtime.InteropServices;
    using UnityEngine;

    /// <summary>
    /// Represents mesh data for a level of detail (LOD).
    /// </summary>
    /// <remarks>
    /// LOD mesh data consists of a mesh, a material, a submesh index, and a distance.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct LodMeshData : IEquatable<LodMeshData>
    {
        public Mesh Mesh;
        public Material Material;

        /// <summary>
        /// The index of a submesh in a LOD mesh.
        /// </summary>
        /// <remarks>
        /// The SubMeshIndex is used to identify a specific submesh within a LOD mesh.
        /// </remarks>
        public uint SubMeshIndex;

        /// <summary>
        /// Represents the distance for a level of detail (LOD) mesh.
        /// </summary>
        public float Distance;

        public bool Equals(LodMeshData other)
        {
            return Equals(Mesh, other.Mesh) && Equals(Material, other.Material) && SubMeshIndex == other.SubMeshIndex && Distance.Equals(other.Distance);
        }

        public override bool Equals(object obj)
        {
            return obj is LodMeshData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Mesh, Material, SubMeshIndex, Distance);
        }

        public static bool operator ==(LodMeshData left, LodMeshData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LodMeshData left, LodMeshData right)
        {
            return !left.Equals(right);
        }
    }
}