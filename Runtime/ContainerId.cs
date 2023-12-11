namespace BrgContainer.Runtime
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Value = {Value}")]
    internal readonly struct ContainerId : IEquatable<ContainerId>
    {
        public readonly long Value;

        public ContainerId(long value)
        {
            Value = value;
        }

        public bool Equals(ContainerId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ContainerId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(ContainerId left, ContainerId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ContainerId left, ContainerId right)
        {
            return !left.Equals(right);
        }
    }
}