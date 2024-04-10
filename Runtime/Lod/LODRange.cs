namespace BrgContainer.Runtime.Lod
{
    using System.Runtime.InteropServices;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct LODRange
    {
        public readonly float MinDistance;
        public readonly float MaxDistance;

        public LODRange(float minDistance, float maxDistance)
        {
            MinDistance = minDistance;
            MaxDistance = maxDistance;
        }

        public static LODRange Create(float4 lodDistances0, float4 lodDistances1, int lodMask)
        {
            var minDist = float.MaxValue;
            var maxDist = 0.0F;
            if ((lodMask & 0x01) == 0x01)
            {
                minDist = 0.0f;
                maxDist = math.max(maxDist, lodDistances0.x);
            }
            if ((lodMask & 0x02) == 0x02)
            {
                minDist = math.min(minDist, lodDistances0.x);
                maxDist = math.max(maxDist, lodDistances0.y);
            }
            if ((lodMask & 0x04) == 0x04)
            {
                minDist = math.min(minDist, lodDistances0.y);
                maxDist = math.max(maxDist, lodDistances0.z);
            }
            if ((lodMask & 0x08) == 0x08)
            {
                minDist = math.min(minDist, lodDistances0.z);
                maxDist = math.max(maxDist, lodDistances0.w);
            }
            if ((lodMask & 0x10) == 0x10)
            {
                minDist = math.min(minDist, lodDistances0.w);
                maxDist = math.max(maxDist, lodDistances1.x);
            }
            if ((lodMask & 0x20) == 0x20)
            {
                minDist = math.min(minDist, lodDistances1.x);
                maxDist = math.max(maxDist, lodDistances1.y);
            }
            if ((lodMask & 0x40) == 0x40)
            {
                minDist = math.min(minDist, lodDistances1.y);
                maxDist = math.max(maxDist, lodDistances1.z);
            }
            if ((lodMask & 0x80) == 0x80)
            {
                minDist = math.min(minDist, lodDistances1.z);
                maxDist = math.max(maxDist, lodDistances1.w);
            }

            return new LODRange(minDist, maxDist);
        }
    }
}