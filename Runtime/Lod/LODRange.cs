namespace BrgContainer.Runtime.Lod
{
    using System.Runtime.InteropServices;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    internal struct LODRange
    {
        public float MinDist;
        public float MaxDist;

        public static LODRange Create(float4 lodDistances, int lodMask)
        {
            var minDist = float.MaxValue;
            var maxDist = 0.0F;
            if ((lodMask & 0x01) == 0x01)
            {
                minDist = 0.0f;
                maxDist = math.max(maxDist, lodDistances.x);
            }
            if ((lodMask & 0x02) == 0x02)
            {
                minDist = math.min(minDist, lodDistances.x);
                maxDist = math.max(maxDist, lodDistances.y);
            }
            if ((lodMask & 0x04) == 0x04)
            {
                minDist = math.min(minDist, lodDistances.y);
                maxDist = math.max(maxDist, lodDistances.z);
            }
            if ((lodMask & 0x08) == 0x08)
            {
                minDist = math.min(minDist, lodDistances.z);
                maxDist = math.max(maxDist, lodDistances.w);
            }

            return new LODRange
            {
                MinDist = minDist,
                MaxDist = maxDist
            };
        }
    }
}