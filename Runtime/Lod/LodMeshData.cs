namespace BrgContainer.Runtime.Lod
{
    using System.Runtime.InteropServices;
    using UnityEngine;

    [StructLayout(LayoutKind.Sequential)]
    public struct LodMeshData
    {
        public Mesh Mesh;
        public Material Material;
        public uint SubMeshIndex;
        public float Distance;

        public readonly bool IsValid => Mesh != null && Material != null;
    }
}