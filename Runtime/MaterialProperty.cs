namespace BrgContainer.Runtime
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using JetBrains.Annotations;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("PropertyId = {PropertyId}, SizeInBytes = {SizeInBytes}, IsPerInstance = {IsPerInstance}")]
    public readonly struct MaterialProperty : IEquatable<MaterialProperty>
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

        public bool Equals(MaterialProperty other)
        {
            return PropertyId == other.PropertyId && SizeInBytes == other.SizeInBytes && IsPerInstance == other.IsPerInstance;
        }

        public override bool Equals(object obj)
        {
            return obj is MaterialProperty other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PropertyId, SizeInBytes, IsPerInstance);
        }

        public static bool operator ==(MaterialProperty left, MaterialProperty right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MaterialProperty left, MaterialProperty right)
        {
            return !left.Equals(right);
        }
    }
}