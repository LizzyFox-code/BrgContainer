namespace BrgContainer.Runtime
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Mathematics;
    using UnityEngine;

    [StructLayout(LayoutKind.Sequential)]
    public struct PackedMatrix : IEquatable<PackedMatrix>
    {
        public static readonly PackedMatrix identityMatrix = new PackedMatrix(
            new float3(1, 0, 0), 
            new float3(0, 1, 0), 
            new float3(0, 0, 1), 
            new float3(0, 0, 0));
        
        public float c0x;
        public float c0y;
        public float c0z;
        
        public float c1x;
        public float c1y;
        public float c1z;
        
        public float c2x;
        public float c2y;
        public float c2z;
        
        public float c3x;
        public float c3y;
        public float c3z;

        public readonly PackedMatrix inverse
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Inverse(this);
        }

        public readonly float4x4 fullMatrix
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetFullMatrix(this);
        }

        public readonly float determinant
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetDeterminant(this);
        }

        public readonly PackedMatrix transpose
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Transpose(this);
        }

        public PackedMatrix(Matrix4x4 m)
        {
            c0x = m.m00;
            c0y = m.m10;
            c0z = m.m20;
            
            c1x = m.m01;
            c1y = m.m11;
            c1z = m.m21;
            
            c2x = m.m02;
            c2y = m.m12;
            c2z = m.m22;
            
            c3x = m.m03;
            c3y = m.m13;
            c3z = m.m23;
        }

        public PackedMatrix(float3 c0, float3 c1, float3 c2, float3 c3)
        {
            c0x = c0.x;
            c0y = c0.y;
            c0z = c0.z;
            
            c1x = c1.x;
            c1y = c1.y;
            c1z = c1.z;
            
            c2x = c2.x;
            c2y = c2.y;
            c2z = c2.z;
            
            c3x = c3.x;
            c3y = c3.y;
            c3z = c3.z;
        }

        public PackedMatrix(float4x4 matrix)
        {
            c0x = matrix.c0.x;
            c0y = matrix.c0.y;
            c0z = matrix.c0.z;
            
            c1x = matrix.c1.x;
            c1y = matrix.c1.y;
            c1z = matrix.c1.z;
            
            c2x = matrix.c2.x;
            c2y = matrix.c2.y;
            c2z = matrix.c2.z;
            
            c3x = matrix.c3.x;
            c3y = matrix.c3.y;
            c3z = matrix.c3.z;
        }

        public PackedMatrix SetIdentity()
        {
            this = identityMatrix;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetPosition()
        {
            return new float3(c3x, c3y, c3z);
        }

        public quaternion GetRotation()
        {
            return new quaternion(fullMatrix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetForward()
        {
            return new float3(c2x, c2y, c2z);
        }
        
        public PackedMatrix SetTranslate(float3 position)
        {
            c0x = 1.0f;
            c0y = 0.0f;
            c0z = 0.0f;
            
            c1x = 0.0f;
            c1y = 1.0f;
            c1z = 0.0f;
            
            c2x = 0.0f;
            c2y = 0.0f;
            c2z = 1.0f;
            
            c3x = position.x;
            c3y = position.y;
            c3z = position.z;

            return this;
        }
        
        public PackedMatrix SetRotation(quaternion rotation)
        {
            var c0 = math.mul(rotation, new float3(1.0f, 0.0f, 0.0f));
            var c1 = math.mul(rotation, new float3(0.0f, 1.0f, 0.0f));
            var c2 = math.mul(rotation, new float3(0.0f, 0.0f, 1.0f));
            
            c0x = c0.x;
            c0y = c0.y;
            c0z = c0.z;
            
            c1x = c1.x;
            c1y = c1.y;
            c1z = c1.z;
            
            c2x = c2.x;
            c2y = c2.y;
            c2z = c2.z;
            
            c3x = 0.0f;
            c3y = 0.0f;
            c3z = 0.0f;

            return this;
        }

        public PackedMatrix SetScale(float3 scale)
        {
            c0x = scale.x;
            c0y = 0.0f;
            c0z = 0.0f;
            
            c1x = 0.0f;
            c1y = scale.y;
            c1z = 0.0f;
            
            c2x = 0.0f;
            c2y = 0.0f;
            c2z = scale.z;
            
            c3x = 0.0f;
            c3y = 0.0f;
            c3z = 0.0f;

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedMatrix TRS(float3 position, quaternion rotation, float3 scale)
        {
            var c0 = math.mul(rotation, new float3(scale.x, 0.0f, 0.0f));
            var c1 = math.mul(rotation, new float3(0.0f, scale.y, 0.0f));
            var c2 = math.mul(rotation, new float3(0.0f, 0.0f, scale.z));
            var c3 = position;
            
            return new PackedMatrix(c0, c1, c2, c3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedMatrix Inverse(PackedMatrix matrix)
        {
            var fullMatrix = GetFullMatrix(matrix);
            var inversedMatrix = math.inverse(fullMatrix);

            return new PackedMatrix(inversedMatrix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 GetFullMatrix(PackedMatrix matrix)
        {
            var c0 = new float4(matrix.c0x, matrix.c0y, matrix.c0z, 0.0f);
            var c1 = new float4(matrix.c1x, matrix.c1y, matrix.c1z, 0.0f);
            var c2 = new float4(matrix.c2x, matrix.c2y, matrix.c2z, 0.0f);
            var c3 = new float4(matrix.c3x, matrix.c3y, matrix.c3z, 1.0f);

            return new float4x4(c0, c1, c2, c3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetDeterminant(PackedMatrix matrix)
        {
            var fullMatrix = GetFullMatrix(matrix);
            return math.determinant(fullMatrix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedMatrix Transpose(PackedMatrix matrix)
        {
            var fullMatrix = GetFullMatrix(matrix);
            var transposedMatrix = math.transpose(fullMatrix);
            return new PackedMatrix(transposedMatrix);
        }

        public readonly bool Equals(PackedMatrix other)
        {
            return c0x.Equals(other.c0x) && c0y.Equals(other.c0y) && c0z.Equals(other.c0z) && c1x.Equals(other.c1x) &&
                   c1y.Equals(other.c1y) && c1z.Equals(other.c1z) && c2x.Equals(other.c2x) && c2y.Equals(other.c2y) &&
                   c2z.Equals(other.c2z) && c3x.Equals(other.c3x) && c3y.Equals(other.c3y) && c3z.Equals(other.c3z);
        }

        public override readonly bool Equals(object obj)
        {
            return obj is PackedMatrix other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            var c0 = new float3(c0x, c0y, c0z);
            var c1 = new float3(c1x, c1y, c1z);
            var c2 = new float3(c2x, c2y, c2z);
            var c3 = new float3(c3x, c3y, c3z);

            var hashCode0 = c0.GetHashCode();
            var hashCode1 = hashCode0 ^ c1.GetHashCode() << 2;
            var hashCode2 = hashCode1 ^ c2.GetHashCode() >> 2;
            var hashCode3 = hashCode2 ^ c3.GetHashCode() >> 1;

            return hashCode3;
        }
    }
}