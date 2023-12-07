namespace BrgContainer.Runtime
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using JetBrains.Annotations;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct MaterialProperty
    {
        public readonly int PropertyId;
        public readonly int SizeInBytes;
        public readonly bool IsPerInstance;

        public MaterialProperty(int sizeInBytes, int propertyId, bool isPerInstance = true)
        {
            PropertyId = propertyId;
            SizeInBytes = sizeInBytes;
            IsPerInstance = isPerInstance;
        }

        public MaterialProperty(int sizeInBytes, [NotNull]string propertyName, bool isPerInstance = true) : 
            this(sizeInBytes, Shader.PropertyToID(propertyName), isPerInstance)
        {
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MaterialProperty Create<T>(int propertyId, bool isPerInstance = true) where T : unmanaged
        {
            return new MaterialProperty(UnsafeUtility.SizeOf<T>(), propertyId, isPerInstance);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MaterialProperty Create<T>([NotNull]string propertyName, bool isPerInstance = true) where T : unmanaged
        {
            return new MaterialProperty(UnsafeUtility.SizeOf<T>(), propertyName, isPerInstance);
        }
    }
}