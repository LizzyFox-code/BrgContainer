namespace BrgContainer.Runtime
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Size = {Size}, Offset = {Offset}, PropertyId = {PropertyId}, IsPerInstance = {IsPerInstance}")]
    public readonly struct MetadataInfo : IEquatable<MetadataInfo>
    {
        /// <summary>
        /// The size of metadata type in bytes.
        /// </summary>
        public readonly int Size;
        /// <summary>
        /// The offset of metadata in bytes.
        /// </summary>
        public readonly int Offset;
        /// <summary>
        /// The material property id.
        /// </summary>
        public readonly int PropertyId;
        /// <summary>
        /// Is this property per instance or per material?
        /// </summary>
        public readonly bool IsPerInstance;

        public MetadataInfo(int size, int offset, int propertyId, bool isPerInstance)
        {
            Size = size;
            Offset = offset;
            
            PropertyId = propertyId;
            IsPerInstance = isPerInstance;
        }

        public bool Equals(MetadataInfo other)
        {
            return PropertyId == other.PropertyId;
        }

        public override bool Equals(object obj)
        {
            return obj is MetadataInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PropertyId);
        }
    }
}