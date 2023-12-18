namespace BrgContainer.Runtime
{
    using System.Runtime.CompilerServices;
    using Unity.Burst;
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
        private static readonly SharedStatic<int> m_ObjectToWorldPropertyId = SharedStatic<int>.GetOrCreate<int, ObjectToWorldPropertyId>();
        private static readonly SharedStatic<int> m_WorldToObjectPropertyId = SharedStatic<int>.GetOrCreate<int, WorldToObjectPropertyId>();
        
        private sealed class ObjectToWorldPropertyId { }
        private sealed class WorldToObjectPropertyId { }

        [BurstDiscard]
        [RuntimeInitializeOnLoadMethod]
        private static void OnDomainReload()
        {
            m_ObjectToWorldPropertyId.Data = Shader.PropertyToID("unity_ObjectToWorld");
            m_WorldToObjectPropertyId.Data = Shader.PropertyToID("unity_WorldToObject");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTRS(this BatchInstanceDataBuffer buffer, int index, Matrix4x4 matrix)
        {
            buffer.WriteInstanceData(index, m_ObjectToWorldPropertyId.Data, new PackedMatrix(matrix));
            buffer.WriteInstanceData(index, m_WorldToObjectPropertyId.Data, new PackedMatrix(matrix.inverse));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTRS(this BatchInstanceDataBuffer buffer, int index, float4x4 matrix)
        {
            buffer.WriteInstanceData(index, m_ObjectToWorldPropertyId.Data, new PackedMatrix(matrix));
            buffer.WriteInstanceData(index, m_WorldToObjectPropertyId.Data, new PackedMatrix(math.inverse(matrix)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTRS(this BatchInstanceDataBuffer buffer, int index, float3 position, quaternion rotation, float3 scale)
        {
            var matrix = PackedMatrix.TRS(position, rotation, scale);
            buffer.WriteInstanceData(index, m_ObjectToWorldPropertyId.Data, matrix);
            buffer.WriteInstanceData(index, m_WorldToObjectPropertyId.Data, matrix.inverse);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTRS(this BatchInstanceDataBuffer buffer, int index, float3 position, quaternion rotation, float scale)
        {
            var matrix = PackedMatrix.TRS(position, rotation, new float3(scale, scale, scale));
            buffer.WriteInstanceData(index, m_ObjectToWorldPropertyId.Data, matrix);
            buffer.WriteInstanceData(index, m_WorldToObjectPropertyId.Data, matrix.inverse);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 GetTRS(this BatchInstanceDataBuffer buffer, int index)
        {
            return buffer.ReadInstanceData<PackedMatrix>(index, m_ObjectToWorldPropertyId.Data).fullMatrix;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetMatrix(this BatchInstanceDataBuffer buffer, int index, int propertyId, float4x4 matrix)
        {
            buffer.WriteInstanceData(index, propertyId, new PackedMatrix(matrix));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetMatrix(this BatchInstanceDataBuffer buffer, int index, int propertyId, Matrix4x4 matrix)
        {
            buffer.WriteInstanceData(index, propertyId, new PackedMatrix(matrix));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 GetMatrix(this BatchInstanceDataBuffer buffer, int index, int propertyId)
        {
            return buffer.ReadInstanceData<PackedMatrix>(index, propertyId).fullMatrix;
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
        public static float4 GetColor(this BatchInstanceDataBuffer buffer, int index, int propertyId)
        {
            return buffer.ReadInstanceData<float4>(index, propertyId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetVector(this BatchInstanceDataBuffer buffer, int index, int propertyId, float4 vector)
        {
            buffer.WriteInstanceData(index, propertyId, vector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 GetVector(this BatchInstanceDataBuffer buffer, int index, int propertyId)
        {
            return buffer.ReadInstanceData<float4>(index, propertyId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetFloat(this BatchInstanceDataBuffer buffer, int index, int propertyId, float value)
        {
            buffer.WriteInstanceData(index, propertyId, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFloat(this BatchInstanceDataBuffer buffer, int index, int propertyId)
        {
            return buffer.ReadInstanceData<float>(index, propertyId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetInt(this BatchInstanceDataBuffer buffer, int index, int propertyId, int value)
        {
            buffer.WriteInstanceData(index, propertyId, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetInt(this BatchInstanceDataBuffer buffer, int index, int propertyId)
        {
            return buffer.ReadInstanceData<int>(index, propertyId);
        }
    }
}