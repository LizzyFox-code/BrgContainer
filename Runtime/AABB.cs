namespace BrgContainer.Runtime
{
    using Unity.Mathematics;
    using System.Runtime.InteropServices;
    
    [StructLayout(LayoutKind.Sequential)]
    public struct AABB
    {
        public float3 Center;
        public float3 Extents;
        
        public float3 Size => Extents * 2;

        public float3 Min => Center - Extents;
        public float3 Max => Center + Extents;

        public bool Contains(float3 point)
        {
            return !math.any(point < Min | Max < point);
        }
        
        public bool Contains(AABB b)
        {
            return !math.any(b.Max < Min | Max < b.Min);
        }
        
        public static AABB Transform(float4x4 transform, AABB localBounds)
        {
            AABB transformed;
            transformed.Extents = RotateExtents(localBounds.Extents, transform.c0.xyz, transform.c1.xyz, transform.c2.xyz);
            transformed.Center = math.transform(transform, localBounds.Center);
            return transformed;
        }
        
        public float DistanceSq(float3 point)
        {
            return math.lengthsq(math.max(math.abs(point - Center), Extents) - Extents);
        }
        
        private static float3 RotateExtents(float3 extents, float3 m0, float3 m1, float3 m2)
        {
            return math.abs(m0 * extents.x) + math.abs(m1 * extents.y) + math.abs(m2 * extents.z);
        }
    }
}