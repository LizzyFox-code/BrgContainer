namespace BrgContainer.Runtime.Lod
{
    using System.Runtime.InteropServices;
    using UnityEngine;

    /// <summary>
    /// Represents mesh data for a level of detail (LOD).
    /// </summary>
    /// <remarks>
    /// LOD mesh data consists of a mesh, a material, a submesh index, and a distance.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct LodMeshData
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
    }
}