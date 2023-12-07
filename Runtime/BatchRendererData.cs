namespace BrgContainer.Runtime
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Mathematics;
    using UnityEngine.Rendering;

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BatchRendererData : IEquatable<BatchRendererData>
    {
        public readonly BatchMeshID MeshID;
        public readonly BatchMaterialID MaterialID;
        public readonly ushort SubMeshIndex;
        public readonly RendererDescription Description;
        public readonly float3 Extents;

        public BatchRendererData(BatchMeshID meshID, BatchMaterialID materialID, ushort subMeshIndex, float3 extents, ref RendererDescription description)
        {
            MeshID = meshID;
            MaterialID = materialID;
            SubMeshIndex = subMeshIndex;
            Extents = extents;
            Description = description;
        }

        public bool Equals(BatchRendererData other)
        {
            return MeshID.Equals(other.MeshID) && MaterialID.Equals(other.MaterialID) && SubMeshIndex == other.SubMeshIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is BatchRendererData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MeshID, MaterialID, SubMeshIndex);
        }

        public static bool operator ==(BatchRendererData left, BatchRendererData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BatchRendererData left, BatchRendererData right)
        {
            return !left.Equals(right);
        }
    }
}