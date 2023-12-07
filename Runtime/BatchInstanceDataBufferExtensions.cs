namespace BrgContainer.Runtime
{
    using System.Runtime.CompilerServices;
    using Unity.Mathematics;
    using UnityEngine;
#if ENABLE_IL2CPP
    using Il2Cpp;
#endif

#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
#endif
    public static class BatchInstanceDataBufferExtensions
    {
        private const int ObjectToWorldPropertyId = 160;
        private const int WorldToObjectPropertyId = 161;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTRS(this BatchInstanceDataBuffer buffer, int index, Matrix4x4 matrix)
        {
            buffer.WriteInstanceData(index, ObjectToWorldPropertyId, new PackedMatrix(matrix));
            buffer.WriteInstanceData(index, WorldToObjectPropertyId, new PackedMatrix(matrix.inverse));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTRS(this BatchInstanceDataBuffer buffer, int index, float4x4 matrix)
        {
            buffer.WriteInstanceData(index, ObjectToWorldPropertyId, new PackedMatrix(matrix));
            buffer.WriteInstanceData(index, WorldToObjectPropertyId, new PackedMatrix(math.inverse(matrix)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTRS(this BatchInstanceDataBuffer buffer, int index, float3 position, quaternion rotation, float3 scale)
        {
            var matrix = PackedMatrix.TRS(position, rotation, scale);
            buffer.WriteInstanceData(index, ObjectToWorldPropertyId, matrix);
            buffer.WriteInstanceData(index, WorldToObjectPropertyId, matrix.inverse);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetColor(this BatchInstanceDataBuffer buffer, int index, int propertyId, Color color)
        {
            var colorVector = new float4(color.r, color.g, color.b, color.a);
            buffer.WriteInstanceData(index, propertyId, colorVector);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetColor(this BatchInstanceDataBuffer buffer, int index, int propertyId, float4 color)
        {
            buffer.WriteInstanceData(index, propertyId, color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetVector(this BatchInstanceDataBuffer buffer, int index, int propertyId, float4 vector)
        {
            buffer.WriteInstanceData(index, propertyId, vector);
        }
    }
}